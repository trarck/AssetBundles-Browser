using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using AssetBundleBuilder.Model;

namespace AssetBundleBuilder.View
{
    public  class BundleTreeItem : TreeViewItem
    {
		private BundleNameData m_NameData;
		protected MessageSystem.MessageState m_BundleMessages = new MessageSystem.MessageState();

		public BundleNameData nameData
		{
			get
			{
				return m_NameData;
			}
			set
			{
				m_NameData = value;
			}
		}

		public int nameHashCode
		{
			get
			{
				return m_NameData.GetHashCode();
			}
		}

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

		public BundleTreeItem(string name, int depth) : base(name!=null?name.GetHashCode():0, depth, name)
		{
			if (name != null)
			{
				m_NameData = new BundleNameData(name);
			}
		}

		public BundleTreeItem(BundleNameData name, int depth) : base(name.GetHashCode(), depth, name.shortName)
		{
			m_NameData = name;
		}

		public BundleTreeItem(BundleNode b, int depth, Texture2D iconTexture) : base(b.nameHashCode, depth, b.displayName)
        {
            icon = iconTexture;
            children = new List<TreeViewItem>();
        }

		public MessageSystem.Message BundleMessage()
        {
			return new MessageSystem.Message("", MessageType.Info);// bundleNode.HighestMessage();
        }

        public override string displayName
        {
            get
            {
				return m_NameData.shortName;// AssetBundleBuilderMain.instance.m_ManageTab.hasSearch ? bundle.m_Name.fullNativeName : bundle.displayName;
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

		public static BundleTreeItem Create(BundleNode b, int depth)
        {
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

		public BundleTreeDataItem(BundleInfo bundleInfo, int depth) : base(bundleInfo.name, depth)
		{
			m_BundleInfo = bundleInfo;
		}

		public BundleTreeDataItem(BundleInfo bundleInfo,BundleNameData nameData, int depth) : base(nameData, depth)
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

		public BundleTreeFolderItem(string name,int depth) : base(name, depth)
		{

		}

		public BundleTreeFolderItem(BundleNameData name, int depth) : base(name, depth)
		{

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
