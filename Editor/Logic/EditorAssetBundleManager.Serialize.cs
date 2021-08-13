using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
	public class AssetSerializeInfo
	{
		public AssetInfo asset;
		public List<string> refers;
		public List<string> dependencies;
		public List<string> allDependencies;

		public AssetSerializeInfo()
		{
			asset = null;
			refers = new List<string>();
			dependencies = new List<string>();
			allDependencies = new List<string>();
		}
	}

	public class BundleSerializeInfo
	{
		public BundleInfo bundle;

		public string mainAsset;
		public List<string> assets;
		public List<uint> refers;
		public List<uint> dependencies;

		public BundleSerializeInfo()
		{
			bundle = null;
			mainAsset = null;
			assets = new List<string>();
			refers = new List<uint>();
			dependencies = new List<uint>();
		}
	}

	/// <summary>
	/// 序列化的时候是无序保存，所以在反序列化的时候要循环二遍。
	/// 如果序列化的时候，按引用层级序列化，反序列化只需要循环一遍。
	/// </summary>
	public partial class EditorAssetBundleManager
	{

		#region Serialize Binary
		public static void SerializeAsset(AssetInfo asset , BinaryWriter writer)
		{
			writer.Write(asset.assetPath);
			writer.Write((byte)asset.assetType);
			writer.Write(asset.fileSize);
			writer.Write(asset.addressable);
			//deps
			writer.Write(asset.dependencies.Count);
			foreach (var dep in asset.dependencies)
			{
				writer.Write(dep.assetPath);
			}
			//refers
			writer.Write(asset.refers.Count);
			foreach(var refer in asset.refers)
			{
				writer.Write(refer.assetPath);
			}
			////all deps
			//writer.Write(asset.allDependencies.Count);
			//foreach (var dep in asset.allDependencies)
			//{
			//	writer.Write(dep.assetPath);
			//}
		}
		public static AssetSerializeInfo DeserializeAsset(BinaryReader reader)
		{
			AssetSerializeInfo assetSerializeInfo = new AssetSerializeInfo();

			string assetPath = reader.ReadString();
			AssetInfo asset = new AssetInfo(assetPath);
			asset.assetType = (AssetInfo.AssetType)reader.ReadByte();
			asset.fileSize = reader.ReadInt64();
			asset.addressable = reader.ReadBoolean();

			assetSerializeInfo.asset = asset;

			int depsCount = reader.ReadInt32();
			assetSerializeInfo.dependencies.Capacity = assetSerializeInfo.dependencies.Count + depsCount;
			for (int i = 0; i < depsCount; ++i)
			{
				assetSerializeInfo.dependencies.Add(reader.ReadString());
			}

			int referCount = reader.ReadInt32();
			assetSerializeInfo.refers.Capacity = assetSerializeInfo.refers.Count + referCount;
			for (int i = 0; i < referCount; ++i)
			{
				assetSerializeInfo.refers.Add(reader.ReadString());
			}
			return assetSerializeInfo;
		}

		public static void SerializeBundle(BundleInfo bundle, BinaryWriter writer)
		{
			writer.Write(bundle.id);
			writer.Write(bundle.name==null?"":bundle.name);
			writer.Write(bundle.variantName == null ? "" : bundle.variantName);
			writer.Write((byte)bundle.bundleType);
			writer.Write(bundle.IsStandalone());
			writer.Write(bundle.refersHashCode);
			//main asset
			writer.Write(bundle.mainAssetPath);

			//assets
			writer.Write(bundle.assets.Count);
			foreach (var asset in bundle.assets)
			{
				writer.Write(asset.assetPath);
			}

			//deps
			writer.Write(bundle.dependencies.Count);
			foreach (var dep in bundle.dependencies)
			{
				writer.Write(dep.id);
			}

			//refers
			writer.Write(bundle.refers.Count);
			foreach (var refer in bundle.refers)
			{
				writer.Write(refer.id);
			}
		}
		public static BundleSerializeInfo DeserializeBundle(BinaryReader reader)
		{
			BundleSerializeInfo bundleSerializeInfo = new BundleSerializeInfo();

			uint id = reader.ReadUInt32();
			string name = reader.ReadString();
			string variantName = reader.ReadString();
			BundleInfo bundle = new BundleInfo(id,name, variantName);
			bundle.bundleType =(BundleInfo.BundleType) reader.ReadByte();
			bundle.SetStandalone(reader.ReadBoolean());
			bundle.refersHashCode = reader.ReadInt32();

			bundleSerializeInfo.bundle = bundle;

			bundleSerializeInfo.mainAsset = reader.ReadString();

			int assetCount = reader.ReadInt32();
			bundleSerializeInfo.assets.Capacity = bundleSerializeInfo.assets.Count + assetCount;
			for (int i = 0; i < assetCount; ++i)
			{
				bundleSerializeInfo.assets.Add(reader.ReadString());
			}


			int depsCount = reader.ReadInt32();
			bundleSerializeInfo.dependencies.Capacity = bundleSerializeInfo.dependencies.Count + depsCount;
			for (int i = 0; i < depsCount; ++i)
			{
				bundleSerializeInfo.dependencies.Add(reader.ReadUInt32());
			}

			int referCount = reader.ReadInt32();
			bundleSerializeInfo.refers.Capacity = bundleSerializeInfo.refers.Count + referCount;
			for (int i = 0; i < referCount; ++i)
			{
				bundleSerializeInfo.refers.Add(reader.ReadUInt32());
			}

			return bundleSerializeInfo;
		}

		public static void SerializeAssets(ICollection<AssetInfo> assets, BinaryWriter writer)
		{
			writer.Write(assets.Count);
			foreach (var asset in assets)
			{
				SerializeAsset(asset, writer);
			}
		}

		public static List<AssetSerializeInfo> DeserializeAssets(BinaryReader reader)
		{
			int assetCount = reader.ReadInt32();
			List<AssetSerializeInfo>  assetSerializeInfos = new List<AssetSerializeInfo>(assetCount);
			for (int i = 0; i < assetCount; ++i)
			{
				AssetSerializeInfo assetSerializeInfo = DeserializeAsset(reader);
				assetSerializeInfos.Add(assetSerializeInfo);
			}
			return assetSerializeInfos;
		}

		public static void SerializeBundles(ICollection<BundleInfo> bundles, BinaryWriter writer)
		{
			writer.Write(bundles.Count);
			foreach (var bundle in bundles)
			{
				SerializeBundle(bundle, writer);
			}
		}

		public static List<BundleSerializeInfo> DeserializeBundles(BinaryReader reader)
		{
			int bundleCount = reader.ReadInt32();
			List<BundleSerializeInfo> bundleSerializeInfos = new List<BundleSerializeInfo>(bundleCount);
			for (int i = 0; i < bundleCount; ++i)
			{
				BundleSerializeInfo bundleSerializeInfo = DeserializeBundle(reader);
				bundleSerializeInfos.Add(bundleSerializeInfo);
			}
			return bundleSerializeInfos;
		}

		#endregion //Serialize

		#region Load Save  Binary
		public void SaveAssets(Stream output)
		{
			using (BinaryWriter bw = new BinaryWriter(output))
			{
				SerializeAssets(m_Assets.Values,bw);
			}
		}

		public void LoadAssets(BinaryReader reader)
		{
			CleanAssets();

			List<AssetSerializeInfo> assetSerializeInfos = DeserializeAssets(reader);

			if (assetSerializeInfos != null)
			{
				foreach (var assetSerializeInfo in assetSerializeInfos)
				{
					AssetInfo asset = assetSerializeInfo.asset;
					m_Assets[asset.assetPath] = asset;
				}

				foreach (var assetSerializeInfo in assetSerializeInfos)
				{
					AssetInfo asset = assetSerializeInfo.asset;
					foreach (var depAssetPath in assetSerializeInfo.dependencies)
					{
						AssetInfo dep = m_Assets[depAssetPath];
						asset.AddDependencyOnlfy(dep);
					}

					foreach (var referAssetPath in assetSerializeInfo.refers)
					{
						AssetInfo refer = m_Assets[referAssetPath];
						asset.AddReferOnly(refer);
					}
				}
			}
		}

		public void LoadAssets(Stream input)
		{
			using (BinaryReader br = new BinaryReader(input))
			{
				LoadAssets(br);
			}
		}

		public void SaveAssets(string filePath)
		{
			string dir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			using (FileStream fs = new FileStream(filePath, FileMode.Truncate))
			{
				SaveAssets(fs);
			}
		}

		public void LoadAssets(string filePath)
		{
			using (FileStream fs = new FileStream(filePath, FileMode.Open))
			{
				LoadAssets(fs);
			}
		}

		public void SaveBundles(Stream output)
		{
			using (BinaryWriter bw = new BinaryWriter(output))
			{
				bw.Write(m_Bundles.Count);
				foreach (var bundle in m_Bundles)
				{
					SerializeBundle(bundle, bw);
				}
			}
		}

		public void LoadBundles(BinaryReader reader)
		{
			CleanBundles();

			List<BundleSerializeInfo> bundleSerializeInfos = DeserializeBundles(reader);

			if (bundleSerializeInfos != null)
			{
				foreach (var bundleSerializeInfo in bundleSerializeInfos)
				{
					BundleInfo bundle = bundleSerializeInfo.bundle;
					m_Bundles.Add(bundle);
					m_BundlesIdMap[bundle.id] = bundle;

					//main asset
					AssetInfo mainAsset = GetAsset(bundleSerializeInfo.mainAsset);
					if (mainAsset != null)
					{
						bundle.SetMainAsset(mainAsset);
					}

					//assets
					foreach (var assetPath in bundleSerializeInfo.assets)
					{
						AssetInfo asset = GetAsset(assetPath);
						if (asset!=null)
						{
							bundle.AddAsset(asset);
						}
					}
				}

				foreach (var bundleSerializeInfo in bundleSerializeInfos)
				{
					BundleInfo bundle = bundleSerializeInfo.bundle;
					foreach (var depId in bundleSerializeInfo.dependencies)
					{
						BundleInfo depBundle = m_BundlesIdMap[depId];
						bundle.AddDependencyOnly(depBundle);
					}

					foreach (var referId in bundleSerializeInfo.refers)
					{
						BundleInfo referBundle = m_BundlesIdMap[referId];
						bundle.AddReferOnly(referBundle);
					}
				}
			}
		}

		public void LoadBundles(Stream input)
		{
			using (BinaryReader br = new BinaryReader(input))
			{
				LoadBundles(br);
			}
		}

		public void SaveBundles(string filePath)
		{
			string dir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			using (FileStream fs = new FileStream(filePath, FileMode.Truncate))
			{
				SaveBundles(fs);
			}
		}

		public void LoadBundles(string filePath)
		{
			using (FileStream fs = new FileStream(filePath, FileMode.Open))
			{
				LoadBundles(fs);
			}
		}

		public void SaveBinary(Stream output)
		{
			using (BinaryWriter bw = new BinaryWriter(output))
			{
				SerializeAssets(m_Assets.Values,bw);
				SerializeBundles(m_Bundles,bw);
			}
		}

		public void LoadBinary(Stream input)
		{
			using (BinaryReader br = new BinaryReader(input))
			{
				LoadAssets(br);
				LoadBundles(br);
			}
		}

		public void SaveBinary(string filePath)
		{
			string dir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			using (FileStream fs = new FileStream(filePath, FileMode.Truncate))
			{
				SaveBinary(fs);
			}
		}

		public void LoadBinary(string filePath)
		{
			using (FileStream fs = new FileStream(filePath, FileMode.Open))
			{
				LoadBinary(fs);
			}
		}

		#endregion // Load Save Binary


		#region Load Save Json

		[System.Serializable]
		public class AssetJsonInfo
		{
			public string assetPath;
			public AssetInfo.AssetType assetType;
			public long fileSize;
			public bool addressable;
			public List<string> refers;
			public List<string> dependencies;
			public List<string> allDependencies;
		}

		[System.Serializable]
		public class BundleJsonInfo
		{
			public uint id;
			public string name;
			public string variantName;
			public BundleInfo.BundleType bundleType;
			public bool standalone ;
			public int refersHashCode;

			public string mainAsset;
			public List<string> assets;
			public List<uint> refers;
			public List<uint> dependencies;
		}

		[System.Serializable]
		public class AssetBundleJsonInfo
		{
			public List<AssetJsonInfo> assets;
			public List<BundleJsonInfo> bundles;
		}

		public AssetJsonInfo AssetInfoToJsonInfo(AssetInfo assetInfo)
		{
			AssetJsonInfo assetJsonInfo = new AssetJsonInfo();
			assetJsonInfo.assetPath = assetInfo.assetPath;
			assetJsonInfo.assetType = assetInfo.assetType;
			assetJsonInfo.fileSize = assetInfo.fileSize;
			assetJsonInfo.refers = new List<string>();
			assetJsonInfo.dependencies = new List<string>();

			foreach (var dep in assetInfo.dependencies)
			{
				assetJsonInfo.dependencies.Add(dep.assetPath);
			}

			foreach (var refer in assetInfo.refers)
			{
				assetJsonInfo.refers.Add(refer.assetPath);
			}
			return assetJsonInfo;
		}

		public BundleJsonInfo BundleInfoToJsonInfo(BundleInfo bundleInfo)
		{
			BundleJsonInfo bundleJsonInfo = new BundleJsonInfo();
			bundleJsonInfo.id = bundleInfo.id;
			bundleJsonInfo.name = bundleInfo.name;
			bundleJsonInfo.variantName = bundleInfo.variantName;
			bundleJsonInfo.bundleType = bundleInfo.bundleType;
			bundleJsonInfo.standalone = bundleInfo.IsStandalone();
			bundleJsonInfo.refersHashCode = bundleInfo.refersHashCode;
			bundleJsonInfo.mainAsset = bundleInfo.mainAssetPath;
			bundleJsonInfo.assets = new List<string>();
			bundleJsonInfo.dependencies = new List<uint>();
			bundleJsonInfo.refers = new List<uint>();

			foreach (var asset in bundleInfo.assets)
			{
				bundleJsonInfo.assets.Add(asset.assetPath);
			}

			foreach (var refer in bundleInfo.refers)
			{
				bundleJsonInfo.refers.Add(refer.id);
			}

			foreach (var dep in bundleInfo.dependencies)
			{
				bundleJsonInfo.dependencies.Add(dep.id);
			}
			return bundleJsonInfo;
		}

		public void BuildAssets(List<AssetJsonInfo> assets)
		{
			CleanAssets();

			if (assets == null || assets.Count == 0)
			{
				return;
			}

			//create assets
			foreach (var assetJson in assets)
			{
				AssetInfo assetInfo = CreateAsset(assetJson.assetPath);
				assetInfo.assetType = assetJson.assetType;
				assetInfo.fileSize = assetJson.fileSize;
				assetInfo.addressable = assetJson.addressable;
			}

			//build asset relations
			foreach (var assetJson in assets)
			{
				AssetInfo assetInfo = GetAsset(assetJson.assetPath);
				//deps
				foreach (var assetPath in assetJson.dependencies)
				{
					AssetInfo dep = GetAsset(assetPath);
					assetInfo.AddDependencyOnlfy(dep);
				}

				//refers
				foreach (var assetPath in assetJson.refers)
				{
					AssetInfo refer = GetAsset(assetPath);
					assetInfo.AddReferOnly(refer);
				}
			}
		}

		public void BuildBundles(List<BundleJsonInfo> bundles)
		{
			CleanBundles();

			if (bundles == null)
			{
				return;
			}

			foreach (var bundleJson in bundles)
			{
				BundleInfo bundle = CreateBundle(bundleJson.id, bundleJson.name, bundleJson.variantName);
				bundle.bundleType = bundleJson.bundleType;
				bundle.SetStandalone(bundleJson.standalone);
				bundle.refersHashCode = bundleJson.refersHashCode;

				//main asset
				AssetInfo mainAsset = GetAsset(bundleJson.mainAsset);
				if (mainAsset != null)
				{
					bundle.SetMainAsset(mainAsset);
				}

				//assets
				foreach (var assetPath in bundleJson.assets)
				{
					AssetInfo asset = GetAsset(assetPath);
					if (asset != null)
					{
						bundle.AddAsset(asset);
					}
				}
			}

			foreach (var bundleJson in bundles)
			{
				BundleInfo bundle = GetBundle(bundleJson.id);

				foreach (var depId in bundleJson.dependencies)
				{
					BundleInfo depBundle = GetBundle(depId);
					bundle.AddDependencyOnly(depBundle);
				}

				foreach (var referId in bundleJson.refers)
				{
					BundleInfo referBundle = GetBundle(referId);
					bundle.AddReferOnly(referBundle);
				}
			}
		}

		public string SerializeToJson(bool pretty=true)
		{
			AssetBundleJsonInfo assetBundleJsonInfo = new AssetBundleJsonInfo();
			assetBundleJsonInfo.assets = new List<AssetJsonInfo>();
			foreach (var iter in assets)
			{
				AssetJsonInfo assetJsonInfo = AssetInfoToJsonInfo(iter.Value);
				assetBundleJsonInfo.assets.Add(assetJsonInfo);
			}

			assetBundleJsonInfo.bundles = new List<BundleJsonInfo>();
			foreach (var bundle in bundles)
			{
				BundleJsonInfo bundleJsonInfo = BundleInfoToJsonInfo(bundle);
				assetBundleJsonInfo.bundles.Add(bundleJsonInfo);
			}
			string jsonStr = JsonUtility.ToJson(assetBundleJsonInfo, pretty);
			return jsonStr;
		}

		public void DeserializeFromJson(string jsonStr)
		{
			AssetBundleJsonInfo data = JsonUtility.FromJson<AssetBundleJsonInfo>(jsonStr);
			BuildAssets(data.assets);
			BuildBundles(data.bundles);
		}

		public void SaveToJson(string jsonFile)
		{
			string jsonStr = SerializeToJson();
			string jsonFileDir = Path.GetDirectoryName(jsonFile);
			if (!Directory.Exists(jsonFileDir))
			{
				Directory.CreateDirectory(jsonFileDir);
			}
			File.WriteAllText(jsonFile, jsonStr);
		}

		public void LoadFromJson(string jsonFile)
		{
			string jsonStr = File.ReadAllText(jsonFile);
			DeserializeFromJson(jsonStr);
		}
		#endregion //Load Save Json
	}
}
