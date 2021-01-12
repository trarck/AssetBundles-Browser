using System.Collections;
using System.Collections.Generic;
using AssetBundleBuilder.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssetBundleBuilder.Tests
{
    public class BundleManagerTests
    {
		BundleManager m_bundleManager = null;

		//run once
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Debug.Log("OneTimeSetUp");
			m_bundleManager = new BundleManager();
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


		[Test]
        public void CreateBundleDataByNameTest()
        {
			m_bundleManager.CreateBundleDataByName("TestA");

		}

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator BundleManagerTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
