using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTrap : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<Player>() != null)
        {
            //other.GetComponent<Player>().TakeDamage(other.GetComponent<Photon.Pun.PhotonView>().Owner.UserId, 10);
            other.GetComponent<Player>().TakeDamage(10);
        }
    }
}
