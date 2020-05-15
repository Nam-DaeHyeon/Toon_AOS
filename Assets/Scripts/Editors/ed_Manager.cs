using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class ed_Manager : Editor
{
    [MenuItem("Custom/Delete All PlayerPrefs")]
    public static void Clear_AllPrefabs()
    {
        Debug.Log("Delete All PlayerPrefs");
        PlayerPrefs.DeleteAll();
    }
}
#endif
