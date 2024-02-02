
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Jenkins
{
    public static class FileExtension
    {
        public static void Write(this string path, BuildSnapshot snapshot)
        {
            File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));
            AssetDatabase.Refresh ();
        }
        
        public static void CreateFolder(this string path)
        {
            if (!path.Contains("/")
                || !path.StartsWith("Assets/"))
            {
                Console.WriteLine("::error:: パスエラー path : " + path);
                EditorApplication.Exit(120);
                return;
            }
            
            var objs = path.Split("/");
            var prev = objs[0];
            for (var i = 1; i < objs.Length; i++)
            {
                var obj = objs[i];
                if (!AssetDatabase.IsValidFolder(prev + "/" + obj)
                    && string.IsNullOrEmpty(Path.GetExtension(obj)))
                {
                    AssetDatabase.CreateFolder(prev, obj);
                }
                prev += "/" + obj;
            }
        }

        public static void Delete(this string path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh ();
        }
    }
}