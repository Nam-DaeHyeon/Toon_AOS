﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealField : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<Player>())
        {
            other.GetComponent<Player>().TakeHeal(1000);
        }
    }
}
