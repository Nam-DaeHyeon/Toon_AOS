using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillViewer : MonoBehaviour
{
    public UI_SkillSlot[] slots { get; set; }
    public UI_SkillDesc desc { get; set; }

    private void Awake()
    {
        slots = GetComponentsInChildren<UI_SkillSlot>();
        desc = GetComponentInChildren<UI_SkillDesc>();
    }
}
