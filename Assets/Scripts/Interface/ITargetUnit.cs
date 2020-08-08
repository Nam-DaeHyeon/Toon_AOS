using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

interface ITargetUnit
{
    void TakeDamage(float damage);
    void TakeMDamage(float damage);
    void TakeDamage_IgnoreDefence(float damage);

    Vector3 Get_Position();

    void Set_Target(Player attacker);
}
