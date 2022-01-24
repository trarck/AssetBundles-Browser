using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{

    public class AssetBundleBuilderMain : EditorWindow, IHasCustomMenu, ISerializationCallbackReceiver
    {

        private static AssetBundleBuilderMain s_instance = null;
        internal static AssetBundleBuilderMain instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = GetWindow<AssetBundleBuilderMain>();
                return s_instance;
            }
        }

        internal const float kButtonWidth = 150;

        public enum Mode
        {
            Config,
            Browser,
            Builder,
            Inspect,
        }
        [SerializeField]
        Mode m_Mode;

        [SerializeField]
        int m_DataSourceIndex;

        [SerializeField]
        internal AssetBundleConfigTab m_ConfigTab;

        [SerializeField]
        internal AssetBundleManageTab m_ManageTab;

        [SerializeField]
        internal AssetBundleBuildTab m_BuildTab;

        [SerializeField]
        internal AssetBundleInspectTab m_InspectTab;

        private Texture2D m_RefreshTexture;
        private Texture2D m_ToolsTexture;

        const float k_ToolbarPadding = 15;
        const float k_MenubarPadding = 32;

        MainData m_Data;

        [MenuItem("Window/AssetBundle Builder", priority = 2050)]
        static void ShowWindow()
        {
            s_instance = null;
            instance.titleContent = new GUIContent("AssetBundles");
            instance.Show();
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {

        }

        private void OnEnable()
        {
            LoadData();

            Rect subPos = GetSubWindowArea();

            if (m_ConfigTab == null)
            {
                m_ConfigTab = new AssetBundleConfigTab();
            }
            m_ConfigTab.OnEnable(subPos, this);

            if(m_ManageTab == null)
                m_ManageTab = new AssetBundleManageTab();
            m_ManageTab.OnEnable(subPos, this);

            if(m_BuildTab == null)
                m_BuildTab = new AssetBundleBuildTab();
            m_BuildTab.OnEnable(this);

            if (m_InspectTab == null)
                m_InspectTab = new AssetBundleInspectTab();
            m_InspectTab.OnEnable(subPos);

            m_RefreshTexture = EditorGUIUtility.FindTexture("Refresh");
            m_ToolsTexture = Resources.Load("Icons/tools")as Texture2D;
        } 
     
        private void OnDisable()
        {
            SaveData();

            if (m_BuildTab != null)
                m_BuildTab.OnDisable();
            if (m_InspectTab != null)
                m_InspectTab.OnDisable();
        }

        public void OnBeforeSerialize()
        {
        }
        public void OnAfterDeserialize()
        {
        }

        private Rect GetSubWindowArea()
        {
            float padding = k_MenubarPadding;
            Rect subPos = new Rect(0, padding, position.width, position.height - padding);
            return subPos;
        }

        private void Update()
        {
            switch (m_Mode)
            {
                case Mode.Config:
                    break;
                case Mode.Builder:
                    break;
                case Mode.Inspect:
                    break;
                case Mode.Browser:
                default:
                    m_ManageTab.Update();
                    break;
            }
        }

        private void OnGUI()
        {
            ModeToggle();

            switch(m_Mode)
            {
                case Mode.Config:
                    m_ConfigTab.OnGUI();
                    break;
                case Mode.Builder:
                    m_BuildTab.OnGUI();
                    break;
                case Mode.Inspect:
                    m_InspectTab.OnGUI(GetSubWindowArea());
                    break;
                case Mode.Browser:
                default:
                    m_ManageTab.OnGUI(GetSubWindowArea());
                    break;
            }
        }

        void ModeToggle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(k_ToolbarPadding);
            bool clicked = false;
            switch(m_Mode)
            {
                case Mode.Config:
                    GUILayout.Space(m_RefreshTexture.width + k_ToolbarPadding);
                    break;
                case Mode.Browser:
                    if (GUILayout.Button("Auto",GUILayout.Height(22)))
                    {
                        m_ManageTab.ShowAutoImportWindow();
                    }

                    clicked = GUILayout.Button(m_ToolsTexture,GUILayout.Width(22), GUILayout.Height(22));
                    if (clicked)
                        m_ManageTab.ShowToolsMenu();

                    clicked = GUILayout.Button(m_RefreshTexture);
                    if (clicked)
                        m_ManageTab.ForceReloadData();
                    break;
                case Mode.Builder:
                    GUILayout.Space(m_RefreshTexture.width + k_ToolbarPadding);
                    break;
                case Mode.Inspect:
                    clicked = GUILayout.Button(m_RefreshTexture);
                    if (clicked)
                        m_InspectTab.RefreshBundles();
                    break;
            }

            float toolbarWidth = position.width - k_ToolbarPadding * 4 - m_RefreshTexture.width;
            //string[] labels = new string[2] { "Configure", "Build"};
            string[] labels = new string[] {"Config", "Browse", "Build", "Inspect" };
            m_Mode = (Mode)GUILayout.Toolbar((int)m_Mode, labels, "LargeButton", GUILayout.Width(toolbarWidth) );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void SaveData()
        {
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/" + AssetBundleConstans.MainSetting;

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);

            bf.Serialize(file, m_Data);
            file.Close();
        }

        void LoadData()
        {
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/" + AssetBundleConstans.MainSetting;

            if (File.Exists(dataPath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(dataPath, FileMode.Open);
                var data = bf.Deserialize(file) as MainData;
                if (data != null)
                    m_Data = data;
                file.Close();
            }
            if (m_Data == null)
            {
                m_Data = new MainData();
            }
        }

        [System.Serializable]
        public class MainData
        {
            public string dataSource;
        }

        public Mode mode
        {
            get
            {
                return m_Mode;
            }
        }
    }
}
