using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Photon.Pun;

public class ItemViewer : MonoBehaviour
{
    Player _owner;

    //플레이어 개인 능력치 및 인벤토리
    [SerializeField] GameObject _playerSpecRoot;
    TMP_Text[] _playerSpec;
    TMP_Text[] _playerItems;
    TMP_Text _playerMoney;
    Image _playerHPBar;
    TMP_Text _playerHPText;

    //아이템 검색 창
    [SerializeField] TMP_InputField _searchField;

    //상점 창
    GameObject _storeWindow;

    //통합 아이템 뷰어
    [SerializeField] GameObject _totalSlotRoot;
    //전체 아이템 탐색 뷰어에 배치된 아이템 슬롯 버튼 집합
    Button[] _totalSlots;

    //세부 아이템 뷰어
    [SerializeField] GameObject _detailSlotRoot;

    //클릭한 아이템 정보 뷰어
    [SerializeField] GameObject _clickedItemSpecRoot;
    TMP_Text _clickedItemSpec;
    TMP_Text _clickedItemButton;    //구매 및 판매 버튼

    //상점 인벤토리
    [SerializeField] GameObject _invenSlotStoreRoot;
    TMP_Text[] _playerItemsInStore;
    [SerializeField] TMP_Text _playerMoneyInStore;

    //부모 노드 아이템 슬롯 버튼 집합 (해당 아이템을 하위 재료로 두는 아이템들의 집합)
    [SerializeField] Button[] _parentSlots;
    //자식 노드 아이템 슬롯 버튼 집합 트리 (해당 아이템을 포함한 하위 재료 아이템들의 집합 트리)
    [SerializeField] Button[] _childSlots;

    //카테고리 토글 활성화 배열
    //공격력 | 마법공격력 | 방어력 | 마법방어력 | 체력 | 이동속도 | 소모품
    [SerializeField] Toggle[] _categoryToggles;

    //이진트리 시작 인덱스 모음
    int[] newDepthIdxs = { 0, 1, 3, 7, 15, 31 };

    /// <summary>
    /// 하위 아이템 중 보유하고 있는 아이템이 위치한 인벤토리 인덱스 모음 리스트
    /// </summary>
    List<int> _carriedUndergradeItemList = new List<int>();

    public void SetInitAddr()
    { 
        _owner = MainManager.instance.owner;
        //SetInit_OwnerPlayer();

        _storeWindow = transform.GetChild(1).gameObject;

        SetInit_PlayerSpecNInventory();
        SetInit_TotalSlots();
        SetInit_ClickSpecs();

        Update_Money();
    }

