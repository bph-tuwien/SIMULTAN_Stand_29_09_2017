using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterStructure.Utils
{
    public class SelectableObject<T>
    {
        public bool IsSelected { get; set; }
        public T ObjectData { get; set; }

        public SelectableObject(T _data)
        {
            this.ObjectData = _data;
            this.IsSelected = false;
        }

        public SelectableObject(T _data, bool _is_selected)
        {
            this.ObjectData = _data;
            this.IsSelected = _is_selected;
        }
    }

    // a non-generic class for easy binding in XAML
    public class SelectableString : SelectableObject<string>
    {
        public SelectableString(string _data)
            :base(_data)
        { }

        public SelectableString(string _data, bool _is_selected)
            :base(_data, _is_selected)
        { }
    }
}
