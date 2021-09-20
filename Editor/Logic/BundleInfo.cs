using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetBundleBuilder
{
	public class BundleNameData
	{
		private List<string> m_PathTokens;
		private string m_FullBundleName;
		private string m_ShortName;
		private string m_VariantName;
		private string m_FullNativeName;

		//input (received from native) is a string of format:
		//  /folder0/.../folderN/name.variant
		//it's broken into:
		//  /m_pathTokens[0]/.../m_pathTokens[n]/m_shortName.m_variantName
		// and...
		//  m_fullBundleName = /m_pathTokens[0]/.../m_pathTokens[n]/m_shortName
		// and...
		//  m_fullNativeName = m_fullBundleName.m_variantName which is the same as the initial input.
		public BundleNameData(string name)
		{
			SetName(name);
		}
		public BundleNameData(string path, string name)
		{
			string finalName = System.String.IsNullOrEmpty(path) ? "" : path + '/';
			finalName += name;
			SetName(finalName);
		}
		public override int GetHashCode()
		{
			return fullNativeName.GetHashCode();
		}
		public string fullNativeName
		{
			get
			{
				return m_FullNativeName;
			}
		}

		public void SetBundleName(string bundleName, string variantName)
		{
			string name = bundleName;
			name += System.String.IsNullOrEmpty(variantName) ? "" : "." + variantName;
			SetName(name);
		}
		public string bundleName
		{
			get
			{
				return m_FullBundleName;
			}
			//set { SetName(value); }
		}
		public string shortName
		{
			get
			{
				return m_ShortName;
			}
		}
		public string variant
		{
			get
			{
				return m_VariantName;
			}
			set
			{
				m_VariantName = value;
				m_FullNativeName = m_FullBundleName;
				m_FullNativeName += System.String.IsNullOrEmpty(m_VariantName) ? "" : "." + m_VariantName;
			}
		}
		public List<string> pathTokens
		{
			get
			{
				return m_PathTokens;
			}
			set
			{
				m_PathTokens = value.GetRange(0, value.Count - 1);
				SetShortName(value.Last());
				GenerateFullName();
			}
		}

		private void SetName(string name)
		{
			if (m_PathTokens == null)
				m_PathTokens = new List<string>();
			else
				m_PathTokens.Clear();

			string shortName = GetPathNames(name, ref m_PathTokens);
			SetShortName(shortName);
			GenerateFullName();
		}

		private void SetShortName(string inputName)
		{
			m_ShortName = inputName;
			int indexOfDot = m_ShortName.LastIndexOf('.');
			if (indexOfDot > -1)
			{
				m_VariantName = m_ShortName.Substring(indexOfDot + 1);
				m_ShortName = m_ShortName.Substring(0, indexOfDot);
			}
			else
				m_VariantName = string.Empty;
		}

		public void PartialNameChange(string newToken, int indexFromBack)
		{
			if (indexFromBack == 0)
			{
				List<string> paths = new List<string>();
				string shortName = GetPathNames(newToken, ref paths);
				m_PathTokens.AddRange(paths);
				SetShortName(shortName);
			}
			else if (indexFromBack == -1)
			{
				if (m_PathTokens.Count == 0)
				{
					m_PathTokens.AddRange(newToken.Split('/'));
				}
				else
				{
					m_PathTokens.InsertRange(0, newToken.Split('/'));
				}
			}
			else if (indexFromBack <= m_PathTokens.Count)
			{
				int index = m_PathTokens.Count - indexFromBack;
				m_PathTokens.RemoveAt(index);
				m_PathTokens.InsertRange(index, newToken.Split('/'));
			}
			
			GenerateFullName();
		}

		public void ShortNameChange(string newShortName)
		{
			if (m_ShortName != newShortName)
			{
				SetShortName(newShortName);
				GenerateFullName();
			}
		}

		private void GenerateFullName()
		{
			m_FullBundleName = string.Empty;
			for (int i = 0; i < m_PathTokens.Count; i++)
			{
				m_FullBundleName += m_PathTokens[i];
				m_FullBundleName += '/';
			}
			m_FullBundleName += m_ShortName;
			m_FullNativeName = m_FullBundleName;
			m_FullNativeName += System.String.IsNullOrEmpty(m_VariantName) ? "" : "." + m_VariantName;
		}

		static public string GetPathNames(string name, ref List<string> pathTokens)
		{
			if (name == null)
			{
				return "";
			}

			int indexOfSlash = name.IndexOf('/');
			int previousIndex = 0;
			while (indexOfSlash != -1)
			{
				pathTokens.Add(name.Substring(previousIndex, (indexOfSlash - previousIndex)));
				previousIndex = indexOfSlash + 1;
				indexOfSlash = name.IndexOf('/', previousIndex);
			}
			return previousIndex == 0 ? name : name.Substring(previousIndex);
		}

		static public List<string> GetPathTokens(string path)
		{
			List<string> pathTokens = new List<string>();

			int indexOfSlash = path.IndexOf('/');
			int previousIndex = 0;
			while (indexOfSlash != -1)
			{
				pathTokens.Add(path.Substring(previousIndex, (indexOfSlash - previousIndex)));
				previousIndex = indexOfSlash + 1;
				indexOfSlash = path.IndexOf('/', previousIndex);
			}
			string left = path.Substring(previousIndex).Trim();
			if (!string.IsNullOrEmpty(left))
			{
				pathTokens.Add(left);
			}
			return pathTokens;
		}
	}


	public class BundleInfo
	{
		private static uint IdIndex = 0;

		public enum BundleType
		{
			None,
			Normal,
			Scene,
			Shader,
			ShaderVariantCollection
		}

		protected uint m_Id;

		//bundle name
		protected string m_Name;

		//bundle variant name
		protected string m_VariantName;

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

		public uint id
		{
			get
			{
				return m_Id;
			}
			set
			{
				m_Id = value;
			}
		}

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

		public string displayName
		{
			get
			{
				return Path.GetFileName(m_Name);
			}
		}

		public string variantName
		{
			get
			{
				return m_VariantName;
			}
			set
			{
				m_VariantName = value;
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

		public BundleInfo():this(++IdIndex,null, null)
		{
		}

		public BundleInfo(string name) : this(++IdIndex,name, null)
		{
			
		}
		public BundleInfo(string name, string variantName) : this(++IdIndex, name, variantName)
		{

		}

		public BundleInfo(uint id,string name) : this(id, name,null)
		{
		}

		public BundleInfo(uint id, string name, string variantName)
		{
			m_Id = id;
			m_Name = name;
			m_VariantName = variantName;

			m_Assets = new HashSet<AssetInfo>();
			refers = new HashSet<BundleInfo>();
			dependencies = new HashSet<BundleInfo>();
			m_Enable = true;
		}


		public void SetMainAsset(AssetInfo asset)
		{
			if (asset == null)
			{
				return;
			}
			m_MainAsset = asset;
			bundleType = AnalyzeAssetType(asset.assetPath);
		}

		public void AddAsset(AssetInfo asset)
		{
			m_Assets.Add(asset);
			asset.bundle = this;
		}

		public void AddAssets(IEnumerable<AssetInfo> assetInfos)
		{
			if (assetInfos == null)
			{
				return;
			}
			foreach (var asset in assetInfos)
			{
				AddAsset(asset);
			}
		}

		public void AddDependencyOnly(BundleInfo dep)
		{
			if (dep != this)
			{
				dependencies.Add(dep);
				//dep.refers.Add(this);
			}
		}

		public void RemoveDependencyOnly(BundleInfo dep)
		{
			if (dep != this)
			{
				dependencies.Remove(dep);
				//dep.refers.Remove(this);
			}
		}

		public void AddReferOnly(BundleInfo refer)
		{
			if (refer != this)
			{
				if (refers.Add(refer))
				{
					ClearRefersHashCode();
				}
			}
		}

		public void RemoveReferOnly(BundleInfo refer)
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

		public bool TryGetAssetsPaths(ref List<string> assetPaths)
		{
			foreach (var asset in assets)
			{
				assetPaths.Add(asset.assetPath);
			}
			return true;
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
				refer.RemoveDependencyOnly(this);
			}
			refers.Clear();

			foreach (var dep in dependencies)
			{
				dep.RemoveReferOnly(this);
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

		public long GetTotalSize()
		{
			long size = 0;
			//assets size
			foreach (var asset in assets)
			{
				size += asset.fileSize;

				//get deps not in any bundle
				foreach (var dep in asset.allDependencies)
				{
					if (dep.bundle == null)
					{
						size += dep.fileSize;
					}
				}
			}
			return size;
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
