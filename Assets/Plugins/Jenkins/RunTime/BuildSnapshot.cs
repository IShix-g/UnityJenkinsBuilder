
using System;
using UnityEngine;

namespace Unity.Jenkins
{
    [Serializable]
    public struct BuildSnapshot
    {
        public const string ResourceRootPath = "Assets/__Jenkins__/";
        public const string ResourceAbsolutePath = ResourceRootPath + "Resources/BuildSnapshot.json";
        public const string ResourcesPath = "BuildSnapshot";

        public string BuildTarget;
        public string ProductName;
        public string BundleId;
        public int BuildNumber;
        public string Version;
        public int VersionCode;
        
        public string BuildStartTime;
        public string BuildEndTime;
        public string BuildTotalTime;
        public int TotalWarnings;
        public int TotalErrors;
        public string UnityVersion;
        
        public string JobName;
        public string JobUrl;
        public string BuildUrl;
        
        public string CommitId;
        public string CommitUrl;
        public string Branch;

#if UNITY_IOS
        public string XcodeVersion;
        public string ProvisioningProfileType;
#elif UNITY_ANDROID
        public string TargetApiLevel;
        public string MinimumApiLevel;
#endif
        public string ScriptingBackEnd;

        public bool IsValid() => !string.IsNullOrEmpty(ProductName) && !string.IsNullOrEmpty(BundleId) && !string.IsNullOrEmpty(Version);
        
        public static BuildSnapshot Load()
        {
            var asset = LoadTextAsset();
            return asset != default ? JsonUtility.FromJson<BuildSnapshot>(asset.text) : default;
        }
        
        public static TextAsset LoadTextAsset() => Resources.Load<TextAsset>(ResourcesPath);
    }
}