using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitLogLifeTimer : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(IE_LifeTimer());
    }

    private void OnDisable()
    {
        MainManager.instance.Enqueue_LogPool(gameObject);
    }

    IEnumerator IE_LifeTimer()
    {
        yield return new WaitForSeconds(0.75f);

        gameObject.SetActive(false);
    }
}
