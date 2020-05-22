using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BushGrass : MonoBehaviourPun, IPunObservable
{
    public List<Player> _innerPlayers = new List<Player>();
    
    private void OnTriggerEnter(Collider other)
    {
        Player tempPlayer = other.GetComponent<Player>();
        if (tempPlayer != null)
        {
            _innerPlayers.Add(tempPlayer);
            //photonView.RPC("CallbackRPC_InnerPlayerAdd", RpcTarget.All, tempPlayer);
            other.GetComponent<Player>().GetIn_Bush(this);
            
            //안에 있는 애들끼리는 보이도록
            for(int i = 0; i < _innerPlayers.Count; i++)
            {
                if (_innerPlayers[i].Equals(tempPlayer)) continue;
                //_innerPlayers[i]._animator.gameObject.SetActive(true);
                _innerPlayers[i]._skinRender.material.SetColor("_Color", new Color(1, 1, 1, 0.43f));
                _innerPlayers[i].UI_WorldCvs.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Player tempPlayer = other.GetComponent<Player>();
        if (tempPlayer != null)
        {
            _innerPlayers.Remove(tempPlayer);
            //photonView.RPC("CallbackRPC_InnerPlayerRemove", RpcTarget.All, tempPlayer);
            other.GetComponent<Player>().GetOut_Bush(this);

            //안에 있는 애들은 안보이도록. 하지만, 안에 있는 개별 유저에 한해서 미처리.
            for(int i = 0; i <_innerPlayers.Count; i++)
            {
                if (_innerPlayers[i].photonView.IsMine) continue;
                //_innerPlayers[i]._animator.gameObject.SetActive(false);
                //_innerPlayers[i]._skinRender.material.SetColor("_Color", new Color(1, 1, 1, 0));
                _innerPlayers[i]._skinRender.material.SetColor("_Color", new Color(1, 1, 1, 0));
                _innerPlayers[i].UI_WorldCvs.gameObject.SetActive(false);
            }
        }
    }

    public bool Check_InnerPlayer(Player sender)
    {
        return _innerPlayers.Contains(sender);
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_innerPlayers);
        }
        else
        {
            _innerPlayers = (List<Player>)stream.ReceiveNext();
        }
    }
}
