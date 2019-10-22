using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder.View
{
    public class EnumGUI<T>
    {
        //select options
        class ToggleData
        {
            internal ToggleData(bool s,string name,string tooltip,T opt)
            {
                state = s;
                this.name = name;
                content = new GUIContent(name, tooltip);
                option = opt;
            }
            internal bool state;
            internal string name;
            internal GUIContent content;
            internal T option;
        }
        //items
        List<ToggleData> m_Toggles;
        //display name
        string m_Name = "";
        //
        bool m_Expand = false;

        public void Init(string name,bool expand=false,bool ignoreZero=false)
        {
            Type t = typeof(T);
            m_Name = string.IsNullOrEmpty(name) ? t.ToString() : name;
            m_Expand = expand;

            //get enum name and values
            string[] names = Enum.GetNames(t);
            Array values = values = Enum.GetValues(t);

            m_Toggles = new List<ToggleData>();

            for (int i = 0; i < names.Length; ++i)
            {
                if (!ignoreZero || (int)values.GetValue(i)!=0)
                {
                    ToggleData enumData = new ToggleData(false, names[i], null, (T)values.GetValue(i));
                    m_Toggles.Add(enumData);
                }
            }
        }

        public void OnGUI()
        {
            m_Expand = EditorGUILayout.Foldout(m_Expand, m_Name);
            if (m_Expand)
            {
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = indent+1;
                foreach (var tog in m_Toggles)
                {
                    bool newState = EditorGUILayout.ToggleLeft(
                        tog.content,
                        tog.state);
                    if (newState != tog.state)
                    {
                        tog.state = newState;
                    }
                }
                EditorGUILayout.Space();
                EditorGUI.indentLevel = indent;
            }
        }

        /// <summary>
        /// get select enum value
        /// </summary>
        /// <returns></returns>
        public List<T> GetValue()
        {
            List<T> values = new List<T>();
            for (int i=0; i < m_Toggles.Count; ++i)
            {
                if (m_Toggles[i].state)
                {
                    values.Add(m_Toggles[i].option);
                }
            }
            return values;
        }

        /// <summary>
        /// get select enum names
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelects()
        {
            List<string> selects = new List<string>();
            for (int i = 0; i < m_Toggles.Count; ++i)
            {
                if (m_Toggles[i].state)
                {
                    selects.Add(m_Toggles[i].name);
                }
            }
            return selects;
        }

        /// <summary>
        /// set select enum name
        /// </summary>
        /// <param name="selects"></param>
        public void SetSelects(List<string> selects)
        {
            foreach (var tog in m_Toggles)
            {
                if (selects.Contains(tog.name))
                {
                    tog.state = true;
                }
                else
                {
                    tog.state = false;
                }
            }
        }
    }
}
