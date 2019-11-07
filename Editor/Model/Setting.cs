using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleBuilder.Model
{
    public static class Setting
    {
        public enum Format
        {
            None = 0,
            FullPath = 1,
            ShortName = 2,
            WithFolder = 4,
            WithExt = 8
        }

        public delegate string CreateBundleNameDelegate(string filePath, bool useFullPath, bool useExt);
        public static CreateBundleNameDelegate CreateBundleNameHandle=null;

        public static List<string> IgnoreFolderPrefixs = new List<string>() { "Assets" };

        public static string CreateBundleName(string filePath, Format format)
        {
            bool useFullname = (format & Setting.Format.FullPath) == Setting.Format.FullPath;
            bool useExt = (format & Setting.Format.WithExt) == Setting.Format.WithExt;
            return CreateBundleName(filePath, useFullname, useExt);
        }

        public static string CreateBundleName(string filePath,bool useFullPath,bool useExt)
        {
            if (CreateBundleNameHandle != null)
            {
                return CreateBundleNameHandle(filePath, useFullPath, useExt);
            }
            if (useFullPath)
            {
                return filePath.Replace('/', '_').Replace('\\', '_').Replace('.','_').ToLower();
            }
            else if(useExt || filePath.Contains(".unity"))//Scene always use ext
            {
                return Path.GetFileName(filePath).Replace('.', '_').ToLower();
            }
            else
            {
                return Path.GetFileNameWithoutExtension(filePath).ToLower();
            }
        }

        public static string GetBundleFolderName(string folderPath)
        {
            //TODO:Support custom name
            return ClearFolderPrefix(folderPath);
        }

        public static string ClearFolderPrefix(string folderPath)
        {
            folderPath = folderPath.Replace("\\", "/").ToLower();
            foreach (var clear in Config.BuilderConfig.Instance.data.bundlePathPrefixClears)
            {
                string lowerClear = clear.ToLower();
                if (folderPath.StartsWith(lowerClear))
                {
                    folderPath= folderPath.Replace(lowerClear, "");
                    if (folderPath.StartsWith("/"))
                    {
                        folderPath = folderPath.Substring(1);
                    }
                    return folderPath;
                }
            }
            return folderPath;
        }
    }
}
