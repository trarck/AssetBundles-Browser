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
        private ConfigTabData m_UserData;

        ReorderableList m_IgnoreFolderPrefixList;

        internal AssetBundleConfigTab()
        {
            m_UserData = new ConfigTabData();
        }

        internal void OnDisable()
        {
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += AssetBundleConstans.ConfigTabSetting;

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);

            bf.Serialize(file, m_UserData);
            file.Close();

        }
        internal void OnEnable(Rect pos, EditorWindow parent)
        {
            //LoadData...
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += AssetBundleConstans.ConfigTabSetting;

            if (File.Exists(dataPath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(dataPath, FileMode.Open);
                var data = bf.Deserialize(file) as ConfigTabData;
                if (data != null)
                    m_UserData = data;
                file.Close();
            }

            CreateObjects();
        }

        protected void CreateObjects()
        {
            CreateIgnoreFolderPrefixList();
        }

        void CreateIgnoreFolderPrefixList()
        {
            m_IgnoreFolderPrefixList = new ReorderableList(Model.Import.IgnoreFolderPrefixs, typeof(string), true, true, true, true);
            m_IgnoreFolderPrefixList.drawHeaderCallback += (Rect rect) =>
            {
                GUI.Label(rect, "IgnoreFolderPrefixs");
            };

            m_IgnoreFolderPrefixList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Model.Import.IgnoreFolderPrefixs[index]=GUI.TextField(rect, Model.Import.IgnoreFolderPrefixs[index]);
            };

            m_IgnoreFolderPrefixList.onRemoveCallback += (ReorderableList list) =>
            {
                list.list.RemoveAt(list.index);
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
            };
        }

        internal void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            m_IgnoreFolderPrefixList.DoLayoutList();

            EditorGUILayout.EndScrollView();
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
       
        [System.Serializable]
        internal class ConfigTabData
        {

        }
    }
}