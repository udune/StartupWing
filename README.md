# StartupWing

Addressable, Language, Player, RoomProperty, Server, Sound, UI
큰 부분을 singleton 객체로 만들어서 구현했습니다.

** 디테일한 코드는 유출이 불가해서 참조를 하지 못했습니다.

[PlayerManager]
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
   
  # PlayerAvatarCreate에서 await _avatarModelController.CreateModel(...)을 사용하여 비동기 로직을 적용.
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
        
  # SetLayerRecursively, PlayerAvatarSetting, SetPhotonObserved 등의 함수로 중복을 줄임.

