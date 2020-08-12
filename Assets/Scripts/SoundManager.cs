using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioSource _bgmSource;
    [SerializeField] AudioSource _seSource;
    
    [HideInInspector] public float bgmVolume = 1;
    [HideInInspector] public float seVolume = 1;

    Dictionary<string, AudioClip> _bgmClips;
    Dictionary<string, AudioClip> _seClips;

    /// <summary>
    /// BGM 볼륨 보정 비율
    /// </summary>
    float _bgmVolumeCorr = 0.2f;

    public static SoundManager s_instance;
    public static SoundManager instance
    {
        get
        {
            if (!s_instance)
            {
                s_instance = FindObjectOfType(typeof(SoundManager)) as SoundManager;
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

            SetInitAddr_SoundResources();

        }
        else if (this != s_instance)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 사운드 리소스를 초기 등록합니다.
    /// </summary>
    private void SetInitAddr_SoundResources()
    {
        _bgmClips = new Dictionary<string, AudioClip>();
        _seClips = new Dictionary<string, AudioClip>();

        bgmVolume = 1;
        seVolume = 1;
        _bgmSource.volume = bgmVolume * _bgmVolumeCorr;
        _seSource.volume = seVolume;

        var objTemp = Resources.LoadAll("Sound/BGM/");
        foreach (var item in objTemp)
        {
            _bgmClips.Add(item.name, item as AudioClip);
        }

        objTemp = Resources.LoadAll("Sound/SE/");
        foreach (var item in objTemp)
        {
            _seClips.Add(item.name, item as AudioClip);
        }
    }

    /// <summary>
    /// BGM을 실행합니다.
    /// </summary>
    /// <param name="bgmName">실행시킬 BGM 이름</param>
    public void Play_BGM(string bgmName)
    {
        if (!_bgmSource.isPlaying) _bgmSource.Stop();

        _bgmSource.clip = _bgmClips[bgmName];
        _bgmSource.Play();
    }

    /// <summary>
    /// 효과음을 실행합니다.
    /// </summary>
    /// <param name="seName">실행시킬 효과음 이름</param>
    public void Play_SE(string seName)
    {
        _seSource.PlayOneShot(_seClips[seName], seVolume);
    }
    
    public void SetVolume_BGM(float vol)
    {
        bgmVolume = vol;
        _bgmSource.volume = bgmVolume * _bgmVolumeCorr;
    }

    public void SetVolume_SE(float vol)
    {
        seVolume = vol;
        _seSource.volume = seVolume;
    }
}
