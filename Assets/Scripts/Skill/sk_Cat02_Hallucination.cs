using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// 특정 지점으로 분신을 날리고 자신은 투명화
public class sk_Cat02_Hallucination : Skill
{
    public override bool directPop { get; set; } = true;

    public override float duration { get; set; } = 3f;

    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();

        //시선 보정
        targetPos = player.GetHitPoint();
        player._animator.transform.LookAt(targetPos);

        //player.SetAnimTrigger("IDLE");

        Vector3 originWCvsLocalPos = player.UI_WorldCvs.transform.localPosition;
        player.Set_HidingState(player.photonView.ViewID);
        CreateDecoy();

        //디코이 캔슬 체크
        Skill[] skills = player.Get_PrivateSkills();
        for(float timer = duration; timer >= 0; timer -= Time.deltaTime)
        {
            //중도에 공격한다면 캔슬
            if (player.isMeleeAttack) break;
            //중도에 스킬을 사용한다면 캔슬
            if (Check_CastingSkill(skills[0], KeyCode.Q)) break;
            //if (Check_CastingSkill(skills[1], KeyCode.W)) break;
            if (Check_CastingSkill(skills[2], KeyCode.E)) break;
            if (Check_CastingSkill(skills[3], KeyCode.R)) break;

            yield return null;
        }
        
        player.Set_UnHidingState(player.photonView.ViewID);
    }
    
    private bool Check_CastingSkill(Skill skill, KeyCode keyCode)
    {
        if (skill.skillLevel == 0) return false;

        if (skill.directPop && Input.GetKeyDown(keyCode)) return true;
        if (!skill.directPop && Input.GetKeyUp(keyCode)) return true;

        return false;
    }

    private void CreateDecoy()
    {
        GameObject decoyObj = PhotonNetwork.Instantiate("Toon_Cat_D", player._animator.transform.position, player._animator.transform.rotation);
        //나 아닌 다른 클라이언트에서는 디코이에 HUD가 달려있게 보이도록 한다.
        MainManager.instance.SetParent_WorldCanvas(decoyObj.GetPhotonView().ViewID, player.photonView.ViewID);
        //if (!photonView.IsMine) player.UI_WorldCvs.transform.parent = decoyObj.transform;   //HUD 낚시

        StartCoroutine(IE_MoveDecoy(decoyObj, player.Get_Speed()));
    }
    
    private void DeleteDecoy(int viewId)
    {
        //HUD 원상 복귀
        MainManager.instance.SetParent_WorldCanvas(player.photonView.ViewID, player.photonView.ViewID);
        MainManager.instance.Destroy_PhotonViewObject(viewId);
    }

    IEnumerator IE_MoveDecoy(GameObject decoy, float speed)
    {
        int viewId = decoy.GetPhotonView().ViewID;
        MainManager.instance.SetAnimatorTrigger_Decoy(viewId, "MOVE");

        float timer = duration;
        while(timer >= 0)
        {
            if (decoy == null) yield break;
            decoy.transform.Translate(decoy.transform.forward * speed * Time.deltaTime, Space.World);
            timer -= Time.deltaTime;
            yield return null;
        }

        DeleteDecoy(viewId);
    }

    public override string Get_FullDescription()
    {
        //return base.Get_FullDescription();
        return "특정 방향으로 분신을 만들어 달려가도록 합니다. 분신은 " + duration + "초 동안 지속되며, 자신은 투명상태가 됩니다. 투명상태는 공격을 하거나 스킬을 사용하면 해제됩니다.";
    }
}
