using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentBuilder.WinUtils
{
    public static class StringHandling
    {
        public static void GetUnQualifiedFileName(string _absolute_path, out string dir_name, out string file_name)
        {
            dir_name = null;
            file_name = null;
            if (_absolute_path == null) return;

            int ind_slash = _absolute_path.LastIndexOf("/");
            int ind_backslash = _absolute_path.LastIndexOf("\\");
            int ind = Math.Max(ind_slash, ind_backslash);

            if (ind < 0)
            {
                dir_name = string.Empty;
                file_name = string.Empty;
                return;
            }

            dir_name = _absolute_path.Substring(0, ind + 1); // take the slash or backslash too
            file_name = _absolute_path.Substring(ind + 1, _absolute_path.Length - ind - 1);
        }

        public static void GetUnQualifiedFileName(string _absolute_path, out string dir_name, out string file_name, out string dir_last_component)
        {
            dir_name = null;
            file_name = null;
            dir_last_component = null;
            if (_absolute_path == null) return;

            string[] components;
            string delimiter;
            StringHandling.GetPathComponents(_absolute_path, out components, out delimiter);

            if (components.Length == 0) return;

            dir_name = string.Empty;
            for(int i = 0; i < components.Length; i++)
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

        public static string ExtractStringBtw(string _input, string _delimiter)
        {
            if (string.IsNullOrEmpty(_input)) return string.Empty;
            if (string.IsNullOrEmpty(_delimiter)) return _input;

            int ind_first = _input.IndexOf(_delimiter);
            if (ind_first < 0)
                return string.Empty;

            int ind_next = _input.IndexOf(_delimiter, ind_first + 1);

            if (ind_next < 0)
                return string.Empty;

            return _input.Substring(ind_first + _delimiter.Length, ind_next - ind_first - _delimiter.Length);
        }
    }
}
