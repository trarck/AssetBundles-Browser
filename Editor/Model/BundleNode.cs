using System;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleBuilder
{
	public class BundleNode
	{
		public enum BundleType
		{
			None,
			Normal,
			Scene,
			Shader,
			ShaderVariantCollection
		}
		//bundle name
		protected string m_Name;
		////主路径
		//public string mainAsset;
		////资源
		//public HashSet<string> assets;

		//主资源
		protected AssetNode m_MainAssetNode;
		//包含的资源
		protected HashSet<AssetNode> m_AssetNodes;

		//直接引用者
		public HashSet<BundleNode> refers;
		//直接依赖
		public HashSet<BundleNode> dependencies;
		//单独的.是--独立加载，需要主动加载的资源。否--依赖加载，不会主动加载。
		//一般prefab，场景需要手动加载，一些贴图和音乐也需要手动加载。
		//fbx基本是依赖加载，大部分材质也是依赖加载。
		//具体还是需要根据项目来定。
		//一般情况调用LoadFromFolder的资源都是独立的，调用LoadDependencies是依赖的。
		protected bool m_Standalone = false;

		protected int m_RefersHashCode = 0;

		protected BundleType m_BundleType;

		protected bool m_Enable = false;

		public string name
		{
			get
			{
				return m_Name;
			}
			set
			{
				m_Name = value;
			}
		}

		public bool canMerge
		{
			get
			{
				return !m_Standalone && !isScene;
			}
		}

		public bool isScene
		{
			get
			{
				return m_BundleType == BundleType.Scene; //mainAsset.Contains(".unity");
			}
		}

		public bool isShader
		{
			get
			{
				return m_BundleType == BundleType.Shader;
			}
		}

		public bool isShaderVariantCollection
		{
			get
			{
				return m_BundleType == BundleType.ShaderVariantCollection;
			}
		}

		public BundleType bundleType
		{
			get
			{
				return m_BundleType;
			}
			set
			{
				m_BundleType = value;
			}
		}

		public bool enbale
		{
			get
			{
				return m_Enable;
			}
			set
			{
				m_Enable = value;
			}
		}
		//TODO::need change to hash64。资源多时，会产生冲突。
		public int refersHashCode
		{
			get
			{
				if (m_RefersHashCode == 0)
				{
					System.Text.StringBuilder sb = new System.Text.StringBuilder();
					foreach (var refer in refers)
					{
						sb.Append(refer.mainAssetPath).Append("-");
					}

					m_RefersHashCode = sb.ToString().GetHashCode();
				}

				return m_RefersHashCode;
			}
			set
			{
				m_RefersHashCode = value;
			}
		}

		public AssetNode mainAssetNode
		{
			get
			{
				if (m_MainAssetNode == null && m_AssetNodes != null && m_AssetNodes.Count > 0)
				{
					foreach (var iter in m_AssetNodes)
					{
						return iter;
					}
				}
				return m_MainAssetNode;
			}
			set
			{
				m_MainAssetNode = value;
			}
		}

		public HashSet<AssetNode> assetNodes => m_AssetNodes;

		public string mainAssetPath
		{
			get
			{
				if (mainAssetNode != null)
				{
					return mainAssetNode.assetPath;
				}
				return "";
			}
		}

		public BundleNode()
		{
			assets = new HashSet<string>();
			m_AssetNodes = new HashSet<AssetNode>();
			refers = new HashSet<BundleNode>();
			dependencies = new HashSet<BundleNode>();
			m_Enable = true;
		}

		public BundleNode(string name) : this()
		{
			m_Name = name;
		}

		public BundleNode(string name, string asset) : this(name)
		{
			SetMainAsset(asset);
		}

		public void Clear()
		{
			m_Name = null;
			m_MainAssetNode = null;
			m_Standalone = false;
			m_RefersHashCode = 0;
			m_BundleType = BundleType.None;

			m_AssetNodes.Clear();
			refers.Clear();
			dependencies.Clear();
		}

		public void SetMainAsset(AssetNode assetNode)
		{
			if (assetNode == null)
			{
				return;
			}
			m_MainAssetNode = assetNode;
			bundleType = AnalyzeAssetType(assetNode.assetPath);
		}

		public void AddAsset(AssetNode assetNode)
		{
			m_AssetNodes.Add(assetNode);
			assetNode.bundle = this;
		}

		public void AddAssets(IEnumerable<AssetNode> assetNodes)
		{
			if (assetNodes == null)
			{
				return;
			}
			foreach (var assetNode in assetNodes)
			{
				AddAsset(assetNode);
			}
		}

		public void AddDependency(BundleNode dep)
		{
			if (dep != this)
			{
				dependencies.Add(dep);
				//dep.refers.Add(this);
			}
		}

		public void RemoveDependency(BundleNode dep)
		{
			if (dep != this)
			{
				dependencies.Remove(dep);
				//dep.refers.Remove(this);
			}
		}

		public void AddRefer(BundleNode refer)
		{
			if (refer != this)
			{
				if (refers.Add(refer))
				{
					ClearRefersHashCode();
				}
			}
		}

		public void RemoveRefer(BundleNode refer)
		{
			if (refer != this)
			{
				if (refers.Remove(refer))
				{
					ClearRefersHashCode();
				}
			}
		}

		public void Link(BundleNode dep)
		{
			if (dep != this)
			{
				dependencies.Add(dep);
				if (dep.refers.Add(this))
				{
					dep.ClearRefersHashCode();
				}
			}
		}

		public void Break(BundleNode dep)
		{
			if (dep != this)
			{
				dependencies.Remove(dep);
				if (dep.refers.Remove(this))
				{
					dep.ClearRefersHashCode();
				}
			}
		}

		//如果已经设置为ture，则不能再改false。
		//由于默认为false，通常只需要设置为true的时候调用。

		public void SetStandalone(bool val)
		{
			if (!m_Standalone)
			{
				m_Standalone = val;
			}
		}

		public bool IsStandalone()
		{
			return m_Standalone;
		}

		public void ClearRefersHashCode()
		{
			m_RefersHashCode = 0;
		}

		public static BundleType AnalyzeAssetType(string assetPath)
		{
			BundleType assetType = BundleType.Normal;

			//现根据扩展名判断
			string ext = Path.GetExtension(assetPath);
			switch (ext.ToLower())
			{
				case ".unity":
					assetType = BundleType.Scene;
					break;
				case ".shader":
					assetType = BundleType.Shader;
					break;
				case ".shadervariants":
					assetType = BundleType.ShaderVariantCollection;
					break;
				default:
					assetType = BundleType.Normal;
					break;
			}
			return assetType;
		}
	}
}
