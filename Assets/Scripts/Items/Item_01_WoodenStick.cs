using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_01_WoodenStick : ItemBase
{
    public Item_01_WoodenStick()
    {
        specs.Add(new ST_Ability(ItemCategory.공격력, 1));
    }
}
