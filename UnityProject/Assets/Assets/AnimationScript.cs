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
    public AK.Wwise.Event resumeEvent;
    public AK.Wwise.Event seekEvent;

    private GameObject bubble;
   
    public GameObject hipBone;

    public Material mat;


    void Start()
    {
        Debug.Log(gameObject.name + " duration: " + clip.length);
        Debug.Log(gameObject.name + " hipPos: " + hipBone.transform.position);
        bubble = GameObject.FindGameObjectsWithTag("Bubble")[0];
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




    public void Update()
    {
        setVisibility();
    }


    public void updateAnimTime(float _animTime)
    {
        animationtime = _animTime;
        
        clip.SampleAnimation(gameObject, animationtime);
    }


    

    
  

}
