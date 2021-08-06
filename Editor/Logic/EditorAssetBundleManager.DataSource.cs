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
		public void SaveToDataSource(DataSource.DataSource ds)
		{
			foreach (var iter in m_Assets)
			{
				AssetInfo asset = iter.Value;
				if (asset.bundle != null)
				{
					ds.SetAssetBundleNameAndVariant(asset.assetPath, asset.bundle.name, asset.bundle.variantName);
				}
			}			
		}

		public void LoadFromDataSource(DataSource.DataSource ds)
		{
			//创建asset
			string[] assetPaths = ds.GetAllAssetPaths();
			foreach (var assetPath in assetPaths)
			{
				CreateAsset(assetPath);
			}
			//更新asset的依赖
			RefreshAllAssetAllDependencies();

			//创建bundle
			string[] bundleNames = ds.GetAllAssetBundleNames();
			foreach (var bundleName in bundleNames)
			{
				BundleInfo bundle = CreateBundle(bundleName);
				assetPaths = ds.GetAssetPathsFromAssetBundle(bundleName);
				foreach (var assetPath in assetPaths)
				{
					bundle.AddAsset(GetAsset(assetPath));
				}
			}

			//更新bundle的依赖
			RefreshAllBundleDependencies();
		}
	}
}
