
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

#if INSTALLED_ADDRESSABLE
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
#endif

namespace Unity.Jenkins
{
    public static class Builder
    {
        const string _devBuildSymbol = "JENKINS_DEBUG";

        [PublicAPI]
        public static void Build()
        {
            var options = Utils.GetValidatedOptions(true);
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            var buildOptions = BuildOptions.None;
            foreach (var buildOptionString in Enum.GetNames(typeof(BuildOptions)))
            {
                if (options.ContainsKey(buildOptionString))
                {
                    var buildOptionEnum = (BuildOptions) Enum.Parse(typeof(BuildOptions), buildOptionString);
                    buildOptions |= buildOptionEnum;
                    Console.WriteLine("add buildOption" + buildOptionEnum);
                }
            }

            var buildTarget = (BuildTarget)Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            var buildTargetGroup = buildTarget == BuildTarget.iOS ? BuildTargetGroup.iOS : BuildTargetGroup.Android;
            
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    AndroidSettings.Apply(options);
                    break;
                case BuildTarget.iOS:
                    iOSSettings.Apply(options);
                    break;
                default:
                    Utils.PrintErrorLog("対応できない、または存在しないBuildTarget : " + options["buildTarget"], 110);
                    break;
            }
            
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = options["buildPath"],
                target = buildTarget,
                options = buildOptions
            };

            // Development Buildのみシンボル指定する
            if ((buildOptions & BuildOptions.Development) != 0)
            {
                buildPlayerOptions.extraScriptingDefines = new[] {_devBuildSymbol};
                Utils.PrintLog("Add Symbol : #" + _devBuildSymbol);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);
            }
            
            if (options.TryGetValue("buildVersion", out var buildVersion)
                && !string.IsNullOrEmpty(buildVersion)
                && buildVersion != "none")
            {
                PlayerSettings.bundleVersion = buildVersion;
            }
            
            // Addressable
#if INSTALLED_ADDRESSABLE
            if (AddressableAssetSettingsDefaultObject.Settings != default
#if UNITY_2021_2_OR_NEWER
                && (!IsAddressableBuildWithPlayer()
                    || options.TryGetValue("buildAddressable", out var buildAddressable)
                       && buildAddressable.ToLower().Contains("true"))
#else
                options.TryGetValue("buildAddressable", out var buildAddressable)
                && buildAddressable.ToLower().Contains("true")
#endif
            )
            {
                AddressableAssetSettings.CleanPlayerContent();
                AddressableAssetSettings.BuildPlayerContent();
            }
#endif
            
            if((buildOptions & BuildOptions.Development) != 0)
            {
               BuildSnapshot.ResourceAbsolutePath.CreateFolder();
                var snapShot = Create(buildPlayerOptions.target, options);
                BuildSnapshot.ResourceAbsolutePath.Write(snapShot);
                Utils.PrintLog("Build Snapshot path : " + BuildSnapshot.ResourceAbsolutePath);
            }
            
            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = buildReport.summary;
            
            if (options.TryGetValue("buildSnapshotPath", out var buildSnapshotPath)
                && !string.IsNullOrEmpty(buildSnapshotPath))
            {
                var snapShot = Create(buildPlayerOptions.target, summary, options);
                (Application.dataPath + "/../" + buildSnapshotPath).Write(snapShot);
                Utils.PrintLog("Build Snapshot path : " + buildSnapshotPath);
            }
            
            if (options.TryGetValue("appIconPath", out var appIconPath)
                && !string.IsNullOrEmpty(appIconPath))
            {
                var iconPath = Application.dataPath + "/../" + appIconPath;
                var icon = PlayerSettings.GetIconsForTargetGroup(buildTargetGroup).FirstOrDefault();
                if (icon == default)
                {
                    icon = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown).FirstOrDefault();
                }
                if (icon != default)
                {
                    appIconPath.CreateFolder();
                    File.WriteAllBytes(iconPath, icon.EncodeToPNG());
                }
                Utils.PrintLog("Export Icon : " + (icon != default ? "Success" : "Failed") + " path : " + iconPath);
            }
            
            Utils.ReportSummary(summary);
            Utils.ExitWithResult(summary.result);
        }
        
        /// <summary>
        /// 実際にビルドはしないが、直前まで走らせて結果を確認する
        /// </summary>
        public static void CheckBuildStatus()
        {
            Utils.GetValidatedOptions(true);
        }

        static bool IsAddressableBuildWithPlayer()
        {
#if INSTALLED_ADDRESSABLE && UNITY_2021_2_OR_NEWER
            return AddressableAssetSettingsDefaultObject.Settings.BuildAddressablesWithPlayerBuild ==
                   AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer
                   || AddressableAssetSettingsDefaultObject.Settings.BuildAddressablesWithPlayerBuild ==
                      AddressableAssetSettings.PlayerBuildOption.PreferencesValue
                      && EditorPrefs.GetBool("Addressables.BuildAddressablesWithPlayerBuild", true);
#else
                return false;
#endif
        }
        
        public static BuildSnapshot Create(BuildTarget target, Dictionary<string, string> options)
        {
            var obj = new BuildSnapshot();
            
            if (options.TryGetValue("buildNumber", out var buildNumber)
                && Int32.TryParse(buildNumber, out var buildNumberInt))
            {
                obj.BuildNumber = buildNumberInt;
            }

            obj.BuildTarget = target.ToString();
            obj.ProductName = Application.productName;
            obj.BundleId = Application.identifier;
            obj.Version = Application.version;
            obj.VersionCode = target == BuildTarget.Android
                ? PlayerSettings.Android.bundleVersionCode
                : Int32.Parse(PlayerSettings.iOS.buildNumber);
            obj.UnityVersion = Application.unityVersion;
            obj.JobName = options.GetValueOrDefault("jobName", "");
            obj.JobUrl = options.GetValueOrDefault("jobUrl", "");
            obj.BuildUrl = options.GetValueOrDefault("buildURL", "");
            obj.CommitId = options.GetValueOrDefault("commitId", "");
            obj.CommitUrl = options.GetValueOrDefault("commitUrl", "");
            obj.Branch = options.GetValueOrDefault("branch", "");

#if UNITY_IOS
            obj.XcodeVersion = options.GetValueOrDefault("xcodeVersion", "");
            obj.ProvisioningProfileType = options.GetValueOrDefault("provisioningProfileType", "");
            obj.ScriptingBackEnd = PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS).ToString();
#elif UNITY_ANDROID
            obj.MinimumApiLevel = PlayerSettings.Android.minSdkVersion.ToString();
            obj.TargetApiLevel = PlayerSettings.Android.targetSdkVersion.ToString();
            obj.ScriptingBackEnd = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android).ToString();
#endif
            return obj;
        }
        
        static BuildSnapshot Create(BuildTarget target, BuildSummary summary, Dictionary<string, string> options)
        {
            var obj = Create(target, options);
            obj.BuildStartTime = TimeZoneInfo.ConvertTimeFromUtc(summary.buildStartedAt, TimeZoneInfo.Local).ToString("yyyy/MM/dd HH:mm:ss");
            obj.BuildEndTime = TimeZoneInfo.ConvertTimeFromUtc(summary.buildEndedAt, TimeZoneInfo.Local).ToString("yyyy/MM/dd HH:mm:ss");
            obj.BuildTotalTime = summary.totalTime.ToString();
            obj.TotalWarnings = summary.totalWarnings;
            obj.TotalErrors = summary.totalErrors;
            return obj;
        }
    }
}