﻿using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using AssetBundleBuilder.Model;

namespace AssetBundleBuilder.View
{
    internal class AssetBundleTree : TreeView
    { 
        AssetBundleManageTab m_Controller;
        private bool m_ContextOnItem = false;
        List<UnityEngine.Object> m_EmptyObjectList = new List<UnityEngine.Object>();

        internal AssetBundleTree(TreeViewState state, AssetBundleManageTab ctrl) : base(state)
        {
            Model.Model.Rebuild();
            m_Controller = ctrl;
            showBorder = true;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item != null && item.displayName.Length > 0;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            var bundleItem = item as BundleTreeItem;
            return bundleItem.bundle.DoesItemMatchSearch(search);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var bundleItem = (args.item as BundleTreeItem);
            if (args.item.icon == null)
                extraSpaceBeforeIconAndLabel = 16f;
            else
                extraSpaceBeforeIconAndLabel = 0f;

            Color old = GUI.color;
            if ((bundleItem.bundle as Model.BundleVariantFolderNode) != null)
                GUI.color = Model.Model.k_LightGrey; //new Color(0.3f, 0.5f, 0.85f);
            base.RowGUI(args);
            GUI.color = old;

            var message = bundleItem.BundleMessage();
            if(message.severity != MessageType.None)
            {
                var size = args.rowRect.height;
                var right = args.rowRect.xMax;
                Rect messageRect = new Rect(right - size, args.rowRect.yMin, size, size);
                GUI.Label(messageRect, new GUIContent(message.icon, message.message ));
            }
        }

        protected override void RenameEnded(RenameEndedArgs args)
        { 
            base.RenameEnded(args);
            if (args.newName.Length > 0 && args.newName != args.originalName)
            {
                args.newName = args.newName.ToLower();
                args.acceptedRename = true;

                BundleTreeItem renamedItem = FindItem(args.itemID, rootItem) as BundleTreeItem;
                args.acceptedRename = Model.Model.HandleBundleRename(renamedItem.bundle, args.newName);
                ReloadAndSelect(renamedItem.bundle.nameHashCode, false);
            }
            else
            {
                args.acceptedRename = false;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            Model.Model.Refresh();
            var root = BundleTreeItem.Create(Model.Model.GetRootBundle(),-1);
            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {

            var selectedBundles = new List<Model.BundleNode>();
            if (selectedIds != null)
            {
                foreach (var id in selectedIds)
                {
                    var item = FindItem(id, rootItem) as BundleTreeItem;
                    if(item != null && item.bundle != null)
                    {
                        item.bundle.RefreshAssetList();
                        selectedBundles.Add(item.bundle);
                    }
                }
            }

            m_Controller.UpdateSelectedBundles(selectedBundles);
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }


        protected override void ContextClicked()
        {
            if (m_ContextOnItem)
            {
                m_ContextOnItem = false;
                return;
            }

            List<BundleTreeItem> selectedNodes = new List<BundleTreeItem>();
            foreach (var nodeID in GetSelection())
            {
                selectedNodes.Add(FindItem(nodeID, rootItem) as BundleTreeItem);
            }

            GenericMenu menu = new GenericMenu();

            if (!Model.Model.DataSource.IsReadOnly ()) {
                menu.AddItem(new GUIContent("Add new bundle"), false, CreateNewBundle, selectedNodes); 
                menu.AddItem(new GUIContent("Add new folder"), false, CreateFolder, selectedNodes);
            }

            menu.AddItem(new GUIContent("Reload all data"), false, ForceReloadData, selectedNodes);

            menu.AddItem(new GUIContent("Import with short name"), false, ImportWithShortName, selectedNodes);
            menu.AddItem(new GUIContent("Import with full name"), false, ImportWithFullName, selectedNodes);
            menu.AddItem(new GUIContent("Import with folder"), false, ImportWithFolder, selectedNodes);

            menu.ShowAsContext();
        }

        protected override void ContextClickedItem(int id)
        {
            if (Model.Model.DataSource.IsReadOnly ()) {
                return;
            }

            m_ContextOnItem = true;
            List<BundleTreeItem> selectedNodes = new List<BundleTreeItem>();
            foreach (var nodeID in GetSelection())
            {
                selectedNodes.Add(FindItem(nodeID, rootItem) as BundleTreeItem);
            }
            
            GenericMenu menu = new GenericMenu();
            
            if(selectedNodes.Count == 1)
            {
                if ((selectedNodes[0].bundle as BundleFolderConcreteNode) != null)
                {
                    menu.AddItem(new GUIContent("Add Child/New Bundle"), false, CreateNewBundle, selectedNodes);
                    menu.AddItem(new GUIContent("Add Child/New Folder"), false, CreateFolder, selectedNodes);
                    menu.AddItem(new GUIContent("Add Sibling/New Bundle"), false, CreateNewSiblingBundle, selectedNodes);
                    menu.AddItem(new GUIContent("Add Sibling/New Folder"), false, CreateNewSiblingFolder, selectedNodes);
                }
                else if( (selectedNodes[0].bundle as BundleVariantFolderNode) != null)
                {
                    menu.AddItem(new GUIContent("Add Child/New Variant"), false, CreateNewVariant, selectedNodes);
                    menu.AddItem(new GUIContent("Add Sibling/New Bundle"), false, CreateNewSiblingBundle, selectedNodes);
                    menu.AddItem(new GUIContent("Add Sibling/New Folder"), false, CreateNewSiblingFolder, selectedNodes);
                }
                else
                {
                    var variant = selectedNodes[0].bundle as BundleVariantDataNode;
                    if (variant == null)
                    {
                        menu.AddItem(new GUIContent("Add Sibling/New Bundle"), false, CreateNewSiblingBundle, selectedNodes);
                        menu.AddItem(new GUIContent("Add Sibling/New Folder"), false, CreateNewSiblingFolder, selectedNodes);
                        menu.AddItem(new GUIContent("Convert to variant"), false, ConvertToVariant, selectedNodes);
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("Add Sibling/New Variant"), false, CreateNewSiblingVariant, selectedNodes);
                    }
                }
                if(selectedNodes[0].bundle.IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles))
                    menu.AddItem(new GUIContent("Move duplicates to new bundle"), false, DedupeAllBundles, selectedNodes);
                menu.AddItem(new GUIContent("Rename"), false, RenameBundle, selectedNodes);
                menu.AddItem(new GUIContent("Delete " + selectedNodes[0].displayName), false, DeleteBundles, selectedNodes);
                
            }
            else if (selectedNodes.Count > 1)
            { 
                menu.AddItem(new GUIContent("Move duplicates shared by selected"), false, DedupeOverlappedBundles, selectedNodes);
                menu.AddItem(new GUIContent("Move duplicates existing in any selected"), false, DedupeAllBundles, selectedNodes);
                menu.AddItem(new GUIContent("Delete " + selectedNodes.Count + " selected bundles"), false, DeleteBundles, selectedNodes);
            }
            menu.ShowAsContext();
        }
        void ForceReloadData(object context)
        {
            Model.Model.ForceReloadData();
            Reload();
        }

