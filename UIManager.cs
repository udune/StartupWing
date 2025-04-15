using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    private const string UIString = "UI";
    private Dictionary<string, GameObject> UIList = new Dictionary<string, GameObject>();

    // Get the UI if it exists, otherwise create it
    public T GetUI<T>(Transform parent = null) where T : Component
    {
        string className = typeof(T).Name;
        
        // If UI already exists in the list, return it
        if (UIList.TryGetValue(className, out GameObject uiObject) && uiObject != null)
            return uiObject.GetComponent<T>();

        // Otherwise, create and return a new UI
        return CreateUI<T>(parent);
    }

    // Create a new UI and add it to the list
    public T CreateUI<T>(Transform parent = null)
    {
        string className = typeof(T).Name;

        // Remove the old UI if it exists
        if (UIList.ContainsKey(className))
            UIList.Remove(className);

        // Load, instantiate, and add to the UI list
        GameObject loadedUI = LoadUI(className);
        if (loadedUI == null) return null;

        GameObject newUI = Instantiate(loadedUI, parent);
        AddUI<T>(newUI);

        return newUI.GetComponent<T>();
    }

    // Load the UI prefab from the Resources folder
    private GameObject LoadUI(string className)
    {
        string path = System.IO.Path.Combine(UIString, className);
        GameObject loadObject = Resources.Load<GameObject>(path);

        if (loadObject == null)
            Debug.LogError($"Failed to load UI: {className} at {path}");

        return loadObject;
    }

    // Add the UI GameObject to the dictionary
    public void AddUI<T>(GameObject go)
    {
        string className = typeof(T).Name;
        
        // If the UI exists, update it; otherwise, add it
        if (UIList.ContainsKey(className))
            UIList[className] = go;
        else
            UIList.Add(className, go);
    }

    // Remove a specific UI from the dictionary
    public void RemoveUI<T>()
    {
        string className = typeof(T).Name;
        
        if (UIList.TryGetValue(className, out GameObject uiObject) && uiObject != null)
        {
            Destroy(uiObject);
            UIList.Remove(className);
        }
    }

    // Check if a UI exists in the list
    public bool IsUIExist<T>()
    {
        return UIList.ContainsKey(typeof(T).Name) && UIList[typeof(T).Name] != null;
    }

    #region Toast
    public void OpenToast(string toast, float timer = 3f, bool center = true)
    {
        UIToast toastUI = CreateUI<UIToast>();
        toastUI.SetToast(toast, timer, center);
    }
    #endregion
}
