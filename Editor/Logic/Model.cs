﻿//using System;
//using UnityEngine;
//using UnityEditor;
//using UnityEngine.Assertions;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor.IMGUI.Controls;

//using AssetBundleBuilder.DataSource;

//namespace AssetBundleBuilder.Model
//{
//    /// <summary>
//    /// Static class holding model data for Asset Bundle Browser tool. Data in Model is read from DataSource, but is not pushed.  
//    /// 
//    /// If not using a custom DataSource, then the data comes from the AssetDatabase.  If you wish to alter the data from code, 
//    ///  you should just push changes to the AssetDatabase then tell the Model to Rebuild(). If needed, you can also loop over
//    ///  Update() until it returns true to force all sub-items to refresh.
//    ///  
//    /// </summary>
//    public static class Model
//    {
//        const string k_NewBundleBaseName = "newbundle";
//        const string k_NewVariantBaseName = "newvariant";
//        internal static /*const*/ Color k_LightGrey = Color.grey * 1.5f;

//        private static DataSource.DataSource s_DataSource;
//        private static BundleFolderConcreteNode s_RootLevelBundles = new BundleFolderConcreteNode("", null);
//        private static List<ABMoveData> s_MoveData = new List<ABMoveData>();
//        private static List<BundleNode> s_BundlesToUpdate = new List<BundleNode>();
//        private static Dictionary<string, AssetNode> s_GlobalAssetList = new Dictionary<string, AssetNode>();
//        private static Dictionary<string, HashSet<string>> s_DependencyTracker = new Dictionary<string, HashSet<string>>();

//        private static bool s_InErrorState = false;
//        const string k_DefaultEmptyMessage = "Drag assets here or right-click to begin creating bundles.";
//        const string k_ProblemEmptyMessage = "There was a problem parsing the list of bundles. See console.";
//        private static string s_EmptyMessageString;

//        static private Texture2D s_folderIcon = null;
//        static private Texture2D s_bundleIcon = null;
//        static private Texture2D s_sceneIcon = null;

//        /// <summary>
//        /// If using a custom source of asset bundles, you can implement your own ABDataSource and set it here as the active
//        ///  DataSource.  This will allow you to use the Browser with data that you provide.
//        ///  
//        /// If no custom DataSource is provided, then the Browser will create one that feeds off of and into the 
//        ///  AssetDatabase.
//        ///  
//        /// </summary>
//        public static DataSource.DataSource DataSource
//        {
//            get
//            {
//                if (s_DataSource == null)
//                {
//                    s_DataSource = new OriginDataSource ();
//                }
//                return s_DataSource;
//            }
//            set { s_DataSource = value; }
//        }

//        /// <summary>
//        /// Update will loop over bundles that need updating and update them. It will only update one bundle
//        ///  per frame and will continue on the same bundle next frame until that bundle is marked as doneUpdating.
//        ///  By default, this will cause a very slow collection of dependency data as it will only update one bundle per
//        /// </summary>
//        public static bool Update()
//        {
//            bool shouldRepaint = false;
//            ExecuteAssetMove(false);     //this should never do anything. just a safety check.

//            //TODO - look into EditorApplication callback functions.
            
//            int size = s_BundlesToUpdate.Count;
//            if (size > 0)
//            {
//                s_BundlesToUpdate[size - 1].Update();
//                s_BundlesToUpdate.RemoveAll(item => item.doneUpdating == true);
//                if (s_BundlesToUpdate.Count == 0)
//                {
//                    shouldRepaint = true;
//                    foreach(var bundle in s_RootLevelBundles.GetChildList())
//                    {
//                        bundle.RefreshDupeAssetWarning();
//                    }
//                }
//            }
//            return shouldRepaint;
//        }

//        internal static void ForceReloadData()
//        {
//            s_InErrorState = false;
            
//            Rebuild();
//            //tree.Reload();

//            bool doneUpdating = s_BundlesToUpdate.Count == 0;

//            //EditorUtility.DisplayProgressBar("Updating Bundles", "", 0);
//            int fullBundleCount = s_BundlesToUpdate.Count;
//            while (!doneUpdating && !s_InErrorState)
//            {
//                int currCount = s_BundlesToUpdate.Count;
//                //EditorUtility.DisplayProgressBar("Updating Bundles", s_BundlesToUpdate[currCount-1].displayName, (float)(fullBundleCount- currCount) / (float)fullBundleCount);
//                doneUpdating = Update();
//            }
//            //EditorUtility.ClearProgressBar();
//        }
        
