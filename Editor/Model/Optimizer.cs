//using System.Collections.Generic;
//using System.IO;
//using UnityEditor;
//using UnityEngine;

//namespace AssetBundleBuilder.Model
//{
//    public class Optimizer
//    {
//        //List<Node> m_Assets=new List<Node>();
//        //这里的node允许重复。有些资源可能被合并到一起。
//        Dictionary<string, AssetBundleBuilder.BundleInfo> m_AssetsMap = new Dictionary<string, AssetBundleBuilder.BundleInfo>();

//        #region Node

//        public void AddNode(AssetBundleBuilder.BundleInfo node)
//        {
//            //m_Assets.Add(node);
//            foreach (var assetName in node.assets)
//            {
//                m_AssetsMap[assetName] = node;
//            }
//        }

//        public AssetBundleBuilder.BundleInfo GetNode(string assetName)
//        {
//			AssetBundleBuilder.BundleInfo node = null;
//            m_AssetsMap.TryGetValue(assetName, out node);
//            return node;
//        }

//        public void RemoveNode(AssetBundleBuilder.BundleInfo node)
//        {
//            foreach (var assetName in node.assets)
//            {
//                if (m_AssetsMap.ContainsKey(assetName))
//                {
//                    m_AssetsMap.Remove(node.mainAsset);
//                }
//            }

//        }

//        public void ReplaceNode(AssetBundleBuilder.BundleInfo from, AssetBundleBuilder.BundleInfo to)
//        {
//            foreach (var assetName in from.assets)
//            {
//                m_AssetsMap[assetName] = to;
//            }
//        }

//        public bool Exists(string assetName)
//        {
//            return m_AssetsMap.ContainsKey(assetName);
//        }

//        public AssetBundleBuilder.BundleInfo MergeNode(AssetBundleBuilder.BundleInfo from, AssetBundleBuilder.BundleInfo to)
//        {
//            //合并资源
//            foreach (var asset in from.assets)
//            {
//                to.assets.Add(asset);
//            }

//            //合并引用
//            foreach (var refer in from.refers)
//            {
//                if (refer != to)
//                {
//                    to.AddReferOnly(refer);
//                    refer.RemoveDependencyOnly(from);
//                    refer.AddDependencyOnly(to);
//                }
//            }

//            //合并依赖
//            foreach (var dep in from.dependencies)
//            {
//                if (dep != to)
//                {
//                    to.AddDependencyOnly(dep);
//                    dep.RemoveReferOnly(from);
//                    dep.AddReferOnly(to);
//                }
//            }

//            //如果from在to的refers或dependencies中(循环引用)，则移除。
//            if (to.refers.Contains(from))
//            {
//                to.RemoveReferOnly(from);
//            }

//            if (to.dependencies.Contains(from))
//            {
//                to.RemoveDependencyOnly(from);
//            }

//            //Repalce from assets.
//            ReplaceNode(from, to);

//            return to;
//        }

//        //m_AssetsMap里可能被合并之后会有重复的node，这里去除重复
//        public HashSet<AssetBundleBuilder.BundleInfo> GetAssets()
//        {
//			HashSet<AssetBundleBuilder.BundleInfo> assets = new HashSet<AssetBundleBuilder.BundleInfo>();
//            foreach (var iter in m_AssetsMap)
//            {
//                assets.Add(iter.Value);
//            }
//            return assets;
//        }

//        //通过Asset列表构建AssetsMap，以便支持增量导入。
//        public void SetAssets(HashSet<AssetBundleBuilder.BundleInfo> assets)
//        {
//            if (m_AssetsMap == null)
//            {
//				m_AssetsMap = new Dictionary<string, AssetBundleBuilder.BundleInfo>();
//            }
//            else
//            {
//                m_AssetsMap.Clear();
//            }

//            foreach (var node in assets)
//            {
//                foreach (var assetName in node.assets)
//                {
//                    m_AssetsMap[assetName] = node;
//                }
//            }
//        }
//        #endregion

