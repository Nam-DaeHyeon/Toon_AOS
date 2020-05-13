using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PongManager : MonoBehaviour
{
    public static PongManager instance;
    private void Awake()
    {
        instance = this;
    }

    public void Add_Score(int playerIndex, int score)
    {

    }
}
