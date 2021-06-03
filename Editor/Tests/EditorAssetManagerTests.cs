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

		public void CollectAssetDeps()
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
		public void CollectAssetDeps2()
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
						 m_AssetManager.assets[node.fullAssetName] = node;
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
						m_AssetManager.assets[node.fullAssetName] = node;
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
						m_AssetManager.assets[node.fullAssetName] = node;
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

		public void CollectAssetAllDeps()
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

		public void CollectAssetAllDeps2()
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
			Debug.LogFormat("{0},{1},{2}", node.fullAssetName, node.dependencies.Count, node.allDependencies.Count);
			string s = "";
			if (node.dependencies != null)
			{
				foreach (var dep in node.dependencies)
				{
					s+=string.Format("Dep:{0}\n", dep.fullAssetName);
				}
			}

			Debug.Log(s);

			s = "";
			if (node.allDependencies != null)
			{
				foreach (var dep in node.allDependencies)
				{
					s += string.Format("AllDep:{0}\n", dep.fullAssetName);
				}
			}
			Debug.Log(s);
		}
	}
}
