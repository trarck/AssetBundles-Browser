using NUnit.Framework;
using UnityEngine;
using AssetBundleBuilder.Model;
using AssetBundleBuilder.View;

namespace AssetBundleBuilder.Tests
{
	public class BundleTreeManagerTests
    {
		BundleTreeManager m_bundleManager = null;

		//run once
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Debug.Log("OneTimeSetUp");
			m_bundleManager = new BundleTreeManager();
			m_bundleManager.Init();
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			Debug.Log("OneTimeTearDown");
			m_bundleManager.Clean();
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


		#region Test Bundle
		[Test]
        public void CreateBundleDataByNameTest()
        {
			m_bundleManager.CreateBundleData("TestA");
			BundleTreeItem bundleInfo = m_bundleManager.GetBundle("TestA");
			Assert.NotNull(bundleInfo);
		}

		[Test]
		public void CreateBundleDataByPathTest()
		{
			m_bundleManager.CreateBundleData("TestB1/TestB2");
			BundleTreeItem bundleInfo = m_bundleManager.GetBundle("TestB1/TestB2");
			Assert.NotNull(bundleInfo);

			BundleTreeItem bundleInfo2 = m_bundleManager.GetBundle("TestB1/TestB22");
			Assert.IsNull(bundleInfo2);
		}

		[Test]
		public void CreateBundleDataByPathExistsTest()
		{
			m_bundleManager.CreateBundleData("TestC1/TestC2");
			BundleTreeItem bundleInfo = m_bundleManager.GetBundle("TestC1/TestC2");
			Assert.NotNull(bundleInfo);

			m_bundleManager.CreateBundleData("TestC1/TestC21");
			BundleTreeItem bundleInfo2 = m_bundleManager.GetBundle("TestC1/TestC21");
			Assert.NotNull(bundleInfo2);
		}

		[Test]
		public void CreateBundleFolderByNameTest()
		{
			m_bundleManager.CreateBundleFolder("TestFolderA");
			BundleTreeItem bundleInfo = m_bundleManager.GetBundle("TestFolderA");
			Assert.NotNull(bundleInfo);
			Assert.IsInstanceOf<BundleTreeFolderItem>(bundleInfo);
		}

		[Test]
		public void CreateBundleFolderByPathTest()
		{
			m_bundleManager.CreateBundleFolder("TestFolderB1/TestFolderB2");
			BundleTreeItem bundleInfo = m_bundleManager.GetBundle("TestFolderB1/TestFolderB2");
			Assert.NotNull(bundleInfo);
			Assert.IsInstanceOf<BundleTreeFolderItem>(bundleInfo);

			BundleTreeItem bundleInfo2 = m_bundleManager.GetBundle("TestFolderB1/TestFolderB22");
			Assert.IsNull(bundleInfo2);
		}

		[Test]
		public void CreateBundleFolderByPathExistsTest()
		{
			m_bundleManager.CreateBundleFolder("TestFolderC1/TestFolderC2");
			BundleTreeItem bundleInfo = m_bundleManager.GetBundle("TestFolderC1/TestFolderC2");
			Assert.NotNull(bundleInfo);
			Assert.IsInstanceOf<BundleTreeFolderItem>(bundleInfo);

			m_bundleManager.CreateBundleFolder("TestFolderC1/TestFolderC21");
			BundleTreeItem bundleInfo2 = m_bundleManager.GetBundle("TestFolderC1/TestFolderC21");
			Assert.NotNull(bundleInfo2);
			Assert.IsInstanceOf<BundleTreeFolderItem>(bundleInfo2);
		}

		[Test]
		public void CreateBundleTest()
		{
			m_bundleManager.CreateBundleData("TestB1/TestB2");
			BundleTreeItem bundleInfo = m_bundleManager.GetBundle("TestB1/TestB2");
			Assert.NotNull(bundleInfo);

			BundleTreeItem bundleInfo2 = m_bundleManager.GetBundle("TestB1");
			Assert.NotNull(bundleInfo2);
			Assert.IsInstanceOf<BundleTreeFolderItem>(bundleInfo2);
		}

		[Test]
		public void GetDataSourceTest()
		{
			DataSource.DataSource ds = m_bundleManager.dataSource;
			Assert.NotNull(ds);
		}

		[Test]
		public void SetupBundleInfosTest()
		{
			DataSource.DataSource ds = m_bundleManager.dataSource;
			string[] bundlePaths = ds.GetAllAssetBundleNames();

			Debug.Log(bundlePaths.Length);
			foreach (var bundlePath in bundlePaths)
			{
				BundleTreeDataItem bundleDataInfo = m_bundleManager.CreateBundleData(bundlePath);
				Assert.NotNull(bundleDataInfo);
			}
		}

		#endregion

		//// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
		//// `yield return null;` to skip a frame.
		//[UnityTest]
		//public IEnumerator BundleManagerTestsWithEnumeratorPasses()
		//{
		//    // Use the Assert class to test conditions.
		//    // Use yield to skip a frame.
		//    yield return null;
		//}
	}
}
