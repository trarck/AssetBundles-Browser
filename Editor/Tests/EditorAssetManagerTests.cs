using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AssetBundleBuilder;
using System.Threading;

namespace AssetBundleBuilder.Tests
{
    public class EditorAssetManagerTests
	{
		private EditorAssetManager m_AssetManager = null;

		//run once
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Debug.Log("OneTimeSetUp");
			m_AssetManager = new EditorAssetManager();
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
			AssetNode assetNode = m_AssetManager.GetAsset(assetPath);
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
			Assert.Greater(m_AssetManager.assets.Count, 100000);

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
					 AssetNode node=  m_AssetManager.CreateAssetNode(f);
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
					AssetNode node = m_AssetManager.CreateAssetNode(f);
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
					AssetNode node = m_AssetManager.CreateAssetNode(f);
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
			Assert.Greater(m_AssetManager.assets.Count, 100000);

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
			Assert.Greater(m_AssetManager.assets.Count, 100000);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetDependencies();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets direct deps used:{0}", used);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetAllDependencies();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets all deps used:{0}", used);

			string assetFile = prefabFiles[0];
			AssetNode node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath,assetFile));
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
			Assert.Greater(m_AssetManager.assets.Count, 100000);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetDependencies();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets direct deps used:{0}", used);

			start = DateTime.Now;
			m_AssetManager.RefreshAllAssetAllDependencies2();
			used = DateTime.Now - start;
			Debug.LogFormat("refresh assets all deps 2 used:{0}", used);

			string assetFile = prefabFiles[0];
			AssetNode node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath, assetFile));
			ShowAssetNode(node);

			assetFile = prefabFiles[1];
			node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath, assetFile));
			ShowAssetNode(node);

			assetFile = prefabFiles[2];
			node = m_AssetManager.GetAsset(FileSystem.Relative(FileSystem.applicationPath, assetFile));
			ShowAssetNode(node);
		}

		private void ShowAssetNode(AssetNode node)
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
			BundleNode bundleNode = m_AssetManager.GetBundle("MyBundle");
			Assert.NotNull(bundleNode);
		}

		[Test]
		public void AddAssetsToBundleTest()
		{
			m_AssetManager.CreateBundle("MyBundle");
			BundleNode bundleNode = m_AssetManager.GetBundle("MyBundle");
			Assert.NotNull(bundleNode);

			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetNode assetNode = m_AssetManager.CreateAsset(assetPath);
			bundleNode.AddAsset(assetNode);
			Assert.AreEqual(bundleNode.assetNodes.Count, 1);
		}

		[Test]
		public void BundleDependenciesTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetNode assetNode = m_AssetManager.CreateAsset(assetPath);
			m_AssetManager.RefreshAssetDependencies(assetNode);

			m_AssetManager.CreateBundle("TestPrefab");
			BundleNode bundleNode = m_AssetManager.GetBundle("TestPrefab");

			bundleNode.AddAsset(assetNode);

			m_AssetManager.RefreshBundleDependencies(bundleNode);

			Assert.AreEqual(assetNode.dependencies.Count, bundleNode.dependencies.Count);
			Assert.AreEqual(assetNode.refers.Count, bundleNode.refers.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, 3);
			Assert.AreEqual(m_AssetManager.bundles.Count, 3);
		}

		[Test]
		public void BundleDependencies2Test()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetNode assetNode1 = m_AssetManager.CreateAsset(assetPath);

			assetPath = "Assets/ArtResources/Prefabs/MyPrefab.prefab";
			AssetNode assetNode2 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleNode bundleNode1 = m_AssetManager.CreateBundle("TestPrefab");
			bundleNode1.AddAsset(assetNode1);

			BundleNode bundleNode2 = m_AssetManager.CreateBundle("MyPrefab");
			bundleNode2.AddAsset(assetNode2);

			m_AssetManager.RefreshAllBundleDependencies();

			Assert.AreEqual(assetNode1.dependencies.Count, bundleNode1.dependencies.Count);
			Assert.AreEqual(assetNode1.refers.Count, bundleNode1.refers.Count);

			Assert.AreEqual(assetNode2.dependencies.Count, bundleNode2.dependencies.Count);
			Assert.AreEqual(assetNode2.refers.Count, bundleNode2.refers.Count);

			Assert.AreEqual(m_AssetManager.assets.Count, 4);
			Assert.AreEqual(m_AssetManager.bundles.Count, 4);
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
			AssetNode assetNode = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleNode bundleNode = m_AssetManager.CreateBundle("APreab");
			bundleNode.AddAsset(assetNode);

			m_AssetManager.RefreshAllBundleDependencies();

			Assert.AreEqual(assetNode.dependencies.Count, bundleNode.dependencies.Count);
			Assert.AreEqual(assetNode.refers.Count, bundleNode.refers.Count);

			Assert.AreEqual(m_AssetManager.assets.Count, 5);
			Assert.AreEqual(m_AssetManager.bundles.Count, 5);
		}


		[Test]
		public void BundleRelationsTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/TestPrefab.prefab";
			AssetNode assetNode = m_AssetManager.CreateAsset(assetPath);
			m_AssetManager.RefreshAssetDependencies(assetNode);

			m_AssetManager.CreateBundle("TestPrefab");
			BundleNode bundleNode = m_AssetManager.GetBundle("TestPrefab");


			bundleNode.AddAsset(assetNode);

			m_AssetManager.RefreshBundleRelations(bundleNode);

			Assert.AreEqual(assetNode.dependencies.Count, bundleNode.dependencies.Count);
			Assert.AreEqual(assetNode.refers.Count, bundleNode.refers.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, 3);
			Assert.AreEqual(m_AssetManager.bundles.Count,3);
		}

		[Test]
		public void BundleRelationsCircleReferenceTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/CircelRefs/APreab.prefab";
			AssetNode assetNode = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleNode bundleNode = m_AssetManager.CreateBundle("APreab");
			bundleNode.AddAsset(assetNode);

			m_AssetManager.RefreshAllBundleRelations();

			Assert.AreEqual(assetNode.dependencies.Count, bundleNode.dependencies.Count);
			Assert.AreEqual(assetNode.refers.Count, bundleNode.refers.Count);

			Assert.AreEqual(m_AssetManager.assets.Count, 5);
			Assert.AreEqual(m_AssetManager.bundles.Count, 5);
		}
		[Test]
		public void ImportTest()
		{
			string testAssets = "Assets/ArtResources/Tests";

			DateTime start = DateTime.Now;
			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			List<AssetNode> assets = new List<AssetNode>();
			foreach (var f in prefabFiles)
			{
				AssetNode assetNode = m_AssetManager.CreateAsset(f);
				//直接使用的资源，可以导址
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
				BundleNode bundleNode = m_AssetManager.CreateBundle(null);
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
			AssetNode assetNode = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleNode bundleNode = m_AssetManager.CreateBundle("APreab");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode);
			bundleNode.AddAsset(assetNode);

			m_AssetManager.RefreshAllBundleDependencies();

			m_AssetManager.Combine();

			Assert.AreEqual(5, m_AssetManager.assets.Count);
			Assert.AreEqual(1, m_AssetManager.bundles.Count);
		}

		[Test]
		public void CombileTest2()
		{
			string assetPath = "Assets/ArtResources/Prefabs/CircelRefs/APreab.prefab";
			AssetNode assetNode = m_AssetManager.CreateAsset(assetPath);

			assetPath = "Assets/ArtResources/Prefabs/CircelRefs/BPreab.prefab";
			AssetNode assetNode2 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleNode bundleNode = m_AssetManager.CreateBundle("APreab");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode);
			bundleNode.AddAsset(assetNode);

			bundleNode = m_AssetManager.CreateBundle("BPreab");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode2);
			bundleNode.AddAsset(assetNode2);

			m_AssetManager.RefreshAllBundleDependencies();

			m_AssetManager.Combine();

			Assert.AreEqual(5, m_AssetManager.assets.Count);
			Assert.AreEqual(2, m_AssetManager.bundles.Count);
		}

		[Test]
		public void CombileSameRefTest()
		{
			string assetPath = "Assets/ArtResources/Prefabs/SameRefers/SameRefA.prefab";
			AssetNode assetNode = m_AssetManager.CreateAsset(assetPath);

			assetPath = "Assets/ArtResources/Prefabs/SameRefers/SameRefB.prefab";
			AssetNode assetNode2 = m_AssetManager.CreateAsset(assetPath);

			m_AssetManager.RefreshAllAssetDependencies();

			BundleNode bundleNode = m_AssetManager.CreateBundle("SameRefA");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode);
			bundleNode.AddAsset(assetNode);

			bundleNode = m_AssetManager.CreateBundle("SameRefB");
			bundleNode.SetStandalone(true);
			bundleNode.SetMainAsset(assetNode2);
			bundleNode.AddAsset(assetNode2);

			m_AssetManager.RefreshAllBundleDependencies();

			m_AssetManager.Combine();

			Assert.AreEqual(6, m_AssetManager.assets.Count);
			Assert.AreEqual(3, m_AssetManager.bundles.Count);
		}

		[Test]
		public void OptimizerTest()
		{
			string testAssets = "Assets/ArtResources/Tests";

			string[] prefabFiles = Directory.GetFiles(testAssets, "*.prefab", SearchOption.AllDirectories);
			List<AssetNode> assets = new List<AssetNode>();
			foreach (var f in prefabFiles)
			{
				AssetNode assetNode = m_AssetManager.CreateAsset(f);
				//直接使用的资源，可以导址
				assetNode.addressable = true;
				assets.Add(assetNode);
			}

			m_AssetManager.RefreshAllAssetDependencies();

			//create bundle from assets
			foreach (var iter in m_AssetManager.assets)
			{
				BundleNode bundleNode = m_AssetManager.CreateBundle(null);
				bundleNode.SetMainAsset(iter.Value);
				bundleNode.AddAsset(iter.Value);
				if (iter.Value.addressable)
				{
					bundleNode.SetStandalone(iter.Value.addressable);
				}
			}

			m_AssetManager.RefreshAllBundleDependencies();

			Debug.LogFormat("Before optimze Asset Count:{0},Bundle Count:{1}", m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
			Assert.AreEqual(m_AssetManager.assets.Count, m_AssetManager.bundles.Count);

			DateTime start = DateTime.Now;

			m_AssetManager.Combine();

			TimeSpan used = DateTime.Now - start;
			Debug.LogFormat("create assets used:{0}", used);
			start = DateTime.Now;

			Debug.LogFormat("After optimze Asset Count:{0},Bundle Count:{1}", m_AssetManager.assets.Count, m_AssetManager.bundles.Count);
		}
	}
}
