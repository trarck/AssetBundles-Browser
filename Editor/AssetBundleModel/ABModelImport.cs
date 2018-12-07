using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleBrowser.AssetBundleModel
{

    public static class Import
    {
        public enum Format
        {
            FullPath=1,
            ShortName=2,
            WithFolder=4,
            WithExt=8
        }

        public delegate string CreateBundleNameDelegate(string filePath, bool useFullPath, bool useExt);
        public static CreateBundleNameDelegate CreateBundleNameHandle=null;

        internal static int ImportFile(string filePath, BundleFolderConcreteInfo parent,bool useFullname=false,bool useExt=false)
        {
            if (Path.IsPathRooted(filePath))
            {
                filePath = Relative(Path.GetDirectoryName(Application.dataPath), filePath);
            }

            if (!Model.ValidateAsset(filePath))
            {
                return 0;
            }

            var newBundle = Model.CreateEmptyBundle(parent, CreateBundleName(filePath, useFullname, useExt));
            Model.MoveAssetToBundle(filePath, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
            return newBundle.nameHashCode;
        }
        public static int ImportFile(string filePath,bool useFullname = true, bool useExt = true)
        {
            return ImportFile(filePath, null, useFullname, useExt);
        }
        public static int ImportFileToStringPath(string filePath,string parentPath, bool useFullname = true, bool useExt = true)
        {
            BundleNameData nameData = new BundleNameData(parentPath);
            BundleFolderConcreteInfo parentFolder = Model.FindBundle(nameData) as BundleFolderConcreteInfo;
            return ImportFile(filePath, parentFolder, useFullname, useExt);
        }

        private struct ImportFolderInfo
        {
            public ImportFolderInfo(DirectoryInfo directory, BundleFolderConcreteInfo parent)
            {
                this.directory = directory;
                this.parent = parent;
            }
            public DirectoryInfo directory;
            public BundleFolderConcreteInfo parent;
        }

        internal static void ImportFolder(string folderPath, BundleFolderConcreteInfo parent, Format format)
        {
            if (!Directory.Exists(folderPath))
                return;
            Stack<ImportFolderInfo> dirs = new Stack<ImportFolderInfo>();

            ImportFolderInfo startInfo = new ImportFolderInfo();
            startInfo.directory = new DirectoryInfo(folderPath);
            startInfo.parent = parent;
            dirs.Push(startInfo);

            ImportFolderInfo dir;
            while (dirs.Count > 0)
            {
                dir = dirs.Pop();

                if ((format & Format.WithFolder)==Format.WithFolder)
                {
                    parent = Model.CreateEmptyBundleFolder(dir.parent, dir.directory.Name) as BundleFolderConcreteInfo;
                }

                foreach (FileInfo fi in dir.directory.GetFiles())
                {
                    if (Path.GetExtension(fi.Name).ToLower() == ".meta")
                        continue;

                    ImportFile(fi.FullName, parent, (format&Format.FullPath)== Format.FullPath, (format & Format.WithExt) == Format.WithExt);
                }

                foreach(DirectoryInfo di in dir.directory.GetDirectories())
                {
                    if (!di.Name.StartsWith("."))
                    {
                        dirs.Push(new ImportFolderInfo(di, parent));
                    }
                }
            }
        }
        public static void ImportFolder(string folderPath, Format format = Format.FullPath)
        {
            ImportFolder(folderPath, null,format);
        }

        public static void ImportFolderToStringPath(string folderPath,string parentPath, Format format = Format.FullPath)
        {
            BundleNameData nameData = new BundleNameData(parentPath);
            BundleFolderConcreteInfo parentFolder =Model.FindBundle(nameData) as BundleFolderConcreteInfo;
            ImportFolder(folderPath, parentFolder, format);
        }

        private static string CreateBundleName(string filePath,bool useFullPath,bool useExt)
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
