using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace AssetBundleBrowser.AssetBundleDataSource
{
    public class AssetDatabaseFlexibleDataSource : ABDataSource
    {
        [Serializable]
        public class Asset
        {
            public string path;
            public List<Bundle> bundles;

            public void AddBundle(Bundle bundle)
            {
                if (!bundles.Contains(bundle))
                {
                    bundles.Add(bundle);
                }
            }

            public void RemoveBundle(Bundle bundle)
            {
                if (bundles.Contains(bundle))
                {
                    bundles.Remove(bundle);
                }
            }
        }

        [Serializable]
        public class Bundle
        {
            public string name;
            public string variantName;
            public List<string> assets; 

            public string GetFullName()
            {
                return string.IsNullOrEmpty(variantName) ? name : (name + "." + variantName);
            }

            public void AddAsset(string asset)
            {
                if (!assets.Contains(asset))
                {
                    assets.Add(asset);
                }
            }

            public void RemoveAsset(string asset)
            {
                if (assets.Contains(asset))
                {
                    assets.Remove(asset);
                }
            }
        }

        public class Database
        {
            public List<Bundle> bundles;
        }

        protected Dictionary<string, Asset> m_Assets;
        protected Dictionary<string, Bundle> m_Bundles;

        public static List<ABDataSource> CreateDataSources()
        {
            var op = new AssetDatabaseFlexibleDataSource();
            op.Init();
            op.Load();

            var retList = new List<ABDataSource>();
            retList.Add(op);
            return retList;
        }

        public string Name {
            get {
                return "FlexibleDataSource";
            }
        }

        public string ProviderName {
            get {
                return "FlexibleDataSource";
            }
        }

        public static string GetBundleFullName(string name,string variantName)
        {
            return string.IsNullOrEmpty(variantName) ? name : (name + "." + variantName);
        }

        public string[] GetAssetPathsFromAssetBundle (string assetBundleName)
        {
            if (m_Bundles.ContainsKey(assetBundleName))
            {
                return m_Bundles[assetBundleName].assets.ToArray();
            }
            return new string[0];
        }

        public string GetAssetBundleName(string assetPath) {
            if (!m_Assets.ContainsKey(assetPath)) {
                return string.Empty;
            }

            Asset asset = m_Assets[assetPath];

            if(asset.bundles!=null && asset.bundles.Count > 0)
            {
                return asset.bundles[0].GetFullName();
            }

            return string.Empty;
        }

        public string GetImplicitAssetBundleName(string assetPath) {
            if (!m_Assets.ContainsKey(assetPath))
            {
                return string.Empty;
            }

            Asset asset = m_Assets[assetPath];

            if (asset.bundles != null && asset.bundles.Count > 0)
            {
                return asset.bundles[0].name;
            }

            return string.Empty;
        }

        public string[] GetAllAssetBundleNames() {
            return m_Bundles.Keys.ToArray();
        }

        public bool IsReadOnly() {
            return false;
        }

        public void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName) {

            if(string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            Bundle bundle=null;
            string fullName = GetBundleFullName(bundleName, variantName);
            if(!m_Bundles.TryGetValue(fullName, out bundle))
            {
                bundle = new Bundle();
                bundle.name = bundleName;
                bundle.variantName = variantName;
            }

            Asset asset = null;
            if (!m_Assets.TryGetValue(assetPath, out asset))
            {
                asset = new Asset();
                asset.path = assetPath;
                asset.bundles = new List<Bundle>();

                m_Assets[assetPath] = asset;
            }

            bundle.AddAsset(asset.path);

            asset.AddBundle(bundle);

            Save();
        }

        public void RemoveAssetBundleNameAndVariant(string assetPath, string bundleName, string variantName)
        {
            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            Bundle bundle = null;
            string fullName = GetBundleFullName(bundleName, variantName);
            if (!m_Bundles.TryGetValue(fullName, out bundle))
            {
                return;
            }

            Asset asset = null;
            if (!m_Assets.TryGetValue(assetPath, out asset))
            {
                return;
            }

            bundle.RemoveAsset(asset.path);

            asset.RemoveBundle(bundle);
        }

        public void RemoveUnusedAssetBundleNames()
        {
            List<string> bundleNames = m_Bundles.Keys.ToList();
            foreach(string bundleName in bundleNames)
            {
                if (m_Bundles[bundleName].assets.Count == 0)
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

        public bool BuildAssetBundles (ABBuildInfo info) {

            List<string> bundleNames = m_Bundles.Keys.ToList();

            List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
            foreach (string bundleName in bundleNames)
            {
                if (m_Bundles[bundleName].assets.Count > 0)
                {
                    AssetBundleBuild build = new AssetBundleBuild();
                    build.assetBundleName = bundleName;
                    build.assetNames = m_Bundles[bundleName].assets.ToArray();
                    builds.Add(build);
                }
            }

            var buildManifest = BuildPipeline.BuildAssetBundles(info.outputDirectory,builds.ToArray(), info.options, info.buildTarget);
            if (buildManifest == null)
                return false;

            DatabaseUtil.ClearTempManifest(info.outputDirectory);

            DatabaseUtil.SaveBundleManifest(buildManifest, info.outputDirectory);

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
            m_Assets = new Dictionary<string, Asset>();
            m_Bundles = new Dictionary<string, Bundle>();
        }

        void BuildData(Database db)
        {
            if (db != null)
            {
                foreach (Bundle bundle in db.bundles)
                {
                    foreach(string assetPath in bundle.assets)
                    {
                        Asset asset;
                        if (!m_Assets.TryGetValue(assetPath,out asset))
                        {
                            asset = new Asset();
                            asset.path = assetPath;
                            asset.bundles = new List<Bundle>();
                            m_Assets[assetPath] = asset;
                        }

                        asset.bundles.Add(bundle);
                    }
                    m_Bundles[bundle.GetFullName()] = bundle;
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
                Database db = JsonUtility.FromJson<Database>(cnt);
                BuildData(db);
            }
        }

        public void Save()
        {
            Database db = new Database();
            db.bundles = m_Bundles.Values.ToList();
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
