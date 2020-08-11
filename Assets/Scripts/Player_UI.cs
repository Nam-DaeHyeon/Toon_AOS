using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Private UI")]
    [SerializeField] UI_SkillSlot[] _skillSlots;
    [SerializeField] UI_SkillDesc _skillDesc;
    private ItemViewer _viewer;
    public Canvas UI_WorldCvs;

    /// <summary>
    /// 스킬 공격 시, 공격 범위를 가시화 해주기 위한 라인렌더러 오브젝트
    /// </summary>
    GameObject _lineObj;
    float radiovector = 5f;
    float radioangle = 45f;

    private void SetInitAddr_SkillSlot()
    {
        SkillViewer viewer = FindObjectOfType<SkillViewer>();

        _skillSlots = viewer.slots;
        foreach (var slot in _skillSlots) slot.owner = this;

        _skillDesc = viewer.desc;
        
        SetInit_MySkillSet(-1);
        Hide_SkillDesc();

        GetSkillPoint();
    }

    private void SetInitAddr_ItemViewer()
    {
        ItemViewer viewer = FindObjectOfType<ItemViewer>();
        viewer.SetInitAddr();

        _viewer = viewer;
    }

    #region 스킬 관련
    /// <summary>
    /// 캐릭터별 스킬을 등록합니다.
    /// </summary>
    private void SetInit_MySkillSet(int viewId)
    {
        Player target = (viewId == -1)? this : PhotonView.Find(viewId).GetComponent<Player>();
        
        switch (GameManager.USER_CHARACTER)
        {
            default: //BEAR
                target._skillSlots[0].SetInit_Skill(target._skillSlots[0].gameObject.AddComponent<sk_Bear01_Bash>());
                target._skillSlots[1].SetInit_Skill(target._skillSlots[1].gameObject.AddComponent<sk_Bear02_Dash>());
                target._skillSlots[2].SetInit_Skill(target._skillSlots[2].gameObject.AddComponent<sk_Bear03_Guard>());
                target._skillSlots[3].SetInit_Skill(target._skillSlots[3].gameObject.AddComponent<sk_Bear04_Frenzy>());
                break;
            case "RABBIT":
                target._skillSlots[0].SetInit_Skill(target._skillSlots[0].gameObject.AddComponent<sk_Rabbit01_IceArrow>());
                target._skillSlots[1].SetInit_Skill(target._skillSlots[1].gameObject.AddComponent<sk_Rabbit02_Heal>());
                target._skillSlots[2].SetInit_Skill(target._skillSlots[2].gameObject.AddComponent<sk_Rabbit03_Barrier>());
                target._skillSlots[3].SetInit_Skill(target._skillSlots[3].gameObject.AddComponent<sk_Rabbit04_Blizard>());
                break;
            case "CHIPMUNK":
                target._skillSlots[0].SetInit_Skill(target._skillSlots[0].gameObject.AddComponent<sk_Chipmunk01_Stab>());
                target._skillSlots[1].SetInit_Skill(target._skillSlots[1].gameObject.AddComponent<sk_Chipmunk02_Hiding>());
                target._skillSlots[2].SetInit_Skill(target._skillSlots[2].gameObject.AddComponent<sk_Chipmunk03_Leap>());
                target._skillSlots[3].SetInit_Skill(target._skillSlots[3].gameObject.AddComponent<sk_Chipmunk04_Kill>());
                break;
            case "CAT":
                target._skillSlots[0].SetInit_Skill(target._skillSlots[0].gameObject.AddComponent<sk_Cat01_PoisonArrow>());
                target._skillSlots[1].SetInit_Skill(target._skillSlots[1].gameObject.AddComponent<sk_Cat02_Hallucination>());
                target._skillSlots[2].SetInit_Skill(target._skillSlots[2].gameObject.AddComponent<sk_Cat03_Trap>());
                target._skillSlots[3].SetInit_Skill(target._skillSlots[3].gameObject.AddComponent<sk_Cat04_GravityBoom>());
                break;
        }

        for (int i = 0; i < _skillSlots.Length; i++)
        {
            _projectiles[i].SetInit_Parameters(target._skillSlots[i].Get_Skill(), transform, _animator.transform);
            target._skillSlots[i].SetInit_Projectile(_projectiles[i]);
        }
    }
    
    public Skill[] Get_PrivateSkills()
    {
        return new Skill[] { _skillSlots[0].GetComponent<Skill>(), _skillSlots[1].GetComponent<Skill>(), _skillSlots[2].GetComponent<Skill>(), _skillSlots[3].GetComponent<Skill>() };
    }

    /// <summary>
    /// 스킬 설명창을 활성화합니다.
    /// </summary>
    public void ShowUp_SkillDesc(Vector2 pos, string name, string mainDesc, int level)
    {
        _skillDesc.tmpName.text = name;
        _skillDesc.tmpDesc.text = mainDesc;
        _skillDesc.tmpLevel.text = level.ToString();
        
        _skillDesc.transform.position = pos;
        _skillDesc.gameObject.SetActive(true);
    }

    /// <summary>
    /// 스킬 설명창을 비활성화합니다.
    /// </summary>
    public void Hide_SkillDesc()
    {
        _skillDesc.gameObject.SetActive(false);
    }

    /// <summary>
    /// 스킬포인트를 얻었습니다.
    /// </summary>
    public void GetSkillPoint()
    {
        _skillPoint++;

        for (int i = 0; i < _skillSlots.Length; i++)
        {
            _skillSlots[i].Show_LevelUPButton();
        }

    }

    /// <summary>
    /// 스킬 레벨업 버튼들을 비활성합니다.
    /// </summary>
    public void Hide_SkillLvUPBtns()
    {
        for (int i = 0; i < _skillSlots.Length; i++)
            _skillSlots[i].Hide_LevelUPButton();
    }

    #endregion

    /// <summary>
    /// 라인 렌더러 오브젝트 초기화
    /// </summary>
    public void Set_InitLineRendererObj(GameObject lineObj)
    {
        _lineObj = lineObj;
        _lineObj.transform.parent = transform;
        _lineObj.transform.localPosition = Vector3.zero;
        _lineObj.transform.localEulerAngles = Vector3.zero;
        _lineObj.SetActive(false);

        //Sample Test...
        //StartCoroutine(IE_TEST_TargetingRender());
    }

    /// <summary>
    /// 라인 렌더러에 사용되는 파라미터를 재설정합니다.
    /// </summary>
    /// <param name="range">라인 렌더러 길이</param>
    /// <param name="angle">라인 렌더러 생성 각도.</param>
    public void SetParam_LineRender(float range, int angle)
    {
        radiovector = range;
        radioangle = angle;
    }

    /// <summary>
    /// 투사체 방향을 지정할 라인렌더러의 포워딩 노멀을 반환합니다.
    /// </summary>
    public Vector3 GetForwordDir_LineRender()
    {
        return _lineObj.transform.forward;
    }

    /// <summary>
    /// 라인 렌더러를 활성화합니다.
    /// </summary>
    public void Draw_LineRender()
    {
        StartCoroutine(IE_Draw_LineRender());
    }

    /// <summary>
    /// 라인 렌더러를 비활성화합니다.
    /// </summary>
    public void DrawOff_LineRender()
    {
        _lineObj.SetActive(false);
    }

    #region Test SkillArea Render
    IEnumerator IE_TEST_TargetingRender()
    {
        //부채꼴 렌더
        LineRenderer line = _lineObj.GetComponent<LineRenderer>();
        if (line == null) line = _lineObj.AddComponent<LineRenderer>();

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Escape));

        _lineObj.transform.localPosition = Vector3.zero;
        _lineObj.transform.localEulerAngles = Vector3.zero;

        Vector3[] linePos = new Vector3[(int)radioangle + 2];
        //linePos[0] = transform.position;
        linePos[0] = Vector3.zero;
        //linePos[(int)radioangle + 1] = transform.position;
        linePos[(int)radioangle + 1] = Vector3.zero;
        _lineObj.transform.Rotate(-Vector3.up * radioangle * 0.5f, Space.Self);
        GameObject tempTr = new GameObject();
        tempTr.transform.parent = _lineObj.transform;
        tempTr.transform.localPosition = Vector3.zero;
        tempTr.transform.localEulerAngles = Vector3.zero;

        for (int i = 1; i < 2 * radioangle + 1; i++)
        {
            if (i >= linePos.Length) break;
            //linePos[i] = tempTr.transform.position + tempTr.transform.forward * radiovector;
            linePos[i] = linePos[0] + tempTr.transform.forward * radiovector;
            tempTr.transform.Rotate(Vector3.up, Space.Self);
        }
        Destroy(tempTr.gameObject);

        line.positionCount = linePos.Length;
        line.SetPositions(linePos);

        //yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Escape));
        //StartCoroutine(IE_TEST_TargetingRender());
    }
    #endregion

    private IEnumerator IE_Draw_LineRender()
    {
        LineRenderer line = _lineObj.GetComponent<LineRenderer>();
        if (line == null) line = _lineObj.AddComponent<LineRenderer>();

        _lineObj.transform.localEulerAngles = Vector3.zero;

        Vector3[] linePos = null;

        //Draw LineRenderer
        if (radioangle == 0)
        {
            linePos = new Vector3[2];
            linePos[0] = Vector3.zero;
            linePos[1] = linePos[0] + _lineObj.transform.forward * radiovector;
        }
        else if(radioangle == 360)
        {
            linePos = new Vector3[(int)radioangle];

            GameObject tempTr = new GameObject();
            tempTr.transform.parent = _lineObj.transform;
            tempTr.transform.localPosition = Vector3.zero;
            tempTr.transform.localEulerAngles = Vector3.zero;
            for (int i = 0; i < radioangle; i++)
            {
                if (i >= linePos.Length) break;
                //linePos[i] = tempTr.transform.position + tempTr.transform.forward * radiovector;
                linePos[i] = linePos[0] + tempTr.transform.forward * radiovector;
                tempTr.transform.Rotate(Vector3.up, Space.Self);
            }
            Destroy(tempTr.gameObject);
        }
        else
        {
            linePos = new Vector3[(int)radioangle + 2];
            //linePos[0] = transform.position;
            linePos[0] = Vector3.zero;
            //linePos[(int)radioangle + 1] = transform.position;
            linePos[(int)radioangle + 1] = Vector3.zero;
            _lineObj.transform.Rotate(-Vector3.up * radioangle * 0.5f, Space.Self);
            GameObject tempTr = new GameObject();
            tempTr.transform.parent = _lineObj.transform;
            tempTr.transform.localPosition = Vector3.zero;
            tempTr.transform.localEulerAngles = Vector3.zero;

            for (int i = 1; i < 2 * radioangle + 1; i++)
            {
                if (i >= linePos.Length) break;
                //linePos[i] = tempTr.transform.position + tempTr.transform.forward * radiovector;
                linePos[i] = linePos[0] + tempTr.transform.forward * radiovector;
                tempTr.transform.Rotate(Vector3.up, Space.Self);
            }
            Destroy(tempTr.gameObject);
        }
        line.positionCount = linePos.Length;
        line.SetPositions(linePos);
        _lineObj.SetActive(true);

        //Trace Mouse Cursor
        while(_lineObj.activeInHierarchy)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Default")))
            {
                _lineObj.transform.LookAt(new Vector3(hit.point.x, transform.position.y, hit.point.z));
            }

            yield return null;
        }
    }

    /// <summary>
    /// 상세 능력치가 표시된 창의 HP 값을 갱신합니다.
    /// </summary>
    private void SetUpdate_ViewerHP()
    {
        if (!photonView.IsMine) return; 
        if (_viewer == null) return;

        _viewer.Update_PlayerSpecHp();
    }

    /// <summary>
    /// 일반 공격 타겟팅 커서 아이콘을 설정합니다.
    /// </summary>
    private void SetIcon_TargetMouseCursor()
    {
        StartCoroutine(IE_SetIcon_MouseCursor("MouseCursorIcons/Targeting"));
    }

    /// <summary>
    /// 기본 커서 아이콘을 설정합니다.
    /// </summary>
    private void SetIcon_IdleMouseCursor()
    {
        StartCoroutine(IE_SetIcon_MouseCursor(null));
    }

    private IEnumerator IE_SetIcon_MouseCursor(string path)
    {
        yield return new WaitForEndOfFrame();

        if (path == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            yield break;
        }

        Texture2D newTex = Resources.Load(path) as Texture2D;

        float _x = newTex.width * 0.5f;
        float _y = newTex.height * 0.5f;

        Vector2 pivot = new Vector2(_x, _y);
        Cursor.SetCursor(newTex, pivot, CursorMode.Auto);
    }

}

