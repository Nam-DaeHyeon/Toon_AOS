using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Photon.Pun;

public enum MONSTER_STATE
{
    IDLE,
    TRACE,
    ATTACK,
    RETURN,
    DEAD
}

public class Monster : MonoBehaviourPun
{
    MONSTER_STATE _state = MONSTER_STATE.IDLE;

    Vector3 _spwnPos;
    Quaternion _spwnRot;
    Quaternion _hpBarRot;

    Player _targetPlayer;

    //파라미터
    float _currHP;
    [SerializeField] float _maxHP;
    [SerializeField] float _speed;
    [SerializeField] int _attackDamage;
    [SerializeField] int _defence;
    [SerializeField] int _mdefence;

    [SerializeField] float _detectDistance = 3f;
    [SerializeField] float _attackDistance = 1.5f;

    //컴포넌트
    NavMeshAgent _agent;
    [SerializeField] Animator _animator;
    Collider _collider;
    [SerializeField] Image _hpBar;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();
        
        SetInit_RespawnPos();
        //SetInit_Parameters();
        if(PhotonNetwork.IsMasterClient) photonView.RPC("CallbackRPC_SetInit_Parameters", RpcTarget.AllBuffered);
    }

    private void OnEnable()
    {
        Set_StateMachine(MONSTER_STATE.IDLE);
    }

    public void SetRespawn()
    {
        transform.position = _spwnPos;
        transform.rotation = _spwnRot;

        CallbackRPC_SetInit_Parameters();
    }

    private void SetInit_RespawnPos()
    {
        _spwnPos = transform.position;
        _spwnRot = transform.rotation;

        _hpBarRot = _hpBar.transform.rotation;
    }

    [PunRPC]
    private void CallbackRPC_SetInit_Parameters()
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
    public void Set_StateMachine(MONSTER_STATE nextState)
    {
        _state = nextState;

        switch(nextState)
        {
            case MONSTER_STATE.IDLE:
                StartCoroutine(IE_Idle());
                break;
            case MONSTER_STATE.TRACE:
                StartCoroutine(IE_Trace());
                break;
            case MONSTER_STATE.ATTACK:
                StartCoroutine(IE_Attack());
                break;
            case MONSTER_STATE.RETURN:
                StartCoroutine(IE_Return());
                break;
            case MONSTER_STATE.DEAD:
                Dead();
                break;
        }
    }

    IEnumerator IE_Idle()
    {
        _targetPlayer = null;

        while(_state.Equals(MONSTER_STATE.IDLE))
        {
            yield return null;
        }
    }

    IEnumerator IE_Trace()
    {
        while(_state.Equals(MONSTER_STATE.TRACE))
        {
            _agent.SetDestination(_targetPlayer.transform.position);
            if (_agent.remainingDistance <= _attackDistance) Set_StateMachine(MONSTER_STATE.ATTACK);
            else if (Vector3.Distance(transform.position, _spwnPos) >= _detectDistance * 3f) Set_StateMachine(MONSTER_STATE.RETURN);

            _hpBar.transform.rotation = _hpBarRot;
            yield return null;
        }
    }

    IEnumerator IE_Attack()
    {
        yield return new WaitForSeconds(1.5f);

        _targetPlayer.TakeDamage(_attackDamage);

        if (Vector3.Distance(transform.position, _spwnPos) >= _detectDistance * 3f) Set_StateMachine(MONSTER_STATE.RETURN);
        else Set_StateMachine(MONSTER_STATE.TRACE);
    }

    IEnumerator IE_Return()
    {
        _agent.SetDestination(_spwnPos);

        float timer = 0.5f;

        while(_state.Equals(MONSTER_STATE.RETURN))
        {
            if (_agent.remainingDistance < 0.5f) Set_StateMachine(MONSTER_STATE.IDLE); 

            if((timer -= Time.deltaTime) < 0)
            {
                timer = 0.5f;
                TakeHealPer10();
            }

            _hpBar.transform.rotation = _hpBarRot;
            yield return null;
        }
    }

    private void Dead()
    {
        StopAllCoroutines();
        MainManager.instance.SetRespawn_Monster(photonView.ViewID, 5);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 물리 피해를 받았습니다.
    /// </summary>
    /// <param name="damage">피해량</param>
    public void TakeDamage(float damage)
    {
        if (_currHP <= 0) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage, false);
    }

    /// <summary>
    /// 마법 피해를 받았습니다.
    /// </summary>
    /// <param name="damage">피해량</param>
    public void TakeMDamage(float damage)
    {
        if (_currHP <= 0) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage, true);
    }
    
    /// <summary>
    /// 최대 체력 10%를 회복합니다.
    /// </summary>
    public void TakeHealPer10()
    {
        if (_currHP <= 0) return;

        int per = -(int)(_maxHP * 0.1f);
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, per, false);
    }

    public void TakeDamage_IgnoreDefence(float damage)
    {
        if (_currHP <= 0) return;

        photonView.RPC("CallbackRPC_SyncHPIgnoreDefence", RpcTarget.All, damage);
        
    }

    /// <summary>
    /// 다른 플레이어로 인해 도트 데미지 디버프를 받았습니다.
    /// </summary>
    /// <param name="paramName">POISON</param>
    /// <param name="damage">도트당 데미지</param>
    /// <param name="delay">데미지 선 딜레이 (초 단위)</param>
    /// <param name="duration">지속시간</param>
    public void GetDotDam_FromOthers(string paramName, int damage, float delay, float duration, Player attacker)
    {
        StartCoroutine(IE_DotDamTimer(paramName, damage, delay, duration, attacker));
    }


    /// <summary>
    /// 플레이어 타겟을 지정합니다.
    /// </summary>
    /// <param name="newPlayer">타겟</param>
    public void Set_Target(Player newPlayer)
    {
        if (_targetPlayer != null) return;
        _targetPlayer = newPlayer;

        Set_StateMachine(MONSTER_STATE.TRACE);
    }

    private IEnumerator IE_DotDamTimer(string paramName, int damage, float delay, float duration, Player attacker)
    {
        float timer = duration;
        while (timer >= 0)
        {
            if ((timer -= delay) < 0) break;
            yield return new WaitForSeconds(delay);

            TakeDamage_IgnoreDefence(damage);

            if (_currHP <= 0)
            {
                attacker.Add_Money(3);
                yield break;
            }

            //photonView.RPC("CallbackRPC_SyncHPIgnoreDefence", RpcTarget.All, photonView.ViewID, damage);
        }

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

        if (_currHP <= 0)
        {
            Set_StateMachine(MONSTER_STATE.DEAD);
        }
        if (_currHP > _maxHP) _currHP = _maxHP;
    }

    [PunRPC]
    private void CallbackRPC_SyncHPIgnoreDefence(float damage)
    {
        _currHP -= damage;
        _hpBar.fillAmount = _currHP / _maxHP;
        if (_currHP <= 0) Set_StateMachine(MONSTER_STATE.DEAD);
    }

    public float Get_CurrentHP()
    {
        return _currHP;
    }

    public float Get_MaxHP()
    {
        return _maxHP;
    }
}
