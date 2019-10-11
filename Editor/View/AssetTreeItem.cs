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
        private AssetInfo m_asset;
        internal AssetInfo asset
        {
            get { return m_asset; }
        }
        internal AssetTreeItem() : base(-1, -1) { }
        internal AssetTreeItem(AssetInfo a) : base(a != null ? a.fullAssetName.GetHashCode() : Random.Range(int.MinValue, int.MaxValue), 0, a != null ? a.displayName : "failed")
        {
            m_asset = a;
            if (a != null)
                icon = AssetDatabase.GetCachedIcon(a.fullAssetName) as Texture2D;
        }

        private Color m_color = new Color(0, 0, 0, 0);
        internal Color itemColor
        {
            get
            {
                if (m_color.a == 0.0f && m_asset != null)
                {
                    m_color = m_asset.GetColor();
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
            return m_asset != null ?
                m_asset.HighestMessageLevel() : MessageType.Error;
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
                if (c != null && c.asset != null && c.asset.fullAssetName == asset.fullAssetName)
                {
                    contains = true;
                    break;
                }
            }

            return contains;
        }

        internal void AddAssets(BundleInfo bundleInfo)
        {
            //if (bundleInfo.HaveChildren())
            //{
            //    foreach (var child in bundleInfo.GetChildren())
            //    {
            //        AddAssets(child);
            //    }
            //}
            //else
            {
                List<AssetInfo> assets = bundleInfo.GetConcretes();
                if (assets != null)
                {
                    foreach (var asset in assets)
                        AddChild(new AssetTreeItem(asset));
                }

                assets = bundleInfo.GetDependencies();
                if (assets != null)
                {
                    foreach (var asset in assets)
                    {
                        if (!ContainsChild(asset))
                            AddChild(new AssetTreeItem(asset));
                    }
                }
            }

            bundleInfo.dirty = false;
        }

    }
}