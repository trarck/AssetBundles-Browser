using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using AssetBundleBuilder.Model;

namespace AssetBundleBuilder.View
{
    internal sealed class BundleTreeItem : TreeViewItem
    {
        private BundleInfo m_Bundle;
        internal BundleInfo bundle
        {
            get { return m_Bundle; }
        }

        internal BundleTreeItem(BundleInfo b, int depth, Texture2D iconTexture) : base(b.nameHashCode, depth, b.displayName)
        {
            m_Bundle = b;
            icon = iconTexture;
            children = new List<TreeViewItem>();
        }

        internal MessageSystem.Message BundleMessage()
        {
            return bundle.HighestMessage();
        }

        public override string displayName
        {
            get
            {
                return AssetBundleBrowserMain.instance.m_ManageTab.hasSearch ? bundle.m_Name.fullNativeName : bundle.displayName;
            }
        }

        //internal void AddAssetsToNode(AssetTreeItem node)
        //{
        //    if (m_Bundle.HaveChildren())
        //    {
        //        foreach(var child in children)
        //        {
        //            BundleTreeItem bundleTreeItem = (BundleTreeItem)child;
        //            if (bundleTreeItem!=null)
        //            {
        //                bundleTreeItem.AddAssetsToNode(node);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        List<AssetInfo> assets = m_Bundle.GetConcretes();
        //        if (assets != null)
        //        {
        //            foreach (var asset in assets)
        //                node.AddChild(new AssetTreeItem(asset));
        //        }

        //        assets = m_Bundle.GetDependencies();
        //        if (assets != null)
        //        {
        //            foreach (var asset in assets)
        //            {
        //                if (!node.ContainsChild(asset))
        //                    node.AddChild(new AssetTreeItem(asset));
        //            }
        //        }
        //    }

        //    m_Bundle.dirty = false;
        //}

        public static BundleTreeItem Create(BundleInfo b, int depth)
        {
            if (!b.HaveChildren())
            {
                b.RefreshAssetList();
            }
            b.RefreshMessages();
            var result = new BundleTreeItem(b, depth, b.GetIcon());

            if (b.HaveChildren())
            {
                foreach (var child in b.GetChildren())
                {
                    result.AddChild(BundleTreeItem.Create(child,depth+1));
                }
            }

            return result;
        }
    }
}
