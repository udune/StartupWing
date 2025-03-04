# StartupWing

Addressable, Language, Player, RoomProperty, Server, Sound, UI
큰 부분을 singleton 객체로 만들어서 구현했습니다.

** 디테일한 코드는 유출이 불가해서 참조를 하지 못했습니다.

[PlayerManager]
1. SubscribeRPC, UnsubscribeRPC, SubscribeVoiceChat 등의 메서드로 이벤트 구독/해제를 관리.
2. SetLayerRecursively, PlayerAvatarSetting, SetPhotonObserved 등의 함수로 중복을 줄임.
3. PlayerAvatarCreate에서 await _avatarModelController.CreateModel(...)을 사용하여 비동기 로직을 적용.
