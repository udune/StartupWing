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
        public List<string> Translations = new List<string>();
    }

    private static LanguageManager instance;
    private static bool initialized = false;

    private static List<Language> languages;
    private static List<string> countryCodes;
    private static Dictionary<string, List<Language>> npcMessages;

    // 0: KR, 1: EN, 2: JP
    private static int currentLanguageIndex = -1;

    private const string LANGUAGE_KEY = "LANGUAGE_KEY_V1";

    public static event Action OnLanguageCreated;
    public static event Action OnLanguageChanged;

    private static LanguageManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject(typeof(LanguageManager).Name).AddComponent<LanguageManager>();
            }
            return instance;
        }
    }

    public static int CurrentLanguageIndex
    {
        get => currentLanguageIndex;
        set => SetLanguage(value);
    }

    public static bool IsInitialized => initialized;

    public static void Initialize(string url)
    {
        if (initialized)
            return;

        Instance.StartCoroutine(FetchLanguageData(url));
    }

    private static IEnumerator FetchLanguageData(string url)
    {
        string baseUrl = url.Contains("/edit") ? url.Split(new[] { "/edit" }, StringSplitOptions.None)[0] : url;
        string gid = ExtractGidFromUrl(url);

        string languageUrl = $"{baseUrl}/export?gid={gid}&format=tsv";

        using (UnityWebRequest www = UnityWebRequest.Get(languageUrl))
        {
            yield return www.SendWebRequest();
            ParseLanguageData(www.downloadHandler.text);
            initialized = true;
        }
    }

    private static string ExtractGidFromUrl(string url)
    {
        if (url.Contains("#gid="))
        {
            return url.Split(new[] { "#gid=" }, StringSplitOptions.None)[1];
        }
        return string.Empty;
    }

    private static void ParseLanguageData(string tsvData)
    {
        languages = new List<Language>();
        countryCodes = new List<string>();
        npcMessages = new Dictionary<string, List<Language>>();

        var rows = tsvData.Split('\n');
        var columnCount = rows[0].Split('\t').Length;

        for (int i = 0; i < rows.Length; i++)
        {
            var columns = rows[i].Split('\t');
            var language = new Language { Code = columns[0] };

            if (i == 0) // 첫 번째 행은 국가 코드
            {
                countryCodes.AddRange(columns.Skip(1).Select(code => code.Trim()));
            }
            else
            {
                language.Translations.AddRange(columns.Skip(1).Take(columnCount - 1));
            }

            languages.Add(language);
        }

        SetLanguageIndex();
        OnLanguageCreated?.Invoke();
    }

    private static void SetLanguageIndex()
    {
        currentLanguageIndex = PlayerPrefs.GetInt(LANGUAGE_KEY, -1);
        if (currentLanguageIndex < 0)
        {
            SetLanguage(AppConfig.AppSettings.defaultLanguage);
        }
    }

    private static void SetLanguage(int languageIndex)
    {
        currentLanguageIndex = languageIndex;
        PlayerPrefs.SetInt(LANGUAGE_KEY, currentLanguageIndex);
        OnLanguageChanged?.Invoke();
    }

    public static void SetLanguage(string countryCode)
    {
        currentLanguageIndex = countryCodes.IndexOf(countryCode) - 1;
        PlayerPrefs.SetInt(LANGUAGE_KEY, currentLanguageIndex);
        OnLanguageChanged?.Invoke();
    }

    public static string GetTranslation(string languageCode)
    {
        var language = languages.FirstOrDefault(lang => lang.Code == languageCode);
        if (language == null)
        {
            Debug.LogError("Invalid Language Code");
            return "Invalid Language Code";
        }
        return language.Translations[currentLanguageIndex].Replace("<N>", "\n");
    }

    public static List<string> GetNpcMessagesForScene(string sceneName)
    {
        if (!npcMessages.TryGetValue(sceneName, out var sceneMessages))
        {
            sceneMessages = languages.Where(lang => lang.Code.Contains(sceneName)).ToList();
        }

        var messages = sceneMessages.Select(lang => lang.Translations[currentLanguageIndex]).ToList();
        return CleanMessages(messages);
    }

    private static List<string> CleanMessages(List<string> messages)
    {
        return messages.Select(message =>
        {
            if (currentLanguageIndex == 2) // 일본어 처리
            {
                message = message.Replace("\r", "").Replace(" ", "");
            }
            return message.Replace("<N>", "\n");
        }).ToList();
    }
}
