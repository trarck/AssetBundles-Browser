using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;

namespace AssetBundleBuilder
{
    using Config;

    public class AutoImportWindow : EditorWindow
    {
        List<ImportInfo> m_ImportInfos=new List<ImportInfo>();

        ReorderableList m_AssetFolderList;

        View.EnumGUI<Setting.Format> m_FormatGUI;

        public static AutoImportWindow ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            return GetWindow<AutoImportWindow>(false, "Auto Import");
        }

        private void OnEnable()
        {
            m_FormatGUI = new View.EnumGUI<Setting.Format>();
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

            Setting.Format format = Setting.Format.None;

            List<Setting.Format> formats = m_FormatGUI.GetValue();
            foreach (var f in formats)
            {
                format |= f;
            }


            float i = 0;
            foreach (var importInfo in m_ImportInfos)
            {
                EditorUtility.DisplayProgressBar("AutoImport", "import "+importInfo.path, ++i/m_ImportInfos.Count);
                if (Directory.Exists(importInfo.path))
                {
                    EditorAssetBundleManager.Instance.ImportBundlesFromFolder(importInfo.path, importInfo.pattern,format);
                }
                else if (File.Exists(importInfo.path))
                {
                    EditorAssetBundleManager.Instance.ImportBundleFromFile(importInfo.path,format);
                }
            }

            EditorUtility.ClearProgressBar();

            EditorAssetBundleManager.Instance.Save();

            if (AssetBundleBuilderMain.instance.mode == AssetBundleBuilderMain.Mode.Browser)
            {
                AssetBundleBuilderMain.instance.m_ManageTab.ForceReloadData();
            }
        }

        void LoadData()
        {
            BuilderConfig.Instance.Load();
           m_ImportInfos = BuilderConfig.Instance.data.importConfig.infos;
            m_FormatGUI.SetSelects(BuilderConfig.Instance.data.importConfig.formats);
        }

        void SaveData()
        {
            BuilderConfig.Instance.data.importConfig.infos = m_ImportInfos;
            BuilderConfig.Instance.data.importConfig.formats= m_FormatGUI.GetSelects();
            BuilderConfig.Instance.Save();
        }
    }
}