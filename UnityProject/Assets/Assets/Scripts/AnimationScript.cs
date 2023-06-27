using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationScript : MonoBehaviour
{
    public AnimationClip clip;
    [Range(0.0f, 4.0f)]
    public float animationtime = 2.0f;
    // Start is called before the first frame update

    public AK.Wwise.Event playEvent;
    public AK.Wwise.Event pauseEvent;
  

    private GameObject bubble;

    private GameObject hipBone;

    public Material mat;

    public bool staticStance = false;


    private void Awake()
    {
        hipBone = FindHipBone(transform).gameObject;

    }


    public GameObject getHipbone()
    {
        if(hipBone != null)
        {
            return hipBone;
        }
        else
        {
            Debug.Log("couldnt find hipbone of: " + gameObject.name);
            return gameObject;
        }
        
    }





    private Transform FindHipBone(Transform parent)
    {
        Transform hipBone = null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name.ToLower().Contains("hip")) // Case-insensitive name comparison
            {
                hipBone = child;
                break;
            }
            else
            {
                hipBone = FindHipBone(child);
                if (hipBone != null)
                {
                    break;
                }
            }
        }

        return hipBone;
    }






    void Start()
    {
        if (!staticStance)
        {
            GameObject originalObject = gameObject;
            GameObject duplicatedObject = Instantiate(originalObject);
            duplicatedObject.GetComponent<AnimationScript>().enabled = false;
            // Disable the ObjectDuplicator script on the duplicated object
            Material duplicatedMat = CopyMaterial(duplicatedObject);
            duplicatedObject.GetComponent<AnimationScript>().mat = duplicatedMat;
            duplicatedObject.GetComponent<AnimationScript>().staticStance = true;
            duplicatedObject.GetComponent<AnimationScript>().enabled = true;

            duplicatedObject.tag = "Untagged";
            Debug.Log(gameObject.name + " duration: " + clip.length);
            setMatAlpha(0f);
        }

        Debug.Log(gameObject.name + " hipPos: " + hipBone.transform.position);

        if (GameObject.FindGameObjectsWithTag("Bubble").Length >= 1)
        {
            bubble = GameObject.FindGameObjectsWithTag("Bubble")[0];
        }
        else
        {
            bubble = null;
        }

    }


    public Material CopyMaterial(GameObject duplicate)
    {
        // Get the second child of the duplicated object
        Transform secondChild = duplicate.transform.GetChild(1);

        // Change the material of the second child
        Renderer renderer = secondChild.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a new material instance for the second child
            Material duplicatedMaterial = new Material(renderer.sharedMaterial);
            renderer.material = duplicatedMaterial;
            return duplicatedMaterial;
        }
        return null;
    }


    public void setMatAlpha(float _alpha)
    {
        Color color = mat.color;
        color.a = _alpha;
        mat.color = color;
    }

    private void setVisibility()
    {
        Vector3 hipPos = hipBone.transform.position;
        Vector3 bubbleCenter = bubble.transform.position;
        float radius = bubble.transform.localScale.x / 2;

        float distance = Vector3.Distance(hipPos, bubbleCenter);
        float maxDistance = radius * 0.85f; // 85% of the radius

        float alpha = 1f;

        if (distance > maxDistance)
        {
            float distanceBeyondThreshold = distance - maxDistance;
            float fadePercentage = distanceBeyondThreshold / (radius - maxDistance);
            alpha = Mathf.Lerp(1f, 0f, fadePercentage);
        }

        setMatAlpha(alpha);
    }

    private void setVisibilityStatic()
    {
        Vector3 hipPos = hipBone.transform.position;
        Vector3 bubbleCenter = bubble.transform.position;
        float radius = bubble.transform.localScale.x / 2;
        float distance = Vector3.Distance(hipPos, bubbleCenter);

        if (distance > radius)
        {
            setMatAlpha(1f);
            return;
        }

        setMatAlpha(0f);

    }





    public void Update()
    {

        if (bubble != null)
        {
            if (staticStance)
            {
                setVisibilityStatic();
            }
            else
            {
                setVisibility();
            }
        }
        else
        {

            if (staticStance)
            {
                setMatAlpha(1f);
            }

            if (GameObject.FindGameObjectsWithTag("Bubble").Length >= 1)
            {
                bubble = GameObject.FindGameObjectsWithTag("Bubble")[0];
            }
            else
            {
                bubble = null;
            }
        }


    }


    public void updateAnimTime(float _animTime)
    {
        animationtime = _animTime;

        clip.SampleAnimation(gameObject, animationtime);
    }







}
