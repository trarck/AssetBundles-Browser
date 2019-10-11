using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetBundleBuilder.Config
{
    public class Ruler
    {

    }

    [Serializable]
    public class FolderRuler
    {
        public string path;

        protected List<int> m_Actions;
    }
}
