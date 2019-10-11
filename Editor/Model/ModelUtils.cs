using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetBundleBuilder.Model
{
    internal class ModelUtils
    {
        internal static BundleFolderInfo CreateBundleFolders(BundleFolderInfo parent, BundleNameData nameData)
        {
            BundleFolderInfo folder = parent;
            int size = nameData.pathTokens.Count;
            BundleInfo currInfo = null;
            for (var index = 0; index < size; index++)
            {
                if (folder != null)
                {
                    currInfo = folder.GetChild(nameData.pathTokens[index]);
                    if (currInfo == null)
                    {
                        currInfo = new BundleFolderConcreteInfo(nameData.pathTokens, index + 1, folder);
                        folder.AddChild(currInfo);
                    }
                    
                    folder = currInfo as BundleFolderInfo;
                    if (folder == null)
                    {
                        return null;
                    }
                }
            }
            return currInfo as BundleFolderInfo;
        }

    }
}
