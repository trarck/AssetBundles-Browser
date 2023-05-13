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

        private static List<AssetBundleBuild> CreateAssetBundleBuilds(List<BundleInfo> validBundles, BuildInfo info)
        {
            List<AssetBundleBuild> builds = new List<AssetBundleBuild>(validBundles.Count);
            List<string> assetsPaths = new List<string>();
            foreach (var bundle in validBundles)
            {
                AssetBundleBuild build = new AssetBundleBuild();
                build.assetBundleName = string.IsNullOrEmpty(info.assetBundleExt)? bundle.name: string.Format("{0}{1}", bundle.name, info.assetBundleExt);
                if (!string.IsNullOrEmpty(bundle.variantName))
                {
                    build.assetBundleVariant = bundle.variantName;
                }
                assetsPaths.Clear();
                bundle.TryGetAssetsPaths(ref assetsPaths);
                build.assetNames = assetsPaths.ToArray();
                builds.Add(build);
            }
            return builds;
        }

        public bool BuildAssetBundles(BuildInfo info)
        {
            //获取有效的bundle信息
            List<BundleInfo> validBundles = new List<BundleInfo>();
            if (!TryGetValidBundles(validBundles))
            {
                return false;
            }
            //生成构建列表
            List<AssetBundleBuild> builds = CreateAssetBundleBuilds(validBundles, info);

            if (!Directory.Exists(info.outputDirectory))
            {
                Directory.CreateDirectory(info.outputDirectory);
            }

            //生成bundle
            var buildManifest = BuildPipeline.BuildAssetBundles(info.outputDirectory, builds.ToArray(), info.options, info.buildTarget);
            if (buildManifest == null)
                return false;

            //如果使用全量依赖，则刷新一次
            if (info.bundleDependenciesAll)
            {
                RefreshAllBundleAllDependencies();
            }

            SaveBundleManifest(validBundles, info);

            if (info.onBuild != null)
            {
                info.onBuild(null);
            }
            return true;
        }

        public bool BuildAssetBundlesPipline(BuildInfo info)
        {
            List<BundleInfo> validBundles = new List<BundleInfo>();
            if (!TryGetValidBundles(validBundles))
            {
                return false;
            }

            List<AssetBundleBuild> builds = CreateAssetBundleBuilds(validBundles, info);

            if (!Directory.Exists(info.outputDirectory))
            {
                Directory.CreateDirectory(info.outputDirectory);
            }

            BundleBuildParameters buildParams = new BundleBuildParameters(info.buildTarget, info.buildTargetGroup, info.outputDirectory);
            buildParams.BundleCompression = BuildCompression.LZ4;
            IList<IBuildTask> buildTasks = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible);

            IBundleBuildResults results;
            var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(builds), out results, buildTasks);
            if(exitCode!= ReturnCode.Success)
            {
                return false;
            }

            //如果使用全量依赖，则刷新一次
            if (info.bundleDependenciesAll)
            {
                RefreshAllBundleAllDependencies();
            }

            SaveBundleManifest(validBundles, info);
            return true;
        }

        private static void SaveBundleManifest(List<BundleInfo> validBundles, BuildInfo buildInfo)
        {
            System.Version version = System.Version.Parse(buildInfo.version);

            //save binary
            string outputManifestFile = Path.Combine(buildInfo.outputDirectory, buildInfo.manifestName);
            using (FileStream fs = new FileStream(outputManifestFile, FileMode.Create))
            {
                AssetBundleManifestWriter writer = new AssetBundleManifestWriter(fs);
                writer.WriteManifest(version, validBundles, buildInfo.bundleDependenciesAll);
            }

            //save json
            string outputManifestJsonFile = outputManifestFile + ".json";

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
