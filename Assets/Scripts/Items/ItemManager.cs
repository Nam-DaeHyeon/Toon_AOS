using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

public static class ItemManager
{
    public static Dictionary<string, ItemBase> ItemDB = new Dictionary<string, ItemBase>();

    static ItemManager()
    {
        //ItemBase를 상속받는 모든 아이템 클래스들을 배열로 반환합니다.
        Type parentType = typeof(ItemBase);
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types = assembly.GetTypes();

        IEnumerable<Type> subclasses = types.Where(t => t.IsSubclassOf(parentType));
        
        //직계 자식 클래스만 찾고 싶을 때
        //IEnumerable<Type> subclasses = types.Where(t => t.BaseType == parentType);

        foreach (Type item in subclasses)
        {
            ItemDB.Add(item.Name.Substring(item.Name.LastIndexOf('_') + 1), Activator.CreateInstance(item) as ItemBase);
        }
    }
}
