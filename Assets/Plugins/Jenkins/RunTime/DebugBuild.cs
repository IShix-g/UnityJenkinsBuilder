#if JENKINS_DEBUG
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Jenkins
{
    public sealed class DebugBuild : MonoBehaviour
    {
        /// <summary>
        /// ゲームが起動できる期間
        /// </summary>
        const int _gameActivationPeriod = 7 * 2;

        const string _lastSavedTimeKey = "DebugBuild_LastSavedTimeKey";
        const string _currentVersionKey = "DebugBuild_CurrentVersionKey";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (s_instance == default)
            {
                s_instance = new GameObject("DebugBuild", typeof(DebugBuild));
                DontDestroyOnLoad(s_instance);
            }
        }

        static GameObject s_instance;

        DateTime LastSavedTime
        {
            get
            {
                if (PlayerPrefs.HasKey(_lastSavedTimeKey)
                    && DateTime.TryParse(PlayerPrefs.GetString(_lastSavedTimeKey), out var dateTimeValue))
                    return dateTimeValue;

                var time = DateTime.Now;
                LastSavedTime = time;
                return time;
            }
            set
            {
                var dateTimeString = value.ToString("o");
                PlayerPrefs.SetString(_lastSavedTimeKey, dateTimeString);
            }
        }

        bool _isShownEndPeriodDialog;

        int CurrentVersion
        {
            get => PlayerPrefs.GetInt(_currentVersionKey);
            set => PlayerPrefs.SetInt(_currentVersionKey, value);
        }

        void Start()
        {
            var snapShot = BuildSnapshot.Load();
            if (!snapShot.IsValid())
            {
              Debug.LogWarning(typeof(DebugBuild) + " snapShot is empty.");
              CreateEndPeriodDialog();
              return;
            }
            
            if (snapShot.BuildNumber != CurrentVersion)
            {
              CurrentVersion = snapShot.BuildNumber;
              LastSavedTime = DateTime.Now;
            }
            
            StartPeriodConfirmation();
            Debug.Log(typeof(DebugBuild) + " created.");
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus
                && !_isShownEndPeriodDialog)
                StartPeriodConfirmation();
        }

        void StartPeriodConfirmation()
        {
            if (_isShownEndPeriodDialog
                || DateTime.Now - LastSavedTime < TimeSpan.FromDays(_gameActivationPeriod))
            {
                return;
            }

            CreateEndPeriodDialog();
        }

        void CreateEndPeriodDialog()
        {
            _isShownEndPeriodDialog = true;

            // canvas
            var canvas = s_instance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            // image
            var image = new GameObject("Image").AddComponent<Image>();
            image.transform.SetParent(s_instance.transform, false);
            image.color = Color.white;
            image.raycastTarget = true;
            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
            // text
            var text = new GameObject("Text").AddComponent<Text>();
            text.transform.SetParent(image.transform, false);
            text.text = Application.systemLanguage == SystemLanguage.Japanese
                ? "試用期間は終了しました。"
                : "Trial period has ended.";
            text.resizeTextForBestFit = true;
            text.color = Color.black;
            var fontName = "Arial.ttf";
#if UNITY_2022_1_OR_NEWER
            fontName = "LegacyRuntime.ttf";
#endif
            text.font = Resources.GetBuiltinResource(typeof(Font), fontName) as Font;
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            var textRectTransform = text.rectTransform;
            textRectTransform.anchorMin = new Vector2(0, 0);
            textRectTransform.anchorMax = new Vector2(1, 1);
            textRectTransform.offsetMin = textRectTransform.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            if (s_instance == gameObject) s_instance = default;
        }
    }
}
#endif