using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_06_DragonHeart : ItemBase
{
    public Item_06_DragonHeart()
    {
        specs.Add(new ST_Ability(ItemCategory.방어력, 5));
        specs.Add(new ST_Ability(ItemCategory.마법방어력, 5));

        cost = Get_ChildItemsCost() + 5;
    }
}
