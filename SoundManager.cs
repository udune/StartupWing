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
        _audioMixer.SetFloat(Volume.MASTER, _masterDBVolume);
        _audioMixer.SetFloat(Volume.BGM, _bgmDBVolume);
        _audioMixer.SetFloat(Volume.SFX, _sfxDBVolume);
        _audioMixer.SetFloat(Volume.ALARM, _alarmDBVolume);
    }

    public void PlayBGM(string _soundName)
    {
        LoadAsyncAudioClip(_soundName, (result) =>
        {
            StopBGM();
            _bgmSource.clip = result;
            _bgmSource.loop = true;
            _bgmSource.enabled = true;
            _bgmSource.Play();
        });
    }

    public void StopBGM()
    {
        if (_bgmSource == null)
            return;

        _bgmSource.Stop();
        _bgmSource.clip = null;
        _bgmSource.enabled = false;
    }

    public void PlaySE(string _soundName)
    {
        LoadAsyncAudioClip(_soundName, (result) => { _sfxSources.PlayOneShot(result); });
    }

    public void PlayALARM(string _soundName)
    {
        LoadAsyncAudioClip(_soundName, (result) => { _alarmSources.PlayOneShot(result); });
    }

    async void LoadAsyncAudioClip(string _soundName, Action<AudioClip> action)
    {
        if (_audioClips.TryGetValue(_soundName, out AudioClip audioClip))
        {
            action?.Invoke(audioClip);
        }
        else
        {
            AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(_soundName);
            await handle.Task;

            if (handle.Status.Equals(AsyncOperationStatus.Succeeded))
            {
                AudioClip result = Instantiate(handle.Result);
                _audioClips.Add(_soundName, result);
                action?.Invoke(result);
            }
            else
                Debug.LogError(handle.OperationException);

#if UNITY_WEBGL
            Addressables.Release(handle);
#endif
        }
    }

    #region tab change event in web brower

    public void OnWebGLTabChangeEvent(string _state)
    {
        if (_state.Equals("focus"))
            OnMasterVolumeMute(false);
        else
            OnMasterVolumeMute(true);
    }

    #endregion

    public void OnMasterVolumeMute(bool _isMute)
    {
        if (!_isMute)
        {
            _audioMixer.SetFloat(Volume.MASTER, _bgmDBVolume);
            return;
        }

        _audioMixer.SetFloat(Volume.MASTER, Volume.MUTE);
    }

    public void SetVolume(string volumeName, float value)
    {
        float dbValue;
        if (value < 0.01f)
        {
            dbValue = Volume.MUTE;
        }
        else
        {
            dbValue = Mathf.Lerp(Volume.MIN, Volume.MAX, value);
        }

        switch (volumeName)
        {
            case Volume.MASTER:
                _masterDBVolume = dbValue;
                _audioMixer.SetFloat(Volume.MASTER, _masterDBVolume);
                break;
            case Volume.BGM:
                _bgmDBVolume = dbValue;
                _audioMixer.SetFloat(Volume.BGM, _bgmDBVolume);
                break;
            case Volume.SFX:
                _sfxDBVolume = dbValue;
                _audioMixer.SetFloat(Volume.SFX, _sfxDBVolume);
                break;
            case Volume.ALARM:
                _alarmDBVolume = dbValue;
                _audioMixer.SetFloat(Volume.ALARM, _alarmDBVolume);
                break;
        }
    }

    public float GetVolume(string volumeName)
    {
        float dbVolume = 0f;
        switch (volumeName)
        {
            case Volume.MASTER:
                _audioMixer.GetFloat(Volume.MASTER, out dbVolume);
                break;
            case Volume.BGM:
                _audioMixer.GetFloat(Volume.BGM, out dbVolume);
                break;
            case Volume.SFX:
                _audioMixer.GetFloat(Volume.SFX, out dbVolume);
                break;
            case Volume.ALARM:
                _audioMixer.GetFloat(Volume.ALARM, out dbVolume);
                break;
        }

        if (dbVolume <= Volume.MIN)
        {
            return 0f;
        }
        else
        {
            return Mathf.InverseLerp(Volume.MIN, Volume.MAX, dbVolume);
        }
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat(Volume.MASTER, _masterDBVolume);
        PlayerPrefs.SetFloat(Volume.BGM, _bgmDBVolume);
        PlayerPrefs.SetFloat(Volume.SFX, _sfxDBVolume);
        PlayerPrefs.SetFloat(Volume.ALARM, _alarmDBVolume);
    }
}