
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
            var sourcePath = "Packages/okinawa.ishix.unity.jenkins.builder/Editor/Jenkinsfile.groovy";
            var destinationPath = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "Jenkinsfile.groovy");
            var hasDestinationPath = File.Exists(destinationPath);
            File.Copy(sourcePath, destinationPath, true);
            if (!hasDestinationPath)
            {
                Debug.Log($"Jenkinsfile.groovy was copied from {sourcePath} to {destinationPath}");
            }
        }
    }
}