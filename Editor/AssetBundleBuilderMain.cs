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

        enum Mode
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

        [SerializeField]
        internal bool multiDataSource = false;
        List<DataSource.DataSource> m_DataSourceList = null;
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if(menu != null)
               menu.AddItem(new GUIContent("Custom Sources"), multiDataSource, FlipDataSource);
        }
        internal void FlipDataSource()
        {
            multiDataSource = !multiDataSource;
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

            InitDataSources();
        } 
        private void InitDataSources()
        {
            //determine if we are "multi source" or not...
            multiDataSource = false;
            m_DataSourceList = new List<DataSource.DataSource>();
            foreach (var info in DataSource.DataSourceProviderUtility.CustomDataSourceTypes)
            {
                m_DataSourceList.AddRange(info.GetMethod("CreateDataSources").Invoke(null, null) as List<DataSource.DataSource>);
            }

            if (m_DataSourceList.Count > 1)
            {
                multiDataSource = true;

                if (!string.IsNullOrEmpty(m_Data.dataSource))
                {
                    for (int i = 0; i < m_DataSourceList.Count; ++i)
                    {
                        if (m_DataSourceList[i].Name.Equals(m_Data.dataSource, System.StringComparison.CurrentCultureIgnoreCase))
                        {
                            m_DataSourceIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    m_DataSourceIndex = 0;
                    m_Data.dataSource = m_DataSourceList[0].Name;
                }

                if (m_DataSourceIndex >= m_DataSourceList.Count)
                    m_DataSourceIndex = 0;
                Model.Model.DataSource = m_DataSourceList[m_DataSourceIndex];
            }
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
            if (multiDataSource)
                padding += k_MenubarPadding * 0.5f;
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
            if(multiDataSource)
            {
                //GUILayout.BeginArea(r);
                GUILayout.BeginHorizontal();

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Bundle Data Source:");
                    GUILayout.FlexibleSpace();
                    var c = new GUIContent(string.Format("{0} ({1})", Model.Model.DataSource.Name, Model.Model.DataSource.ProviderName), "Select Asset Bundle Set");
                    if (GUILayout.Button(c , EditorStyles.toolbarPopup) )
                    {
                        GenericMenu menu = new GenericMenu();

                        for (int index = 0; index < m_DataSourceList.Count; index++)
                        {
                            var ds = m_DataSourceList[index];
                            if (ds == null)
                                continue;

                            if (index > 0)
                                menu.AddSeparator("");
                             
                            menu.AddItem(new GUIContent(string.Format("{0} ({1})", ds.Name, ds.ProviderName)), false,
                                () =>
                                {
                                    var thisDataSource = ds;
                                    Model.Model.DataSource = thisDataSource;
                                    m_Data.dataSource = thisDataSource.Name;
                                    m_ManageTab.ForceReloadData();
                                }
                            );

                        }

                        menu.ShowAsContext();
                    }

                    GUILayout.FlexibleSpace();
                    if (Model.Model.DataSource.IsReadOnly())
                    {
                        GUIStyle tbLabel = new GUIStyle(EditorStyles.toolbar);
                        tbLabel.alignment = TextAnchor.MiddleRight;

                        GUILayout.Label("Read Only", tbLabel);
                    }
                }

                GUILayout.EndHorizontal();
                //GUILayout.EndArea();
            }
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
    }
}
