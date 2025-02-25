using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;
using UnityEngine.EventSystems;


public class UIManager : Singleton<UIManager>
{
 
    const string UIString = "UI";

    Dictionary<string, GameObject> UIList = new Dictionary<string, GameObject>();

    public T GetUI<T>(Transform parent = null) where T : Component
    {
        if (UIList.ContainsKey(typeof(T).Name) && UIList[typeof(T).Name] != null)
            return UIList[typeof(T).Name].GetComponent<T>();
        else
            return CreateUI<T>(parent);
    }
    public T CreateUI<T>(Transform parent = null)
    {
        string className = typeof(T).Name;

        if (UIList.ContainsKey(className))
        {
            UIList.Remove(className);
        }

        GameObject loadObject = LoadUI(className);
        GameObject go = Instantiate(loadObject, parent);

        AddUI<T>(go);

        return UIList[className].GetComponent<T>();

    }
    GameObject LoadUI(string className)
    {

        GameObject loadObject = null;

        string path = System.IO.Path.Combine(UIString, className);

        try
        {
            loadObject = Resources.Load<GameObject>(path);

        }
        catch (Exception e)
        {
            Debug.Log("Failed Load UI :" + e.Message);
        }

        return loadObject;

    }
    public void AddUI<T>(GameObject go)
    {
        string className = typeof(T).Name;
        if (UIList.ContainsKey(className))
        {
            UIList[className] = go;
        }
        else
        {
            UIList.Add(className, go);
        }
        
    }
    public void RemoveUI<T>()
    {
        string className = typeof(T).Name;
        if (UIList.ContainsKey(className))
        {
            if (UIList[className].gameObject != null)
            {
                Destroy(UIList[className]);
                UIList.Remove(className);
            }
        }
    }
    public bool isUIExist<T>()
    {
        if (UIList.ContainsKey(typeof(T).Name) && UIList[typeof(T).Name] != null)
            return true;
        else
            return false;
    }


    #region Toast
    public void OpenToast(string toast, float timer = 3f, bool Center = true)
    {
        UIToast Toast = null;

        Toast = CreateUI<UIToast>();

        Toast.SetToast(toast, timer, Center);
    }
    #endregion
  
   

   
}
