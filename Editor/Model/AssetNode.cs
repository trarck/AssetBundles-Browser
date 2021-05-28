using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace AssetBundleBuilder
{
    public class AssetNode
    {
		private string m_AssetName;
        private string m_DisplayName;
		private long m_FileSize = -1;
		private string m_RealFilePath = null;

		private HashSet<AssetNode> m_Refers;
        private HashSet<AssetNode> m_Dependencies = null;
        private HashSet<AssetNode> m_AllDependencies = null;

		public bool isScene
		{
			get; set;
		}
        public long fileSize
		{
			get
			{
				if (m_FileSize == -1)
				{
					System.IO.FileInfo fileInfo = new System.IO.FileInfo(m_AssetName);
					if (fileInfo.Exists)
					{
						m_FileSize = fileInfo.Length;
					}
					else
					{
						m_FileSize = 0;
					}
				}
				return m_FileSize;
			}
		}

        public string fullAssetName
        {
            get { return m_AssetName; }
            set
            {
                m_AssetName = value;
                m_DisplayName = System.IO.Path.GetFileNameWithoutExtension(m_AssetName);
            }
        }
        public string displayName
        {
            get { return m_DisplayName; }
        }

		public HashSet<AssetNode> refers
		{
			get
			{
				if (m_Refers == null)
				{
					m_Refers = new HashSet<AssetNode>();
				}
				return m_Refers;
			}
			set
			{
				m_Refers = value;
			}
		}

		public HashSet<AssetNode> dependencies
		{
			get
			{
				if (m_Dependencies == null)
				{
					m_Dependencies = new HashSet<AssetNode>();
				}
				return m_Dependencies;
			}
			set
			{
				m_Dependencies = value;
			}
		}

		public HashSet<AssetNode> allDependencies
		{
			get
			{
				return m_AllDependencies;
			}
			set
			{
				m_AllDependencies = value;
			}
		}

		public AssetNode(string assetName)
		{
			fullAssetName = assetName;
			m_Refers = new HashSet<AssetNode>();
			isScene = false;
		}

		public AssetNode(string assetName, string filePath)
		{
			fullAssetName = assetName;
			m_RealFilePath = filePath;
			m_Refers = new HashSet<AssetNode>();
			isScene = false;
		}

		public void AddRefer(AssetNode referNode)
		{
			AddReferNode(referNode);
			referNode.AddDependencyNode(this);
		}

		public void RemoveRefer(AssetNode referNode)
		{
			RemoveReferNode(referNode);
			referNode.RemoveDependencyNode(this);
		}

		public void AddDependency(AssetNode depNode)
		{
			AddDependencyNode(depNode);
			depNode.AddReferNode(this);
		}

		public void RemoveDependency(AssetNode depNode)
		{
			RemoveDependencyNode(depNode);
			depNode.RemoveReferNode(this);
		}

		public void AddReferNode(AssetNode assetNode)
		{
			refers.Add(assetNode);
		}

		public void RemoveReferNode(AssetNode assetNode)
		{
			refers.Remove(assetNode);
		}

		public void AddDependencyNode(AssetNode assetNode)
		{
			dependencies.Add(assetNode);
		}

		public void RemoveDependencyNode(AssetNode assetNode)
		{
			dependencies.Remove(assetNode);
		}

		public string GetSizeString()
		{
			if (fileSize == 0)
				return "--";
			return EditorUtility.FormatBytes(fileSize);
		}
	}
}
