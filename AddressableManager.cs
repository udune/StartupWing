using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class Labels
{
    public readonly string Scene = "Scene";
}

public class AddressableManager : MonoBehaviour
{
    private Labels labels;
    private Dictionary<string, GameObject> preloadedObjects = new Dictionary<string, GameObject>();

    void Start()
    {
        // 초기화 로직이 필요하면 여기에 작성
    }

    void Update()
    {
        // 업데이트 로직이 필요하면 여기에 작성
    }

    public void LoadAssetByLabel(string label, string assetName)
    {
        StartCoroutine(PreloadAssetsWithLabel(label, assetName));
    }

    private IEnumerator PreloadAssetsWithLabel(string label, string assetName)
    {
        // 라벨과 관련된 리소스 위치들을 로드
        var loadResourceLocationsHandle = Addressables.LoadResourceLocationsAsync(label);

        // 리소스 위치 로드가 완료될 때까지 대기
        yield return loadResourceLocationsHandle;

        if (loadResourceLocationsHandle.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError("리소스 위치 로드 실패.");
            yield break;
        }

        // 각 자산을 비동기적으로 로딩 시작
        List<AsyncOperationHandle> loadOperations = new List<AsyncOperationHandle>();

        foreach (IResourceLocation location in loadResourceLocationsHandle.Result)
        {
            var loadAssetHandle = Addressables.LoadAssetAsync<GameObject>(location);
            loadAssetHandle.Completed += (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    preloadedObjects.Add(location.PrimaryKey, handle.Result);
                }
                else
                {
                    Debug.LogError($"위치 {location.PrimaryKey}에서 자산 로드 실패.");
                }
            };

            loadOperations.Add(loadAssetHandle);
        }

        // 모든 자산 로딩이 완료될 때까지 대기
        var groupOp = Addressables.ResourceManager.CreateGenericGroupOperation(loadOperations);
        yield return groupOp;

        // 리소스 위치 핸들 해제
        Addressables.Release(loadResourceLocationsHandle);

        // 로드된 자산들 출력
        foreach (var item in preloadedObjects)
        {
            Debug.Log($"{item.Key} - {item.Value.name}");
        }
    }

    public void InstantiateAsset(string assetKey)
    {
        // 키로 자산을 비동기적으로 인스턴스화
        Addressables.InstantiateAsync(assetKey);
    }
}
