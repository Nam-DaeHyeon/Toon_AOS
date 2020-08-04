using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_03_SharpMass : ItemBase
{
    public Item_03_SharpMass()
    {
        specs.Add(new ST_Ability(ItemCategory.공격력, 3));
    }
}
