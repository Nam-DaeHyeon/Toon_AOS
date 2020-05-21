using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//적을 한 지점으로 끌어당기는 중력탄 투척
public class sk_Chipmunk04_GravityBoom : Skill
{
    public override int Get_MaxLevel()
    {
        //return base.Get_MaxLevel();
        return 3;
    }
}
