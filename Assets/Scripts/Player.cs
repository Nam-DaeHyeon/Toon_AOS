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

/// <summary>
/// 플레이어 컨트롤러 상태
/// </summary>
public enum PLAYER_STATE
{
    IDLE,
    ATTACK
}

public partial class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    /// <summary> 게임에 온전히 로드된 경우 활성화 : 이외 공격 무효 </summary>
    bool m_ready = false;

    /// <summary> 플레이어가 이동할 좌표 </summary>
    Vector3 _targetPos;

    #region 플레이어 능력치 파라미터
    PLAYER_STATE _myState = PLAYER_STATE.IDLE;

    float _currHP = 0;
    float _maxHP = 100;
    float _attackDamage = 20f;
    float _attackDistance = 2.5f;

    float _speed = 5f;

    bool _onHide = false;   //은신상태 | 수풀에 숨어있는 상태

    public int _skillPoint { set; get; } = 0;
    #endregion

    #region 플레이어 행동 트리거 및 변수
    bool isMove = false;    //움직이고 있는 상태
    float distance = 0f;
    Vector3 dir;

    bool atkToggle = false;    //공격 대상 지정 상태
    Player atkTargetPlayer = null;  //공격 대상
    float atkAnimTime = 1.042f;    //공격 애니메이션 재생 시간

    Shader baseShader;
    Shader alphaShader;
    #endregion

    #region 컴포넌트
    [SerializeField] Camera _mainCamera;
    [SerializeField] TextMeshPro _textNickName;
    [SerializeField] Image _imgHPBar;
    Animator _animator;
    #endregion
    
    //커서 오브젝트 샘플
    public GameObject _cursorObj;

    private void Awake()
    {
        baseShader = Shader.Find("Unlit/CellShader");
        alphaShader = Shader.Find("Unlit/CellShaderAlpha");
    }

    private void Start()
    {
        Set_InitParameter();

        if (photonView.IsMine)
        {
            //photonView.RPC("CallbackRPC_SetInitParameter", RpcTarget.All);

            StartCoroutine(IE_BaseController());
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
        
        //캐릭터별 스킬 설정
        SetInit_MySkillSet();
        GetSkillPoint();
        Hide_SkillDesc();

        //체력 초기화
        _currHP = _maxHP;

        //컨트롤러 스테이트 초기화
        _myState = PLAYER_STATE.IDLE;

        //트리거 & 변수 초기화
        isMove = false;
        distance = 0f;
        dir = Vector3.zero;
        atkToggle = false;
        atkTargetPlayer = null;

        _onHide = false;

        if (photonView.IsMine) _cursorObj.SetActive(false);
    }

    /// <summary>
    /// 플레이어 컨트롤러 스테이트 머신
    /// </summary>
    private void Set_StateMachine(PLAYER_STATE nextState)
    {
        _myState = nextState;
        switch(nextState)
        {
            default:
                StartCoroutine(IE_BaseController());
                break;
            case PLAYER_STATE.ATTACK:
                StartCoroutine(IE_AttackController());
                break;
        }
    }

    #region 플레이어 기본 컨트롤러 코루틴
    /// <summary> 플레이어 기본 컨트롤러 </summary>
    IEnumerator IE_BaseController()
    {
        while (_currHP > 0 && _myState.Equals(PLAYER_STATE.IDLE))
        {
            #region 이동관련 : 우클릭한 지면 좌표 저장
            if (Input.GetMouseButtonDown(1))
            {
                SetRayHitPos_Ground();
            }
            #endregion

            if (atkTargetPlayer == null)
            {
                #region 이동관련 : 입력 좌표에 도달했는가
                if (isMove)
                {
                    distance = Vector3.Distance(transform.position, _targetPos);
                    if (distance < 0.15f)
                    {
                        isMove = false;
                        //_animator.SetTrigger("IDLE");
                        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, "IDLE");
                    }
                    else
                    {
                        //Correction_DirectionVector();
                        transform.Translate(dir * _speed * Time.deltaTime, Space.World);
                    }
                }
                #endregion
            }
            else
            {
                distance = Vector3.Distance(transform.position, atkTargetPlayer.transform.position);
                //공격범위에 들어왔다면 공격
                if (distance < _attackDistance)
                {
                    isMove = false;
                    Set_StateMachine(PLAYER_STATE.ATTACK);
                    yield break;
                }
                //이외에 추적
                else
                {
                    //애니메이션
                    if (!isMove)
                    {
                        //_animator.SetTrigger("MOVE");
                        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, "MOVE");
                    }
                    isMove = true;

                    _animator.transform.LookAt(new Vector3(atkTargetPlayer.transform.position.x, transform.position.y, atkTargetPlayer.transform.position.z));

                    //Correction_DirectionVector();
                    transform.Translate(dir * _speed * Time.deltaTime, Space.World);
                }
            }

            yield return null;
        }
    }
    #endregion

    #region 플레이어 공격 컨트롤러 코루틴
    private IEnumerator IE_AttackController()
    {
        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, "ATTACK");

        //애니메이션 이벤트 리시버 에러로 인한 코루틴 처리
        StartCoroutine(IE_AnimEvent_Attack());

        float delay = atkAnimTime;

        while(_currHP > 0 && _myState.Equals(PLAYER_STATE.ATTACK))
        {
            if (atkTargetPlayer == null) break;
            _animator.transform.LookAt(atkTargetPlayer.transform.position);

            //공격모션 중간에 우클릭을 통해 공격캔슬, 이동한다.
            if (Input.GetMouseButtonDown(1))
            {
                SetRayHitPos_Ground();
                Set_StateMachine(PLAYER_STATE.IDLE);
                break;
            }

            delay -= Time.deltaTime;
            if (delay <= 0)
            {
                //공격 후 공격 범위에 있다면 공격을 지속하고, 아니라면 기본 컨트롤러로 상태를 변환한다.
                distance = Vector3.Distance(atkTargetPlayer.transform.position, transform.position);
                if (distance > _attackDistance)
                {
                    atkTargetPlayer = null;
                    Set_StateMachine(PLAYER_STATE.IDLE);
                }
                else
                {
                    Set_StateMachine(PLAYER_STATE.ATTACK);
                }

                yield break;
            }

            yield return null;
        }
    }
    #endregion

    /// <summary> 플레이어 키 입력을 받습니다. </summary>
    IEnumerator IE_PlayerInputKeyManager()
    {
        bool ctrlPressed = false;

        while (_currHP > 0)
        {
            //기본 공격 타겟팅
            if (Input.GetKeyDown(KeyCode.A))
            {
                atkToggle = true;
                StartCoroutine(IE_FollowTargetCursor());
            }
            else if (Input.GetKeyUp(KeyCode.A)) atkToggle = false;

            //스킬 사용/레벨업 퀵버튼
            if (!ctrlPressed)
            {
                if (Input.GetKeyDown(KeyCode.Q)) _skillSlots[0].SetUse_Skill(KeyCode.Q);
                if (Input.GetKeyDown(KeyCode.W)) _skillSlots[1].SetUse_Skill(KeyCode.W);
                if (Input.GetKeyDown(KeyCode.E)) _skillSlots[2].SetUse_Skill(KeyCode.E);
                if (Input.GetKeyDown(KeyCode.R)) _skillSlots[3].SetUse_Skill(KeyCode.R);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Q)) _skillSlots[0].UI_Button_SkillLevelUP();
                if (Input.GetKeyDown(KeyCode.W)) _skillSlots[1].UI_Button_SkillLevelUP();
                if (Input.GetKeyDown(KeyCode.E)) _skillSlots[2].UI_Button_SkillLevelUP();
                if (Input.GetKeyDown(KeyCode.R)) _skillSlots[3].UI_Button_SkillLevelUP();
            }

            //CTRL 버튼 클릭
            if (Input.GetKeyDown(KeyCode.LeftControl)) ctrlPressed = true;
            else if (Input.GetKeyUp(KeyCode.LeftControl)) ctrlPressed = false;

            yield return null;
        }
    }

    /// <summary> 타겟팅 커서를 활성화합니다. </summary>
    IEnumerator IE_FollowTargetCursor()
    {
        _cursorObj.SetActive(true);

        while(atkToggle)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Default")))
            {
                if (hit.transform.tag.Equals("Ground"))
                    _cursorObj.transform.position = hit.point;
            }

            #region 타겟팅관련 : 다른 플레이어를 타겟팅
            if (Input.GetMouseButtonDown(0))
            {
                ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Player")))
                {
                    _targetPos = hit.point;

                    //방향벡터 갱신
                    dir = _targetPos - transform.position;
                    dir.Normalize();

                    //회전
                    _animator.transform.LookAt(new Vector3(_targetPos.x, transform.position.y, _targetPos.z));

                    atkTargetPlayer = hit.transform.GetComponent<Player>();
                }

                break;
            }
            #endregion

            yield return null;
        }

        _cursorObj.SetActive(false);
    }

    /// <summary>
    /// Ground Tag가 있는 오브젝트의 레이캐스팅 좌표를 저장합니다.
    /// </summary>
    private void SetRayHitPos_Ground()
    {
        //지면 확인
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Default")))
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

                distance = Vector3.Distance(transform.position, _targetPos);

                //방향벡터 갱신
                dir = _targetPos - transform.position;
                dir.Normalize();

                //회전
                _animator.transform.LookAt(new Vector3(_targetPos.x, transform.position.y, _targetPos.z));

                //공격 대상 초기화
                atkTargetPlayer = null;
            }
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
        if(!photonView.ObservedComponents.Contains(animator.GetComponent<PhotonAnimatorView>()))
            photonView.ObservedComponents.Add(animator.GetComponent<PhotonAnimatorView>());
    }

    /// <summary>
    /// 애니메이터 컴포넌트 널 체크
    /// </summary>
    public bool GetNullCheck_Animator()
    {
        return _animator == null;
    }

    /// <summary>
    /// 경사면 방향벡터 보정
    /// </summary>
    private void Correction_DirectionVector()
    {
        //dir = 보정.. 외적벡터
        Ray ray = new Ray(new Vector3(transform.position.x, transform.position.y + 1.3f, transform.position.z), dir);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 0.6f, LayerMask.GetMask("Default")))
        {
            Debug.Log(hit.transform.name + " Input : " + dir);
            var T = Vector3.Cross(hit.normal, dir);
            var Out = Vector3.Cross(T, hit.normal);
            dir = Out;
            Debug.Log(hit.transform.name + " Output : " + dir);
        }
        else
        {
            if(atkTargetPlayer == null)
            {
                dir = _targetPos - transform.position;
                dir.Normalize();
            }
            else
            {
                dir = atkTargetPlayer.transform.position - transform.position;
                dir.Normalize();
            }
        }
    }

    /// <summary>
    /// RPC - 특정 애니메이션을 실행시킵니다.
    /// </summary>
    [PunRPC]
    private void CallbackRPC_AnimatorTrigger(string animTriggerName)
    {
        //if (!photonView.IsMine) return;

        //Debug.Log(photonView.ViewID + " AnimTriggered " + animTriggerName);
        _animator.SetTrigger(animTriggerName);
    }

    /// <summary>
    /// Receiver Error로 인한 Attack Event 코루틴
    /// AnimationEvent 'AnimEvent_Attack' on animation 'PolyAnim|Fight_Punch_Right' has no receiver!
    /// </summary>
    /// <returns></returns>
    private IEnumerator IE_AnimEvent_Attack()
    {
        yield return new WaitForSeconds(atkAnimTime * 0.5f);
        AnimEvent_Attack();
    }

    /// <summary>
    /// Animation Event - 공격 모션의 특정 프레임에서 데미지 전달을 시킵니다.
    /// </summary>
    public void AnimEvent_Attack()
    {
        //공격 상태가 아니라면 리턴
        if (!_myState.Equals(PLAYER_STATE.ATTACK)) return;
        //중간에 대상 플레이어가 이탈한 경우 리턴
        if (atkTargetPlayer == null) return;

        atkTargetPlayer.TakeDamage(_attackDamage);
    }

    /// <summary>
    /// 플레이어가 피격 당했습니다.
    /// </summary>
    /// <param name="id">피격당한 플레이어 포톤 유저 ID</param>
    /// <param name="damage">피격 데미지</param>
    //public void TakeDamage(string id, int damage)
    public void TakeDamage(float damage)
    {
        if (_currHP < 0) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage);
    }

    [PunRPC]
    private void CallbackRPC_SyncHP(int endDamage)
    {
        _currHP -= endDamage;
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

    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if(other.gameObject.layer.Equals(LayerMask.NameToLayer("Grass")))
        {
            _animator.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader = alphaShader;
            _onHide = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.gameObject.layer.Equals(LayerMask.NameToLayer("Grass")))
        {
            _animator.transform.GetChild(0).GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader = baseShader;
            _onHide = false;
        }
    }
}
