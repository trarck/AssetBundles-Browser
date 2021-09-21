using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using AssetBundleBuilder.Model;
using UnityEditor.IMGUI.Controls;

namespace AssetBundleBuilder.View
{
    internal class BundleDetailItem : TreeViewItem
    {
        internal BundleDetailItem(int id, int depth, string displayName, MessageType type) : base(id, depth, displayName)
        {
            MessageLevel = type;
        }

        internal MessageType MessageLevel
        { get; set; }
    }

    internal class TogglePathTreeViewItem : TreeViewItem
    {
        private static bool m_DisplayAlt = false;
        
        private string m_DisplayNamePrefix;
        private string m_Path;

        public string Path
        {
            get { return m_Path; }
        }
        
        public string DisplayNamePrefix
        {
            get { return m_DisplayNamePrefix; }
        }

        public TogglePathTreeViewItem( int id, int depth, string displayName, string path )
        {
            base.depth = depth;
            base.id = id;
            base.displayName = displayName;
            m_Path = path;
            m_DisplayNamePrefix = "";
        }
        
        public TogglePathTreeViewItem( int id, int depth, string displayNamePrefix, string displayName, string path )
        {
            base.depth = depth;
            base.id = id;
            base.displayName = displayName;
            m_Path = path;
            m_DisplayNamePrefix = displayNamePrefix;
        }
        
        public override string displayName
        {
            get
            {
                // TODO this is a bit unresponsive here in large projects, see if can be better elsewhere
                Event e = Event.current;
                if( e.alt && e.type == EventType.MouseDown )
                    m_DisplayAlt = !m_DisplayAlt;

                return m_DisplayNamePrefix + ( m_DisplayAlt ? m_Path : base.displayName );
            }
            set
            {
                base.displayName = value;
            }
        }
    }
    internal class BundleDetailList : TreeView
    {
        HashSet<BundleTreeDataItem> m_Selecteditems;
        Rect m_TotalRect;

        const float k_DoubleIndent = 32f;
        const string k_SizeHeader = "Size: ";
        const string k_DependencyHeader = "Dependent On:";
        const string k_DependencyEmpty = k_DependencyHeader + " - None";
        const string k_MessageHeader = "Messages:";
        const string k_MessageEmpty = k_MessageHeader + " - None";
        private const string k_ReferencedPrefix = "- ";


