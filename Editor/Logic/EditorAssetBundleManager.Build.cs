using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YH.AssetManage;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine.Build.Pipeline;

namespace AssetBundleBuilder
{
	public partial class EditorAssetBundleManager
	{

        private List<AssetBundleBuild> CreateAssetBundleBuilds()
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
            return builds;
        }

        public bool BuildAssetBundles(BuildInfo info)
        {

            List<AssetBundleBuild> builds = CreateAssetBundleBuilds();

            if (!Directory.Exists(info.outputDirectory))
            {
                Directory.CreateDirectory(info.outputDirectory);
            }

            var buildManifest = BuildPipeline.BuildAssetBundles(info.outputDirectory, builds.ToArray(), info.options, info.buildTarget);
            if (buildManifest == null)
                return false;

            //不能消除Manifest，否则无法增量构建。可以在最终目录把Manifest删除
            //DatabaseUtil.ClearTempManifest(info.outputDirectory);

            SaveBundleManifest(buildManifest, info);

            if (info.onBuild != null)
            {
                info.onBuild(null);
            }
            return true;
        }

        public bool BuildAssetBundlesPipline(BuildInfo info)
        {
            List<AssetBundleBuild> builds = CreateAssetBundleBuilds();

            if (!Directory.Exists(info.outputDirectory))
            {
                Directory.CreateDirectory(info.outputDirectory);
            }

            BundleBuildParameters buildParams = new BundleBuildParameters(info.buildTarget, info.buildTargetGroup, info.outputDirectory);
            buildParams.BundleCompression = BuildCompression.LZ4;
            IList<IBuildTask> buildTasks = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible);

            IBundleBuildResults results;
            var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(builds), out results, buildTasks);

            //SaveBundleManifest(results, info);
            return true;
        }

        private void SaveBundleManifest(AssetBundleManifest buildManifest, BuildInfo buildInfo)
        {
            BundleManifest bundleManifest = new BundleManifest();
            bundleManifest.version = buildInfo.version;

            List<AssetBundleInfo> bundleInfos = new List<AssetBundleInfo>();

            foreach (var assetBundleName in buildManifest.GetAllAssetBundles())
            {
                FileInfo assetBundleFileInfo = new FileInfo(Path.Combine(buildInfo.outputDirectory, assetBundleName));
                if (assetBundleFileInfo != null)
                {
                    BundleInfo bundleInfo = GetBundle(assetBundleName);
                    if (bundleInfo != null)
                    {
                        YH.AssetManage.AssetBundleInfo assetBundleInfo = new YH.AssetManage.AssetBundleInfo();
                        assetBundleInfo.fullName = assetBundleName;
                        assetBundleInfo.shortName = Path.GetFileName(assetBundleName);
                        assetBundleInfo.size = (int)assetBundleFileInfo.Length;
                        assetBundleInfo.hash = buildManifest.GetAssetBundleHash(assetBundleName).ToString();
                        assetBundleInfo.dependencies = buildManifest.GetDirectDependencies(assetBundleName);

                        List<YH.AssetManage.AssetInfo> assets = new List<YH.AssetManage.AssetInfo>();
                        foreach (var assetInfo in bundleInfo.assets)
                        {
                            YH.AssetManage.AssetInfo ai = new YH.AssetManage.AssetInfo();
                            ai.fullName = assetInfo.assetPath;
                            assets.Add(ai);
                        }
                        assetBundleInfo.assets = assets;

                        bundleInfos.Add(assetBundleInfo);
                    }
                    else
                    {
                        Debug.LogWarningFormat("Can't get BundleInfo  {0}", assetBundleName);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("No builded asset bundle file {0}", assetBundleName);
                }
            }
            bundleManifest.bundleInfos = bundleInfos;

            //save binary
            string outputManifestFile = Path.Combine(buildInfo.outputDirectory, buildInfo.manifestName);
            using (FileStream fs = new FileStream(outputManifestFile, FileMode.Create))
            using (BinaryWriter br = new BinaryWriter(fs))
            {
                bundleManifest.Write(br);
            }

            //save json
            string outputManifestJsonFile = outputManifestFile + ".json";
            string content = JsonUtility.ToJson(bundleManifest, true);
            File.WriteAllText(outputManifestJsonFile, content);
        }

        private void SaveBundleManifest(IBundleBuildResults results, BuildInfo buildInfo)
        {
            BundleManifest bundleManifest = new BundleManifest();
            bundleManifest.version = buildInfo.version;

            List<AssetBundleInfo> bundleInfos = new List<AssetBundleInfo>();

            foreach (var iter in results.BundleInfos)
            {
                string assetBundleName = iter.Key;
                BundleDetails bundleDetail= iter.Value;                
                FileInfo assetBundleFileInfo = new FileInfo(bundleDetail.FileName);
                if (assetBundleFileInfo != null)
                {
                    BundleInfo bundleInfo = GetBundle(assetBundleName);
                    if (bundleInfo != null)
                    {
                        YH.AssetManage.AssetBundleInfo assetBundleInfo = new YH.AssetManage.AssetBundleInfo();
                        assetBundleInfo.fullName = assetBundleName;
                        assetBundleInfo.shortName = Path.GetFileName(assetBundleName);
                        assetBundleInfo.size = (int)assetBundleFileInfo.Length;
                        assetBundleInfo.hash = bundleDetail.Hash.ToString();
                        assetBundleInfo.dependencies = bundleDetail.Dependencies;

                        List<YH.AssetManage.AssetInfo> assets = new List<YH.AssetManage.AssetInfo>();
                        foreach (var assetInfo in bundleInfo.assets)
                        {
                            YH.AssetManage.AssetInfo ai = new YH.AssetManage.AssetInfo();
                            ai.fullName = assetInfo.assetPath;
                            assets.Add(ai);
                        }
                        assetBundleInfo.assets = assets;

                        bundleInfos.Add(assetBundleInfo);
                    }
                    else
                    {
                        Debug.LogWarningFormat("Can't get BundleInfo  {0}", assetBundleName);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("No builded asset bundle file {0}", assetBundleName);
                }
            }
            bundleManifest.bundleInfos = bundleInfos;

            //save binary
            string outputManifestFile = Path.Combine(buildInfo.outputDirectory, buildInfo.manifestName);
            using (FileStream fs = new FileStream(outputManifestFile, FileMode.Create))
            using (BinaryWriter br = new BinaryWriter(fs))
            {
                bundleManifest.Write(br);
            }

            //save json
            string outputManifestJsonFile = outputManifestFile + ".json";
            string content = JsonUtility.ToJson(bundleManifest, true);
            File.WriteAllText(outputManifestJsonFile, content);
        }

        private BuildTargetGroup GetBuildTargetGroupFromTarget(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return BuildTargetGroup.Android;
                case BuildTarget.iOS:
                    return BuildTargetGroup.iOS;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                    return BuildTargetGroup.Standalone;
                case BuildTarget.PS4:
                    return BuildTargetGroup.PS4;
                case BuildTarget.Switch:
                    return BuildTargetGroup.Switch;
                default:
                    return EditorUserBuildSettings.selectedBuildTargetGroup;
            }
        }
    }
}
