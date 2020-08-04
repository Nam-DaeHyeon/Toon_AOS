using System.Collections;
using System.Collections.Generic;

public class Item_05_Staff : ItemBase
{
    public Item_05_Staff()
    {
        //Set_ChildItem(new Item_01_WoodenStick(), new Item_02_Jewel());
        Set_ChildItem(ItemManager.ItemDB["WoodenStick"], ItemManager.ItemDB["Jewel"]);

        specs.Add(new ST_Ability(ItemCategory.마법공격력, 5));
        specs.Add(new ST_Ability(ItemCategory.체력, 20));
    }
}