//        /// <summary>
//        /// Clears and rebuilds model data.  
//        /// </summary>
//        public static void Rebuild()
//        {
//            s_RootLevelBundles = new BundleFolderConcreteNode("", null);
//            s_MoveData = new List<ABMoveData>();
//            s_BundlesToUpdate = new List<BundleNode>();
//            s_GlobalAssetList = new Dictionary<string, AssetNode>();
//            Refresh();
//        }

//        internal static void AddBundlesToUpdate(IEnumerable<BundleNode> bundles)
//        {
//            foreach(var bundle in bundles)
//            {
//                bundle.ForceNeedUpdate();
//                s_BundlesToUpdate.Add(bundle);
//            }
//        }

//        internal static void Refresh()
//        {
//            s_EmptyMessageString = k_ProblemEmptyMessage;
//            if (s_InErrorState)
//                return;

//            var bundleList = ValidateBundleList();
//            if(bundleList != null)
//            {
//                s_EmptyMessageString = k_DefaultEmptyMessage;
//                foreach (var bundleName in bundleList)
//                {
//                    AddBundleToModel(bundleName);
//                }
//                AddBundlesToUpdate(s_RootLevelBundles.GetChildList());
//            }

//            if(s_InErrorState)
//            {
//                s_RootLevelBundles = new BundleFolderConcreteNode("", null);
//                s_EmptyMessageString = k_ProblemEmptyMessage;
//            }
//        }

//        internal static string[] ValidateBundleList()
//        {
//            var bundleList = DataSource.GetAllAssetBundleNames();
//            bool valid = true;
//            HashSet<string> bundleSet = new HashSet<string>();
//            int index = 0;
//            bool attemptedBundleReset = false;
//            while(index < bundleList.Length)
//            {
//                var name = bundleList[index];
//                if (!bundleSet.Add(name))
//                {
//                    LogError("Two bundles share the same name: " + name);
//                    valid = false;
//                }

//                int lastDot = name.LastIndexOf('.');
//                if (lastDot > -1)
//                {
//                    var bunName = name.Substring(0, lastDot);
//                    var extraDot = bunName.LastIndexOf('.');
//                    if(extraDot > -1)
//                    {
//                        if(attemptedBundleReset)
//                        {
//                            var message = "Bundle name '" + bunName + "' contains a period.";
//                            message += "  Internally Unity keeps 'bundleName' and 'variantName' separate, but externally treat them as 'bundleName.variantName'.";
//                            message += "  If a bundleName contains a period, the build will (probably) succeed, but this tool cannot tell which portion is bundle and which portion is variant.";
//                            LogError(message);
//                            valid = false;
//                        }
//                        else
//                        {
//                            if (!DataSource.IsReadOnly ())
//                            {
//                                DataSource.RemoveUnusedAssetBundleNames();
//                            }
//                            index = 0;
//                            bundleSet.Clear();
//                            bundleList = DataSource.GetAllAssetBundleNames();
//                            attemptedBundleReset = true;
//                            continue;
//                        }
//                    }


//                    if (bundleList.Contains(bunName))
//                    {
//                        //there is a bundle.none and a bundle.variant coexisting.  Need to fix that or return an error.
//                        if (attemptedBundleReset)
//                        {
//                            valid = false;
//                            var message = "Bundle name '" + bunName + "' exists without a variant as well as with variant '" + name.Substring(lastDot+1) + "'.";
//                            message += " That is an illegal state that will not build and must be cleaned up.";
//                            LogError(message);
//                        }
//                        else
//                        {
//                            if (!DataSource.IsReadOnly ())
//                            {
//                                DataSource.RemoveUnusedAssetBundleNames();
//                            }
//                            index = 0;
//                            bundleSet.Clear();
//                            bundleList = DataSource.GetAllAssetBundleNames();
//                            attemptedBundleReset = true;
//                            continue;
//                        }
//                    }
//                }

//                index++;
//            }

//            if (valid)
//                return bundleList;
//            else
//                return null;
//        }

//        internal static bool BundleListIsEmpty()
//        {
//            return (s_RootLevelBundles.GetChildList().Count() == 0);
//        }