//        #region Load

//        /// <summary>
//        /// 加载依赖项
//        /// </summary>
//        /// <param name="node"></param>
//        public void LoadDependencies(AssetBundleBuilder.BundleInfo node)
//        {
//            List<string> deps = new List<string>();
//            foreach (var asset in node.assets)
//            {
//                string[] ds = AssetDatabase.GetDependencies(asset, false);
//                deps.AddRange(ds);
//            }

//            foreach (var dep in deps)
//            {
//				AssetBundleBuilder.BundleInfo depNode = LoadAsset(dep, true);
//                if (depNode != null)
//                {
//                    node.AddDependencyOnly(depNode);
//                    depNode.AddReferOnly(node);
//                }
//            }
//        }

//        /// <summary>
//        /// 加载资源
//        /// </summary>
//        /// <param name="assetPath"></param>
//        /// <param name="loadDependency"></param>
//        /// <returns></returns>
//        public AssetBundleBuilder.BundleInfo LoadAsset(string assetPath, bool loadDependency = true)
//        {
//            if (IgnoreAsset(assetPath))
//            {
//                return null;
//            }

//            if (Path.IsPathRooted(assetPath))
//            {
//                assetPath = ModelUtils.Relative(Path.GetDirectoryName(Application.dataPath), assetPath);
//            }

//			AssetBundleBuilder.BundleInfo node = GetNode(assetPath);
//            if (node == null)
//            {
//                node = new BundleNode(null,assetPath);
//                //一定要先添加，后加载Dependencies。否则就会因为循环引用而无限循环。
//                AddNode(node);
//                if (loadDependency)
//                {
//                    LoadDependencies(node);
//                }
//            }
//            return node;
//        }

//        /// <summary>
//        /// 从目录加载资源
//        /// </summary>
//        /// <param name="folderPath"></param>
//        /// <param name="pattern"></param>
//        public void LoadFromFolder(string folderPath, string pattern = null)
//        {
//            DirectoryInfo startInfo = new DirectoryInfo(folderPath);
//            if (!startInfo.Exists)
//            {
//                return;
//            }

//            Stack<DirectoryInfo> dirs = new Stack<DirectoryInfo>();
//            dirs.Push(startInfo);

//            DirectoryInfo dir;

//            bool haveFilter = false;
//            System.Text.RegularExpressions.Regex reg = null;
//            if (!string.IsNullOrEmpty(pattern))
//            {
//                haveFilter = true;
//                reg = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
//            }

//            while (dirs.Count > 0)
//            {
//                dir = dirs.Pop();

//                foreach (FileInfo fi in dir.GetFiles())
//                {
//                    if (!haveFilter || reg.IsMatch(fi.FullName))
//                    {
//						AssetBundleBuilder.BundleInfo node = LoadAsset(fi.FullName);
//                        if (node != null)
//                        {
//                            node.SetStandalone(true);
//                        }
//                    }
//                }

//                foreach (DirectoryInfo subDir in dir.GetDirectories())
//                {
//                    if (!subDir.Name.StartsWith("."))
//                    {
//                        dirs.Push(subDir);
//                    }
//                }
//            }
//        }

//        protected bool IgnoreAsset(string assetPath)
//        {
//            string ext = Path.GetExtension(assetPath).ToLower();
//            return ext == ".meta" || ext == ".cs" || ext == ".js" || ext == ".dll";
//        }
//        #endregion

//        #region Combine
//        /// <summary>
//        /// ShaderVariantCollection用到的shader要打在一个assetbundle里。
//        /// </summary>
//        protected void MergeShaderToShaderVariantCollection()
//        {
//			HashSet<AssetBundleBuilder.BundleInfo> assets = GetAssets();
//			List<AssetBundleBuilder.BundleInfo> deps = new List<AssetBundleBuilder.BundleInfo>();
//            foreach (var node in assets)
//            {
//                if (node.isShaderVariantCollection)
//                {
//                    deps.Clear();
//                    deps.AddRange(node.dependencies);
//                    foreach(var dep in deps)
//                    {
//                        MergeNode(dep,node);
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// 合并只有一个引用的项
//        /// </summary>
//        /// <returns></returns>
//        protected bool MergeOneReferAssets()
//        {
//            bool merged = false;
//			HashSet<AssetBundleBuilder.BundleInfo> assets = GetAssets();

