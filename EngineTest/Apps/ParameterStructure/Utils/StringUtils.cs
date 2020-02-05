using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterStructure.Utils
{
    public static class StringUtils
    {
        public static void GetUnQualifiedFileName(string _absolute_path, out string dir_name, out string file_name, out string dir_last_component)
        {
            dir_name = null;
            file_name = null;
            dir_last_component = null;
            if (_absolute_path == null) return;

            string[] components;
            string delimiter;
            StringUtils.GetPathComponents(_absolute_path, out components, out delimiter);

            if (components.Length == 0) return;

            dir_name = string.Empty;
            for (int i = 0; i < components.Length; i++)
            {
                if (i == components.Length - 1)
                {
                    file_name = components[i];
                }
                else if (i == components.Length - 2)
                {
                    dir_last_component = components[i];
                }
                else if (i == components.Length - 3)
                {
                    dir_name += components[i];
                }
                else
                {
                    dir_name += components[i] + delimiter;
                }
            }

        }

        private static void GetPathComponents(string _absolute_path, out string[] components, out string delimiter)
        {
            components = new string[0];
            delimiter = string.Empty;
            if (_absolute_path == null) return;

            int ind_slash = _absolute_path.LastIndexOf("/");
            int ind_backslash = _absolute_path.LastIndexOf("\\");

            if (ind_slash > 0)
            {
                components = _absolute_path.Split(new char[] { '/' });
                delimiter = "/";
            }
            else if (ind_backslash > 0)
            {
                components = _absolute_path.Split(new char[] { '\\' });
                delimiter = "\\";
            }
        }

        public static bool UnQualifiedFileNamesEqual(string _absolute_path_1, string _absolute_path_2)
        {
            if (string.IsNullOrEmpty(_absolute_path_1) && !string.IsNullOrEmpty(_absolute_path_2)) return false;
            if (!string.IsNullOrEmpty(_absolute_path_1) && string.IsNullOrEmpty(_absolute_path_2)) return false;
            
            if (string.IsNullOrEmpty(_absolute_path_1) && string.IsNullOrEmpty(_absolute_path_2)) return true;

            string dir_1 = string.Empty;
            string dir_2 = string.Empty;
            string dir_1_last_comp = string.Empty;
            string dir_2_last_comp = string.Empty;
            string file_name_1 = string.Empty;
            string file_name_2 = string.Empty;

            Utils.StringUtils.GetUnQualifiedFileName(_absolute_path_1, out dir_1, out file_name_1, out dir_1_last_comp);
            Utils.StringUtils.GetUnQualifiedFileName(_absolute_path_2, out dir_2, out file_name_2, out dir_2_last_comp);

            return (file_name_1 == file_name_2);
        }
    }
}
