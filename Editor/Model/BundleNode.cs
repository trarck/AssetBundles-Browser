using System;
using System.Collections.Generic;
using System.IO;

namespace AssetBundleBuilder
{
    public class BundleNode
    {
        public enum AssetType
        {
            Normal,
            Scene,
            Shader,
            ShaderVariantCollection
        }
        //bundle name
        public string name;
        //主路径
        public string mainAsset;
        //资源
        public HashSet<string> assets;
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

        protected AssetType m_AssetType;
        
        public BundleNode()
        {
            assets = new HashSet<string>();
            refers = new HashSet<BundleNode>();
            dependencies = new HashSet<BundleNode>();
        }

        public BundleNode(string asset):this()
        {
            mainAsset = asset;
            assets.Add(asset);
            assetType=AnalyzeAssetType(asset);
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

        public bool CanMerge
        {
            get
            {
                return !m_Standalone && !IsScene;
            }
        }

        public bool IsScene
        {
            get
            {
                return m_AssetType == AssetType.Scene; //mainAsset.Contains(".unity");
            }
        }

        public bool IsShader
        {
            get
            {
                return m_AssetType == AssetType.Shader;
            }
        }

        public bool IsShaderVariantCollection
        {
            get
            {
                return m_AssetType == AssetType.ShaderVariantCollection;
            }
        }

        public void ClearRefersHashCode()
        {
            m_RefersHashCode = 0;
        }

        public int refersHashCode
        {
            get
            {
                if (m_RefersHashCode == 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (var refer in refers)
                    {
                        sb.Append(refer.mainAsset).Append("-");
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
        
        public static AssetType AnalyzeAssetType(string assetPath)
        {
            AssetType assetType = AssetType.Normal;

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
    }
}
