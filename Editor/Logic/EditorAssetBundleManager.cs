﻿using System;
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

		Dictionary<uint, BundleInfo> m_BundlesIdMap = new Dictionary<uint, BundleInfo>();

		//private List<BundleNode> m_TempBundles = new List<BundleNode>(4096);
		//private List<BundleNode> m_TempBundleDeps = new List<BundleNode>(4096);

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

		public void Clean()
		{
			if (m_Assets != null)
			{
				m_Assets.Clear();
			}

			if (m_Bundles != null)
			{
				m_Bundles.Clear();
			}

			if (m_BundlesIdMap != null)
			{
				m_BundlesIdMap.Clear();
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
			if (!AssetDatabase.IsValidFolder(asset.assetPath))
			{
				//dep
				asset.dependencies.Clear();
				foreach (var dep in AssetDatabase.GetDependencies(asset.assetPath, false))
				{
					if (ValidateAsset(dep) && dep != asset.assetPath)
					{
						AssetInfo depAsset = GetAsset(dep);
						if (depAsset == null)
						{
							depAsset = CreateAsset(dep);
							RefreshAssetDependencies(depAsset);
						}

						depAsset.AddRefer(asset);
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
			//clear all deps
			if (asset.allDependencies == null)
			{
				asset.allDependencies = new HashSet<AssetInfo>();
			}
			else
			{
				asset.allDependencies.Clear();
			}

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
						asset.allDependencies.Add(dep);
						assetsStack.Push(dep);
					}
				}
			}
		}

		public void RefreshAssetAllDependencies2(AssetInfo asset)
		{
			//dep
			if (asset.allDependencies == null)
			{
				asset.allDependencies = new HashSet<AssetInfo>();
			}
			else
			{
				asset.allDependencies.Clear();
			}

			foreach (var dep in AssetDatabase.GetDependencies(asset.assetPath, true))
			{
				if (ValidateAsset(dep) && dep != asset.assetPath)
				{
					AssetInfo depAsset = GetAsset(dep);
					if (depAsset == null)
					{
						depAsset = CreateAsset(dep);
						RefreshAssetAllDependencies2(depAsset);
					}

					asset.allDependencies.Add(depAsset);
				}
			}
		}

		/// <summary>
		/// 更新所有资源的直接依赖
		/// </summary>
		public void RefreshAllAssetDependencies()
		{
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
			BundleInfo bundle = CreateBundleInfo(bundleName);
			m_Bundles.Add(bundle);
			m_BundlesIdMap[bundle.id] = bundle;
			return bundle;
		}
		public BundleInfo CreateBundle(string bundleName, AssetInfo assetNode)
		{
			BundleInfo bundle = CreateBundle(bundleName);
			bundle.SetMainAsset(assetNode);
			bundle.AddAsset(assetNode);
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
				if (bundle.name == bundleName)
				{
					return bundle;
				}
			}
			return null;
		}

		public BundleInfo GetBundle(uint id)
		{
			BundleInfo bundle = null;
			if (m_BundlesIdMap.TryGetValue(id, out bundle))
			{
				return bundle;
			}
			return null;
		}

		public void RemoveBundle(BundleInfo bundle)
		{
			m_Bundles.Remove(bundle);
			m_BundlesIdMap.Remove(bundle.id);
			bundle.enbale = false;
			bundle.Clear();
		}

		public BundleInfo MergeBundle(BundleInfo from, BundleInfo to)
		{
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
			foreach (AssetInfo assetNode in bundle.assets)
			{
				//add dep
				foreach (AssetInfo assetDep in assetNode.dependencies)
				{
					if (assetDep.bundle == null)
					{
						assetDep.bundle = CreateBundle(null, assetDep);
						RefreshBundleDependencies(assetDep.bundle);
					}
					bundle.AddDependencyOnly(assetDep.bundle);
					assetDep.bundle.AddReferOnly(bundle);
				}
			}
		}

		public void RefreshBundleRelations(BundleInfo bundle)
		{
			foreach (AssetInfo assetNode in bundle.assets)
			{
				//add dep
				foreach (AssetInfo assetDep in assetNode.dependencies)
				{
					if (assetDep.bundle == null)
					{
						assetDep.bundle = CreateBundle(null, assetDep);
						RefreshBundleRelations(assetDep.bundle);
					}
					bundle.AddDependencyOnly(assetDep.bundle);
					assetDep.bundle.AddReferOnly(bundle);
				}

				//add refer
				foreach (AssetInfo assetRef in assetNode.refers)
				{
					if (assetRef.bundle == null)
					{
						assetRef.bundle = CreateBundle(null, assetRef);
						RefreshBundleRelations(assetRef.bundle);
					}
					bundle.AddReferOnly(assetRef.bundle);
					assetRef.bundle.AddDependencyOnly(bundle);
				}
			}
		}

		public void RefreshAllBundleDependencies()
		{
			//m_TempBundles.Clear();
			//m_TempBundles.AddRange(m_Bundles);
			List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
			foreach (var bundle in bundles)
			{
				RefreshBundleDependencies(bundle);
			}
		}

		public void RefreshAllBundleRelations()
		{
			//m_TempBundles.Clear();
			//m_TempBundles.AddRange(m_Bundles);
			List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
			foreach (var bundle in bundles)
			{
				RefreshBundleRelations(bundle);
			}
		}

		#endregion //Bundle

		#region Asset Bundle

		public void CreateBundlesFromAssets()
		{
			foreach (var iter in m_Assets)
			{
				BundleInfo bundle = CreateBundle(null);
				bundle.SetMainAsset(iter.Value);
				bundle.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundle.SetStandalone(iter.Value.addressable);
				}
			}
		}

		public void AddAssetToBundle(BundleInfo bundle, AssetInfo asset)
		{
			if (bundle.mainAsset == null)
			{
				bundle.SetMainAsset(asset);
			}

			bundle.AddAsset(asset);
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
					List<BundleInfo> items = null;
					if (!sameRefers.TryGetValue(hash, out items))
					{
						items = new List<BundleInfo>();
						sameRefers[hash] = items;
					}
					items.Add(bundle);
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