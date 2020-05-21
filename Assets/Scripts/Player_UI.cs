using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Private UI")]
    [SerializeField] UI_SkillSlot[] _skillSlots;
    [SerializeField] UI_SkillDesc _skillDesc;
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
}

