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
		public List<int> refers;
		public List<int> dependencies;
		public List<int> allDependencies;

		public AssetSerializeInfo()
		{
			asset = null;
			refers = new List<int>();
			dependencies = new List<int>();
			allDependencies = new List<int>();
		}
	}

	public class BundleSerializeInfo
	{
		public BundleInfo bundle;

		public int mainAsset;
		public List<int> assets;
		public List<int> refers;
		public List<int> dependencies;

		public BundleSerializeInfo()
		{
			bundle = null;
			mainAsset = -1;
			assets = new List<int>();
			refers = new List<int>();
			dependencies = new List<int>();
		}
	}

	/// <summary>
	/// 序列化的时候是无序保存，所以在反序列化的时候要循环二遍。
	/// 如果序列化的时候，按引用层级序列化，反序列化只需要循环一遍。
	/// </summary>
	public partial class EditorAssetBundleManager
	{
		public static string BundleDataName = "Bundles";
		public static string AssetAndBundleDataName = "AssetBundleData";
		public static string DataSaveDir = "AssetDatabase";
		public static string BinaryExtName = ".bin";
		public static string JsonExtName = ".json";

		#region Asset Binary Serialize
		private static void GenerateAssetsSerilizeIndex(ICollection<AssetInfo> assets)
		{
			int i = 0;
			foreach (var asset in assets)
			{
				//建立索引号。这里直接用数组的下标。
				asset.serializeIndex = i++;
			}
		}
		private static List<AssetSerializeInfo> CreateAssetSerializeInfos(ICollection<AssetInfo> assets)
		{
			//生成索引号
			GenerateAssetsSerilizeIndex(assets);

			//生成序列化信息
			List<AssetSerializeInfo> serializeInfos = new List<AssetSerializeInfo>(assets.Count);
			foreach (var asset in assets)
			{
				//生成序列化对象
				AssetSerializeInfo serializeInfo = new AssetSerializeInfo()
				{
					asset = asset
				};
				serializeInfos.Add(serializeInfo);

				//转换引用
				foreach (var refer in asset.refers)
				{
					serializeInfo.refers.Add(refer.serializeIndex);
				}

				//转换依赖
				foreach (var dep in asset.dependencies)
				{
					serializeInfo.dependencies.Add(dep.serializeIndex);
				}

				//转换所有依赖
				foreach (var dep in asset.allDependencies)
				{
					serializeInfo.allDependencies.Add(dep.serializeIndex);
				}
			}

			return serializeInfos;
		}
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
				writer.Write(dep.serializeIndex);
			}
			//refers
			writer.Write(asset.refers.Count);
			foreach(var refer in asset.refers)
			{
				writer.Write(refer.serializeIndex);
			}

			//all deps
			writer.Write(asset.allDependencies.Count);
			foreach (var dep in asset.allDependencies)
			{
				writer.Write(dep.serializeIndex);
			}
		}
		public static void SerializeAsset(AssetSerializeInfo serializeInfo, BinaryWriter writer)
		{
			AssetInfo asset = serializeInfo.asset;

			writer.Write(asset.assetPath);
			writer.Write((byte)asset.assetType);
			writer.Write(asset.fileSize);
			writer.Write(asset.addressable);

			//deps
			writer.Write(serializeInfo.dependencies.Count);
			foreach (var depIndex in serializeInfo.dependencies)
			{
				writer.Write(depIndex);
			}
			//refers
			writer.Write(serializeInfo.refers.Count);
			foreach (var referInfex in serializeInfo.refers)
			{
				writer.Write(referInfex);
			}

			//all deps
			writer.Write(serializeInfo.allDependencies.Count);
			foreach (var depIndex in serializeInfo.allDependencies)
			{
				writer.Write(depIndex);
			}
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
				assetSerializeInfo.dependencies.Add(reader.ReadInt32());
			}

			int referCount = reader.ReadInt32();
			assetSerializeInfo.refers.Capacity = assetSerializeInfo.refers.Count + referCount;
			for (int i = 0; i < referCount; ++i)
			{
				assetSerializeInfo.refers.Add(reader.ReadInt32());
			}

			int allDepsCount = reader.ReadInt32();
			assetSerializeInfo.allDependencies.Capacity = assetSerializeInfo.allDependencies.Count + allDepsCount;
			for (int i = 0; i < allDepsCount; ++i)
			{
				assetSerializeInfo.allDependencies.Add(reader.ReadInt32());
			}
			return assetSerializeInfo;
		}

		public static void SerializeAssets(ICollection<AssetInfo> assets, BinaryWriter writer)
		{
			writer.Write(assets.Count);
			foreach (var asset in assets)
			{
				SerializeAsset(asset, writer);
			}
		}
		public static void SerializeAssets(ICollection<AssetSerializeInfo> serializeInfos, BinaryWriter writer)
		{
			writer.Write(serializeInfos.Count);
			foreach (var serializeInfo in serializeInfos)
			{
				SerializeAsset(serializeInfo, writer);
			}
		}
		public static List<AssetSerializeInfo> DeserializeAssets(BinaryReader reader)
		{
			int assetCount = reader.ReadInt32();
			List<AssetSerializeInfo> assetSerializeInfos = new List<AssetSerializeInfo>(assetCount);
			for (int i = 0; i < assetCount; ++i)
			{
				AssetSerializeInfo assetSerializeInfo = DeserializeAsset(reader);
				assetSerializeInfos.Add(assetSerializeInfo);
			}
			return assetSerializeInfos;
		}


		public static void ShortSerializeAsset(AssetInfo asset, BinaryWriter writer)
		{
			writer.Write(asset.assetPath);
		}
		public static AssetInfo ShortDeserializeAsset(BinaryReader reader)
		{
			string assetPath = reader.ReadString();
			AssetInfo asset = new AssetInfo(assetPath);
			return asset;
		}

		public static void ShortSerializeAssets(ICollection<AssetInfo> assets, BinaryWriter writer)
		{
			writer.Write(assets.Count);
			foreach (var asset in assets)
			{
				ShortSerializeAsset(asset, writer);
			}
		}
		public static List<AssetInfo> ShortDeserializeAssets(BinaryReader reader)
		{
			int assetCount = reader.ReadInt32();
			List<AssetInfo> assetInfos = new List<AssetInfo>(assetCount);
			for (int i = 0; i < assetCount; ++i)
			{
				AssetInfo assetInfo = ShortDeserializeAsset(reader);
				assetInfos.Add(assetInfo);
			}
			return assetInfos;
		}

		#endregion //Asset Binary Serialize

		#region Bundle Binary Serialize
		private static void GenerateBundlesSerilizeIndex(ICollection<BundleInfo> bundles)
		{
			int i = 0;
			foreach (var bundle in bundles)
			{
				//建立索引号。这里直接用数组的下标。
				bundle.serializeIndex = i++;
			}
		}
		private static List<BundleSerializeInfo> CreateBundleSerializeInfos(ICollection<BundleInfo> bundles)
		{
			//生成索引号
			GenerateBundlesSerilizeIndex(bundles);

			//生成序列化信息
			List<BundleSerializeInfo> serializeInfos = new List<BundleSerializeInfo>(bundles.Count);
			foreach (var bundle in bundles)
			{
				//生成序列化对象
				BundleSerializeInfo serializeInfo = new BundleSerializeInfo()
				{
					bundle = bundle,
				};
				serializeInfos.Add(serializeInfo);

				//转换主资源
				if (bundle.mainAsset != null)
				{
					serializeInfo.mainAsset = bundle.mainAsset.serializeIndex;
				}

				//转换资源
				foreach (var assetInfo in bundle.assets)
				{
					serializeInfo.assets.Add(assetInfo.serializeIndex);
				}

				//转换引用
				foreach (var refer in bundle.refers)
				{
					serializeInfo.refers.Add(refer.serializeIndex);
				}

				//转换依赖
				foreach (var dep in bundle.dependencies)
				{
					serializeInfo.dependencies.Add(dep.serializeIndex);
				}
			}

			return serializeInfos;
		}
		public static void SerializeBundle(BundleInfo bundle, BinaryWriter writer)
		{
			writer.Write(bundle.name==null?"":bundle.name);
			writer.Write(bundle.variantName == null ? "" : bundle.variantName);
			writer.Write((byte)bundle.bundleType);
			writer.Write(bundle.IsStandalone());
			writer.Write(bundle.refersHashCode);
			//main asset
			writer.Write(bundle.mainAsset.serializeIndex);

			//assets
			writer.Write(bundle.assets.Count);
			foreach (var asset in bundle.assets)
			{
				writer.Write(asset.serializeIndex);
			}

			//deps
			writer.Write(bundle.dependencies.Count);
			foreach (var dep in bundle.dependencies)
			{
				writer.Write(dep.serializeIndex);
			}

			//refers
			writer.Write(bundle.refers.Count);
			foreach (var refer in bundle.refers)
			{
				writer.Write(refer.serializeIndex);
			}
		}
		public static void SerializeBundle(BundleSerializeInfo serializeInfo, BinaryWriter writer)
		{
			BundleInfo bundle = serializeInfo.bundle;

			writer.Write(bundle.name == null ? "" : bundle.name);
			writer.Write(bundle.variantName == null ? "" : bundle.variantName);
			writer.Write((byte)bundle.bundleType);
			writer.Write(bundle.IsStandalone());
			writer.Write(bundle.refersHashCode);

			//main asset
			writer.Write(serializeInfo.mainAsset);

			//assets
			writer.Write(serializeInfo.assets.Count);
			foreach (var assetIndex in serializeInfo.assets)
			{
				writer.Write(assetIndex);
			}

			//deps
			writer.Write(serializeInfo.dependencies.Count);
			foreach (var depIndex in serializeInfo.dependencies)
			{
				writer.Write(depIndex);
			}

			//refers
			writer.Write(serializeInfo.refers.Count);
			foreach (var referIndex in serializeInfo.refers)
			{
				writer.Write(referIndex);
			}
		}
		public static BundleSerializeInfo DeserializeBundle(BinaryReader reader)
		{
			BundleSerializeInfo bundleSerializeInfo = new BundleSerializeInfo();

			string name = reader.ReadString();
			string variantName = reader.ReadString();
			BundleInfo bundle = new BundleInfo(name, variantName);

			bundle.bundleType =(BundleInfo.BundleType) reader.ReadByte();
			bundle.SetStandalone(reader.ReadBoolean());
			bundle.refersHashCode = reader.ReadInt32();

			bundleSerializeInfo.bundle = bundle;

			bundleSerializeInfo.mainAsset = reader.ReadInt32();

			int assetCount = reader.ReadInt32();
			bundleSerializeInfo.assets.Capacity = bundleSerializeInfo.assets.Count + assetCount;
			for (int i = 0; i < assetCount; ++i)
			{
				bundleSerializeInfo.assets.Add(reader.ReadInt32());
			}


			int depsCount = reader.ReadInt32();
			bundleSerializeInfo.dependencies.Capacity = bundleSerializeInfo.dependencies.Count + depsCount;
			for (int i = 0; i < depsCount; ++i)
			{
				bundleSerializeInfo.dependencies.Add(reader.ReadInt32());
			}

			int referCount = reader.ReadInt32();
			bundleSerializeInfo.refers.Capacity = bundleSerializeInfo.refers.Count + referCount;
			for (int i = 0; i < referCount; ++i)
			{
				bundleSerializeInfo.refers.Add(reader.ReadInt32());
			}

			return bundleSerializeInfo;
		}

		public static void SerializeBundles(ICollection<BundleInfo> bundles, BinaryWriter writer)
		{
			writer.Write(bundles.Count);
			foreach (var bundle in bundles)
			{
				SerializeBundle(bundle, writer);
			}
		}
		public static void SerializeBundles(ICollection<BundleSerializeInfo> serializeInfos, BinaryWriter writer)
		{
			writer.Write(serializeInfos.Count);
			foreach (var serializeInfo in serializeInfos)
			{
				SerializeBundle(serializeInfo, writer);
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

		#endregion //Bundle Binary Serialize

		#region Load Save  Binary
		public void SaveAssets(Stream output)
		{
			using (BinaryWriter bw = new BinaryWriter(output))
			{
				GenerateAssetsSerilizeIndex(m_Assets.Values);
				SerializeAssets(m_Assets.Values,bw);
			}
		}

		public List<AssetInfo> LoadAssets(BinaryReader reader)
		{
			CleanAssets();

			List<AssetInfo> assetsMap = null;

			List <AssetSerializeInfo> assetSerializeInfos = DeserializeAssets(reader);

			if (assetSerializeInfos != null)
			{
				assetsMap = new List<AssetInfo>();

				//建立其他资源信息
				foreach (var assetSerializeInfo in assetSerializeInfos)
				{
					AssetInfo asset = assetSerializeInfo.asset;
					m_Assets[asset.assetPath] = asset;

					assetsMap.Add(asset);
				}

				//设置索引对象
				foreach (var assetSerializeInfo in assetSerializeInfos)
				{
					AssetInfo asset = assetSerializeInfo.asset;
					foreach (var depIndex in assetSerializeInfo.dependencies)
					{
						AssetInfo dep = assetsMap[depIndex];
						asset.AddDependencyOnlfy(dep);
					}

					foreach (var referIndex in assetSerializeInfo.refers)
					{
						AssetInfo refer = assetsMap[referIndex];
						asset.AddReferOnly(refer);
					}
				}
			}

			return assetsMap;
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
			using (FileStream fs = new FileStream(filePath, FileMode.Create))
			{
				SaveAssets(fs);
			}
		}

		public void LoadAssets(string filePath)
		{
			if (File.Exists(filePath))
			{
				using (FileStream fs = new FileStream(filePath, FileMode.Open))
				{
					LoadAssets(fs);
				}
			}
		}

		public void SaveBundles(Stream output)
		{
			using (BinaryWriter bw = new BinaryWriter(output))
			{
				GenerateAssetsSerilizeIndex(m_Assets.Values);
				GenerateBundlesSerilizeIndex(m_Bundles);

				ShortSerializeAssets(m_Assets.Values, bw);
				SerializeBundles(m_Bundles, bw);
			}
		}

		public void LoadBundles(BinaryReader reader, List<AssetInfo> assetsMap)
		{
			CleanBundles();

			List<BundleSerializeInfo> bundleSerializeInfos = DeserializeBundles(reader);

			if (bundleSerializeInfos != null)
			{
				//基本信息
				foreach (var bundleSerializeInfo in bundleSerializeInfos)
				{
					BundleInfo bundle = bundleSerializeInfo.bundle;
					m_Bundles.Add(bundle);

					//main asset
					if (bundleSerializeInfo.mainAsset > -1)
					{
						AssetInfo mainAsset = assetsMap[bundleSerializeInfo.mainAsset];
						bundle.SetMainAsset(mainAsset);
					}

					//assets
					foreach (var assetIndex in bundleSerializeInfo.assets)
					{
						if (assetIndex > -1)
						{
							AssetInfo asset = assetsMap[assetIndex];
							bundle.AddAsset(asset);
						}
					}
				}

				//索引转成对象
				foreach (var bundleSerializeInfo in bundleSerializeInfos)
				{
					BundleInfo bundle = bundleSerializeInfo.bundle;
					foreach (var depIndex in bundleSerializeInfo.dependencies)
					{
						BundleInfo depBundle = m_Bundles[depIndex];
						bundle.AddDependencyOnly(depBundle);
					}

					foreach (var referIndex in bundleSerializeInfo.refers)
					{
						BundleInfo referBundle = m_Bundles[referIndex];
						bundle.AddReferOnly(referBundle);
					}
				}
			}
		}

		public void LoadBundles(Stream input)
		{
			using (BinaryReader br = new BinaryReader(input))
			{
				List<AssetInfo> assetInfos = ShortDeserializeAssets(br);
				LoadBundles(br, assetInfos);
			}
		}

		public void SaveBundles(string filePath)
		{
			string dir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			using (FileStream fs = new FileStream(filePath, FileMode.Create))
			{
				SaveBundles(fs);
			}
		}

		public void LoadBundles(string filePath)
		{
			if (File.Exists(filePath))
			{
				using (FileStream fs = new FileStream(filePath, FileMode.Open))
				{
					LoadBundles(fs);
				}
			}
		}

		public void SaveBinary(Stream output)
		{
			using (BinaryWriter bw = new BinaryWriter(output))
			{
				List<AssetSerializeInfo> assetSerializeInfos = CreateAssetSerializeInfos(m_Assets.Values);
				SerializeAssets(assetSerializeInfos, bw);

				List<BundleSerializeInfo> bundleSerializeInfos = CreateBundleSerializeInfos(m_Bundles);
				SerializeBundles(bundleSerializeInfos, bw);
			}
		}

		public void SaveBinarySimple(Stream output)
		{
			using (BinaryWriter bw = new BinaryWriter(output))
			{
				GenerateAssetsSerilizeIndex(m_Assets.Values);
				GenerateBundlesSerilizeIndex(m_Bundles);

				SerializeAssets(m_Assets.Values, bw);
				SerializeBundles(m_Bundles, bw);
			}
		}

		public void LoadBinary(Stream input)
		{
			using (BinaryReader br = new BinaryReader(input))
			{
				List<AssetInfo> assetsMap = LoadAssets(br);
				LoadBundles(br, assetsMap);
			}
		}

		public void SaveBinary(string filePath)
		{
			string dir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			using (FileStream fs = new FileStream(filePath, FileMode.Create))
			{
				SaveBinary(fs);
			}
		}

		public void LoadBinary(string filePath)
		{
			if (File.Exists(filePath))
			{
				using (FileStream fs = new FileStream(filePath, FileMode.Open))
				{
					LoadBinary(fs);
				}
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
			public string name;
			public string variantName;
			public BundleInfo.BundleType bundleType;
			public bool standalone ;
			public int refersHashCode;

			public string mainAsset;
			public List<string> assets;
			public List<string> refers;
			public List<string> dependencies;
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
			bundleJsonInfo.name = bundleInfo.name;
			bundleJsonInfo.variantName = bundleInfo.variantName;
			bundleJsonInfo.bundleType = bundleInfo.bundleType;
			bundleJsonInfo.standalone = bundleInfo.IsStandalone();
			bundleJsonInfo.refersHashCode = bundleInfo.refersHashCode;
			bundleJsonInfo.mainAsset = bundleInfo.mainAssetPath;
			bundleJsonInfo.assets = new List<string>();
			bundleJsonInfo.dependencies = new List<string>();
			bundleJsonInfo.refers = new List<string>();

			foreach (var asset in bundleInfo.assets)
			{
				bundleJsonInfo.assets.Add(asset.assetPath);
			}

			foreach (var refer in bundleInfo.refers)
			{
				bundleJsonInfo.refers.Add(refer.name);
			}

			foreach (var dep in bundleInfo.dependencies)
			{
				bundleJsonInfo.dependencies.Add(dep.name);
			}
			return bundleJsonInfo;
		}

		public void SetupAssets(List<AssetJsonInfo> assets)
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

		public void SetupBundles(List<BundleJsonInfo> bundles)
		{
			CleanBundles();

			if (bundles == null)
			{
				return;
			}

			foreach (var bundleJson in bundles)
			{
				BundleInfo bundle = CreateBundle(bundleJson.name, bundleJson.variantName);
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
				BundleInfo bundle = GetBundle(bundleJson.name);

				foreach (var depName in bundleJson.dependencies)
				{
					BundleInfo depBundle = GetBundle(depName);
					bundle.AddDependencyOnly(depBundle);
				}

				foreach (var referName in bundleJson.refers)
				{
					BundleInfo referBundle = GetBundle(referName);
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
			SetupAssets(data.assets);
			SetupBundles(data.bundles);
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
			if (File.Exists(jsonFile))
			{
				string jsonStr = File.ReadAllText(jsonFile);
				DeserializeFromJson(jsonStr);
			}
		}
		#endregion //Load Save Json

		#region Path
		public string GetBinaryAssetBundleSavePath()
		{
			return Path.Combine(Application.dataPath, "../", DataSaveDir, AssetAndBundleDataName + BinaryExtName);
		}

		public string GetJsonAssetBundleSavePath()
		{
			return Path.Combine(Application.dataPath, "../", DataSaveDir, AssetAndBundleDataName + JsonExtName);
		}
		public string GetBinaryBundleSavePath()
		{
			return Path.Combine(Application.dataPath, "../", DataSaveDir, BundleDataName + BinaryExtName);
		}

		#endregion //Path
	}
}
