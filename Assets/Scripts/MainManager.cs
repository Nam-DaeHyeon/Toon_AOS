using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public partial class MainManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public Player[] allPlayers { get; set; }
    GameObject playerObj;
    GameObject childChar;
    public static MainManager s_instance;
    public static MainManager instance
    {
        get
        {
            if (!s_instance)
            {
                s_instance = FindObjectOfType(typeof(MainManager)) as MainManager;
                if (!s_instance)
                {
                    Debug.LogError("MainManager s_instance null");
                    return null;
                }
            }

            return s_instance;
        }
    }

    [SerializeField] GameObject cursorObjSample;
    [SerializeField] GameObject lineObjSample;

    [Header("Prefabs")]
    [SerializeField] GameObject _playerPrefab;

    private void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;

            DontDestroyOnLoad(this);

        }
        else if (this != s_instance)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetSpawn_Player();
        //if(photonView.IsMine) Add_EffectResource(GameManager.USER_CHARACTER);

    }

    private void SetSpawn_Player()
    {
        float randX = Random.Range(-20, 20);
        float randZ = Random.Range(-20, 20);

        int localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        playerObj = PhotonNetwork.Instantiate(_playerPrefab.name,
                                                         new Vector3(randX, 0, randZ),
                                                         Quaternion.Euler(0, 0, 0));

        playerObj.GetComponent<Player>()._cursorObj = cursorObjSample;
        playerObj.GetComponent<Player>().Set_InitLineRendererObj(lineObjSample);
        
        photonView.RPC("CallbackRPC_CreateCharacter", RpcTarget.AllBuffered, playerObj.GetComponent<PhotonView>().ViewID, GameManager.USER_CHARACTER);
        
        //photonView.RPC("CallbackRPC_SetParentCharacter", RpcTarget.AllBuffered);

        //자신의 캐릭터 이펙트 풀링
        Add_EffectResource(GameManager.USER_CHARACTER);
    }

    /// <summary>
    /// RPC 동기화 : 캐릭터 오브젝트 생성
    /// </summary>
    [PunRPC]
    private void CallbackRPC_CreateCharacter(int viewId, string Character)
    {
        //중복 생성 차단
        //if (!playerObj.GetComponent<Player>().GetNullCheck_Animator()) return;
        //if (!photonView.IsMine) return;

        childChar = null;
        //switch (GameManager.USER_CHARACTER)
        switch (Character)
        {
            default:
            case "BEAR":
                childChar = Instantiate(Resources.Load<GameObject>("Toon_Bear_A"));
                //sltChar = PhotonNetwork.Instantiate(Resources.Load<GameObject>("Toon_Bear_A").name, playerObj.transform.position, playerObj.transform.rotation);
                //childChar = PhotonNetwork.Instantiate("Toon_Bear_A", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "RABBIT":
                childChar = Instantiate(Resources.Load<GameObject>("Toon_Rabbit_A"));
                //childChar = PhotonNetwork.Instantiate("Toon_Rabbit_A", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "CAT":
                childChar = Instantiate(Resources.Load<GameObject>("Toon_Cat_D"));
                //childChar = PhotonNetwork.Instantiate("Toon_Cat_D", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "CHIPMUNK":
                childChar = Instantiate(Resources.Load<GameObject>("Toon_Chipmunk"));
                //childChar = PhotonNetwork.Instantiate("Toon_Chipmunk", playerObj.transform.position, playerObj.transform.rotation);
                break;
        }

        //SetParent(playerObj.GetComponent<Player>(), childChar.GetComponent<Animator>());
        SetParent(viewId, childChar.GetComponent<Animator>());
    }

    /// <summary>
    /// RPC 동기화 - 캐릭터 부모 플레이어(종속성) 설정 
    /// </summary>
    [PunRPC]
    private void CallbackRPC_SetParentCharacter()
    {
        allPlayers = FindObjectsOfType<Player>();
        Animator[] allAnims = FindObjectsOfType<Animator>();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            int tempid = allPlayers[i].GetComponent<PhotonView>().ViewID;
            tempid /= 10;

            for (int j = 0; j < allAnims.Length; j++)
            {
                if (allAnims[j].GetComponent<PhotonView>() == null) continue;
                int tempid2 = allAnims[j].GetComponent<PhotonView>().ViewID;
                tempid2 /= 10;
                if (tempid == tempid2)
                {
                    //SetParent(allPlayers[i], allAnims[j]);
                    break;
                }
            }
        }
    }

    //private void SetParent(Player parentPlayer, Animator childAnim)
    private void SetParent(int viewId, Animator childAnim)
    {
        Transform parentPlayerObj = PhotonView.Find(viewId).transform;

        //childAnim.transform.SetParent(parentPlayer.transform);
        childAnim.transform.SetParent(parentPlayerObj);
        childAnim.transform.localPosition = Vector3.zero;
        childAnim.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        
        //parentPlayer.SetAnimatorComponent(childAnim);
        parentPlayerObj.GetComponent<Player>().SetAnimatorComponent(childAnim);
    }
    
    public void Set_ActiveProjectile(GameObject target, bool value)
    {
        if(value)
        {
            Vector3 skillPos = target.GetComponent<PlayerProjectile>().Get_PlayerTr().position;
            target.transform.position = new Vector3(skillPos.x, target.transform.position.y, skillPos.z) + Vector3.up;
            photonView.RPC("CallbackRPC_ActiveObject", RpcTarget.All, target.GetComponent<PhotonView>().ViewID, value);
            target.GetComponent<PlayerProjectile>().SetPlay_Missile();
        }
        else
            photonView.RPC("CallbackRPC_ActiveObject", RpcTarget.All, target.GetComponent<PhotonView>().ViewID, value);

    }

    [PunRPC]
    private void CallbackRPC_ActiveObject(int viewId, bool value)
    {
        var tempView = PhotonView.Find(viewId);
        if (tempView == null) return;
        GameObject tempObj = tempView.gameObject;
        tempObj.SetActive(value);
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
}
