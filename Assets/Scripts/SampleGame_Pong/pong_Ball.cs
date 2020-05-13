using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class pong_Ball : MonoBehaviourPun
{
    /// <summary> 룸 마스터에서만 동작하기 위한 설정 변수 /// </summary>
    public bool IsMasterClientLocal => PhotonNetwork.IsMasterClient || photonView.IsMine;

    private Vector2 direction = Vector2.right;
    private readonly float speed = 10f;
    private readonly float randomReflectionIntensity = 0.1f;

    private void FixedUpdate()
    {
        if(!IsMasterClientLocal || PhotonNetwork.PlayerList.Length < 2)
        {
            return;
        }

        var distance = speed * Time.deltaTime;
        var hit = Physics2D.Raycast(transform.position, direction, distance);

        if(hit.collider != null)
        {
            var goalPost = hit.collider.GetComponent<pong_Goal>();

            if(goalPost != null)
            {
                if (goalPost.playerIndex == 1) PongManager.instance.Add_Score(playerIndex: 2, 1);
                if (goalPost.playerIndex == 2) PongManager.instance.Add_Score(playerIndex: 1, 1);
            }

            direction = Vector2.Reflect(direction, hit.normal);
            direction += Random.insideUnitCircle * randomReflectionIntensity;
        }

        transform.position = (Vector2)transform.position + direction * distance;
    }
}
