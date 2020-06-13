using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sk_Bear01_Bash : Skill
{
    public override int[] damage { get; set; } = { 15, 22, 29, 36, 45 };
    public override bool directPop { get; set; } = false;

    public override float skillDistance { get; set; } = 3f;

    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();
        //animation time 0.833...?

        //시선 보정
        targetPos = player.GetHitPoint();
        player._animator.transform.LookAt(targetPos);

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.CAST);
        
        //애니메이션
        player.SetAnimTrigger("ATTACK");

        //선 이펙트

        yield return new WaitForSeconds(0.4f);

        //부가기능 초기화

        //후 이펙트
        //MainManager.instance.SetActive_SkillEffect("DashHit", player.transform.position);
        MainManager.instance.SetActive_SkillEffect("Bash", player._animator.transform);

        //데미지 OnTriggerEnter
        projectile.transform.position = player.transform.position;
        projectile.gameObject.SetActive(true);

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.IDLE);

        yield return new WaitForSeconds(0.05f);
        projectile.gameObject.SetActive(false);
    }
}
