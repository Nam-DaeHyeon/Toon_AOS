using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Reflection;

public class ItemViewer : MonoBehaviour
{
    //아이템 검색 창
    [SerializeField] TMP_InputField _searchField;

    //통합 아이템 뷰어
    [SerializeField] GameObject _totalSlotRoot;
    //전체 아이템 탐색 뷰어에 배치된 아이템 슬롯 버튼 집합
    Button[] _totalSlots;

    //세부 아이템 뷰어
    [SerializeField] GameObject _detailSlotRoot;

    //부모 노드 아이템 슬롯 버튼 집합 (해당 아이템을 하위 재료로 두는 아이템들의 집합)
    [SerializeField] Button[] _parentSlots;
    //자식 노드 아이템 슬롯 버튼 집합 트리 (해당 아이템을 포함한 하위 재료 아이템들의 집합 트리)
    [SerializeField] Button[] _childSlots;

    //카테고리 토글 활성화 배열
    //공격력 | 마법공격력 | 방어력 | 마법방어력 | 체력 | 이동속도 | 소모품
    [SerializeField] Toggle[] _categoryToggles;

    private void Awake()
    {
        SetInit_TotalSlots();
    }

    // Start is called before the first frame update
    void Start()
    {
        Set_TotalViewer();
    }
    
    private void SetInit_TotalSlots()
    {
        _totalSlots = _totalSlotRoot.GetComponentsInChildren<Button>();

        var array = Get_AllItemTypes();
        Update_TotalSlot(array);
    }

    private void Update_TotalSlot(ItemBase[] sortArray)
    {
        for (int i = 0; i < _totalSlots.Length; i++)
        {
            if (i >= sortArray.Length)
            {
                _totalSlots[i].interactable = false;
                _totalSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                continue;
            }

            //Debug.Log(i + ": " + array[i].GetType().Name);
            _totalSlots[i].interactable = true;
            string typeName = sortArray[i].GetType().ToString();
            _totalSlots[i].GetComponentInChildren<TMP_Text>().text = typeName.Substring(typeName.LastIndexOf('_') + 1);
        }
    }

    //모든 아이템들의 목록을 확인할 수 있는 창을 엽니다. (+검색 기능)
    private void Set_TotalViewer()
    {
        //카테고리 토글을 초기화합니다.
        for (int i = 0; i < _categoryToggles.Length; i++)
        {
            _categoryToggles[i].isOn = false;
        }

        //검색창 초기화
        _searchField.text = "";

        //뷰어 전환 : 전체 목록 뷰어 활성화
        _totalSlotRoot.SetActive(true);
        _detailSlotRoot.SetActive(false);
    }

    //특정 아이템의 자식 트리 및 부모 목록을 확인할 수 있는 창을 엽니다.
    private void Set_DetailViewer(string itemName)
    {
        //자식 & 부모 아이템 탐색 
        //탐색한 아이템 목록들을 뷰어에 시각적으로 표현
        Set_ChildTreeViewer(itemName);
        Set_ParentArrayViewer(itemName);

        //뷰어 전환
        _totalSlotRoot.SetActive(false);
        _detailSlotRoot.SetActive(true);
    }