//        internal static string GetEmptyMessage()
//        {
//            return s_EmptyMessageString;
//        }

//        internal static BundleNode CreateEmptyBundle(BundleFolderNode folder = null, string newName = null)
//        {
//            if ((folder as BundleVariantFolderNode) != null)
//                return CreateEmptyVariant(folder as BundleVariantFolderNode);

//            folder = (folder == null) ? s_RootLevelBundles : folder;
//            string name = GetUniqueName(folder, newName);
//            BundleNameData nameData;
//            nameData = new BundleNameData(folder.m_Name.bundleName, name);
//            return AddBundleToFolder(folder, nameData);
//        }

//        internal static BundleNode CreateOrGetBundle(BundleFolderNode folder = null, string name = null)
//        {
//            if ((folder as BundleVariantFolderNode) != null)
//                return CreateOrGetVariant(folder as BundleVariantFolderNode,name);

//            folder = (folder == null) ? s_RootLevelBundles : folder;
//            BundleNameData nameData;
//            nameData = new BundleNameData(folder.m_Name.bundleName, name);
//            return AddBundleToFolder(folder, nameData);
//        }

//        internal static BundleNode CreateEmptyVariant(BundleVariantFolderNode folder)
//        {
//            string name = GetUniqueName(folder, k_NewVariantBaseName);
//            string variantName = folder.m_Name.bundleName + "." + name;
//            BundleNameData nameData = new BundleNameData(variantName);
//            return AddBundleToFolder(folder.parent, nameData);
//        }

//        internal static BundleNode CreateOrGetVariant(BundleVariantFolderNode folder,string name)
//        {
//            name =string.IsNullOrEmpty(name)?k_NewVariantBaseName:name;
//            string variantName = folder.m_Name.bundleName + "." + name;
//            BundleNameData nameData = new BundleNameData(variantName);
//            return AddBundleToFolder(folder.parent, nameData);
//        }

//        internal static BundleFolderNode CreateEmptyBundleFolder(BundleFolderConcreteNode folder = null)
//        {
//            folder = (folder == null) ? s_RootLevelBundles : folder;
//            string name = GetUniqueName(folder) + "/dummy";
//            BundleNameData nameData = new BundleNameData(folder.m_Name.bundleName, name);
//            return AddFoldersToBundle(s_RootLevelBundles, nameData);
//        }

//        internal static BundleFolderNode CreateEmptyBundleFolder(BundleFolderConcreteNode folder,string newName)
//        {
//            folder = (folder == null) ? s_RootLevelBundles : folder;
//            string name = GetUniqueName(folder, newName)+"/dummy";
//            BundleNameData nameData = new BundleNameData(folder.m_Name.bundleName, name);
//            return AddFoldersToBundle(s_RootLevelBundles, nameData);
//        }

//        internal static BundleFolderNode CreateOrGetBundleFolder(BundleFolderConcreteNode folder, string newName)
//        {
//            folder = (folder == null) ? s_RootLevelBundles : folder;
//            if (string.IsNullOrEmpty(newName))
//            {
//                return folder;
//            }

//            string name = newName + "/dummy";
//            BundleNameData nameData = new BundleNameData(folder.m_Name.bundleName, name);
//            return AddFoldersToBundle(s_RootLevelBundles, nameData);
//        }

//        private static BundleNode AddBundleToModel(string name)
//        {
//            if (name == null)
//                return null;
            
//            BundleNameData nameData = new BundleNameData(name);

//            BundleFolderNode folder = AddFoldersToBundle(s_RootLevelBundles, nameData);
//            if (folder == null)
//            {
//                folder = s_RootLevelBundles;
//            }
//            BundleNode currInfo = AddBundleToFolder(folder, nameData);

//            return currInfo;
//        }

//        internal static BundleFolderNode AddFoldersToBundle(BundleFolderNode parent, BundleNameData nameData)
//        {
//            BundleNode currInfo = null;
//            BundleFolderNode folder = parent;
//            var size = nameData.pathTokens.Count;
//            for (var index = 0; index < size; index++)
//            {
//                if (folder != null)
//                {
//                    currInfo = folder.GetChild(nameData.pathTokens[index]);
//                    if (currInfo == null)
//                    {
//                        currInfo = new BundleFolderConcreteNode(nameData.pathTokens, index + 1, folder);
//                        folder.AddChild(currInfo);
//                    }

