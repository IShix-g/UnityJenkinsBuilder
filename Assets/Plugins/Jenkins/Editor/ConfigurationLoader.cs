
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Jenkins
{
    public class ConfigurationLoader
    {
        [InitializeOnLoadMethod]
        static void LoadConfigurationOnEditorStart()
        {
            var assetsPath = Application.dataPath;
            var rootPath = Directory.GetParent(assetsPath).ToString();
            var sourcePath = "Packages/Unity Jenkins Builder/Jenkinsfile.groovy";
            var destinationPath = Path.Combine(rootPath, "Jenkinsfile.groovy");
            var hasDestinationPath = File.Exists(destinationPath);
            File.Copy(sourcePath, destinationPath, true);
            if (!hasDestinationPath)
            {
                Debug.Log($"Jenkinsfile.groovy was copied from {sourcePath} to {destinationPath}");
            }
        }
    }
}