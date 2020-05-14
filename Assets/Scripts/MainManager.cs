using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MainManager : MonoBehaviourPunCallbacks
{
    public static MainManager s_instance
    {
        get
        {
            if (instance == null) instance = FindObjectOfType<MainManager>();
            return instance;
        }
    }
    public static MainManager instance;

    [Header("Prefabs")]
    [SerializeField] GameObject _playerPrefab;

    private void Start()
    {
        SetSpawn_Player();
    }

    private void SetSpawn_Player()
    {
        float randX = Random.Range(-20, 20);
        float randZ = Random.Range(-20, 20);

        int localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        GameObject sltChar = null;
        switch (GameManager.USER_CHARACTER)
        {
            default:
            case "BEAR":
                sltChar = Instantiate(Resources.Load<GameObject>("Toon_Bear_A"));
                break;
            case "RABBIT":
                sltChar = Instantiate(Resources.Load<GameObject>("Toon_Rabbit_A"));
                break;
            case "CAT":
                sltChar = Instantiate(Resources.Load<GameObject>("Toon_Cat_D"));
                break;
            case "CHIPMUNK":
                sltChar = Instantiate(Resources.Load<GameObject>("Toon_Chipmunk"));
                break;
        }

        GameObject playerObj = PhotonNetwork.Instantiate(_playerPrefab.name,
                                                         new Vector3(randX, 0, randZ),
                                                         Quaternion.Euler(0, 0, 0));
        sltChar.transform.parent = playerObj.transform;
        sltChar.transform.localPosition = Vector3.zero;
        //sltChar.transform.rotation = playerObj.transform.rotation;
        sltChar.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

        playerObj.GetComponent<Player>().SetAnimatorComponent();

        //PhotonNetwork.Instantiate();
    }
}
