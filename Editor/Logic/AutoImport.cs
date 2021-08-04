using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace AssetBundleBuilder.Model
{
    public class AutoImport
    {
        Setting.Format m_Format;

        public Setting.Format format
        {
            get
            {
                return m_Format;
            }
            set
            {
                m_Format = value;
            }
        }
        //Optimizer m_Optimizer = null;

        public AutoImport()
        {
            //m_Optimizer = new Optimizer();
        }

        protected string GetAssetsDataFile() {
            var dataPath = Path.GetFullPath(".");
            var dataFile = Path.Combine(dataPath, AssetBundleConstans.AssetsDataFile);
            return dataFile;
        }

        public void ImportFile(string filePath)
        {
			//AssetBundleBuilder.BundleInfo assetNode = m_Optimizer.LoadAsset(filePath);
   //         assetNode.SetStandalone(true);
        }

        public void ImportFolder(string folderPath, string pattern = null)
        {
            //m_Optimizer.LoadFromFolder(folderPath, pattern);
        }

        public void GenerateBundles()
        {
            //m_Optimizer.Combine();
            //m_Optimizer.GenerateBundles(m_Format);
        }


        [MenuItem("My/TestTTT")]
        public static void Test()
        {
            //var appPath = Path.GetFullPath(".");

            //Setting.Format format = Setting.Format.WithFolder;

            //Optimizer optimizer = new Optimizer();
            //optimizer.LoadFromFolder(Path.Combine(appPath, "Assets/ArtResources"), ".*\\.prefab");
            //optimizer.Combine();
            //optimizer.SaveAssetsData(Path.Combine(appPath, AssetBundleConstans.AssetsDataFile));
            //optimizer.GenerateBundles(format);
        }
        
    }
}
