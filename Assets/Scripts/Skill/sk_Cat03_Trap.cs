using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

//속박 함정 설치
public class sk_Cat03_Trap : Skill
{
    public override int[] damage { get; set; } = { 5, 5, 10, 10, 15 };

    public override float duration { get; set; } = 2f;

    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();

        player.SetAnimTrigger("IDLE");

        GameObject trap = PhotonNetwork.Instantiate("Trap", player.transform.position, player._animator.transform.rotation);
        trap.GetComponent<FireTrap>().Set_InitParameter(player, damage[skillLevel - 1], duration);

        yield return new WaitForSeconds(1.5f);

        trap.GetComponent<FireTrap>().Set_ReadyMode();
    }
}
