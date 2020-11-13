using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

using AssetBundleBuilder.DataSource;

namespace AssetBundleBuilder.Model
{
	public class BundleManager
	{
		Dictionary<string, BundleInfo> m_Bundles;
		Dictionary<string, AssetInfo> m_Assets;
		BundleFolderInfo m_Root;

		public void Init()
		{
			m_Bundles = new Dictionary<string, BundleInfo>();
			m_Assets = new Dictionary<string, AssetInfo>();
			m_Root = new BundleFolderConcreteInfo("",null);
		}

		public BundleInfo GetBundle(string bundlePath)
		{
			BundleInfo bundleInfo = null;
			m_Bundles.TryGetValue(bundlePath, out bundleInfo);
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

		public BundleDataInfo CreateBundleDataByName(string bundleName, BundleFolderInfo parent)
		{
			if (parent == null)
			{
				parent = m_Root;
			}

			BundleDataInfo bundleInfo = new BundleDataInfo(bundleName, parent);

			AddBundle(bundleInfo, parent);

			return bundleInfo;
		}

		public BundleDataInfo CreateBundleDataByPath(string bundlePath, BundleFolderInfo parent)
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
	}
}
