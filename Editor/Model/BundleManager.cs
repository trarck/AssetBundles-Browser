using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

using AssetBundleBuilder.DataSource;
using System;

namespace AssetBundleBuilder.Model
{
	public class BundleManager
	{
		Dictionary<string, BundleInfo> m_Bundles;
		Dictionary<string, AssetInfo> m_Assets;
		BundleFolderInfo m_Root;

		DataSource.DataSource m_DataSource;
		Type m_DefaultDataSourceType = typeof(JsonDataSource);


		static BundleManager m_Instance = null;
		public static BundleManager Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = new BundleManager();
					m_Instance.Init();
				}
				return m_Instance;
			}
		}

		public void Init()
		{
			m_Bundles = new Dictionary<string, BundleInfo>();
			m_Assets = new Dictionary<string, AssetInfo>();
			m_Root = new BundleFolderConcreteInfo("",null);
		}

		public void Clean()
		{
			if (m_Bundles != null)
			{
				m_Bundles.Clear();
				m_Bundles = null;
			}

			if (m_Assets != null)
			{
				m_Assets.Clear();
				m_Bundles = null;
			}

			if (m_Root != null)
			{
				m_Root = null;
			}
		}

		public void Clear()
		{
			if (m_Bundles != null)
			{
				m_Bundles.Clear();
			}

			if (m_Assets != null)
			{
				m_Assets.Clear();
			}

			if (m_Root != null)
			{
				m_Root = new BundleFolderConcreteInfo("", null);
			}
		}

		#region Bundle
		public BundleInfo GetBundle(string bundlePath)
		{
			BundleInfo bundleInfo = null;
			m_Bundles.TryGetValue(bundlePath, out bundleInfo);
			return bundleInfo;
		}

		public BundleInfo GetBundle(string bundlePath,BundleFolderInfo parent)
		{
			BundleInfo bundleInfo = null;
			if (parent != null)
			{
				bundlePath = parent.m_Name.fullNativeName + "/" + bundlePath;
			}
			m_Bundles.TryGetValue(bundlePath, out bundleInfo);
			return bundleInfo;
		}

		private BundleInfo GetBundle(List<string> pathTokens,  BundleFolderInfo parent)
		{
			string bundleName = null;
			BundleInfo bundleInfo = null;

			for (int i = 0,l=pathTokens.Count; i < l; ++i)
			{
				bundleName = pathTokens[i];
				bundleInfo = parent.GetChild(bundleName);

				if (bundleInfo == null)
				{
					return null;
				}

				if (bundleInfo is BundleFolderInfo)
				{
					parent = bundleInfo as BundleFolderInfo;
				}

				else if (bundleInfo is BundleDataInfo)
				{
					if (i == l - 1)
					{
						return bundleInfo;
					}
					else
					{
						Debug.LogErrorFormat("GetBundleFolder:{0} is not bundle folder", parent.m_Name.fullNativeName + "/" + bundleName);
						return null;
					}
				}
				else if (bundleInfo is BundleFolderInfo)
				{
					parent = bundleInfo as BundleFolderInfo;
				}
				else
				{
					Debug.LogErrorFormat("GetBundleFolder:{0} is not bundle folder", parent.m_Name.fullNativeName + "/" + bundleName);
					return null;
				}
			}
			return bundleInfo;
		}

		public void AddBundle(BundleInfo bundleInfo, BundleFolderInfo parent)
		{
			if (parent != null)
			{
				//add to parent
				parent.AddChild(bundleInfo);

				//full path map
				string bundlePath = parent.m_Name.fullNativeName + "/" + bundleInfo.displayName;
				m_Bundles[bundlePath] = bundleInfo;
			}
		}

		public BundleDataInfo CreateBundleDataByName(string bundleName, BundleFolderInfo parent = null)
		{
			if (parent == null)
			{
				parent = m_Root;
			}

			BundleDataInfo bundleInfo = new BundleDataInfo(bundleName, parent);

			AddBundle(bundleInfo, parent);

			return bundleInfo;
		}

		public BundleDataInfo CreateBundleDataByPath(string bundlePath, BundleFolderInfo parent = null)
		{
			if (string.IsNullOrEmpty(bundlePath))
			{
				return null;
			}

			if (parent == null)
			{
				parent = m_Root;
			}

			BundleNameData bundleNameData = new BundleNameData(bundlePath);

			parent = GetBundleFolder(bundleNameData.pathTokens, bundleNameData.pathTokens.Count - 1, parent);

			string bundleName = bundleNameData.pathTokens[bundleNameData.pathTokens.Count - 1];

			bundleName = GetUniqueName(bundleName, parent);

			return CreateBundleDataByName(bundleName, parent);
		}

		public BundleFolderInfo CreateBundleFolderByName(string folderName, BundleFolderInfo parent)
		{
			if (parent == null)
			{
				parent = m_Root;
			}

			BundleFolderInfo bundleInfo = new BundleFolderInfo(folderName, parent);

			AddBundle(bundleInfo, parent);

			return bundleInfo;
		}

		public BundleFolderInfo CreateBundleFolderByPath(string folderPath, BundleFolderInfo parent)
		{
			if (string.IsNullOrEmpty(folderPath))
			{
				return null;
			}

			if (parent == null)
			{
				parent = m_Root;
			}

			BundleNameData bundleNameData = new BundleNameData(folderPath);

			parent = GetBundleFolder(bundleNameData.pathTokens, bundleNameData.pathTokens.Count - 1, parent);

			string bundleName = bundleNameData.pathTokens[bundleNameData.pathTokens.Count - 1];

			bundleName = GetUniqueName(bundleName, parent);

			return CreateBundleFolderByName(bundleName, parent);
		}

		public BundleFolderInfo GetBundleFolder(string folderPath, BundleFolderInfo parent)
		{
			if (m_Bundles.ContainsKey(folderPath))
			{
				return m_Bundles[folderPath] as BundleFolderInfo;
			}

			BundleNameData bundleNameData = new BundleNameData(folderPath);
			return GetBundleFolder(bundleNameData.pathTokens, bundleNameData.pathTokens.Count, parent);
		}

		private BundleFolderInfo GetBundleFolder(List<string> pathTokens, int deep, BundleFolderInfo parent)
		{
			string bundleName = null;
			BundleInfo bundleInfo = null;

			for (int i = 0; i < deep; ++i)
			{
				bundleName = pathTokens[i];
				bundleInfo = parent.GetChild(bundleName);

				if (bundleInfo == null)
				{
					bundleInfo = CreateBundleFolderByName(bundleName, parent);
				}
				else if (bundleInfo is BundleDataInfo)
				{
					Debug.LogErrorFormat("GetBundleFolder:{0} is not bundle folder", parent.m_Name.fullNativeName + "/" + bundleName);
					return null;
				}

				parent = bundleInfo as BundleFolderInfo;
			}
			return parent;
		}

		private string GetUniqueName(string name,BundleFolderInfo parent)
		{
			int i = 0;
			string newName = name;
			while (parent.GetChild(newName) != null)
			{
				++i;
				newName = name + i;
			}
			return newName;
		}

		public void SetupBundleInfos()
		{
			string[] bundlePaths = dataSource.GetAllAssetBundleNames();

			foreach (var bundlePath in bundlePaths)
			{
				BundleDataInfo bundleDataInfo = CreateBundleDataByPath(bundlePath,m_Root);
				string[] assets = dataSource.GetAssetPathsFromAssetBundle(bundlePath);

			}
		}

		#endregion Bundle

		#region Asset
		public AssetInfo GetAssetInfo(string assetPath)
		{
			AssetInfo assetInfo = null;
			m_Assets.TryGetValue(assetPath, out assetInfo);
			return assetInfo;
		}

		public AssetInfo CreateAsset(string assetPath, string bundlePath=null)
		{
			if (string.IsNullOrEmpty(bundlePath))
			{
				bundlePath = dataSource.GetAssetBundleName(assetPath);
			}

			AssetInfo assetInfo = new AssetInfo(assetPath, bundlePath);
			m_Assets[assetPath] = assetInfo;
			return assetInfo;
		}

		internal void GatherAssetDependencies(AssetInfo assetInfo)
		{
			if (!AssetDatabase.IsValidFolder(assetInfo.fullAssetName))
			{
				//dep
				assetInfo.dependencies.Clear();
				foreach (var dep in AssetDatabase.GetDependencies(assetInfo.fullAssetName, false))
				{
					if (dep != assetInfo.fullAssetName)
					{
						AssetInfo depAsset = GetAssetInfo(dep);
						if (depAsset == null)
						{
							depAsset = CreateAsset(dep);
						}

						depAsset.AddRefer(assetInfo);
					}
				}


				////all
				//assetInfo.allDependencies.Clear();
				//foreach (var dep in AssetDatabase.GetDependencies(assetInfo.fullAssetName, true))
				//{
				//	if (dep != assetInfo.fullAssetName)
				//	{
				//		AssetInfo depAsset = GetAssetInfo(dep);
				//		if (depAsset == null)
				//		{
				//			depAsset = CreateAsset(dep);
				//		}

				//		depAsset.AddRefer(assetInfo);

				//		assetInfo.allDependencies.Add(depAsset);
				//	}
				//}
			}
		}
		#endregion Asset

		public DataSource.DataSource dataSource
		{
			get
			{
				if (m_DataSource == null)
				{
					m_DataSource = DataSourceProviderUtility.GetDataSource(m_DefaultDataSourceType, true);
				}
				return m_DataSource;
			}
			set
			{
				m_DataSource = value;
			}
		}
	}
}