    /// <summary>
    /// 특정 아이템을 만드는 데 필요한 아이템들을 트리구조로 나타냅니다.
    /// </summary>
    /// <param name="itemName">아이템 이름</param>
    private void Set_ChildTreeViewer(string itemName)
    {
        Debug.Log("[Child Tree Input Item] " + itemName);

        //이전에 생성해둔 버튼 인스턴스 제거
        for (int i = 1; i < _childSlots.Length; i++)
        {
            Destroy(_childSlots[i].gameObject);
        }
        Array.Resize(ref _childSlots, 1);

        Type root = Type.GetType(itemName);
        ItemBase[] list = GetTree_ChildTypes(ItemManager.ItemDB[itemName]);
        if (list == null)
        {
            _childSlots[0].gameObject.SetActive(false);
            return;
        }

        _childSlots[0].gameObject.SetActive(true);
        Array.Resize(ref _childSlots, list.Length);
        int[] newDepthIdxs = { 0, 1, 3, 7, 15, 31 };
        int currD = 1;
        for (int i = 1; i < _childSlots.Length; i++)
        {
            _childSlots[i] = Instantiate(_childSlots[0], _childSlots[0].transform.parent);

            //새로운 뎁스가 추가되었습니다.
            if(newDepthIdxs[currD] == i)
            {
                RectTransform pivot = _childSlots[newDepthIdxs[currD - 1]].GetComponent<RectTransform>();
                currD++;

                _childSlots[i].transform.localPosition = new Vector2(pivot.localPosition.x + pivot.sizeDelta.x * -0.5f * (currD - 1), pivot.localPosition.y + pivot.sizeDelta.y * -1.5f); 
            }
            else
            {
                RectTransform prevSlot = _childSlots[i - 1].GetComponent<RectTransform>();
                //_childSlots[i].transform.Translate(new Vector2(prevSlot.sizeDelta.x, 0), Space.Self);
                _childSlots[i].transform.localPosition = new Vector2(prevSlot.localPosition.x + prevSlot.sizeDelta.x, prevSlot.localPosition.y);
            }
        }

        for (int i = 0; i < _childSlots.Length; i++)
        {
            //디버깅
            //if (list[i] == null) Debug.Log("child is null");
            //else Debug.Log("child " + i + ": " + list[i].GetType().ToString());

            if (list[i] == null)
            {
                _childSlots[i].gameObject.SetActive(false);
                continue;
            }
            string tempName = list[i].GetType().ToString();
            tempName = tempName.Substring(tempName.LastIndexOf('_') + 1);
            _childSlots[i].GetComponentInChildren<TMP_Text>().text = tempName;
        }
    }

    /// <summary>
    /// 특정 아이템을 자식으로 두는 아이템들을 배열로 나타냅니다.
    /// </summary>
    /// <param name="itemName"></param>
    private void Set_ParentArrayViewer(string itemName)
    {
        Debug.Log("[Parent Input Item] " + itemName);

        //이전에 생성해둔 버튼 인스턴스 제거
        for(int i = 1; i < _parentSlots.Length; i++)
        {
            Destroy(_parentSlots[i].gameObject);
        }
        Array.Resize(ref _parentSlots, 1);

        Type root = Type.GetType(itemName);
        ItemBase[] list = Get_ParentType(ItemManager.ItemDB[itemName]);
        //부모가 없을 경우 
        if (list == null)
        {
            _parentSlots[0].gameObject.SetActive(false);
            return;
        }

        //추가적인 부모 버튼 인스턴스 생성
        _parentSlots[0].gameObject.SetActive(true);
        //_parentSlots = new Button[list.Length];
        Array.Resize(ref _parentSlots, list.Length);
        for (int i = 1; i < _parentSlots.Length; i++)
        {
            _parentSlots[i] = Instantiate(_parentSlots[0], _parentSlots[0].transform.parent);

            //그리드 레이아웃으로 좌표 보정
        }

        //부모 버튼 풀에 부모 노드 값 저장
        for (int i = 0; i < _parentSlots.Length; i++)
        {
            //디버깅
            //Debug.Log("parent " + i + ": " + list[i].GetType().ToString());

            string tempName = list[i].GetType().ToString();
            tempName = tempName.Substring(tempName.LastIndexOf('_') + 1);
            _parentSlots[i].GetComponentInChildren<TMP_Text>().text = tempName;
        }
    }

    /// <summary>
    /// 모든 아이템 클래스들을 배열로 반환합니다.
    /// </summary>
    private ItemBase[] Get_AllItemTypes()
    {
        //특정 클래스를 상속하는 자식 타입을 찾고, 다시 인스턴스화하는 과정이 매우 무겁기에 ItemManager를 통해 미리 생성한 DB 해쉬를 이용한다.
        return new List<ItemBase>(ItemManager.ItemDB.Values).ToArray();
    }

