using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    [TypeConverter(typeof(ParameterConverter))]
    public class ParameterDouble
    {
        private String name_id;

        public String Name_id
        {
            get { return name_id; }
            set { name_id = value; }
        }   


        private double _value;
        public double Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public ParameterDouble(String name_id, double value)
        {
            Name_id = name_id;
            Value = value;
        }

        public ParameterDouble()
        {
            Name_id = "";
            Value=0;
        }
    }
}
