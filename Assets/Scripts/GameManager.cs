using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static string USER_NICKNAME = "MyName";
    public static string USER_CHARACTER = "BEAR";

    public static GameManager s_instance;
    public static GameManager instance
    {
        get
        {
            if (!s_instance)
            {
                s_instance = FindObjectOfType(typeof(GameManager)) as GameManager;
                if (!s_instance)
                {
                    Debug.LogError("GameManager s_instance null");
                    return null;
                }
            }

            return s_instance;
        }
    }

    void Awake()
    {
        if (s_instance == null)
        {
            s_instance = this;

            DontDestroyOnLoad(this);

        }
        else if (this != s_instance)
        {
            Destroy(gameObject);
        }

        //GET PLAYERPREFS "NICKNAME"
        USER_NICKNAME = (PlayerPrefs.HasKey("NICKNAME")) ? PlayerPrefs.GetString("NICKNAME") : "MyName";
        if (!PlayerPrefs.HasKey("NICKNAME")) PlayerPrefs.SetString("NICKNAME", "MyName");

        //GET PLAYERPREFS "CHARACTER"
        if (!PlayerPrefs.HasKey("CHARACTER")) PlayerPrefs.SetString("CHARACTER", "BEAR");
        USER_CHARACTER = PlayerPrefs.GetString("CHARACTER");
    }
}
