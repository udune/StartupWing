using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.Mathematics;
using Photon.Pun;

public class PlayerData
{
    public AvatarPartsInfo.AvatarPartsData avatarData;
}

public class PlayerManager : MonoBehaviour
{
    public static PlayerData PlayerData => _playerData;
    private static PlayerData _playerData = new PlayerData();

    public RpcModel RpcModel => _rpcModel;
    private RpcModel _rpcModel;
    public event Action<GameObject> OnCreateAvatarEvent = null;

    [HideInInspector] public GameObject _playerAvatarObject = null;
    [SerializeField] HUD HUD;
    [SerializeField] PlayerView _playerView;
    [SerializeField] EmojiIconController _emojiIconController;

    private Camera _uiCamera => Camera.main;
    private PhotonView _pv;
    private CharacterAnimationModel _animationModel;
    private AvatarModelController _avatarModelController;

    public static void SetPlayerData(PlayerData data)
    {
        _playerData = data;
    }

    private void Awake()
    {
        PhotonNetwork.IsMessageQueueRunning = false;
        _pv = GetComponent<PhotonView>();
        _rpcModel = gameObject.AddComponent<RpcModel>();
        _rpcModel.OnEmojiWobbleup = UIEmotionPresenter.OnEmojiWobbleup;
        _animationModel = new CharacterAnimationModel();
        _avatarModelController = new AvatarModelController();
    }

    private void Start()
    {
        if (!_pv.IsMine)
            PlayerAvatarCreate();
    }

    public void OnGetMessage(PhotonChatData photonChatData)
    {
        if (photonChatData.chatData.userId == _pv.Owner.UserId)
            _rpcModel.OnSendMessage(photonChatData.chatData.msg);
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

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

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

    private void ChangeNickname(int userId, string nickname, string position)
    {
        if (_pv.IsMine)
        {
            PhotonNetwork.LocalPlayer.NickName = nickname;
        }

        _playerView.SetPlayerNickname(nickname);
    }

    #region Voice Chat

    private void SubscribeVoiceChat()
    {
        if (_pv.IsMine)
        {
            AgoraModel.OnLocalVolumeIndication += OnLocalVoiceChat;
        }
    }

    private void OnLocalVoiceChat(bool isMute)
    {
        _rpcModel.OnPlayerVoiceChat(isMute);
    }

    #endregion

    #region Video Chat

    private void SubscribeVideoChat()
    {
        AgoraModel.OnVideoSizeChanged += OnVideoSizeChanged;
        _rpcModel.OnVideoChat += OnVideoChat;

        if (_pv.IsMine)
        {
            AgoraModel.OnMutedLocalVideoStream += OnLocalVideoChat;
        }
    }

    private void UnSubscribeVideoChat()
    {
        AgoraModel.OnVideoSizeChanged -= OnVideoSizeChanged;
        _rpcModel.OnVideoChat -= _playerView.SetVideo;

        if (_pv.IsMine)
        {
            AgoraModel.OnMutedLocalVideoStream -= OnLocalVideoChat;
        }
    }

    private void OnLocalVideoChat(bool muted)
    {
        int uid = (int)_pv.Owner.CustomProperties[Const.User.id];
        _rpcModel.OnPlayerVideoChat(uid, muted);
    }

    private void OnVideoChat(int uid, bool muted)
    {
        PlayerVideoChatSetting(muted);
        _playerView.SetVideo(uid, muted);
    }


    private void PlayerVideoChatSetting(bool isActive)
    {
        Transform tr = _playerAvatarObject.transform;

        for (int i = 0; i < tr.childCount; i++)
        {
            GameObject avatarObject = tr.GetChild(i).gameObject;
            avatarObject.SetActive(isActive);
        }

        if (isActive)
        {
            HUD.SetHeight(150f);
        }
        else
        {
            HUD.SetHeight(100f);
        }
    }

    private void OnVideoSizeChanged(uint uid, Vector2 resolution, AgoraType type)
    {
        int ownerUid = 0;

        if (type == AgoraType.Camera)
        {
            ownerUid = (int)_pv.Owner.CustomProperties[Const.User.id];
            // uid: 0 = local user = myself
            if (uid == 0)
            {
                if (_pv.IsMine)
                    _playerView.SetVideoSizeChanged(resolution);
            }
            else
            {
                if (uid == (uint)ownerUid)
                    _playerView.SetVideoSizeChanged(resolution);
            }
        }

        Debug.Log($"[PlyaerManager] OnVideoSizeChanged - uid: {uid}, ownerUid: {ownerUid}, resolution: {resolution}, type: {type}");
    }

    #endregion

    private void OnDestroy()
    {
        UnSubscribeVideoChat();
        UnsubscribeRPC();
    }
}