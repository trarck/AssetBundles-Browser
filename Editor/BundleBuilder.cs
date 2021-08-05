using System;
using AssetBundleBuilder.DataSource;

namespace AssetBundleBuilder
{
    public class BundleBuilder
    {
		DataSource.DataSource m_DataSource;
		Type m_DefaultDataSourceType = typeof(JsonDataSource);


		static BundleBuilder m_Instance = null;
		public static BundleBuilder Instance
		{
			get
			{
				if (m_Instance == null)
				{
					m_Instance = new BundleBuilder();
					m_Instance.Init();
				}
				return m_Instance;
			}
		}

		public DataSource.DataSource dataSource
		{
			get
			{
				if (m_DataSource == null)
				{
					m_DataSource = DataSourceProviderUtility.GetDataSource(m_DefaultDataSourceType, true);
				}
				return m_DataSource;
			}
			set
			{
				m_DataSource = value;
			}
		}

		public void Init()
		{

		}
	}
}