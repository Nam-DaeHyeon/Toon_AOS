using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sk_Bear03_Guard : Skill
{
    public override int[] damage { get; set; } = { 40, 50, 60, 70, 80 };
    public override float duration { get; set; } = 3f;
    /// <summary>
    /// 쉴드를 생성한다. 쉴드량은 Damage를 참조한다.
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

        //부가 기능 구현
        player.Set_MaxShield(damage[skillLevel - 1]);

        //선 이펙트
        MainManager.instance.SetActive_SkillEffect("Guard", player._animator.transform, player.transform);

        //yield return new WaitForSeconds(3F);

        float timer = duration;
        while(timer > 0f)
        {
            timer -= Time.deltaTime;
            if (player.Get_CurrentShield() < 0f) break;

            yield return null;
        }

        //부가 기능 초기화
        player.Set_ZeroShield();

        //후 이펙트
        MainManager.instance.SetUnActive_SkillEffect("Guard");
    }

    public override string Get_FullDescription()
    {
        //return base.Get_FullDescription();
        return "자신에게 " + GetDesc_Damage() + "의 피해를 받는 보호막을 시전합니다. 보호막은 " + duration + "초 동안 유지됩니다.";
    }
}
