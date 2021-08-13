using AssetBundleBuilder.DataSource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
	public partial class EditorAssetBundleManager
	{
        public bool BuildAssetBundles(BuildInfo info)
        {
            List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
            List<string> assetsPaths = new List<string>();
            foreach (var bundle in bundles)
            {
                if (bundle.assets.Count > 0)
                {
                    AssetBundleBuild build = new AssetBundleBuild();
                    build.assetBundleName = bundle.name;
                    if (!string.IsNullOrEmpty(bundle.variantName))
                    {
                        build.assetBundleVariant = bundle.variantName;
                    }
                    assetsPaths.Clear();
                    bundle.TryGetAssetsPaths(ref assetsPaths);
                    build.assetNames = assetsPaths.ToArray();
                    builds.Add(build);
                }
            }

            if (!Directory.Exists(info.outputDirectory))
            {
                Directory.CreateDirectory(info.outputDirectory);
            }

            var buildManifest = BuildPipeline.BuildAssetBundles(info.outputDirectory, builds.ToArray(), info.options, info.buildTarget);
            if (buildManifest == null)
                return false;

            //不能消除Manifest，否则无法增量构建。可以在最终目录把Manifest删除
            //DatabaseUtil.ClearTempManifest(info.outputDirectory);

            DatabaseUtil.SaveBundleManifest(buildManifest, info, this);

            foreach (var assetBundleName in buildManifest.GetAllAssetBundles())
            {
                if (info.onBuild != null)
                {
                    info.onBuild(assetBundleName);
                }
            }
            return true;
        }

        public void SaveBundleManifest(AssetBundleManifest buildManifest, BuildInfo buildInfo, DataSource dataSource)
        {
            BundleManifest bundleManifest = new BundleManifest();
            bundleManifest.version = buildInfo.version;

            List<AssetBundleInfo> all = new List<AssetBundleInfo>();

            //if (Model.Model.BundleListIsEmpty())
            //{
            //    Model.Model.Rebuild();
            //}

            foreach (var assetBundleName in buildManifest.GetAllAssetBundles())
            {
                Model.BundleNameData bundleNameData = new Model.BundleNameData(assetBundleName);
                //Model.BundleDataInfo bundleInfo = Model.Model.FindBundle(bundleNameData) as Model.BundleDataInfo;
                FileInfo bundleInfo = new FileInfo(Path.Combine(buildInfo.outputDirectory, assetBundleName));
                if (bundleInfo != null)
                {
                    YH.AssetManage.AssetBundleInfo assetBundleInfo = new YH.AssetManage.AssetBundleInfo();
                    assetBundleInfo.fullName = bundleNameData.fullNativeName;
                    assetBundleInfo.shortName = bundleNameData.shortName;
                    assetBundleInfo.size = (int)bundleInfo.Length;
                    assetBundleInfo.hash = buildManifest.GetAssetBundleHash(assetBundleName).ToString();
                    assetBundleInfo.dependencies = buildManifest.GetDirectDependencies(assetBundleName);

                    List<YH.AssetManage.AssetInfo> assets = new List<YH.AssetManage.AssetInfo>();
                    // foreach (Model.AssetInfo assetInfo in bundleInfo.GetConcretes())
                    foreach (var assetPath in dataSource.GetAssetPathsFromAssetBundle(assetBundleName))
                    {
                        YH.AssetManage.AssetInfo ai = new YH.AssetManage.AssetInfo();
                        //Debug.Log(assetInfo.displayName + "," + assetInfo.bundleName + "," + assetInfo.fullAssetName);
                        ai.fullName = assetPath;// assetInfo.fullAssetName;
                        assets.Add(ai);
                        //assets.Add(AssetPaths.RemoveAssetPrev(assetInfo.fullAssetName));
                    }

                    assetBundleInfo.assets = assets;

                    all.Add(assetBundleInfo);
                }
            }
            bundleManifest.bundleInfos = all;

            string content = JsonUtility.ToJson(bundleManifest);

            File.WriteAllText(Path.Combine(buildInfo.outputDirectory, "all.manifest"), content);
        }
    }
}
