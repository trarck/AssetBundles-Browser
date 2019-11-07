using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AssetBundleBuilder.Config
{
    //=======================import config===================//
    [Serializable]
    public class ImportInfo
    {
        public string path;
        public string pattern;
    }

    [Serializable]
    public class ImportConfig
    {
        public List<ImportInfo> infos;
        public List<string> formatSelects;
    }

    [Serializable]
    public class ConfigData
    {
        //导入配置
        public ImportConfig importConfig;
        //BundlePath 前缀清除。
        public List<string> bundlePathPrefixClears;
    }

    public class BuilderConfig
    {
        public string dataFile = "AssetDatabase/BuilderConfig.json";

        public ConfigData m_Data;
        public ConfigData data
        {
            get
            {
                if (m_Data == null)
                {
                    Load();
                }
                return m_Data;
            }
        }

        #region Instance
        private static BuilderConfig m_Instance;
        private static readonly object syslock = new object();

        public static BuilderConfig Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (syslock)
                    {
                        if (m_Instance == null)
                        {
                            m_Instance = new BuilderConfig();
                        }
                    }
                }
                return m_Instance;
            }
        }

        public void DestroyInstance()
        {
            m_Instance = null;
        }
        #endregion

        #region Load|Save
        public void Load()
        {
            string fullpath = GetDataFileFullpath();

            if (File.Exists(fullpath))
            {
                string content = File.ReadAllText(fullpath);
                ConfigData configData = JsonUtility.FromJson<ConfigData>(content);
                m_Data = configData;
            }
            else
            {
                m_Data = new ConfigData();
                m_Data.importConfig = new ImportConfig();
                m_Data.importConfig.infos = new List<ImportInfo>();
                m_Data.importConfig.formatSelects = new List<string>();

                m_Data.bundlePathPrefixClears = new List<string>();
            }
        }

        public void Save()
        {
            if (m_Data != null)
            {
                string fullpath = GetDataFileFullpath();
                if (!Directory.Exists(Path.GetDirectoryName(fullpath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullpath));
                }

                var content = JsonUtility.ToJson(m_Data);
                File.WriteAllText(dataFile, content);
            }
        }

        string GetDataFileFullpath()
        {
            if (Path.IsPathRooted(dataFile))
            {
                return dataFile;
            }
            var workDir = Path.GetFullPath(".");
            workDir = workDir.Replace("\\", "/");
            return Path.Combine(workDir, dataFile);
        }
        #endregion
        public void InsertBundlePathPrefixClear(string prefix,int index)
        {
            if (index < 0)
            {
                index += data.bundlePathPrefixClears.Count;
            }
            if (index < 0)
            {
                return;
            }
            if(index>= data.bundlePathPrefixClears.Count)
            {
                data.bundlePathPrefixClears.Add(prefix);
            }

            data.bundlePathPrefixClears.Insert(index, prefix);
        }

        public void InsertBundlePathPrefixClear(string prefix, string beforeItemSearch)
        {
            for(int i=0;i< data.bundlePathPrefixClears.Count; ++i)
            {
                if (data.bundlePathPrefixClears[i].Contains(beforeItemSearch))
                {
                    data.bundlePathPrefixClears.Insert(i, prefix);
                    return;
                }
            }
            //添加到最后
            data.bundlePathPrefixClears.Add( prefix);
        }

        public void AddBundlePathPrefixClear(string prefix, bool first = true)
        {
            if (first)
            {
                data.bundlePathPrefixClears.Insert(0, prefix);
            }
            else
            {
                data.bundlePathPrefixClears.Add(prefix);
            }
        }
    }
}
