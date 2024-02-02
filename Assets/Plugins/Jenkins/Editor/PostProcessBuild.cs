
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Unity.Jenkins
{
    public class PostProcessBuild
    {
        [PostProcessBuild]
        public static void Start(BuildTarget buildTarget, string pathToProject)
        {
#if UNITY_IOS
            if (buildTarget == BuildTarget.iOS)
            {
                var plistPath = pathToProject + "/Info.plist";
                var plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));
                var rootDict = plist.root;
                rootDict.SetString("ITSAppUsesNonExemptEncryption", "false");
                File.WriteAllText(plistPath, plist.WriteToString());
            }
#endif
        }
    }
}