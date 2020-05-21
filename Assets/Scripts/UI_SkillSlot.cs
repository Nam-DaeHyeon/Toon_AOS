using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_SkillSlot : MonoBehaviour
{
    [SerializeField] Player _player;
    Skill setSkill { set; get; }

    float _leftTime;

    [SerializeField] Image _imgCoolTime;
    [SerializeField] Image _imgSkillIcon;
    [SerializeField] Image _imgLevelBar;
    [SerializeField] Button _btnLvUP;

    /// <summary>
    /// 스킬을 등록합니다.
    /// </summary>
    public void SetInit_Skill(Skill newSkill)
    {
        //스킬값 저장
        setSkill = newSkill;

        //스킬 아이콘 이미지 변경
        _imgSkillIcon.sprite = newSkill.skillImage;

        //기타 초기화
        _imgLevelBar.fillAmount = 0;
        _imgCoolTime.fillAmount = 0;
        _imgLevelBar.fillAmount = 0;
        _btnLvUP.gameObject.SetActive(false);
    }

    /// <summary>
    /// 스킬을 사용합니다.
    /// </summary>
    public void SetUse_Skill(KeyCode inputCode)
    {
        if (_leftTime > 0) return;

        //스킬이 즉시 시전되는지에 따라 스킬을 발동합니다.
        if (setSkill.directPop) Callback_UseSkill();
        else StartCoroutine(IE_Wait_KeyCodeUp(inputCode));
    }

    private void Callback_UseSkill()
    {
        //쿨타임 초기화
        StartCoroutine(IE_SlotTimer());

        //스킬 발동
        setSkill.Use_Skill();
    }

    private IEnumerator IE_Wait_KeyCodeUp(KeyCode inputCode)
    {
        yield return new WaitUntil(() => Input.GetKeyUp(inputCode));

        Callback_UseSkill();
    }

    /// <summary>
    /// 스킬 설명을 표시합니다.
    /// </summary>
    public void UI_PointerEnter_ShowSkillDesc()
    {
        _player.ShowUp_SkillDesc(transform.position, setSkill.Get_SkillName(), setSkill.Get_SkillDesc(), setSkill.skillLevel);
    }

    /// <summary>
    /// 스킬 설명을 끕니다.
    /// </summary>
    public void UI_PointerExit_HideSkillDesc()
    {
        _player.Hide_SkillDesc();
    }

    /// <summary>
    /// 스킬 쿨타임 코루틴
    /// </summary>
    IEnumerator IE_SlotTimer()
    {
        _leftTime = setSkill.coolTime;

        while (_leftTime > 0)
        {
            _imgCoolTime.fillAmount = _leftTime / setSkill.coolTime;
            _leftTime -= Time.deltaTime;

            yield return null;
        }
    }

    /// <summary>
    /// 해당 슬롯의 스킬의 레벨을 올립니다.
    /// </summary>
    public void UI_Button_SkillLevelUP()
    {
        //스킬 포인트가 없을 경우 리턴
        if (_player._skillPoint <= 0) return;
        _player._skillPoint--;

        //스킬 포인트가 없을 경우 모든 버튼 비활성화
        if (_player._skillPoint <= 0) _player.Hide_SkillLvUPBtns();

        setSkill.skillLevel++;
        _imgLevelBar.fillAmount = (float)setSkill.skillLevel / (float)setSkill.Get_MaxLevel();
    }

    /// <summary>
    /// 스킬 레벨업 버튼을 활성화합니다.
    /// </summary>
    public void Show_LevelUPButton()
    {
        _btnLvUP.gameObject.SetActive(true);
    }

    /// <summary>
    /// 스킬 레벨업 버튼을 비활성합니다.
    /// </summary>
    public void Hide_LevelUPButton()
    {
        _btnLvUP.gameObject.SetActive(false);
    }
}
