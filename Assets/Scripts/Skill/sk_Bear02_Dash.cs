using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sk_Bear02_Dash : Skill
{
    public override int[] damage { get; set; } = { 15, 15, 15, 25, 25};

    public override float skillDistance { get; set; } = 3f;
    
    /// <summary>
    /// 특정 지점으로 점프합니다.
    /// </summary>
    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();
        //animation time 1.667
        
        //시선 보정
        targetPos = player.GetHitPoint();
        player._animator.transform.LookAt(targetPos);

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.CAST);

        //애니메이션
        //player._animator.SetTrigger("JUMP");
        player.SetAnimTrigger("JUMP");

        //부가기능 구현
        Vector3 dir = targetPos - player.transform.position;
        dir.Normalize();
        dir += Vector3.up;
        Rigidbody rigid = player.GetComponent<Rigidbody>();
        rigid.AddForce(dir * 400f);

        //선 이펙트 
        yield return new WaitForSeconds(1.667f);

        //부가기능 초기화
        rigid.velocity = Vector3.zero;

        //후 이펙트
        //MainManager.instance.SetActive_SkillEffect("DashHit", player.transform.position);
        MainManager.instance.SetActive_SkillEffect("DashHit", player._animator.transform);

        //데미지 OnTriggerEnter
        projectile.transform.position = player.transform.position;
        projectile.gameObject.SetActive(true);

        //상태 변환
        player.Set_StateMachine(PLAYER_STATE.IDLE);

        //콜라이더 OFF
        yield return new WaitForSeconds(0.05f);
        projectile.gameObject.SetActive(false);
        /*
        //Method : Bezier
        Vector3 startPos = player.transform.position;
        Vector3 endPos = player._animator.transform.forward * skillDistance;
        Vector3 middlePos = (startPos + endPos) * 0.5f + Vector3.up * 3;
        float tt = 0f;
        float speed = 1f;
        for (float t = 1.667f; t >= 0f; t-=Time.deltaTime)
        {
            player.transform.position = Bezier(startPos, middlePos, endPos, tt);
            tt = Mathf.Min(1, tt + Time.deltaTime * speed);

            yield return null;
        }
        */
    }

    Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float tt)
    {
        var omt = 1f - tt;
        return a * omt * omt + 2f * b * omt * tt + c * tt * tt;
    }

    public override string Get_FullDescription()
    {
        //return base.Get_FullDescription();
        return "특정 방향으로 크게 점프합니다. 착지하는 순간 주변의 대상에게 " + GetDesc_Damage() + "의 " + GetDesc_DamageType() + "을 입힙니다.";
    }
}
