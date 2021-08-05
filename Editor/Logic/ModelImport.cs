using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleBuilder.Model
{

    public static class Import
    {


        internal static int ImportFile(string filePath, BundleFolderConcreteNode parent,Setting.Format format,bool force=true)
        {
            if (Path.IsPathRooted(filePath))
            {
                filePath = ModelUtils.Relative(Path.GetDirectoryName(Application.dataPath), filePath);
            }

            if (!Model.ValidateAsset(filePath))
            {
                return 0;
            }

            if ((format & Setting.Format.WithFolder) == Setting.Format.WithFolder)
            {
                parent = Model.CreateOrGetBundleFolder(parent, Setting.GetBundleFolderName(Path.GetDirectoryName(filePath))) as BundleFolderConcreteNode;
            }

            bool useFullname = (format & Setting.Format.FullPath) == Setting.Format.FullPath;
            bool useExt = (format & Setting.Format.WithExt) == Setting.Format.WithExt;

            string bundleName = Setting.CreateBundleName(filePath, useFullname, useExt);


            if (!force)
            {
                //check assets have bundle name
                if (!string.IsNullOrEmpty(Model.DataSource.GetAssetBundleName(filePath))){
                    return 0;
                }

                //check asset tree have bundle name
                BundleNameData nameData = new BundleNameData(bundleName);
                BundleNode info = Model.FindBundle(nameData);
                if (info != null)
                {
                    return 0;
                }
            }

            var newBundle = Model.CreateEmptyBundle(parent, bundleName);
            Model.MoveAssetToBundle(filePath, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
            return newBundle.nameHashCode;
        }
        public static int ImportFile(string filePath, Setting.Format format, bool force = true)
        {
            return ImportFile(filePath, null, format, force);
        }
        public static int ImportFileToStringPath(string filePath,string parentPath, Setting.Format format, bool force = true)
        {
            BundleNameData nameData = new BundleNameData(parentPath);
            BundleFolderConcreteNode parentFolder = Model.FindBundle(nameData) as BundleFolderConcreteNode;
            return ImportFile(filePath, parentFolder, format, force);
        }

        private struct ImportFolderInfo
        {
            public ImportFolderInfo(DirectoryInfo directory, BundleFolderConcreteNode parent)
            {
                this.directory = directory;
                this.parent = parent;
            }
            public DirectoryInfo directory;
            public BundleFolderConcreteNode parent;
        }

        internal static List<int> ImportFolder(string folderPath, BundleFolderConcreteNode parent, Setting.Format format, bool force = true)
        {
            List<int> ids = new List<int>();

            if (!Directory.Exists(folderPath))
                return ids;

            Stack<ImportFolderInfo> dirs = new Stack<ImportFolderInfo>();

            ImportFolderInfo startInfo = new ImportFolderInfo();
            startInfo.directory = new DirectoryInfo(folderPath);
            startInfo.parent = parent;
            dirs.Push(startInfo);

            ImportFolderInfo dir;
            int hashCode = 0;
            while (dirs.Count > 0)
            {
                dir = dirs.Pop();

                //if ((format & Format.WithFolder)==Format.WithFolder)
                //{
                //    parent = Model.CreateEmptyBundleFolder(dir.parent, dir.directory.Name) as BundleFolderConcreteInfo;
                //}

                foreach (FileInfo fi in dir.directory.GetFiles())
                {
                    if (Path.GetExtension(fi.Name).ToLower() == ".meta")
                        continue;
                    hashCode = ImportFile(fi.FullName, parent, format,force);
                    if (hashCode > 0)
                    {
                        ids.Add(hashCode);
                    }
                }

                foreach(DirectoryInfo di in dir.directory.GetDirectories())
                {
                    if (!di.Name.StartsWith("."))
                    {
                        dirs.Push(new ImportFolderInfo(di, parent));
                    }
                }
            }

            return ids;
        }
        public static List<int> ImportFolder(string folderPath, Setting.Format format = Setting.Format.FullPath, bool force = true)
        {
            return ImportFolder(folderPath, null,format,force);
        }

        public static List<int> ImportFolderToStringPath(string folderPath,string parentPath, Setting.Format format = Setting.Format.FullPath, bool force = true)
        {
            BundleNameData nameData = new BundleNameData(parentPath);
            BundleFolderConcreteNode parentFolder =Model.FindBundle(nameData) as BundleFolderConcreteNode;
            return ImportFolder(folderPath, parentFolder, format,force);
        }
    }
}
