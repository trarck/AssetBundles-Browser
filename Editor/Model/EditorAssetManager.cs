﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder.Editor
{
	public class EditorAssetManager
	{
		Dictionary<string, AssetNode> m_Assets;
		#region Asset
		public AssetNode GetAsset(string assetPath)
		{
			AssetNode assetNode = null;
			m_Assets.TryGetValue(assetPath, out assetNode);
			return assetNode;
		}

		public AssetNode CreateAsset(string assetPath)
		{
			string realPath = assetPath;
			if (!Path.IsPathRooted(assetPath))
			{
				realPath = Path.Combine(EditorApplication.applicationPath, assetPath);
			}
			AssetNode assetNode = new AssetNode(assetPath);
			m_Assets[assetPath] = assetNode;
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
			assetNode.allDependencies.Clear();

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
			assetNode.allDependencies.Clear();
			foreach (var dep in AssetDatabase.GetDependencies(assetNode.fullAssetName, true))
			{
				if (dep != assetNode.fullAssetName)
				{
					AssetNode depAsset = GetAsset(dep);
					if (depAsset == null)
					{
						depAsset = CreateAsset(dep);
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

		#endregion
	}
}
