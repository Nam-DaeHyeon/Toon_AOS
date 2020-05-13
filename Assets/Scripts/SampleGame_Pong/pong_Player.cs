using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class pong_Player : MonoBehaviourPun
{
    private Rigidbody2D rigid2d;
    private SpriteRenderer renderer;

    // Start is called before the first frame update
    void Start()
    {
        rigid2d = GetComponent<Rigidbody2D>();
        renderer = GetComponent<SpriteRenderer>();

        if (photonView.IsMine)
        {
            renderer.color = Color.blue;
        }
        else
            renderer.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;
        
        /*
        ... 상호작용 관련 코드...
        var input = inputbutton
         */
    }
}
