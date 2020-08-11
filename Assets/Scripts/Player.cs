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
    ATTACK,
    CAST,
    DEAD
}

public partial class Player : MonoBehaviourPunCallbacks, IPunObservable, ITargetUnit
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
    float _mattackDamage = 0f;
    float _attackDistance = 2.5f;   //공격범위
    int _defence = 0;   //물리방어력
    int _mdefence = 0;  //마법방어력

    float _speed = 5f;  //이동속도
    float _meleeMissileSpd = 45f;   //원거리 평타 프로젝타일 속도

    public bool isInvincible { get; set; } = false; //무적 상태

    public int _skillPoint { set; get; } = 0;

    public string[] inventory = new string[6];
    public int money = 0;
    #endregion

    #region 플레이어 행동 트리거 및 변수
    bool isMove = false;    //움직이고 있는 상태
    public bool isMeleeAttack { get; set; } //기본 공격을 하고 있는 상태
    float distance = 0f;
    Vector3 dir;

    bool atkToggle = false;    //공격 대상 지정 상태
    ITargetUnit targetUnit = null;  //공격 대상
    float atkAnimTime = 1.042f;    //공격 애니메이션 재생 시간

    Shader baseShader;
    Shader alphaShader;

    [SerializeField] Transform[] _meleeProjectile;  //평타 프로젝타일
    Coroutine[] meleeCoroutine = new Coroutine[3];
    int meleeIndex = 0;
    [SerializeField] PlayerProjectile[] _projectiles;   //스킬 프로젝타일
    public Coroutine runningSkillRoutine { get; set; }
    #endregion

    #region 컴포넌트
    [SerializeField] Camera _mainCamera;
    [SerializeField] TextMeshPro _textNickName;
    [SerializeField] Image _imgHPBar;
    [HideInInspector] public Animator _animator { get; set; }
    [HideInInspector] public SkinnedMeshRenderer _skinRender { get; set; }
    [HideInInspector] public MeshRenderer _weaponRender { get; set; }
    #endregion

    private void Awake()
    {
        baseShader = Shader.Find("Unlit/CellShader");
        alphaShader = Shader.Find("Unlit/CellShaderAlpha");

        //프로젝타일 생성
        if (photonView.IsMine)
        {
            /*
            _projectiles = new PlayerProjectile[4];
            for (int i = 0; i < _projectiles.Length; i++)
            {
                GameObject newObj = new GameObject("Player Projectile");
                newObj.SetActive(false);
                _projectiles[i] = newObj.AddComponent<PlayerProjectile>();
                Collider newCol = newObj.AddComponent<SphereCollider>();
                newCol.isTrigger = true;
            }
            */

            photonView.RPC("CallbackRPC_InitProjectileTransform", RpcTarget.AllBuffered);
        }
    }

    private void Start()
    {
        //Set_InitParameter();

        if (photonView.IsMine)
        {
            //photonView.RPC("CallbackRPC_SetInitParameter", RpcTarget.All);
            _mainCamera.GetComponent<CameraFilter>().setGray = false;

            SetInitAddr_SkillSlot();
            SetInitAddr_ItemViewer();

            photonView.RPC("CallbackRPC_InitParameter", RpcTarget.AllBuffered, photonView.ViewID, GameManager.USER_CHARACTER);
            SetUpdate_ViewerHP();

            SetIcon_IdleMouseCursor();

            StartCoroutine(IE_BaseController());
            StartCoroutine(IE_PlayerInputKeyManager());
        }

        m_ready = true;

        //UI 설정
        //photonView.RPC("CallbackRPC_SyncNickname", RpcTarget.All, photonView.Owner.NickName);
        //photonView.RPC("CallbackRPC_SyncHPBar", RpcTarget.All);
        
        _textNickName.text = photonView.Owner.NickName;
        _imgHPBar.fillAmount = _currHP / _maxHP;
    }

    /// <summary>
    /// 초기 플레이어의 능력치를 동기화합니다.
    /// </summary>
    //private void Set_InitParameter()
    [PunRPC]
    private void CallbackRPC_InitParameter(int viewId, string characterName)
    {
        var player = PhotonView.Find(viewId).GetComponent<Player>();

        //캐릭터에 따른 능력치 설정
        switch(characterName)
        {
            default:
                player.atkAnimTime = 1.042f;    //공격 애니메이션 재생 시간
                player._attackDamage = 10;
                player._defence = 2;
                break;
            case "RABBIT":
                player.atkAnimTime = 1.7f;
                player._attackDamage = 5;
                player._attackDistance = 8f;
                break;
            case "CAT":
                player.atkAnimTime = 0.76f;
                player._attackDamage = 6;
                player._attackDistance = 6f;
                break;
        }

        //캐릭터별 스킬 설정
        //player.SetInit_MySkillSet(viewId);
        //player.Hide_SkillDesc();

        //능력치 초기화 및 시작 설정
        _currHP = _maxHP;
        //GetSkillPoint(viewId);
        isInvincible = false;

        //컨트롤러 스테이트 초기화
        player._myState = PLAYER_STATE.IDLE;

        //트리거 & 변수 초기화
        player.isMove = false;
        player.distance = 0f;
        player.dir = Vector3.zero;
        player.atkToggle = false;
        player.targetUnit = null;

        //로컬 쉐이더 초기화
        player._skinRender.material.shader = baseShader;
        player._weaponRender.material.shader = baseShader;

        //player._weaponRender.material.SetColor("_Color", new Color(1, 1, 1, 1));

        if (photonView.IsMine)
        {
            player._lineObj.SetActive(false);
        }
        else
        {
            _mainCamera.gameObject.SetActive(false);
            if (_lineObj != null) Destroy(_lineObj.gameObject);
        }
    }

    [PunRPC]
    private void CallbackRPC_InitProjectileTransform()
    {
        for (int i = 0; i < _projectiles.Length; i++)
        {
            _projectiles[i].transform.parent = null;
            _projectiles[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 플레이어 컨트롤러 스테이트 머신
    /// </summary>
    public void Set_StateMachine(PLAYER_STATE nextState)
    {
        _myState = nextState;
        if (_currHP <= 0) _myState = PLAYER_STATE.DEAD;

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
            case PLAYER_STATE.DEAD:
                _mainCamera.GetComponent<CameraFilter>().setGray = true;
                _animator.gameObject.SetActive(false);
                if(photonView.IsMine) PhotonNetwork.Instantiate("Grave", transform.position, _animator.transform.rotation);
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

            if (targetUnit == null)
            {
                #region 이동관련 : 입력 좌표에 도달했는가
                if (isMove)
                {
                    distance = Vector3.Distance(transform.position, _targetPos);
                    if (distance < 0.15f)
                    {
                        isMove = false;
                        //_animator.SetTrigger("IDLE");
                        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, photonView.ViewID, "IDLE");
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
                distance = Vector3.Distance(transform.position, targetUnit.Get_Position());
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
                        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, photonView.ViewID, "MOVE");
                    }
                    isMove = true;

                    _animator.transform.LookAt(new Vector3(targetUnit.Get_Position().x, transform.position.y, targetUnit.Get_Position().z));

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
        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, photonView.ViewID, "ATTACK");

        //애니메이션 이벤트 리시버 에러로 인한 코루틴 처리
        StartCoroutine(IE_AnimEvent_Attack());

        float delay = atkAnimTime;

        while(_currHP > 0 && _myState.Equals(PLAYER_STATE.ATTACK))
        {
            if (targetUnit == null) break;
            _animator.transform.LookAt(targetUnit.Get_Position());

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
                distance = Vector3.Distance(targetUnit.Get_Position(), transform.position);
                if (distance > _attackDistance)
                {
                    targetUnit = null;
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
            else if (Input.GetKeyUp(KeyCode.A))
            {
                atkToggle = false;
            }

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

            //아이템 사용 퀵버튼
            if (Input.GetKeyDown(KeyCode.Alpha1)) UseItem_Inventory(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) UseItem_Inventory(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) UseItem_Inventory(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) UseItem_Inventory(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) UseItem_Inventory(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) UseItem_Inventory(5);

            yield return null;
        }
    }

    /// <summary> 타겟팅용 레이캐스트를 활성화합니다. </summary>
    IEnumerator IE_FollowTargetCursor()
    {
        SetIcon_TargetMouseCursor();

        while (atkToggle)
        {
            //스킬 시전 중일 때에는 타겟팅 기능 일시정지
            yield return new WaitUntil(() => !_myState.Equals(PLAYER_STATE.CAST));

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            #region 타겟팅관련 : 다른 플레이어를 타겟팅
            if (Input.GetMouseButtonDown(0))
            {
                ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Player") | LayerMask.GetMask("Monster")))
                {
                    _targetPos = hit.point;

                    //방향벡터 갱신
                    dir = _targetPos - transform.position;
                    dir.Normalize();

                    //회전
                    _animator.transform.LookAt(new Vector3(_targetPos.x, transform.position.y, _targetPos.z));

                    targetUnit = hit.transform.GetComponent<ITargetUnit>();
                }

                break;
            }
            #endregion

            yield return null;
        }
        
        SetIcon_IdleMouseCursor();
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
                    photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, photonView.ViewID, "MOVE");
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
                targetUnit = null;
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
        RaycastHit[] hits;

        //if (Physics.Raycast(ray, out hit, 200, LayerMask.GetMask("Default")))
        hits = Physics.RaycastAll(ray, 200, LayerMask.GetMask("Default"));
        foreach(var hit in hits)
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
        //_skinRender = _animator.transform.GetComponentInChildren<SkinnedMeshRenderer>();
        _skinRender = _animator.GetComponent<PlayerMeshRenderLinker>().skinRender;
        _weaponRender = _animator.GetComponent<PlayerMeshRenderLinker>().weaponRender;

        if (_animator.transform.Find("MeleeProjectile 0") != null)
        {
            _meleeProjectile = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                _meleeProjectile[i] = _animator.transform.Find("MeleeProjectile " + i);
                if (_meleeProjectile[i] == null) break;
                _meleeProjectile[i].parent = null;
                MainManager.instance.Set_ActiveProjectile(_meleeProjectile[i].gameObject, false);
                //_meleeProjectile[i].gameObject.SetActive(false);
            }
        }

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
        photonView.RPC("CallbackRPC_AnimatorTrigger", RpcTarget.All, photonView.ViewID, keyName);
    }

    /// <summary>
    /// RPC - 특정 애니메이션을 실행시킵니다.
    /// </summary>
    [PunRPC]
    private void CallbackRPC_AnimatorTrigger(int viewId, string animTriggerName)
    {
        //if (!photonView.IsMine) return;

        //Debug.Log(photonView.ViewID + " AnimTriggered " + animTriggerName);
        Player player = PhotonView.Find(viewId).GetComponent<Player>();
        player._animator.SetTrigger(animTriggerName);
    }

    /// <summary>
    /// Receiver Error로 인한 Attack Event 코루틴
    /// AnimationEvent 'AnimEvent_Attack' on animation 'PolyAnim|Fight_Punch_Right' has no receiver!
    /// </summary>
    /// <returns></returns>
    private IEnumerator IE_AnimEvent_Attack()
    {
        isMeleeAttack = true;
        yield return new WaitForSeconds(atkAnimTime * 0.5f);
        AnimEvent_Attack();
        isMeleeAttack = false;
    }

    /// <summary>
    /// Animation Event - 공격 모션의 특정 프레임에서 데미지 전달을 시킵니다.
    /// </summary>
    public void AnimEvent_Attack()
    {
        //공격 상태가 아니라면 리턴
        if (!_myState.Equals(PLAYER_STATE.ATTACK)) return;
        //중간에 대상 플레이어가 이탈한 경우 리턴
        if (targetUnit == null) return;

        //근접공격
        if (_meleeProjectile.Length == 0)
        {
            targetUnit.Set_Target(this);
            targetUnit.TakeDamage(_attackDamage);
        }
        //원거리 공격일 경우 등록해둔 밀리프로젝타일 풀을 돌려가며 사용한다.
        else
        {
            if (_meleeProjectile[meleeIndex].gameObject.activeInHierarchy)
            {
                StopCoroutine(meleeCoroutine[meleeIndex]);
                //StopCoroutine(IE_MissileAttack(45f, meleeIndex));
                
                MainManager.instance.Set_ActiveProjectile(_meleeProjectile[meleeIndex].gameObject, false);
                //_meleeProjectile[meleeIndex].gameObject.SetActive(false);
            }

            meleeCoroutine[meleeIndex] = StartCoroutine(IE_MissileAttack(45f, meleeIndex));
            meleeIndex++;
            if (meleeIndex == 3) meleeIndex = 0;
        }
    }

    /// <summary>
    /// 평타 프로젝타일 프로세스
    /// </summary>
    /// <param name="spd"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private IEnumerator IE_MissileAttack(float spd, int index)
    {
        float subDistance;
        Vector3 dir;

        yield return new WaitForEndOfFrame();
        _meleeProjectile[index].transform.position = transform.position + Vector3.up;
        MainManager.instance.Set_ActiveProjectile(_meleeProjectile[index].gameObject, true);
        //_meleeProjectile[index].gameObject.SetActive(true);

        do
        {
            if (targetUnit == null) break;
            subDistance = Vector3.Distance(targetUnit.Get_Position(), new Vector3(_meleeProjectile[index].transform.position.x, targetUnit.Get_Position().y, _meleeProjectile[index].transform.position.z));

            dir = targetUnit.Get_Position() - new Vector3(_meleeProjectile[index].transform.position.x, targetUnit.Get_Position().y, _meleeProjectile[index].transform.position.z);
            dir.Normalize();
            _meleeProjectile[index].Translate(dir * _speed * Time.deltaTime, Space.World);

            yield return null;
        } while (subDistance >= 0.1f);

        if (targetUnit != null)
        {
            targetUnit.Set_Target(this);
            targetUnit.TakeDamage(_attackDamage);
        }
        MainManager.instance.Set_ActiveProjectile(_meleeProjectile[index].gameObject, false);
        // _meleeProjectile[index].gameObject.SetActive(false);
        //_meleeProjectile.parent = _animator.transform;
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
            if (targetUnit == null)
            {
                dir = _targetPos - transform.position;
                dir.Normalize();
            }
            else
            {
                dir = targetUnit.Get_Position() - transform.position;
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
        if (isInvincible) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage, false);

        SetUpdate_ViewerHP();
    }

    /// <summary>
    /// 플레이어가 마법 피해를 받았습니다.
    /// </summary>
    /// <param name="damage">피격 데미지</param>
    //public void TakeDamage(string id, int damage)
    public void TakeMDamage(float damage)
    {
        if (_currHP < 0) return;
        if (isInvincible) return;

        int endDamage = (int)damage;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endDamage, true);

        SetUpdate_ViewerHP();
    }

    /// <summary>
    /// 플레이어가 체력을 회복합니다.
    /// </summary>
    /// <param name="healAmount">회복량. (양수)</param>
    public void TakeHeal(float healAmount)
    {
        if (_currHP < 0) return;

        int endAmount = -1 * (int)healAmount;
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, endAmount, true);

        SetUpdate_ViewerHP();
    }

    public void TakeDamage_IgnoreDefence(float damage)
    {
        if (_currHP < 0) return;

        photonView.RPC("CallbackRPC_SyncHPIgnoreDefence", RpcTarget.All, photonView.ViewID, damage);

        SetUpdate_ViewerHP();
    }

    /// <summary>
    /// [RPC] 플레이어 체력 동기화
    /// </summary>
    /// <param name="endDamage"></param>
    [PunRPC]
    private void CallbackRPC_SyncHP(int endDamage, bool isMagic = false)
    {
        int tempDamage = endDamage;

        //일반적인 플레이어를 타격한 경우 (피해량)
        if (endDamage > 0)
        {
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
            tempDamage -= endDefence;
            if (tempDamage <= 0) tempDamage = 0;    //유효 데미지 없음
        }
        //플레이어를 회복한 경우 (회복량)
        else
        {
            
        }

        //회복하는 경우(음수)와 단순히 신호를 보내는 경우(0)를 제외한 피해를 받은 경우만 텍스트 로그를 출력한다.
        if (tempDamage > 0) MainManager.instance.SetVisible_HitLog(transform.position, tempDamage);

        _currHP -= tempDamage;
        _imgHPBar.fillAmount = _currHP / _maxHP;

        if(_currHP <= 0)
        {
            Set_StateMachine(PLAYER_STATE.DEAD);
        }
        if (_currHP > _maxHP) _currHP = _maxHP;
    }

    [PunRPC]
    private void CallbackRPC_SyncHPIgnoreDefence(int viewId, float damage)
    {
        Player target = PhotonView.Find(viewId).GetComponent<Player>();
        target._currHP -= damage;
        target._imgHPBar.fillAmount = _currHP / _maxHP;
        if (target._currHP <= 0) target.Set_StateMachine(PLAYER_STATE.DEAD);
    }

    /// <summary>
    /// 최대 쉴드량을 설정합니다.
    /// </summary>
    /// <param name="sp"></param>
    public void Set_MaxShield(int sp)
    {
        _maxSP = sp;
        _currSP = _maxSP;

        photonView.RPC("CallbackRPC_SyncSP", RpcTarget.All, photonView.ViewID);
    }

    [PunRPC]
    private void CallbackRPC_SyncSP(int viewId)
    {
        Player target = PhotonView.Find(viewId).GetComponent<Player>();

        //체력바 업데이트 (+쉴드바 추가)
        if (_maxSP != 0)
        {
            target._imgHPBar.color = Color.white;
            target._imgHPBar.fillAmount = _currSP / _maxSP;
        }
        else
        {
            target._imgHPBar.color = Color.red;
            target._imgHPBar.fillAmount = _currHP / _maxHP;
        }
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

    public float Get_CurrentHP()
    {
        return _currHP;
    }

    public float Get_MaxHP()
    {
        return _maxHP;
    }

    public float Get_AttackDamage()
    {
        return _attackDamage;
    }

    public float Get_MAttackDamage()
    {
        return _mattackDamage;
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
        photonView.RPC("CallbackRPC_SyncHP", RpcTarget.All, 0, false);
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

    private IEnumerator IE_BuffTimer(string paramName, int value, bool isBuff, float duration)
    {
        photonView.RPC("CallbackRPC_SyncParam", RpcTarget.All, paramName, value, isBuff);
        yield return new WaitForSeconds(duration);
        photonView.RPC("CallbackRPC_SyncParam", RpcTarget.All, paramName, value, !isBuff);
    }

    private IEnumerator IE_DotDamTimer(string paramName, int damage, float delay, float duration, Player attacker)
    {
        float timer = duration;
        while(timer >= 0)
        {
            if ((timer -= delay) < 0) break;
            yield return new WaitForSeconds(delay);

            TakeDamage_IgnoreDefence(damage);
            if (_currHP <= 0)
            {
                attacker.Add_Money(5);
                yield break;
            }
            //photonView.RPC("CallbackRPC_SyncHPIgnoreDefence", RpcTarget.All, photonView.ViewID, damage);
        }

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

        if(photonView.IsMine)
        {
            //스킬로 인한 능력치 변환값들을 뷰어와 동기화 표시합니다.
            _viewer.Update_PlayerSpec();
        }
    }

    #region Bush & Invisible Section
    BushGrass _currBush;
    /// <summary>
    /// 부쉬에 들어갔습니다.
    /// </summary>
    public void GetIn_Bush(BushGrass bush)
    {
        //_currBush = bush;
        Set_HidingState(photonView.ViewID, bush.photonView.ViewID);
    }

    /// <summary>
    /// 부쉬에서 나왔습니다.
    /// </summary>
    public void GetOut_Bush(BushGrass bush)
    {
        Set_UnHidingState(photonView.ViewID, bush.photonView.ViewID);
        //bush = null;
    }

    /// <summary>
    /// 투명 상태로 돌입합니다.
    /// 부쉬에 들어가는 것이 아니라면 두번째 파라미터 입력을 생략합니다.
    /// </summary>
    public void Set_HidingState(int playerId, int bushId = -1)
    {
        photonView.RPC("CallbackRPC_Hide", RpcTarget.All, playerId, bushId);
    }
    
    /// <summary>
    /// 투명 상태를 해제합니다.
    /// 부쉬에 나오는 것이 아니라면 두번째 파라미터 입력을 생략합니다.
    /// </summary>
    public void Set_UnHidingState(int playerId, int bushId = -1)
    {
        photonView.RPC("CallbackRPC_UnHide", RpcTarget.All, playerId, bushId);
    }

    [PunRPC]
    private void CallbackRPC_Hide(int playerId, int bushId)
    {
        _skinRender.material.shader = alphaShader;
        _weaponRender.material.shader = alphaShader;
        //_weaponRender.material.SetColor("_Color", new Color(1, 1, 1, 0));
        //_skinRender.sharedMaterial.SetColor("_Color", new Color32(255, 255, 255, 110));

        Player hidePlayer = PhotonView.Find(playerId).GetComponent<Player>();
        if(bushId != -1) hidePlayer._currBush = PhotonView.Find(bushId).GetComponent<BushGrass>();
        
        //본인은 불투명하게 표현
        if (photonView.IsMine)
        {
            _skinRender.material.SetColor("_Color", new Color(1, 1, 1, 0.43f));
            _weaponRender.material.SetColor("_Color", new Color(1, 1, 1, 0.43f));
        }
        //나머지한테는 투명하게 표현
        //뒤늦게 들어온 애가 있을 경우 전에 들어온 친구한테 렌더링되도록 업데이트.. allPlayers로 해결...
        else
        {
            //if (MainManager.instance.allPlayers == null) return;
            Player[] allPlayers = GameObject.FindObjectsOfType<Player>();

            //같은 부쉬에 있는 플레이어의 경우 생략
            //for (int i = 0; i < MainManager.instance.allPlayers.Length; i++)
            for (int i = 0; i < allPlayers.Length; i++)
            {
                //자신(this) & NULL 예외처리
                //if (MainManager.instance.allPlayers[i].Equals(this)) continue;
                if (allPlayers[i].Equals(this)) continue;
                if (_currBush == null || !_currBush.Equals(allPlayers[i]._currBush))
                {
                    //안에 있는 플레이어끼리 불투명하게 처리하는 것은 BushGrass.cs 에서 처리
                
                    //부쉬 밖에 있는 Other's Camera : 자신의 캐릭터 비활성화 
                    _skinRender.material.SetColor("_Color", new Color(1, 1, 1, 0));
                    _weaponRender.material.SetColor("_Color", new Color(1, 1, 1, 0));
                    UI_WorldCvs.gameObject.SetActive(false);
                }
            }
        }
    }

    [PunRPC]
    private void CallbackRPC_UnHide(int playerId, int bushId)
    {
        //gameObject.layer = LayerMask.NameToLayer("Player");
        _skinRender.material.shader = baseShader;
        _weaponRender.material.shader = baseShader;
        UI_WorldCvs.gameObject.SetActive(true);

        Player unPlayer = PhotonView.Find(playerId).GetComponent<Player>();
        if (bushId != -1) unPlayer._currBush = null;
    }
    #endregion

    public void Set_PlayerLevelUp()
    {
        if (!photonView.IsMine) return;
        GetSkillPoint();
        photonView.RPC("CallbackRPC_PlayerLevelUP", RpcTarget.All, photonView.ViewID);
    }

    /// <summary>
    /// [RPC] 플레이어 레벨업 동기화
    /// </summary>
    [PunRPC]
    private void CallbackRPC_PlayerLevelUP(int viewId)
    {
        Player target = PhotonView.Find(viewId).GetComponent<Player>();
        if (target._level >= 18) return;
        target._level++;
    }

    /// <summary>
    /// 이동관련이 아닌 강제적으로 위치를 조정합니다.
    /// </summary>
    /// <param name="newPos">새 좌표</param>
    public void ForceSetPosition(Vector3 newPos)
    {
        photonView.RPC("CallbackRPC_ForceSyncPosition", RpcTarget.All, newPos);
    }

    /// <summary>
    /// [RPC] 플레이어 위치 값 강제 조정
    /// </summary>
    /// <param name="viewId">플레이어 뷰아이디</param>
    /// <param name="newPos">좌표 조정 값. 이 좌표로 재설정합니다.</param>
    [PunRPC]
    private void CallbackRPC_ForceSyncPosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(GameManager.USER_NICKNAME);
            stream.SendNext(this._imgHPBar.fillAmount);
            stream.SendNext(_animator);

            // We own this player: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            _textNickName.text = (string)stream.ReceiveNext();
            this._imgHPBar.fillAmount = (float)stream.ReceiveNext();
            _animator = (Animator)stream.ReceiveNext();

            // Network player, receive data
            this.transform.position = (Vector3)stream.ReceiveNext();
            this.transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }

    private void OnMouseEnter()
    {
        if (_animator == null) return;

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
        if (_animator == null) return;

        if (_skinRender.material.GetColor("_Color").a == 0) OnMouseExit();
    }

    private void OnMouseExit()
    {
        if (_animator == null) return;

        _animator.GetComponent<Outline>().enabled = false;
    }

    /// <summary>
    /// 카테고리에 해당하는 현재 능력치 값을 반환합니다. 해당하는 값이 없을 경우 -1을 반환합니다.
    /// </summary>
    public float Get_Spec(ItemCategory category)
    {
        switch(category)
        {
            case ItemCategory.공격력:
                return _attackDamage;
            case ItemCategory.마법공격력:
                return _mattackDamage;
            case ItemCategory.방어력:
                return _defence;
            case ItemCategory.마법방어력:
                return _mdefence;
            case ItemCategory.이동속도:
                return _speed;

            case ItemCategory.체력:
                return _maxHP;

            default:
                return -1;
        }
    }

    /// <summary>
    /// 인벤토리에 아이템을 추가합니다.
    /// </summary>
    /// <param name="idx">인벤토리 슬롯 인덱스</param>
    /// <param name="itemName">추가할 아이템 이름</param>
    public void AddItem_Inventory(int idx, string itemName)
    {
        ItemBase item = ItemManager.ItemDB[itemName];

        inventory[idx] = itemName;

        for(int i = 0; i < item.specs.Count; i++)
        {
            int tempValue = item.specs[i].value;
            switch (item.specs[i].category)
            {
                case ItemCategory.공격력:
                    _attackDamage += tempValue;
                    break;
                case ItemCategory.마법공격력:
                    _mattackDamage += tempValue;
                    break;
                case ItemCategory.방어력:
                    _defence += tempValue;
                    break;
                case ItemCategory.마법방어력:
                    _mdefence += tempValue;
                    break;
                case ItemCategory.체력:
                    _maxHP += tempValue;
                    break;
                case ItemCategory.이동속도:
                    _speed += tempValue;
                    break;

                //소모품
                default:

                    break;
            }
        }

        //인벤토리 및 능력치 동기화
        //...

    }

    /// <summary>
    /// 인벤토리에 아이템을 삭제합니다.
    /// </summary>
    /// <param name="idx">인벤토리 슬롯 인덱스</param>
    public void RemoveItem_Inventory(int idx)
    {
        ItemBase item = ItemManager.ItemDB[inventory[idx]];

        inventory[idx] = "";

        for (int i = 0; i < item.specs.Count; i++)
        {
            int tempValue = item.specs[i].value;
            switch (item.specs[i].category)
            {
                case ItemCategory.공격력:
                    _attackDamage -= tempValue;
                    break;
                case ItemCategory.마법공격력:
                    _mattackDamage -= tempValue;
                    break;
                case ItemCategory.방어력:
                    _defence -= tempValue;
                    break;
                case ItemCategory.마법방어력:
                    _mdefence -= tempValue;
                    break;
                case ItemCategory.체력:
                    _maxHP -= tempValue;
                    break;
                case ItemCategory.이동속도:
                    _speed -= tempValue;
                    break;

                //소모품
                default:

                    break;
            }
        }
    }

    private void UseItem_Inventory(int invenIdx)
    {
        if (inventory[invenIdx] == "") return;
        _viewer.UseItem(invenIdx);
    }

    /// <summary>
    /// 아이템이 위치한 인벤토리 인덱스를 반환합니다.
    /// 없다면 -1을 반환합니다.
    /// </summary>
    public int GetIndex_Inventory(string itemName)
    {
        for(int i = 0; i < inventory.Length; i++)
        {
            if(inventory[i].Equals(itemName))
            {
                return i;
            }
        }

        return -1;
    }

    public void Add_Money(int amount)
    {
        money += amount;
        _viewer.Update_Money();
    }

    public Vector3 Get_Position()
    {
        return transform.position;
    }

    /// <summary>
    /// [몬스터 전용] 자신을 때린 플레이어를 등록합니다. 
    /// </summary>
    public void Set_Target(Player attacker)
    {
        return;
    }
}
