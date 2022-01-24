//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;

//namespace AssetBundleBuilder.DataSource
//{ 
//    internal class DataSourceProviderUtility {

//        private static List<Type> s_customNodes;

//		internal static DataSource GetDataSource(Type type, bool useFirst = false)
//		{
//			List<DataSource> dataSources = null;

//			foreach (var info in DataSourceProviderUtility.CustomDataSourceTypes)
//			{
//				if (info == type)
//				{
//					dataSources = info.GetMethod("CreateDataSources").Invoke(null, null) as List<DataSource>;
//					if (dataSources != null && dataSources.Count > 0)
//					{
//						return dataSources[0];
//					}
//				}
//			}

//			if (useFirst)
//			{
//				if (DataSourceProviderUtility.CustomDataSourceTypes.Count > 0)
//				{
//					dataSources = DataSourceProviderUtility.CustomDataSourceTypes[0]
//						.GetMethod("CreateDataSources").Invoke(null, null) as List<DataSource>;

//					if (dataSources != null && dataSources.Count > 0)
//					{
//						return dataSources[0];
//					}
//				}
//			}

//			return null;
//		}

//		internal static DataSource GetDataSource(string typeStr)
//		{
//			Type type = null;
//			var x = AppDomain.CurrentDomain.GetAssemblies();
//			foreach (var assembly in x)
//			{
//				type = assembly.GetType(typeStr);
//				if (type != null)
//				{
//					break;
//				}
//			}

//			return GetDataSource(type);
//		}

//		internal static List<Type> CustomDataSourceTypes {
//            get {
//                if(s_customNodes == null) {
//                    s_customNodes = BuildCustomDataSourceList();
//                }
//                return s_customNodes;
//            }
//        }

		

//        private static List<Type> BuildCustomDataSourceList()
//        {
//            var properList = new List<Type>();
//            properList.Add(null); //empty spot for "default" 
//            var x = AppDomain.CurrentDomain.GetAssemblies();
//            foreach (var assembly in x)
//            {
//                try
//                {
//                    var list = new List<Type>(
//                        assembly
//                        .GetTypes()
//                        .Where(t => t != typeof(DataSource))
//                        .Where(t => typeof(DataSource).IsAssignableFrom(t)));


//                    for (int count = 0; count < list.Count; count++)
//                    {
//                        if (list[count].Name == "OriginDataSource")
//                            properList[0] = list[count];
//                        else if (list[count] != null)
//                            properList.Add(list[count]);
//                    }
//                }
//                catch (System.Exception)
//                {
//                    //assembly which raises exception on the GetTypes() call - ignore it
//                }
//            }


//            return properList;
//        }
//    }
//}
