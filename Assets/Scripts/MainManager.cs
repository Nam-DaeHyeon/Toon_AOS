using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MainManager : MonoBehaviourPunCallbacks
{
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
    }

    private void SetSpawn_Player()
    {
        float randX = Random.Range(-20, 20);
        float randZ = Random.Range(-20, 20);

        int localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        GameObject playerObj = PhotonNetwork.Instantiate(_playerPrefab.name,
                                                         new Vector3(randX, 0, randZ),
                                                         Quaternion.Euler(0, 0, 0));
        
        GameObject sltChar = null;
        switch (GameManager.USER_CHARACTER)
        {
            default:
            case "BEAR":
                //sltChar = Instantiate(Resources.Load<GameObject>("Toon_Bear_A"));
                //sltChar = PhotonNetwork.Instantiate(Resources.Load<GameObject>("Toon_Bear_A").name, playerObj.transform.position, playerObj.transform.rotation);
                sltChar = PhotonNetwork.Instantiate("Toon_Bear_A", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "RABBIT":
                //sltChar = Instantiate(Resources.Load<GameObject>("Toon_Rabbit_A"));
                sltChar = PhotonNetwork.Instantiate("Toon_Rabbit_A", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "CAT":
                //sltChar = Instantiate(Resources.Load<GameObject>("Toon_Cat_D"));
                sltChar = PhotonNetwork.Instantiate("Toon_Cat_D", playerObj.transform.position, playerObj.transform.rotation);
                break;
            case "CHIPMUNK":
                //sltChar = Instantiate(Resources.Load<GameObject>("Toon_Chipmunk"));
                sltChar = PhotonNetwork.Instantiate("Toon_Chipmunk", playerObj.transform.position, playerObj.transform.rotation);
                break;
        }

        sltChar.transform.parent = playerObj.transform;
        sltChar.transform.localPosition = Vector3.zero;
        sltChar.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        playerObj.GetComponent<Player>().SetAnimatorComponent();

        //PhotonNetwork.Instantiate();
    }
}
