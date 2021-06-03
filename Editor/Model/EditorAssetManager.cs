using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
	public class EditorAssetManager
	{
		Dictionary<string, AssetNode> m_Assets=new Dictionary<string, AssetNode>();

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

		public void Clean()
		{
			if (m_Assets != null)
			{
				m_Assets.Clear();
			}
		}

		#region Asset
		public AssetNode GetAsset(string assetPath)
		{
			AssetNode assetNode = null;
			m_Assets.TryGetValue(assetPath, out assetNode);
			return assetNode;
		}

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
			m_Assets[assetNode.fullAssetName] = assetNode;
			return assetNode;
		}

		/// <summary>
		/// 刷新资源的直接依赖
		/// </summary>
		/// <param name="assetNode"></param>
		internal void RefreshAssetDependencies(AssetNode assetNode)
		{
			if (!AssetDatabase.IsValidFolder(assetNode.fullAssetName))
			{
				//dep
				assetNode.dependencies.Clear();
				foreach (var dep in AssetDatabase.GetDependencies(assetNode.fullAssetName, false))
				{
					if (dep != assetNode.fullAssetName)
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
		internal void RefreshAssetAllDependencies(AssetNode assetNode)
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

		internal void RefreshAssetAllDependencies2(AssetNode assetNode)
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

			foreach (var dep in AssetDatabase.GetDependencies(assetNode.fullAssetName, true))
			{
				if (dep != assetNode.fullAssetName)
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

		#endregion
	}
}
