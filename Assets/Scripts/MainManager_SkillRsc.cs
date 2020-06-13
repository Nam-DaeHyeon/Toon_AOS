using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public partial class MainManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Skill_ResourcesPool")]
    Dictionary<string, GameObject> skillPool = new Dictionary<string, GameObject>();

    [Header("Shared Resources Pool")]
    Dictionary<string, GameObject> effectPool = new Dictionary<string, GameObject>();

    /// <summary>
    /// 공유 자원 이펙트 리소스를 추가합니다. (폭발, 스턴 등등)
    /// </summary>
    private void Add_SharedEffectResources()
    {
        string[] effects = new string[] { "TrapHit", "StunEffect" };

        for(int i = 0; i < effects.Length; i++)
        {
            GameObject tempObj = Instantiate(Resources.Load<GameObject>("SkillEffect/" + effects[i]));
            tempObj.SetActive(false);
            effectPool.Add(effects[i], tempObj);
        }
    }

    /// <summary>
    /// 직업 스킬 이펙트 리소스를 추가합니다.
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
                skills = new string[] { "PoisonArrowProjectile", "PoisonArrowHit", "TrapHit", "BoomProjectile", "GravityBoom" };
                break;
            case "CHIPMUNK":
                //skills = new string[] { "Stab", "Hiding", "GravityBoom" };
                break;
        }
        
        //[ISSUE 2] 스킬 이펙트 리소스 공유 문제.. 다른 클라이언트의 리소스를 읽어오질 못한다.
        /*
        for (int i = 0; i < skills.Length; i++)
        {
            //[ISSUE 1] 이펙트 비활성화 문제.. 단일로 할 경우 적용되지 않는다.
            //tempObj.SetActive(false); //나중에 일괄로 비활성화 처리한다.
            //photonView.RPC("CallbackRPC_UnActiveParticle", RpcTarget.AllBuffered, tempObj);   //parameter hasn't GameObject type.

            //2-1. 자기 이펙트만 풀링
            GameObject tempObj = PhotonNetwork.Instantiate("SkillEffect/" + skills[i], Vector3.zero, Resources.Load<GameObject>("SkillEffect/" + skills[i]).transform.rotation);
            if (tempObj == null) continue;
            tempObj.name = "SkillEffect " + tempObj.name;
            skillPool.Add(PhotonNetwork.LocalPlayer.UserId + skills[i], tempObj);
        }

        photonView.RPC("CallbackRPC_Init_UnActiveParticleMyEffect", RpcTarget.AllBuffered);
        */
        //2-2. 다른 클라이언트에도 풀링
        photonView.RPC("CallbackRPC_SharingSkillResources", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.UserId + PhotonNetwork.LocalPlayer.ActorNumber, skills);
        
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

    [PunRPC]
    private void CallbackRPC_SharingSkillResources(string id, string[] keyName)
    {
        string temp = null;
        for (int i = 0; i < keyName.Length; i++)
        {
            //GameObject tempObj = PhotonNetwork.Instantiate("SkillEffect/" + keyName[i], Vector3.zero, Resources.Load<GameObject>("SkillEffect/" + keyName[i]).transform.rotation);
            GameObject tempObj = Instantiate(Resources.Load<GameObject>("SkillEffect/" + keyName[i]));
            if (tempObj == null) return;
            tempObj.name = "SkillEffect " + tempObj.name;
            tempObj.SetActive(false);

            temp = id + keyName[i];
            if (skillPool.ContainsKey(temp))
            {
                Debug.Log("Already Carried Resources.. ID : " + temp + " ACTOR : " + PhotonNetwork.LocalPlayer.ActorNumber + " VALUE : " + skillPool[temp]);
                continue;
            }
            skillPool.Add(temp, tempObj);
        }
    }

    /// <summary>
    /// 특정 직업 스킬 이펙트를 활성화합니다.
    /// </summary>
    /// <param name="skillKeyName">활성화하고자 하는 스킬 이펙트 키</param>
    /// <param name="skillPos">활성화하고자 하는 위치</param>
    /// <param name="parentTr">이펙트의 부모 Default Null</param>
    public void SetActive_SkillEffect(string skillKeyName, Transform skillTr, Transform parentTr = null)
    {
        //tempObj.SetActive(true);
        //ParticleSystem particle = tempObj.GetComponent<ParticleSystem>();
        //if (particle != null) particle.Play();
        string fullKeyName = PhotonNetwork.LocalPlayer.UserId + PhotonNetwork.LocalPlayer.ActorNumber + skillKeyName;
        if (parentTr != null)
        {
            photonView.RPC("CallbackRPC_Effect_SetParentProjectile", RpcTarget.AllBuffered, skillTr.position, parentTr.GetComponent<PhotonView>().ViewID, fullKeyName);
        }
        photonView.RPC("CallbackRPC_ActiveParticle", RpcTarget.All, fullKeyName, skillTr.position, skillTr.eulerAngles);
    }

    /// <summary>
    /// 기본 이펙트를 활성화 합니다.
    /// </summary>
    /// <param name="effectName">활성화하고자 하는 이펙트 키</param>
    /// <param name="skillTr">활성화 위치</param>
    /// <param name="parentTr">이펙트 부모 Default Null</param>
    public void SetActive_SharedEffect(string effectName, Transform skillTr, Transform parentTr = null)
    {
        string fullKeyName = effectName;

        if (parentTr != null)
        {
            photonView.RPC("CallbackRPC_Effect_SetParentProjectile", RpcTarget.AllBuffered, skillTr.position, parentTr.GetComponent<PhotonView>().ViewID, fullKeyName);
        }
        photonView.RPC("CallbackRPC_ActiveParticle", RpcTarget.All, fullKeyName, skillTr.position, skillTr.eulerAngles);
    }

    /// <summary>
    /// 특정 직업 스킬 이펙트 오브젝트를 반환합니다.
    /// </summary>
    public GameObject Get_SkillEffectObj(string skillKeyName)
    {
        string fullKeyName = PhotonNetwork.LocalPlayer.UserId + PhotonNetwork.LocalPlayer.ActorNumber + skillKeyName;
        return skillPool[fullKeyName];
    }

    public void SetUnActive_SkillEffect(string skillKeyName)
    {
        string fullKeyName = PhotonNetwork.LocalPlayer.UserId + PhotonNetwork.LocalPlayer.ActorNumber + skillKeyName;
        photonView.RPC("CallbackRPC_UnActiveParticle", RpcTarget.All, fullKeyName);
    }

    /// <summary>
    /// 이펙트 오브젝트의 부모를 지정합니다.
    /// </summary>
    /// <param name="projViewId">지정하고자 하는 포톤 뷰 아이디 (프로젝타일)</param>
    /// <param name="fullKeyName">스킬 이펙트 풀의 키 이름 (풀네임)</param>
    [PunRPC]
    private void CallbackRPC_Effect_SetParentProjectile(Vector3 skillPos, int projViewId, string fullKeyName)
    {
        var view = PhotonView.Find(projViewId);
        view.transform.position = skillPos;

        GameObject effectObj = (skillPool.ContainsKey(fullKeyName)) ? skillPool[fullKeyName] : effectPool[fullKeyName];
        if (effectObj == null) return;

        effectObj.transform.parent = view.transform;
        effectObj.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// [RPC] 스킬 이펙트 활성화
    /// </summary>
    [PunRPC]
    private void CallbackRPC_ActiveParticle(string fullKeyName, Vector3 skillPos, Vector3 skillEuler)
    {
        GameObject tempObj = (skillPool.ContainsKey(fullKeyName)) ? skillPool[fullKeyName] : effectPool[fullKeyName];
        if (tempObj == null) return;

        //tempObj.transform.position = new Vector3(skillPos.x, tempObj.transform.position.y, skillPos.z);
        tempObj.transform.position = skillPos;
        //tempObj.transform.eulerAngles = Vector3.zero;
        float tempx = tempObj.transform.eulerAngles.x;

        if (tempObj.transform.eulerAngles.x + tempObj.transform.eulerAngles.y + tempObj.transform.eulerAngles.z != 0)
        {
            tempObj.transform.eulerAngles = Vector3.zero;
            tempObj.transform.eulerAngles = new Vector3(tempx, 0, skillEuler.y - tempx * 2);
        }
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
    private void CallbackRPC_UnActiveParticle(string fullKeyName)
    {
        //GameObject tempObj = skillPool[skillKeyName];
        GameObject tempObj = skillPool[fullKeyName];
        if (tempObj == null)
        {
            return;
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

    public void SkillFunc_PushTarget(Vector3 original, Transform target, float distance, float lerpSpeed, float duration)
    {
        photonView.RPC("CallbackRPC_Skill_PushTargetCoroutine", RpcTarget.All, target.GetComponent<PhotonView>().ViewID, original, distance, lerpSpeed, duration);
    }

    [PunRPC]
    private void CallbackRPC_Skill_PushTargetCoroutine(int viewId, Vector3 original,  float distance, float lerpSpeed, float duration)
    {
        Transform target = PhotonView.Find(viewId).transform;
        if (target == null) return;

        StartCoroutine(IE_SkillFunc_PushTarget(original, target, distance, lerpSpeed, duration));
    }

    private IEnumerator IE_SkillFunc_PushTarget(Vector3 original, Transform targetTr, float distance, float lerpSpeed, float duration)
    {
        Vector3 pushDir = targetTr.position - original;
        pushDir.Normalize();
        pushDir *= distance;

        float timer = duration;
        while (timer > 0)
        {
            if (targetTr == null) yield break;
            //targetTr.position = Vector3.Lerp(targetTr.position, pushDir, 5 * Time.deltaTime);
            targetTr.GetComponent<Player>().ForceSetPosition(Vector3.Lerp(targetTr.position, pushDir, 5 * Time.deltaTime));

            timer -= Time.deltaTime;
            yield return null;
        }
    }
}

