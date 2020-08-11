using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioSource _bgmSource;
    [SerializeField] AudioSource _seSource;

    [HideInInspector] public float bgmVolume;
    [HideInInspector] public float seVolume;

    Dictionary<string, AudioClip> _bgmClips;
    Dictionary<string, AudioClip> _seClips;

    public static SoundManager instance;
    private void Awake()
    {
        instance = this;

        SetInitAddr_SoundResources();
    }

    /// <summary>
    /// 사운드 리소스를 초기 등록합니다.
    /// </summary>
    private void SetInitAddr_SoundResources()
    {
        _bgmClips = new Dictionary<string, AudioClip>();
        _seClips = new Dictionary<string, AudioClip>();

        var objTemp = Resources.LoadAll("Sound/BGM/");
        foreach(var item in objTemp)
        {
            _bgmClips.Add(item.ToString(), item as AudioClip);
        }

        objTemp = Resources.LoadAll("Sound/SE/");
        foreach(var item in objTemp)
        {
            _seClips.Add(item.ToString(), item as AudioClip);
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
}
