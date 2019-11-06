using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace AssetBundleBuilder.DataSource
{
    public class JsonDataSource : DataSource
    {
        [Serializable]
        protected class AssetDatabaseAsset
        {
            public string path;
            public string bundleName;
            public string variantName;
        }

        protected class JsonAssetDatabase
        {
            public List<AssetDatabaseAsset> assets;
        }

        protected Dictionary<string, AssetDatabaseAsset> m_Assets;
        protected Dictionary<string, HashSet<string>> m_Bundles;

        public static List<DataSource> CreateDataSources()
        {
            var op = new JsonDataSource();
            op.Init();
            op.Load();

            var retList = new List<DataSource>();
            retList.Add(op);
            return retList;
        }

        public string Name {
            get {
                return "JsonDataSource";
            }
        }

        public string ProviderName {
            get {
                return "JsonDataSource";
            }
        }

        public string[] GetAssetPathsFromAssetBundle (string assetBundleName)
        {
            if (m_Bundles.ContainsKey(assetBundleName))
            {
                return m_Bundles[assetBundleName].ToArray();
            }
            return new string[0];
        }

        public string GetAssetBundleName(string assetPath) {
            if (!m_Assets.ContainsKey(assetPath)) {
                return string.Empty;
            }
            AssetDatabaseAsset item = m_Assets[assetPath];

            var bundleName = item.bundleName;
            if (item.variantName.Length > 0) {
                bundleName = bundleName + "." + item.variantName;
            }
            return bundleName;
        }

        public string GetImplicitAssetBundleName(string assetPath) {
            if(m_Assets.ContainsKey(assetPath))
            {
                return m_Assets[assetPath].bundleName;
            }
            return null;
        }

        public string[] GetAllAssetBundleNames() {
            return m_Bundles.Keys.ToArray();
        }

        public bool IsReadOnly() {
            return false;
        }

        public void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName) {
            if (m_Assets.ContainsKey(assetPath))
            {
                AssetDatabaseAsset item = m_Assets[assetPath];

                var oldBundleName = item.bundleName;

                if (string.IsNullOrEmpty(bundleName))
                {
                    //把asset从bundle中移除。
                    if (m_Bundles.ContainsKey(oldBundleName))
                    {
                        m_Bundles[oldBundleName].Remove(assetPath);
                    }
                    //asset的bundle name为空，则表示删除asset.
                    m_Assets.Remove(assetPath);
                }
                else
                {
                    if (oldBundleName != bundleName)
                    {
                        //把asset从bundle中移除。
                        if (m_Bundles.ContainsKey(oldBundleName))
                        {
                            m_Bundles[oldBundleName].Remove(assetPath);
                        }

                        item.bundleName = bundleName;

                        if (!m_Bundles.ContainsKey(bundleName))
                        {
                            m_Bundles[bundleName] = new HashSet<string>();
                        }

                        m_Bundles[bundleName].Add(assetPath);
                    }

                    item.variantName = variantName;
                }
            }
            else if (!string.IsNullOrEmpty(bundleName))
            {
                AssetDatabaseAsset item = new AssetDatabaseAsset();
                item.path = assetPath;
                item.bundleName = bundleName;
                item.variantName = variantName;

                m_Assets[assetPath] = item;

                if (!m_Bundles.ContainsKey(bundleName))
                {
                    m_Bundles[bundleName] = new HashSet<string>();
                }

                m_Bundles[bundleName].Add(assetPath);
            }
            
            Save();
        }

        public void RemoveAssetBundleNameAndVariant(string assetPath, string bundleName, string variantName)
        {
            SetAssetBundleNameAndVariant(assetPath, null, null);
        }

        public void RemoveUnusedAssetBundleNames()
        {
            List<string> bundleNames = m_Bundles.Keys.ToList();
            foreach(string bundleName in bundleNames)
            {
                if (m_Bundles[bundleName].Count == 0)
                {
                    m_Bundles.Remove(bundleName);
                }
            }
        }

        public bool CanSpecifyBuildTarget { 
            get { return true; } 
        }
        public bool CanSpecifyBuildOutputDirectory { 
            get { return true; } 
        }

        public bool CanSpecifyBuildOptions { 
            get { return true; } 
        }

        public bool BuildAssetBundles (BuildInfo info) {

            List<string> bundleNames = m_Bundles.Keys.ToList();

            List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
            foreach (string bundleName in bundleNames)
            {
                if (m_Bundles[bundleName].Count > 0)
                {
                    AssetBundleBuild build = new AssetBundleBuild();
                    build.assetBundleName = bundleName;
                    build.assetNames = m_Bundles[bundleName].ToArray();
                    builds.Add(build);
                }
            }
            
            if (!Directory.Exists(info.outputDirectory))
            {
                Directory.CreateDirectory(info.outputDirectory);
            }

            var buildManifest = BuildPipeline.BuildAssetBundles(info.outputDirectory,builds.ToArray(), info.options, info.buildTarget);
            if (buildManifest == null)
                return false;

            //不能消除Manifest，否则无法增量构建。可以在最终目录把Manifest删除
            //DatabaseUtil.ClearTempManifest(info.outputDirectory);

            DatabaseUtil.SaveBundleManifest(buildManifest, info.outputDirectory,this);

            foreach (var assetBundleName in buildManifest.GetAllAssetBundles())
            {
                if (info.onBuild != null)
                {
                    info.onBuild(assetBundleName);
                }
            }
            return true;
        }

        public void Init()
        {
            m_Assets = new Dictionary<string, AssetDatabaseAsset>();
            m_Bundles = new Dictionary<string, HashSet<string>>();
        }

        void BuildData(JsonAssetDatabase db)
        {
            if (db != null)
            {
                foreach (AssetDatabaseAsset item in db.assets)
                {
                    m_Assets[item.path] = item;
                    if (!m_Bundles.ContainsKey(item.bundleName))
                    {
                        m_Bundles[item.bundleName] = new HashSet<string>();
                    }
                    m_Bundles[item.bundleName].Add(item.path);
                }
            }
        }

        public void Load()
        {
            var dataPath = System.IO.Path.GetFullPath(".");
            var dataFile = Path.Combine(dataPath, AssetBundleConstans.JsonDatabaseFile);
            if (File.Exists(dataFile))
            {
                string cnt = File.ReadAllText(dataFile);
                JsonAssetDatabase db = JsonUtility.FromJson<JsonAssetDatabase>(cnt);
                BuildData(db);
            }
        }

        public void Save()
        {
            JsonAssetDatabase db = new JsonAssetDatabase();
            db.assets = m_Assets.Values.ToList();
            string cnt = JsonUtility.ToJson(db);

            var dataPath = Path.GetFullPath(".");
            var dataFile = Path.Combine(dataPath, AssetBundleConstans.JsonDatabaseFile);

            if (!Directory.Exists(Path.GetDirectoryName(dataFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dataFile));
            }

            File.WriteAllText(dataFile, cnt);
        }
    }
}
