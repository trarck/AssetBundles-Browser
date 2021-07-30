using System;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleBuilder
{
	public class BundleInfo
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
		protected AssetInfo m_MainAsset;
		//包含的资源
		protected HashSet<AssetInfo> m_Assets;

		//直接引用者
		public HashSet<BundleInfo> refers;
		//直接依赖
		public HashSet<BundleInfo> dependencies;
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

		public AssetInfo mainAsset
		{
			get
			{
				if (m_MainAsset == null && m_Assets != null && m_Assets.Count > 0)
				{
					foreach (var iter in m_Assets)
					{
						return iter;
					}
				}
				return m_MainAsset;
			}
			set
			{
				m_MainAsset = value;
			}
		}

		public HashSet<AssetInfo> assets => m_Assets;

		public string mainAssetPath
		{
			get
			{
				if (mainAsset != null)
				{
					return mainAsset.assetPath;
				}
				return "";
			}
		}

		public BundleInfo()
		{
			m_Assets = new HashSet<AssetInfo>();
			refers = new HashSet<BundleInfo>();
			dependencies = new HashSet<BundleInfo>();
			m_Enable = true;
		}

		public BundleInfo(string name) : this()
		{
			m_Name = name;
		}
		public void SetMainAsset(AssetInfo assetNode)
		{
			if (assetNode == null)
			{
				return;
			}
			m_MainAsset = assetNode;
			bundleType = AnalyzeAssetType(assetNode.assetPath);
		}

		public void AddAsset(AssetInfo assetNode)
		{
			m_Assets.Add(assetNode);
			assetNode.bundle = this;
		}

		public void AddAssets(IEnumerable<AssetInfo> assetNodes)
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

		public void AddDependency(BundleInfo dep)
		{
			if (dep != this)
			{
				dependencies.Add(dep);
				//dep.refers.Add(this);
			}
		}

		public void RemoveDependency(BundleInfo dep)
		{
			if (dep != this)
			{
				dependencies.Remove(dep);
				//dep.refers.Remove(this);
			}
		}

		public void AddRefer(BundleInfo refer)
		{
			if (refer != this)
			{
				if (refers.Add(refer))
				{
					ClearRefersHashCode();
				}
			}
		}

		public void RemoveRefer(BundleInfo refer)
		{
			if (refer != this)
			{
				if (refers.Remove(refer))
				{
					ClearRefersHashCode();
				}
			}
		}

		public void Link(BundleInfo dep)
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

		public void Break(BundleInfo dep)
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

		public void ClearAssets()
		{
			foreach (var asset in m_Assets)
			{
				if (asset.bundle == this)
				{
					asset.bundle = null;
				}
			}
			m_Assets.Clear();
		}

		public void ClearRelations()
		{
			foreach (var refer in refers)
			{
				refer.RemoveDependency(this);
			}
			refers.Clear();

			foreach (var dep in dependencies)
			{
				dep.RemoveRefer(this);
			}
			dependencies.Clear();
		}

		public void Clear()
		{
			m_Name = null;
			m_MainAsset = null;
			m_Standalone = false;
			m_RefersHashCode = 0;
			m_BundleType = BundleType.None;

			ClearAssets();
			ClearRelations();
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
