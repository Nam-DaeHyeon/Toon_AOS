using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

//둔화
public class sk_Rabbit01_IceArrow : Skill
{
    public override bool directPop { get; set; } = false;
    public override int skillDistance { get; set; } = 1;
    public override int skillAngle { get; set; } = 0;

    public override int[] mdamage { get; set; } = { 25, 25, 30, 30, 35 };

    public override float duration { get; set; } = 3.5f;  //둔화 지속시간

    public override float skillMissileSpeed { get; set; } = 20f;
    public override float skillMissileExistTime { get; set; } = 0.6f;

    /// <summary>
    /// 맞으면 둔화 효과가 발생하는 투사체를 발사합니다.
    /// </summary>
    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();
        //time 1.876

        //시선 보정
        targetPos = player.GetHitPoint();
        player._animator.transform.LookAt(targetPos);

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.CAST);

        //애니메이션
        player.SetAnimTrigger("ATTACK");

        //부가기능 구현

        //선 이펙트

        yield return new WaitForSeconds(0.85f);

        //부가기능 초기화

        //후 이펙트
        MainManager.instance.SetActive_SkillEffect("IceArrowProjectile", player._animator.transform, projectile.transform);
        GameObject effectObj = MainManager.instance.Get_SkillEffectObj("IceArrowProjectile");

        //... 프로젝타일과 이펙트가 같이 날아가도록 부모 종속 처리
        if (effectObj.transform.parent != projectile.transform)
        {
            effectObj.transform.parent = projectile.transform;
            effectObj.transform.position = projectile.transform.position;
            effectObj.transform.rotation = projectile.transform.rotation;
        }
        //projectile.transform.position = player.transform.position + Vector3.up;
        //projectile.gameObject.SetActive(true);
        //projectile.SetPlay_Missile(player.GetForwordDir_LineRender());
        MainManager.instance.Set_ActiveProjectile(projectile.gameObject, true);

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.IDLE);

        yield return new WaitWhile(() => projectile.myOption.Equals(PROJECTILE_OPTION.투사체));

        //projectile.gameObject.SetActive(false);
        MainManager.instance.Set_ActiveProjectile(projectile.gameObject, false);
        MainManager.instance.SetUnActive_SkillEffect("IceArrowProjectile");

        if (projectile.myOption.Equals(PROJECTILE_OPTION.투사체충돌발생))
        {
            MainManager.instance.SetActive_SkillEffect("IceArrowHit", projectile.transform);

            //데미지 OnTriggerEnter
            projectile.Modify_ColliderRadius(2f);
            projectile.SetPlay_SimpleThings();
            projectile.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.05f);

            //콜라이더 OFF
            projectile.gameObject.SetActive(false);

            //둔화 처리
            foreach(Player target in projectile.colliderPlayers)
            {
                if (target == null) continue;
                //StartCoroutine(IE_SlowTimer(target));
                target.GetBuff_FromOthers("SPEED", (int)(target.Get_Speed() * 0.5F), false, duration);
            }
        }
    }
}
