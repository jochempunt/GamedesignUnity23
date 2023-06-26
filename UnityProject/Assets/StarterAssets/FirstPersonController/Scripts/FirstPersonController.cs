using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif




    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;
        [Header("Wwise Events")]
        public AK.Wwise.Event myFootsteps;
        public float footStepSpeed = 600f;


        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        // cinemachine
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        //wwise 
        private bool footStepIsPlaying = false;
        private float lastFootstepTime = 0f;
        public bool isJumping = false;

        // stuff for laserbeam
        private bool laserActive = false;
        [SerializeField] private Camera mainCamera;
        public LineRenderer lineRenderer;
        public Color hitColor;
        public Color noHitColor;


        //stuff for bubble spawning
        public GameObject bubblePrefab;
        private bool canSpawn = false;
        private GameObject currentBubble;

        [SerializeField]
        private GlobalTimeController timeController;

        //stuff for sound
        public AK.Wwise.RTPC inBubble;


        public AK.Wwise.Event musicPause;
        public AK.Wwise.Event musicPlay;



#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;
        private bool cursorControlEnabled = false;


        public Texture2D cursorImage;
        public Texture2D mouseClick;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            lastFootstepTime = Time.time;
            Cursor.SetCursor(cursorImage, new Vector2(30, 0), CursorMode.ForceSoftware);
            Cursor.visible = false;
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            Cursor.lockState = CursorLockMode.Confined;


            enableUI(false);


        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();
            Move();

            if (laserActive)
            {
                RaycastLaser();
                if (Input.GetKeyDown(KeyCode.Mouse0) && canSpawn)
                {
                    spawnBubble();
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) && !cursorControlEnabled)
            {
                LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
                lineRenderer.enabled = !lineRenderer.enabled;
                laserActive = lineRenderer.enabled;
            }


            if (Input.GetKeyDown(KeyCode.C))
            {
                setCursorControlls();
            }

            if (Input.GetMouseButton(0))
            {
                Cursor.SetCursor(mouseClick, new Vector2(30, 0), CursorMode.ForceSoftware);
            }
            else
            {
                Cursor.SetCursor(cursorImage, new Vector2(30, 0), CursorMode.ForceSoftware);
            }


            if (Input.GetMouseButtonDown(1))
            {
                destroyBubble();
            }

            if (currentBubble != null)
            {
                getInBubble();
            }

        }

        void deactivateLaser()
        {
            LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
            lineRenderer.enabled = false;
            laserActive = false;
        }



        private void destroyBubble()
        {
            if (currentBubble != null)
            {
                Destroy(currentBubble);
                currentBubble = null;
                musicPlay.Post(_mainCamera);
                timeController.canTimeManipulate = false;
                enableUI(false);
            }
        }

        private void enableUI(bool en)
        {


            timeController.sliderOBJ.SetActive(en);
            timeController.forwardText.enabled = en;
            timeController.sliderTime.enabled = en;



        }


        private void spawnBubble()
        {

            if (currentBubble != null)
            {
                Destroy(currentBubble);
                currentBubble = null;
            }


            // Get the mouse position in screen coordinates
            Vector3 mousePosition = Input.mousePosition;
            //Vector2 mousePosition = Mouse.current.position.ReadValue();
            Debug.Log(mousePosition);

            // Convert mouse position to ray
            Ray direction = mainCamera.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y));


            RaycastHit hit;
            Vector3 origin = new Vector3(transform.position.x - 0.3f, transform.position.y + 1f, transform.position.z + 0.7f);


            if (Physics.Raycast(origin, Vector3.Normalize(direction.direction), out hit, 10f))
            {
                bubblePrefab.transform.position = hit.point;
                Debug.Log("own position:" + transform.position + " bubble pos:" + hit.point);

                currentBubble = Instantiate(bubblePrefab);
                musicPause.Post(_mainCamera);
                timeController.canTimeManipulate = true;
                enableUI(true);
                //timeController.resetTime();
            }
            else
            {
                musicPlay.Post(_mainCamera);
            }

        }
        private void RaycastLaser()
        {
            // Get the mouse position in screen coordinates
            Vector3 mousePosition = Input.mousePosition;
            //Vector2 mousePosition = Mouse.current.position.ReadValue();

            // Convert mouse position to ray
            Ray direction = mainCamera.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y));

            Vector3 origin = new Vector3(transform.position.x - 0.3f, transform.position.y + 1f, transform.position.z + 0.7f);

            //Debug.DrawLine(origin, Vector3.Normalize(direction.direction) * 10f, Color.red);


            if (Physics.Raycast(origin, Vector3.Normalize(direction.direction), 10f))
            {

                lineRenderer.material.SetColor("_BaseColor", Color.green);
                canSpawn = true;
            }
            else
            {

                lineRenderer.material.SetColor("_BaseColor", Color.red);
                canSpawn = false;
            }

            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin + Vector3.Normalize(direction.direction) * 10);

        }



        void getInBubble()
        {
            Vector3 playerPos = transform.position;
            Vector3 bubbleCenter = currentBubble.transform.position;
            float radius = currentBubble.transform.localScale.x / 2;

            float distance = Vector3.Distance(playerPos, bubbleCenter);
            float maxDistance = radius * 0.85f; // 85% of the radius

            float mix = 1f;

            if (distance > maxDistance)
            {
                float distanceBeyondThreshold = distance - maxDistance;
                float fadePercentage = distanceBeyondThreshold / (radius - maxDistance);
                mix = Mathf.Lerp(1f, 0f, fadePercentage);
            }

            inBubble.SetGlobalValue(mix);

        }





        // UI stuffs
        private void setCursorControlls()
        {
            cursorControlEnabled = !cursorControlEnabled;

            if (cursorControlEnabled && timeController.animationSoundIsPlaying != PlayState.PLAYING)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            deactivateLaser();
        }

        private void LateUpdate()
        {
            if (!cursorControlEnabled)
            {
                CameraRotation();
            }

        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            // if there is an input
            if (_input.look.sqrMagnitude >= _threshold)
            {
                //Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                // clamp our pitch rotation
                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                // Update Cinemachine camera target pitch
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

                // rotate the player left and right
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;


            }
            else
            {
                _speed = targetSpeed;
            }



            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                // move
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;

                // wwise footstep code
                if (!footStepIsPlaying && !isJumping)
                {
                    myFootsteps.Post(gameObject);
                    lastFootstepTime = Time.time;
                    footStepIsPlaying = true;

                }
                else
                {
                    if ((_speed > 1) && (Time.time - lastFootstepTime > footStepSpeed / _speed * Time.deltaTime))
                    {
                        footStepIsPlaying = false;
                    }
                }


            }

            // move the player
            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    //for wwise
                    isJumping = true;
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);


                }
                else
                {
                    isJumping = false;
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {

                //for wwise
                isJumping = true;
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}