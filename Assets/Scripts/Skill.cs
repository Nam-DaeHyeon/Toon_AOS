using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill : MonoBehaviour
{
    /// <summary>
    /// 재사용 대기시간
    /// </summary>
    public virtual float coolTime { set; get; } = 1f;

    /// <summary>
    /// 타겟팅이 필요없이 바로 시전되는 스킬 여부
    /// </summary>
    public virtual bool directPop { set; get; } = true;

    public int skillLevel { set; get; } = 0;

    public virtual int damage { set; get; }
    public virtual int mdamage { set; get; }

    public Sprite skillImage;

    private void Awake()
    {
        skillImage = Resources.Load<Sprite>("Skill/" + Get_SkillName());
    }

    public string Get_SkillName()
    {
        string[] temp = GetType().ToString().Split('_');
        return temp[2];
    }

    public virtual string Get_SkillDesc()
    {
        return "이것은 스킬입니다.";
    }

    public virtual int Get_MaxLevel()
    {
        return 5;
    }

    public virtual void Use_Skill()
    {

    }
}