    /// <summary>
    /// 특정 아이템의 하위 아이템 타입들을 이진트리 배열로 반환합니다.
    /// </summary>
    /// <param name="parentType">부모로 두는 아이템 타입</param>
    private ItemBase[] GetTree_ChildTypes(ItemBase parentType)
    {
        Item_BST bst = new Item_BST();
        bst.ChildNodeTraversal(parentType);

        return bst.Get_Array();
    }

    /// <summary>
    /// 특정 아이템을 자식으로 두는 부모 아이템 타입들을 배열로 반환합니다.
    /// </summary>
    /// <param name="targetType">자식으로 두는 아이템 타입</param>
    private ItemBase[] Get_ParentType(ItemBase targetType)
    {
        //return targetType.Get_ParentNode();

        Item_BST bst = new Item_BST();
        bst.ParentNodeTraversal(targetType);

        return bst.Get_Array();
    }

    #region UI Section
    //검색어 및 뷰어 창을 초기화합니다.
    public void UI_ButtonClick_SearchReset()
    {
        Set_TotalViewer();
    }

    //뷰어 창을 닫습니다.
    public void UI_ButtonClick_ViewerQuit()
    {
        gameObject.SetActive(false);
    }
    
    //전체 항목 뷰어에서 아이템을 클릭했습니다.
    public void UI_ButtonClick_ItemSlotInTotal(int btnIdx)
    {
        string itemName = _totalSlots[btnIdx].GetComponentInChildren<TMP_Text>().text;

        Set_DetailViewer(itemName);
    }

    //아이템 세부 뷰어에서 트리에 표기된 아이템을 클릭했습니다.
    public void UI_ButtonClick_ItemSlotInTree(TMP_Text btnAttr)
    {
        Set_DetailViewer(btnAttr.text);
    }

    //아이템 세부 뷰어에서 부모 배열에 표기된 아이템을 클릭했습니다.
    public void UI_ButtonClick_ItemSlotInParentArray(TMP_Text btnAttr)
    {
        Set_DetailViewer(btnAttr.text);
    }

    //아이템을 검색합니다.
    public void UI_InputFieldOnChanged_SearchItem()
    {
        string inputText = _searchField.text;
        if(inputText.Trim().Length == 0)
        {
            Update_TotalSlot(Get_AllItemTypes());
            return;
        }

        var liq = from node in ItemManager.ItemDB
                  where node.Key.Contains(inputText)
                  select node.Value;

        Update_TotalSlot(liq.ToArray());
    }

    //종류별로 아이템을 나열하기 위한 항목을 선택합니다.
    public void UI_ToggleOnChanged_CheckCategory(int toggleIdx)
    {
        List<ItemCategory> checkedCate = new List<ItemCategory>();
        var cateArr = Enum.GetValues(typeof(ItemCategory)).Cast<ItemCategory>().ToList();
        for (int i = 0; i < _categoryToggles.Length; i++)
        {
            if (_categoryToggles[i].isOn) checkedCate.Add(cateArr[i]);
        }

        //모든 토글이 꺼져있다면, 모든 아이템 항목 정렬
        if (checkedCate.Count == 0)
        {
            Update_TotalSlot(Get_AllItemTypes());
            return;
        }

        //특정 토글이 클릭되있다면, 해당 조건으로 아이템 정렬
        

        var liqq = from item in ItemManager.ItemDB

                   //where item.Value.CheckLINQ_Category((ItemCategory)Enum.Parse(typeof(ItemCategory), clickNode))
                   where item.Value.CheckLINQ_Category(checkedCate)
                   select item.Value;

        foreach(var node in liqq.ToArray())
        {
            Debug.Log("Sorted Item List : " + node);
        }

        Update_TotalSlot(liqq.ToArray());
    }

    #endregion
}
