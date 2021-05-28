using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AssetBundleBuilder;

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
	}
}
