using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public partial class MainManager : MonoBehaviourPunCallbacks
{
    [Header("Skill_ResourcesPool")]
    Dictionary<string, GameObject> skillPool = new Dictionary<string, GameObject>();

    /// <summary>
    /// 스킬 및 이펙트 리소스를 추가합니다.
    /// </summary>
    /// <param name="className"></param>
    private void Add_EffectResource(string className)
    {
        string[] skills = null;
        switch (className)
        {
            //case "BEAR":
            default:
                skills = new string[] { "Bash", "DashHit", "Guard", "Frenzy" };
                break;
            case "RABBIT":
                skills = new string[] { "IceArrowProjectile", "IceArrowHit", "Heal", "Barrier", "Blizard" };
                break;
            case "CAT":
                skills = new string[] { "PoisonArrowProjectile", "TrapHit", "SnipeProjectile", "SnipeHit" };
                break;
            case "CHIPMUNK":
                skills = new string[] { "Stab", "Hiding", "GravityBoom" };
                break;
        }

        for (int i = 0; i < skills.Length; i++)
        {
            GameObject tempObj = PhotonNetwork.Instantiate("SkillEffect/" + skills[i], Vector3.zero, Resources.Load<GameObject>("SkillEffect/" + skills[i]).transform.rotation);
            if (tempObj == null) continue;
            tempObj.name = "SkillEffect " + tempObj.name;
            //tempObj.SetActive(false); //나중에 일괄로 비활성화 처리한다.
            //photonView.RPC("CallbackRPC_UnActiveParticle", RpcTarget.AllBuffered, tempObj);   //parameter hasn't GameObject type.
            //자기 이펙트만 풀링
            skillPool.Add(skills[i], tempObj);
        }
        photonView.RPC("CallbackRPC_Init_UnActiveParticleMyEffect", RpcTarget.AllBuffered);
        
        //이전에 입장한 플레이어의 이펙트 비활성화 (RPC 동기화가 Start보다 느리게 됨.. 그래서 탐색을 못함)
        /*
        ParticleSystem[] temps = FindObjectsOfType<ParticleSystem>();
        for (int i = 0; i < temps.Length; i++)
        {
            string parentname = (temps[i].transform.parent == null)? "NULL" : temps[i].transform.parent.name;
            Debug.Log(photonView.ViewID + "'s particle system : " + temps[i].name + " ==> " + parentname);
            if (temps[i].transform.parent != null) continue;
            temps[i].gameObject.SetActive(false);
        }
        */
    }

    private void Update()
    {
        //Debug
        if (Input.GetKeyDown(KeyCode.Alpha1))
            foreach (var item in skillPool) Debug.Log(item.Key + " = " + item.Value.ToString());
    }

    /// <summary>
    /// 특정 스킬 이펙트를 활성화합니다.
    /// </summary>
    /// <param name="skillKeyName">활성화하고자 하는 스킬 이펙트 키</param>
    /// <param name="skillPos">활성화하고자 하는 위치</param>
    /// <param name="isParent">이펙트의 부모 종속 여부</param>
    public void SetActive_SkillEffect(string skillKeyName, Transform skillTr, bool isParent = false)
    {
        //tempObj.SetActive(true);
        //ParticleSystem particle = tempObj.GetComponent<ParticleSystem>();
        //if (particle != null) particle.Play();

        if(isParent)
        {
            GameObject tempObj = skillPool[skillKeyName];
            if (tempObj != null)
            {
                tempObj.transform.parent = skillTr;
            }
        }

        photonView.RPC("CallbackRPC_ActiveParticle", RpcTarget.All, skillKeyName, skillTr.position, skillTr.eulerAngles);
    }

    /// <summary>
    /// 특정 스킬 이펙트 오브젝트를 반환합니다.
    /// </summary>
    public GameObject Get_SkillEffectObj(string skillKeyName)
    {
        return skillPool[skillKeyName];
    }

    public void SetUnActive_SkillEffect(string skillKeyName)
    {
        photonView.RPC("CallbackRPC_UnActiveParticle", RpcTarget.All, skillKeyName);
    }

    /// <summary>
    /// [RPC] 스킬 이펙트 활성화
    /// </summary>
    [PunRPC]
    private void CallbackRPC_ActiveParticle(string skillKeyName, Vector3 skillPos, Vector3 skillEuler)
    {
        GameObject tempObj = skillPool[skillKeyName];
        if (tempObj == null)
        {
            Debug.Log("Skill Key Name {" + skillKeyName + "} is wrong or Not Pooled.");
        }

        tempObj.transform.position = skillPos;
        //tempObj.transform.eulerAngles = Vector3.zero;
        float tempx = tempObj.transform.eulerAngles.x;
        tempObj.transform.eulerAngles = Vector3.zero;
        tempObj.transform.eulerAngles = new Vector3(tempx, 0, skillEuler.y - tempx * 2);

        //Transform은 Parameter로 사용할 수 없다.
        //if (isParent) tempObj.transform.parent = skillTr;

        if (!tempObj.activeInHierarchy) tempObj.SetActive(true);

        ParticleSystem particle = tempObj.GetComponent<ParticleSystem>();
        if (particle != null) particle.Play();
    }

    /// <summary>
    /// [RPC] 스킬 이펙트 비활성화
    /// </summary>
    [PunRPC]
    private void CallbackRPC_UnActiveParticle(string skillKeyName)
    {
        //GameObject tempObj = skillPool[skillKeyName];
        GameObject tempObj = skillPool[skillKeyName];
        if (tempObj == null)
        {
            Debug.Log("Skill Key Name {" + skillKeyName + "} is wrong or Not Pooled.");
        }

        tempObj.transform.parent = null;

        tempObj.SetActive(false);
    }

    [PunRPC]
    private void CallbackRPC_Init_UnActiveParticleMyEffect()
    {
        ParticleSystem[] temps = FindObjectsOfType<ParticleSystem>();
        for (int i = 0; i < temps.Length; i++)
        {
            string parentname = (temps[i].transform.parent == null) ? "NULL" : temps[i].transform.parent.name;
            //Debug.Log(photonView.ViewID + "'s particle system : " + temps[i].name + " ==> " + parentname);
            if (temps[i].transform.parent != null) continue;
            temps[i].gameObject.SetActive(false);
        }
    }
}

