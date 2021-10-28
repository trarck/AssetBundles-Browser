﻿using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

using AssetBundleBuilder.DataSource;
using System;
using AssetBundleBuilder.View;

namespace AssetBundleBuilder.Model
{
	public class BundleTreeManager
	{
		public static string DefaultFolderName = "dummy";
		public static string DefaultBundleName = "newbundle";
		public static int BundleFolderItemId = 10;

		Dictionary<string, AssetNode> m_Assets;

		DataSource.DataSource m_DataSource;
		Type m_DefaultDataSourceType = typeof(JsonDataSource);


		BundleTreeFolderItem m_RootItem;
		//Dictionary<int, BundleTreeItem> m_BundleItems;

		static BundleTreeManager m_Instance = null;
		public static BundleTreeManager Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = new BundleTreeManager();
					m_Instance.Init();
				}
				return m_Instance;
			}
		}

		public BundleTreeItem rootItem
		{
			get
			{
				return m_RootItem;
			}
		}

		public void Init()
		{
			m_Assets = new Dictionary<string, AssetNode>();
			//m_BundleItems = new Dictionary<int, BundleTreeItem>();
			m_RootItem = new BundleTreeFolderItem(null, -1, 0);
		}

		public void Clean()
		{

			if (m_Assets != null)
			{
				m_Assets.Clear();
			}

		}

		public void Clear()
		{

			if (m_Assets != null)
			{
				m_Assets.Clear();
			}
		}

		public void ReloadBundles()
		{
			string bundleDataPath = EditorAssetBundleManager.Instance.GetBinaryAssetBundleSavePath();
			EditorAssetBundleManager.Instance.LoadBinary(bundleDataPath);
			EditorAssetBundleManager.Instance.RefreshAllAssetDependencies();
			EditorAssetBundleManager.Instance.RefreshAllAssetAllDependencies();

			m_RootItem.ClearChildren();
			//m_BundleItems.Clear();


			//create all bundle node
			foreach (var bundleInfo in EditorAssetBundleManager.Instance.bundles)
			{
				CreateBundleData(bundleInfo);
			}
		}

		public void RefreshBundles()
		{
			m_RootItem.ClearChildren();
			//m_BundleItems.Clear();

			//create all bundle node
			foreach (var bundleInfo in EditorAssetBundleManager.Instance.bundles)
			{
				CreateBundleData(bundleInfo);
			}
		}

		public void SaveBundles()
		{
			string bundleDataPath = EditorAssetBundleManager.Instance.GetBinaryAssetBundleSavePath();
			EditorAssetBundleManager.Instance.SaveBinary(bundleDataPath);
			string saveJsonPath = EditorAssetBundleManager.Instance.GetJsonAssetBundleSavePath();
			EditorAssetBundleManager.Instance.SaveToJson(saveJsonPath);
		}

		public bool BundleListIsEmpty()
		{
			return m_RootItem.children==null || m_RootItem.children.Count == 0;
		}

		#region Bundle
		public BundleTreeItem GetBundle(string bundlePath, BundleTreeFolderItem parent=null)
		{
			BundleTreeItem bundle = null;
			if (parent == null)
			{
				parent = m_RootItem;
			}
			Debug.LogFormat(bundlePath);
			List<string> pathTokens = BundleNameData.GetPathTokens(bundlePath);

			return GetBundle(pathTokens, parent);
		}

		private BundleTreeItem GetBundle(List<string> pathTokens, BundleTreeFolderItem parent)
		{
			string bundleName = null;
			BundleTreeItem bundle = null;

			for (int i = 0, l = pathTokens.Count; i < l; ++i)
			{
				bundleName = pathTokens[i];
				bundle = parent.GetChild(bundleName);

				if (bundle == null)
				{
					return null;
				}

				if (bundle is BundleTreeFolderItem)
				{
					parent = bundle as BundleTreeFolderItem;
				}
				else if (bundle is BundleTreeDataItem)
				{
					if (i == l - 1)
					{
						return bundle;
					}
					else
					{
						Debug.LogErrorFormat("GetBundleFolder:{0} is not bundle folder", string.IsNullOrEmpty(parent.fullName) ? bundleName : (parent.fullName  + "/" + bundleName));
						return null;
					}
				}
				else
				{
					Debug.LogErrorFormat("GetBundleFolder:{0} is not bundle folder", string.IsNullOrEmpty(parent.fullName) ? bundleName : (parent.fullName + "/" + bundleName));
					return null;
				}
			}
			return bundle;
		}

		public void AddBundle(BundleTreeItem bundle, BundleTreeFolderItem parent)
		{
			if (parent != null)
			{
				//add to parent
				parent.AddChild(bundle);
			}
		}

		public void RemoveBundle(BundleTreeItem bundle)
		{
			//remove from bundle tree
			if (bundle.parent != null)
			{
				if (bundle.parent.children != null)
				{
					bundle.parent.children.Remove(bundle);
				}
			}

			//remove from asset bundle manager
			if (bundle is BundleTreeDataItem)
			{
				BundleTreeDataItem dataBundleItem = bundle as BundleTreeDataItem;
				RemoveBundleDataInfo(dataBundleItem);
			}
			else if (bundle is BundleTreeFolderItem)
			{
				//remove all child
				BundleTreeFolderItem folderItem = bundle as BundleTreeFolderItem;
				if (folderItem.children != null)
				{
					Stack<BundleTreeFolderItem> folders = new Stack<BundleTreeFolderItem>();
					folders.Push(folderItem);
					while (folders.Count > 0)
					{
						folderItem = folders.Pop();
						foreach (var child in folderItem.children)
						{
							BundleTreeDataItem dataBundleItem = child as BundleTreeDataItem;
							if (dataBundleItem != null)
							{
								RemoveBundleDataInfo(dataBundleItem);
							}
							else
							{
								folders.Push(child as BundleTreeFolderItem);
							}
						}
					}
				}
			}
		}

		private void RemoveBundleDataInfo(BundleTreeDataItem bundleDataItem)
		{
			EditorAssetBundleManager.Instance.RemoveBundle(bundleDataItem.bundleInfo);
		}

		private BundleNameData CreateBundleNameData(string bundlePath, ref BundleTreeFolderItem parent, int offset = 0)
		{
			if (parent == null)
			{
				parent = m_RootItem;
			}

			BundleTreeFolderItem originParent = parent;

			BundleNameData bundleNameData = new BundleNameData(bundlePath);

			parent = GetBundleFolder(bundleNameData.pathTokens, bundleNameData.pathTokens.Count - offset, parent);

			string bundleName = bundleNameData.shortName;

			bundleName = GetUniqueName(bundleName, parent);

			bundleNameData.ShortNameChange(bundleName);
			bundleNameData.PartialNameChange(originParent.fullName, -1);
			return bundleNameData;
		}

		private void ParseBundleName(string bundlePath, ref BundleNameData bundleNameData, ref BundleTreeFolderItem parent, int offset = 0, bool uniqueName = true)
		{
			if (parent == null)
			{
				parent = m_RootItem;
			}

			BundleTreeFolderItem originParent = parent;

			bundleNameData.SetBundleName(bundlePath);

			parent = GetBundleFolder(bundleNameData.pathTokens, bundleNameData.pathTokens.Count - offset, parent);

			string bundleName = bundleNameData.shortName;

			if (uniqueName)
			{
				bundleName = GetUniqueName(bundleName, parent);
			}

			bundleNameData.ShortNameChange(bundleName);
			bundleNameData.PartialNameChange(originParent.fullName, -1);
		}

		public BundleTreeDataItem CreateBundleData(BundleInfo bundleInfo)
		{
			if (bundleInfo==null)
			{
				return null;
			}

			List<string> pathTokens = BundleNameData.GetPathTokens(bundleInfo.name);
			BundleTreeFolderItem parent = GetBundleFolder(pathTokens, pathTokens.Count-1, m_RootItem);
			BundleTreeDataItem bundleItem = new BundleTreeDataItem(bundleInfo, parent.depth + 1);

			AddBundle(bundleItem, parent);

			return bundleItem;
		}

		public BundleTreeDataItem CreateBundleDataByName(string bundleName, BundleTreeFolderItem parent = null, bool uniqueName=false)
		{

			if (parent == null)
			{
				parent = m_RootItem;
			}

			if(uniqueName)
				bundleName = GetUniqueName(bundleName, parent);

			string fullPath =string.IsNullOrEmpty(parent.fullName)? bundleName : parent.fullName + "/" + bundleName;

			BundleTreeDataItem bundleItem = new BundleTreeDataItem(fullPath.GetHashCode(), parent.depth + 1, bundleName);
			AddBundle(bundleItem, parent);
			return bundleItem;
		}

		public BundleTreeDataItem CreateBundleDataByPath(string bundlePath, BundleTreeFolderItem parent = null, bool uniqueName = false)
		{
			BundleNameData bundleNameData = new BundleNameData();
			ParseBundleName(bundlePath, ref bundleNameData, ref parent, 0, uniqueName);
			BundleTreeDataItem bundleItem = new BundleTreeDataItem(bundleNameData.fullNativeName.GetHashCode(), parent.depth + 1, bundleNameData.shortName);

			AddBundle(bundleItem, parent);

			return bundleItem;
		}

		public BundleTreeDataItem CreateBundleData(string bundleName, BundleTreeFolderItem parent = null, bool uniqueName = false)
		{
			if (string.IsNullOrEmpty(bundleName))
			{
				return null;
			}

			BundleTreeDataItem bundleItem = null;

			if (bundleName.IndexOf("/") > -1)
			{
				bundleItem = CreateBundleDataByPath(bundleName,parent, uniqueName);
			}
			else
			{
				bundleItem = CreateBundleDataByName(bundleName, parent, uniqueName);
			}

			AddBundle(bundleItem, parent);

			return bundleItem;
		}


		public BundleTreeDataItem CreateEmptyBundle(BundleTreeFolderItem parent = null)
		{
			return CreateBundleData(DefaultBundleName, parent);
		}

		public BundleTreeFolderItem CreateBundleFolderByName(string folderName, BundleTreeFolderItem parent = null, bool checkUnique = false, bool pathHashAsId = false)
		{
			if (parent == null)
			{
				parent = m_RootItem;
			}

			if (checkUnique)
			{
				folderName = GetUniqueName(folderName, parent);
			}

			int itemId = -1;
			if (pathHashAsId)
			{
				string fullPath = string.IsNullOrEmpty(parent.fullName) ? folderName : parent.fullName + "/" + folderName;
				itemId = fullPath.GetHashCode();
			}
			else
			{
				itemId = ++BundleFolderItemId;
			}

			BundleTreeFolderItem bundle = new BundleTreeFolderItem(folderName, parent.depth + 1, itemId);
			AddBundle(bundle, parent);
			return bundle;
		}

		public BundleTreeFolderItem CreateBundleFolderByPath(string folderPath, BundleTreeFolderItem parent = null, bool checkUnique = false, bool pathHashAsId = false)
		{
			BundleNameData bundleNameData = new BundleNameData();
			ParseBundleName(folderPath, ref bundleNameData, ref parent, 0, checkUnique);

			int itemId = -1;
			if (pathHashAsId)
			{
				itemId = bundleNameData.fullNativeName.GetHashCode();
			}
			else
			{
				itemId = ++BundleFolderItemId;
			}

			BundleTreeFolderItem bundle = new BundleTreeFolderItem(bundleNameData.shortName, parent.depth + 1, itemId);
			AddBundle(bundle, parent);
			return bundle;
		}

		public BundleTreeFolderItem CreateBundleFolder(string folderName, BundleTreeFolderItem parent = null, bool checkUnique = false,bool pathHashAsId = false)
		{
			if (string.IsNullOrEmpty(folderName))
			{
				folderName = DefaultFolderName;
			}

			if (folderName.IndexOf("/") > -1)
			{
				return CreateBundleFolderByPath(folderName, parent, checkUnique, pathHashAsId);
			}
			else
			{
				return CreateBundleFolderByName(folderName, parent, checkUnique, pathHashAsId);
			}
		}

		public BundleTreeFolderItem CreateEmptyFolder(BundleTreeFolderItem parent)
		{
			return CreateBundleFolder(DefaultFolderName, parent);
		}

		public BundleTreeFolderItem GetBundleFolder(string folderPath, BundleTreeFolderItem parent)
		{
			List<string> pathTokens = BundleNameData.GetPathTokens(folderPath);
			return GetBundleFolder(pathTokens, pathTokens.Count, parent);
		}

		private BundleTreeFolderItem GetBundleFolder(List<string> pathTokens, BundleTreeFolderItem parent)
		{
			return GetBundleFolder(pathTokens, pathTokens.Count, parent);
		}

		private BundleTreeFolderItem GetBundleFolder(List<string> pathTokens, int endIndex, BundleTreeFolderItem parent)
		{
			string bundleName = null;
			BundleTreeItem bundleInfo = null;

			for (int i = 0; i < endIndex; ++i)
			{
				bundleName = pathTokens[i];
				bundleInfo = parent.GetChild(bundleName);

				if (bundleInfo == null)
				{
					bundleInfo = CreateBundleFolder(bundleName, parent);
				}
				else if (bundleInfo is BundleTreeDataItem)
				{
					Debug.LogErrorFormat("GetBundleFolder:{0} is not bundle folder", string.IsNullOrEmpty(parent.fullName) ? bundleName : (parent.fullName + "/" + bundleName));
					return null;
				}

				parent = bundleInfo as BundleTreeFolderItem;
			}
			return parent;
		}

		public bool RenameBundle(BundleTreeItem bundle, string newName)
		{
			if (bundle.displayName == newName|| string.IsNullOrEmpty(newName) || newName.IndexOf("/")>-1 || newName.IndexOf("\\")>-1)
			{
				return false;
			}

			bundle.displayName = newName;

			//更新info信息
			BundleTreeDataItem dataBundle = bundle as BundleTreeDataItem;
			if (dataBundle != null)
			{
				if (dataBundle.bundleInfo != null)
				{
					dataBundle.bundleInfo.name = dataBundle.fullName;
				}
				else
				{
					bundle.id = dataBundle.fullName.GetHashCode();
				}
			}
			else
			{
				BundleTreeFolderItem folderBundle = bundle as BundleTreeFolderItem;
				if (folderBundle != null)
				{
					RefreshBundleFolder(folderBundle);
				}
			}

			return true;
		}

		public bool ChangeBundleParent(BundleTreeItem bundle, BundleTreeFolderItem parent)
		{
			if (parent == null)
			{
				parent = m_RootItem;
			}

			BundleTreeFolderItem originParent = bundle.parent as BundleTreeFolderItem;
			//remove from old parent
			if (originParent!=null && originParent.children != null)
			{
				originParent.children.Remove(bundle);
			}

			//add to new parent
			AddBundle(bundle, parent);

			//update bundle info
			BundleTreeDataItem dataBundle = bundle as BundleTreeDataItem;
			if (dataBundle != null)
			{
				if (dataBundle.bundleInfo != null)
				{
					dataBundle.bundleInfo.name = dataBundle.fullName;
				}
				else
				{
					dataBundle.id = dataBundle.fullName.GetHashCode();
				}
			}
			else
			{
				BundleTreeFolderItem folderBundle = bundle as BundleTreeFolderItem;
				if (folderBundle != null)
				{
					RefreshBundleFolder(folderBundle);
				}
			}

			SaveBundles();

			return true;
		}

		private void UpdateBundleDataInfo(BundleTreeDataItem dataBundle)
		{
			if (dataBundle != null)
			{
				if (dataBundle.bundleInfo != null)
				{
					dataBundle.bundleInfo.name = dataBundle.fullName;
				}

				dataBundle.id = dataBundle.fullName.GetHashCode();
			}
		}

		private void RefreshBundleFolder(BundleTreeFolderItem bundleFolder)
		{
			Stack<BundleTreeFolderItem> folders = new Stack<BundleTreeFolderItem>();
			folders.Push(bundleFolder);
			BundleTreeFolderItem current = null;
			List<string> pathTokens = new List<string>();
			while (folders.Count > 0)
			{
				current = folders.Pop();
				if (current.children != null)
				{
					foreach (var child in current.children)
					{
						var item = child as BundleTreeItem;
						BundleTreeDataItem dataBundle = child as BundleTreeDataItem;
						if (dataBundle != null)
						{
							UpdateBundleDataInfo(dataBundle);
						}
						else
						{
							BundleTreeFolderItem childFolder = child as BundleTreeFolderItem;
							if (childFolder != null)
							{
								folders.Push(childFolder);
							}
						}
					}
				}
			}
		}

		private void UpdateBundleInfo(BundleTreeItem bundle)
		{
			//更新info信息
			BundleTreeDataItem dataBundle = bundle as BundleTreeDataItem;
			if (dataBundle != null)
			{
				UpdateBundleDataInfo(dataBundle);
			}
			else
			{
				BundleTreeFolderItem folderBundle = bundle as BundleTreeFolderItem;
				if (folderBundle != null)
				{
					RefreshBundleFolder(folderBundle);
				}
			}
		}


		private string GetUniqueName(string name, BundleTreeFolderItem parent)
		{
			int i = 0;
			string newName = name;
			while (parent.GetChild(newName) != null)
			{
				++i;
				newName = string.Format("{0}_{1}",name , i);
			}
			return newName;
		}

		public BundleTreeDataItem HandleDedupeBundles(IEnumerable<BundleTreeDataItem> bundles, bool onlyOverlappedAssets)
		{
			//var newBundle = CreateEmptyBundle();
			//HashSet<string> dupeAssets = new HashSet<string>();
			//HashSet<string> fullAssetList = new HashSet<string>();

			////if they were just selected, then they may still be updating.
			//bool doneUpdating = s_BundlesToUpdate.Count == 0;
			//while (!doneUpdating)
			//	doneUpdating = Update();

			//foreach (var bundle in bundles)
			//{
			//	foreach (var asset in bundle.GetDependencies())
			//	{
			//		if (onlyOverlappedAssets)
			//		{
			//			if (!fullAssetList.Add(asset.fullAssetName))
			//				dupeAssets.Add(asset.fullAssetName);
			//		}
			//		else
			//		{
			//			if (asset.IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles))
			//				dupeAssets.Add(asset.fullAssetName);
			//		}
			//	}
			//}

			//if (dupeAssets.Count == 0)
			//	return null;

			//MoveAssetToBundle(dupeAssets, newBundle.m_Name.bundleName, string.Empty);
			//ExecuteAssetMove();
			//return newBundle;
			return null;
		}

		public void RemoveBundles(List<BundleTreeItem> bundletems)
		{
			foreach (var bundleItem in bundletems)
			{
				RemoveBundle(bundleItem);
			}

			SaveBundles();
		}

		public void HandleBundleMerge(List<BundleTreeDataItem> bundles,BundleTreeDataItem target)
		{
		
		}

		public void HandleBundleReparent(List<BundleTreeDataItem> bundles, BundleTreeFolderItem parent)
		{
		
		}

		#endregion //Bundle

		#region Asset

		public BundleTreeDataItem CreateBundleFromAsset(string assetName, BundleTreeFolderItem parent=null)
		{
			AssetInfo assetInfo = EditorAssetBundleManager.Instance.GetOrCreateAsset(assetName);
			EditorAssetBundleManager.Instance.RefreshAssetDependencies(assetInfo);
			EditorAssetBundleManager.Instance.RefreshAssetAllDependencies(assetInfo);

			string bundleName = EditorAssetBundleManager.Instance.CreateBundleName(assetName, true, true, false);
			BundleInfo bundle = EditorAssetBundleManager.Instance.CreateBundle(bundleName, assetInfo);
			//EditorAssetBundleManager.Instance.RefreshBundleRelations(bundle);
			//EditorAssetBundleManager.Instance.RefreshAllBundlesName();
			//EditorAssetBundleManager.Instance.Combine();
			RefreshBundles();
			return GetBundle(bundleName, parent) as BundleTreeDataItem;			
		}

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
		public void RefreshAssetDependencies(AssetNode assetInfo)
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
		public void RefreshAssetAllDependencies(AssetNode assetInfo)
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

		public void RefreshAssetAllDependencies2(AssetNode assetInfo)
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

		public void MoveAssetToBundle(string assetName, string bundleName)
		{
		}

		public void MoveAssetToBundle(IEnumerable<string> assetNames, string bundleName)
		{
			foreach (var assetName in assetNames)
				MoveAssetToBundle(assetName, bundleName);
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

		#region Icons
		static private Texture2D s_folderIcon = null;
		static private Texture2D s_bundleIcon = null;
		static private Texture2D s_sceneIcon = null;

		static internal Texture2D GetFolderIcon()
		{
			if (s_folderIcon == null)
				FindBundleIcons();
			return s_folderIcon;
		}
		static internal Texture2D GetBundleIcon()
		{
			if (s_bundleIcon == null)
				FindBundleIcons();
			return s_bundleIcon;
		}
		static internal Texture2D GetSceneIcon()
		{
			if (s_sceneIcon == null)
				FindBundleIcons();
			return s_sceneIcon;
		}
		static private void FindBundleIcons()
		{
			s_folderIcon = EditorGUIUtility.FindTexture("Folder Icon");

			var packagePath = System.IO.Path.GetFullPath("Packages/com.unity.assetbundlebuilder");
			if (System.IO.Directory.Exists(packagePath))
			{
				s_bundleIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.unity.assetbundlebuilder/Editor/Icons/ABundleBrowserIconY1756Basic.png", typeof(Texture2D));
				s_sceneIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.unity.assetbundlebuilder/Editor/Icons/ABundleBrowserIconY1756Scene.png", typeof(Texture2D));
			}
		}
		#endregion
	}
}
