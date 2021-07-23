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
	public class EditorAssetManager
	{
		//assets
		Dictionary<string, AssetNode> m_Assets=new Dictionary<string, AssetNode>();
		
		//bundles
		//Key:asset relative path
		Dictionary<string, BundleNode> m_AssetBundles = new Dictionary<string, BundleNode>();
		//Key:bundle name
		//Dictionary<string, BundleNode> m_Bundles = new Dictionary<string, BundleNode>();
		List<BundleNode> m_Bundles = new List<BundleNode>(4096);

		private List<BundleNode> m_TempBundles = new List<BundleNode>(4096);

		public Dictionary<string, AssetNode> assets
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

		public List<BundleNode> bundles
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
		}

		#region Asset
		public AssetNode CreateAssetNode(string assetPath)
		{
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

			AssetNode assetNode = new AssetNode(assetPath, realPath);
			return assetNode;
		}

		public AssetNode CreateAsset(string assetPath)
		{
			AssetNode assetNode = CreateAssetNode(assetPath);
			m_Assets[assetNode.assetPath] = assetNode;
			return assetNode;
		}

		public AssetNode GetAsset(string assetPath)
		{
			AssetNode assetNode = null;
			m_Assets.TryGetValue(assetPath, out assetNode);
			return assetNode;
		}

		public AssetNode GetOrCreateAsset(string assetPath)
		{
			AssetNode assetNode = null;
			if (!m_Assets.TryGetValue(assetPath, out assetNode))
			{
				assetNode = CreateAsset(assetPath);
			}
			return assetNode;
		}

		/// <summary>
		/// 刷新资源的直接依赖
		/// </summary>
		/// <param name="assetNode"></param>
		public void RefreshAssetDependencies(AssetNode assetNode)
		{
			if (!AssetDatabase.IsValidFolder(assetNode.assetPath))
			{
				//dep
				assetNode.dependencies.Clear();
				foreach (var dep in AssetDatabase.GetDependencies(assetNode.assetPath, false))
				{
					if (dep != assetNode.assetPath)
					{
						AssetNode depAsset = GetAsset(dep);
						if (depAsset == null)
						{
							depAsset = CreateAsset(dep);
							RefreshAssetDependencies(depAsset);
						}

						depAsset.AddRefer(assetNode);
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
		/// <param name="assetNode"></param>
		public void RefreshAssetAllDependencies(AssetNode assetNode)
		{
			//clear all deps
			if (assetNode.allDependencies == null)
			{
				assetNode.allDependencies = new HashSet<AssetNode>();
			}
			else
			{
				assetNode.allDependencies.Clear();
			}

			Stack<AssetNode> assetsStack = new Stack<AssetNode>();
			HashSet<AssetNode> visiteds = new HashSet<AssetNode>();

			assetsStack.Push(assetNode);

			while (assetsStack.Count > 0)
			{
				AssetNode current = assetsStack.Pop();
				if (visiteds.Contains(current))
				{
					continue;
				}

				visiteds.Add(current);

				if (current.dependencies != null && current.dependencies.Count > 0)
				{
					foreach (var dep in current.dependencies)
					{
						assetNode.allDependencies.Add(dep);
						assetsStack.Push(dep);
					}
				}
			}
		}

		public void RefreshAssetAllDependencies2(AssetNode assetNode)
		{
			//dep
			if (assetNode.allDependencies == null)
			{
				assetNode.allDependencies = new HashSet<AssetNode>();
			}
			else
			{
				assetNode.allDependencies.Clear();
			}

			foreach (var dep in AssetDatabase.GetDependencies(assetNode.assetPath, true))
			{
				if (dep != assetNode.assetPath)
				{
					AssetNode depAsset = GetAsset(dep);
					if (depAsset == null)
					{
						depAsset = CreateAsset(dep);
						RefreshAssetAllDependencies2(depAsset);
					}

					assetNode.allDependencies.Add(depAsset);
				}
			}
		}

		/// <summary>
		/// 更新所有资源的直接依赖
		/// </summary>
		public void RefreshAllAssetDependencies()
		{
			List<AssetNode> assets = new List<AssetNode>(m_Assets.Values);
			foreach (var assetNode in assets)
			{
				RefreshAssetDependencies(assetNode);
			}
		}

		/// <summary>
		/// 更新所有资源的所有依赖
		/// </summary>
		public void RefreshAllAssetAllDependencies()
		{
			List<AssetNode> assets = new List<AssetNode>(m_Assets.Values);
			foreach (var assetNode in assets)
			{
				RefreshAssetAllDependencies(assetNode);
			}
		}

		public void RefreshAllAssetAllDependencies2()
		{
			List<AssetNode> assets = new List<AssetNode>(m_Assets.Values);
			foreach (var assetNode in assets)
			{
				RefreshAssetAllDependencies2(assetNode);
			}
		}

		#endregion //Asset

		#region Bundle

		public BundleNode CreateBundleNode(string bundleName)
		{
			BundleNode bundleNode = new BundleNode(bundleName);
			return bundleNode;
		}

		public BundleNode CreateBundle(string bundleName)
		{
			BundleNode bundle = CreateBundleNode(bundleName);
			m_Bundles.Add(bundle);
			return bundle;
		}
		public BundleNode CreateBundle(string bundleName, AssetNode assetNode)
		{
			BundleNode bundle = CreateBundle(bundleName);
			bundle.SetMainAsset(assetNode);
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

		public BundleNode GetBundle(string bundleName)
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

		public void RefreshBundleDependencies(BundleNode bundleNode)
		{
			foreach (AssetNode assetNode in bundleNode.assetNodes)
			{
				//add dep
				foreach (AssetNode assetDep in assetNode.dependencies)
				{
					if (assetDep.bundle == null)
					{
						assetDep.bundle = CreateBundle(null, assetDep);
						RefreshBundleDependencies(assetDep.bundle);
					}
					bundleNode.AddDependency(assetDep.bundle);
					assetDep.bundle.AddRefer(bundleNode);
				}
			}
		}

		public void RefreshBundleRelations(BundleNode bundleNode)
		{
			foreach (AssetNode assetNode in bundleNode.assetNodes)
			{
				//add dep
				foreach (AssetNode assetDep in assetNode.dependencies)
				{
					if (assetDep.bundle == null)
					{
						assetDep.bundle = CreateBundle(null, assetDep);
						RefreshBundleRelations(assetDep.bundle);
					}
					bundleNode.AddDependency(assetDep.bundle);
					assetDep.bundle.AddRefer(bundleNode);
				}

				//add refer
				foreach (AssetNode assetRef in assetNode.refers)
				{
					if (assetRef.bundle == null)
					{
						assetRef.bundle = CreateBundle(null, assetRef);
						RefreshBundleRelations(assetRef.bundle);
					}
					bundleNode.AddRefer(assetRef.bundle);
					assetRef.bundle.AddDependency(bundleNode);
				}
			}
		}

		public void RefreshAllBundleDependencies()
		{
			m_TempBundles.Clear();
			m_TempBundles.AddRange(m_Bundles);
			foreach (var bundleNode in m_TempBundles)
			{
				RefreshBundleDependencies(bundleNode);
			}
		}

		public void RefreshAllBundleRelations()
		{
			m_TempBundles.Clear();
			m_TempBundles.AddRange(m_Bundles);
			foreach (var bundleNode in m_TempBundles)
			{
				RefreshBundleRelations(bundleNode);
			}
		}

		public void ReplaceBundle(BundleNode from, BundleNode to)
		{
			foreach (var assetName in from.assets)
			{
				m_AssetBundles[assetName] = to;
			}
		}

		public BundleNode MergeBundle(BundleNode from, BundleNode to)
		{
			//合并资源
			foreach (var asset in from.assets)
			{
				to.assets.Add(asset);
			}

			//合并引用
			foreach (var refer in from.refers)
			{
				if (refer != to)
				{
					to.AddRefer(refer);
					refer.RemoveDependency(from);
					refer.AddDependency(to);
				}
			}

			//合并依赖
			foreach (var dep in from.dependencies)
			{
				if (dep != to)
				{
					to.AddDependency(dep);
					dep.RemoveRefer(from);
					dep.AddRefer(to);
				}
			}

			//如果from在to的refers或dependencies中(循环引用)，则移除。
			if (to.refers.Contains(from))
			{
				to.RemoveRefer(from);
			}

			if (to.dependencies.Contains(from))
			{
				to.RemoveDependency(from);
			}

			//Repalce from assets.
			ReplaceBundle(from, to);

			return to;
		}


		#endregion //Bundle

		#region Asset Bundle

		public void CreateBundlesFromAssets()
		{
			foreach (var iter in m_Assets)
			{
				BundleNode bundleNode = CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}
		}

		public void AddAssetToBundle(BundleNode bundleNode, AssetNode assetNode)
		{
			if (bundleNode.mainAssetNode == null)
			{
				bundleNode.SetMainAsset(assetNode);
			}
			else
			{
				bundleNode.AddAsset(assetNode);
			}
		}

		public void AddAssetBundle(BundleNode node)
		{
			//m_Assets.Add(node);
			foreach (var assetName in node.assets)
			{
				m_AssetBundles[assetName] = node;
			}
		}

		public void RemoveAssetBundle(BundleNode bundle)
		{
			foreach (var assetName in bundle.assets)
			{
				if (m_AssetBundles.ContainsKey(assetName))
				{
					m_AssetBundles.Remove(assetName);
				}
			}
		}

		public BundleNode GetAssetBundle(string assetPath)
		{
			BundleNode bundle = null;
			m_AssetBundles.TryGetValue(assetPath, out bundle);
			return bundle;
		}

		public bool IsAssetHaveBundle(string assetPath)
		{
			return m_AssetBundles.ContainsKey(assetPath);
		}

		#endregion //Asset Bundle

	}
}
