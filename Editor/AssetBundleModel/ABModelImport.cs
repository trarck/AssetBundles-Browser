using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleBrowser.AssetBundleModel
{

    public static class Import
    {

        internal static int ImportFile(string filePath, BundleFolderInfo parent=null,bool useFullPath = false)
        {
            if (Path.IsPathRooted(filePath))
            {
                filePath = Relative(Path.GetDirectoryName(Application.dataPath), filePath);
            }

            if (!Model.ValidateAsset(filePath))
            {
                return 0;
            }

            var newBundle = Model.CreateEmptyBundle(parent, GetBundleName(filePath, useFullPath));
            Model.MoveAssetToBundle(filePath, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
            return newBundle.nameHashCode;
        }
        public static int ImportFile(string filePath, bool useFullPath = false)
        {
            return ImportFile(filePath,null,useFullPath);
        }
        internal static void ImportForlder(string folderPath, BundleFolderInfo parent = null, bool useFullPath = false)
        {
            if (!Directory.Exists(folderPath))
                return;
            Stack<DirectoryInfo> dirs = new Stack<DirectoryInfo>();
            dirs.Push(new DirectoryInfo(folderPath));
            DirectoryInfo dir = null;
            while (dirs.Count > 0)
            {
                dir = dirs.Pop();

                foreach (FileInfo fi in dir.GetFiles())
                {
                    if (Path.GetExtension(fi.Name).ToLower() == ".meta")
                        continue;

                    ImportFile(fi.FullName, parent, useFullPath);
                }

                foreach(DirectoryInfo di in dir.GetDirectories())
                {
                    if (!di.Name.StartsWith("."))
                    {
                        dirs.Push(di);
                    }
                }
            }
        }
        public static void ImportForlder(string folderPath,  bool useFullPath = false)
        {
            ImportForlder(folderPath, null, useFullPath);
        }
        private static string GetBundleName(string filePath,bool useFullPath)
        {
            if (useFullPath)
            {
                return filePath.Replace('/', '_').Replace('\\', '_').Replace('.','_').ToLower();
            }
            else
            {
                return Path.GetFileNameWithoutExtension(filePath).ToLower();
            }
        }

        public static string Relative(string fromPath, string toPath)
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
                System.Text.StringBuilder toSB = new System.Text.StringBuilder();

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
