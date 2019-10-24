using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using AssetBundleBuilder.DataSource;
using UnityEditorInternal;

namespace AssetBundleBuilder
{
    public class AutoImportWindow : EditorWindow
    {
        [System.Serializable]
        public class ImportInfo
        {
            public string path;
            public string pattern;
        }

        [System.Serializable]
        public class ImportData
        {
            public List<ImportInfo> infos;
            public List<string> formatSelects;
        }

        List<ImportInfo> m_ImportInfos=new List<ImportInfo>();

        ReorderableList m_AssetFolderList;

        View.EnumGUI<Model.Setting.Format> m_FormatGUI;

        public static AutoImportWindow ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            return EditorWindow.GetWindow<AutoImportWindow>(false, "Auto Import");
        }

        private void OnEnable()
        {
            m_FormatGUI = new View.EnumGUI<Model.Setting.Format>();
            m_FormatGUI.Init("Format", true, true);

            LoadData();

            CreateAssetFolderList();
        }

        private void OnDisable()
        {
            SaveData();
        }

        void CreateAssetFolderList()
        {
            m_AssetFolderList = new ReorderableList(m_ImportInfos, typeof(string), true, true, true, true);
            m_AssetFolderList.drawHeaderCallback += (Rect rect) =>
            {
                GUI.Label(rect, "Assets");
            };

            m_AssetFolderList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Rect r = rect;
                r.width -= 300;
                m_ImportInfos[index].path = GUI.TextField(r, m_ImportInfos[index].path);
                r.x += r.width;
                r.width = 160;
                m_ImportInfos[index].pattern = GUI.TextField(r, m_ImportInfos[index].pattern);
                r.x += r.width;
                r.width = 80;
                if (GUI.Button(r, "Browser"))
                {
                    m_ImportInfos[index].path = EditorUtility.OpenFolderPanel("Choose Folder",
                        string.IsNullOrEmpty(m_ImportInfos[index].path)?Application.dataPath: m_ImportInfos[index].path, 
                        string.Empty);
                }
            };

            m_AssetFolderList.onRemoveCallback += (ReorderableList list) =>
            {
                list.list.RemoveAt(list.index);
            };

            m_AssetFolderList.onAddCallback += (ReorderableList list) =>
            {
                ImportInfo importInfo = new ImportInfo();

                if (list.index >= 0)
                {
                    list.list.Insert(list.index, importInfo);
                }
                else
                {
                    list.list.Add(importInfo);
                }
            };
        }

        private void OnGUI()
        {
            if (m_AssetFolderList!=null)
            {
                m_AssetFolderList.DoLayoutList();
            }

            if (m_FormatGUI != null)
            {
                m_FormatGUI.OnGUI();
            }

            if (GUILayout.Button("Import"))
            {
                DoImport();
            }
        }

        void DoImport()
        {
            SaveData();

            Model.Setting.Format format = Model.Setting.Format.None;

            List<Model.Setting.Format> formats = m_FormatGUI.GetValue();
            foreach (var f in formats)
            {
                format |= f;
            }

            Model.AutoImport autoImport = new Model.AutoImport();
            autoImport.format = format;

            foreach (var importInfo in m_ImportInfos)
            {
                if (Directory.Exists(importInfo.path))
                {
                    autoImport.ImportFolder(importInfo.path, importInfo.pattern);
                }
                else if (File.Exists(importInfo.path))
                {
                    autoImport.ImportFile(importInfo.path);
                }
            }

            autoImport.GenerateBundles();

            

            AssetBundleBuilderMain.instance.m_ManageTab.ForceReloadData();
        }

        void LoadData()
        {
            var dataPath = Path.GetFullPath(".");
            var dataFile = Path.Combine(dataPath, AssetBundleConstans.ImportDataFile);

            if (File.Exists(dataFile))
            {
                string content = File.ReadAllText(dataFile);
                ImportData importData = JsonUtility.FromJson<ImportData>(content);

                m_ImportInfos = importData.infos != null ? importData.infos : new List<ImportInfo>();
                m_FormatGUI.SetSelects(importData.formatSelects);
            }
        }

        void SaveData()
        {
            ImportData importData = new ImportData();
            importData.infos = m_ImportInfos;
            importData.formatSelects = m_FormatGUI.GetSelects();

            var dataPath = Path.GetFullPath(".");
            var dataFile = Path.Combine(dataPath, AssetBundleConstans.ImportDataFile);
            var content = JsonUtility.ToJson(importData);
            File.WriteAllText(dataFile, content);
        }
    }
}