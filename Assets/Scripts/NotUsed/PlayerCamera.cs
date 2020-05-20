using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    Camera _camera;

    float maxFadeDistance = 5f;
    float minFadeDistance = 0f;
    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        //HandleFading();
    }

    //void HandleFading()
    private void Update()
    {
        float fadeSpread  = maxFadeDistance - minFadeDistance;
        float ratio;
        float dist;
        while (true)
        {
            dist = (transform.position - Camera.main.transform.position).sqrMagnitude;
            if (dist < minFadeDistance)
            {
                GetComponent<Renderer>().material.color = Color.white;
                continue;
            }
            if (dist > maxFadeDistance)
            {
                GetComponent<Renderer>().material.color = Color.black;
                continue;
            }
            ratio = ((transform.position - Camera.main.transform.position).sqrMagnitude - minFadeDistance) / fadeSpread;
            ratio = Mathf.Min(ratio, 1.0f);
            GetComponent<Renderer>().material.color = Color.white * (1 - ratio);
            // optionally:  renderer.material.color.a = 1.0;
        }
    }
}
