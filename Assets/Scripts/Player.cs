﻿using System.Collections;
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
    ATTACK,
    CAST
}

public partial class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    /// <summary> 게임에 온전히 로드된 경우 활성화 : 이외 공격 무효 </summary>
    bool m_ready = false;

    /// <summary> 플레이어가 이동할 좌표 </summary>
    Vector3 _targetPos;

    #region 플레이어 능력치 파라미터
    PLAYER_STATE _myState = PLAYER_STATE.IDLE;

    int _level = 1;

    float _currHP = 0;  //현재 체력
    float _maxHP = 100; //최대 체력
    float _currSP = 0F; //현재 쉴드량
    float _maxSP = 0F;  //최대 쉴드량
    float _attackDamage = 5f;       //공격력
    float _attackDistance = 2.5f;   //공격범위
    int _defence = 0;   //물리방어력
    int _mdefence = 0;  //마법방어력

    float _speed = 5f;  //이동속도

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

    PlayerProjectile[] _projectiles;
    public Coroutine runningSkillRoutine { get; set; }
    #endregion

    #region 컴포넌트
    [SerializeField] Camera _mainCamera;
    [SerializeField] TextMeshPro _textNickName;
    [SerializeField] Image _imgHPBar;
    [HideInInspector] public Animator _animator { get; set; }
    [HideInInspector] public SkinnedMeshRenderer _skinRender { get; set; }
    #endregion
    
    //커서 오브젝트 샘플
    [HideInInspector] public GameObject _cursorObj;

    private void Awake()
    {
        baseShader = Shader.Find("Unlit/CellShader");
        alphaShader = Shader.Find("Unlit/CellShaderAlpha");

        //프로젝타일 생성
        if (photonView.IsMine)
        {
            _projectiles = new PlayerProjectile[4];
            for (int i = 0; i < _projectiles.Length; i++)
            {
                GameObject newObj = new GameObject("Player Projectile");
                newObj.SetActive(false);
                _projectiles[i] = newObj.AddComponent<PlayerProjectile>();
                Collider newCol = newObj.AddComponent<SphereCollider>();
                newCol.isTrigger = true;
            }
        }
    }

    private void Start()
    {
        //Set_InitParameter();
        photonView.RPC("CallbackRPC_InitParameter", RpcTarget.AllBuffered);

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) Debug.Log(photonView);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Debug.Log(photonView.Owner);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Debug.Log(photonView.Owner.NickName);
    }

    /// <summary>
    /// RPC 동기화. 초기 플레이어의 능력치를 동기화합니다.
    /// </summary>
    [PunRPC]
    //private void Set_InitParameter()
    private void CallbackRPC_InitParameter()
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

        //로컬 쉐이더 초기화
        _skinRender.material.shader = baseShader;

        if (photonView.IsMine)
        {
            _cursorObj.SetActive(false);
            _lineObj.SetActive(false);
        }
        else
        {
            if(_cursorObj != null) Destroy(_cursorObj.gameObject);
            if(_lineObj != null) Destroy(_lineObj.gameObject);
        }
    }

    /// <summary>
    /// 플레이어 컨트롤러 스테이트 머신
    /// </summary>
    public void Set_StateMachine(PLAYER_STATE nextState)
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
            case PLAYER_STATE.CAST:
                //StartCoroutine(IE_CastController());
                break;
        }
    }

    public PLAYER_STATE Get_CurrentState()
    {
        return _myState;
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

    #region 플레이어 스킬 시전 중 컨트롤러 코루틴
    IEnumerator IE_CastController()
    {
        yield break;
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

            //스킬 사용 퀵버튼
            if (!ctrlPressed)
            {
                if (Input.GetKeyDown(KeyCode.Q)) _skillSlots[0].SetUse_Skill(KeyCode.Q);
                if (Input.GetKeyDown(KeyCode.W)) _skillSlots[1].SetUse_Skill(KeyCode.W);
                if (Input.GetKeyDown(KeyCode.E)) _skillSlots[2].SetUse_Skill(KeyCode.E);
                if (Input.GetKeyDown(KeyCode.R)) _skillSlots[3].SetUse_Skill(KeyCode.R);
            }
            //스킬 레벨업 퀵버튼
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
    /// 마우스 포인트가 가리키는 지면 좌표를 반환합니다.
    /// </summary>
    /// <returns>포인터가 있을 경우 지표면 좌표 반환. 포인터가 없을 경우 플레이어의 절대좌표를 반환합니다.</returns>
    public Vector3 GetHitPoint()
    {
        //지면 확인
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 200, LayerMask.GetMask("Default")))
        {
            if (hit.transform.tag.Equals("Ground"))
            {
                return new Vector3(hit.point.x, transform.position.y, hit.point.z);
            }
        }

        return transform.position;
    }

    /// <summary>
    /// 스킬 사용으로 인한 시선 재설정 및 행동 중지를 명렵합니다.
    /// </summary>
    public void Callback_SetDir_OnKeyUp()
    {
        isMove = false;
        distance = 0f;
        //photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, "IDLE");
        //CallbackRPC_AnimatorTrigger("IDLE")
    }

    #region Animator Section
    /// <summary>
    /// 애니메이터 컴포넌트 등록
    /// </summary>
    public void SetAnimatorComponent(Animator animator)
    {
        if (animator == null) _animator = GetComponentInChildren<Animator>();
        else _animator = animator;
        _skinRender = _animator.transform.GetComponentInChildren<SkinnedMeshRenderer>();

        //애니메이션 동기화를 위해 옵저버 등록
        if (!photonView.ObservedComponents.Contains(animator.GetComponent<PhotonAnimatorView>()))
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
    /// 애니메이션 트리거를 동작시킵니다.
    /// </summary>
    public void SetAnimTrigger(string keyName)
    {
        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, keyName);
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
    #endregion

    /// <summary>
    /// 경사면 방향벡터 보정 (TEST)
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
            if (atkTargetPlayer == null)
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

    #region HP & Shield 피해관련 연산 및 능력치 갱신
    /// <summary>
    /// 플레이어가 물리 피해를 받았습니다.
    /// </summary>
    /// <param name="damage">피격 데미지</param>
    //public void TakeDamage(string id, int damage)
    public void TakeDamage(float damage)
    {
        if (_currHP < 0) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage, false);
    }

    /// <summary>
    /// 플레이어가 마법 피해를 받았습니다.
    /// </summary>
    /// <param name="damage">피격 데미지</param>
    //public void TakeDamage(string id, int damage)
    public void TakeMDamage(float damage)
    {
        if (_currHP < 0) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage, true);
    }

    /// <summary>
    /// [RPC] 플레이어 체력 동기화
    /// </summary>
    /// <param name="endDamage"></param>
    [PunRPC]
    private void CallbackRPC_SyncHP(int endDamage, bool isMagic)
    {
        int tempDamage = endDamage;
        if (_currSP > 0)
        {
            //잔여 쉴드량이 남을 경우
            if (_currSP - endDamage > 0)
            {
                _currSP -= tempDamage;
                _imgHPBar.fillAmount = _currSP / _maxSP;
                return;
            }
            else
            {
                tempDamage += ((int)_currSP - endDamage);
                _imgHPBar.color = Color.red;
            }
        }

        int endDefence = _defence;
        if (isMagic) endDefence = _mdefence;
        tempDamage -= endDamage;
        if (tempDamage <= 0) tempDamage = 0;    //유효 데미지 없음

        _currHP -= tempDamage;
        _imgHPBar.fillAmount = _currHP / _maxHP;
    }

    /// <summary>
    /// 최대 쉴드량을 설정합니다.
    /// </summary>
    /// <param name="sp"></param>
    public void Set_MaxShield(int sp)
    {
        _maxSP = sp;
        _currSP = _maxSP;

        //체력바 업데이트 (+쉴드바 추가)
        _imgHPBar.color = Color.white;
        _imgHPBar.fillAmount = _currSP / _maxSP;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, 0);
    }

    /// <summary>
    /// 현재 쉴드량을 반환합니다.
    /// </summary>
    public float Get_CurrentShield()
    {
        return _currSP;
    }

    /// <summary>
    /// 이동속도를 반환합니다.
    /// </summary>
    public float Get_Speed()
    {
        return _speed;
    }

    /// <summary>
    /// 쉴드를 제거합니다.
    /// </summary>
    public void Set_ZeroShield()
    {
        _maxSP = 0;
        _currSP = _maxSP;

        //체력바 업데이트
        _imgHPBar.color = Color.red;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, 0);
    }
    #endregion

    /// <summary>
    /// 버프/디버프로 인한 능력치를 갱신합니다. 합연산으로 처리합니다.
    /// </summary>
    /// <param name="paramName">ATTACKDAMAGE | DEFENCE | MDEFENCE | SPEED</param>
    /// <param name="value">합 연산 수치</param>
    /// <param name="isBuff">합 연산 여부. false 일경우, 차 연산을 진행한다.</param>
    public void UpdateParamAboutBuff(string paramName, int value, bool isBuff)
    {
        photonView.RPC("CallbackRPC_SyncParam", RpcTarget.All, paramName, value, isBuff);
    }

    /// <summary>
    /// 다른 플레이어로 인해 버프/디버프를 받았습니다.
    /// </summary>
    /// <param name="paramName">ATTACKDAMAGE | DEFENCE | MDEFENCE | SPEED</param>
    /// <param name="value">합 연산 수치</param>
    /// <param name="isBuff">합 연산 여부. false 일경우, 차 연산을 진행한다.</param>
    /// <param name="duration">버프/디버프 지속시간</param>
    public void GetBuff_FromOthers(string paramName, int value, bool isBuff, float duration)
    {
        StartCoroutine(IE_BuffTimer(paramName, value, isBuff, duration));
    }

    private IEnumerator IE_BuffTimer(string paramName, int value, bool isBuff, float duration)
    {
        photonView.RPC("CallbackRPC_SyncParam", RpcTarget.All, paramName, value, isBuff);
        yield return new WaitForSeconds(duration);
        photonView.RPC("CallbackRPC_SyncParam", RpcTarget.All, paramName, value, !isBuff);
    }

    [PunRPC]
    private void CallbackRPC_SyncParam(string paramName, int value, bool isBuff)
    {
        int corr = (isBuff) ? 1 : -1;
        switch (paramName)
        {
            case "ATTACKDAMAGE":
                _attackDamage += corr * value;
                break;
            case "DEFENCE":
                _defence += corr * value;
                break;
            case "MDEFENCE":
                _mdefence += corr * value;
                break;
            case "SPEED":
                _speed += corr * value;
                break;
        }
    }

    #region Bush & Invisible Section
    BushGrass _currBush;
    /// <summary>
    /// 부쉬에 들어갔습니다.
    /// </summary>
    public void GetIn_Bush(BushGrass bush)
    {
        _currBush = bush;
        Set_HidingState();
    }

    /// <summary>
    /// 부쉬에서 나왔습니다.
    /// </summary>
    public void GetOut_Bush(BushGrass bush)
    {
        Set_UnHidingState();
        bush = null;
    }

    /// <summary>
    /// 투명 상태로 돌입합니다.
    /// </summary>
    public void Set_HidingState()
    {
        photonView.RPC("CallbackRPC_Hide", RpcTarget.All);
    }

    /// <summary>
    /// 투명 상태를 해제합니다.
    /// </summary>
    public void Set_UnHidingState()
    {
        photonView.RPC("CallbackRPC_UnHide", RpcTarget.All);
    }

    [PunRPC]
    private void CallbackRPC_Hide()
    {
        //gameObject.layer = LayerMask.NameToLayer("HidePlayer");
        _skinRender.material.shader = alphaShader;
        //_skinRender.sharedMaterial.SetColor("_Color", new Color32(255, 255, 255, 110));

        //뒤늦게 들어온 애가 있을 경우 전에 들어온 친구한테 렌더링되도록 업데이트.. allPlayers로 해결...
        //수풀 안에선 서로를 타겟팅할 수 있도록, 밖에선 안이 타겟팅 못하게.. layerMask 설정 시도...
        if (photonView.IsMine)
        {
            _skinRender.material.SetColor("_Color", new Color(1, 1, 1, 0.43f));
        }
        else
        {
            //같은 부쉬에 있는 플레이어의 경우 생략
            for (int i = 0; i < MainManager.instance.allPlayers.Length; i++)
            {
                //자신(this) & NULL 예외처리
                if (MainManager.instance.allPlayers[i].Equals(this)) continue;
                if (_currBush == null) continue;

                if (_currBush.Equals(MainManager.instance.allPlayers[i]._currBush))
                {

                }
                else
                {
                    //부쉬 밖에 있는 Other's Camera : 자신의 캐릭터 비활성화 
                    _skinRender.material.SetColor("_Color", new Color(1, 1, 1, 0));
                    UI_WorldCvs.gameObject.SetActive(false);
                }
            }
        }
    }

    [PunRPC]
    private void CallbackRPC_UnHide()
    {
        //gameObject.layer = LayerMask.NameToLayer("Player");
        _skinRender.material.shader = baseShader;
    }
    #endregion

    /// <summary>
    /// [RPC] 플레이어 레벨업 동기화
    /// </summary>
    [PunRPC]
    private void CallbackRPC_PlayerLevelUP()
    {
        _level++;
        GetSkillPoint();
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
        //다른 플레이어에게 마우스를 올렸을 때
        if (!photonView.IsMine)
        {
            //은신 상태일 때 윤곽선 생략
            //if (_onHide) return;
            if (_skinRender.material.GetColor("_Color").a == 0) return;
            
            _animator.GetComponent<Outline>().OutlineColor = Color.red;
        }
        //플레이어 자신에게 마우스를 올렸을때..
        else _animator.GetComponent<Outline>().OutlineColor = Color.white;

        _animator.GetComponent<Outline>().enabled = true;
    }

    private void OnMouseOver()
    {
        if (_skinRender.material.GetColor("_Color").a == 0) OnMouseExit();
    }

    private void OnMouseExit()
    {
        _animator.GetComponent<Outline>().enabled = false;
    }
}