        internal BundleDetailList(TreeViewState state) : base(state)
        {
            m_Selecteditems = new HashSet<BundleTreeDataItem>();
            showBorder = true;
        }
        internal void Update()
        {
            bool dirty = false;
            foreach (var bundle in m_Selecteditems)
            {
                dirty |= bundle.dirty;
            }
            if (dirty)
            {
                Reload();
                ExpandAll( 2 );
            }
        }
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();
            if (m_Selecteditems != null)
            {
                foreach(var bundle in m_Selecteditems)
                {
                    root.AddChild(AppendBundleToTree(bundle));
                }
            }
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if ((args.item as BundleDetailItem) != null)
            {
                EditorGUI.HelpBox(
                    new Rect(args.rowRect.x + k_DoubleIndent, args.rowRect.y, args.rowRect.width - k_DoubleIndent, args.rowRect.height), 
                    args.item.displayName,
                    (args.item as BundleDetailItem).MessageLevel);
            }
            else
            {
                Color old = GUI.color;
                if (args.item.depth == 1 &&
                    (args.item.displayName == k_MessageEmpty || args.item.displayName == k_DependencyEmpty))
                    GUI.color = Model.Model.k_LightGrey;
                base.RowGUI(args);
                GUI.color = old;
            }
        }
        public override void OnGUI(Rect rect)
        {
            m_TotalRect = rect;
            base.OnGUI(rect);
        }
        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            if( (item as BundleDetailItem) != null)
            {
                float height = DefaultStyles.backgroundEven.CalcHeight(new GUIContent(item.displayName), m_TotalRect.width);
                return height + 3f;
            }
            return base.GetCustomRowHeight(row, item);
        }

        
        protected override void SelectionChanged( IList<int> selectedIds )
        {
            base.SelectionChanged( selectedIds );
            List<string> pathList = new List<string>();

            for( int i = 0; i < selectedIds.Count; ++i )
            {
                TreeViewItem item = this.FindItem( selectedIds[i], rootItem );
                if( item != null )
                {
                    AddDependentAssetsRecursive( item, pathList );
                }
            }
            
            AssetBundleBuilderMain.instance.m_ManageTab.SetAssetListSelection( pathList );
        }

        void AddDependentAssetsRecursive( TreeViewItem item, List<string> pathList )
        {
            TogglePathTreeViewItem pathItem = item as TogglePathTreeViewItem;
            if( pathItem != null )
            {
                if( string.IsNullOrEmpty(pathItem.DisplayNamePrefix) == false && pathList.Contains( pathItem.Path ) == false )
                {
                    pathList.Add( pathItem.Path );
                }
            }

            if( item.hasChildren )
            {
                for( int i=0; i<item.children.Count; ++i )
                    AddDependentAssetsRecursive( item.children[i], pathList );
            }
        }

        protected override void DoubleClickedItem( int id )
        {
            base.DoubleClickedItem( id );
            TreeViewItem item = this.FindItem( id, rootItem );
            if( item != null )
            {
                TogglePathTreeViewItem pathItem = item as TogglePathTreeViewItem;
                if( pathItem != null )
                {
                    Object o = AssetDatabase.LoadAssetAtPath<Object>( pathItem.Path );
                    if( o != null )
                    {
                        Selection.activeObject = o;
                        EditorGUIUtility.PingObject( o );
                    }
                }
            }
        }

        internal static TreeViewItem AppendBundleToTree(BundleTreeDataItem bundle)
        {
            var itemName = bundle.nameData.fullNativeName;
            var bunRoot = new TreeViewItem(itemName.GetHashCode(), 0, itemName);

            var str = itemName + k_SizeHeader;
            var sz = new TreeViewItem(str.GetHashCode(), 1, k_SizeHeader + bundle.GetTotalSizeStr());

            str = itemName + k_DependencyHeader;
            var dependency = new TreeViewItem(str.GetHashCode(), 1, k_DependencyEmpty);
            if(bundle.haveDependecy)
            {
                var depList = bundle.bundleInfo.dependencies;
                dependency.displayName = k_DependencyHeader;
                foreach (var dep in depList)
                {
                    str = itemName + dep.name;
                    TreeViewItem newItem = new TreeViewItem( str.GetHashCode(), 2, dep.name );
                    newItem.icon = Model.Model.GetBundleIcon();
                    dependency.AddChild(newItem);
                }
            }

            str = itemName + k_MessageHeader;
            var msg = new TreeViewItem(str.GetHashCode(), 1, k_MessageEmpty);
            if (bundle.HasMessages())
            {
                msg.displayName = k_MessageHeader;
                var currMessages = bundle.GetMessages();

                foreach(var currMsg in currMessages)
                {
                    str = itemName + currMsg.message;
                    msg.AddChild(new BundleDetailItem(str.GetHashCode(), 2, currMsg.message, currMsg.severity));
                }
            }


            bunRoot.AddChild(sz);
            bunRoot.AddChild(dependency);
            bunRoot.AddChild(msg);

            return bunRoot;
        }

        internal void SetItems(IEnumerable<BundleTreeItem> items)
        {
            m_Selecteditems.Clear();
            foreach(var item in items)
            {
                CollectBundles(item);
            }
            SetSelection(new List<int>());
            Reload();
            ExpandAll( 2 );
        }
        internal void CollectBundles(BundleTreeItem bundle)
        {
            var bunData = bundle as BundleTreeDataItem;
            if (bunData != null)
                m_Selecteditems.Add(bunData);
            else
            {
                var bunFolder = bundle as BundleTreeFolderItem;
                if (bunFolder.hasChildren)
                {
                    foreach (var bun in bunFolder.children)
                    {
                        CollectBundles(bun as BundleTreeItem);
                    }
                }
            }
        }

        internal void ExpandAll( int maximumDepth )
        {
            List<int> expanded = new List<int>( GetExpanded() );
            FindItems( rootItem, maximumDepth, expanded );
            SetExpanded( expanded );
        }
        
        internal void FindItems( TreeViewItem item, int maximumDepth, List<int> expanded )
        {
            if( item.depth >= maximumDepth || ! item.hasChildren )
                return;
            
            expanded.Add( item.id );
            for( int i = 0; i < item.children.Count; ++i )
            {
                FindItems( item.children[i], maximumDepth, expanded );
            }
        }
    }
}
