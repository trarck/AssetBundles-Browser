using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder.Model
{
    public class Optimizer
    {
        public class Node
        {
            //主路径
            public string mainAsset;
            //资源
            public HashSet<string> assets;
            //直接引用者
            public HashSet<Node> refers;
            //直接依赖
            public HashSet<Node> dependencies;
            //单独的.是--独立加载，需要主动加载的资源。否--依赖加载，不会主动加载。
            //一般prefab，场景需要手动加载，一些贴图和音乐也需要手动加载。
            //fbx基本是依赖加载，大部分材质也是依赖加载。
            //具体还是需要根据项目来定。
            //一般情况调用LoadFromFolder的资源都是独立的，调用LoadDependencies是依赖的。
            protected bool m_Standalone = false;

            int m_RefersHashCode = 0;

            public Node(string asset)
            {
                assets = new HashSet<string>();
                refers = new HashSet<Node>();
                dependencies = new HashSet<Node>();
                mainAsset = asset;
                assets.Add(asset);
            }

            public void AddDependency(Node dep)
            {
                if (dep != this)
                {
                    dependencies.Add(dep);
                    //dep.refers.Add(this);
                }
            }

            public void RemoveDependency(Node dep)
            {
                if (dep != this)
                {
                    dependencies.Remove(dep);
                    //dep.refers.Remove(this);
                }
            }

            public void AddRefer(Node refer)
            {
                if (refer != this)
                {
                    if (refers.Add(refer))
                    {
                        ClearRefersHashCode();
                    }
                }
            }

            public void RemoveRefer(Node refer)
            {
                if (refer != this)
                {
                    if (refers.Remove(refer))
                    {
                        ClearRefersHashCode();
                    }
                }
            }

            public void Link(Node dep)
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

            public void Break(Node dep)
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

            public bool canMerge
            {
                get
                {
                    return !m_Standalone;
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
        }

        //List<Node> m_Assets=new List<Node>();
        //这里的node允许重复。有些资源可能被合并到一起。
        Dictionary<string, Node> m_AssetsMap = new Dictionary<string, Node>();

        #region Node

        public void AddNode(Node node)
        {
            //m_Assets.Add(node);
            foreach (var assetName in node.assets)
            {
                m_AssetsMap[assetName] = node;
            }
        }

        public Node GetNode(string assetName)
        {
            Node node = null;
            m_AssetsMap.TryGetValue(assetName, out node);
            return node;
        }

        public void RemoveNode(Node node)
        {
            foreach (var assetName in node.assets)
            {
                if (m_AssetsMap.ContainsKey(assetName))
                {
                    m_AssetsMap.Remove(node.mainAsset);
                }
            }

        }

        public void ReplaceNode(Node from, Node to)
        {
            foreach (var assetName in from.assets)
            {
                m_AssetsMap[assetName] = to;
            }
        }

        public bool Exists(string assetName)
        {
            return m_AssetsMap.ContainsKey(assetName);
        }

        public Node MergeNode(Node to, Node from)
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
            ReplaceNode(from, to);

            return to;
        }

        //m_AssetsMap里可能被合并之后会有重复的node，这里去除重复
        public HashSet<Node> GetAssets()
        {
            HashSet<Node> assets = new HashSet<Node>();
            foreach (var iter in m_AssetsMap)
            {
                assets.Add(iter.Value);
            }
            return assets;
        }

        //通过Asset列表构建AssetsMap，以便支持增量导入。
        public void SetAssets(HashSet<Node> assets)
        {
            if (m_AssetsMap == null)
            {
                m_AssetsMap = new Dictionary<string, Node>();
            }
            else
            {
                m_AssetsMap.Clear();
            }

            foreach (var node in assets)
            {
                foreach (var assetName in node.assets)
                {
                    m_AssetsMap[assetName] = node;
                }
            }
        }
        #endregion

        #region Load

        /// <summary>
        /// 加载依赖项
        /// </summary>
        /// <param name="node"></param>
        public void LoadDependencies(Node node)
        {
            List<string> deps = new List<string>();
            foreach (var asset in node.assets)
            {
                string[] ds = AssetDatabase.GetDependencies(asset, false);
                deps.AddRange(ds);
            }

            foreach (var dep in deps)
            {
                Node depNode = LoadAsset(dep, true);
                if (depNode != null)
                {
                    node.AddDependency(depNode);
                    depNode.AddRefer(node);
                }
            }
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="loadDependency"></param>
        /// <returns></returns>
        public Node LoadAsset(string assetPath, bool loadDependency = true)
        {
            if (IgnoreAsset(assetPath))
            {
                return null;
            }

            if (Path.IsPathRooted(assetPath))
            {
                assetPath = ModelUtils.Relative(Path.GetDirectoryName(Application.dataPath), assetPath);
            }

            Node node = GetNode(assetPath);
            if (node == null)
            {
                node = new Node(assetPath);
                //一定要先添加，后加载Dependencies。否则就会因为循环引用而无限循环。
                AddNode(node);
                if (loadDependency)
                {
                    LoadDependencies(node);
                }
            }
            return node;
        }

        /// <summary>
        /// 从目录加载资源
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="pattern"></param>
        public void LoadFromFolder(string folderPath, string pattern = null)
        {
            DirectoryInfo startInfo = new DirectoryInfo(folderPath);
            if (!startInfo.Exists)
            {
                return;
            }

            Stack<DirectoryInfo> dirs = new Stack<DirectoryInfo>();
            dirs.Push(startInfo);

            DirectoryInfo dir;

            bool haveFilter = false;
            System.Text.RegularExpressions.Regex reg = null;
            if (!string.IsNullOrEmpty(pattern))
            {
                haveFilter = true;
                reg = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            while (dirs.Count > 0)
            {
                dir = dirs.Pop();

                foreach (FileInfo fi in dir.GetFiles())
                {
                    if (!haveFilter || reg.IsMatch(fi.FullName))
                    {
                        Node node = LoadAsset(fi.FullName);
                        if (node != null)
                        {
                            node.SetStandalone(true);
                        }
                    }
                }

                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    if (!subDir.Name.StartsWith("."))
                    {
                        dirs.Push(subDir);
                    }
                }
            }
        }

        protected bool IgnoreAsset(string assetPath)
        {
            string ext = Path.GetExtension(assetPath).ToLower();
            return ext == ".meta" || ext == ".cs" || ext == ".js" || ext == ".dll";
        }
        #endregion

        #region Combine
        /// <summary>
        /// 合并只有一个引用的项
        /// </summary>
        /// <returns></returns>
        protected bool MergeOneReferAssets()
        {
            bool merged = false;
            HashSet<Node> assets = GetAssets();

            foreach (var node in assets)
            {
                if (node.refers.Count == 1 && node.canMerge)
                {
                    merged = true;
                    var iter = node.refers.GetEnumerator();
                    iter.MoveNext();
                    MergeNode(iter.Current, node);
                }
            }
            return merged;
        }

        /// <summary>
        /// 合并相同引用的项
        /// </summary>
        /// <returns></returns>
        protected bool MergeSameReferAssets()
        {
            bool merged = false;
            HashSet<Node> assets = GetAssets();
            Dictionary<int, List<Node>> sameRefers = new Dictionary<int, List<Node>>();

            foreach (var node in assets)
            {
                if (node.canMerge)
                {
                    int hash = node.refersHashCode;
                    List<Node> items = null;
                    if (!sameRefers.TryGetValue(hash, out items))
                    {
                        items = new List<Node>();
                        sameRefers[hash] = items;
                    }
                    items.Add(node);
                }
            }

            foreach (var iter in sameRefers)
            {
                if (iter.Value.Count > 1)
                {
                    for (int i = 1; i < iter.Value.Count; ++i)
                    {
                        MergeNode(iter.Value[0], iter.Value[i]);
                    }
                }
            }
            return merged;
        }

        //拼合资源
        public void Combine()
        {
            int k = 0;
            do
            {
                int n = 0;
                while (MergeOneReferAssets())
                {
                    ++n;
                }
                Debug.Log("Merge one refer use " + n + " Times");
                ++k;
            } while (MergeSameReferAssets());
            Debug.Log("Combine assets use " + k + " Times");
        }
        #endregion

        #region Data

        [System.Serializable]
        public class NodeInfo
        {
            //主路径
            public string mainAsset;
            //资源
            public List<string> assets;
            //直接依赖
            public List<string> dependencies;
            public bool standalone = false;
            public int refersHashCode = 0;
        }

        [System.Serializable]
        public class AssetsData
        {
            public NodeInfo[] assets;
        }

        protected AssetsData GetAssetsData()
        {
            //crate asset infos
            List<NodeInfo> assetInfos = new List<NodeInfo>();
            HashSet<Node> assets = GetAssets();
            foreach (var node in assets)
            {
                NodeInfo nodeInfo = new NodeInfo()
                {
                    mainAsset = node.mainAsset,
                    assets = new List<string>(node.assets),
                    standalone = node.IsStandalone(),
                    refersHashCode = node.refersHashCode,
                };

                nodeInfo.dependencies = new List<string>();
                foreach (var dep in node.dependencies)
                {
                    nodeInfo.dependencies.Add(dep.mainAsset);
                }

                assetInfos.Add(nodeInfo);
            }
            //create data
            AssetsData data = new AssetsData();
            data.assets = assetInfos.ToArray();

            return data;
        }

        protected void SetAssetsData(AssetsData data)
        {
            //create node info
            foreach (var nodeInfo in data.assets)
            {
                Node node = new Node(nodeInfo.mainAsset);
                node.SetStandalone(nodeInfo.standalone);
                node.refersHashCode = nodeInfo.refersHashCode;

                foreach (var assetName in nodeInfo.assets)
                {
                    node.assets.Add(assetName);
                }

                AddNode(node);
            }

            //build dependencies and refers
            foreach (var nodeInfo in data.assets)
            {
                Node node = GetNode(nodeInfo.mainAsset);
                if (node != null)
                {
                    foreach (var dep in nodeInfo.dependencies)
                    {
                        Node depNode = GetNode(dep);
                        if (depNode != null)
                        {
                            node.dependencies.Add(depNode);
                            depNode.refers.Add(node);
                        }
                        else
                        {
                            Debug.LogErrorFormat("SetAssetsData can't {0} find dependency {1}", nodeInfo.mainAsset, dep);
                        }
                    }
                }
                else
                {
                    Debug.LogErrorFormat("SetAssetsData can't find node {0}", nodeInfo.mainAsset);
                }
            }
        }

        public void SaveAssetsData(string filePath)
        {
            AssetsData data = GetAssetsData();
            string content = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, content);
        }

        public void LoadAssetsData(string filePath)
        {
            if (File.Exists(filePath))
            {
                string content = File.ReadAllText(filePath);
                AssetsData data = JsonUtility.FromJson<AssetsData>(content);
                SetAssetsData(data);
            }
        }
        #endregion

        #region Bundle
        protected BundleInfo CreateBundleInfo(Node node, Setting.Format forma)
        {
            string mainAsset = node.mainAsset;

            BundleFolderInfo parent = null;
            if ((forma & Setting.Format.WithFolder) == Setting.Format.WithFolder)
            {
                parent = Model.CreateOrGetBundleFolder(null, Setting.FilterFolderPrefix(Path.GetDirectoryName(mainAsset))) as BundleFolderConcreteInfo;
            }

            string bundleName = Setting.CreateBundleName(mainAsset, forma);
            var newBundle = Model.CreateEmptyBundle(parent, bundleName);
            foreach (var assetPath in node.assets)
            {
                Model.MoveAssetToBundle(assetPath, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
            }
            return newBundle;
        }

        public List<BundleInfo> GenerateBundles(Setting.Format format)
        {
            List<BundleInfo> bundleInfos = new List<BundleInfo>();
            HashSet<Optimizer.Node> assets = GetAssets();
            foreach (var node in assets)
            {
                BundleInfo bundleInfo = CreateBundleInfo(node, format);
                if (bundleInfo != null)
                {
                    bundleInfos.Add(bundleInfo);
                }
            }
            Model.ExecuteAssetMove();
            Model.Refresh();
            return bundleInfos;
        }
        #endregion

    }
}
