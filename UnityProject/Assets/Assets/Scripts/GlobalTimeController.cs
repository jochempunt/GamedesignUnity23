using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AK.Wwise;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum PlayState
{
    PLAYING,
    PAUSED,
    STOPPED
}

enum DIRECTION
{
    FORWARDS,
    BACKWARDS
}

public class GlobalTimeController : MonoBehaviour
{
    // Start is called before the first frame update

    public TextMeshProUGUI forwardText;
    public TextMeshProUGUI sliderTime;
    public Slider slider;




    private GameObject[] objectsToAnimate;

    [SerializeField]
    [Range(0.0f, 4.0f)]
    private float globalAnimTime;

    [SerializeField]
    private float velocity = 0.5f;

    [SerializeField]
    private float acceleration;

    private float maxTimeframe = 5;

    [SerializeField]
    private AK.Wwise.RTPC soundSpeed;




    //private bool timeScratchIsPlaying = false;
    public PlayState animationSoundIsPlaying = PlayState.STOPPED;
    private bool forwards = true;


    [SerializeField]
    private float currentvelocity = 1;

    private float soundSpeedFloat = 0;

    private List<uint> actual_playingID = new List<uint>();

    private DIRECTION previousDirection = DIRECTION.FORWARDS;

    private DIRECTION currentDirection = DIRECTION.FORWARDS;








    void Start()
    {
        objectsToAnimate = GameObject.FindGameObjectsWithTag("AnimObject");
        Debug.Log(objectsToAnimate.Length);
        globalAnimTime = 0.0f;
        //currentvelocity = 0.0f;
        soundSpeed.SetGlobalValue(velocity);
        soundSpeedFloat = currentvelocity;
        AkSoundEngine.SetState("TimeDirection", "Forwards");

    }

    void UpdateAnimations()
    {
        foreach (GameObject obj in objectsToAnimate)
        {
            obj.GetComponent<AnimationScript>().updateAnimTime(globalAnimTime);
        }


    }



    // Update is called once per frame


    private float GetSeekDirectionPercentage()
    {
        float currentPercentage = (globalAnimTime / 5.0f) * 100;
        Debug.Log("current% =" + currentPercentage + " new % = : " + (Mathf.Abs(100 - currentPercentage)));
        return Mathf.Abs(100 - currentPercentage);

    }

    private void Seek(float _percentage, uint eventID, GameObject obj)
    {

        //uint eventID = obj.GetComponent<TimeScript>().playEvent.Id;
        AkSoundEngine.SeekOnEvent(eventID, obj, 4500);

    }

    private void FixedUpdate()
    {
        UpdateAnimations();

    }


    private void SeekObjectInMs(GameObject g, int ms)
    {
        AK.Wwise.Event eventW = g.GetComponent<AnimationScript>().playEvent;
        uint playID = eventW.Post(g);
        AkSoundEngine.SeekOnEvent(eventW.Id, g, ms);
    }







    private void Update()
    {





        soundSpeed.SetGlobalValue(velocity);
        sliderTime.text = (Mathf.Round(globalAnimTime * 100f) / 100f)  + "s";

        if (animationSoundIsPlaying == PlayState.PLAYING)
        {
            slider.value = globalAnimTime;
            slider.interactable = false;

        }
        else
        {
            globalAnimTime = slider.value;
            slider.interactable = true;
        }


        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (animationSoundIsPlaying == PlayState.STOPPED || animationSoundIsPlaying == PlayState.PAUSED)
            {
                if (forwards)
                {
                    forwards = false;
                    forwardText.text = "Backwards";
                    AkSoundEngine.SetState("TimeDirection", "Backwards");
                    //AkSoundEngine.SeekOnEvent(objectsToAnimate[0].GetComponent<AnimationScript>().playEvent.Id, objectsToAnimate[0], 3000, false, actual_playingID[0]);
                    currentDirection = DIRECTION.BACKWARDS;
                }

            }

        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (animationSoundIsPlaying == PlayState.STOPPED || animationSoundIsPlaying == PlayState.PAUSED)
            {
                if (!forwards)
                {
                    forwards = true;
                    forwardText.text = "Forwards";
                    AkSoundEngine.SetState("TimeDirection", "Forwards");
                    //seekPercentageOnDirectionChange = GetSeekDirectionPercentage();
                    currentDirection = DIRECTION.FORWARDS;
                }


            }

        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayPause();
        }

