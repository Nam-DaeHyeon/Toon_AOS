using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//적을 한 지점으로 끌어당기는 중력탄 투척
public class sk_Cat04_GravityBoom : Skill
{
    public override int[] mdamage { get; set; } = { 1, 3, 5 };
    public override float skillDistance { get; set; } = 7.5f;
    public override float duration { get; set; } = 3.5f;

    public override int Get_MaxLevel()
    {
        //return base.Get_MaxLevel();
        return 3;
    }

    public override IEnumerator IE_SkillProcess()
    {
        //return base.IE_SkillProcess();
        
        //시선 보정
        targetPos = player.GetHitPoint();
        player._animator.transform.LookAt(targetPos);

        player.Set_StateMachine(PLAYER_STATE.CAST);

        player.SetAnimTrigger("THROW");

        yield return new WaitForSeconds(0.7f);

        player.Set_StateMachine(PLAYER_STATE.IDLE);

        MainManager.instance.SetActive_SkillEffect("BoomProjectile", player._animator.transform);
        GameObject effectObj = MainManager.instance.Get_SkillEffectObj("BoomProjectile");

        //Method : Bezier
        Vector3 startPos = player.transform.position;
        Vector3 endPos = targetPos;
        Vector3 middlePos = (startPos + endPos) * 0.5f + Vector3.up * 3;
        float tt = 0f;
        float speed = 2f;
        for (float t = 1f; t >= 0f; t -= Time.deltaTime)
        {
            effectObj.transform.position = Bezier(startPos, middlePos, endPos, tt);
            tt = Mathf.Min(1, tt + Time.deltaTime * speed);

            yield return null;
        }

        MainManager.instance.Set_ActiveProjectile(projectile.gameObject, false);
        MainManager.instance.SetUnActive_SkillEffect("BoomProjectile");

        //중력장 기능
        MainManager.instance.Set_ActiveProjectile(projectile.gameObject, true);
        projectile.transform.position = targetPos;
        MainManager.instance.SetActive_SkillEffect("GravityBoom", projectile.transform);
        StartCoroutine(IE_Gravity());

        yield return new WaitForSeconds(duration);

        MainManager.instance.SetUnActive_SkillEffect("GravityBoom");
        MainManager.instance.Set_ActiveProjectile(projectile.gameObject, false);
    }

    Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float tt)
    {
        var omt = 1f - tt;
        return a * omt * omt + 2f * b * omt * tt + c * tt * tt;
    }

    IEnumerator IE_Gravity()
    {
        Vector3 dir;
        float gravityPower = 4f;
        while(projectile.gameObject.activeInHierarchy)
        {
            foreach(var target in projectile.colliderPlayers)
            {
                dir = projectile.transform.position - target.transform.position;
                dir.Normalize();

                target.TakeDamage_IgnoreDefence(mdamage[skillLevel - 1]);
                target.ForceSetPosition(target.transform.position + dir * gravityPower * Time.deltaTime);
                //target.transform.Translate(dir * gravityPower * Time.deltaTime);
            }

            if(projectile.colMonster != null)

            gravityPower -= Time.deltaTime;

            yield return new WaitForSeconds(0.05f);
        }
    }

    public override string Get_FullDescription()
    {
        //return base.Get_FullDescription();
        return "특정 좌표에 주변 대상을 끌어당기는 폭탄을 던집니다. 해당 지역에 " + duration + "초 동안 주기적으로 " + GetDesc_Damage() + "의 " + GetDesc_DamageType() + "를 입히는 중력장을 생성합니다.";
    }
}
