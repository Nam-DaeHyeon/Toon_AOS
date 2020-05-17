using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public enum ATK_TARGET
{
    ENEMY,
    ENEMY_NONTG,
    ALIAS,
    ALIAS_NONTG
}

public partial class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    /// <summary> 게임에 온전히 로드된 경우 활성화 : 이외 공격 무효 </summary>
    bool m_ready = false;

    /// <summary> 게임에 온전히 로드된 경우 활성화 : 이외 공격 무효 </summary>
    Vector3 _targetPos;

    #region 플레이어 능력치 파라미터
    float _currHP = 0;
    float _maxHP = 100;
    float _attackDamage = 20f;
    float _attackDistance = 2.5f;

    float _speed = 5f;
    #endregion

    #region 플레이어 행동 트리거 및 변수
    bool isMove = false;    //움직이고 있는 상태
    float distance = 0f;
    Vector3 dir;

    bool atkToggle = false;    //공격 대상 지정 상태
    Player atkTargetPlayer = null;  //공격 대상
    #endregion

    #region 컴포넌트
    [SerializeField] Camera _mainCamera;
    [SerializeField] TextMeshPro _textNickName;
    [SerializeField] Image _imgHPBar;
    Animator _animator;
    #endregion

    private void Start()
    {
        Set_InitParameter();

        if (photonView.IsMine)
        {
            StartCoroutine(IE_PlayerController());
            StartCoroutine(IE_PlayerInputKeyManager());
        }
        else
        {
            _mainCamera.gameObject.SetActive(false);
        }

        m_ready = true;

        //UI 설정
        //photonView.RPC("CallbackRPC_SyncNickname", RpcTarget.All, photonView.Owner.NickName);
        //photonView.RPC("CallbackRPC_SyncHPBar", RpcTarget.All);
        _textNickName.text = photonView.Owner.NickName;
        _imgHPBar.fillAmount = _currHP / _maxHP;
    }

    private void Set_InitParameter()
    {
        //캐릭터에 따른 능력치 설정

        //체력 초기화
        _currHP = _maxHP;

        //트리거 & 변수 초기화
        isMove = false;
        distance = 0f;
        dir = Vector3.zero;
        atkToggle = false;
        atkTargetPlayer = null;
    }

    IEnumerator IE_PlayerController()
    {
        while (_currHP > 0)
        {
            if (atkTargetPlayer == null)
            {
                #region 이동관련 : 우클릭한 지면 좌표로 이동
                if (Input.GetMouseButtonDown(1))
                {
                    //지면 확인
                    Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.transform.tag.Equals("Ground"))
                        {
                            //애니메이션
                            if (!isMove)
                            {
                                //_animator.SetTrigger("MOVE");
                                photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, "MOVE");
                            }
                            _targetPos = hit.point;
                            isMove = true;

                            //방향벡터 갱신
                            dir = _targetPos - transform.position;
                            dir.Normalize();

                            //회전
                            _animator.transform.LookAt(new Vector3(_targetPos.x, transform.position.y, _targetPos.z));

                        }
                    }
                }
                #endregion

                #region 이동관련 : 입력 좌표에 도달했는가
                if (isMove)
                {
                    distance = Vector3.Distance(transform.position, _targetPos);
                    if (distance < 0.1f)
                    {
                        isMove = false;
                        //_animator.SetTrigger("IDLE");
                        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, "IDLE");
                    }
                    else
                    {
                        transform.Translate(dir * _speed * Time.deltaTime, Space.World);
                        //transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime);
                    }
                }
                #endregion

                #region 타겟팅관련 : 다른 플레이어를 타겟팅
                if (atkToggle)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, LayerMask.NameToLayer("Player")))
                        {
                            //애니메이션
                            if (!isMove)
                            {
                                //_animator.SetTrigger("MOVE");
                                photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, "MOVE");
                            }
                            isMove = true;

                            //방향벡터 갱신
                            dir = _targetPos - transform.position;
                            dir.Normalize();

                            //회전
                            _animator.transform.LookAt(new Vector3(_targetPos.x, transform.position.y, _targetPos.z));


                        }
                    }
                }
                #endregion
            }
            else
            {
                distance = Vector3.Distance(transform.position, _targetPos);
                if (distance < _attackDistance)
                {
                    isMove = false;
                    _animator.SetInteger("ATTACK", 0);
                }
                else
                {
                    transform.Translate(dir * _speed * Time.deltaTime, Space.World);
                    //transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime);
                }
            }

            yield return null;
        }
    }

    IEnumerator IE_PlayerInputKeyManager()
    {
        while (_currHP > 0)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                atkToggle = !atkToggle;
            }

            yield return null;
        }
    }

    /// <summary>
    /// 애니메이터 컴포넌트 등록
    /// </summary>
    public void SetAnimatorComponent(Animator animator)
    {
        if (animator == null) _animator = GetComponentInChildren<Animator>();
        else _animator = animator;

        //애니메이션 동기화를 위해 옵저버 등록
        if(photonView.IsMine) photonView.ObservedComponents.Add(animator.GetComponent<PhotonAnimatorView>());
    }

    /// <summary>
    /// 애니메이터 컴포넌트 널 체크
    /// </summary>
    public bool GetNullCheck_Animator()
    {
        return _animator == null;
    }

    [PunRPC]
    private void CallbackRPC_AnimatorTrigger(string animTriggerName)
    {
        _animator.SetTrigger(animTriggerName);
    }

    /// <summary>
    /// 플레이어가 피격 당했습니다.
    /// </summary>
    /// <param name="id">피격당한 플레이어 포톤 유저 ID</param>
    /// <param name="damage">피격 데미지</param>
    //public void TakeDamage(string id, int damage)
    public void TakeDamage(int damage)
    {
        //if (!photonView.Owner.UserId.Equals(id)) return;
        //if (this.GetInstanceID() != id) return;

        if (_currHP < 0) return;

        _currHP -= damage;
        _imgHPBar.fillAmount = _currHP / _maxHP;
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(GameManager.USER_NICKNAME);
            stream.SendNext(this._imgHPBar.fillAmount);

        }
        else
        {
            _textNickName.text = (string)stream.ReceiveNext();
            this._imgHPBar.fillAmount = (float)stream.ReceiveNext();
        }
    }

    private void OnMouseEnter()
    {
        if (!photonView.IsMine)
        {
            _animator.GetComponent<Outline>().OutlineColor = Color.red;
        }
        else _animator.GetComponent<Outline>().OutlineColor = Color.white;
        _animator.GetComponent<Outline>().enabled = true;
    }

    private void OnMouseExit()
    {
        _animator.GetComponent<Outline>().enabled = false;

    }
}
