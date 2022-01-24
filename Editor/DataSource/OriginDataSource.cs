//using System;
//using UnityEngine;
//using UnityEditor;
//using UnityEngine.Assertions;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor.IMGUI.Controls;

//namespace AssetBundleBuilder.DataSource
//{
//    internal class OriginDataSource : DataSource
//    {
//        public static List<DataSource> CreateDataSources()
//        {
//            var op = new OriginDataSource();
//            var retList = new List<DataSource>();
//            retList.Add(op);
//            return retList;
//        }

//        public string Name {
//            get {
//                return "Default";
//            }
//        }

//        public string ProviderName {
//            get {
//                return "Built-in";
//            }
//        }

//        public string[] GetAssetPathsFromAssetBundle (string assetBundleName) {
//            return AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
//        }

//        public string GetAssetBundleName(string assetPath) {
//            var importer = AssetImporter.GetAtPath(assetPath);
//            if (importer == null) {
//                return string.Empty;
//            }
//            var bundleName = importer.assetBundleName;
//            if (importer.assetBundleVariant.Length > 0) {
//                bundleName = bundleName + "." + importer.assetBundleVariant;
//            }
//            return bundleName;
//        }

//        public string GetImplicitAssetBundleName(string assetPath) {
//            return AssetDatabase.GetImplicitAssetBundleName (assetPath);
//        }

//        public string[] GetAllAssetBundleNames() {
//            return AssetDatabase.GetAllAssetBundleNames ();
//        }

//        public string[] GetAllAssetPaths()
//        {
//            return AssetDatabase.GetAllAssetPaths();
//        }

//        public bool IsReadOnly() {
//            return false;
//        }

//        public void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName) {
//            AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(bundleName, variantName);
//        }

//        public void RemoveAssetBundleNameAndVariant(string assetPath, string bundleName, string variantName)
//        {
//            SetAssetBundleNameAndVariant(assetPath,null, null);
//        }

//        public void RemoveUnusedAssetBundleNames() {
//            AssetDatabase.RemoveUnusedAssetBundleNames ();
//        }

//        public bool CanSpecifyBuildTarget { 
//            get { return true; } 
//        }
//        public bool CanSpecifyBuildOutputDirectory { 
//            get { return true; } 
//        }

//        public bool CanSpecifyBuildOptions { 
//            get { return true; } 
//        }

//        public bool BuildAssetBundles (BuildInfo info) {
//            if(info == null)
//            {
//                Debug.Log("Error in build");
//                return false;
//            }

//            var buildManifest = BuildPipeline.BuildAssetBundles(info.outputDirectory, info.options, info.buildTarget);

//            if (buildManifest == null)
//                return false;

//            DatabaseUtil.SaveBundleManifest(buildManifest, info,this);

//            foreach (var assetBundleName in buildManifest.GetAllAssetBundles())
//            {
//                if (info.onBuild != null)
//                {
//                    info.onBuild(assetBundleName);
//                }
//            }
//            return true;
//        }
//    }
//}