//            foreach (var node in assets)
//            {
//                if (node.refers.Count == 1 && node.canMerge)
//                {
//                    var iter = node.refers.GetEnumerator();
//                    iter.MoveNext();
//                    //检查目标是不是Scene。Scene所在的AssetBundle,不能包含其它资源
//                    if (!iter.Current.isScene)
//                    {
//                        merged = true;
//                        MergeNode(node, iter.Current);
//                    }
//                }
//            }
//            return merged;
//        }

//        /// <summary>
//        /// 合并相同引用的项
//        /// </summary>
//        /// <returns></returns>
//        protected bool MergeSameReferAssets()
//        {
//            bool merged = false;
//			HashSet<AssetBundleBuilder.BundleInfo> assets = GetAssets();
//			Dictionary<int, List<AssetBundleBuilder.BundleInfo>> sameRefers = new Dictionary<int, List<AssetBundleBuilder.BundleInfo>>();

//            foreach (var node in assets)
//            {
//                if (node.canMerge)
//                {
//                    int hash = node.refersHashCode;
//					List<AssetBundleBuilder.BundleInfo> items = null;
//                    if (!sameRefers.TryGetValue(hash, out items))
//                    {
//                        items = new List<AssetBundleBuilder.BundleInfo>();
//                        sameRefers[hash] = items;
//                    }
//                    items.Add(node);
//                }
//            }

//            foreach (var iter in sameRefers)
//            {
//                if (iter.Value.Count > 1)
//                {
//                    for (int i = 1; i < iter.Value.Count; ++i)
//                    {
//                        MergeNode(iter.Value[i], iter.Value[0]);
//                    }
//                }
//            }
//            return merged;
//        }

//        //拼合资源
//        public void Combine()
//        {
//            //只要执行一次就可以了。
//            MergeShaderToShaderVariantCollection();

//            int k = 0;
//            do
//            {
//                int n = 0;
//                while (MergeOneReferAssets())
//                {
//                    ++n;
//                }
//                Debug.Log("Merge one refer use " + n + " Times");
//                ++k;
//            } while (MergeSameReferAssets());
//            Debug.Log("Combine assets use " + k + " Times");
//        }
//        #endregion

//        #region Data

//        [System.Serializable]
//        public class NodeInfo
//        {
//            //主路径
//            public string mainAsset;
//            //资源
//            public List<string> assets;
//            //直接依赖
//            public List<string> dependencies;
//            public bool standalone = false;
//            public int refersHashCode = 0;
//        }

//        [System.Serializable]
//        public class AssetsData
//        {
//            public NodeInfo[] assets;
//        }

//        protected AssetsData GetAssetsData()
//        {
//            //crate asset infos
//            List<NodeInfo> assetInfos = new List<NodeInfo>();
//			HashSet<AssetBundleBuilder.BundleInfo> assets = GetAssets();
//            foreach (var node in assets)
//            {
//                NodeInfo nodeInfo = new NodeInfo()
//                {
//                    mainAsset = node.mainAsset,
//                    assets = new List<string>(node.assets),
//                    standalone = node.IsStandalone(),
//                    refersHashCode = node.refersHashCode,
//                };

//                nodeInfo.dependencies = new List<string>();
//                foreach (var dep in node.dependencies)
//                {
//                    nodeInfo.dependencies.Add(dep.mainAsset);
//                }

//                assetInfos.Add(nodeInfo);
//            }
//            //create data
//            AssetsData data = new AssetsData();
//            data.assets = assetInfos.ToArray();

//            return data;
//        }