//                    folder = currInfo as BundleFolderNode;
//                    if (folder == null)
//                    {
//                        s_InErrorState = true;
//                        LogFolderAndBundleNameConflict(currInfo.m_Name.fullNativeName);
//                        break;
//                    }
//                }
//            }
//            return currInfo as BundleFolderNode;
//        }

//        private static void LogFolderAndBundleNameConflict(string name)
//        {
//            var message = "Bundle '";
//            message += name;
//            message += "' has a name conflict with a bundle-folder.";
//            message += "Display of bundle data and building of bundles will not work.";
//            message += "\nDetails: If you name a bundle 'x/y', then the result of your build will be a bundle named 'y' in a folder named 'x'.  You thus cannot also have a bundle named 'x' at the same level as the folder named 'x'.";
//            LogError(message);
//        }

//        private static BundleNode AddBundleToFolder(BundleFolderNode folder, BundleNameData nameData)
//        {
//            BundleNode currInfo = folder.GetChild(nameData.shortName);
//            if (!System.String.IsNullOrEmpty(nameData.variant))
//            {
//                if(currInfo == null)
//                {
//                    currInfo = new BundleVariantFolderNode(nameData.bundleName, folder);
//                    folder.AddChild(currInfo);
//                }
//                var variantFolder = currInfo as BundleVariantFolderNode;
//                if (variantFolder == null)
//                {
//                    var message = "Bundle named " + nameData.shortName;
//                    message += " exists both as a standard bundle, and a bundle with variants.  ";
//                    message += "This message is not supported for display or actual bundle building.  ";
//                    message += "You must manually fix bundle naming in the inspector.";
                    
//                    LogError(message);
//                    return null;
//                }
                
                
//                currInfo = variantFolder.GetChild(nameData.variant);
//                if (currInfo == null)
//                {
//                    currInfo = new BundleVariantDataNode(nameData.fullNativeName, variantFolder);
//                    variantFolder.AddChild(currInfo);
//                }
                
//            }
//            else
//            {
//                if (currInfo == null)
//                {
//                    currInfo = new BundleDataNode(nameData.fullNativeName, folder);
//                    folder.AddChild(currInfo);
//                }
//                else
//                {
//                    var dataInfo = currInfo as BundleDataNode;
//                    if (dataInfo == null)
//                    {
//                        s_InErrorState = true;
//                        LogFolderAndBundleNameConflict(nameData.fullNativeName);
//                    }
//                }
//            }
//            return currInfo;
//        }

//        private static string GetUniqueName(BundleFolderNode folder, string suggestedName = null)
//        {
//            suggestedName = (suggestedName == null) ? k_NewBundleBaseName : suggestedName;
//            string name = suggestedName;
//            int index = 1;
//            bool foundExisting = (folder.GetChild(name) != null);
//            while (foundExisting)
//            {
//                name = suggestedName + index;
//                index++;
//                foundExisting = (folder.GetChild(name) != null);
//            }
//            return name;
//        }

//        //internal static BundleTreeItem CreateBundleTreeView()
//        //{
//        //    return s_RootLevelBundles.CreateTreeView(-1);
//        //}

//        public static BundleNode GetRootBundle()
//        {
//            return s_RootLevelBundles;
//        }

//        //internal static AssetTreeItem CreateAssetListTreeView(IEnumerable<BundleInfo> selectedBundles)
//        //{
//        //    var root = new AssetTreeItem();
//        //    if (selectedBundles != null)
//        //    {
//        //        foreach (var bundle in selectedBundles)
//        //        {
//        //            bundle.AddAssetsToNode(root);
//        //        }
//        //    }
//        //    return root;
//        //}

//        internal static bool HandleBundleRename(BundleNode bundle, string newName)
//        {
//            var originalName = new BundleNameData(bundle.m_Name.fullNativeName);

//            var findDot = newName.LastIndexOf('.');
//            var findSlash = newName.LastIndexOf('/');
//            var findBSlash = newName.LastIndexOf('\\');
//            if (findDot == 0 || findSlash == 0 || findBSlash == 0)
//                return false; //can't start a bundle with a / or .

//            bool result = bundle.HandleRename(newName, 0);

