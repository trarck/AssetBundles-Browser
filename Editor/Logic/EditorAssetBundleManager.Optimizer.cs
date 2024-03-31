using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YH.AssetManage;

namespace AssetBundleBuilder
{
	public partial class EditorAssetBundleManager
	{

        //拼合资源
        public void Combine()
        {
            //只要执行一次就可以了。
            BaseCombine();

            int k = 0;
            do
            {
                int n = 0;
                while (MergeOneRefer())
                {
                    ++n;
                }
                Debug.Log("Merge one refer use " + n + " Times");
                ++k;
            } while (MergeSameRefer());
            Debug.Log("Combine assets use " + k + " Times");
        }

       protected void BaseCombine()
        {
            MergeShaderToShaderVariantCollection();
            MergeSpriteToAtlas();
        }

        /// <summary>
        /// shader和ShaderVariant打在一起
        /// </summary>
        protected void MergeShaderToShaderVariantCollection()
        {
            List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
            List<BundleInfo> deps = new List<BundleInfo>();
            foreach (var bundle in bundles)
            {
                if (bundle.enbale && bundle.isShaderVariantCollection && bundle.dependencies.Count > 0)
                {
                    deps.Clear();
                    deps.AddRange(bundle.dependencies);
                    foreach (var dep in deps)
                    {
                        MergeBundle(dep, bundle);
                    }
                }
            }
        }

        /// <summary>
        /// 图集打在一起
        /// </summary>
        protected void MergeSpriteToAtlas()
        {

        }

        /// <summary>
        /// 合并只有一个引用的项
        /// </summary>
        /// <returns></returns>
        protected bool MergeOneRefer()
        {
            bool merged = false;
            List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
            foreach (var bundle in bundles)
            {
                if (bundle.enbale && bundle.refers.Count == 1 && bundle.canMerge)
                {
                    var iter = bundle.refers.GetEnumerator();
                    iter.MoveNext();
                    //检查目标是不是Scene。Scene所在的AssetBundle,不能包含其它资源
                    if (!iter.Current.isScene)
                    {
                        merged = true;
                        MergeBundle(bundle, iter.Current);
                    }
                }
            }
            return merged;
        }

        /// <summary>
        /// 合并相同引用的项
        /// </summary>
        /// <returns></returns>
        protected bool MergeSameRefer()
        {
            bool merged = false;
            Dictionary<int, List<BundleInfo>> sameRefers = new Dictionary<int, List<BundleInfo>>();
            List<BundleInfo> bundles = new List<BundleInfo>(m_Bundles);
            foreach (var bundle in bundles)
            {
                if (bundle.enbale && bundle.canMerge)
                {
                    int hash = bundle.refersHashCode;
                    if (hash != 0)
                    {
                        List<BundleInfo> items = null;
                        if (!sameRefers.TryGetValue(hash, out items))
                        {
                            items = new List<BundleInfo>();
                            sameRefers[hash] = items;
                        }
                        items.Add(bundle);
                    }
                }
            }

            foreach (var iter in sameRefers)
            {
                if (iter.Value.Count > 1)
                {
                    merged = true;
                    for (int i = 1; i < iter.Value.Count; ++i)
                    {
                        MergeBundle(iter.Value[i], iter.Value[0]);
                    }
                }
            }
            return merged;
        }
    }
}