//        protected void SetAssetsData(AssetsData data)
//        {
//            //create node info
//            foreach (var nodeInfo in data.assets)
//            {
//                BundleNode node = new BundleNode(null,nodeInfo.mainAsset);
//                node.SetStandalone(nodeInfo.standalone);
//                node.refersHashCode = nodeInfo.refersHashCode;

//                foreach (var assetName in nodeInfo.assets)
//                {
//                    node.assets.Add(assetName);
//                }

//                AddNode(node);
//            }

//            //build dependencies and refers
//            foreach (var nodeInfo in data.assets)
//            {
//				AssetBundleBuilder.BundleInfo node = GetNode(nodeInfo.mainAsset);
//                if (node != null)
//                {
//                    foreach (var dep in nodeInfo.dependencies)
//                    {
//						AssetBundleBuilder.BundleInfo depNode = GetNode(dep);
//                        if (depNode != null)
//                        {
//                            node.dependencies.Add(depNode);
//                            depNode.refers.Add(node);
//                        }
//                        else
//                        {
//                            Debug.LogErrorFormat("SetAssetsData can't {0} find dependency {1}", nodeInfo.mainAsset, dep);
//                        }
//                    }
//                }
//                else
//                {
//                    Debug.LogErrorFormat("SetAssetsData can't find node {0}", nodeInfo.mainAsset);
//                }
//            }
//        }

//        public void SaveAssetsData(string filePath)
//        {
//            AssetsData data = GetAssetsData();
//            string content = JsonUtility.ToJson(data, true);
//            File.WriteAllText(filePath, content);
//        }

//        public void LoadAssetsData(string filePath)
//        {
//            if (File.Exists(filePath))
//            {
//                string content = File.ReadAllText(filePath);
//                AssetsData data = JsonUtility.FromJson<AssetsData>(content);
//                SetAssetsData(data);
//            }
//        }
//        #endregion

//        #region Bundle
//        protected BundleNode CreateBundleInfo(AssetBundleBuilder.BundleInfo node, Setting.Format forma)
//        {
//            string mainAsset = node.mainAsset;

//            BundleFolderInfo parent = null;
//            if ((forma & Setting.Format.WithFolder) == Setting.Format.WithFolder)
//            {
//                parent = Model.CreateOrGetBundleFolder(null, Setting.GetBundleFolderName(Path.GetDirectoryName(mainAsset))) as BundleFolderConcreteInfo;
//            }

//            string bundleName = Setting.CreateBundleName(mainAsset, forma);
//            var newBundle = Model.CreateOrGetBundle(parent, bundleName);
//            BundleDataInfo dataBundle=newBundle as BundleDataInfo;
//            if (dataBundle != null && !dataBundle.IsEmpty())
//            {
//                Dictionary<string, bool> signMap = new Dictionary<string, bool>();
//                foreach(var assetInfo in dataBundle.GetConcretes())
//                {
//                    signMap[assetInfo.fullAssetName] = true;
//                }
//                foreach (var assetPath in node.assets)
//                {
//                    if (!signMap.ContainsKey(assetPath))
//                    {
//                        Model.MoveAssetToBundle(assetPath, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
//                    }
//                }
//            }
//            else
//            {
//                foreach (var assetPath in node.assets)
//                {
//                    Model.MoveAssetToBundle(assetPath, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
//                }
//            }
//            return newBundle;
//        }

//        public List<BundleNode> GenerateBundles(Setting.Format format)
//        {
//            List<BundleNode> bundleInfos = new List<BundleNode>();
//			HashSet<AssetBundleBuilder.BundleInfo> assets = GetAssets();
//            foreach (var node in assets)
//            {
//                BundleNode bundleInfo = CreateBundleInfo(node, format);
//                if (bundleInfo != null)
//                {
//                    bundleInfos.Add(bundleInfo);
//                }
//            }
//            Model.ExecuteAssetMove();
//            Model.Refresh();
//            return bundleInfos;
//        }
//        #endregion

//    }
//}