//            //if (findDot > 0 || findSlash > 0 || findBSlash > 0)
//            //{
//            //    bundle.parent.HandleChildRename(originalName.fullNativeName, newName);
//            //}

//            ExecuteAssetMove();

//            var node = FindBundle(originalName);
//            if (node != null)
//            {
//                var message = "Failed to rename bundle named: ";
//                message += originalName.fullNativeName;
//                message += ".  Most likely this is due to the bundle being assigned to a folder in your Assets directory, AND that folder is either empty or only contains assets that are explicitly assigned elsewhere.";
//                Debug.LogError(message);
//            }

//            return result;  
//        }

//        internal static void HandleBundleReparent(IEnumerable<BundleNode> bundles, BundleFolderNode parent)
//        {
//            parent = (parent == null) ? s_RootLevelBundles : parent;
//            foreach (var bundle in bundles)
//            {
//                bundle.HandleReparent(parent.m_Name.bundleName, parent);
//            }
//            ExecuteAssetMove();
//        }

//        internal static void HandleBundleMerge(IEnumerable<BundleNode> bundles, BundleDataNode target)
//        {
//            foreach (var bundle in bundles)
//            {
//                bundle.HandleDelete(true, target.m_Name.bundleName, target.m_Name.variant);
//            }
//            ExecuteAssetMove();
//        }

//        internal static void HandleBundleDelete(IEnumerable<BundleNode> bundles)
//        {
//            var nameList = new List<BundleNameData>();
//            foreach (var bundle in bundles)
//            {
//                nameList.Add(bundle.m_Name);
//                bundle.HandleDelete(true);
//            }
//            ExecuteAssetMove();

//            //check to see if any bundles are still there...
//            foreach(var name in nameList)
//            {
//                var node = FindBundle(name);
//                if(node != null)
//                {
//                    var message = "Failed to delete bundle named: ";
//                    message += name.fullNativeName;
//                    message += ".  Most likely this is due to the bundle being assigned to a folder in your Assets directory, AND that folder is either empty or only contains assets that are explicitly assigned elsewhere.";
//                    Debug.LogError(message);
//                }
//            }
//        }

//        internal static BundleNode FindBundle(BundleNameData name)
//        {
//            BundleNode currNode = s_RootLevelBundles;
//            foreach (var token in name.pathTokens)
//            {
//                if(currNode is BundleFolderNode)
//                {
//                    currNode = (currNode as BundleFolderNode).GetChild(token);
//                    if (currNode == null)
//                        return null;
//                }
//                else
//                {
//                    return null;
//                }
//            }

//            if(currNode is BundleFolderNode)
//            {
//                currNode = (currNode as BundleFolderNode).GetChild(name.shortName);
//                if(currNode is BundleVariantFolderNode)
//                {
//                    currNode = (currNode as BundleVariantFolderNode).GetChild(name.variant);
//                }
//                return currNode;
//            }
//            else
//            {
//                return null;
//            }
//        }

//        internal static BundleNode HandleDedupeBundles(IEnumerable<BundleNode> bundles, bool onlyOverlappedAssets)
//        {
//            var newBundle = CreateEmptyBundle();
//            HashSet<string> dupeAssets = new HashSet<string>();
//            HashSet<string> fullAssetList = new HashSet<string>();

//            //if they were just selected, then they may still be updating.
//            bool doneUpdating = s_BundlesToUpdate.Count == 0;
//            while (!doneUpdating)
//                doneUpdating = Update();

//            foreach (var bundle in bundles)
//            {
//                foreach (var asset in bundle.GetDependencies())
//                {
//                    if (onlyOverlappedAssets)
//                    {
//                        if (!fullAssetList.Add(asset.fullAssetName))
//                            dupeAssets.Add(asset.fullAssetName);
//                    }
//                    else
//                    {
//                        if (asset.IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles))
//                            dupeAssets.Add(asset.fullAssetName);
//                    }
//                }
//            }

//            if (dupeAssets.Count == 0)
//                return null;
            
//            MoveAssetToBundle(dupeAssets, newBundle.m_Name.bundleName, string.Empty);
//            ExecuteAssetMove();
//            return newBundle;
//        }

