using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class ServerManager : Singleton<ServerManager>
{
    CoroutineQueue corQueue = new CoroutineQueue();

    public void APIPostRequest<T>(string url, Hashtable postData, Action<T> act = null) 
    {
        corQueue.StartCoroutines(WebPost(url, postData, act), StartCoroutine);
    }

    public void APIGetRequest<T>(string url, Action<T> act = null)
    {
        corQueue.StartCoroutines(WebGet(url, act), StartCoroutine);
    }

    #region Web
    public IEnumerator WebPost<T>(string _url, Hashtable _postData, Action<T> _act = null)
    {
        var jsonString = JsonConvert.SerializeObject(_postData);

        using UnityWebRequest www = UnityWebRequest.PostWwwForm(_url, jsonString);
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonString);
        www.uploadHandler.Dispose();
        www.uploadHandler = new UploadHandlerRaw(jsonToSend);
        www.SetRequestHeader("Content-Type","application/json");

        yield return www.SendWebRequest();

        if (IsWebRequestError(www.result))
            Debug.Log($"<color=red>Server Error : {www.error}</color> // {_url}");
        else
        {
            Debug.Log($"<color=yellow>Server Responce : {_url}</color> // {www.downloadHandler.text}");
            var data = JsonUtility.FromJson<T>(www.downloadHandler.text);
            _act?.Invoke(data);
        }
    }

    public IEnumerator WebGet<T>(string _url, Action<T> _act = null)
    {
        using UnityWebRequest www = UnityWebRequest.Get(_url);
        yield return www.SendWebRequest();

        if (IsWebRequestError(www.result))
            Debug.Log($"<color=red>Server Error : {www.error}</color> // {_url}");
        else
        {
            Debug.Log($"<color=yellow>Server Responce : {_url}</color> // {www.downloadHandler.text}");
            var data = JsonUtility.FromJson<T>(www.downloadHandler.text);
            _act?.Invoke(data);
        }
    }
    #endregion

    bool IsWebRequestError(UnityWebRequest.Result _result)
    {
        return _result.Equals(UnityWebRequest.Result.ProtocolError) ||
                _result.Equals(UnityWebRequest.Result.ConnectionError);
    }
}