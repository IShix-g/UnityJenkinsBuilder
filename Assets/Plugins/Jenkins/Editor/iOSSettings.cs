
using System.Collections.Generic;
using UnityEditor;

namespace Unity.Jenkins
{
    public sealed class iOSSettings
    {
        public static void Validate(Dictionary<string, string> options)
        {
            if (!options.TryGetValue("provisioningProfileID", out string provisioningProfileID)
                || string.IsNullOrEmpty(provisioningProfileID))
            {
                Utils.PrintErrorLog("Missing argument -provisioningProfileID", 110);
            }

            if(!options.TryGetValue("provisioningProfileType", out string provisioningProfileType)
               || string.IsNullOrEmpty(provisioningProfileType))
            {
                Utils.PrintErrorLog("Missing argument -provisioningProfileID", 110);
            }
        }
        
        public static void Apply(Dictionary<string, string> options)
        {
            if (options.TryGetValue("versionCode", out var versionCode)
                && !string.IsNullOrEmpty(versionCode))
            {
                PlayerSettings.iOS.buildNumber = versionCode;
            }
            
            if (options.TryGetValue("provisioningProfileID", out var provisioningProfileID)
                && !string.IsNullOrEmpty(provisioningProfileID))
            {
                PlayerSettings.iOS.appleEnableAutomaticSigning = false;
                PlayerSettings.iOS.iOSManualProvisioningProfileID = provisioningProfileID;
            }
            
            if (options.TryGetValue("provisioningProfileType", out var provisioningProfileType))
            {
                PlayerSettings.iOS.iOSManualProvisioningProfileType = provisioningProfileType == "Development"
                    ? ProvisioningProfileType.Development
                    : ProvisioningProfileType.Distribution;
            }
        }
    }
}