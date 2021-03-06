﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public partial class MainManager : MonoBehaviourPunCallbacks, IPunObservable
{
    GameObject playerObj;
    GameObject childChar;

    [HideInInspector] public Player owner;

    [SerializeField] GameObject _logPrefab;
    [SerializeField] int _logPoolCount = 10;
    Queue<GameObject> _logPool;
    
    [SerializeField] GameObject lineObjSample;

    [SerializeField] GameObject _optionWindow;

    [Header("Prefabs")]
    [SerializeField] GameObject _playerPrefab;

    public static MainManager instance;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        SetSpawn_Player();
        //if(photonView.IsMine) Add_EffectResource(GameManager.USER_CHARACTER);

        SetInit_LogPool();

    }

    private void SetSpawn_Player()
    {
        float randX = Random.Range(-20, 20);
        float randZ = Random.Range(-20, 20);

        int localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        playerObj = PhotonNetwork.Instantiate(_playerPrefab.name,
                                                         new Vector3(randX, 0, randZ),
                                                         Quaternion.Euler(0, 0, 0));

        owner = playerObj.GetComponent<Player>();
        owner.Set_InitLineRendererObj(lineObjSample);

        SetCreate_Character(GameManager.USER_CHARACTER);
        //photonView.RPC("CallbackRPC_CreateCharacter", RpcTarget.AllBuffered, playerObj.GetComponent<PhotonView>().ViewID, GameManager.USER_CHARACTER);
        
        photonView.RPC("CallbackRPC_SetParentCharacter", RpcTarget.AllBuffered, playerObj.GetComponent<PhotonView>().ViewID, childChar.GetComponent<PhotonView>().ViewID);

        //자신의 캐릭터 이펙트 풀링
        Add_EffectResource(GameManager.USER_CHARACTER);

        //공용 이펙트 풀링
        Add_SharedEffectResources();
    }

    /// <summary>
    ///  캐릭터 오브젝트 생성
    /// </summary>
    private void SetCreate_Character(string Character)
    {
        childChar = null;
        //switch (GameManager.USER_CHARACTER)
        switch (Character)
        {
            default:
            case "BEAR":
                //childChar = Instantiate(Resources.Load<GameObject>("Toon_Bear_A"));
                //sltChar = PhotonNetwork.Instantiate(Resources.Load<GameObject>("Toon_Bear_A").name, playerObj.transform.position, playerObj.transform.rotation);
                childChar = PhotonNetwork.Instantiate("Toon_Bear_A", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "RABBIT":
                //childChar = Instantiate(Resources.Load<GameObject>("Toon_Rabbit_A"));
                childChar = PhotonNetwork.Instantiate("Toon_Rabbit_A", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "CAT":
                //childChar = Instantiate(Resources.Load<GameObject>("Toon_Cat_D"));
                childChar = PhotonNetwork.Instantiate("Toon_Cat_D", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "CHIPMUNK":
                //childChar = Instantiate(Resources.Load<GameObject>("Toon_Chipmunk"));
                childChar = PhotonNetwork.Instantiate("Toon_Chipmunk", playerObj.transform.position, playerObj.transform.rotation);
                break;
        }
    }

    /// <summary>
    /// RPC 동기화 - 캐릭터 부모 플레이어(종속성) 설정 
    /// </summary>
    [PunRPC]
    private void CallbackRPC_SetParentCharacter(int parentId, int chilldId)
    {
        SetParent(parentId, chilldId);
    }

    //private void SetParent(Player parentPlayer, Animator childAnim)
    private void SetParent(int parentId, int childId)
    {
        Transform parentPlayerObj = PhotonView.Find(parentId).transform;
        Animator childAnim = PhotonView.Find(childId).transform.GetComponent<Animator>();

        //childAnim.transform.SetParent(parentPlayer.transform);
        childAnim.transform.SetParent(parentPlayerObj);
        childAnim.transform.localPosition = Vector3.zero;
        childAnim.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        
        //parentPlayer.SetAnimatorComponent(childAnim);
        parentPlayerObj.GetComponent<Player>().SetAnimatorComponent(childAnim);
    }
    
    /// <summary>
    /// 프로젝타일을 활성화/비활성화시킵니다.
    /// </summary>
    /// <param name="target">프로젝타일 게임오브젝트</param>
    /// <param name="value">활성화/비활성화</param>
    public void Set_ActiveProjectile(GameObject target, bool value)
    {
        if (value)
        {
            //밀리 프로젝타일의 경우
            if (target.GetComponent<PlayerProjectile>() == null)
            {
                photonView.RPC("CallbackRPC_ActiveObject", RpcTarget.All, target.GetComponent<PhotonView>().ViewID, value);
                return;
            }

            Vector3 skillPos = target.GetComponent<PlayerProjectile>().Get_PlayerTr().position;
            target.transform.position = new Vector3(skillPos.x, target.transform.position.y, skillPos.z) + Vector3.up;
            photonView.RPC("CallbackRPC_ActiveObject", RpcTarget.All, target.GetComponent<PhotonView>().ViewID, value);
            target.GetComponent<PlayerProjectile>().SetPlay_Missile();
        }
        else
        {
            photonView.RPC("CallbackRPC_ActiveObject", RpcTarget.All, target.GetComponent<PhotonView>().ViewID, value);
        }
    }

    [PunRPC]
    private void CallbackRPC_ActiveObject(int viewId, bool value)
    {
        var tempView = PhotonView.Find(viewId);
        if (tempView == null) return;
        GameObject tempObj = tempView.gameObject;
        tempObj.SetActive(value);
    }

    /// <summary>
    /// 포톤뷰 오브젝트를 삭제합니다.
    /// </summary>
    /// <param name="viewId">삭제하고자 하는 포톤뷰 아이디</param>
    public void Destroy_PhotonViewObject(int viewId)
    {
        photonView.RPC("CallbackRPC_DestroyObject", RpcTarget.AllBuffered, viewId);
    }

    [PunRPC]
    private void CallbackRPC_DestroyObject(int viewId)
    {
        var tempView = PhotonView.Find(viewId);
        if (tempView == null) return;
        Destroy(tempView.gameObject);
    }

    /// <summary>
    /// 환상 오브젝트의 애니메이터 트리거를 전달합니다.
    /// </summary>
    /// <param name="viewId">환상 오브젝트의 포톤뷰 아이디</param>
    /// <param name="triggerName">동작시킬 트리거 이름</param>
    public void SetAnimatorTrigger_Decoy(int viewId, string triggerName)
    {
        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, viewId, triggerName);
    }

    [PunRPC]
    private void CallbackRPC_AnimatorTrigger(int viewId, string triggerName)
    {
        var tempView = PhotonView.Find(viewId);
        if (tempView == null) return;
        tempView.GetComponent<Animator>().SetTrigger(triggerName);
    }

    /// <summary>
    /// 다른 클라이언트 상에서 자신의 HUD를 특정 오브젝트에 부모종속시킵니다. 
    /// </summary>
    /// <param name="viewId">특정 오브젝트 포톤뷰 아이디</param>
    /// <param name="PlayerViewId">자신의 플레이어 포톤뷰 아이디</param>
    public void SetParent_WorldCanvas(int viewId, int PlayerViewId)
    {
        photonView.RPC("CallbackRPC_ParentWorldCanvas", RpcTarget.All, viewId, PlayerViewId);
    }

    [PunRPC]
    private void CallbackRPC_ParentWorldCanvas(int viewId, int PlayerViewId)
    {
        var targetView = PhotonView.Find(viewId);
        var playerView = PhotonView.Find(PlayerViewId);

        if (!playerView.IsMine)
        {
            Transform hudTr = playerView.GetComponent<Player>().UI_WorldCvs.transform;
            Vector3 localPos = hudTr.localPosition;
            hudTr.parent = targetView.transform;
            hudTr.localPosition = localPos;

            hudTr.gameObject.SetActive(true);
        }
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(skillPool);

        }
        else
        {
            skillPool = (Dictionary<string, GameObject>)stream.ReceiveNext();
        }
    }

    /// <summary>
    /// 몬스터를 재소환합니다.
    /// </summary>
    /// <param name="viewId">소환하고자 하는 몬스터의 포톤뷰 아이디</param>
    /// <param name="delay">재소환 지연시간</param>
    public void SetRespawn_Monster(int viewId, float delay)
    {
        photonView.RPC("CallbackRPC_Respawn_Monster", RpcTarget.AllBuffered, viewId, delay);
    }

    [PunRPC]
    private void CallbackRPC_Respawn_Monster(int viewId, float delay)
    {
        Monster monster = PhotonView.Find(viewId).GetComponent<Monster>();
        StartCoroutine(IE_Respawm_Monster(monster, delay));
    }

    IEnumerator IE_Respawm_Monster(Monster obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        obj.SetRespawn();
        obj.gameObject.SetActive(true);
    }

    /// <summary>
    /// 피해량을 표시하는 텍스트 로그 풀링을 생성합니다.
    /// </summary>
    private void SetInit_LogPool()
    {
        if (_logPrefab.activeInHierarchy) _logPrefab.SetActive(false);
        if (_logPool == null) _logPool = new Queue<GameObject>();

        for (int i = 1; i < _logPoolCount; i++)
        {
            GameObject obj = Instantiate(_logPrefab);
            _logPool.Enqueue(obj);
        }
    }

    /// <summary>
    /// 피해량을 텍스트 로그로 표시합니다.
    /// </summary>
    /// <param name="pos">로그를 호출한 월드 좌표</param>
    /// <param name="logValue">로그에 입력할 피해량</param>
    public void SetVisible_HitLog(Vector3 pos, int logValue)
    {
        GameObject tempObj = _logPool.Dequeue();
        if (tempObj == null)
        {
            tempObj = Instantiate(_logPrefab);
        }
        TMP_Text tmpText = tempObj.transform.GetChild(0).GetComponent<TMP_Text>();
        tmpText.text = logValue.ToString();

        tempObj.transform.position = pos;
        tempObj.SetActive(true);
    }

    public void Enqueue_LogPool(GameObject item)
    {
        if (_logPool == null) _logPool = new Queue<GameObject>();
        _logPool.Enqueue(item);
    }

    /// <summary>
    /// Option UI : 배경음 볼륨을 조절합니다. 
    /// </summary>
    public void UI_Slider_ModifyBGMVolume(Slider slider)
    {
        SoundManager.instance.SetVolume_BGM(slider.value);
    }
    
    /// <summary>
    /// Option UI : 효과음 볼륨을 조절합니다. 
    /// </summary>
    public void UI_Slider_ModifySFXVolume(Slider slider)
    {
        SoundManager.instance.SetVolume_SE(slider.value);
    }

    /// <summary>
    /// 옵션창을 열거나 닫습니다.
    /// </summary>
    public void OpenClose_OptionWindow()
    {
        if (_optionWindow.activeInHierarchy) _optionWindow.SetActive(false);
        else _optionWindow.SetActive(true);
    }

    public void UI_ButtonClick_OptionBackToLobby()
    {
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void UI_ButtonClick_OptionGameQuit()
    {
        PhotonNetwork.LeaveRoom();
        Application.Quit();
    }
}
