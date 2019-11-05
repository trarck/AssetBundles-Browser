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
            else if(useExt)
            {
                return Path.GetFileName(filePath).Replace('.', '_').ToLower();
            }
            else
            {
                return Path.GetFileNameWithoutExtension(filePath).ToLower();
            }
        }

        public static void AddIgnoreFolderPrefix(string prefix,bool first=true)
        {
            if (first)
            {
                IgnoreFolderPrefixs.Insert(0, prefix);
            }
            else
            {
                //the default is Assets,and in the last.so insert before it
                IgnoreFolderPrefixs.Insert(IgnoreFolderPrefixs.Count - 1, prefix);
            }
        }

        public static string FilterFolderPrefix(string folderPath)
        {
            folderPath = folderPath.Replace("\\", "/");
            foreach (var ignore in IgnoreFolderPrefixs)
            {
                if (folderPath.StartsWith(ignore))
                {
                    return folderPath.Replace(ignore.EndsWith("/") ? ignore : ignore + "/", "");
                }
            }
            return folderPath;
        }
    }
}
