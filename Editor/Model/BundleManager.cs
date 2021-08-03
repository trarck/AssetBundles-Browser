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
		Dictionary<string, BundleNode> m_Bundles;
		Dictionary<string, AssetNode> m_Assets;
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
			m_Bundles = new Dictionary<string, BundleNode>();
			m_Assets = new Dictionary<string, AssetNode>();
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

		public void CreateBundlesFromDataSource()
		{
			string[] bundlePaths = dataSource.GetAllAssetBundleNames();

			foreach (var bundlePath in bundlePaths)
			{
				BundleDataInfo bundleDataInfo = CreateBundleDataByPath(bundlePath, m_Root);
				string[] assets = dataSource.GetAssetPathsFromAssetBundle(bundlePath);

			}
		}

		#region Bundle
		public BundleNode GetBundle(string bundlePath)
		{
			BundleNode bundleInfo = null;
			m_Bundles.TryGetValue(bundlePath, out bundleInfo);
			return bundleInfo;
		}

		public BundleNode GetBundle(string bundlePath,BundleFolderInfo parent)
		{
			BundleNode bundleInfo = null;
			if (parent != null)
			{
				bundlePath = parent.m_Name.fullNativeName + "/" + bundlePath;
			}
			m_Bundles.TryGetValue(bundlePath, out bundleInfo);
			return bundleInfo;
		}

		private BundleNode GetBundle(List<string> pathTokens,  BundleFolderInfo parent)
		{
			string bundleName = null;
			BundleNode bundleInfo = null;

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

		public void AddBundle(BundleNode bundleInfo, BundleFolderInfo parent)
		{
			if (parent != null)
			{
				//add to parent
				parent.AddChild(bundleInfo);

				//full path map
				string bundlePath = string.IsNullOrEmpty(parent.m_Name.fullNativeName) ? bundleInfo.displayName : (parent.m_Name.fullNativeName + "/" + bundleInfo.displayName);
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

			parent = GetBundleFolder(bundleNameData.pathTokens, bundleNameData.pathTokens.Count, parent);

			string bundleName = bundleNameData.shortName;

			bundleName = GetUniqueName(bundleName, parent);

			return CreateBundleDataByName(bundleName, parent);
		}

		public BundleFolderInfo CreateBundleFolderByName(string folderName, BundleFolderInfo parent=null)
		{
			if (parent == null)
			{
				parent = m_Root;
			}

			BundleFolderInfo bundleInfo = new BundleFolderInfo(folderName, parent);

			AddBundle(bundleInfo, parent);

			return bundleInfo;
		}

		public BundleFolderInfo CreateBundleFolderByPath(string folderPath, BundleFolderInfo parent=null)
		{
			if (string.IsNullOrEmpty(folderPath))
			{
				return null;
			}

			if (parent == null)
			{
				parent = m_Root;
			}

			List<string> pathTokens= BundleNameData.GetPathTokens(folderPath);

			parent = GetBundleFolder(pathTokens, pathTokens.Count-1 , parent);

			string bundleName = pathTokens[pathTokens.Count - 1];

			bundleName = GetUniqueName(bundleName, parent);

			return CreateBundleFolderByName(bundleName, parent);
		}

		public BundleFolderInfo GetBundleFolder(string folderPath, BundleFolderInfo parent)
		{
			if (m_Bundles.ContainsKey(folderPath))
			{
				return m_Bundles[folderPath] as BundleFolderInfo;
			}

			List<string> pathTokens = BundleNameData.GetPathTokens(folderPath);
			return GetBundleFolder(pathTokens, pathTokens.Count, parent);
		}

		private BundleFolderInfo GetBundleFolder(List<string> pathTokens, int deep, BundleFolderInfo parent)
		{
			string bundleName = null;
			BundleNode bundleInfo = null;

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

		public void RefreshBundleAssets(BundleDataInfo bundleDataInfo)
		{
			string[] assets = dataSource.GetAssetPathsFromAssetBundle(bundleDataInfo.m_Name.fullNativeName);
		}

		#endregion Bundle

		#region Asset
		public AssetNode GetAsset(string assetPath)
		{
			AssetNode assetInfo = null;
			m_Assets.TryGetValue(assetPath, out assetInfo);
			return assetInfo;
		}

		public AssetNode CreateAsset(string assetPath, string bundlePath=null)
		{
			if (string.IsNullOrEmpty(bundlePath))
			{
				bundlePath = dataSource.GetAssetBundleName(assetPath);
			}

			AssetNode assetInfo = new AssetNode(assetPath, bundlePath);
			m_Assets[assetPath] = assetInfo;
			return assetInfo;
		}

		/// <summary>
		/// 刷新资源的直接依赖
		/// </summary>
		/// <param name="assetInfo"></param>
		internal void RefreshAssetDependencies(AssetNode assetInfo)
		{
			if (!AssetDatabase.IsValidFolder(assetInfo.fullAssetName))
			{
				//dep
				assetInfo.dependencies.Clear();
				foreach (var dep in AssetDatabase.GetDependencies(assetInfo.fullAssetName, false))
				{
					if (dep != assetInfo.fullAssetName)
					{
						AssetNode depAsset = GetAsset(dep);
						if (depAsset == null)
						{
							depAsset = CreateAsset(dep);
						}

						depAsset.AddRefer(assetInfo);
					}
				}
			}
		}

		/// <summary>
		/// 刷新资源的所有依赖
		/// 通过直接依赖，循环遍历获取所有依赖。
		/// 注意：要在 RefreshAssetDependencies 之后才能执行这个方法
		/// TODO::测试通过unity的直接获取所有依赖和通过遍历的速度
		/// </summary>
		/// <param name="assetInfo"></param>
		internal void RefreshAssetAllDependencies(AssetNode assetInfo)
		{
			if (!AssetDatabase.IsValidFolder(assetInfo.fullAssetName))
			{
				//clear all deps
				assetInfo.allDependencies.Clear();

				Stack<AssetNode> assetsStack = new Stack<AssetNode>();
				HashSet<AssetNode> visitedInfos = new HashSet<AssetNode>();
				
				assetsStack.Push(assetInfo);

				while (assetsStack.Count > 0)
				{
					AssetNode ai = assetsStack.Pop();
					if (visitedInfos.Contains(ai))
					{
						continue;
					}

					visitedInfos.Add(ai);

					if (ai.dependencies != null && ai.dependencies.Count > 0)
					{
						foreach (var dep in ai.dependencies)
						{
							assetInfo.allDependencies.Add(dep);
							assetsStack.Push(dep);
						}
					}
				}
			}
		}

		internal void RefreshAssetAllDependencies2(AssetNode assetInfo)
		{
			if (!AssetDatabase.IsValidFolder(assetInfo.fullAssetName))
			{
				//dep
				assetInfo.allDependencies.Clear();
				foreach (var dep in AssetDatabase.GetDependencies(assetInfo.fullAssetName, true))
				{
					if (dep != assetInfo.fullAssetName)
					{
						AssetNode depAsset = GetAsset(dep);
						if (depAsset == null)
						{
							depAsset = CreateAsset(dep);
						}

						assetInfo.allDependencies.Add(depAsset);
					}
				}
			}
		}

		/// <summary>
		/// 更新所有资源的直接依赖
		/// </summary>
		public void RefreshAllAssetDependencies()
		{
			foreach (var iter in m_Assets)
			{
				RefreshAssetDependencies(iter.Value);
			}
		}

		/// <summary>
		/// 更新所有资源的所有依赖
		/// </summary>
		public void RefreshAllAssetAllDependencies()
		{
			foreach (var iter in m_Assets)
			{
				RefreshAssetAllDependencies(iter.Value);
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
