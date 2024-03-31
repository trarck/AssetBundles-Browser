using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YH.AssetManage;

namespace AssetBundleBuilder
{
	public partial class EditorAssetBundleManager
	{
		public AssetInfo ImportAsset(string assetPath, bool importDependency = true)
		{
			if (Path.IsPathRooted(assetPath))
			{
				assetPath = AssetPaths.Relative(Path.GetDirectoryName(Application.dataPath), assetPath);
			}

			if (!ValidateAsset(assetPath))
			{
				return null;
			}

			AssetInfo asset = GetOrCreateAsset(assetPath);
			if (asset != null)
			{
				asset.addressable = true;
				if (importDependency)
				{
					RefreshAssetDependencies(asset);
				}
			}
			return asset;
		}

		public bool ImportAssetFromFolder(string folderPath, string pattern = null, List<AssetInfo> assets = null)
		{
			DirectoryInfo startInfo = new DirectoryInfo(folderPath);
			if (!startInfo.Exists)
			{
				return false;
			}

			Stack<DirectoryInfo> dirs = new Stack<DirectoryInfo>();
			dirs.Push(startInfo);

			DirectoryInfo dir;

			bool haveFilter = false;
			System.Text.RegularExpressions.Regex reg = null;
			if (!string.IsNullOrEmpty(pattern))
			{
				haveFilter = true;
				reg = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			}

			while (dirs.Count > 0)
			{
				dir = dirs.Pop();

				foreach (FileInfo fi in dir.GetFiles())
				{
					if (!haveFilter || reg.IsMatch(fi.FullName))
					{
						AssetInfo asset = ImportAsset(fi.FullName);
						if (assets != null)
						{
							assets.Add(asset);
						}
					}
				}

				foreach (DirectoryInfo subDir in dir.GetDirectories())
				{
					if (!subDir.Name.StartsWith("."))
					{
						dirs.Push(subDir);
					}
				}
			}

			return true;
		}

		public BundleInfo ImportBundleFromFile(string assetPath,Setting.Format format)
        {
			AssetInfo assetInfo=ImportAsset(assetPath);
			if(assetInfo == null)
            {
				return null;
            }

			string bundleName = CreateBundleName(assetInfo.assetPath, format);
			BundleInfo bundleInfo = CreateBundle(bundleName, assetInfo);
			return bundleInfo;
		}

		public bool ImportBundlesFromFolder(string folderPath, string pattern, Setting.Format format, ICollection<BundleInfo> bundleInfos=null)
		{
			DirectoryInfo startInfo = new DirectoryInfo(folderPath);
			if (!startInfo.Exists)
			{
				return false;
			}

			Stack<DirectoryInfo> dirs = new Stack<DirectoryInfo>();
			dirs.Push(startInfo);

			DirectoryInfo dir;

			bool haveFilter = false;
			System.Text.RegularExpressions.Regex reg = null;
			if (!string.IsNullOrEmpty(pattern))
			{
				haveFilter = true;
				reg = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			}

			while (dirs.Count > 0)
			{
				dir = dirs.Pop();

				//Debug.LogFormat("Import {0}",dir.FullName);
				foreach (FileInfo fi in dir.GetFiles())
				{
					//Debug.LogFormat("{0},{1}", fi.FullName, reg.IsMatch(fi.FullName));
					if (!haveFilter || reg.IsMatch(fi.FullName))
					{
						BundleInfo bundleInfo = ImportBundleFromFile(fi.FullName,format);
						if (bundleInfo != null)
						{
							bundleInfo.SetStandalone(true);
							if (bundleInfos != null)
							{
								bundleInfos.Add(bundleInfo);
							}
						}
					}
				}

				foreach (DirectoryInfo subDir in dir.GetDirectories())
				{
					if (!subDir.Name.StartsWith("."))
					{
						dirs.Push(subDir);
					}
				}
			}

			return true;
		}
	}
}
