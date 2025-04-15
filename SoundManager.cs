using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Audio;

public class AudioClips
{
    public const string MAIN_BGM = "ssu_center";
}

public class Volume
{
    public const string MASTER = "MasterVolume";
    public const string BGM = "BGMVolume";
    public const string SFX = "SFXVolume";
    public const string ALARM = "ALARMVolume";
    public const float MAX = 0f;
    public const float MIN = -30f;
    public const float MUTE = -80f;
}

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] AudioSource _bgmSource;
    [SerializeField] AudioSource _sfxSources;
    [SerializeField] AudioSource _alarmSources;
    [SerializeField] AudioMixer _audioMixer;

    private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

    private float _masterDBVolume = -15;
    private float _bgmDBVolume = -15;
    private float _sfxDBVolume = -15;
    private float _alarmDBVolume = -15;

    private void Start()
    {
        _masterDBVolume = PlayerPrefs.GetFloat(Volume.MASTER, -15);
        _bgmDBVolume = PlayerPrefs.GetFloat(Volume.BGM, -15);
        _sfxDBVolume = PlayerPrefs.GetFloat(Volume.SFX, -15);
        _alarmDBVolume = PlayerPrefs.GetFloat(Volume.ALARM, -15);
        ApplyVolumeSettings();
    }

    private void ApplyVolumeSettings()
    {
        _audioMixer.SetFloat(Volume.MASTER, _masterDBVolume);
        _audioMixer.SetFloat(Volume.BGM, _bgmDBVolume);
        _audioMixer.SetFloat(Volume.SFX, _sfxDBVolume);
        _audioMixer.SetFloat(Volume.ALARM, _alarmDBVolume);
    }

    public void PlaySound(string soundName, AudioSource source, bool loop = false)
    {
        LoadAsyncAudioClip(soundName, (clip) =>
        {
            source.clip = clip;
            source.loop = loop;
            source.enabled = true;
            source.Play();
        });
    }

    public void PlayBGM(string soundName)
    {
        PlaySound(soundName, _bgmSource, true);
    }

    public void PlaySE(string soundName)
    {
        PlaySound(soundName, _sfxSources);
    }

    public void PlayALARM(string soundName)
    {
        PlaySound(soundName, _alarmSources);
    }

    public void StopBGM()
    {
        if (_bgmSource == null)
            return;

        _bgmSource.Stop();
        _bgmSource.clip = null;
        _bgmSource.enabled = false;
    }

    async void LoadAsyncAudioClip(string soundName, Action<AudioClip> action)
    {
        if (_audioClips.TryGetValue(soundName, out AudioClip audioClip))
        {
            action?.Invoke(audioClip);
        }
        else
        {
            AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(soundName);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                AudioClip result = Instantiate(handle.Result);
                _audioClips.Add(soundName, result);
                action?.Invoke(result);
            }
            else
            {
                Debug.LogError(handle.OperationException);
            }

#if UNITY_WEBGL
            Addressables.Release(handle);
#endif
        }
    }

    public void OnWebGLTabChangeEvent(string state)
    {
        bool isMute = state.Equals("blur");
        OnMasterVolumeMute(isMute);
    }

    public void OnMasterVolumeMute(bool isMute)
    {
        _audioMixer.SetFloat(Volume.MASTER, isMute ? Volume.MUTE : _bgmDBVolume);
    }

    public void SetVolume(string volumeName, float value)
    {
        float dbValue = value < 0.01f ? Volume.MUTE : Mathf.Lerp(Volume.MIN, Volume.MAX, value);

        switch (volumeName)
        {
            case Volume.MASTER:
                _masterDBVolume = dbValue;
                break;
            case Volume.BGM:
                _bgmDBVolume = dbValue;
                break;
            case Volume.SFX:
                _sfxDBVolume = dbValue;
                break;
            case Volume.ALARM:
                _alarmDBVolume = dbValue;
                break;
        }

        _audioMixer.SetFloat(volumeName, dbValue);
    }

    public float GetVolume(string volumeName)
    {
        _audioMixer.GetFloat(volumeName, out float dbVolume);
        return dbVolume <= Volume.MIN ? 0f : Mathf.InverseLerp(Volume.MIN, Volume.MAX, dbVolume);
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat(Volume.MASTER, _masterDBVolume);
        PlayerPrefs.SetFloat(Volume.BGM, _bgmDBVolume);
        PlayerPrefs.SetFloat(Volume.SFX, _sfxDBVolume);
        PlayerPrefs.SetFloat(Volume.ALARM, _alarmDBVolume);
    }
}
