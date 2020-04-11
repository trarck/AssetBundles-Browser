using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AssetBundleBuilder.Model;


namespace AssetBundleBuilder.Distribute
{
    public class DeepNode:BundleNode
    {
        public int deep = -1;
    }

    public class BundleGroup
    {
        List<DeepNode> m_Bundles;
        //key:assets full path
        Dictionary<string, DeepNode> m_AssetBundles;

        public BundleGroup()
        {
            m_Bundles = new List<DeepNode>();
            m_AssetBundles = new Dictionary<string, DeepNode>();
        }

        public void BuildRelationShip()
        {
            m_Bundles.Clear();
            m_AssetBundles.Clear();

            //create bundle
            string[] bundleNames= Model.Model.DataSource.GetAllAssetBundleNames();
            foreach(var bundleName in bundleNames)
            {
                DeepNode node = new DeepNode();
                node.name = bundleName;
                m_Bundles.Add(node);
                //add assets
                string[] assets = Model.Model.DataSource.GetAssetPathsFromAssetBundle(bundleName);
                foreach(var asset in assets)
                {
                    node.assets.Add(asset);
                    //asset in bundle map
                    m_AssetBundles[asset] = node;
                }
            }

            //create deps
            DeepNode depNode = null;
            foreach (var node in m_Bundles)
            {
                foreach(var asset in node.assets)
                {
                    string[] depAssets = AssetDatabase.GetDependencies(asset, false);
                    foreach(var depAsset in depAssets)
                    {
                        if(m_AssetBundles.TryGetValue(depAsset,out depNode))
                        {
                            node.AddDependency(depNode);
                            depNode.AddRefer(node);
                        }
                    }
                }
            }
        }

        public List<List<DeepNode>> Group()
        {
            Dictionary<int,List<DeepNode>> grouped = new Dictionary<int, List<DeepNode>>();
            Dictionary<string,DeepNode >remainds =CaculateDeeps();

            bool haveRemaind = remainds.Count > 0;

            //create deep layers
            foreach (var bundle in m_Bundles)
            {
                if (haveRemaind && !remainds.ContainsKey(bundle.name))
                {
                    List<DeepNode> nodes = null;
                    if (!grouped.TryGetValue(bundle.deep, out nodes))
                    {
                        nodes = new List<DeepNode>();
                        grouped[bundle.deep] = nodes;
                    }
                    nodes.Add(bundle);
                }
            }
            //to layer list
            List<List<DeepNode>> layers = new List<List<DeepNode>>();
            for(int i = 0; i < grouped.Count; ++i)
            {
                layers.Add(grouped[i]);
            }

            if (haveRemaind)
            {
                //all remaind together
                List<DeepNode> allRemaind = new List<DeepNode>(remainds.Values);
                layers.Add(allRemaind);
            }

            return layers;
        }

        protected Dictionary<string, DeepNode> CaculateDeeps()
        {
            //create bundle map
            Dictionary<string, DeepNode> remainds = new Dictionary<string, DeepNode>();
            Queue<DeepNode> noDepsNodes = new Queue<DeepNode>();

            //search no deps bundle
            foreach (var node in m_Bundles)
            {
                if (node.dependencies.Count == 0)
                {
                    noDepsNodes.Enqueue(node);
                }
                else
                {
                    remainds[node.name] = node;
                }
            }

            ParseNoDeps(noDepsNodes, ref remainds);

            ////check remainds
            //while (remainds.Count > 0)
            //{
            //    bool breakSuccess = false;

            //    Debug.LogErrorFormat("The remain bundle not empty.count={0}.Thiers are:[", remainds.Count);
            //    foreach(var r in remainds)
            //    {
            //        Debug.LogErrorFormat("---{0}", r.Key);
            //    }
            //    Debug.LogError("]----------------");

            //    //存在循环引用
            //    foreach (var iter in remainds)
            //    {
            //        //在deep不为-1的结点上断开
            //        if (iter.Value.deep != -1)
            //        {
            //            noDepsNodes.Enqueue(iter.Value);
            //            breakSuccess = true;
            //        }
            //    }

            //    ParseNoDeps(noDepsNodes, ref remainds);

            //    if(!breakSuccess && remainds.Count > 0)
            //    {
            //        break;
            //    }
            //}

            return remainds;
        }

        void ParseNoDeps(Queue<DeepNode> noDepsNodes, ref Dictionary<string, DeepNode> remainds)
        {
            while (noDepsNodes.Count > 0)
            {
                DeepNode node = noDepsNodes.Dequeue();
                //set deep
                if (node.deep == -1)
                {
                    node.deep = 0;
                }
                //remove from remainds
                remainds.Remove(node.name);
                //parse refers
                ParseRefers(node, ref noDepsNodes);
            }
        }

        void ParseRefers(DeepNode node,ref Queue<DeepNode> noDepsNodes)
        {
            foreach(var refer in node.refers)
            {
                DeepNode referNode = refer as DeepNode;
                //set deep
                int deep = node.deep + 1;
                if (referNode.deep < deep)
                {
                    referNode.deep = deep;
                }
                //remove dep
                referNode.RemoveDependency(node);
                //add no deps node
                if (referNode.dependencies.Count == 0)
                {
                    noDepsNodes.Enqueue(referNode);
                }
            }
        }

        public void ShowInfos()
        {
            foreach(var node in m_Bundles)
            {
                Debug.LogFormat("Bundle {0}:{1}", node.name, node.deep);
            }
        }
    }
}
