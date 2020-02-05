using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParameterStructure.Utils
{
    public static class DecimalToTertiary
    {
        public static string DecimalToTertiaryString(int _decimal_number)
        {
            string result = string.Empty;
            if (_decimal_number <= 0) return result;

            int decNr = _decimal_number;
            int remainder = 0;
            while(decNr > 0)
            {
                remainder = decNr % 3;
                decNr /= 3;
                result = remainder.ToString() + result;
            }
            return result;
        }

    }
}