//        internal static BundleNode HandleConvertToVariant(BundleDataNode bundle)
//        {
//            bundle.HandleDelete(true, bundle.m_Name.bundleName, k_NewVariantBaseName);
//            ExecuteAssetMove();
//            var root = bundle.parent.GetChild(bundle.m_Name.shortName) as BundleVariantFolderNode;

//            if (root != null)
//                return root.GetChild(k_NewVariantBaseName);
//            else
//            {
//                //we got here because the converted bundle was empty.
//                var vfolder = new BundleVariantFolderNode(bundle.m_Name.bundleName, bundle.parent);
//                var vdata = new BundleVariantDataNode(bundle.m_Name.bundleName + "." + k_NewVariantBaseName, vfolder);
//                bundle.parent.AddChild(vfolder);
//                vfolder.AddChild(vdata);
//                return vdata;
//            }
//        }

//        internal class ABMoveData
//        {
//            internal string assetName;
//            internal string bundleName;
//            internal string variantName;
//            internal ABMoveData(string asset, string bundle, string variant)
//            {
//                assetName = asset;
//                bundleName = bundle;
//                variantName = variant;
//            }
//            internal void Apply()
//            {
//                if (!DataSource.IsReadOnly ())
//                {
//                    DataSource.SetAssetBundleNameAndVariant(assetName, bundleName, variantName);
//                }
//            }
//        }
//        internal static void MoveAssetToBundle(AssetNode asset, string bundleName, string variant)
//        {
//            s_MoveData.Add(new ABMoveData(asset.fullAssetName, bundleName, variant));
//        }
//        internal static void MoveAssetToBundle(string assetName, string bundleName, string variant)
//        {
//            s_MoveData.Add(new ABMoveData(assetName, bundleName, variant));
//        }
//        internal static void MoveAssetToBundle(IEnumerable<AssetNode> assets, string bundleName, string variant)
//        {
//            foreach (var asset in assets)
//                MoveAssetToBundle(asset, bundleName, variant);
//        }
//        internal static void MoveAssetToBundle(IEnumerable<string> assetNames, string bundleName, string variant)
//        {
//            foreach (var assetName in assetNames)
//                MoveAssetToBundle(assetName, bundleName, variant);
//        }

//        internal static void ExecuteAssetMove(bool forceAct=true)
//        {
//            var size = s_MoveData.Count;
//            if(forceAct)
//            {
//                if (size > 0)
//                {
//                    bool autoRefresh = EditorPrefs.GetBool("kAutoRefresh");
//                    EditorPrefs.SetBool("kAutoRefresh", false);
//                    EditorUtility.DisplayProgressBar("Moving assets to bundles", "", 0);
//                    for (int i = 0; i < size; i++)
//                    {
//                        EditorUtility.DisplayProgressBar("Moving assets to bundle " + s_MoveData[i].bundleName, System.IO.Path.GetFileNameWithoutExtension(s_MoveData[i].assetName), (float)i / (float)size);
//                        s_MoveData[i].Apply();
//                    }
//                    EditorUtility.ClearProgressBar();
//                    EditorPrefs.SetBool("kAutoRefresh", autoRefresh);
//                    s_MoveData.Clear();
//                }
//                if (!DataSource.IsReadOnly ())
//                {
//                    DataSource.RemoveUnusedAssetBundleNames();
//                }
//                Refresh();
//            }
//        }
        
//        //this version of CreateAsset is only used for dependent assets. 
//        internal static AssetNode CreateAsset(string name, AssetNode parent)
//        {
//            if (ValidateAsset(name))
//            {
//                var bundleName = GetBundleName(name); 
//                return CreateAsset(name, bundleName, parent);
//            }
//            return null;
//        }

//        internal static AssetNode CreateAsset(string name, string bundleName)
//        {
//            if(ValidateAsset(name))
//            {
//                return CreateAsset(name, bundleName, null);
//            }
//            return null;
//        }

//        private static AssetNode CreateAsset(string name, string bundleName, AssetNode parent)
//        {
//            if(!System.String.IsNullOrEmpty(bundleName))
//            {
//                return new AssetNode(name, bundleName);
//            }
//            else
//            {
//                AssetNode info = null;
//                if(!s_GlobalAssetList.TryGetValue(name, out info))
//                {
//                    info = new AssetNode(name, string.Empty);
//                    s_GlobalAssetList.Add(name, info);
//                }
//                //info.AddRefer(parent.displayName);
//                return info;
//            }

