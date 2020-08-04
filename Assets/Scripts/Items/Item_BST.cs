using System.Collections;
using System.Collections.Generic;
using System;

public class Item_BST
{
    //List<ItemBase> Tree = new List<ItemBase>();
    ItemBase[] CTree;   //자식 노드 트리
    List<ItemBase> PList;   //부모 노드 리스트
    int powIndex = 2;

    #region 자식 노드들을 완전이진트리로 저장
    public void ChildNodeTraversal(ItemBase item)
    {
        ChildNodeRecursive(item, -1, true);
    }

    private void ChildNodeRecursive(ItemBase item, int pIndex, bool isLeftNode)
    {
        if (item == null) return;
        
        if (pIndex == -1)
        {
            CTree = new ItemBase[powIndex - 1];
            powIndex *= 2;
            CTree[0] = item;
            pIndex = 0;
        }
        else
        {
            int nIndex = isLeftNode? 2 * pIndex + 1 : 2 * pIndex + 2;
            if (nIndex >= CTree.Length)
            {
                Array.Resize<ItemBase>(ref CTree, powIndex - 1);
                powIndex *= 2;
            }

            CTree[nIndex] = item;
            pIndex = nIndex;
        }

        ChildNodeRecursive(item.Get_ChildNode(0), pIndex, true);
        ChildNodeRecursive(item.Get_ChildNode(1), pIndex, false);
    }
    #endregion

    #region 부모 노드와 그 상위 노드까지 리스트 저장
    public void ParentNodeTraversal(ItemBase item)
    {
        ParentNodeRecursive(item);
    }

    private void ParentNodeRecursive(ItemBase item)
    {
        if (item == null) return;
        if (item.Get_ParentNode() == null) return;

        foreach(ItemBase parent in item.Get_ParentNode())
        {
            if (PList == null) PList = new List<ItemBase>();
            PList.Add(parent);

            ParentNodeRecursive(parent);
        }
    }
    #endregion

    /// <summary>
    /// 자식 노트 트리 또는 부모 노드 리스트를 반환합니다.
    /// </summary>
    public ItemBase[] Get_Array()
    {
        if (CTree != null) return CTree;
        if (PList != null) return PList.ToArray();

        return null;
    }
}