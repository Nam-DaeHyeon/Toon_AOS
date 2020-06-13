using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FireTrap : MonoBehaviourPun
{
    Player owner;
    float damage = 0;
    float duration = 0;
    bool _readyOn = false;

    bool hasColl = false;

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<Player>() != null)
        {
            //중복 충돌 방지
            if (hasColl) return;

            //설치 완료 대기시간 중 충돌 무시
            if (!_readyOn) return;

            //설치 유저 충돌 무시
            if (other.transform.GetComponent<Player>().Equals(owner)) return;


            //other.GetComponent<Player>().TakeDamage(other.GetComponent<Photon.Pun.PhotonView>().Owner.UserId, 10);
            Player target = other.transform.GetComponent<Player>();
            target.TakeDamage_IgnoreDefence(damage);
            target.GetBuff_FromOthers("SPEED", (int)target.Get_Speed(), false, duration);

            //MainManager.instance.SetActive_SkillEffect("TrapHit", transform);
            MainManager.instance.SetActive_SharedEffect("TrapHit", transform);
            photonView.RPC("CallbackRPC_DeleteTrap", RpcTarget.All, photonView.ViewID);

            hasColl = true;
        }
    }

    [PunRPC]
    private void CallbackRPC_DeleteTrap(int viewId)
    {
        GameObject trapObj = PhotonView.Find(viewId).gameObject;
        if (trapObj == null) return;

        Destroy(trapObj);
    }

    public void Set_InitParameter(Player owner, int damage, float duration)
    {
        this.owner = owner;
        this.damage = damage;
        this.duration = duration;

        _readyOn = false;
    }

    public void Set_ReadyMode()
    {
        _readyOn = true;
    }
}
