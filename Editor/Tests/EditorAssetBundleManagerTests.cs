﻿using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AssetBundleBuilder;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline;
using System.Reflection;

namespace AssetBundleBuilder.Tests
{
    public class EditorAssetBundleManagerTests
	{
		private EditorAssetBundleManager m_AssetManager = null;

		//run once
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Debug.Log("OneTimeSetUp");
			m_AssetManager = new EditorAssetBundleManager();
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			Debug.Log("OneTimeTearDown");
			m_AssetManager.Clean();
		}

		[SetUp]
		public void SetUp()
		{
			Debug.Log("Setup");
			m_AssetManager.Clean();
		}

		[TearDown]
		public void TearDown()
		{
			Debug.Log("TearDown");
		}
		
		[Test]
        public void CreateAssetTest()
        {
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			m_AssetManager.CreateAsset(assetPath);
			AssetInfo assetNode = m_AssetManager.GetAsset(assetPath);
			Assert.NotNull(assetNode);
		}

		[Test]

		public void CollectAssetDepsTest()
		{
			string testAssets = "Assets/ArtResources/Tests";

			DateTime start = DateTime.Now;

			//get all textures
			string[] textureFiles = Directory.GetFiles(testAssets, "*.png", SearchOption.AllDirectories);
			foreach (var f in textureFiles)
			{
				m_AssetManager.CreateAsset( f);
			}

			string[] matFiles = Directory.GetFiles(testAssets, "*.mat", SearchOption.AllDirectories);
			foreach (var f in matFiles)
			{
				m_AssetManager.CreateAsset(f);
			}

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			foreach (var f in prefabFiles)
			{
				m_AssetManager.CreateAsset(f);
			}
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("create assets used:{0}", used);
			//Debug.LogFormat("asset count:{0}", m_AssetManager.assets.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, textureFiles.Length + matFiles.Length + prefabFiles.Length);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetDependencies();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets direct deps used:{0}", used);

		}

		[Test]
		public void CollectAssetDeps2Test()
		{
			string testAssets = "Assets/ArtResources/Tests";

			object locker = new object();
			string appPath =  FileSystem.applicationPath;

			DateTime start = DateTime.Now;

			Thread t1 = new Thread(() =>
			 {
				 //get all textures
				 string[] textureFiles = Directory.GetFiles(testAssets, "*.png", SearchOption.AllDirectories);
				 foreach (var f in textureFiles)
				 {
					 AssetInfo node=  m_AssetManager.CreateAssetInfo(f);
					 lock (locker)
					 {
						 m_AssetManager.assets[node.assetPath] = node;
					 }
				 }
			 });

			Thread t2 = new Thread(() =>
			{
				string[] matFiles = Directory.GetFiles(testAssets, "*.mat", SearchOption.AllDirectories);
				foreach (var f in matFiles)
				{
					AssetInfo node = m_AssetManager.CreateAssetInfo(f);
					lock (locker)
					{
						m_AssetManager.assets[node.assetPath] = node;
					}
				}
			});

			Thread t3 = new Thread(() =>
			{
				string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
				foreach (var f in prefabFiles)
				{
					AssetInfo node = m_AssetManager.CreateAssetInfo(f);
					lock (locker)
					{
						m_AssetManager.assets[node.assetPath] = node;
					}
				}
			});

			t1.Start();
			t2.Start();
			t3.Start();

			t1.Join();
			t2.Join();
			t3.Join();

			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("create assets used:{0}", used);
			//Debug.LogFormat("asset count:{0}", m_AssetManager.assets.Count);
			Assert.Greater(m_AssetManager.assets.Count, 0);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetDependencies();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets direct deps used:{0}", used);
		}


		[Test]

