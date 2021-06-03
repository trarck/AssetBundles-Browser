using System.IO;
using System.Text;

namespace AssetBundleBuilder.Model
{
    internal class ModelUtils
    {
        public static string assetPathPrev = "Assets/";

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

        /// <summary>
        /// 归一化路径
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string NormalizeFilePath(string filename)
        {
            return filename.Replace("\\", "/");
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

        /// <summary>
        /// 移除路径中的Assets
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string RemoveAssetPrev(string path)
        {
            if (!string.IsNullOrEmpty(path) && path.StartsWith(assetPathPrev, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return path.Substring(assetPathPrev.Length);
            }
            return path;
        }

        /// <summary>
        /// 添加Assets到路径。如果存在则不添加。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string AddAssetPrev(string path)
        {
            if (!string.IsNullOrEmpty(path) && !path.StartsWith(assetPathPrev, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return Combine(assetPathPrev, path);
            }
            return path;
        }

        /// <summary>
        /// 合并路径
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static string Combine(params string[] paths)
        {
            if (paths.Length == 0)
                return string.Empty;
            if (paths.Length == 1)
                return paths[0];

            StringBuilder sb = new StringBuilder();

            string c = null, n = null;

            int start = 0;
            for (; start < paths.Length; ++start)
            {
                c = paths[start];
                if (!string.IsNullOrEmpty(c))
                {
                    sb.Append(c);
                    break;
                }
            }

            for (int i = start + 1; i < paths.Length; ++i)
            {
                n = paths[i];
                if (string.IsNullOrEmpty(n))
                {
                    continue;
                }

                if ((c.EndsWith("/") || c.EndsWith("\\")))
                {
                    if (n.StartsWith("/") || n.StartsWith("\\"))
                    {
                        sb.Append(n.Substring(1));
                    }
                    else
                    {
                        sb.Append(n);
                    }
                }
                else
                {
                    if (n.StartsWith("/") || n.StartsWith("\\"))
                    {
                        sb.Append(n);
                    }
                    else
                    {
                        sb.Append(Path.DirectorySeparatorChar);
                        sb.Append(n);
                    }
                }
                c = n;
            }
            return sb.ToString();
        }
    }
}
