using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item_08_Potion : ItemBase
{
    public Item_08_Potion()
    {
        specs.Add(new ST_Ability(ItemCategory.소모품_체력회복, 50));
    }

    public override void Use_Consumables(Player user)
    {
        user.TakeHeal(user.Get_MaxHP() * 0.5f);
    }
}
