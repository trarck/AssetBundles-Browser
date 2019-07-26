using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AssetBundleBuilder.DataSource
{ 
    internal class DataSourceProviderUtility {

        private static List<Type> s_customNodes;

        internal static List<Type> CustomDataSourceTypes {
            get {
                if(s_customNodes == null) {
                    s_customNodes = BuildCustomDataSourceList();
                }
                return s_customNodes;
            }
        }

        private static List<Type> BuildCustomDataSourceList()
        {
            var properList = new List<Type>();
            properList.Add(null); //empty spot for "default" 
            var x = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in x)
            {
                try
                {
                    var list = new List<Type>(
                        assembly
                        .GetTypes()
                        .Where(t => t != typeof(DataSource))
                        .Where(t => typeof(DataSource).IsAssignableFrom(t)));


                    for (int count = 0; count < list.Count; count++)
                    {
                        if (list[count].Name == "OriginDataSource")
                            properList[0] = list[count];
                        else if (list[count] != null)
                            properList.Add(list[count]);
                    }
                }
                catch (System.Exception)
                {
                    //assembly which raises exception on the GetTypes() call - ignore it
                }
            }


            return properList;
        }
    }
}