        if (animationSoundIsPlaying == PlayState.PLAYING)
        {
            TimeUpdateFB(forwards);
        }


    }

    public void resetTime()
    {
        globalAnimTime = 0;
    }
    public static float CalculateOpposite(float number)
    {
        if (number >= 0 && number <= 5)
        {
            return 5.0f - number;
        }
        else
        {
            Debug.LogError("Input number must be between 0 and 5.");
            return 0.0f;

        }
    }



    void PlayPause()
    {
        switch (animationSoundIsPlaying)
        {
            case PlayState.PLAYING:
                animationSoundIsPlaying = PlayState.PAUSED;


                int i = 0;
                foreach (GameObject obj in objectsToAnimate)
                {
                    obj.GetComponent<AnimationScript>().pauseEvent.Post(obj);

                    if (actual_playingID.Count > 1)
                    {
                        AkSoundEngine.StopPlayingID(actual_playingID[i]);
                        actual_playingID.Remove(actual_playingID[i]);
                        i++;
                    }
                }



                break;
            case PlayState.PAUSED:
                animationSoundIsPlaying = PlayState.PLAYING;
                SeekAllAnims();

                if (previousDirection != currentDirection)
                {
                    previousDirection = currentDirection;
                }

                break;
            case PlayState.STOPPED:
                if ((!forwards && globalAnimTime >= 0) || (forwards && globalAnimTime <= maxTimeframe))
                {
                    animationSoundIsPlaying = PlayState.PLAYING;

                    SeekAllAnims();

                    previousDirection = currentDirection;

                }
                break;
        }

    }


    void SeekAllAnims()
    {
        foreach (GameObject obj in objectsToAnimate)
        {
            int shiftTime = (int)(globalAnimTime * 1000);
            if (currentDirection == DIRECTION.BACKWARDS)
            {
                shiftTime = (int)(CalculateOpposite(globalAnimTime) * 1000);
            }
            foreach (GameObject objj in objectsToAnimate)
            {
                SeekObjectInMs(obj, shiftTime);
            }
            //obj.GetComponent<AnimationScript>().playEvent.Post(obj);
        }
    }
    // function to increase/decrease the time variable of the animation (forwards or backwards)
    void TimeUpdateFB(bool forwards)
    {


        Debug.Log(globalAnimTime);

        if (forwards && globalAnimTime <= maxTimeframe)
        {
            globalAnimTime += Time.deltaTime * velocity;

            return;
        }
        if (globalAnimTime >= 0)
        {
            globalAnimTime -= Time.deltaTime * velocity;
        }

        if (globalAnimTime <= 0 && !forwards)
        {
            globalAnimTime = 0;
            animationSoundIsPlaying = PlayState.STOPPED;

        }

        if (globalAnimTime >= maxTimeframe)
        {
            globalAnimTime = maxTimeframe;
            animationSoundIsPlaying = PlayState.STOPPED;
        }
    }


    void TimeUpdateAccel(bool forwards)
    {
        float targetVelocity = velocity * (forwards ? 1 : -1);
        currentvelocity = Mathf.Lerp(currentvelocity, targetVelocity, acceleration * Time.deltaTime);
        currentvelocity = Mathf.Clamp(currentvelocity, -1.0f, 1.0f); // Clamp the current velocity to a maximum of 1
        //speed.SetGlobalValue(Mathf.Abs(currentvelocity * 100));
        float tempglobalAnimTime = globalAnimTime + Time.deltaTime * currentvelocity;
        globalAnimTime = Mathf.Clamp(tempglobalAnimTime, 0f, maxTimeframe);
    }








}
