using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace AssetBundleBuilder.Model
{
    public class AssetNode
    {
		private string m_AssetName;
        private string m_DisplayName;
        private string m_BundleName;
		private long m_FileSize = -1;

		private HashSet<AssetNode> m_Refers;
		HashSet<AssetNode> m_Dependencies = null;
		HashSet<AssetNode> m_AllDependencies = null;

		private MessageSystem.MessageState m_AssetMessages = new MessageSystem.MessageState();
		internal bool isScene
		{
			get; set;
		}
		internal bool isFolder
		{
			get; set;
		}
		internal long fileSize
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

        internal string fullAssetName
        {
            get { return m_AssetName; }
            set
            {
                m_AssetName = value;
                m_DisplayName = System.IO.Path.GetFileNameWithoutExtension(m_AssetName);
            }
        }
        internal string displayName
        {
            get { return m_DisplayName; }
        }
        internal string bundleName
        { get { return System.String.IsNullOrEmpty(m_BundleName) ? "auto" : m_BundleName; } }
        
		internal HashSet<AssetNode> dependencies
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

		internal HashSet<AssetNode> allDependencies
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

		internal AssetNode(string inName, string bundleName = "")
		{
			fullAssetName = inName;
			m_BundleName = bundleName;
			m_Refers = new HashSet<AssetNode>();
			isScene = false;
			isFolder = false;
		}

		#region Message
		internal bool IsMessageSet(MessageSystem.MessageFlag flag)
        {
            return m_AssetMessages.IsSet(flag);
        }
        internal void SetMessageFlag(MessageSystem.MessageFlag flag, bool on)
        {
            m_AssetMessages.SetFlag(flag, on);
        }
        internal MessageType HighestMessageLevel()
        {
            return m_AssetMessages.HighestMessageLevel();
        }
        internal IEnumerable<MessageSystem.Message> GetMessages()
        {
            List<MessageSystem.Message> messages = new List<MessageSystem.Message>();
            if(IsMessageSet(MessageSystem.MessageFlag.SceneBundleConflict))
            {
                var message = displayName + "\n";
                if (isScene)
                    message += "Is a scene that is in a bundle with non-scene assets. Scene bundles must have only one or more scene assets.";
                else
                    message += "Is included in a bundle with a scene. Scene bundles must have only one or more scene assets.";
                messages.Add(new MessageSystem.Message(message, MessageType.Error));
            }
            if(IsMessageSet(MessageSystem.MessageFlag.DependencySceneConflict))
            {
                var message = displayName + "\n";
                message += MessageSystem.GetMessage(MessageSystem.MessageFlag.DependencySceneConflict).message;
                messages.Add(new MessageSystem.Message(message, MessageType.Error));
            }
            if (IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles))
            {
                var bundleNames = Model.CheckDependencyTracker(this);
                string message = displayName + "\n" + "Is auto-included in multiple bundles:\n";
                foreach(var bundleName in bundleNames)
                {
                    message += bundleName + ", ";
                }
                message = message.Substring(0, message.Length - 2);//remove trailing comma.
                messages.Add(new MessageSystem.Message(message, MessageType.Warning));
            }

            if (System.String.IsNullOrEmpty(m_BundleName) && m_Refers.Count > 0)
            {
                //TODO - refine the parent list to only include those in the current asset list
                var message = displayName + "\n" + "Is auto included in bundle(s) due to parent(s): \n";
                foreach (var parent in m_Refers)
                {
                    message += parent + ", ";
                }
                message = message.Substring(0, message.Length - 2);//remove trailing comma.
                messages.Add(new MessageSystem.Message(message, MessageType.Info));
            }            

            if (m_Dependencies != null && m_Dependencies.Count > 0)
            {
                var message = string.Empty;
                var sortedDependencies = m_Dependencies.OrderBy(d => d.bundleName);
                foreach (var dependent in sortedDependencies)
                {
                    if (dependent.bundleName != bundleName)
                    {
                        message += dependent.bundleName + " : " + dependent.displayName + "\n";
                    }
                }
                if (string.IsNullOrEmpty(message) == false)
                {
                    message = message.Insert(0, displayName + "\n" + "Is dependent on other bundle's asset(s) or auto included asset(s): \n");
                    message = message.Substring(0, message.Length - 1);//remove trailing line break.
                    messages.Add(new MessageSystem.Message(message, MessageType.Info));
                }
            }            

            messages.Add(new MessageSystem.Message(displayName + "\n" + "Path: " + fullAssetName, MessageType.Info));

            return messages;
        }

		#endregion

		internal void AddRefer(AssetNode referInfo)
		{
			m_Refers.Add(referInfo);
			referInfo.dependencies.Add(this);
		}

		internal void RemoveRefer(AssetNode referInfo)
		{
			m_Refers.Remove(referInfo);
			referInfo.dependencies.Remove(this);
		}

        internal HashSet<AssetNode> RefreshDependencies()
        {
            //TODO - not sure this refreshes enough. need to build tests around that.
            if (m_Dependencies == null)
            {
				m_Dependencies = new HashSet<AssetNode>();
                if (AssetDatabase.IsValidFolder(m_AssetName))
                {
                    //if we have a folder, its dependencies were already pulled in through alternate means.  no need to GatherFoldersAndFiles
                    //GatherFoldersAndFiles();
                }
                else
                {
					
				}
            }
            return m_Dependencies;
        }

		internal string GetSizeString()
		{
			if (fileSize == 0)
				return "--";
			return EditorUtility.FormatBytes(fileSize);
		}

	}

}
