using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using System.IO;

namespace AssetBundleBuilder
{
    public class AssetInfo
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
		private ulong m_HashCode = 0;

		private HashSet<AssetInfo> m_Refers;
        private HashSet<AssetInfo> m_Dependencies = null;
        private HashSet<AssetInfo> m_AllDependencies = null;

		protected AssetType m_AssetType = AssetType.None;

		//单独的.是--独立加载，需要主动加载的资源。否--依赖加载，不会主动加载。
		//一般prefab，场景需要手动加载，一些贴图和音乐也需要手动加载。
		//fbx基本是依赖加载，大部分材质也是依赖加载。
		//具体还是需要根据项目来定。
		//一般情况调用LoadFromFolder的资源都是独立的，调用LoadDependencies是依赖的。
		protected bool m_Addressable = false;

		public BundleInfo bundle;

		public string assetPath
		{
			get
			{
				return m_AssetPath;
			}
			set
			{
				m_AssetPath = value;
				m_DisplayName = Path.GetFileNameWithoutExtension(m_AssetPath);
				//m_HashCode = YH.Hash.xxHash.xxHash64.ComputeHash(m_AssetPath);
			}
		}
		public string displayName
		{
			get
			{
				return m_DisplayName;
			}
		}

		public ulong hashCode
		{
			get
			{
				return m_HashCode;
			}
			set
			{
				m_HashCode = value;
			}
		}

		public AssetType assetType
		{
			get
			{
				return m_AssetType;
			}
			set
			{
				m_AssetType = value;
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
					if (!string.IsNullOrEmpty(m_RealFilePath))
					{
						FileInfo fileInfo = new FileInfo(m_RealFilePath);
						if (fileInfo.Exists)
						{
							m_FileSize = fileInfo.Length;
						}
						else
						{
							m_FileSize = 0;
						}
					}
				}
				return m_FileSize;
			}
			set
			{
				m_FileSize = value;
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

		public HashSet<AssetInfo> refers
		{
			get
			{
				if (m_Refers == null)
				{
					m_Refers = new HashSet<AssetInfo>();
				}
				return m_Refers;
			}
			set
			{
				m_Refers = value;
			}
		}

		public HashSet<AssetInfo> dependencies
		{
			get
			{
				if (m_Dependencies == null)
				{
					m_Dependencies = new HashSet<AssetInfo>();
				}
				return m_Dependencies;
			}
			set
			{
				m_Dependencies = value;
			}
		}

		public HashSet<AssetInfo> allDependencies
		{
			get
			{
				if (m_AllDependencies == null)
				{
					m_AllDependencies = new HashSet<AssetInfo>();
				}
				return m_AllDependencies;
			}
			set
			{
				m_AllDependencies = value;
			}
		}

		public AssetInfo(string assetPath):this(assetPath,null)
		{
		}

		public AssetInfo(string assetPath, string assetFilePath)
		{
			this.assetPath = assetPath;
			m_RealFilePath = assetFilePath;
			m_Refers = new HashSet<AssetInfo>();
			m_Dependencies = new HashSet<AssetInfo>();
			m_AssetType = AnalyzeAssetType(assetPath);
		}

		public void AddRefer(AssetInfo refer)
		{
			AddReferOnly(refer);
			refer.AddDependencyOnlfy(this);
		}

		public void RemoveRefer(AssetInfo refer)
		{
			RemoveReferOnly(refer);
			refer.RemoveDependencyOnly(this);
		}

		public void AddDependency(AssetInfo dep)
		{
			AddDependencyOnlfy(dep);
			dep.AddReferOnly(this);
		}

		public void RemoveDependency(AssetInfo dep)
		{
			RemoveDependencyOnly(dep);
			dep.RemoveReferOnly(this);
		}

		public void AddReferOnly(AssetInfo asset)
		{
			refers.Add(asset);
		}

		public void RemoveReferOnly(AssetInfo asset)
		{
			refers.Remove(asset);
		}

		public void AddDependencyOnlfy(AssetInfo asset)
		{
			dependencies.Add(asset);
		}

		public void RemoveDependencyOnly(AssetInfo asset)
		{
			dependencies.Remove(asset);
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
