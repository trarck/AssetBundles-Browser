//using System.Collections.Generic;
//using System.IO;
//using UnityEngine;
//using UnityEditor;

//namespace AssetBundleBuilder.Model
//{
//    public class BatchImport
//    {
//        Setting.Format m_Format;

//        public Setting.Format format
//        {
//            get
//            {
//                return m_Format;
//            }
//            set
//            {
//                m_Format = value;
//            }
//        }
//        //Optimizer m_Optimizer = null;

//        public BatchImport()
//        {
//            //m_Optimizer = new Optimizer();
//        }

//        protected string GetAssetsDataFile() {
//            var dataPath = Path.GetFullPath(".");
//            var dataFile = Path.Combine(dataPath, AssetBundleConstans.AssetsDataFile);
//            return dataFile;
//        }

//        public BundleInfo ImportFile(string filePath)
//        {
//            if (Path.IsPathRooted(filePath))
//            {
//                filePath = FileSystem.Relative(Path.GetDirectoryName(Application.dataPath), filePath);
//            }

//            if (!EditorAssetBundleManager.ValidateAsset(filePath))
//            {
//                return null;
//            }

//            string bundleName = EditorAssetBundleManager.Instance.CreateBundleName(filePath, format);
//            BundleInfo bundleInfo = EditorAssetBundleManager.Instance.GetOrCreateBundle(bundleName);
//            EditorAssetBundleManager.Instance.AddAssetToBundle(bundleInfo, filePath);
//            return bundleInfo;
//        }

//        public List<BundleInfo> LoadFromFolder(string folderPath, string pattern = null)
//        {
//            DirectoryInfo startInfo = new DirectoryInfo(folderPath);
//            if (!startInfo.Exists)
//            {
//                return null;
//            }

//            List<BundleInfo> bundleInfos = new List<BundleInfo>();

//            Stack<DirectoryInfo> dirs = new Stack<DirectoryInfo>();
//            dirs.Push(startInfo);

//            DirectoryInfo dir;

//            bool haveFilter = false;
//            System.Text.RegularExpressions.Regex reg = null;
//            if (!string.IsNullOrEmpty(pattern))
//            {
//                haveFilter = true;
//                reg = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
//            }

//            while (dirs.Count > 0)
//            {
//                dir = dirs.Pop();

//                foreach (FileInfo fi in dir.GetFiles())
//                {
//                    if (!haveFilter || reg.IsMatch(fi.FullName))
//                    {
//                        BundleInfo bundleInfo = ImportFile(fi.FullName);
//                        if (bundleInfo != null)
//                        {
//                            bundleInfo.SetStandalone(true);
//                            bundleInfos.Add(bundleInfo);
//                        }
//                    }
//                }

//                foreach (DirectoryInfo subDir in dir.GetDirectories())
//                {
//                    if (!subDir.Name.StartsWith("."))
//                    {
//                        dirs.Push(subDir);
//                    }
//                }
//            }

//            return bundleInfos;
//        }

//        public void ImportFolder(string folderPath, string pattern = null)
//        {
//            //m_Optimizer.LoadFromFolder(folderPath, pattern);
//        }

//        public void GenerateBundles()
//        {
//            //m_Optimizer.Combine();
//            //m_Optimizer.GenerateBundles(m_Format);
//        }


//        [MenuItem("My/TestTTT")]
//        public static void Test()
//        {
//            //var appPath = Path.GetFullPath(".");

//            //Setting.Format format = Setting.Format.WithFolder;

//            //Optimizer optimizer = new Optimizer();
//            //optimizer.LoadFromFolder(Path.Combine(appPath, "Assets/ArtResources"), ".*\\.prefab");
//            //optimizer.Combine();
//            //optimizer.SaveAssetsData(Path.Combine(appPath, AssetBundleConstans.AssetsDataFile));
//            //optimizer.GenerateBundles(format);
//        }
        
//    }
//}
