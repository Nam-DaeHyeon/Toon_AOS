using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFilter : MonoBehaviour
{
    [SerializeField] Material normalMaterial;
    [SerializeField] Material grayMaterial;

    public bool setGray { get; set; }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (setGray) Graphics.Blit(source, destination, grayMaterial);
        else Graphics.Blit(source, destination, normalMaterial);
    }
}
