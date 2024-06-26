﻿using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using AssetBundleBuilder.Model;
using System.Text;

namespace AssetBundleBuilder.View
{
    public  class BundleTreeItem : TreeViewItem
    {
		//private BundleNameData m_NameData;
		protected MessageSystem.MessageState m_BundleMessages = new MessageSystem.MessageState();

		//public BundleNameData nameData
		//{
		//	get
		//	{
		//		return m_NameData;
		//	}
		//	set
		//	{
		//		m_NameData = value;
		//	}
		//}

		private string m_ShortName;

		//public int nameHashCode
		//{
		//	get
		//	{
		//		return m_NameData.GetHashCode();
		//	}
		//}

		public virtual bool dirty
		{
			get
			{
				return false;
			}
		}

		public virtual bool haveChildren
		{
			get
			{
				return children != null && children.Count > 0;
			}
		}

		public BundleTreeItem(int id, int depth, string name):base(id,depth,name)
		{
			m_ShortName = name;
		}

		public BundleTreeItem(int depth) : base(0,depth)
		{

		}

		public MessageSystem.Message BundleMessage()
        {
			return new MessageSystem.Message("", MessageType.Info);// bundleNode.HighestMessage();
        }

		public override string displayName
		{
			get
			{
				return m_ShortName;// AssetBundleBuilderMain.instance.m_ManageTab.hasSearch ? bundle.m_Name.fullNativeName : bundle.displayName;
			}
			set
			{
				m_ShortName = value;
			}
		}

		public virtual string fullName
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				TreeViewItem p = parent;
				while (p != null)
				{
					if (p.parent != null)
					{
						sb.Insert(0, "/");
						sb.Insert(0, p.displayName);
					}
					p = p.parent;
				}
				sb.Append(displayName);
				return sb.ToString();
			}
		}

		public virtual string parentPath
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				TreeViewItem p = parent;
				while (p != null)
				{
					if (p.parent != null)
					{
						sb.Insert(0, "/");
						sb.Insert(0, p.displayName);
					}
				}
				return sb.ToString();
			}
		}

		public virtual List<string> pathTokens
		{
			get
			{
				List<string> tokens = null;
				if (parent != null)
				{

					tokens = new List<string>();
					TreeViewItem p = parent;
					while (p != null)
					{
						tokens.Append(p.displayName);
					}
				}
				return tokens;
			}
		}

		public virtual bool DoesItemMatchSearch(string search)
		{
			return false;
		}

		public bool IsMessageSet(MessageSystem.MessageFlag flag)
		{
			return m_BundleMessages.IsSet(flag);
		}
		public void SetMessageFlag(MessageSystem.MessageFlag flag, bool on)
		{
			m_BundleMessages.SetFlag(flag, on);
		}
		public List<MessageSystem.Message> GetMessages()
		{
			return m_BundleMessages.GetMessages();
		}
		public bool HasMessages()
		{
			return m_BundleMessages.HasMessages();
		}
    }

	public class BundleTreeDataItem : BundleTreeItem
	{
		private BundleInfo m_BundleInfo;
		public BundleInfo bundleInfo
		{
			get
			{
				return m_BundleInfo;
			}
			set
			{
				m_BundleInfo = value;
			}
		}

		public override Texture2D icon
		{
			get
			{
				if (m_BundleInfo!=null && m_BundleInfo.bundleType == BundleInfo.BundleType.Scene)
				{
					return BundleTreeManager.GetSceneIcon();
				}
				else
				{
					return BundleTreeManager.GetBundleIcon();
				}
			}
		}

		public bool isSceneBundle
		{
			get
			{
				return m_BundleInfo != null && m_BundleInfo.bundleType == BundleInfo.BundleType.Scene;
			}
		}

		public bool haveDependecy
		{
			get
			{
				return bundleInfo != null && bundleInfo.dependencies != null && bundleInfo.dependencies.Count>0;
			}
		}

		public BundleTreeDataItem(BundleInfo bundleInfo, int depth) : base(bundleInfo.name.GetHashCode(), depth,bundleInfo.displayName)
		{
			m_BundleInfo = bundleInfo;
		}

		public BundleTreeDataItem(int id, int depth, string displayName) : base(id, depth, displayName)
		{
			m_BundleInfo = bundleInfo;
		}

		public override bool DoesItemMatchSearch(string search)
		{
			if (bundleInfo != null)
			{
				//check assets
				foreach (var asset in bundleInfo.assets)
				{
					if (asset.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return true;
					}

					//check asset deps
					foreach (var dep in asset.allDependencies)
					{
						//not in other bundle
						if (dep.bundle == null && dep.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public bool IsEmpty()
		{
			return m_BundleInfo == null || m_BundleInfo.assets.Count == 0;
		}

		public long GetTotalSize()
		{
			return m_BundleInfo != null ? m_BundleInfo.GetTotalSize():0;
		}

		public string GetTotalSizeStr()
		{
			long totalSize = GetTotalSize();
			if (totalSize == 0)
				return "--";
			return EditorUtility.FormatBytes(totalSize);
		}
	}

	public class BundleTreeFolderItem : BundleTreeItem
	{
		public override Texture2D icon
		{
			get
			{
				return BundleTreeManager.GetFolderIcon();
			}
		}

		public BundleTreeFolderItem(string name,int depth,int id) : base(id, depth,name)
		{
			children = new List<TreeViewItem>();
		}

		public BundleTreeItem GetChild(string name)
		{
			if (children == null)
			{
				return null;
			}

			foreach (var child in children)
			{
				if (child.displayName == name)
				{
					return (BundleTreeItem) child;
				}
			}
			return null;
		}

		public void ClearChildren()
		{
			if (children != null)
			{
				foreach (var child in children)
				{
					child.parent = null;
				}
				children.Clear();
			}
		}
	}
}
