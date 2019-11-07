using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using AssetBundleBuilder.DataSource;
using UnityEditorInternal;

namespace AssetBundleBuilder
{
    [System.Serializable]
    internal class AssetBundleConfigTab
    {
        [SerializeField]
        private Vector2 m_ScrollPosition;

        [SerializeField]
        private ConfigTabData m_ConfigData;

        //文件夹前缀忽略列表
        ReorderableList m_IgnoreFolderPrefixList;

        internal AssetBundleConfigTab()
        {
            m_ConfigData = new ConfigTabData();
        }

        internal void OnDisable()
        {
            SaveData();
        }

        internal void OnEnable(Rect pos, EditorWindow parent)
        {
            //LoadData...
            LoadData();

            CreateObjects();
        }

        protected void CreateObjects()
        {
            CreateIgnoreFolderPrefixList();
        }

        void CreateIgnoreFolderPrefixList()
        {
            m_IgnoreFolderPrefixList = new ReorderableList(Config.BuilderConfig.Instance.data.bundlePathPrefixClears, typeof(string), true, true, true, true);
            m_IgnoreFolderPrefixList.drawHeaderCallback += (Rect rect) =>
            {
                GUI.Label(rect, "IgnoreFolderPrefixs");
            };

            m_IgnoreFolderPrefixList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Config.BuilderConfig.Instance.data.bundlePathPrefixClears[index]=GUI.TextField(rect, Config.BuilderConfig.Instance.data.bundlePathPrefixClears[index]);
            };

            m_IgnoreFolderPrefixList.onRemoveCallback += (ReorderableList list) =>
            {
                list.list.RemoveAt(list.index);
                SaveData();
            };

            m_IgnoreFolderPrefixList.onAddCallback += (ReorderableList list) =>
            {
                if (list.index >= 0)
                {
                    list.list.Insert(list.index, "");
                }
                else
                {
                    list.list.Add("");
                }
                SaveData();
            };
        }

        internal void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            m_IgnoreFolderPrefixList.DoLayoutList();

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Save"))
            {
                SaveData();
            }
        }


        private void BrowseForFolder()
        {
            //var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", m_UserData.m_OutputPath, string.Empty);
            //if (!string.IsNullOrEmpty(newPath))
            //{
            //    var gamePath = System.IO.Path.GetFullPath(".");
            //    gamePath = gamePath.Replace("\\", "/");
            //    if (newPath.StartsWith(gamePath) && newPath.Length > gamePath.Length)
            //        newPath = newPath.Remove(0, gamePath.Length+1);
            //    m_UserData.m_OutputPath = newPath;
            //    //EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_OutputPath);
            //}
        }

        void LoadData()
        {
            
        }

        void SaveData()
        {
            Config.BuilderConfig.Instance.Save();
        }
       
        [System.Serializable]
        internal class ConfigTabData
        {
            public string[] ignoreFolderPrefixs;
        }
    }
}