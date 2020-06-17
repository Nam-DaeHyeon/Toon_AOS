using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sk_Bear04_Frenzy : Skill
{
    public override int[] damage { get; set; } = { 25, 30, 35 };
    public override float duration { get; set; } = 10f;

    public override int Get_MaxLevel()
    {
        return 3;
    }

    /// <summary>
    /// 지속시간 동안 공격력 및 방어력, 마방, 이동 속도가 증가합니다.
    /// </summary>
    /// <returns></returns>
    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();

        //시선 보정

        //상태 변환

        //애니메이션

        //부가기능 구현
        string[] param = { "ATTACKDAMAGE", "DEFENCE", "MDEFENCE", "SPEED" };
        int[] values = { (int)(damage[skillLevel - 1] * 0.5f), damage[skillLevel - 1], damage[skillLevel - 1], (int)(player.Get_Speed() * 0.75f) };
        for(int i = 0; i < param.Length; i++)
        {
            player.UpdateParamAboutBuff(param[i], values[i], true);
        }

        //선 이펙트
        MainManager.instance.SetActive_SkillEffect("Frenzy", player._animator.transform, player.transform);

        yield return new WaitForSeconds(duration);

        //부가기능 초기화
        for (int i = 0; i < param.Length; i++)
        {
            player.UpdateParamAboutBuff(param[i], values[i], false);
        }

        //후 이펙트
        MainManager.instance.SetUnActive_SkillEffect("Frenzy");

        //데미지 OnTriggerEnter

        //상태 변환

        //콜라이더 OFF
    }

    public override string Get_FullDescription()
    {
        //return base.Get_FullDescription();

        string atk = (int)(damage[0] * 0.5f) + "/" + (int)(damage[1] * 0.5f) + "/" + (int)(damage[2] * 0.5f);
        string dfc = damage[0] + "/" + damage[1] + "/" + damage[2];
        string spd = ((int)(player.Get_Speed() * 0.75f)).ToString();
        return "분노 상태에 돌입합니다. " + duration + "초 동안 공격력 " + atk + ", 방어력 " + dfc + ", 마법방어력 " + dfc + ", 이동속도 " + spd + "만큼 증가합니다.";
    }
}