        void CreateNewSiblingFolder(object context)
        {
            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                Model.BundleFolderConcreteNode folder = null;
                folder = selectedNodes[0].bundle.parent as BundleFolderConcreteNode;
                CreateFolderUnderParent(folder);
            }
            else
                Debug.LogError("could not add 'sibling' with no bundles selected");
        }
        void CreateFolder(object context)
        {
            Model.BundleFolderConcreteNode folder = null;
            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0 && selectedNodes[0]!=null)
            {
                folder = selectedNodes[0].bundle as Model.BundleFolderConcreteNode;
            }
            CreateFolderUnderParent(folder);
        }
        void CreateFolderUnderParent(BundleFolderConcreteNode folder)
        {
            var newBundle = Model.Model.CreateEmptyBundleFolder(folder);
            ReloadAndSelect(newBundle.nameHashCode, true);
        }
        void RenameBundle(object context)
        {
            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                BeginRename(FindItem(selectedNodes[0].bundle.nameHashCode, rootItem));
            }
        }

        void CreateNewSiblingBundle(object context)
        {
            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                BundleFolderConcreteNode folder = null;
                folder = selectedNodes[0].bundle.parent as BundleFolderConcreteNode;
                CreateBundleUnderParent(folder);
            }
            else
                Debug.LogError("could not add 'sibling' with no bundles selected");
        }
        void CreateNewBundle(object context)
        {
            BundleFolderConcreteNode folder = null;
            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0 && selectedNodes[0]!=null)
            {
                folder = selectedNodes[0].bundle as BundleFolderConcreteNode;
            }
            CreateBundleUnderParent(folder);
        }

        void CreateBundleUnderParent(BundleFolderNode folder)
        {
            var newBundle = Model.Model.CreateEmptyBundle(folder);
            ReloadAndSelect(newBundle.nameHashCode, true);
        }


        void CreateNewSiblingVariant(object context)
        {
            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                Model.BundleVariantFolderNode folder = null;
                folder = selectedNodes[0].bundle.parent as Model.BundleVariantFolderNode;
                CreateVariantUnderParent(folder);
            }
            else
                Debug.LogError("could not add 'sibling' with no bundles selected");
        }
        void CreateNewVariant(object context)
        {
            BundleVariantFolderNode folder = null;
            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count == 1)
            {
                folder = selectedNodes[0].bundle as Model.BundleVariantFolderNode;
                CreateVariantUnderParent(folder);
            }
        }
        void CreateVariantUnderParent(BundleVariantFolderNode folder)
        {
            if (folder != null)
            {
                var newBundle = Model.Model.CreateEmptyVariant(folder);
                ReloadAndSelect(newBundle.nameHashCode, true);
            }
        }

        void ConvertToVariant(object context)
        {
            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes.Count == 1)
            {
                var bundle = selectedNodes[0].bundle as BundleDataNode;
                var newBundle = Model.Model.HandleConvertToVariant(bundle);
                int hash = 0;
                if (newBundle != null)
                    hash = newBundle.nameHashCode;
                ReloadAndSelect(hash, true);
            }
        }

        void DedupeOverlappedBundles(object context)
        {
            DedupeBundles(context, true);
        }
        void DedupeAllBundles(object context)
        {
            DedupeBundles(context, false);
        }
        void DedupeBundles(object context, bool onlyOverlappedAssets)
        {
            var selectedNodes = context as List<BundleTreeItem>;
            var newBundle = Model.Model.HandleDedupeBundles(selectedNodes.Select(item => item.bundle), onlyOverlappedAssets);
            if(newBundle != null)
            {
                var selection = new List<int>();
                selection.Add(newBundle.nameHashCode);
                ReloadAndSelect(selection);
            }
            else
            {
                if (onlyOverlappedAssets)
                    Debug.LogWarning("There were no duplicated assets that existed across all selected bundles.");
                else
                    Debug.LogWarning("No duplicate assets found after refreshing bundle contents.");
            }
        }

        void DeleteBundles(object b)
        {
            var selectedNodes = b as List<BundleTreeItem>;
            Model.Model.HandleBundleDelete(selectedNodes.Select(item => item.bundle));
            ReloadAndSelect(new List<int>());


        }
        protected override void KeyEvent()
        {
            if (Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                List<BundleTreeItem> selectedNodes = new List<BundleTreeItem>();
                foreach (var nodeID in GetSelection())
                {
                    selectedNodes.Add(FindItem(nodeID, rootItem) as BundleTreeItem);
                }
                DeleteBundles(selectedNodes);
            }
        }

        #region Drag
        class DragAndDropData
        {
            internal bool hasBundleFolder = false;
            internal bool hasScene = false;
            internal bool hasNonScene = false;
            internal bool hasVariantChild = false;
            internal List<BundleNode> draggedNodes;
            internal BundleTreeItem targetNode;
            internal DragAndDropArgs args;
            internal string[] paths;

            internal DragAndDropData(DragAndDropArgs a)
            {
                args = a;
                draggedNodes = DragAndDrop.GetGenericData("Model.BundleNode") as List<BundleNode>;
                targetNode = args.parentItem as BundleTreeItem;
                paths = DragAndDrop.paths;

                if (draggedNodes != null)
                {
                    foreach (var bundle in draggedNodes)
                    {
                        if ((bundle as Model.BundleFolderNode) != null)
                        {
                            hasBundleFolder = true;
                        }
                        else
                        {
                            var dataBundle = bundle as Model.BundleDataNode;
                            if (dataBundle != null)
                            {
                                if (dataBundle.isSceneBundle)
                                    hasScene = true;
                                else
                                    hasNonScene = true;

                                if ( (dataBundle as Model.BundleVariantDataNode) != null)
                                    hasVariantChild = true;
                            }
                        }
                    }
                }
                else if (DragAndDrop.paths != null)
                {
                    foreach (var assetPath in DragAndDrop.paths)
                    {
                        if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(SceneAsset))
                            hasScene = true;
                        else
                            hasNonScene = true;
                    }
                }
            }

        }
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;
            DragAndDropData data = new DragAndDropData(args);
            
            if (Model.Model.DataSource.IsReadOnly ()) {
                return DragAndDropVisualMode.Rejected;
            }

            if ( (data.hasScene && data.hasNonScene) ||
                (data.hasVariantChild) )
                return DragAndDropVisualMode.Rejected;
            
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                    visualMode = HandleDragDropUpon(data);
                    break;
                case DragAndDropPosition.BetweenItems:
                    visualMode = HandleDragDropBetween(data);
                    break;
                case DragAndDropPosition.OutsideItems:
                    if (data.draggedNodes != null)
                    {
                        visualMode = DragAndDropVisualMode.Copy;
                        if (data.args.performDrop)
                        {
                            Model.Model.HandleBundleReparent(data.draggedNodes, null);
                            Reload();
                        }
                    }
                    else if(data.paths != null)
                    {
                        visualMode = DragAndDropVisualMode.Copy;
                        if (data.args.performDrop)
                        {
                            DragPathsToNewSpace(data.paths, null);
                        }
                    }
                    break;
            }
            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropUpon(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Copy;//Move;
            var targetDataBundle = data.targetNode.bundle as Model.BundleDataNode;
            if (targetDataBundle != null)
            {
                if (targetDataBundle.isSceneBundle)
                {
                    if(data.hasNonScene)
                        return DragAndDropVisualMode.Rejected;
                }
                else
                {
                    if (data.hasBundleFolder)
                    {
                        return DragAndDropVisualMode.Rejected;
                    }
                    else if (data.hasScene && !targetDataBundle.IsEmpty())
                    {
                        return DragAndDropVisualMode.Rejected;
                    }

                }

               
                if (data.args.performDrop)
                {
                    if (data.draggedNodes != null)
                    {
                        Model.Model.HandleBundleMerge(data.draggedNodes, targetDataBundle);
                        ReloadAndSelect(targetDataBundle.nameHashCode, false);
                    }
                    else if (data.paths != null)
                    {
                        Model.Model.MoveAssetToBundle(data.paths, targetDataBundle.m_Name.bundleName, targetDataBundle.m_Name.variant);
                        Model.Model.ExecuteAssetMove();
                        ReloadAndSelect(targetDataBundle.nameHashCode, false);
                    }
                }

            }
            else
            {
                var folder = data.targetNode.bundle as Model.BundleFolderNode;
                if (folder != null)
                {
                    if (data.args.performDrop)
                    {
                        if (data.draggedNodes != null)
                        {
                            Model.Model.HandleBundleReparent(data.draggedNodes, folder);
                            Reload();
                        }
                        else if (data.paths != null)
                        {
                            DragPathsToNewSpace(data.paths, folder);
                        }
                    }
                }
                else
                    visualMode = DragAndDropVisualMode.Rejected; //must be a variantfolder
                
            }
            return visualMode;
        }
        private DragAndDropVisualMode HandleDragDropBetween(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Copy;//Move;

            var parent = (data.args.parentItem as BundleTreeItem);

            if (parent != null)
            {
                var variantFolder = parent.bundle as Model.BundleVariantFolderNode;
                if (variantFolder != null)
                    return DragAndDropVisualMode.Rejected;

                if (data.args.performDrop)
                {
                    var folder = parent.bundle as Model.BundleFolderConcreteNode;
                    if (folder != null)
                    {
                        if (data.draggedNodes != null)
                        {
                            Model.Model.HandleBundleReparent(data.draggedNodes, folder);
                            Reload();
                        }
                        else if (data.paths != null)
                        {
                            DragPathsToNewSpace(data.paths, folder);
                        }
                    }
                }
            }

            return visualMode;
        }

        private string[] dragToNewSpacePaths = null;
        private Model.BundleFolderNode dragToNewSpaceRoot = null;
        private void DragPathsAsOneBundle()
        {
            var newBundle = Model.Model.CreateEmptyBundle(dragToNewSpaceRoot);
            Model.Model.MoveAssetToBundle(dragToNewSpacePaths, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
            Model.Model.ExecuteAssetMove();
            ReloadAndSelect(newBundle.nameHashCode, true);
        }
        private void DragPathsAsManyBundles()
        {
            List<int> hashCodes = new List<int>();
            foreach (var assetPath in dragToNewSpacePaths)
            {
                var newBundle = Model.Model.CreateEmptyBundle(dragToNewSpaceRoot, System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower());
                Model.Model.MoveAssetToBundle(assetPath, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
                hashCodes.Add(newBundle.nameHashCode);
            }
            Model.Model.ExecuteAssetMove();
            ReloadAndSelect(hashCodes);
        }

        private void DragPathsAsManyBundlesEx()
        {
            List<int> hashCodes = new List<int>();
            Setting.Format format = Setting.Format.ShortName;
            if(m_Controller.assetBundleNameWithExt)
            {
                format |= Setting.Format.WithExt;
            }

            foreach (var assetPath in dragToNewSpacePaths)
            {
                string fullPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), assetPath);
                if (System.IO.Directory.Exists(fullPath))
                {
                    hashCodes.AddRange(Import.ImportFolder(fullPath, dragToNewSpaceRoot as BundleFolderConcreteNode,
                        format));
                }
                else
                {
                    hashCodes.Add(Import.ImportFile(assetPath, dragToNewSpaceRoot as BundleFolderConcreteNode, format));
                }
            }
            Model.Model.ExecuteAssetMove();
            ReloadAndSelect(hashCodes);
        }

        private void DragPathsToNewSpace(string[] paths, BundleFolderNode root)
        {
            dragToNewSpacePaths = paths;
            dragToNewSpaceRoot = root;
            if (paths.Length > 1)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Create 1 Bundle"), false, DragPathsAsOneBundle);
                var message = "Create ";
                message += paths.Length;
                message += " Bundles";
                menu.AddItem(new GUIContent(message), false, DragPathsAsManyBundlesEx);
                menu.ShowAsContext();
            }
            else
                DragPathsAsManyBundlesEx();
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            if (args.draggedItemIDs == null)
                return;

            DragAndDrop.PrepareStartDrag();

            var selectedBundles = new List<BundleNode>();
            foreach (var id in args.draggedItemIDs)
            {
                var item = FindItem(id, rootItem) as BundleTreeItem;
                selectedBundles.Add(item.bundle);
            }
            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = m_EmptyObjectList.ToArray();
            DragAndDrop.SetGenericData("Model.BundleNode", selectedBundles);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;//Move;
            DragAndDrop.StartDrag("AssetBundleTree");
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }
        #endregion

        internal void Refresh()
        {
            var selection = GetSelection();
            Reload();
            SelectionChanged(selection);
        }

        private void ReloadAndSelect(int hashCode, bool rename)
        {
            var selection = new List<int>();
            selection.Add(hashCode);
            ReloadAndSelect(selection);
            if(rename)
            {
                BeginRename(FindItem(hashCode, rootItem), 0.25f);
            }
        }
        private void ReloadAndSelect(IList<int> hashCodes)
        {
            Reload();
            SetSelection(hashCodes, TreeViewSelectionOptions.RevealAndFrame);
            SelectionChanged(hashCodes);
        }

        #region Import

        void ImportWithShortName(object context)
        {
            ImportAssets(context, Setting.Format.ShortName);
        }

        void ImportWithFullName(object context)
        {
            ImportAssets(context, Setting.Format.FullPath);
        }

        void ImportWithFolder(object context)
        {
            ImportAssets(context, Setting.Format.WithFolder);
        }

        void ImportAssets(object context, Setting.Format format)
        {
            BundleFolderConcreteNode folder = null;

            var selectedNodes = context as List<BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                folder = selectedNodes[0].bundle as BundleFolderConcreteNode;
            }
            //translate to asset path
            List<string> assetPaths = new List<string>();
            foreach (UnityEngine.Object obj in Selection.objects)
            {

                string objPath = AssetDatabase.GetAssetPath(obj.GetInstanceID());
                assetPaths.Add(objPath);
            }

            ImportAssets(assetPaths, folder,format);    
        }

        public void ImportAssets(Setting.Format format)
        {
            //translate to asset path
            List<string> assetPaths = new List<string>();
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                string objPath = AssetDatabase.GetAssetPath(obj.GetInstanceID());
                assetPaths.Add(objPath);
            }

            ImportAssets(assetPaths, null, format);
        }

        public void ImportAssets(List<string> assetPaths, BundleFolderConcreteNode parent=null, Setting.Format format= Setting.Format.None)
        {
            //check parent folder.
            if (parent == null)
            {
                //get from selection
                foreach (var nodeID in GetSelection())
                {
                    BundleTreeItem treeItem =FindItem(nodeID, rootItem) as BundleTreeItem;
                    if (treeItem != null)
                    {
                        parent = treeItem.bundle as BundleFolderConcreteNode;
                        if (parent != null)
                        {
                            break;
                        }
                    }
                }
            }

            foreach (string assetPath in assetPaths)
            {
                string fullPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), assetPath);
                if (System.IO.Directory.Exists(fullPath))
                {
                    Import.ImportFolder(fullPath, parent, format);
                }
                else
                {
                    Import.ImportFile(assetPath, parent,format);
                }
            }

            //save to database
            Model.Model.ExecuteAssetMove();

            //refresh bundle list tree
            Refresh();
        }

        #endregion
    }
}
