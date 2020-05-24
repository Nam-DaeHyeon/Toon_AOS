using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Private UI")]
    [SerializeField] UI_SkillSlot[] _skillSlots;
    [SerializeField] UI_SkillDesc _skillDesc;
    public Canvas UI_WorldCvs;


    /// <summary>
    /// 스킬 공격 시, 공격 범위를 가시화 해주기 위한 라인렌더러 오브젝트
    /// </summary>
    GameObject lineObj;
    int radiovector = 5;
    float radioangle = 45f;

    #region 스킬 관련
    /// <summary>
    /// 캐릭터별 스킬을 등록합니다.
    /// </summary>
    private void SetInit_MySkillSet()
    {
        switch (GameManager.USER_CHARACTER)
        {
            default: //BEAR
                _skillSlots[0].SetInit_Skill(_skillSlots[0].gameObject.AddComponent<sk_Bear01_Bash>());
                _skillSlots[1].SetInit_Skill(_skillSlots[1].gameObject.AddComponent<sk_Bear02_Dash>());
                _skillSlots[2].SetInit_Skill(_skillSlots[2].gameObject.AddComponent<sk_Bear03_Guard>());
                _skillSlots[3].SetInit_Skill(_skillSlots[3].gameObject.AddComponent<sk_Bear04_Frenzy>());
                break;
            case "RABBIT":
                _skillSlots[0].SetInit_Skill(_skillSlots[0].gameObject.AddComponent<sk_Rabbit01_IceArrow>());
                _skillSlots[1].SetInit_Skill(_skillSlots[1].gameObject.AddComponent<sk_Rabbit02_Heal>());
                _skillSlots[2].SetInit_Skill(_skillSlots[2].gameObject.AddComponent<sk_Rabbit03_Barrier>());
                _skillSlots[3].SetInit_Skill(_skillSlots[3].gameObject.AddComponent<sk_Rabbit04_Blizard>());
                break;
            case "CHIPMUNK":
                _skillSlots[0].SetInit_Skill(_skillSlots[0].gameObject.AddComponent<sk_Chipmunk01_Stab>());
                _skillSlots[1].SetInit_Skill(_skillSlots[1].gameObject.AddComponent<sk_Chipmunk02_Hiding>());
                _skillSlots[2].SetInit_Skill(_skillSlots[2].gameObject.AddComponent<sk_Chipmunk03_Leap>());
                _skillSlots[3].SetInit_Skill(_skillSlots[3].gameObject.AddComponent<sk_Chipmunk04_GravityBoom>());
                break;
            case "CAT":
                _skillSlots[0].SetInit_Skill(_skillSlots[0].gameObject.AddComponent<sk_Cat01_PoisonArrow>());
                _skillSlots[1].SetInit_Skill(_skillSlots[1].gameObject.AddComponent<sk_Cat02_Hallucination>());
                _skillSlots[2].SetInit_Skill(_skillSlots[2].gameObject.AddComponent<sk_Cat03_Trap>());
                _skillSlots[3].SetInit_Skill(_skillSlots[3].gameObject.AddComponent<sk_Cat04_Snipe>());
                break;
        }
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
        for(int i =0;i <_skillSlots.Length;i++)
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
    private void Set_InitLineRendererObj()
    {
        lineObj = MainManager.instance.lineObjSample;
        lineObj.transform.parent = transform;
        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localEulerAngles = Vector3.zero;
        lineObj.SetActive(false);

        //Sample Test...
        //StartCoroutine(IE_TEST_TargetingRender());
    }

    /// <summary>
    /// 라인 렌더러에 사용되는 파라미터를 재설정합니다.
    /// </summary>
    /// <param name="range">라인 렌더러 길이</param>
    /// <param name="angle">라인 렌더러 생성 각도.</param>
    public void SetParam_LineRender(int range, int angle)
    {
        radiovector = range;
        radioangle = angle;
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
        lineObj.SetActive(false);
    }

    #region Test SkillArea Render
    IEnumerator IE_TEST_TargetingRender()
    {
        //부채꼴 렌더
        LineRenderer line = lineObj.GetComponent<LineRenderer>();
        if (line == null) line = lineObj.AddComponent<LineRenderer>();

        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Escape));

        lineObj.transform.localPosition = Vector3.zero;
        lineObj.transform.localEulerAngles = Vector3.zero;

        Vector3[] linePos = new Vector3[(int)radioangle + 2];
        //linePos[0] = transform.position;
        linePos[0] = Vector3.zero;
        //linePos[(int)radioangle + 1] = transform.position;
        linePos[(int)radioangle + 1] = Vector3.zero;
        lineObj.transform.Rotate(-Vector3.up * radioangle * 0.5f, Space.Self);
        GameObject tempTr = new GameObject();
        tempTr.transform.parent = lineObj.transform;
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
        LineRenderer line = lineObj.GetComponent<LineRenderer>();
        if (line == null) line = lineObj.AddComponent<LineRenderer>();

        lineObj.transform.localEulerAngles = Vector3.zero;

        Vector3[] linePos = null;

        //Draw LineRenderer
        if (radioangle == 0)
        {
            linePos = new Vector3[2];
            linePos[0] = Vector3.zero;
            linePos[1] = linePos[0] + lineObj.transform.forward * radiovector;
        }
        else
        {
            linePos = new Vector3[(int)radioangle + 2];
            //linePos[0] = transform.position;
            linePos[0] = Vector3.zero;
            //linePos[(int)radioangle + 1] = transform.position;
            linePos[(int)radioangle + 1] = Vector3.zero;
            lineObj.transform.Rotate(-Vector3.up * radioangle * 0.5f, Space.Self);
            GameObject tempTr = new GameObject();
            tempTr.transform.parent = lineObj.transform;
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
        lineObj.SetActive(true);

        //Trace Mouse Cursor
        while(lineObj.activeInHierarchy)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Default")))
            {
                lineObj.transform.LookAt(new Vector3(hit.point.x, transform.position.y, hit.point.z));
            }

            yield return null;
        }
    }
}

