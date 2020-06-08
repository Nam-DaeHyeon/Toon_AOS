using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//무적 지역을 생성합니다.
public class sk_Rabbit03_Barrier : Skill
{
    public override int[] mdamage { get; set; } = { 7, 11, 16, 20, 25 };

    public override int skillAngle { get; set; } = 360;
    public override int skillDistance { get; set; } = 6;

    public override float duration { get; set; } = 2f;

    ///////////////
    bool projectileTrigger = false;

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
        player.SetAnimTrigger("ATTACK_LOOP");

        yield return new WaitForSeconds(0.85f);

        //부가 기능 구현
        player.isInvincible = true;
        StartCoroutine(IE_ProjectileActiveProcess());

        //후 이펙트
        MainManager.instance.SetActive_SkillEffect("Barrier", player._animator.transform, player.transform);

        //yield return new WaitForSeconds(3F);

        float timer = duration * skillLevel;
        bool directMove = false;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            //지속시간 중에 키를 떼었을 때 캔슬
            if (Input.GetKeyUp(KeyCode.E))
            {
                break;
            }
            //지속시간 중에 이동 키를 눌렀을 때 캔슬
            if (Input.GetMouseButtonDown(1))
            {
                directMove = true;
                break;
            }

            yield return null;
        }

        //부가 기능 초기화
        if(!directMove) player.SetAnimTrigger("IDLE");
        player.Set_StateMachine(PLAYER_STATE.IDLE);
        player.isInvincible = false;
        projectileTrigger = false;

        //후 이펙트
        MainManager.instance.SetUnActive_SkillEffect("Barrier");
    }

    IEnumerator IE_ProjectileActiveProcess()
    {
        projectileTrigger = true;
        while (projectileTrigger)
        {
            projectile.transform.position = player.transform.position;
            projectile.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            projectile.gameObject.SetActive(false);
        }
    }
}
