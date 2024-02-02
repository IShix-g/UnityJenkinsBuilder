
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;

namespace Unity.Jenkins.Test
{
    public class BuildSnapShotTest1
    {
        [Test]
        public void BuildTest()
        {
            // create
            BuildSnapshot.ResourceAbsolutePath.CreateFolder();
            var snapShot = Builder.Create(BuildTarget.iOS, new Dictionary<string, string>());
            BuildSnapshot.ResourceAbsolutePath.Write(snapShot);
            // load
            var asset = BuildSnapshot.Load();
            Debug.Log(JsonUtility.ToJson(snapShot, true));
            Assert.IsTrue(asset.IsValid());
            // delete
            BuildSnapshot.ResourceRootPath.Delete();
        }
    }
}