using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//자기 주변 아군 힐링
public class sk_Rabbit02_Heal : Skill
{
    public override int[] mdamage { get; set; } = { 25, 35, 45, 55, 65 };

    /// <summary>
    /// 자신의 체력을 회복한다. 힐량은 mdamage를 참조한다.
    /// </summary>
    /// <returns></returns>
    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();

        //시선 보정
        //...

        //상태 변환
        //...

        //애니메이션
        player.SetAnimTrigger("IDLE");

        //부가 기능 구현
        player.TakeHeal(mdamage[skillLevel - 1]);

        //선 이펙트
        MainManager.instance.SetActive_SkillEffect("Heal", player._animator.transform, player.transform);

        yield return null;
        //yield return new WaitForSeconds(3F);

        //부가 기능 초기화

        //후 이펙트
        //MainManager.instance.SetUnActive_SkillEffect("Heal");
    }

    public override string Get_FullDescription()
    {
        //return base.Get_FullDescription();
        return "자신의 체력을 " + GetDesc_Damage() + "만큼 회복합니다.";
    }
}
