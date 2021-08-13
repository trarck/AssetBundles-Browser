using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
	public partial class EditorAssetBundleManager
	{
		public AssetInfo ImportAsset(string assetPath, bool importDependency = true)
		{
			if (!ValidateAsset(assetPath))
			{
				return null;
			}

			if (Path.IsPathRooted(assetPath))
			{
				assetPath = YH.FileSystem.Relative(Path.GetDirectoryName(Application.dataPath), assetPath);
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
	}
}
