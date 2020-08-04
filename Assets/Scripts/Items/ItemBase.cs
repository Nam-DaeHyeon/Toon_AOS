using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum ItemCategory
{
    공격력,
    마법공격력,
    방어력,
    마법방어력,
    체력,
    이동속도,
    소모품
}

public struct ST_Ability
{
    public ItemCategory category;
    public int value;

    public ST_Ability(ItemCategory cate, int val)
    {
        category = cate;
        value = val;
    }
}

[System.Serializable]
public class ItemBase
{
    protected List<ItemBase> parentItem = null;
    protected ItemBase[] childItem = null;

    public List<ST_Ability> specs = new List<ST_Ability>();

    /// <summary>
    /// 부모 아이템 타입을 지정합니다.
    /// </summary>
    protected void Set_ParentItem(ItemBase parent)
    {
        //ItemBase를 상속하지 않는다면 Null 처리
        //if (GetType() != parent.GetType().BaseType) parent = null;

        if (parentItem == null) parentItem = new List<ItemBase>();
        parentItem.Add(parent);
    }

    /// <summary>
    /// 자식 아이템 타입들을 지정합니다.
    /// </summary>
    /// <param name="leftChild">좌측 노드</param>
    /// <param name="rightChild">우측 노드</param>
    protected void Set_ChildItem(ItemBase leftChild = null, ItemBase rightChild = null)
    {
        childItem = new ItemBase[2] { leftChild, rightChild };
        
        for(int i = 0; i < childItem.Length; i++)
        {
            if (childItem[i] == null) break;

            //본 아이템을 1단계 부모 노드에 추가
            childItem[i].Set_ParentItem(this);
        }
    }

    /// <summary>
    /// 부모 노드의 아이템을 반환합니다.
    /// </summary>
    public ItemBase[] Get_ParentNode()
    {
        if (parentItem == null) return null;
        return parentItem.ToArray();
    }

    /// <summary>
    /// 자식 노드의 아이템을 반환합니다.
    /// </summary>
    /// <param name="index">0 : 좌측 노드 | 1 : 우측 노드</param>
    public ItemBase Get_ChildNode(int index)
    {
        if (childItem == null || childItem.Length <= index) return null;
        return childItem[index];
    }

    /// <summary>
    /// 특정 카테고리가 해당 아이템에 해당되는지 확인합니다.
    /// </summary>
    public bool CheckLINQ_Category(List<ItemCategory> targets)
    {
        List<ItemCategory> cateInSpec = (from node in specs
                                        select node.category).ToList();

        int stack = 0;
        for(int i = 0; i < targets.Count; i++)
        {
            if(cateInSpec.Contains(targets[i]))
            {
                cateInSpec.Remove(targets[i]);
                stack++;
            }
        }

        if (stack == targets.Count) return true;
        return false;
    }
}
