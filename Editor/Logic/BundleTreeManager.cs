using System.IO;
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

		DataSource.DataSource m_DataSource;
		Type m_DefaultDataSourceType = typeof(JsonDataSource);

		BundleTreeFolderItem m_RootItem;

		private List<string> m_TempPathTokens = new List<string>();

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
			m_RootItem = new BundleTreeFolderItem(null, -1, 0);
		}

		public void Clean()
		{
		}

		public void Clear()
		{
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
			if (parent != null && bundle.parent!=parent)
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

		private BundleTreeFolderItem ParseBundleName(string bundlePath, ref BundleNameData bundleNameData, BundleTreeFolderItem parent, int offset = 0, bool uniqueName = true)
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

			return parent;
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

		public BundleTreeDataItem CreateBundleDataByName(string bundleName, BundleTreeFolderItem parent = null)
		{
			if (parent == null)
			{
				parent = m_RootItem;
			}


			bundleName = GetUniqueName(bundleName, parent);

			string fullPath =string.IsNullOrEmpty(parent.fullName)? bundleName : parent.fullName + "/" + bundleName;

			BundleInfo bundleInfo = EditorAssetBundleManager.Instance.GetOrCreateBundle(fullPath);
			BundleTreeDataItem bundleItem = new BundleTreeDataItem(bundleInfo, parent.depth + 1);
			AddBundle(bundleItem, parent);
			return bundleItem;
		}

		public BundleTreeDataItem CreateBundleDataByPath(string bundlePath, BundleTreeFolderItem parent = null)
		{
			BundleNameData bundleNameData = new BundleNameData();
			parent = ParseBundleName(bundlePath, ref bundleNameData, parent, 0, true);

			BundleInfo bundleInfo = EditorAssetBundleManager.Instance.GetOrCreateBundle(bundleNameData.fullNativeName);
			BundleTreeDataItem bundleItem = new BundleTreeDataItem(bundleInfo, parent.depth + 1);

			AddBundle(bundleItem, parent);

			return bundleItem;
		}

		public BundleTreeDataItem CreateBundleData(string bundleName, BundleTreeFolderItem parent = null)
		{
			if (string.IsNullOrEmpty(bundleName))
			{
				return null;
			}

			BundleTreeDataItem bundleItem = null;

			if (bundleName.IndexOf("/") > -1)
			{
				bundleItem = CreateBundleDataByPath(bundleName,parent);
			}
			else
			{
				bundleItem = CreateBundleDataByName(bundleName, parent);
			}

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
			parent = ParseBundleName(folderPath, ref bundleNameData, parent, 0, checkUnique);

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

		public BundleTreeFolderItem GetBundleFolder(string folderPath, BundleTreeFolderItem parent, int offset=0)
		{
			m_TempPathTokens.Clear();
			BundleNameData.GetPathTokens(folderPath, ref m_TempPathTokens);
			return GetBundleFolder(m_TempPathTokens, parent, offset);
		}

		private BundleTreeFolderItem GetBundleFolder(List<string> pathTokens, BundleTreeFolderItem parent,int offset =0)
		{
			return GetBundleFolder(pathTokens, pathTokens.Count - offset, parent);
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
			UpdateBundleInfo(bundle);
			return true;
		}

		public bool IsAncestor(TreeViewItem child, TreeViewItem ancestor)
		{
			TreeViewItem p = child.parent;
			while (p != null)
			{
				if (p == ancestor)
				{
					return true;
				}
				p = p.parent;
			}
			return false;
		}

		public bool ChangeBundleParent(BundleTreeItem bundle, BundleTreeFolderItem parent)
		{
			if (parent == null)
			{
				parent = m_RootItem;
			}

			Debug.LogFormat("ChangeBundleParent from {0},{1}", bundle.parent.displayName, parent.displayName);
			BundleTreeFolderItem originParent = bundle.parent as BundleTreeFolderItem;
			//父结点相同不移动。
			if (originParent == parent)
			{
				Debug.LogFormat("ChangeBundleParent same parent {0}", bundle.parent.displayName);
				return false;
			}

			//不能把父结点移动到子结点
			if (IsAncestor(parent, bundle))
			{
				Debug.LogFormat("ChangeBundleParent parent can't be children {0},{1}", bundle.parent.displayName, parent.displayName);
				return false;
			}

			//检查同名
			BundleTreeItem child = parent.GetChild(bundle.displayName);
			if (child != null)
			{
				BundleTreeFolderItem folderItem = bundle as BundleTreeFolderItem;
				if (folderItem != null)
				{
					if (child is BundleTreeFolderItem)
					{
						MergeBundleFolder(folderItem, child as BundleTreeFolderItem);
						return true;
					}
				}
				else if(bundle is BundleTreeDataItem && child is BundleTreeDataItem)
				{
					bundle.displayName = GetUniqueName(bundle.displayName, parent);
				}
			}

			//remove from old parent
			if (originParent!=null && originParent.children != null)
			{
				originParent.children.Remove(bundle);
			}

			//add to new parent
			AddBundle(bundle, parent);
			bundle.depth = parent.depth + 1;

			//update bundle info
			UpdateBundleInfo(bundle);

			return true;
		}

		public void MergeBundleFolder(BundleTreeFolderItem from, BundleTreeFolderItem to)
		{
			if (from.children != null)
			{
				foreach (var child in from.children)
				{
					ChangeBundleParent(child as BundleTreeItem, to);
				}
			}
		}

		public void MergeBundleData(BundleTreeDataItem from, BundleTreeDataItem to)
		{
			if (from.bundleInfo != null && to.bundleInfo != null)
			{
				EditorAssetBundleManager.Instance.MergeBundle(from.bundleInfo, to.bundleInfo);
				RemoveBundle(from);
			}
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
						child.depth = child.parent.depth + 1;

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

		public BundleTreeDataItem HandleDedupeBundles(IEnumerable<BundleTreeItem> bundles, bool onlyOverlappedAssets)
		{
			var newBundle = CreateEmptyBundle();
			HashSet<AssetInfo> dupeAssets = new HashSet<AssetInfo>();
			HashSet<AssetInfo> fullAssetList = new HashSet<AssetInfo>();

			foreach (var bundle in bundles)
			{
				BundleTreeDataItem dataItem = bundle as BundleTreeDataItem;

				if (dataItem!=null && dataItem.bundleInfo != null)
				{
					HashSet<AssetInfo> includeAssets = new HashSet<AssetInfo>();
					EditorAssetBundleManager.Instance.GetAssetsIncludeByBundle(dataItem.bundleInfo,ref includeAssets);

					foreach (var asset in includeAssets)
					{
						if (onlyOverlappedAssets)
						{
							//bundle中包含的重复的资源
							if (!fullAssetList.Add(asset))
							{
								dupeAssets.Add(asset);
							}
						}
						else
						{
							//不仅在所选的重复资源，所选的资源和其他重复的资源。
							//if (asset.IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles))
							//	dupeAssets.Add(asset.fullAssetName);
						}
					}
				}
			}

			if (dupeAssets.Count == 0)
				return null;

			MoveAssetsToBundle(dupeAssets, newBundle);
			SaveBundles();
			return newBundle;
		}

		public void RemoveBundles(List<BundleTreeItem> bundletems)
		{
			foreach (var bundleItem in bundletems)
			{
				RemoveBundle(bundleItem);
			}

			SaveBundles();
		}

		public void HandleBundleMerge(ICollection<BundleTreeItem> bundles, BundleTreeDataItem target)
		{
			Debug.Log("HandleBundleMerge");
			foreach (var bundle in bundles)
			{
				BundleTreeDataItem dataItem = bundle as BundleTreeDataItem;
				if (dataItem != null)
				{
					MergeBundleData(dataItem, target);
				}
			}
			SaveBundles();
		}

		public void HandleBundleReparent(List<BundleTreeItem> bundles, BundleTreeFolderItem parent)
		{
			Debug.Log("HandleBundleReparent");
			foreach (var item in bundles)
			{
				ChangeBundleParent(item, parent);
			}

			SaveBundles();
		}

		public BundleTreeDataItem HandleCreateAssetsOneBundle(ICollection<string> assetPaths, BundleTreeFolderItem parent)
		{
			var newBundle = CreateEmptyBundle(parent);
			foreach (var assetPath in assetPaths)
			{
				EditorAssetBundleManager.Instance.AddAssetToBundle(newBundle.bundleInfo, assetPath);
			}
			SaveBundles();
			return newBundle;
		}

		public List<int> HandleCreateAssetsMultiBundle(ICollection<string> assetPaths, BundleTreeFolderItem parent)
		{
			List<int> bundleIds = new List<int>();

			foreach (var assetPath in assetPaths)
			{
				string fullPath = assetPath;
				BundleTreeDataItem bundleDataItem = CreateBundleFromAsset(fullPath, parent);
				if (bundleDataItem != null)
				{
					bundleIds.Add(bundleDataItem.id);
				}
				else
				{
					Debug.LogErrorFormat("Create AssetBundle from {0} fail", fullPath);
				}
			}

			SaveBundles();
			return bundleIds;
		}

		public void HandleMoveAssetsToBundle(ICollection<string> assetPaths, BundleTreeDataItem target)
		{
			if (target.bundleInfo == null)
			{
				return;
			}

			EditorAssetBundleManager.Instance.AddAssetsToBundle(target.bundleInfo, assetPaths);

			SaveBundles();
		}

		public void HandleMoveAssetsToBundle(ICollection<AssetInfo> assets, BundleTreeDataItem target)
		{
			if (target==null || target.bundleInfo == null)
			{
				return;
			}

			EditorAssetBundleManager.Instance.AddAssetsToBundle(target.bundleInfo, assets);

			SaveBundles();
		}

		public void HandleRemoveAssetsBundle(ICollection<AssetInfo> assets)
		{
			EditorAssetBundleManager.Instance.RemoveAssetsBundle(assets);
			SaveBundles();
		}

		#endregion //Bundle

		#region Asset

		public BundleTreeDataItem CreateBundleFromAsset(string assetName)
		{
			//Debug.LogFormat("CreateBundleFromAsset {0}", assetName);
			
			string bundleFullName = EditorAssetBundleManager.Instance.CreateBundleName(assetName, true, true, false);

			BundleTreeDataItem bundleData = GetBundle(bundleFullName) as BundleTreeDataItem;
			if (bundleData == null)
			{
				bundleData = CreateBundleDataByPath(assetName);
			}

			EditorAssetBundleManager.Instance.AddAssetToBundle(bundleData.bundleInfo, assetName);
			return bundleData;
		}

		public BundleTreeDataItem CreateBundleFromAsset(string assetName, BundleTreeFolderItem parent)
		{
			if (parent == null)
			{
				parent = m_RootItem;
			}

			string bundleName = EditorAssetBundleManager.Instance.CreateBundleName(assetName, false, true, false);
			BundleTreeDataItem bundleData = parent.GetChild(bundleName) as BundleTreeDataItem;
			if (bundleData == null)
			{
				bundleData = CreateBundleDataByName(bundleName,parent);
			}

			EditorAssetBundleManager.Instance.AddAssetToBundle(bundleData.bundleInfo, assetName);
			return bundleData;
		}

		public void MoveAssetToBundle(string assetName, string bundleName)
		{
		}

		public void MoveAssetToBundle(IEnumerable<string> assetNames, string bundleName)
		{
			foreach (var assetName in assetNames)
				MoveAssetToBundle(assetName, bundleName);
		}

		public void MoveAssetToBundle(AssetInfo asset, BundleTreeDataItem bundle)
		{
			EditorAssetBundleManager.Instance.AddAssetToBundle(bundle.bundleInfo,asset);
		}

		public void MoveAssetsToBundle(IEnumerable<AssetInfo> assets, BundleTreeDataItem bundle)
		{
			foreach (var asset in assets)
				MoveAssetToBundle(asset, bundle);
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
