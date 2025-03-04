# StartupWing

Addressable, Language, Player, RoomProperty, Server, Sound, UI
큰 부분을 singleton 객체로 만들어서 구현했습니다.

** 디테일한 코드는 유출이 불가해서 참조를 하지 못했습니다.

# [PlayerManager]
  # SubscribeRPC, UnsubscribeRPC, SubscribeVoiceChat 등의 메서드로 이벤트 구독/해제를 관리.
    private void SubscribeRPC()
    {
        _rpcModel.OnEmojiIcon += _emojiIconController.SetEmoji;
        _rpcModel.OnChangeNickname += ChangeNickname;
        _rpcModel.OnGetMessage += _playerView.SetChatText;
        _rpcModel.OnPlayAnimation += _animationModel.PlayEmotion;
        _rpcModel.OnPlayEffectAnimation += _animationModel.PlayEffect;
        _rpcModel.OnChangeAvatarParts += _avatarModelController.SetAvatarData;
        _rpcModel.OnVoiceChat += _playerView.PlayBilnkAnimation;

        PhotonNetwork.IsMessageQueueRunning = true;
    }

    private void UnsubscribeRPC()
    {
        _rpcModel.OnEmojiIcon -= _emojiIconController.SetEmoji;
        _rpcModel.OnChangeNickname -= ChangeNickname;
        _rpcModel.OnGetMessage -= _playerView.SetChatText;
        _rpcModel.OnPlayAnimation -= _animationModel.PlayEmotion;
        _rpcModel.OnPlayEffectAnimation -= _animationModel.PlayEffect;
        _rpcModel.OnChangeAvatarParts -= _avatarModelController.SetAvatarData;
        _rpcModel.OnVoiceChat -= _playerView.PlayBilnkAnimation;
    }
   
  # PlayerAvatarCreate에서 await avatarModelController.CreateModel(...)을 사용하여 비동기 로직을 적용.
       public async void PlayerAvatarCreate(Transform respawn = null)
        {
        Vector3 position = respawn?.position ?? Vector3.zero;

        _playerAvatarObject = await _avatarModelController.CreateModel(AppConfig.AppSettings.defaultAvatarKey, position, transform);

        if (_playerAvatarObject == null)
        {
            Debug.LogError("Failed to create player avatar.");
            return;
        }

        if (respawn == null)
        {
            _playerAvatarObject.transform.eulerAngles = Vector3.zero;
        }
        else
        {
            _playerAvatarObject.transform.position = respawn.position;
            _playerAvatarObject.transform.rotation = respawn.rotation;
        }

        PlayerAvatarSetting(_playerAvatarObject);
        }

# [LanguageManager]
  # Action 이벤트를 활용한 언어 변경 감지
    private static void SetLanguage(int value)
    {
        index = value;
        PlayerPrefs.SetInt(LANGUAGE_KEY, index);
    #if !UNITY_EDITOR
      //  SaveLangToLocalStorage(index);
    #endif
        OnChangedLanguage?.Invoke();
    }

    public static void SetLanguage(string countryCode)
    {
        index = countryCodes.IndexOf(countryCode) - 1;

        PlayerPrefs.SetInt(LANGUAGE_KEY, index);
    #if !UNITY_EDITOR
      //  SaveLangToLocalStorage(index);
    #endif
        OnChangedLanguage?.Invoke();
    }
  
  # Dictionary를 사용한 npcMessage 관리
    public static List<string> GetNPCMessageList(string sceneName)
    {
        List<string> messageList;
        List<Language> language;

        if (!npcMessage.TryGetValue(sceneName, out language))
        {
            language = languages.FindAll(o => o.Code.Contains(sceneName));
        }

        messageList = language.Select(o => o.Languages[index]).ToList();

        for (int i = 0; i < messageList.Count; i++)
        {
            if (index == 2) // 일본어 데이터 load시 불필요 공백 및 \r 추가 제거
            {
                messageList[i] = messageList[i].Replace("\r", "");
                messageList[i] = messageList[i].Replace(" ", "");
            }

            messageList[i] = messageList[i].Replace("<N>", "\n");
        }

        return messageList;
    }
  
# [AudioManager]
  # Dictionary<string, AudioClip>을 이용한 오디오 캐싱 최적화
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

# [UIManager]
  # Dictionary<string, GameObject>를 활용한 UI 캐싱
  # CreateUI<T>()에서 부모(Parent) 지정 가능
    public T GetUI<T>(Transform parent = null) where T : Component
    {
        if (UIList.ContainsKey(typeof(T).Name) && UIList[typeof(T).Name] != null)
            return UIList[typeof(T).Name].GetComponent<T>();
        else
            return CreateUI<T>(parent);
    }

  # OpenToast()로 간편한 토스트 메시지 UI 생성
    public void OpenToast(string toast, float timer = 3f, bool Center = true)
    {
        UIToast Toast = null;

        Toast = CreateUI<UIToast>();

        Toast.SetToast(toast, timer, Center);
    }
