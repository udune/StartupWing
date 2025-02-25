using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class LanguageManager : MonoBehaviour
{
    private class Language
    {
        public string Code;
        public List<string> Languages = new List<string>();
    }

    private static LanguageManager instance;

    private static LanguageManager Instance
    {
        get => instance == null ? instance = new GameObject(typeof(LanguageManager).Name).AddComponent<LanguageManager>() : instance;
    }

    private static List<Language> languages;
    private static List<string> countryCodes;
    private static Dictionary<string, List<Language>> npcMessage;

    // 0 : KR , 1 : EN,  2: JP
    private static int index = -1;
    private static bool initialized = false;

    public static int Index
    {
        get => index;
        set => SetLanguage(value);
    }

    const string LANGUAGE_KEY = "LANGUAGE_KEY_V1";

    public static event Action OnCreatedLanguage;
    public static event Action OnChangedLanguage;

    public static void Initialize(string url)
    {
        if (initialized)
            return;

        Instance.StartCoroutine(GetLanguageCorutine(url));

        IEnumerator GetLanguageCorutine(string _url)
        {
            // '/edit'의 존재 여부를 확인하고, 존재한다면 해당 부분을 기준으로 URL을 분리합니다.
            string url = "";
            string gid = "";

            if (_url.Contains("/edit"))
            {
                string[] parts = _url.Split(new string[] { "/edit" }, StringSplitOptions.None);
                url = parts[0];

                if (parts.Length > 1 && parts[1].Contains("#gid="))
                {
                    string[] subParts = parts[1].Split(new string[] { "#gid=" }, StringSplitOptions.None);
                    if (subParts.Length > 1)
                    {
                        gid = subParts[1];
                    }
                }
            }
            else
            {
                // '/edit'가 없는 경우, gid 파트만 분리합니다.
                if (_url.Contains("#gid="))
                {
                    string[] parts = _url.Split(new string[] { "#gid=" }, StringSplitOptions.None);
                    url = parts[0];
                    if (parts.Length > 1)
                    {
                        gid = parts[1];
                    }
                }
            }

            string URL = $"{url}/export?gid={gid}&format=tsv";

            using (UnityWebRequest www = UnityWebRequest.Get(URL))
            {
                yield return www.SendWebRequest();
                CreateLanguage(www.downloadHandler.text);
#if !UNITY_EDITOR
             //   InitializeLanguageTable(www.downloadHandler.text);
#endif
                initialized = true;
            }
        }
    }

    public static bool isInitialized()
    {
        return initialized;
    }

    public static string Get(string _index)
    {
        int code = -1;
        try
        {
            code = languages.FindIndex(item => item.Code == _index);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        if (code < 0)
            return "Wrong Code";

        return languages[code].Languages[index].Replace("<N>", "\n");
    }

    public static List<string> GetNPCMessageList(string sceneName)
    {
        List<string> messageList;
        List<Language> language;

        if (!npcMessage.TryGetValue(sceneName, out language))
        {
            language = languages.FindAll(o => o.Code.Contains(sceneName));
        }

        messageList = language.Select(o => o.Languages[index]).ToList();

        for (int i = 0; i < messageList.Count; i++)
        {
            if (index == 2) // 일본어 데이터 load시 불필요 공백 및 \r 추가 제거
            {
                messageList[i] = messageList[i].Replace("\r", "");
                messageList[i] = messageList[i].Replace(" ", "");
            }

            messageList[i] = messageList[i].Replace("<N>", "\n");
        }

        return messageList;
    }

    private static void SetLanguage(int value)
    {
        index = value;
        PlayerPrefs.SetInt(LANGUAGE_KEY, index);
#if !UNITY_EDITOR
      //  SaveLangToLocalStorage(index);
#endif
        OnChangedLanguage?.Invoke();
    }

    public static void SetLanguage(string countryCode)
    {
        index = countryCodes.IndexOf(countryCode) - 1;

        PlayerPrefs.SetInt(LANGUAGE_KEY, index);
#if !UNITY_EDITOR
      //  SaveLangToLocalStorage(index);
#endif
        OnChangedLanguage?.Invoke();
    }


    private static void CreateLanguage(string tsv)
    {
        languages = new List<Language>();
        countryCodes = new List<string>();
        npcMessage = new Dictionary<string, List<Language>>();

        // 행 갯수
        string[] row = tsv.Split('\n');
        int rowSize = row.Length;

        // 열 갯수 
        int columnSize = row[0].Split('\t').Length;

        for (int i = 0; i < rowSize; i++)
        {
            Language lang = new Language();

            string[] column = row[i].Split('\t');
            lang.Code = column[0];

            // 국가 코드 등록
            if (i < 1)
            {
                for (int k = 0; k < column.Length; k++)
                    countryCodes.Add(column[k].Trim());
            }

            // 언어 등록
            for (int j = 1; j < columnSize; j++)
                lang.Languages.Add(column[j]);

            languages.Add(lang);
        }

        // 언어인덱스 설정
        SetLanguageIndex();
        OnCreatedLanguage?.Invoke();
    }

    private static void SetLanguageIndex()
    {
        index = PlayerPrefs.GetInt(LANGUAGE_KEY, -1);

        // 기존에 설정한 언어가 없을 경우, DefaultLanguage(KR)로 세팅
        if (index < 0)
            SetLanguage(AppConfig.AppSettings.defaultLanguage);
    }
}