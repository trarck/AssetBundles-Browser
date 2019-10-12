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

        internal static string Relative(string fromPath, string toPath)
        {
            fromPath = fromPath.Replace("\\", "/");
            toPath = toPath.Replace("\\", "/");

            if (fromPath[fromPath.Length - 1] == '/')
            {
                fromPath = fromPath.Substring(0, fromPath.Length - 1);
            }

            if (toPath[toPath.Length - 1] == '/')
            {
                toPath = toPath.Substring(0, toPath.Length - 1);
            }

            string[] froms = fromPath.Split('/');
            string[] tos = toPath.Split('/');

            int i = 0;
            //look for same part
            for (; i < froms.Length; ++i)
            {
                if (froms[i] != tos[i])
                {
                    break;
                }
            }

            if (i == 0)
            {
                //just windows. eg.fromPath=c:\a\b\c,toPath=d:\e\f\g
                //if linux the first is empty always same. eg. fromPath=/a/b/c,toPath=/d/e/f
                return toPath;
            }
            else
            {
                System.Text.StringBuilder result = new System.Text.StringBuilder();

                for (int j = i; j < froms.Length; ++j)
                {
                    result.Append("../");
                }

                for (int j = i; j < tos.Length; ++j)
                {
                    result.Append(tos[j]);
                    if (j < tos.Length - 1)
                    {
                        result.Append("/");
                    }
                }
                return result.ToString();
            }
        }

    }
}
