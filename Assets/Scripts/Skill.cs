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

    private void Awake()
    {
        player = GetComponentInParent<Player>();
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

    /// <summary>
    /// 스킬을 사용한 플레이어의 좌표를 기준으로 스킬을 발동합니다
    /// </summary>
    public void Use_Skill()
    {
        this.targetPos = player.transform.position;

        if(player.runningSkillRoutine != null) StopCoroutine(player.runningSkillRoutine);
        player.runningSkillRoutine = StartCoroutine(IE_SkillProcess());
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

}
