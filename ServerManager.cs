using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class ServerManager : Singleton<ServerManager>
{
    private CoroutineQueue corQueue = new CoroutineQueue();

    public void APIPostRequest<T>(string url, Hashtable postData, Action<T> callback = null) 
    {
        corQueue.StartCoroutines(ExecuteWebRequest<T>(url, postData, WebRequestType.POST, callback), StartCoroutine);
    }

    public void APIGetRequest<T>(string url, Action<T> callback = null)
    {
        corQueue.StartCoroutines(ExecuteWebRequest<T>(url, null, WebRequestType.GET, callback), StartCoroutine);
    }

    #region Web Requests
    private enum WebRequestType
    {
        GET,
        POST
    }

    private IEnumerator ExecuteWebRequest<T>(string url, Hashtable postData, WebRequestType requestType, Action<T> callback)
    {
        using UnityWebRequest www = CreateWebRequest(url, postData, requestType);

        yield return www.SendWebRequest();

        if (IsWebRequestError(www.result))
        {
            Debug.LogError($"<color=red>Server Error : {www.error}</color> // {url}");
        }
        else
        {
            Debug.Log($"<color=yellow>Server Response : {url}</color> // {www.downloadHandler.text}");
            var data = JsonUtility.FromJson<T>(www.downloadHandler.text);
            callback?.Invoke(data);
        }
    }

    private UnityWebRequest CreateWebRequest(string url, Hashtable postData, WebRequestType requestType)
    {
        UnityWebRequest www;

        if (requestType == WebRequestType.POST)
        {
            var jsonString = JsonConvert.SerializeObject(postData);
            byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonString);

            www = UnityWebRequest.PostWwwForm(url, string.Empty);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.SetRequestHeader("Content-Type", "application/json");
        }
        else
        {
            www = UnityWebRequest.Get(url);
        }

        return www;
    }
    #endregion

    private bool IsWebRequestError(UnityWebRequest.Result result)
    {
        return result == UnityWebRequest.Result.ProtocolError || result == UnityWebRequest.Result.ConnectionError;
    }
}