		public void CollectAssetAllDepsTest()
		{
			string testAssets = "Assets/ArtResources/Tests";

			DateTime start = DateTime.Now;

			//get all textures
			string[] textureFiles = Directory.GetFiles(testAssets, "*.png", SearchOption.AllDirectories);
			foreach (var f in textureFiles)
			{
				m_AssetManager.CreateAsset(f);
			}

			string[] matFiles = Directory.GetFiles(testAssets, "*.mat", SearchOption.AllDirectories);
			foreach (var f in matFiles)
			{
				m_AssetManager.CreateAsset(f);
			}

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			foreach (var f in prefabFiles)
			{
				m_AssetManager.CreateAsset(f);
			}
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("create assets used:{0}", used);
			//Debug.LogFormat("asset count:{0}", m_AssetManager.assets.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, textureFiles.Length + matFiles.Length + prefabFiles.Length);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetDependencies();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets direct deps used:{0}", used);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetAllDependencies();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets all deps used:{0}", used);

			string assetFile = prefabFiles[0];
			AssetInfo node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath,assetFile));
			ShowAssetNode(node);

			assetFile = prefabFiles[1];
			node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath, assetFile));
			ShowAssetNode(node);

			assetFile = prefabFiles[2];
			node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath, assetFile));
			ShowAssetNode(node);
		}

		[Test]

		public void CollectAssetAllDeps2Test()
		{
			string testAssets = "Assets/ArtResources/Tests";

			DateTime start = DateTime.Now;

			//get all textures
			string[] textureFiles = Directory.GetFiles(testAssets, "*.png", SearchOption.AllDirectories);
			foreach (var f in textureFiles)
			{
				m_AssetManager.CreateAsset(f);
			}

			string[] matFiles = Directory.GetFiles(testAssets, "*.mat", SearchOption.AllDirectories);
			foreach (var f in matFiles)
			{
				m_AssetManager.CreateAsset(f);
			}

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			foreach (var f in prefabFiles)
			{
				m_AssetManager.CreateAsset(f);
			}
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("create assets used:{0}", used);
			//Debug.LogFormat("asset count:{0}", m_AssetManager.assets.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, textureFiles.Length+matFiles.Length+prefabFiles.Length);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetDependencies();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets direct deps used:{0}", used);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetAllDependencies2();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets all deps 2 used:{0}", used);

			string assetFile = prefabFiles[0];
			AssetInfo node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath, assetFile));
			ShowAssetNode(node);

			assetFile = prefabFiles[1];
			node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath, assetFile));
			ShowAssetNode(node);

			assetFile = prefabFiles[2];
			node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath, assetFile));
			ShowAssetNode(node);
		}

		private void ShowAssetNode(AssetInfo node)
		{
			Debug.LogFormat("{0},{1},{2}", node.assetPath, node.dependencies.Count, node.allDependencies.Count);
			string s = "";
			if (node.dependencies != null)
			{
				foreach (var dep in node.dependencies)
				{
					s+=string.Format("Dep:{0}\n", dep.assetPath);
				}
			}

			Debug.Log(s);

			s = "";
			if (node.allDependencies != null)
			{
				foreach (var dep in node.allDependencies)
				{
					s += string.Format("AllDep:{0}\n", dep.assetPath);
				}
			}
			Debug.Log(s);
		}

		[Test]
		public void CreateBundleTest()
		{
			m_AssetManager.CreateBundle("MyBundle");
			BundleInfo bundleNode = m_AssetManager.GetBundle("MyBundle");
			Assert.NotNull(bundleNode);
		}

		[Test]
		public void AddAssetsToBundleTest()
		{
			m_AssetManager.CreateBundle("MyBundle");
			BundleInfo bundleNode = m_AssetManager.GetBundle("MyBundle");
			Assert.NotNull(bundleNode);

			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetInfo assetNode = m_AssetManager.CreateAsset(assetPath);
			bundleNode.AddAsset(assetNode);
			Assert.AreEqual(bundleNode.assets.Count, 1);
		}

		[Test]
		public void BundleDependenciesTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetInfo assetNode = m_AssetManager.CreateAsset(assetPath);
			m_AssetManager.RefreshAssetDependencies(assetNode);

			m_AssetManager.CreateBundle("TestPrefab");
			BundleInfo bundleNode = m_AssetManager.GetBundle("TestPrefab");

			bundleNode.AddAsset(assetNode);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshBundleDependencies(bundleNode);

			Assert.AreEqual(assetNode.dependencies.Count, bundleNode.dependencies.Count);
			Assert.AreEqual(assetNode.refers.Count, bundleNode.refers.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
		}

		[Test]
		public void BundleDependencies2Test()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetInfo assetNode1 = m_AssetManager.CreateAsset(assetPath);

			assetPath = "Assets/ArtResources/Prefabs/MyPrefab.prefab";
			AssetInfo assetNode2 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();
	

			BundleInfo bundleNode1 = m_AssetManager.CreateBundle("TestPrefab");
			bundleNode1.AddAsset(assetNode1);

			BundleInfo bundleNode2 = m_AssetManager.CreateBundle("MyPrefab");
			bundleNode2.AddAsset(assetNode2);

			m_AssetManager.CreateBundleForAllAssets();

			m_AssetManager.RefreshAllBundleDependencies();

			Assert.AreEqual(assetNode1.dependencies.Count, bundleNode1.dependencies.Count);
			Assert.AreEqual(assetNode1.refers.Count, bundleNode1.refers.Count);

			Assert.AreEqual(assetNode2.dependencies.Count, bundleNode2.dependencies.Count);
			Assert.AreEqual(assetNode2.refers.Count, bundleNode2.refers.Count);

			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
		}

		[Test]
		public void BundleDependenciesCircleReferenceTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/CircelRefs/BPreab.prefab";
			//string[] deps = UnityEditor.AssetDatabase.GetDependencies(assetPath, false);
			//foreach (var d in deps)
			//{
			//	Debug.LogFormat("d1:{0}", d);
			//}
			//deps = UnityEditor.AssetDatabase.GetDependencies(assetPath, true);
			//foreach (var d in deps)
			//{
			//	Debug.LogFormat("d2:{0}", d);
			//}

			//GameObject obj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			//UnityEngine.Object[] deps2 = UnityEditor.EditorUtility.CollectDependencies(new UnityEngine.Object[] { obj });
			//foreach (var d in deps2)
			//{
			//	Debug.LogFormat("d3:{0}", UnityEditor.AssetDatabase.GetAssetPath(d));
			//}

			assetPath = "Assets/ArtResources/Prefabs/CircelRefs/APreab.prefab";
			AssetInfo assetNode = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleInfo bundleNode = m_AssetManager.CreateBundle("APreab");
			bundleNode.AddAsset(assetNode);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			Assert.AreEqual(assetNode.dependencies.Count, bundleNode.dependencies.Count);
			Assert.AreEqual(assetNode.refers.Count, bundleNode.refers.Count);

			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
		}


		[Test]
		public void BundleRelationsTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetInfo assetNode = m_AssetManager.CreateAsset(assetPath);
			m_AssetManager.RefreshAssetDependencies(assetNode);

			m_AssetManager.CreateBundle("TestPrefab");
			BundleInfo bundleNode = m_AssetManager.GetBundle("TestPrefab");


			bundleNode.AddAsset(assetNode);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshBundleRelations(bundleNode);

			Assert.AreEqual(assetNode.dependencies.Count, bundleNode.dependencies.Count);
			Assert.AreEqual(assetNode.refers.Count, bundleNode.refers.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
		}

		[Test]
		public void BundleRelationsCircleReferenceTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/CircelRefs/APreab.prefab";
			AssetInfo assetNode = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleInfo bundleNode = m_AssetManager.CreateBundle("APreab");
			bundleNode.AddAsset(assetNode);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleRelations();

			Assert.AreEqual(assetNode.dependencies.Count, bundleNode.dependencies.Count);
			Assert.AreEqual(assetNode.refers.Count, bundleNode.refers.Count);

			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
		}
		[Test]
		public void ImportTest()
		{
			string testAssets = "Assets/ArtResources/Tests";

			DateTime start = DateTime.Now;
			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			List<AssetInfo> assets = new List<AssetInfo>();
			foreach (var f in prefabFiles)
			{
				AssetInfo assetNode = m_AssetManager.CreateAsset(f);
				//直接使用的资源，可以寻址
				assetNode.addressable = true;
				assets.Add(assetNode);
			}
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("create assets used:{0}", used);
			start = DateTime.Now;

			m_AssetManager.RefreshAllAssetDependencies();

			used = DateTime.Now - start;
			Debug.LogFormat("RefreshAllAssetDependencies used:{0}", used);
			start = DateTime.Now;

			//create bundle from assets
			foreach (var iter in m_AssetManager.assets)
			{
				BundleInfo bundleNode = m_AssetManager.CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				bundleNode.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}

			used = DateTime.Now - start;
			Debug.LogFormat("Create Bundle used:{0}", used);
			start = DateTime.Now;

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			used = DateTime.Now - start;
			Debug.LogFormat("Refresh Budnle Deps Bundle used:{0}", used);
			start = DateTime.Now;

			Debug.LogFormat("Asset Count:{0},Bundle Count:{1}", m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
		}

		[Test]
		public void CombileTest1()
		{
			string assetPath = "Assets/ArtResources/Prefabs/CircelRefs/APreab.prefab";
			AssetInfo assetNode = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleInfo bundleNode = m_AssetManager.CreateBundle("APreab");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode);
			bundleNode.AddAsset(assetNode);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			m_AssetManager.Combine();

			Assert.AreEqual(1, m_AssetManager.bundles.Count);
		}

		[Test]
		public void CombileTest2()
		{
			string assetPath = "Assets/ArtResources/Prefabs/CircelRefs/APreab.prefab";
			AssetInfo assetNode = m_AssetManager.CreateAsset(assetPath);

			assetPath = "Assets/ArtResources/Prefabs/CircelRefs/BPreab.prefab";
			AssetInfo assetNode2 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleInfo bundleNode = m_AssetManager.CreateBundle("APreab");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode);
			bundleNode.AddAsset(assetNode);

			bundleNode = m_AssetManager.CreateBundle("BPreab");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode2);
			bundleNode.AddAsset(assetNode2);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			m_AssetManager.Combine();

			Assert.AreEqual(2, m_AssetManager.bundles.Count);
		}

		[Test]
		public void CombileSameRefTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/SameRefers/SameRefA.prefab";
			AssetInfo assetNode = m_AssetManager.CreateAsset(assetPath);

			assetPath = "Assets/ArtResources/Prefabs/SameRefers/SameRefB.prefab";
			AssetInfo assetNode2 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleInfo bundleNode = m_AssetManager.CreateBundle("SameRefA");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode);
			bundleNode.AddAsset(assetNode);

			bundleNode = m_AssetManager.CreateBundle("SameRefB");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode2);
			bundleNode.AddAsset(assetNode2);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			m_AssetManager.Combine();

			Assert.AreEqual(3, m_AssetManager.bundles.Count);
		}

		[Test]
		public void OptimizerTest()
		{
			string testAssets = "Assets/ArtResources/Tests";
			DateTime start = DateTime.Now;
			
			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			List<AssetInfo> assets = new List<AssetInfo>();
			foreach (var f in prefabFiles)
			{
				AssetInfo assetNode = m_AssetManager.CreateAsset(f);
				//直接使用的资源，可以导址
				assetNode.addressable = true;
				assets.Add(assetNode);
			}

			//used
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("import assets used:{0}", used);
			start = DateTime.Now;

			m_AssetManager.RefreshAllAssetDependencies();

			//used
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets deps used:{0}", used);
			start = DateTime.Now;

			//create bundle from assets
			foreach (var iter in m_AssetManager.assets)
			{
				BundleInfo bundleNode = m_AssetManager.CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				bundleNode.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}

			//used
			used = DateTime.Now - start;
			Debug.LogFormat("create bundle used:{0}", used);
			start = DateTime.Now;

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			//used
			used = DateTime.Now - start;
			Debug.LogFormat("refresh bundles deps used:{0}", used);


			Debug.LogFormat("Before optimze Asset Count:{0},Bundle Count:{1}", m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
			
			start = DateTime.Now;
			
			m_AssetManager.Combine();

			used = DateTime.Now - start;
			Debug.LogFormat("Combine bundle used:{0}", used);

			Debug.LogFormat("After optimze Asset Count:{0},Bundle Count:{1}", m_AssetManager.assets.Count, m_AssetManager.bundles.Count);

			int n = 0;
			List<BundleInfo> bundles = new List<BundleInfo>();
			foreach (var bundle in m_AssetManager.bundles)
			{
				if (bundle.refers.Count > 1)
				{
					++n;
					bundles.Add(bundle);
				}	
			}
			Debug.LogFormat("More refer bundle count:{0}", n);

			List<EditorAssetBundleManager.BundleJsonInfo> bundleJsons = new List<EditorAssetBundleManager.BundleJsonInfo>();
			foreach (var bundle in bundles)
			{
				EditorAssetBundleManager.BundleJsonInfo bundleJson = m_AssetManager.BundleInfoToJsonInfo(bundle);
				bundleJsons.Add(bundleJson);
			}
			EditorAssetBundleManager.AssetBundleJsonInfo asbInfo = new EditorAssetBundleManager.AssetBundleJsonInfo();
			asbInfo.bundles = bundleJsons;

			string jsonStr =  JsonUtility.ToJson(asbInfo, true);
			File.WriteAllText("t.json", jsonStr);
		}

		[Test]
		public void SerializeAssetTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetInfo assetPrefab = m_AssetManager.CreateAssetInfo(assetPath);

			string assetMatPath = "Assets/ArtResources/Materials/MyMaterial.mat";
			AssetInfo assetMat = m_AssetManager.CreateAssetInfo(assetMatPath);

			assetPrefab.AddDependency(assetMat);

			MemoryStream ms = new MemoryStream();
			using (BinaryWriter bw = new BinaryWriter(ms))
			{
				EditorAssetBundleManager.SerializeAsset(assetPrefab,bw);
			}

			AssetSerializeInfo deserializeInfo = null;
			MemoryStream rms = new MemoryStream(ms.GetBuffer());
			using (BinaryReader br = new BinaryReader(rms))
			{
				deserializeInfo = EditorAssetBundleManager.DeserializeAsset(br);
			}

			Assert.NotNull(deserializeInfo);
			Assert.AreEqual(assetPrefab.assetPath,deserializeInfo.asset.assetPath);
			Assert.AreEqual(assetPrefab.fileSize, deserializeInfo.asset.fileSize);
			Assert.AreEqual(assetPrefab.assetType, deserializeInfo.asset.assetType);
			Assert.AreEqual(assetPrefab.dependencies.Count, deserializeInfo.dependencies.Count);
		}

		[Test]
		public void SaveLoadTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetInfo assetNode1 = m_AssetManager.CreateAsset(assetPath);

			assetPath = "Assets/ArtResources/Prefabs/MyPrefab.prefab";
			AssetInfo assetNode2 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleInfo bundleNode1 = m_AssetManager.CreateBundle("TestPrefab");
			bundleNode1.SetMainAsset(assetNode1);
			bundleNode1.AddAsset(assetNode1);

			BundleInfo bundleNode2 = m_AssetManager.CreateBundle("MyPrefab");
			bundleNode2.SetMainAsset(assetNode2);
			bundleNode2.AddAsset(assetNode2);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			MemoryStream wms = new MemoryStream();
			m_AssetManager.SaveBinary(wms);
			byte[] data = wms.GetBuffer();
	
			EditorAssetBundleManager editorAssetManager = new EditorAssetBundleManager();
			
			MemoryStream rms = new MemoryStream(data);
			editorAssetManager.LoadBinary(rms);

			Assert.AreEqual(m_AssetManager.assets.Count,editorAssetManager.assets.Count);
			Assert.AreEqual(m_AssetManager.bundles.Count, editorAssetManager.bundles.Count);

			foreach (var iter in m_AssetManager.assets)
			{
				var asset = iter.Value;
				var otherAsset = editorAssetManager.GetAsset(iter.Key);
				Assert.NotNull(otherAsset);
				Assert.AreEqual(asset.fileSize, otherAsset.fileSize);
				Assert.AreEqual(asset.assetType, otherAsset.assetType);

				Assert.AreEqual(asset.dependencies.Count, otherAsset.dependencies.Count);
				Assert.AreEqual(asset.refers.Count, otherAsset.refers.Count);
			}

			int i = 0;
			foreach (var bundle in m_AssetManager.bundles)
			{
				var otherBundle = editorAssetManager.bundles[i++];
				Assert.NotNull(otherBundle);
				if (string.IsNullOrEmpty(bundle.name))
				{
					Assert.AreEqual(true, string.IsNullOrEmpty(otherBundle.name));
				}
				else
				{
					Assert.AreEqual(bundle.name, otherBundle.name);
				}

				Assert.AreEqual(bundle.bundleType, otherBundle.bundleType);
				Assert.AreEqual(bundle.IsStandalone(), otherBundle.IsStandalone());
				Assert.AreEqual(bundle.refersHashCode, otherBundle.refersHashCode);
				Assert.AreEqual(bundle.mainAssetPath, otherBundle.mainAssetPath);
				Assert.AreEqual(bundle.dependencies.Count, otherBundle.dependencies.Count);
				Assert.AreEqual(bundle.refers.Count, otherBundle.refers.Count);
			}
		}

		[Test]
		public void SaveBundleTest()
		{
			string testAssets = "Assets/ArtResources/Prefabs";

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			foreach (var f in prefabFiles)
			{
				AssetInfo assetNode = m_AssetManager.GetOrCreateAsset(f);
				//直接使用的资源，可以寻址
				assetNode.addressable = true;
			}

			m_AssetManager.RefreshAllAssetDependencies();
			m_AssetManager.RefreshAllAssetAllDependencies();

			m_AssetManager.CleanBundles();

			//create bundle from assets
			foreach (var iter in m_AssetManager.assets)
			{
				BundleInfo bundleNode = m_AssetManager.CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				bundleNode.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}
			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			m_AssetManager.Combine();
			m_AssetManager.RefreshAllBundlesName();

			string savePath = m_AssetManager.GetBinaryBundleSavePath();
			m_AssetManager.SaveBundles(savePath);

			string saveJsonPath = m_AssetManager.GetJsonAssetBundleSavePath();
			m_AssetManager.SaveToJson(saveJsonPath);
		}

		[Test]
		public void BinarySaveLoadTimeTest()
		{
			string testAssets = "Assets/ArtResources/Tests";

			EditorAssetBundleManager assetManager = new EditorAssetBundleManager();

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			List<AssetInfo> assets = new List<AssetInfo>();
			foreach (var f in prefabFiles)
			{
				AssetInfo assetNode = assetManager.CreateAsset(f);
				//直接使用的资源，可以寻址
				assetNode.addressable = true;
				assets.Add(assetNode);
			}

			assetManager.RefreshAllAssetDependencies();

			//create bundle from assets
			foreach (var iter in assetManager.assets)
			{
				BundleInfo bundleNode = assetManager.CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				bundleNode.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}

			assetManager.CreateBundleForAllAssets();
			assetManager.RefreshAllBundleDependencies();

			DateTime start = DateTime.Now;

			string savePath = Path.Combine(Application.dataPath, "../tttt.bin");
			assetManager.SaveBinary(savePath);
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("Save binary used:{0}", used);

			EditorAssetBundleManager assetManagerLoader = new EditorAssetBundleManager();

			start = DateTime.Now;
			assetManagerLoader.LoadBinary(savePath);
			used = DateTime.Now - start;
			Debug.LogFormat("Load binary used:{0}", used);

			File.Delete(savePath);
		}

		[Test]
		public void BinarySaveLoadBundlesTimeTest()
		{
			string testAssets = "Assets/ArtResources/Tests";

			EditorAssetBundleManager assetManager = new EditorAssetBundleManager();

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			List<AssetInfo> assets = new List<AssetInfo>();
			foreach (var f in prefabFiles)
			{
				AssetInfo assetNode = assetManager.CreateAsset(f);
				//直接使用的资源，可以寻址
				assetNode.addressable = true;
				assets.Add(assetNode);
			}

			assetManager.RefreshAllAssetDependencies();

			//create bundle from assets
			foreach (var iter in assetManager.assets)
			{
				BundleInfo bundleNode = assetManager.CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				bundleNode.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}

			assetManager.CreateBundleForAllAssets();
			assetManager.RefreshAllBundleDependencies();


			DateTime start = DateTime.Now;

			string savePath = Path.Combine(Application.dataPath, "../AssetDatabase/Bundes.bin");
			assetManager.SaveBundles(savePath);
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("Save bundes used:{0}", used);

			EditorAssetBundleManager assetManagerLoader = new EditorAssetBundleManager();

			start = DateTime.Now;
			assetManagerLoader.LoadBundles(savePath);
			used = DateTime.Now - start;
			Debug.LogFormat("Load bundes used:{0}", used);

			File.Delete(savePath);
		}

		[Test]
		public void JsonSaveLoadTimeTest()
		{
			string testAssets = "Assets/ArtResources/Tests";

			EditorAssetBundleManager assetManager = new EditorAssetBundleManager();

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			List<AssetInfo> assets = new List<AssetInfo>();
			foreach (var f in prefabFiles)
			{
				AssetInfo assetNode = assetManager.CreateAsset(f);
				//直接使用的资源，可以寻址
				assetNode.addressable = true;
				assets.Add(assetNode);
			}

			assetManager.RefreshAllAssetDependencies();

			//create bundle from assets
			foreach (var iter in assetManager.assets)
			{
				BundleInfo bundleNode = assetManager.CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				bundleNode.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}

			assetManager.CreateBundleForAllAssets();
			assetManager.RefreshAllBundleDependencies();

			DateTime start = DateTime.Now;

			string savePath = Path.Combine(Application.dataPath, "../tttt.json");
			assetManager.SaveToJson(savePath);
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("Save json used:{0}", used);

			EditorAssetBundleManager assetManagerLoader = new EditorAssetBundleManager();

			start = DateTime.Now;
			assetManagerLoader.LoadFromJson(savePath);
			used = DateTime.Now - start;
			Debug.LogFormat("Load json used:{0}", used);

			File.Delete(savePath);
		}

		[Test]
		public void SimpleBuildBundlesTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestMatProp.prefab";
			AssetInfo assetNode1 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleInfo bundleNode1 = m_AssetManager.CreateBundle("TestMatProp");
			bundleNode1.SetMainAsset(assetNode1);
			bundleNode1.AddAsset(assetNode1);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();
			m_AssetManager.RefreshAllBundlesName();

			BuildInfo buildInfo = new BuildInfo();

			buildInfo.outputDirectory = Path.Combine(Application.dataPath, "../AssetBundles", EditorUserBuildSettings.activeBuildTarget.ToString());
			buildInfo.options = BuildAssetBundleOptions.ChunkBasedCompression;
			buildInfo.buildTarget = EditorUserBuildSettings.activeBuildTarget;
			buildInfo.version = "1.0";

			m_AssetManager.BuildAssetBundles(buildInfo);
		}

		[Test]
		public void SimpleBuildBundlesTest2()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestMatProp.prefab";
			AssetInfo assetNode1 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleInfo bundleNode1 = m_AssetManager.CreateBundle("TestMatProp");
			bundleNode1.SetMainAsset(assetNode1);
			bundleNode1.AddAsset(assetNode1);

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();
			m_AssetManager.RefreshAllBundlesName();

			BuildInfo buildInfo = new BuildInfo();

			buildInfo.outputDirectory = Path.Combine(Application.dataPath, "../AssetBundles", EditorUserBuildSettings.activeBuildTarget.ToString());
			buildInfo.options = BuildAssetBundleOptions.ChunkBasedCompression;
			buildInfo.buildTarget = EditorUserBuildSettings.activeBuildTarget;
			buildInfo.version = "1.0";

			m_AssetManager.BuildAssetBundlesPipline(buildInfo);
		}

		private void PrepareAssets()
		{
			string testAssets = "Assets/ArtResources/Tests";
			DateTime start = DateTime.Now;

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			List<AssetInfo> assets = new List<AssetInfo>();
			int n = 50000;
			int i = 0;
			foreach (var f in prefabFiles)
			{

				if (++i > n)
				{
					break;
				}
				AssetInfo assetNode = m_AssetManager.CreateAsset(f);
				//直接使用的资源，可以导址
				assetNode.addressable = true;
				assets.Add(assetNode);
			}

			//used
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("import assets used:{0}", used);
			start = DateTime.Now;

			m_AssetManager.RefreshAllAssetDependencies();

			//used
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets deps used:{0}", used);
			start = DateTime.Now;

			//create bundle from assets
			foreach (var iter in m_AssetManager.assets)
			{
				BundleInfo bundleNode = m_AssetManager.CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				bundleNode.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}

			//used
			used = DateTime.Now - start;
			Debug.LogFormat("create bundle used:{0}", used);
			start = DateTime.Now;

			m_AssetManager.CreateBundleForAllAssets();
			m_AssetManager.RefreshAllBundleDependencies();

			//used
			used = DateTime.Now - start;
			Debug.LogFormat("refresh bundles deps used:{0}", used);


			Debug.LogFormat("Before optimze Asset Count:{0},Bundle Count:{1}", m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);

			start = DateTime.Now;

			m_AssetManager.Combine();

			used = DateTime.Now - start;
			Debug.LogFormat("Combine bundle used:{0}", used);

			Debug.LogFormat("After optimze Asset Count:{0},Bundle Count:{1}", m_AssetManager.assets.Count, m_AssetManager.bundles.Count);

			start = DateTime.Now;
			m_AssetManager.RefreshAllBundlesName();

			used = DateTime.Now - start;
			Debug.LogFormat("RefreshAllBundlesName used:{0}", used);
		}

		[Test]
		public void BuildBundlesTest()
		{
			DateTime start = DateTime.Now;

			PrepareAssets();

			BuildInfo buildInfo = new BuildInfo();

			buildInfo.outputDirectory = Path.Combine(Application.dataPath, "../AssetBundles", EditorUserBuildSettings.activeBuildTarget.ToString());
			buildInfo.options = BuildAssetBundleOptions.ChunkBasedCompression;
			buildInfo.buildTarget = EditorUserBuildSettings.activeBuildTarget;
			buildInfo.version = "1.0";

			start = DateTime.Now;
			m_AssetManager.BuildAssetBundles(buildInfo);
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("Build AssetBundle used:{0}", used);
		}

		[Test]
		public void BuildBundlesPiplineTest()
		{
			DateTime start = DateTime.Now;

			PrepareAssets();

			BuildInfo buildInfo = new BuildInfo();

			buildInfo.outputDirectory = Path.Combine(Application.dataPath, "../AssetBundlesPipline", EditorUserBuildSettings.activeBuildTarget.ToString());
			buildInfo.options = BuildAssetBundleOptions.ChunkBasedCompression;
			buildInfo.buildTarget = EditorUserBuildSettings.activeBuildTarget;
			buildInfo.version = "1.0";

			start = DateTime.Now;
			m_AssetManager.BuildAssetBundlesPipline(buildInfo);
			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("Build AssetBundle pipline used:{0}", used);
		}

		[Test]
		public void WriteSerializedFileTest()
		{
			string outputFolder = Path.Combine(Application.dataPath, "../AssetBundlesPipline", EditorUserBuildSettings.activeBuildTarget.ToString());

			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			string bundleName = "TestPrefab";

			BundleBuildParameters buildParams = new BundleBuildParameters(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.selectedBuildTargetGroup, outputFolder);


			AssetLoadInfo assetLoadInfo = CreateAssetLoadInfo(assetPath, EditorUserBuildSettings.activeBuildTarget);
			List<AssetLoadInfo> assetLoadInfos = new List<AssetLoadInfo>();
			assetLoadInfos.Add(assetLoadInfo);


			WriteCommand writeCommand = CreateWriteCommand(bundleName, assetLoadInfos, EditorUserBuildSettings.activeBuildTarget);

			BuildUsageTagSet usageSet = new BuildUsageTagSet();

			var referenceMap = new BuildReferenceMap();
			referenceMap.AddMappings(writeCommand.internalName, writeCommand.serializeObjects.ToArray());

			var bundleInfo = new AssetBundleInfo();
			bundleInfo.bundleName = bundleName;
			bundleInfo.bundleAssets = assetLoadInfos;

			ContentBuildInterface.WriteSerializedFile(outputFolder, writeCommand, buildParams.GetContentBuildSettings(), new BuildUsageTagGlobal(), usageSet, referenceMap, bundleInfo);

			//Type type = typeof(ContentBuildInterface);
			//MethodInfo m = type.GetMethod("WriteSerializedFileAssetBundle", BindingFlags.Static|BindingFlags.NonPublic);


			////if (m != null)
			////{
			////	object[] args = new object[]
			////	{
			////			 outputFolder, writeCommand, buildParams.GetContentBuildSettings(), new BuildUsageTagGlobal(), usageSet, referenceMap, bundleInfo
			////	};
			////	m.Invoke(null, args);
			////}

			//Thread t = new Thread(() =>
			//{
			//	if (m != null)
			//	{
			//		object[] args = new object[]
			//		{
			//			 outputFolder, writeCommand, buildParams.GetContentBuildSettings(), new BuildUsageTagGlobal(), usageSet, referenceMap, bundleInfo
			//		};
			//		m.Invoke(null, args);
			//	}
			//});

			//t.Start();
		}

		private AssetLoadInfo CreateAssetLoadInfo(string assetPath,BuildTarget buildTarget)
		{
			GUID assetGuid = new GUID(AssetDatabase.AssetPathToGUID(assetPath));

			AssetLoadInfo assetInfo = new AssetLoadInfo()
			{
				asset = assetGuid,
				address = assetPath
			};
			var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(assetGuid, buildTarget);
			assetInfo.includedObjects = new List<ObjectIdentifier>(includedObjects);

			var referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, buildTarget, null);
			assetInfo.referencedObjects = new List<ObjectIdentifier>(referencedObjects);
			FilterReferencesForAsset(assetInfo.referencedObjects);
			return assetInfo;
		}

		private WriteCommand CreateWriteCommand(string bundleName, List<AssetLoadInfo> assets, BuildTarget buildTarget)
		{
			HashSet<ObjectIdentifier> allObjs = new HashSet<ObjectIdentifier>();
			foreach (var assetLoadInfo in assets)
			{
				allObjs.UnionWith(assetLoadInfo.includedObjects);
				allObjs.UnionWith(assetLoadInfo.referencedObjects);
			}

			List<SerializationInfo> serializationInfos = new List<SerializationInfo>();
			foreach (var objId in allObjs)
			{
				SerializationInfo serializationInfo = new SerializationInfo();
				serializationInfo.serializationObject = objId;
				serializationInfo.serializationIndex = SerializationIndexFromObjectIdentifier(objId);

				serializationInfos.Add(serializationInfo);
			}

			string internalName = string.Format("archive:/{0}/{0}", GenerateInternalFileName(bundleName));
			WriteCommand command = new WriteCommand();
			command.internalName = internalName;
			command.fileName = Path.GetFileName(internalName);
			command.serializeObjects = serializationInfos;
			return command;
		}

		private string GenerateInternalFileName(string name)
		{
			var hash = HashingMethods.Calculate(name).ToString();
			return string.Format("CAB-{0}", hash);
		}

		private long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
		{
			byte[] assetHash = HashingMethods.Calculate(objectID.guid, objectID.filePath).ToBytes();
			byte[] objectHash = HashingMethods.Calculate(objectID).ToBytes();

			var assetVal = BitConverter.ToUInt64(assetHash, 0);
			var objectVal = BitConverter.ToUInt64(objectHash, 0);
			return (long)((0xFFFFFFFF00000000 & assetVal) | (0x00000000FFFFFFFF & (objectVal ^ assetVal)));
		}

		private void FilterReferencesForAsset(List<ObjectIdentifier> references)
		{
			var referencesPruned = new List<ObjectIdentifier>(references.Count);
			// Remove Default Resources and Includes for Assets assigned to Bundles
			foreach (ObjectIdentifier reference in references)
			{
				if (reference.filePath.Equals("library/unity default resources", StringComparison.OrdinalIgnoreCase))
					continue;

				referencesPruned.Add(reference);
			}
			references.Clear();
			references.AddRange(referencesPruned);
		}
	}
}
