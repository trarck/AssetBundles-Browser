using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using AssetBundleBrowser.AssetBundleModel;

namespace AssetBundleBrowser.View
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
            return m_Bundle.HighestMessage();
        }

        public override string displayName
        {
            get
            {
                return AssetBundleBrowserMain.instance.m_ManageTab.hasSearch ? m_Bundle.m_Name.fullNativeName : m_Bundle.displayName;
            }
        }
    }
}
