using NUnit.Framework;
using UnityEngine;
using AssetBundleBuilder.Model;

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
			m_bundleManager.CreateBundleDataByName("TestA");
			BundleNode bundleInfo = m_bundleManager.GetBundle("TestA");
			Assert.NotNull(bundleInfo);
		}

		[Test]
		public void CreateBundleDataByPathTest()
		{
			m_bundleManager.CreateBundleDataByPath("TestB1/TestB2");
			BundleNode bundleInfo = m_bundleManager.GetBundle("TestB1/TestB2");
			Assert.NotNull(bundleInfo);

			BundleNode bundleInfo2 = m_bundleManager.GetBundle("TestB1/TestB22");
			Assert.IsNull(bundleInfo2);
		}

		[Test]
		public void CreateBundleDataByPathExistsTest()
		{
			m_bundleManager.CreateBundleDataByPath("TestC1/TestC2");
			BundleNode bundleInfo = m_bundleManager.GetBundle("TestC1/TestC2");
			Assert.NotNull(bundleInfo);

			m_bundleManager.CreateBundleDataByPath("TestC1/TestC2");
			BundleNode bundleInfo2 = m_bundleManager.GetBundle("TestC1/TestC21");
			Assert.NotNull(bundleInfo2);
		}

		[Test]
		public void CreateBundleFolderByNameTest()
		{
			m_bundleManager.CreateBundleFolderByName("TestFolderA");
			BundleNode bundleInfo = m_bundleManager.GetBundle("TestFolderA");
			Assert.NotNull(bundleInfo);
			Assert.IsInstanceOf<BundleFolderNode>(bundleInfo);
		}

		[Test]
		public void CreateBundleFolderByPathTest()
		{
			m_bundleManager.CreateBundleFolderByPath("TestFolderB1/TestFolderB2");
			BundleNode bundleInfo = m_bundleManager.GetBundle("TestFolderB1/TestFolderB2");
			Assert.NotNull(bundleInfo);
			Assert.IsInstanceOf<BundleFolderNode>(bundleInfo);

			BundleNode bundleInfo2 = m_bundleManager.GetBundle("TestFolderB1/TestFolderB22");
			Assert.IsNull(bundleInfo2);
		}

		[Test]
		public void CreateBundleFolderByPathExistsTest()
		{
			m_bundleManager.CreateBundleFolderByPath("TestFolderC1/TestFolderC2");
			BundleNode bundleInfo = m_bundleManager.GetBundle("TestFolderC1/TestFolderC2");
			Assert.NotNull(bundleInfo);
			Assert.IsInstanceOf<BundleFolderNode>(bundleInfo);

			m_bundleManager.CreateBundleFolderByPath("TestFolderC1/TestFolderC2");
			BundleNode bundleInfo2 = m_bundleManager.GetBundle("TestFolderC1/TestFolderC21");
			Assert.NotNull(bundleInfo2);
			Assert.IsInstanceOf<BundleFolderNode>(bundleInfo2);
		}

		[Test]
		public void CreateBundleTest()
		{
			m_bundleManager.CreateBundleDataByPath("TestB1/TestB2");
			BundleNode bundleInfo = m_bundleManager.GetBundle("TestB1/TestB2");
			Assert.NotNull(bundleInfo);

			BundleNode bundleInfo2 = m_bundleManager.GetBundle("TestB1");
			Assert.NotNull(bundleInfo2);
			Assert.IsInstanceOf<BundleFolderNode>(bundleInfo2);
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
				BundleDataNode bundleDataInfo = m_bundleManager.CreateBundleDataByPath(bundlePath);
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
