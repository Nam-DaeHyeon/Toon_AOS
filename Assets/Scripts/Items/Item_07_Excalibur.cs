using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_07_Excalibur : ItemBase
{
    public Item_07_Excalibur()
    {
        //Set_ChildItem(new Item_04_Sword(), new Item_06_DragonHeart());
        Set_ChildItem(ItemManager.ItemDB["Sword"], ItemManager.ItemDB["DragonHeart"]);

        specs.Add(new ST_Ability(ItemCategory.공격력, 20));
        specs.Add(new ST_Ability(ItemCategory.방어력, 7));
        specs.Add(new ST_Ability(ItemCategory.마법방어력, 7));
    }
}