//        }

//        internal static bool ValidateAsset(string name)
//        {
//            if (!name.StartsWith("Assets/"))
//                return false;
//            string ext = System.IO.Path.GetExtension(name);
//            if (ext == ".dll" || ext == ".cs" || ext == ".meta" || ext == ".js" || ext == ".boo")
//                return false;

//            return true;
//        }

//        internal static string GetBundleName(string asset)
//        {
//            return DataSource.GetAssetBundleName (asset);
//        }

//        internal static int RegisterAsset(AssetNode asset, string bundle)
//        {
//            if(s_DependencyTracker.ContainsKey(asset.fullAssetName))
//            {
//                s_DependencyTracker[asset.fullAssetName].Add(bundle);
//                int count = s_DependencyTracker[asset.fullAssetName].Count;
//                if (count > 1)
//                    asset.SetMessageFlag(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles, true);
//                return count;
//            }

//            var bundles = new HashSet<string>();
//            bundles.Add(bundle);
//            s_DependencyTracker.Add(asset.fullAssetName, bundles);
//            return 1;            
//        }

//        internal static void UnRegisterAsset(AssetNode asset, string bundle)
//        {
//            if (s_DependencyTracker == null || asset == null)
//                return;

//            if (s_DependencyTracker.ContainsKey(asset.fullAssetName))
//            {
//                s_DependencyTracker[asset.fullAssetName].Remove(bundle);
//                int count = s_DependencyTracker[asset.fullAssetName].Count;
//                switch (count)
//                {
//                    case 0:
//                        s_DependencyTracker.Remove(asset.fullAssetName);
//                        break;
//                    case 1:
//                        asset.SetMessageFlag(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles, false);
//                        break;
//                    default:
//                        break;
//                }
//            }
//        }

//        internal static IEnumerable<string> CheckDependencyTracker(AssetNode asset)
//        {
//            if (s_DependencyTracker.ContainsKey(asset.fullAssetName))
//            {
//                return s_DependencyTracker[asset.fullAssetName];
//            }
//            return new HashSet<string>();
//        }
        
//        //TODO - switch local cache server on and utilize this method to stay up to date.
//        //static List<string> m_importedAssets = new List<string>();
//        //static List<string> m_deletedAssets = new List<string>();
//        //static List<KeyValuePair<string, string>> m_movedAssets = new List<KeyValuePair<string, string>>();
//        //class AssetBundleChangeListener : AssetPostprocessor
//        //{
//        //    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
//        //    {
//        //        m_importedAssets.AddRange(importedAssets);
//        //        m_deletedAssets.AddRange(deletedAssets);
//        //        for (int i = 0; i < movedAssets.Length; i++)
//        //            m_movedAssets.Add(new KeyValuePair<string, string>(movedFromAssetPaths[i], movedAssets[i]));
//        //        //m_dirty = true;
//        //    }
//        //}

//        static internal void LogError(string message)
//        {
//            Debug.LogError("AssetBundleBrowser: " + message);
//        }
//        static internal void LogWarning(string message)
//        {
//            Debug.LogWarning("AssetBundleBrowser: " + message);
//        }

//        static internal Texture2D GetFolderIcon()
//        {
//            if (s_folderIcon == null)
//                FindBundleIcons();
//            return s_folderIcon;
//        }
//        static internal Texture2D GetBundleIcon()
//        {
//            if (s_bundleIcon == null)
//                FindBundleIcons();
//            return s_bundleIcon;
//        }
//        static internal Texture2D GetSceneIcon()
//        {
//            if (s_sceneIcon == null)
//                FindBundleIcons();
//            return s_sceneIcon;
//        }
//        static private void FindBundleIcons()
//        {
//            s_folderIcon = EditorGUIUtility.FindTexture("Folder Icon");

//            var packagePath = System.IO.Path.GetFullPath("Packages/com.unity.assetbundlebuilder");
//            if (System.IO.Directory.Exists(packagePath))
//            {
//                s_bundleIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.unity.assetbundlebuilder/Editor/Icons/ABundleBrowserIconY1756Basic.png", typeof(Texture2D));
//                s_sceneIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.unity.assetbundlebuilder/Editor/Icons/ABundleBrowserIconY1756Scene.png", typeof(Texture2D));
//            }
//        }
//    }
//}
