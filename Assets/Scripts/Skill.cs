using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Skill : MonoBehaviourPun
{
    /// <summary>
    /// 재사용 대기시간 (기본값 1)
    /// </summary>
    public virtual float coolTime { set; get; } = 1f;

    /// <summary>
    /// 타겟팅이 필요없이 바로 시전되는 스킬 여부 (기본값 true)
    /// </summary>
    public virtual bool directPop { set; get; } = true;

    /// <summary>
    /// 스킬 사정거리 / 충돌체 크기 (기본값 5)
    /// </summary>
    public virtual float skillDistance { set; get; } = 5f;
    /// <summary>
    /// 스킬 범위 (기본값 60 Radian)
    /// </summary>
    public virtual int skillAngle { set; get; } = 60;

    /// <summary>
    /// 투사체 스킬 속도 (기본값 0)
    /// </summary>
    public virtual float skillMissileSpeed { set; get; } = 0f;

    /// <summary>
    /// 투사체 스킬 잔존 시간 (기본 0)
    /// </summary>
    public virtual float skillMissileExistTime { set; get; } = 0f;
        
    public int skillLevel { set; get; } = 0;

    public virtual int[] damage { set; get; }
    public virtual int[] mdamage { set; get; }

    public virtual float duration { set; get; } = 0f;

    //컴포넌트
    public Sprite skillImage;
    public PlayerProjectile projectile;

    internal Player player;
    internal Vector3 targetPos;

    /// <summary>
    /// 지속시간이 있는 스킬의 경우 코루틴 동작 여부를 확인해 다른 작업을 수행시킨다.
    /// </summary>
    internal bool coroutinerigger;

    private void Awake()
    {
        //player = GetComponentInParent<Player>();  //씬에 분리했으니 추가적인 처리가 필요
        
        skillImage = Resources.Load<Sprite>("Skill/" + Get_SkillName());
    }

    /// <summary>
    /// 스킬 사용자 주소를 등록합니다.
    /// </summary>
    public void SetOwner(Player player)
    {
        this.player = player;
    }

    public string Get_SkillName()
    {
        string[] temp = GetType().ToString().Split('_');
        return temp[2];
    }

    public string Get_SkillDesc()
    {
        //return "이것은 스킬입니다.";
        return Get_FullDescription();
    }

    public virtual int Get_MaxLevel()
    {
        return 5;
    }

    /// <summary>
    /// 스킬을 사용한 플레이어의 좌표를 기준으로 스킬을 발동합니다
    /// </summary>
    public void Use_Skill()
    {
        this.targetPos = player.transform.position;

        StartCoroutine(IE_SkillProcess());
    }
    
    /// <summary>
    /// 기본값, 플레이어 위치에서 1회성 ON/OFF
    /// </summary>
    public virtual IEnumerator IE_SkillProcess()
    {
        projectile.transform.position = targetPos;
        projectile.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.05f);

        projectile.gameObject.SetActive(false);
    }

    public int Get_EffectiveDamage()
    {
        if (skillLevel == 0) return 0;
        if (damage == null) return 0;

        return damage[skillLevel - 1];
    }

    public int Get_EffectiveMagicDamage()
    {
        if (skillLevel == 0) return 0;
        if (mdamage == null) return 0;

        return mdamage[skillLevel - 1];
    }

    /// <summary>
    /// 레벨별 데미지를 반환합니다.
    /// </summary>
    /// <returns></returns>
    internal string GetDesc_Damage()
    {
        string temp = "";

        int[] param = (damage != null) ? damage : mdamage;
        for(int i = 0; i < param.Length; i++)
        {
            temp += param[i] + "/";
        }
        temp = temp.Substring(0, temp.Length - 1);
        if (temp == null) temp = "";

        return temp;
    }

    /// <summary>
    /// 피해 타입을 반환합니다.
    /// </summary>
    /// <returns></returns>
    internal string GetDesc_DamageType()
    {
        if (damage != null) return "물리 피해";
        return "마법 피해";
    }

    /// <summary>
    /// 스킬에 대한 설명을 반환합니다.
    /// [일반적인 경우] 전방의/주변의 대상에게 (0/1/2/3/4)의 물리/마법 피해를 입힙니다.
    /// [투사체의 경우] 특정 방향으로 (0/1/2/3/4)의 물리/마법 피해를 입히는 투사체를 발사합니다.
    /// </summary>
    /// <returns></returns>
    public virtual string Get_FullDescription()
    {
        string desc = "";
        if (skillMissileSpeed == 0)
        {
            string area = (skillAngle % 360 != 0)? "전방의 " : "주변의 ";
            desc = area + "대상에게 " + GetDesc_Damage() + "의 " + GetDesc_DamageType() + "를 입힙니다.";

        }
        else desc = "특정 방향으로 " + GetDesc_Damage() + "의 " + GetDesc_DamageType() + "를 입히는 투사체를 발사합니다.";

        return desc;
    }

    public bool Check_IsMagicDamage()
    {
        if (mdamage != null) return true;
        return false;
    }
}
