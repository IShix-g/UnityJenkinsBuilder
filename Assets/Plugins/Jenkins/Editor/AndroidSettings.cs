
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.Jenkins
{
    public sealed class AndroidSettings
    {
        public static void Validate(Dictionary<string, string> options)
        {
            if (!options.TryGetValue("androidExportType", out string androidExportType)
                || string.IsNullOrEmpty(androidExportType))
            {
                Utils.PrintErrorLog("Missing argument -androidExportType", 110);
            }
        }
        
        public static void Apply(Dictionary<string, string> options)
        {
            if (options.TryGetValue("versionCode", out var versionCode)
                && Int32.TryParse(versionCode, out var versionCodeInt))
            {
                PlayerSettings.Android.bundleVersionCode = versionCodeInt;
            }
            
            if (options.TryGetValue("androidKeystoreName", out string keystoreName)
                && !string.IsNullOrEmpty(keystoreName))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystoreName;
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
            }
            
            if (options.TryGetValue("androidKeystorePass", out string keystorePass) &&
                !string.IsNullOrEmpty(keystorePass))
            {
                PlayerSettings.Android.keystorePass = keystorePass;
            }

            if (options.TryGetValue("androidKeyaliasName", out string keyaliasName) &&
                !string.IsNullOrEmpty(keyaliasName))
            {
                PlayerSettings.Android.keyaliasName = keyaliasName;
            }

            if (options.TryGetValue("androidKeyaliasPass", out string keyaliasPass) &&
                !string.IsNullOrEmpty(keyaliasPass))
            {
                PlayerSettings.Android.keyaliasPass = keyaliasPass;
            }

            if (options.TryGetValue("androidTargetSdkVersion", out string androidTargetSdkVersion)
                && !string.IsNullOrEmpty(androidTargetSdkVersion))
            {
                var targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
                try
                {
                    targetSdkVersion = (AndroidSdkVersions)Enum.Parse(typeof(AndroidSdkVersions), androidTargetSdkVersion);
                }
                catch
                {
                    Utils.PrintErrorLog("Failed to parse androidTargetSdkVersion! Fallback to AndroidApiLevelAuto", 1);
                }
                PlayerSettings.Android.targetSdkVersion = targetSdkVersion;
            }
            
            if (options.TryGetValue("androidExportType", out var androidExportType)
                && !string.IsNullOrEmpty(androidExportType))
            {
                switch (androidExportType)
                {
                    case "androidStudioProject":
                        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                        EditorUserBuildSettings.buildAppBundle = false;
                        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
                        break;
                    case "androidAppBundle":
                        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                        EditorUserBuildSettings.buildAppBundle = true;
                        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;
                        break;
                    case "androidPackage":
                        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                        EditorUserBuildSettings.buildAppBundle = false;
                        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
                        break;
                }
            }
            
            if (options.TryGetValue("androidSymbolType", out var symbolType)
                && !string.IsNullOrEmpty(symbolType))
            {
                switch (symbolType)
                {
                    case "public":
                        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
                        break;
                    case "debugging":
                        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;
                        break;
                    case "none":
                        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
                        break;
                }
            }
            
            if (options.TryGetValue("gradleUseEmbedded", out var gradleUseEmbedded)
                && !string.IsNullOrEmpty(gradleUseEmbedded))
            {
                EditorPrefs.SetBool("GradleUseEmbedded", gradleUseEmbedded.ToLower().Trim() == "true");
            }
        }
    }
}