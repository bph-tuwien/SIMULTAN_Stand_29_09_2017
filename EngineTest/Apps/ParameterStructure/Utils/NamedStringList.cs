using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterStructure.Utils
{
    public class NamedStringList
    {
        private List<String> strings;

        #region ACCESS

        public string S1
        {
            get
            {
                if (this.strings.Count > 0)
                    return this.strings[0];
                else
                    return string.Empty;
            }
        }

        public string S2
        {
            get
            {
                if (this.strings.Count > 1)
                    return this.strings[1];
                else
                    return string.Empty;
            }
        }

        public string S3
        {
            get
            {
                if (this.strings.Count > 2)
                    return this.strings[2];
                else
                    return string.Empty;
            }
        }

        public string S4
        {
            get
            {
                if (this.strings.Count > 3)
                    return this.strings[3];
                else
                    return string.Empty;
            }
        }

        public string S5
        {
            get
            {
                if (this.strings.Count > 4)
                    return this.strings[4];
                else
                    return string.Empty;
            }
        }

        public string S6
        {
            get
            {
                if (this.strings.Count > 5)
                    return this.strings[5];
                else
                    return string.Empty;
            }
        }

        public string S7
        {
            get
            {
                if (this.strings.Count > 6)
                    return this.strings[6];
                else
                    return string.Empty;
            }
        }

        public string S8
        {
            get
            {
                if (this.strings.Count > 7)
                    return this.strings[7];
                else
                    return string.Empty;
            }
        }

        public string S9
        {
            get
            {
                if (this.strings.Count > 8)
                    return this.strings[8];
                else
                    return string.Empty;
            }
        }

        public string S10
        {
            get
            {
                if (this.strings.Count > 9)
                    return this.strings[9];
                else
                    return string.Empty;
            }
        }

        #endregion

        public NamedStringList(List<string> _content)
        {
            if (_content == null)
                this.strings = new List<string>();
            else
                this.strings = new List<string>(_content);
        }
    }
}
