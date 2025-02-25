# StartupWing

Addressable, Language, Player, RoomProperty, Server, Sound, UI
큰 부분을 singleton 객체로 만들어서 구현했습니다.

** 디테일한 코드는 유출이 불가해서 참조를 하지 못했습니다.

[PlayerManager]
public class PlayerData
{
    public AvatarPartsInfo.AvatarPartsData avatarData;
}

public class PlayerManager : MonoBehaviour
{
    // Player Data (아바타 파츠 데이터) 를 다른 객체에서 호출할수 있습니다.
    public static PlayerData PlayerData => _playerData;
    private static PlayerData _playerData = new PlayerData();

    // Player Data (아바타 파츠 데이터) 를 세팅합니다.
    public static void SetPlayerData(PlayerData data)
    {
        _playerData = data;
    }

    // 아바타를 생성합니다. (내가 아닌 경우)
    private void Start()
    {
        if (!_pv.IsMine)
            PlayerAvatarCreate();
    }

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

    private void PlayerAvatarSetting(GameObject avatar)
    {
        if (_pv.IsMine)
        {
            GameObject camTarget = transform.Find("CamTarget")?.gameObject ?? new GameObject("CamTarget");
            camTarget.transform.SetParent(avatar.transform);
            camTarget.transform.localPosition = new Vector3(0, 1, 0);
            camTarget.transform.localRotation = quaternion.identity;

            avatar.AddComponent<CharacterController>();
            avatar.tag = PlayerInfo.PLAYER_TAG;
            SetLayerRecursively(avatar, LayerMask.NameToLayer(PlayerInfo.PLAYER_TAG));

            NavMeshAgent navmesh = avatar.gameObject.AddComponent<NavMeshAgent>();
            navmesh.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            navmesh.autoTraverseOffMeshLink = false;
        }
        else
        {
            avatar.tag = PlayerInfo.OTHER_PLAYER_TAG;
            SetLayerRecursively(avatar, LayerMask.NameToLayer(PlayerInfo.OTHER_PLAYER_TAG));
        }

        SetAvatarAttribute(avatar);
        SetPhotonObserved(avatar);

        SubscribeRPC();
        SubscribeVideoChat();
        SubscribeVoiceChat();

        OnCreateAvatarEvent?.Invoke(avatar);
        _avatarModelController.SetAvatarData(_pv.InstantiationData);
        _playerView.SetPlayerNickname(_pv.Owner.NickName);
    }

    private void SetAvatarAttribute(GameObject avatar)
    {
        CharacterAttribute attribute = avatar.GetComponent<CharacterAttribute>();
        HUD.SetTargetPoint(attribute.HudTransform, _uiCamera, attribute.headOffset);

        _animationModel.Animator = attribute.Animator;

        GeneralAnimationBehaviour[] animationBehaviours = attribute.Animator.GetBehaviours<GeneralAnimationBehaviour>();
        foreach (var animationBehaviour in animationBehaviours)
        {
            animationBehaviour.SetAnimationTarget(attribute.gameObject);
        }
    }

    // PhotonTransformView를 Add하고 동기화 설정을 해줍니다.
    private void SetPhotonObserved(GameObject newModel)
    {
        PhotonTransformView photonTransformView = newModel.AddComponent<PhotonTransformView>();
        photonTransformView.m_SynchronizePosition = true;
        photonTransformView.m_SynchronizeRotation = true;
        photonTransformView.m_SynchronizeScale = false;
        photonTransformView.m_UseLocal = true;
        _pv.ObservedComponents.Add(photonTransformView);

        PhotonAnimatorView photonAnimatorView = newModel.AddComponent<PhotonAnimatorView>();
        photonAnimatorView.SetLayerSynchronized(0, PhotonAnimatorView.SynchronizeType.Discrete);
        photonAnimatorView.SetParameterSynchronized(AnimationInfo.ANI_SPEED, PhotonAnimatorView.ParameterType.Float, PhotonAnimatorView.SynchronizeType.Discrete);
        photonAnimatorView.SetParameterSynchronized(AnimationInfo.ANI_RUN, PhotonAnimatorView.ParameterType.Bool, PhotonAnimatorView.SynchronizeType.Discrete);
        photonAnimatorView.SetParameterSynchronized(AnimationInfo.ANI_JUMP, PhotonAnimatorView.ParameterType.Trigger, PhotonAnimatorView.SynchronizeType.Discrete);
        photonAnimatorView.SetParameterSynchronized(AnimationInfo.ANI_EMOTION, PhotonAnimatorView.ParameterType.Int, PhotonAnimatorView.SynchronizeType.Discrete);
        photonAnimatorView.SetParameterSynchronized(AnimationInfo.ANI_CYCLEEMOTION, PhotonAnimatorView.ParameterType.Int, PhotonAnimatorView.SynchronizeType.Discrete);
        photonAnimatorView.SetParameterSynchronized(AnimationInfo.ANI_EFFECT, PhotonAnimatorView.ParameterType.Int, PhotonAnimatorView.SynchronizeType.Discrete);
        _pv.ObservedComponents.Add(photonAnimatorView);
    }

    // 포톤 RPC를 설정합니다.
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

    // 포톤 RPC를 해지합니다.
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

    // 닉네임을 변경합니다.
    private void ChangeNickname(int userId, string nickname, string position)
    {
        if (_pv.IsMine)
        {
            PhotonNetwork.LocalPlayer.NickName = nickname;
        }

        _playerView.SetPlayerNickname(nickname);
    }
}
