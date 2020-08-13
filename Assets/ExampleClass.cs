
using UnityEngine;
using System.Collections;

public class ExampleClass : MonoBehaviour
{
    [SerializeField] Material targetMat;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, targetMat);
    }
}