using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//지속 데미지 중독
public class sk_Cat01_PoisonArrow : Skill
{
    public override bool directPop { get; set; } = false;
    public override float skillDistance { get; set; } = 0.7f;
    public override int skillAngle { get; set; } = 0;

    public override int[] mdamage { get; set; } = { 6, 7, 8, 9, 10 };

    public override float duration { get; set; } = 3f;  //중독 지속시간

    public override float skillMissileSpeed { get; set; } = 35f;
    public override float skillMissileExistTime { get; set; } = 0.45f;

    /// <summary>
    /// 맞으면 둔화 효과가 발생하는 투사체를 발사합니다.
    /// </summary>
    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();

        //시선 보정
        targetPos = player.GetHitPoint();
        player._animator.transform.LookAt(targetPos);

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.CAST);

        //애니메이션
        player.SetAnimTrigger("ATTACK");

        //부가기능 구현

        //선 이펙트

        yield return new WaitForSeconds(0.2f);

        //부가기능 초기화

        //후 이펙트
        MainManager.instance.SetActive_SkillEffect("PoisonArrowProjectile", player._animator.transform, projectile.transform);
        GameObject effectObj = MainManager.instance.Get_SkillEffectObj("PoisonArrowProjectile");

        //... 프로젝타일과 이펙트가 같이 날아가도록 부모 종속 처리
        if (effectObj.transform.parent != projectile.transform)
        {
            effectObj.transform.parent = projectile.transform;
            effectObj.transform.position = projectile.transform.position;
            effectObj.transform.rotation = projectile.transform.rotation;
        }
        MainManager.instance.Set_ActiveProjectile(projectile.gameObject, true);

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.IDLE);

        yield return new WaitWhile(() => projectile.myOption.Equals(PROJECTILE_OPTION.투사체));

        //projectile.gameObject.SetActive(false);
        MainManager.instance.Set_ActiveProjectile(projectile.gameObject, false);
        MainManager.instance.SetUnActive_SkillEffect("PoisonArrowProjectile");

        if (projectile.myOption.Equals(PROJECTILE_OPTION.투사체충돌발생))
        {
            MainManager.instance.SetActive_SkillEffect("PoisonArrowHit", projectile.transform);
            
            //중독 처리 : 플레이어
            foreach (Player target in projectile.colliderPlayers)
            {
                if (target == null) continue;
                target.GetDotDam_FromOthers("POISON", (int)(target.Get_MaxHP() * 0.1F + (skillLevel - 1)), 1, duration, player);
            }

            //중독 처리 : 몬스터
            if(projectile.colMonster != null) projectile.colMonster.GetDotDam_FromOthers("POISON", (int)(projectile.colMonster.Get_MaxHP() * 0.1F + (skillLevel - 1)), 1, duration, player);
        }
    }

    public override string Get_FullDescription()
    {
        return base.Get_FullDescription() + " 투사체는 중독 상태를 발생시키며, 중독된 대상은 " + duration + "초마다 최대 체력의 10% + 0/1/2의 피해를 받습니다.";
    }
}