    /// <summary>
    /// [최우선 등록] 스크립트 종속 플레이어 등록
    /// </summary>
    private void SetInit_OwnerPlayer()
    {
        Player[] players = FindObjectsOfType<Player>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].GetComponent<PhotonView>().IsMine)
            {
                _owner = players[i];

                return;
            }
        }
    }

    /// <summary>
    /// [초기 등록] 플레이어 능력치와 인벤토리 주소를 탐색/등록합니다.
    /// </summary>
    private void SetInit_PlayerSpecNInventory()
    {
        TMP_Text[] temp = _playerSpecRoot.GetComponentsInChildren<TMP_Text>();

        _playerSpec = new TMP_Text[5];
        _playerItems = new TMP_Text[temp.Length - 5];
        for (int i = 0; i < temp.Length; i++)
        {
            //공격력 | 마법공격력 | 방어력 | 마법방어력 | 이동속도
            if( i < 5)
            {
                _playerSpec[i] = temp[i];
            }
            //체력바 (텍스트)
            else if(i == temp.Length - 2)
            {
                _playerHPText = temp[i];
                _playerHPBar = _playerHPText.transform.parent.GetComponent<Image>();
            }
            //보유 재화
            else if(i==temp.Length - 1)
            {
                _playerMoney = temp[i];
            }
            else
            {
                _playerItems[i - 5] = temp[i];
            }
        }

        Update_PlayerSpec();
    }

    /// <summary>
    /// [초기 등록] 전체 아이템 항목 뷰어에 있는 아이템 버튼들의 주소를 탐색/등록합니다.
    /// </summary>
    private void SetInit_TotalSlots()
    {
        _totalSlots = _totalSlotRoot.GetComponentsInChildren<Button>();

        var array = Get_AllItemTypes();
        Update_TotalSlot(array);

        //플레이어 인벤토리(상점) UI 컴포넌트 등록
        _playerItemsInStore = _invenSlotStoreRoot.GetComponentsInChildren<TMP_Text>();

        Set_TotalViewer();
    }

    /// <summary>
    /// [초기 등록] 선택한 아이템을 표시하기 위한 설명란 컴포넌트와 구매 버튼 컴포넌트를 탐색/등록합니다.
    /// </summary>
    private void SetInit_ClickSpecs()
    {
        TMP_Text[] children = _clickedItemSpecRoot.transform.GetComponentsInChildren<TMP_Text>();
        _clickedItemSpec = children[0];
        _clickedItemButton = children[1];
    }

    /// <summary>
    /// 해당 설정값으로 전체 아이템 항목을 정렬/갱신합니다.
    /// </summary>
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

    /// <summary>
    /// 플레이어 능력치 정보를 갱신합니다.
    /// </summary>
    public void Update_PlayerSpec()
    {
        //공격력 | 마법공격력 | 방어력 | 마법방어력 | 이동속도
        _playerSpec[0].text = "공격력 " + _owner.Get_Spec(ItemCategory.공격력).ToString();
        _playerSpec[1].text = "마법공격력 " + _owner.Get_Spec(ItemCategory.마법공격력).ToString();
        _playerSpec[2].text = "방어력 " + _owner.Get_Spec(ItemCategory.방어력).ToString();
        _playerSpec[3].text = "마법방어력 " + _owner.Get_Spec(ItemCategory.마법방어력).ToString();
        _playerSpec[4].text = "이동속도 " + _owner.Get_Spec(ItemCategory.이동속도).ToString();

        //Update_PlayerSpecHp();
    }

    public void Update_PlayerSpecHp()
    {
        _playerHPBar.fillAmount = _owner.Get_CurrentHP() / _owner.Get_MaxHP();
        _playerHPText.text = _owner.Get_CurrentHP() + "/" + _owner.Get_MaxHP();
        //_owner.TakeHeal(0);
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
        //소모품일 경우 중첩으로 쌓인 경우 DB 탐색에 불필요한 키값이 포함되어 있으므로 제외한다.
        if(itemName.Contains('×'))
        {
            itemName = itemName.Substring(0, itemName.IndexOf('×'));
        }

        //선택한 아이템 스펙 정보 표시
        _clickedItemSpec.text = itemName + "\n";
        ItemBase item = ItemManager.ItemDB[itemName];
        for (int i = 0; i < item.specs.Count; i++)
        {
            _clickedItemSpec.text += item.specs[i].category + " + " + item.specs[i].value + "\n";
        }

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

    //뷰어를 활성화합니다.
    public void UI_ButtonClick_ViewerOpen()
    {
        if (_storeWindow.activeInHierarchy) _storeWindow.SetActive(false);
        //transform.GetChild(1).gameObject.SetActive(true);
        _storeWindow.SetActive(true);
    }

    //뷰어 창을 닫습니다.
    public void UI_ButtonClick_ViewerQuit()
    {
        _storeWindow.SetActive(false);
    }

    /// <summary>
    /// 전체 항목 뷰어에서 아이템을 클릭했습니다.
    /// </summary>
    public void UI_ButtonClick_ItemSlotInTotal(int btnIdx)
    {
        string itemName = _totalSlots[btnIdx].GetComponentInChildren<TMP_Text>().text;

        //하위 아이템 보유 여부에 따라 최종 구매 비용 측정
        int cost = Calculate_UndergradeCost(itemName);

        Update_PurchaseButton(true, cost);
        Set_DetailViewer(itemName);
    }

    /// <summary>
    /// 아이템 세부 뷰어에서 트리에 표기된 아이템을 클릭했습니다.
    /// </summary>
    public void UI_ButtonClick_ItemSlotInTree(TMP_Text btnAttr)
    {
        if (btnAttr.text.Trim().Equals("") || btnAttr.text == null) return;

        //하위 아이템 보유 여부에 따라 최종 구매 비용 측정
        int cost = Calculate_UndergradeCost(btnAttr.text);

        Update_PurchaseButton(true, cost);
        Set_DetailViewer(btnAttr.text);
    }

    /// <summary>
    /// 아이템 세부 뷰어에서 부모 배열에 표기된 아이템을 클릭했습니다.
    /// </summary>
    public void UI_ButtonClick_ItemSlotInParentArray(TMP_Text btnAttr)
    {
        if (btnAttr.text.Trim().Equals("") || btnAttr.text == null) return;

        //하위 아이템 보유 여부에 따라 최종 구매 비용 측정
        int cost = Calculate_UndergradeCost(btnAttr.text);

        Update_PurchaseButton(true, cost);
        Set_DetailViewer(btnAttr.text);
    }

    /// <summary>
    /// 상점 UI에 있는 인벤토리의 아이템을 클릭했습니다.
    /// 아이템을 팔 수 있습니다.
    /// </summary>
    public void UI_ButtonClick_ItemSlotInInventory(TMP_Text btnAttr)
    {
        string itemName = btnAttr.text;
        if (itemName.Trim().Equals("") || itemName == null) return;

        //소모품인지 확인
        if (itemName.Contains('×')) itemName = itemName.Substring(0, itemName.IndexOf('×'));

        //판매는 해당 아이템이 원 구매비용의 절반으로 측정한다.
        int cost = (int)(ItemManager.ItemDB[itemName].cost * 0.5f);

        Update_PurchaseButton(false, cost);
        Set_DetailViewer(itemName);
    }

    /// <summary>
    /// 클릭한 아이템의 상호작용 버튼 속성을 구매(true)로 설정합니다.
    /// </summary>
    /// <param name="purchase">구매 true : 판매 false</param>
    /// <param name="cost">최종 구매/판매 비용</param>
    private void Update_PurchaseButton(bool purchase, int cost)
    {
        //보유 아이템을 클릭했으니 판매 기능을 수행하도록한다.
        _clickedItemButton.text = purchase ? "구매" : "판매";

        _clickedItemButton.text += " " + cost;

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
                   
                   where item.Value.CheckLINQ_Category(checkedCate)
                   select item.Value;

        Update_TotalSlot(liqq.ToArray());
    }

    //선택한 아이템의 보유 여부에 따라 구매 / 판매를 합니다.
    public void UI_ButtonClick_PurchaseItem()
    {
        int cost = 0;

        string costStr = _clickedItemButton.text;
        costStr = costStr.Replace(costStr.Substring(0, 2), "");

        cost = int.Parse(costStr.Trim());

        //구매 옵션일 때 보유 재화량이 부족하면 리턴
        if (_clickedItemButton.text.Contains("구매"))
        {
            if (_owner.money < cost) return;
        }
            

        //선택한 아이템 이름 호출
        string itemName = _childSlots[0].GetComponentInChildren<TMP_Text>().text;

        //소모품 여부 확인
        if(ItemManager.ItemDB[itemName].Check_IsConsumables())
        {
            PurchaseItem_Consumables(itemName, cost);
            return;
        }

        if (_clickedItemButton.text.Contains("구매"))
        {
            //하위 아이템이 존재할 경우 아이템을 삭제한다.
            for(int i = 0; i < _carriedUndergradeItemList.Count; i++)
            {
                if (_carriedUndergradeItemList[i] == -1) continue;
                _owner.RemoveItem_Inventory(_carriedUndergradeItemList[i]);
                _playerItems[_carriedUndergradeItemList[i]].text = _owner.inventory[i]; //상시 스펙 뷰어 인벤토리 UI
                _playerItemsInStore[_carriedUndergradeItemList[i]].text = _playerItems[_carriedUndergradeItemList[i]].text; //상점 인벤토리 UI
            }

            for(int i = 0; i< _owner.inventory.Length; i++)
            {
                //빈 슬롯에 아이템 추가
                if(_owner.inventory[i] == "")
                {
                    //구매완료 : 슬롯에 추가
                    //_owner.inventory[i] = itemName;   //플레이어 인벤토리 데이터
                    _owner.AddItem_Inventory(i, itemName);   //플레이어 인벤토리 데이터
                    _playerItems[i].text = _owner.inventory[i]; //상시 스펙 뷰어 인벤토리 UI
                    _playerItemsInStore[i].text = _playerItems[i].text; //상점 인벤토리 UI
                    
                    break;
                }
            }

            //_owner.money -= cost;
            _owner.Add_Money(-cost);
        }
        else if (_clickedItemButton.text.Contains("판매"))
        {
            for (int i = 0; i < _owner.inventory.Length; i++)
            {
                //해당 슬롯 아이템 제거
                if (_owner.inventory[i] == itemName)
                {
                    //판매완료 : 슬롯에서 제거
                    //_owner.inventory[i] = "";   //플레이어 인벤토리 데이터
                    _owner.RemoveItem_Inventory(i);   //플레이어 인벤토리 데이터
                    _playerItems[i].text = _owner.inventory[i]; //상시 스펙 뷰어 인벤토리 UI
                    _playerItemsInStore[i].text = _playerItems[i].text; //상점 인벤토리 UI

                    //버튼 속성 변환
                    Update_PurchaseButton(true, ItemManager.ItemDB[itemName].cost);

                    break;
                }
            }

            //_owner.money += cost;
            _owner.Add_Money(cost);
        }

        //플레이어 능력치 갱신
        Update_PlayerSpec();
        Update_PlayerSpecHp();

        //하위 아이템 보유 관련해서 비용 재계산한다. 그리고 버튼 정보를 수정한다.
        cost = Calculate_UndergradeCost(itemName);
        Update_PurchaseButton(true, cost);
    }

    private int Calculate_UndergradeCost(string itemName)
    {
        _carriedUndergradeItemList.Clear();
        int cost = Get_ItemCost(itemName);

        return cost;
    }

    private void PurchaseItem_Consumables(string itemName, int cost)
    {
        if (_clickedItemButton.text.Contains("구매"))
        {
            AddCount_Consumables(itemName);
            //_owner.money -= cost;
            _owner.Add_Money(-cost);
        }
        else if (_clickedItemButton.text.Contains("판매"))
        {
            SubstractCount_Consumables(itemName);
            //_owner.money += cost;
            _owner.Add_Money(cost);
        }
    }

    /// <summary>
    /// 소모품을 인벤토리에 추가합니다. 보유 여부에 따라 1개씩 더합니다.
    /// </summary>
    private void AddCount_Consumables(string itemName)
    {
        //기존에 있던 슬롯에 아이템 중첩 추가
        if (_owner.inventory.Contains(itemName))
        {
            for (int i = 0; i < _playerItems.Length; i++)
            {
                if (_playerItems[i].text.Contains(itemName))
                {
                    //기존 1개 보유했을 경우
                    if (!_playerItems[i].text.Contains('×')) _playerItems[i].text += "×" + 2;
                    else
                    {
                        int idx = _playerItems[i].text.IndexOf('×');
                        int amount = int.Parse(_playerItems[i].text.Substring(idx + 1));

                        _playerItems[i].text = _playerItems[i].text.Substring(0, idx) + "×" + (amount + 1);
                    }

                    _playerItemsInStore[i].text = _playerItems[i].text; //상점 인벤토리 UI

                    break;
                }
            }
        }
        //빈 슬롯에 아이템 새로 추가
        else
        {
            for (int i = 0; i < _owner.inventory.Length; i++)
            {
                //빈 슬롯에 아이템 추가
                if (_owner.inventory[i] == "")
                {
                    //구매완료 : 슬롯에 추가
                    //_owner.inventory[i] = itemName;   //플레이어 인벤토리 데이터
                    _owner.AddItem_Inventory(i, itemName);   //플레이어 인벤토리 데이터
                    _playerItems[i].text = _owner.inventory[i]; //상시 스펙 뷰어 인벤토리 UI
                    _playerItemsInStore[i].text = _playerItems[i].text; //상점 인벤토리 UI

                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// 소모품을 인벤토리에서 뺍니다. 보유 여부에 따라 1개씩 차감합니다.
    /// </summary>
    private void SubstractCount_Consumables(string itemName)
    {
        for (int i = 0; i < _owner.inventory.Length; i++)
        {
            //해당 슬롯 아이템 제거
            if (_owner.inventory[i] == itemName)
            {
                //기존 1개 보유했을 경우
                if (!_playerItems[i].text.Contains('×'))
                {
                    //판매완료 : 슬롯에서 제거
                    //_owner.inventory[i] = "";   //플레이어 인벤토리 데이터
                    _owner.RemoveItem_Inventory(i);   //플레이어 인벤토리 데이터
                    _playerItems[i].text = _owner.inventory[i]; //상시 스펙 뷰어 인벤토리 UI

                    //버튼 속성 변환
                    Update_PurchaseButton(true, ItemManager.ItemDB[itemName].cost);
                }
                else
                {
                    int idx = _playerItems[i].text.IndexOf('×');
                    int amount = int.Parse(_playerItems[i].text.Substring(idx + 1));

                    _playerItems[i].text = _playerItems[i].text.Substring(0, idx);
                    if (amount != 2) _playerItems[i].text += "×" + (amount - 1);

                }

                _playerItemsInStore[i].text = _playerItems[i].text; //상점 인벤토리 UI

                break;
            }
        }
    }

    public void UseItem(int index)
    {
        string itemName = _owner.inventory[index];
        ItemBase item = ItemManager.ItemDB[itemName];
        if (!item.Check_IsConsumables()) return;

        //소모품 능력 적용
        item.Use_Consumables(_owner);

        //아이템 사용으로 인한 보유 개수 감소
        SubstractCount_Consumables(itemName);
    }

    /// <summary>
    /// 플레이어의 보유 재화를 UI에 동기화 표시합니다.
    /// </summary>
    public void Update_Money()
    {
        if (_owner == null) return;
        _playerMoney.text = "Gold " + _owner.money.ToString();
        _playerMoneyInStore.text = "Gold " + _owner.money.ToString();
    }


    #endregion

    /// <summary>
    /// 해당 아이템의 최종 구매 비용을 반환합니다. 하위 아이템의 보유 여부를 확인하고 구매비용을 차감합니다.
    /// </summary>
    private int Get_ItemCost(string itemName)
    {
        ItemBase item = ItemManager.ItemDB[itemName];
        int endCost = item.cost;

        //위 아이템의 보유 여부를 확인하고 구매비용을 차감합니다.
        if (item.Get_ChildNode(0) != null)
        {
            Item_BST bst = new Item_BST();
            bst.ChildNodeTraversal(item);
            ItemBase[] arr = bst.Get_Array();

            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i] == null)
                {
                    //자식 노드들 NULL 값으로 변환
                    SetNull_NearChildNode(ref bst, i);

                    continue;
                }

                if (_owner.inventory.Contains(arr[i].Get_ItemName()))
                {
                    endCost -= arr[i].cost;

                    //자식 노드들 NULL 값으로 변환
                    SetNull_NearChildNode(ref bst, i);
                    
                    _carriedUndergradeItemList.Add(_owner.GetIndex_Inventory(arr[i].Get_ItemName()));
                }
            }
        }

        return endCost;
    }

    /// <summary>
    /// 가까운 자식 노드들을 반환합니다.
    /// </summary>
    /// <returns></returns>
    private void SetNull_NearChildNode(ref Item_BST bst, int nodeIndex)
    {
        //Left Node
        if (nodeIndex * 2 + 1 < bst.Get_Array().Length)
        {
            bst.Get_Array()[nodeIndex * 2 + 1] = null;
        }
        //Right Node
        if (nodeIndex * 2 + 2 < bst.Get_Array().Length)
        {
            bst.Get_Array()[nodeIndex * 2 + 2] = null;
        }
    }
}
