using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using System.IO;

namespace AssetBundleBuilder
{
    public class AssetNode
    {
		public enum AssetType
		{
			None,
			Normal,
			Scene,
			Shader,
			ShaderVariantCollection
		}

		private string m_AssetPath;
        private string m_DisplayName;
		private long m_FileSize = -1;
		private string m_RealFilePath = null;

		private HashSet<AssetNode> m_Refers;
        private HashSet<AssetNode> m_Dependencies = null;
        private HashSet<AssetNode> m_AllDependencies = null;

		protected AssetType m_AssetType = AssetType.None;

		//单独的.是--独立加载，需要主动加载的资源。否--依赖加载，不会主动加载。
		//一般prefab，场景需要手动加载，一些贴图和音乐也需要手动加载。
		//fbx基本是依赖加载，大部分材质也是依赖加载。
		//具体还是需要根据项目来定。
		//一般情况调用LoadFromFolder的资源都是独立的，调用LoadDependencies是依赖的。
		protected bool m_Addressable = false;

		public BundleNode bundle;

		public string assetPath
		{
			get
			{
				return m_AssetPath;
			}
			set
			{
				m_AssetPath = value;
				m_DisplayName = System.IO.Path.GetFileNameWithoutExtension(m_AssetPath);
			}
		}
		public string displayName
		{
			get
			{
				return m_DisplayName;
			}
		}


		public bool isScene
		{
			get
			{
				return m_AssetType == AssetType.Scene;
			}
		}

		public bool isShader
		{
			get
			{
				return m_AssetType == AssetType.Shader;
			}
		}

		public long fileSize
		{
			get
			{
				if (m_FileSize == -1)
				{
					System.IO.FileInfo fileInfo = new System.IO.FileInfo(m_RealFilePath);
					if (fileInfo.Exists)
					{
						m_FileSize = fileInfo.Length;
					}
					else
					{
						m_FileSize = 0;
					}
				}
				return m_FileSize;
			}
		}

		public bool addressable
		{
			get
			{
				return m_Addressable;
			}
			set
			{
				m_Addressable = value;
			}
		}

		public HashSet<AssetNode> refers
		{
			get
			{
				if (m_Refers == null)
				{
					m_Refers = new HashSet<AssetNode>();
				}
				return m_Refers;
			}
			set
			{
				m_Refers = value;
			}
		}

		public HashSet<AssetNode> dependencies
		{
			get
			{
				if (m_Dependencies == null)
				{
					m_Dependencies = new HashSet<AssetNode>();
				}
				return m_Dependencies;
			}
			set
			{
				m_Dependencies = value;
			}
		}

		public HashSet<AssetNode> allDependencies
		{
			get
			{
				return m_AllDependencies;
			}
			set
			{
				m_AllDependencies = value;
			}
		}

		public AssetNode(string assetPath)
		{
			this.assetPath = assetPath;
			m_Refers = new HashSet<AssetNode>();
			m_AssetType = AnalyzeAssetType(assetPath);
		}

		public AssetNode(string assetPath, string assetFilePath)
		{
			this.assetPath = assetPath;
			m_RealFilePath = assetFilePath;
			m_Refers = new HashSet<AssetNode>();
			m_AssetType = AnalyzeAssetType(assetPath);
		}

		public void AddRefer(AssetNode referNode)
		{
			AddReferNode(referNode);
			referNode.AddDependencyNode(this);
		}

		public void RemoveRefer(AssetNode referNode)
		{
			RemoveReferNode(referNode);
			referNode.RemoveDependencyNode(this);
		}

		public void AddDependency(AssetNode depNode)
		{
			AddDependencyNode(depNode);
			depNode.AddReferNode(this);
		}

		public void RemoveDependency(AssetNode depNode)
		{
			RemoveDependencyNode(depNode);
			depNode.RemoveReferNode(this);
		}

		public void AddReferNode(AssetNode assetNode)
		{
			refers.Add(assetNode);
		}

		public void RemoveReferNode(AssetNode assetNode)
		{
			refers.Remove(assetNode);
		}

		public void AddDependencyNode(AssetNode assetNode)
		{
			dependencies.Add(assetNode);
		}

		public void RemoveDependencyNode(AssetNode assetNode)
		{
			dependencies.Remove(assetNode);
		}

		public string GetSizeString()
		{
			if (fileSize == 0)
				return "--";
			return EditorUtility.FormatBytes(fileSize);
		}

		public static AssetType AnalyzeAssetType(string assetPath)
		{
			AssetType assetType = AssetType.None;
			//现根据扩展名判断
			string ext = Path.GetExtension(assetPath);
			switch (ext.ToLower())
			{
				case ".unity":
					assetType = AssetType.Scene;
					break;
				case ".shader":
					assetType = AssetType.Shader;
					break;
				case ".shadervariants":
					assetType = AssetType.ShaderVariantCollection;
					break;
				default:
					assetType = AssetType.Normal;
					break;
			}
			return assetType;
		}

	}
}
