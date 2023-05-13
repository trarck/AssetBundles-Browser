using System;
using UnityEditor;

namespace AssetBundleBuilder
{
    /// <summary>
    /// Build Info struct used by ABDataSource to pass needed build data around.
    /// </summary>
    public partial class BuildInfo
    {
        /// <summary>
        /// Directory to place build result
        /// </summary>
        public string outputDirectory
        {
            get { return m_outputDirectory; }
            set { m_outputDirectory = value; }
        }
        private string m_outputDirectory;
        /// <summary>
        /// Standard asset bundle build options.
        /// </summary>
        public BuildAssetBundleOptions options
        {
            get { return m_options; }
            set { m_options = value; }
        }
        private BuildAssetBundleOptions m_options;
        /// <summary>
        /// Target platform for build.
        /// </summary>
        public BuildTarget buildTarget
        {
            get { return m_buildTarget; }
            set { m_buildTarget = value; }
        }
        private BuildTarget m_buildTarget;

        public BuildTargetGroup buildTargetGroup
        {
            get
            {
                return m_buildTargetGroup;
            }
            set
            {
                m_buildTargetGroup = value;
            }
        }
        private BuildTargetGroup m_buildTargetGroup;
        /// <summary>
        /// Callback for build event.
        /// </summary>
        public Action<string> onBuild
        {
            get { return m_onBuild; }
            set { m_onBuild = value; }
        }
        private Action<string> m_onBuild;
        //资源的版本号
        public string version;

        //manifest 文件名
        public string manifestName = "all.manifest";
        //assetbundle扩展名
        public string assetBundleExt = ".ab";
        //是否保存所有依赖
        public bool bundleDependenciesAll;
    }
}
