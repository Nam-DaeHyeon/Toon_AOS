using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_02_Jewel : ItemBase
{
    public Item_02_Jewel()
    {
        specs.Add(new ST_Ability(ItemCategory.체력, 10));
        cost = 2;
    }
}
