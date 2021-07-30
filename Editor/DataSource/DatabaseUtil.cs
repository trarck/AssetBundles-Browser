using System.IO;
using System.Collections.Generic;

using YH.AssetManage;
using UnityEngine;

namespace AssetBundleBuilder.DataSource
{
    public class DatabaseUtil
    {
        public static void SaveBundleManifest(AssetBundleManifest buildManifest, BuildInfo buildInfo,DataSource dataSource)
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
                   foreach(var assetPath in dataSource.GetAssetPathsFromAssetBundle(assetBundleName))
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