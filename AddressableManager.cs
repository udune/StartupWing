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
  
    Labels labels;
    AssetLabelReference lable;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Load(string label, string name)
    {
        
        StartCoroutine(PreloadHazards(labels.Scene, "name"));
    }

    Dictionary<string, GameObject> _preloadedObjects = new Dictionary<string, GameObject>();
    private IEnumerator PreloadHazards(string label, string name)
        {
      
            //find all the locations with label "SpaceHazards"
            var loadResourceLocationsHandle
                = Addressables.LoadResourceLocationsAsync("SpaceHazards");

        AsyncOperationHandle h =  Addressables.LoadSceneAsync("");

       

            if (!loadResourceLocationsHandle.IsDone)
                yield return loadResourceLocationsHandle;

            //start each location loading
            List<AsyncOperationHandle> opList = new List<AsyncOperationHandle>();

            foreach (IResourceLocation location in loadResourceLocationsHandle.Result)
            {
                AsyncOperationHandle<GameObject> loadAssetHandle   = Addressables.LoadAssetAsync<GameObject>(location);
                loadAssetHandle.Completed += obj => { _preloadedObjects.Add(location.PrimaryKey, obj.Result); };
                opList.Add(loadAssetHandle);
            }

            //create a GroupOperation to wait on all the above loads at once. 
            var groupOp = Addressables.ResourceManager.CreateGenericGroupOperation(opList);

            if (!groupOp.IsDone)
                yield return groupOp;

            Addressables.Release(loadResourceLocationsHandle);

            //take a gander at our results.
            foreach (var item in _preloadedObjects)
            {
                Debug.Log(item.Key + " - " + item.Value.name);
            }
        }
    

    void Instate()
    {
        Addressables.InstantiateAsync("test");
    }
}
