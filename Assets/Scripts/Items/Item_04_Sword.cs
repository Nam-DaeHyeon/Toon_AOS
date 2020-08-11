using System.Collections;
using System.Collections.Generic;

public class Item_04_Sword : ItemBase
{
    public Item_04_Sword()
    {
        //Set_ChildItem(new Item_01_WoodenStick(), new Item_03_SharpMass());
        Set_ChildItem(ItemManager.ItemDB["WoodenStick"], ItemManager.ItemDB["SharpMass"]);

        specs.Add(new ST_Ability(ItemCategory.공격력, 10));

        cost = Get_ChildItemsCost() + 2;
    }
}
