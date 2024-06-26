﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using AssetBundleBuilder.Model;

namespace AssetBundleBuilder.View
{
    internal sealed class AssetTreeItem : TreeViewItem
    {
		internal static /*const*/ Color k_LightGrey = Color.grey * 1.5f;

		private AssetInfo m_asset;
        internal AssetInfo asset
        {
            get { return m_asset; }
        }
        internal AssetTreeItem() : base(-1, -1) { }
        internal AssetTreeItem(AssetInfo a) : base(a != null ? a.assetPath.GetHashCode() : Random.Range(int.MinValue, int.MaxValue), 0, a != null ? a.displayName : "failed")
        {
            m_asset = a;
            if (a != null)
                icon = AssetDatabase.GetCachedIcon(a.assetPath) as Texture2D;
        }

        private Color m_color = new Color(0, 0, 0, 0);
        internal Color itemColor
        {
            get
            {
                if (m_color.a == 0.0f && m_asset != null)
                {
					if (m_asset.bundle==null)
						m_color = k_LightGrey;
					else
						m_color =  Color.white;
				}
                return m_color;
            }
            set { m_color = value; }
        }
        internal Texture2D MessageIcon()
        {
            return MessageSystem.GetIcon(HighestMessageLevel());
        }
        internal MessageType HighestMessageLevel()
        {
            return MessageType.None;//            m_asset != null ?   m_asset.HighestMessageLevel() : MessageType.Error;
        }

        internal bool ContainsChild(AssetInfo asset)
        {
            bool contains = false;
            if (children == null)
                return contains;

            if (asset == null)
                return false;
            foreach (var child in children)
            {
                var c = child as AssetTreeItem;
                if (c != null && c.asset != null && c.asset.assetPath == asset.assetPath)
                {
                    contains = true;
                    break;
                }
            }

            return contains;
        }

        private static HashSet<AssetInfo> m_IncludeAssets = new HashSet<AssetInfo>();

        internal void AddAssets(BundleTreeItem item)
        {
            if (item is BundleTreeFolderItem)
            {
                BundleTreeFolderItem folder = item as BundleTreeFolderItem;

                if (folder.children != null)
                {
                    foreach (var child in folder.children)
                    {
                        AddAssets(child as BundleTreeItem);
                    }
                }
            }
            else
            {
                BundleTreeDataItem dataItem = item as BundleTreeDataItem;
                HashSet<AssetInfo> assets = dataItem.bundleInfo != null ? dataItem.bundleInfo.assets : null;

                if (dataItem.bundleInfo != null)
                {
                    m_IncludeAssets.Clear();
                    dataItem.bundleInfo.GetIncludeAssets(ref m_IncludeAssets);
                    foreach (var asset in m_IncludeAssets)
                    {
                        if (!ContainsChild(asset))
                            AddChild(new AssetTreeItem(asset));
                    }
                }
            }
        }
    }
}
