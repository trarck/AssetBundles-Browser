using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using AssetBundleBuilder.DataSource;
using UnityEditorInternal;

namespace AssetBundleBuilder
{
    using Config;

    public class AutoImportWindow : EditorWindow
    {
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

            float i = 0;
            foreach (var importInfo in m_ImportInfos)
            {
                EditorUtility.DisplayProgressBar("AutoImport", "import "+importInfo.path, ++i/m_ImportInfos.Count);
                if (Directory.Exists(importInfo.path))
                {
                    autoImport.ImportFolder(importInfo.path, importInfo.pattern);
                }
                else if (File.Exists(importInfo.path))
                {
                    autoImport.ImportFile(importInfo.path);
                }
            }          

            EditorUtility.DisplayProgressBar("AutoImport", "GenerateBundles ",0);
            autoImport.GenerateBundles();
            
            EditorUtility.ClearProgressBar();

            AssetBundleBuilderMain.instance.m_ManageTab.ForceReloadData();
        }

        void LoadData()
        {

            m_ImportInfos = BuilderConfig.Instance.data.importConfig.infos;
            m_FormatGUI.SetSelects(BuilderConfig.Instance.data.importConfig.formatSelects);
        }

        void SaveData()
        {
            BuilderConfig.Instance.data.importConfig.infos = m_ImportInfos;
            BuilderConfig.Instance.data.importConfig.formatSelects= m_FormatGUI.GetSelects();
            BuilderConfig.Instance.Save();
        }
    }
}