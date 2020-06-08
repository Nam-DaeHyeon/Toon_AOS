using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//자신 주변에서 적을 밀치고 얼리기
public class sk_Rabbit04_Blizard : Skill
{
    public override int[] mdamage { get; set; } = { 40, 50, 60 };
    public override float duration { get; set; } = 3f;

    public override int skillAngle { get; set; } = 360;
    public override int skillDistance { get; set; } = 8;

    public override int Get_MaxLevel()
    {
        //return base.Get_MaxLevel();
        return 3;
    }

    /// <summary>
    /// 지속시간동안 무적 지역을 생성합니다. 해당지역에서 플레이어는 무적, 적은 지속적인 데미지를 받습니다. 
    /// 움직이거나, 스킬버튼을 뗐을 때 캔슬됩니다.
    /// </summary>
    /// <returns></returns>
    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();

        //시선 보정
        //...

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.CAST);

        //애니메이션
        player.SetAnimTrigger("ATTACK");

        //부가 기능 구현

        //선 이펙트

        yield return new WaitForSeconds(1.2f);
        
        //후 이펙트
        MainManager.instance.SetActive_SkillEffect("Blizard", player._animator.transform, player.transform);
        projectile.transform.position = player.transform.position;
        projectile.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.05f);

        //부가 기능 초기화
        player.Set_StateMachine(PLAYER_STATE.IDLE);
        projectile.gameObject.SetActive(false);

        foreach(var targetPlayer in projectile.colliderPlayers)
        {
            //targetPlayer.TakeMDamage(mdamage[skillLevel - 1]);
            targetPlayer.GetBuff_FromOthers("SPEED", (int)(targetPlayer.Get_Speed() * 0.5F), false, duration);

            //StartCoroutine(IE_PushTargetPlayer(targetPlayer));
            MainManager.instance.SkillFunc_PushTarget(player.transform.position, targetPlayer.transform, skillDistance, 1, duration);
        }
    }
    
    
}
