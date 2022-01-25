using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
	/*
	 *  AssetNode和BundleNode的关系：
	 *  1.如果一个asset只属于一个bundle，则可以建立AssetNode指向BundleNode的属性。
	 *  2.如果一个asset可属于多个bundle，则不能建立AssetNode指向BundleNode的属性。把asset添加到bundle时会这被覆盖掉。如何建立asset到bundle之间的映射关系?
	 */
	public partial class EditorAssetBundleManager
	{
		//assets
		Dictionary<string, AssetInfo> m_Assets = new Dictionary<string, AssetInfo>();

		////bundles
		////Key:asset relative path
		//Dictionary<string, BundleNode> m_AssetBundles = new Dictionary<string, BundleNode>();
		//Key:bundle name
		//Dictionary<string, BundleNode> m_Bundles = new Dictionary<string, BundleNode>();
		List<BundleInfo> m_Bundles = new List<BundleInfo>(4096);

		private Stack<AssetInfo> m_VisitAssetsStack = new Stack<AssetInfo>();
		private HashSet<AssetInfo> m_VisitedAssets = new HashSet<AssetInfo>();

		private static EditorAssetBundleManager m_Instance = null;
		public static EditorAssetBundleManager Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = new EditorAssetBundleManager();
					m_Instance.Init();
				}
				return m_Instance;
			}
		}

		public Dictionary<string, AssetInfo> assets
		{
			get
			{
				return m_Assets;
			}
			set
			{
				m_Assets = value;
			}
		}

		public List<BundleInfo> bundles
		{
			get
			{
				return m_Bundles;
			}
			set
			{
				m_Bundles = value;
			}
		}


		public void Init()
		{
			
		}

		public void Clean()
		{
			CleanAssets();
			CleanBundles();
		}

		public void CleanAssets()
		{
			if (m_Assets != null)
			{
				m_Assets.Clear();
			}
		}

		public void CleanBundles()
		{
			if (m_Bundles != null)
			{
				m_Bundles.Clear();
			}
		}

		#region Asset
		public AssetInfo CreateAssetInfo(string assetPath)
		{
			//if (!ValidateAsset(assetPath))
			//{
			//	return null;
			//}

			string realPath = assetPath;
			if (Path.IsPathRooted(assetPath))
			{
				realPath = assetPath;
				assetPath = FileSystem.Relative(FileSystem.applicationPath, assetPath);
			}
			else
			{
				assetPath = FileSystem.AddAssetPrev(assetPath);
				realPath = Path.Combine(FileSystem.applicationPath, assetPath);
			}

			assetPath = FileSystem.NormalizePath(assetPath);

			AssetInfo assetInfo = new AssetInfo(assetPath, realPath);
			return assetInfo;
		}

		public AssetInfo CreateAsset(string assetPath)
		{
			//if (!ValidateAsset(assetPath))
			//{
			//	return null;
			//}

			AssetInfo assetInfo = CreateAssetInfo(assetPath);
			if (assetInfo != null)
			{
				m_Assets[assetInfo.assetPath] = assetInfo;
			}
			return assetInfo;
		}

		public AssetInfo GetAsset(string assetPath)
		{
			AssetInfo assetInfo = null;
			m_Assets.TryGetValue(assetPath, out assetInfo);
			return assetInfo;
		}

		public AssetInfo GetOrCreateAsset(string assetPath)
		{
			AssetInfo assetInfo = null;
			if (!m_Assets.TryGetValue(assetPath, out assetInfo))
			{
				assetInfo = CreateAsset(assetPath);
			}
			return assetInfo;
		}

		/// <summary>
		/// 刷新资源的直接依赖
		/// </summary>
		/// <param name="asset"></param>
		public void RefreshAssetDependencies(AssetInfo asset)
		{
			if(asset.dependencyDirty)
			{
				//clear deps

				asset.dependencyDirty = false;

				foreach (var dep in AssetDatabase.GetDependencies(asset.assetPath, false))
				{
					if (ValidateAsset(dep) && dep != asset.assetPath)
					{
						AssetInfo depAsset = GetOrCreateAsset(dep);
						if (depAsset != null)
						{
							depAsset.AddRefer(asset);
							RefreshAssetDependencies(depAsset);
						}
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
		/// <param name="asset"></param>
		public void RefreshAssetAllDependencies(AssetInfo asset)
		{
			Stack<AssetInfo> assetsStack = new Stack<AssetInfo>();
			HashSet<AssetInfo> visiteds = new HashSet<AssetInfo>();

			assetsStack.Push(asset);

			while (assetsStack.Count > 0)
			{
				AssetInfo current = assetsStack.Pop();
				if (visiteds.Contains(current))
				{
					continue;
				}

				visiteds.Add(current);

				if (current.dependencies != null && current.dependencies.Count > 0)
				{
					foreach (var dep in current.dependencies)
					{
						if (asset != dep)
						{
							asset.allDependencies.Add(dep);
							assetsStack.Push(dep);
						}
					}
				}
			}
		}

		public void RefreshAssetAllDependencies2(AssetInfo asset)
		{
			//celar all deps
			if (!asset.dependencyDirty)
			{
				return;
			}

			asset.dependencyDirty = false;

			foreach (var dep in AssetDatabase.GetDependencies(asset.assetPath, true))
			{
				if (ValidateAsset(dep) && dep != asset.assetPath)
				{
					AssetInfo depAsset = GetOrCreateAsset(dep);
					if (depAsset != null && asset!= depAsset)
					{
						asset.allDependencies.Add(depAsset);
						RefreshAssetAllDependencies2(depAsset);
					}
				}
			}
		}

		/// <summary>
		/// 清除资源之间的依赖关系
		/// </summary>
		public void ClearAllAssetRelations()
		{
			foreach (var iter in m_Assets)
			{
				iter.Value.dependencyDirty = true;
				iter.Value.dependencies.Clear();
				iter.Value.refers.Clear();
				iter.Value.allDependencies.Clear();
			}
		}

		/// <summary>
		/// 更新所有资源的直接依赖
		/// </summary>
		public void RefreshAllAssetDependencies()
		{
			//必须提前清除。如果和创建再一个循序清除，会导致数据丢失。
			ClearAllAssetRelations();

			List<AssetInfo> assets = new List<AssetInfo>(m_Assets.Values);
			foreach (var asset in assets)
			{
				RefreshAssetDependencies(asset);
			}
		}

		/// <summary>
		/// 更新所有资源的所有依赖
		/// </summary>
		public void RefreshAllAssetAllDependencies()
		{
			List<AssetInfo> assets = new List<AssetInfo>(m_Assets.Values);
			foreach (var asset in assets)
			{
				RefreshAssetAllDependencies(asset);
			}
		}

		public void RefreshAllAssetAllDependencies2()
		{
			List<AssetInfo> assets = new List<AssetInfo>(m_Assets.Values);
			foreach (var asset in assets)
			{
				RefreshAssetAllDependencies2(asset);
			}
		}

		/// <summary>
		/// 清除不在使用的资源。
		/// 不包含在AssetBundle中
		/// 不被其它包含在AssetBundle中的资源引用。
		/// </summary>
		public void ClearUnUseAssets()
        {
			List<AssetInfo> noBundleAssets = new List<AssetInfo>(m_Assets.Count);
			//找出没有AssetBundle的Assets
			foreach(var iter  in m_Assets)
            {
                if (!IsAssetBunded(iter.Value))
                {
					noBundleAssets.Add(iter.Value);
				}
            }

			//从assets中移除
			foreach(var asset in noBundleAssets)
            {
				m_Assets.Remove(asset.assetPath);
            }
        }

		public bool IsAssetBunded(AssetInfo asset)
        {
			Stack<AssetInfo> checkingAssets = new Stack<AssetInfo>();
			HashSet<AssetInfo> checkedAssets=new HashSet<AssetInfo>();
			checkingAssets.Push(asset);

			while (checkingAssets.Count > 0)
			{
				asset= checkingAssets.Pop();
                if (checkedAssets.Contains(asset))
                {
					continue;
                }
				checkedAssets.Add(asset);

				//有没有直接包含在bundle中
				if (asset.bundle != null)
				{
					return true;
				}

				if (asset.refers != null)
				{
					foreach (var refer in asset.refers)
					{
						  checkingAssets.Push(refer);
					}
				}
			}
			return false;
        }

		public static bool ValidateAsset(string name)
		{
			if (!name.StartsWith("Assets/"))
				return false;
			string ext = Path.GetExtension(name);
			if (ext == ".dll" || ext == ".cs" || ext == ".meta" || ext == ".js" || ext == ".boo")
				return false;

			return true;
		}

		#endregion //Asset

		#region Bundle
		public BundleInfo CreateBundleInfo(string bundleName)
		{
			BundleInfo bundleInfo = new BundleInfo(bundleName);
			return bundleInfo;
		}

		public BundleInfo CreateBundle(string bundleName)
		{
			BundleInfo bundle = new BundleInfo(bundleName);
			m_Bundles.Add(bundle);
			return bundle;
		}

		public BundleInfo CreateBundle(string bundleName,string variantName)
		{
			BundleInfo bundle = new BundleInfo(bundleName,variantName);
			m_Bundles.Add(bundle);
			return bundle;
		}

		public BundleInfo CreateBundle(string bundleName, AssetInfo assetInfo)
		{
			BundleInfo bundle = CreateBundle(bundleName);
			bundle.SetMainAsset(assetInfo);
			bundle.AddAsset(assetInfo);
			//m_AssetBundles[assetNode.assetPath] = bundle;
			return bundle;
		}

		//public BundleNode CreateBundle(string bundleName,string assetPath)
		//{
		//	BundleNode bundle = CreateBundle(bundleName);
		//	bundle.SetMainAsset(assetPath);
		//	m_AssetBundles[assetPath] = bundle;
		//	return bundle;
		//}

		public BundleInfo GetBundle(string bundleName)
		{
			foreach (var bundle in m_Bundles)
			{
				if (bundle.name!=null && bundle.name.Equals(bundleName,StringComparison.OrdinalIgnoreCase))
				{
					return bundle;
				}
			}
			return null;
		}

		public BundleInfo GetOrCreateBundle(string bundleName)
		{
			BundleInfo bundle = GetBundle(bundleName);
			if (bundle == null)
			{
				bundle = CreateBundle(bundleName);
			}
			return bundle;
		}

		public void RemoveBundle(BundleInfo bundle)
		{
			m_Bundles.Remove(bundle);
			bundle.enbale = false;
			bundle.Clear();
		}

		public void RemoveBundlesByCreateTag(uint tag)
        {
			for(int i = m_Bundles.Count - 1; i >= 0; i--)
            {
				BundleInfo bundle = m_Bundles[i];
				if(bundle.HaveCreateTag(tag))
                {
					m_Bundles.RemoveAt(i);
					bundle.enbale=false;
					bundle.Clear();
				}
            }
        }

		public BundleInfo MergeBundle(BundleInfo from, BundleInfo to)
		{
			Debug.LogFormat("merge bundle {0} to {1}",from.name,to.name);
			//合并资源
			foreach (var asset in from.assets)
			{
				to.AddAsset(asset);
			}

			//合并引用
			foreach (var refer in from.refers)
			{
				if (refer != to)
				{
					to.AddReferOnly(refer);
					refer.RemoveDependencyOnly(from);
					refer.AddDependencyOnly(to);
				}
			}

			//合并依赖
			foreach (var dep in from.dependencies)
			{
				if (dep != to)
				{
					to.AddDependencyOnly(dep);
					dep.RemoveReferOnly(from);
					dep.AddReferOnly(to);
				}
			}

			//如果from在to的refers或dependencies中(循环引用)，则移除。
			if (to.refers.Contains(from))
			{
				to.RemoveReferOnly(from);
			}

			if (to.dependencies.Contains(from))
			{
				to.RemoveDependencyOnly(from);
			}

			//Repalce from assets.
			//ReplaceBundle(from, to);

			//remove from	bundle node
			RemoveBundle(from);

			return to;
		}

		public void RefreshBundleDependencies(BundleInfo bundle)
		{
			m_VisitAssetsStack.Clear();
			m_VisitedAssets.Clear();

			foreach (AssetInfo assetNode in bundle.assets)
			{
				m_VisitAssetsStack.Push(assetNode);
			}

			AssetInfo currentAsset = null;
			while (m_VisitAssetsStack.Count > 0)
			{
				currentAsset = m_VisitAssetsStack.Pop();
				if (m_VisitedAssets.Contains(currentAsset))
				{
					continue;
				}
				m_VisitedAssets.Add(currentAsset);

				//add dep
				foreach (AssetInfo assetDep in currentAsset.dependencies)
				{
					if (assetDep.bundle != null)
					{
						bundle.AddDependencyOnly(assetDep.bundle);
						assetDep.bundle.AddReferOnly(bundle);
					}
					else
					{
						m_VisitAssetsStack.Push(assetDep);
					}
				}
			}
		}

		public void RefreshBundleRelations(BundleInfo bundle)
		{
			m_VisitAssetsStack.Clear();
			m_VisitedAssets.Clear();

			foreach (AssetInfo assetNode in bundle.assets)
			{
				m_VisitAssetsStack.Push(assetNode);
			}

			AssetInfo currentAsset = null;
			while (m_VisitAssetsStack.Count > 0)
			{
				currentAsset = m_VisitAssetsStack.Pop();
				if (m_VisitedAssets.Contains(currentAsset))
				{
					continue;
				}
				m_VisitedAssets.Add(currentAsset);

				//add dep
				foreach (AssetInfo assetDep in currentAsset.dependencies)
				{
					if (assetDep.bundle != null)
					{
						bundle.AddDependencyOnly(assetDep.bundle);
						assetDep.bundle.AddReferOnly(bundle);
					}
					else
					{
						m_VisitAssetsStack.Push(assetDep);
					}
				}

				//add refer
				foreach (AssetInfo assetRef in currentAsset.refers)
				{
					if (assetRef.bundle != null)
					{
						bundle.AddReferOnly(assetRef.bundle);
						assetRef.bundle.AddDependencyOnly(bundle);
					}
					else
					{
						m_VisitAssetsStack.Push(assetRef);
					}
				}
			}
		}

		/// <summary>
		/// 清除Bundle之间的依赖关系
		/// </summary>
		public void ClearAllBundleRelations()
		{
			foreach (var bundle in m_Bundles)
			{
				ClearBundleRelations(bundle);
			}
		}

		public void ClearBundleRelations(BundleInfo bundle)
		{
			bundle.dependencies.Clear();
			bundle.refers.Clear();
		}

		public void RefreshAllBundleDependencies()
		{
			ClearAllBundleRelations();

			List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
			foreach (var bundle in bundles)
			{
				RefreshBundleDependencies(bundle);
			}
		}

		public void RefreshAllBundleRelations()
		{
			ClearAllBundleRelations();

			List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
			foreach (var bundle in bundles)
			{
				RefreshBundleRelations(bundle);
			}
		}

		public string CreateBundleName(string filePath, bool useFullPath, bool useExt, bool flatPath)
		{
			if (string.IsNullOrEmpty(filePath))
			{
				return null;
			}

			if (useFullPath)
			{
				if (flatPath)
				{
					if (!useExt && !filePath.Contains(".unity"))//Scene always use ext
					{
						string dir=Path.GetDirectoryName(filePath);
						string baseName = Path.GetFileNameWithoutExtension(filePath);
						filePath = dir + "/" + baseName;
					}
					return filePath.Replace('/', '_').Replace('\\', '_').Replace('.', '_').ToLower();
				}
				else
				{
					string baseName = null;
					if (useExt || filePath.Contains(".unity"))//Scene always use ext
					{
						baseName = Path.GetFileName(filePath).Replace('.', '_').ToLower();
					}
					else
					{
						baseName = Path.GetFileNameWithoutExtension(filePath).ToLower();
					}
					return Path.Combine(Path.GetDirectoryName(filePath), baseName).Replace("\\", "/");
				}
			}
			else
			{
				if (useExt || filePath.Contains(".unity"))//Scene always use ext
				{
					return Path.GetFileName(filePath).Replace('.', '_').ToLower();
				}
				else
				{
					return Path.GetFileNameWithoutExtension(filePath).ToLower();
				}
			}
		}

		public string CreateBundleName(string filePath, Setting.Format format)
		{
			bool useFullPath = (format & Setting.Format.FullPath) !=0;
			bool useExt = (format & Setting.Format.WithExt) != 0;
			bool flatPath = (format & Setting.Format.Flat) != 0;

			return CreateBundleName(filePath, useFullPath, useExt, flatPath);
		}

		public void RefreshAllBundlesName(Setting.Format format = Setting.Format.FullPath | Setting.Format.WithExt)
		{
			List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
			foreach (var bundle in bundles)
			{
				if (string.IsNullOrEmpty(bundle.name))
				{
					bundle.name = CreateBundleName(bundle.mainAssetPath, format);
				}
			}
		}


		public string[] GetAllAssetBundleNames()
        {
			List<string> bundleNames = new List<string>();
			foreach(var bundle in m_Bundles)
            {
				if (!string.IsNullOrEmpty(bundle.name))
				{
					bundleNames.Add(bundle.name);
				}
            }
			return bundleNames.ToArray();
        }

		#endregion //Bundle

		#region Asset Bundle

		public void CreateBundleForAllAssets(Setting.Format format=Setting.Format.FullPath|Setting.Format.WithExt)
		{
			foreach (var iter in m_Assets)
			{
				AssetInfo asset = iter.Value;

				if (asset.bundle == null)
				{
					string bundleName = CreateBundleName(asset.assetPath, format);
					asset.bundle = CreateBundle(bundleName, asset);
				}
			}
		}

		public void AddAssetToBundle(BundleInfo bundle, AssetInfo asset)
		{
			if (asset.bundle == bundle)
			{
				return;
			}

			if (asset.bundle != null)
			{
				RemoveAssetFromBundle(asset.bundle, asset);
			}

			if (bundle.AddAsset(asset))
			{
				if (bundle.mainAsset == null)
				{
					bundle.SetMainAsset(asset);
				}

				ClearBundleRelations(bundle);
				RefreshBundleRelations(bundle);
			}
		}

		public void AddAssetsToBundle(BundleInfo bundle, ICollection<AssetInfo> assets)
		{
			bool needRefresh = false;

			foreach (var asset in assets)
			{
				if (asset.bundle == bundle)
				{
					continue;
				}

				if (asset.bundle != null)
				{
					RemoveAssetFromBundle(asset.bundle, asset);
				}

				if (bundle.AddAsset(asset))
				{
					if (bundle.mainAsset == null)
					{
						bundle.SetMainAsset(asset);
					}
					needRefresh = true;
				}
			}

			if (needRefresh)
			{
				ClearBundleRelations(bundle);
				RefreshBundleRelations(bundle);
			}
		}

		public void AddAssetToBundle(BundleInfo bundle, string assetPath)
		{
			AssetInfo asset = GetOrCreateAsset(assetPath);
			RefreshAssetDependencies(asset);
			RefreshAssetAllDependencies(asset);
			Debug.LogFormat("AddAssetToBundle old bundle {0},new bundle {1}", asset.bundle != null ? asset.bundle.name : "null",bundle.name);
			AddAssetToBundle(bundle, asset);
		}

		public void AddAssetsToBundle(BundleInfo bundle, ICollection<string> assetPaths)
		{
			List<AssetInfo> assets = new List<AssetInfo>();
			foreach (var assetPath in assetPaths)
			{
				AssetInfo asset = GetOrCreateAsset(assetPath);
				RefreshAssetDependencies(asset);
				RefreshAssetAllDependencies(asset);
				assets.Add(asset);
			}
			AddAssetsToBundle(bundle, assets);
		}

		public void RemoveAssetFromBundle(BundleInfo bundle, AssetInfo asset)
		{
			Debug.LogFormat("Remove {0} from {1} ", asset.assetPath, bundle.name);
			if (bundle.RemoveAsset(asset))
			{
				if (bundle.mainAsset == asset)
				{
					//reset main asset
					foreach (var newMainAsset in bundle.assets)
					{
						bundle.SetMainAsset(newMainAsset);
					}
				}

				ClearBundleRelations(bundle);
				RefreshBundleRelations(bundle);
			}
		}

		public void RemoveAssetFromBundle(BundleInfo bundle, string assetPath)
		{
			AssetInfo asset = GetAsset(assetPath);
			if (asset != null)
			{
				RemoveAssetFromBundle(bundle, asset);
			}
		}

		public void RemoveAssetBundle(AssetInfo asset)
		{
			if (asset.bundle != null)
			{
				RemoveAssetFromBundle(asset.bundle, asset);
			}
		}

		public void RemoveAssetsBundle(ICollection<AssetInfo> assets)
		{
			HashSet<BundleInfo> needReshresBundles = new HashSet<BundleInfo>();

			foreach (var asset in assets)
			{
				BundleInfo bundle = asset.bundle;

				if (bundle != null)
				{
					if (bundle.RemoveAsset(asset))
					{
						if (bundle.mainAsset == asset)
						{
							//reset main asset
							foreach (var newMainAsset in bundle.assets)
							{
								bundle.SetMainAsset(newMainAsset);
							}
						}
						needReshresBundles.Add(bundle);
					}
				}
			}

			foreach (var bundle in needReshresBundles)
			{
				ClearBundleRelations(bundle);
				RefreshBundleRelations(bundle);
			}
		}

		public BundleInfo GetAssetBundle(string assetPath)
		{
			AssetInfo asset = null;
			if (m_Assets.TryGetValue(assetPath, out asset))
			{
				return asset.bundle;
			}
			return null;
		}

		public bool IsAssetHaveBundle(string assetPath)
		{
			AssetInfo asset = null;
			if (m_Assets.TryGetValue(assetPath, out asset))
			{
				return asset.bundle != null;
			}
			return false;
		}

		public void GetAssetsIncludeByBundle(BundleInfo bundle,ref HashSet<AssetInfo> includeAssets)
		{

			if (bundle.assets == null || bundle.assets.Count == 0)
			{
				return;
			}

			m_VisitAssetsStack.Clear();
			m_VisitedAssets.Clear();


			foreach (var asset in bundle.assets)
			{
				//直接包含的资源
				includeAssets.Add(asset);
				m_VisitAssetsStack.Push(asset);
			}

			//依赖的资源，且没有包含在其它Bundle中。
			AssetInfo currentAsset = null;
			while (m_VisitAssetsStack.Count > 0)
			{
				currentAsset = m_VisitAssetsStack.Pop();
				if (m_VisitedAssets.Contains(currentAsset))
				{
					continue;
				}
				m_VisitedAssets.Add(currentAsset);

				foreach (var dep in currentAsset.dependencies)
				{
					if (dep.bundle == null)
					{
						includeAssets.Add(dep);
						m_VisitAssetsStack.Push(dep);
					}
				}
			}
		}

		#endregion //Asset Bundle

		#region Optimizer
		protected void MergeShaderToShaderVariantCollection()
		{
			List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
			List<BundleInfo> deps = new List<BundleInfo>();
			foreach (var bundle in bundles)
			{
				if(bundle.enbale && bundle.isShaderVariantCollection && bundle.dependencies.Count>0)
				{
					deps.Clear();
					deps.AddRange(bundle.dependencies);
					foreach (var dep in deps)
					{
						MergeBundle(dep, bundle);
					}
				}
			}
		}

		/// <summary>
		/// 合并只有一个引用的项
		/// </summary>
		/// <returns></returns>
		protected bool MergeOneRefer()
		{
			bool merged = false;
			List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
			foreach (var bundle in bundles)
			{
				if (bundle.enbale &&  bundle.refers.Count == 1 && bundle.canMerge)
				{
					var iter = bundle.refers.GetEnumerator();
					iter.MoveNext();
					//检查目标是不是Scene。Scene所在的AssetBundle,不能包含其它资源
					if (!iter.Current.isScene)
					{
						merged = true;
						MergeBundle(bundle, iter.Current);
					}
				}
			}
			return merged;
		}

		/// <summary>
		/// 合并相同引用的项
		/// </summary>
		/// <returns></returns>
		protected bool MergeSameRefer()
		{
			bool merged = false;
			Dictionary<int, List<BundleInfo>> sameRefers = new Dictionary<int, List<BundleInfo>>();
			List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
			foreach (var bundle in bundles)
			{
				if (bundle.enbale && bundle.canMerge)
				{
					int hash = bundle.refersHashCode;
					if (hash != 0)
					{
						List<BundleInfo> items = null;
						if (!sameRefers.TryGetValue(hash, out items))
						{
							items = new List<BundleInfo>();
							sameRefers[hash] = items;
						}
						items.Add(bundle);
					}
				}
			}

			foreach (var iter in sameRefers)
			{
				if (iter.Value.Count > 1)
				{
					merged = true;
					for (int i = 1; i < iter.Value.Count; ++i)
					{
						MergeBundle(iter.Value[i], iter.Value[0]);
					}
				}
			}
			return merged;
		}

		//拼合资源
		public void Combine()
		{
			//只要执行一次就可以了。
			MergeShaderToShaderVariantCollection();

			int k = 0;
			do
			{
				int n = 0;
				while (MergeOneRefer())
				{
					++n;
				}
				Debug.Log("Merge one refer use " + n + " Times");
				++k;
			} while (MergeSameRefer());
			Debug.Log("Combine assets use " + k + " Times");
		}

		#endregion //Optimizer
	}
}
