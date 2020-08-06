using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Photon.Pun;

public class Monster : MonoBehaviourPun
{
    enum MONSTER_STATE
    {
        IDLE,
        TRACE,
        ATTACK,
        RETURN,
        DEAD
    }

    MONSTER_STATE _state = MONSTER_STATE.IDLE;

    Vector3 _spwnPos;
    Quaternion _spwnRot;

    Player _targetPlayer;

    //파라미터
    float _currHP;
    [SerializeField] float _maxHP;
    [SerializeField] float _speed;
    [SerializeField] int _attackDamage;
    [SerializeField] int _defence;
    [SerializeField] int _mdefence;

    //컴포넌트
    NavMeshAgent _agent;
    [SerializeField] Animator _animator;
    [SerializeField] Image _hpBar;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        SetInit_RespawnPos();
        //SetInit_Parameters();
        if(PhotonNetwork.IsMasterClient) photonView.RPC("RPC_SetInit_Parameters", RpcTarget.AllBuffered);
    }

    // Start is called before the first frame update
    void Start()
    {
        Set_StateMachine(MONSTER_STATE.IDLE);
    }

    private void SetInit_RespawnPos()
    {
        _spwnPos = transform.position;
        _spwnRot = transform.rotation;
    }

    [PunRPC]
    private void RPC_SetInit_Parameters()
    {
        _currHP = _maxHP;
        _hpBar.fillAmount = 1;
        //photonView.RPC("CallbackRPC_SyncHP", RpcTarget.AllBuffered, 0, false);

        _agent.speed = _speed;
    }

    private void SetInit_RespawnPos(Vector3 pos, Quaternion rot)
    {
        _spwnPos = pos;
        _spwnRot = rot;
    }

    /// <summary>
    /// 스테이트 머신
    /// </summary>
    /// <param name="nextState">몬스터의 다음 상태</param>
    void Set_StateMachine(MONSTER_STATE nextState)
    {
        switch(nextState)
        {
            case MONSTER_STATE.IDLE:

                break;
            case MONSTER_STATE.TRACE:

                break;
            case MONSTER_STATE.ATTACK:

                break;
            case MONSTER_STATE.RETURN:

                break;
            case MONSTER_STATE.DEAD:

                break;
        }
    }

    IEnumerator IE_Idle()
    {
        while(_state.Equals(MONSTER_STATE.IDLE))
        {
            yield return null;
        }
    }


    public void TakeDamage(float damage)
    {
        if (_currHP < 0) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage, false);
    }

    public void TakeMDamage(float damage)
    {
        if (_currHP < 0) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage, true);
    }
    
    public void TakeHealPer10()
    {
        if (_currHP < 0) return;

        int per = -(int)(_maxHP * 0.1f);
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, per, false);
    }
    
    /// <summary>
    /// [RPC] 체력 동기화
    /// </summary>
    /// <param name="endDamage"></param>
    [PunRPC]
    private void CallbackRPC_SyncHP(int endDamage, bool isMagic = false)
    {
        int tempDamage = endDamage;

        //일반적인 타격한 경우 (피해량)
        if (endDamage > 0)
        {
            int endDefence = _defence;
            if (isMagic) endDefence = _mdefence;
            tempDamage -= endDefence;
            if (tempDamage <= 0) tempDamage = 0;    //유효 데미지 없음
        }
        //플레이어를 회복한 경우 (회복량)
        else
        {

        }

        _currHP -= tempDamage;
        _hpBar.fillAmount = _currHP / _maxHP;

        if (_currHP < 0)
        {
            Set_StateMachine(MONSTER_STATE.DEAD);
        }
        if (_currHP > _maxHP) _currHP = _maxHP;
    }
}
