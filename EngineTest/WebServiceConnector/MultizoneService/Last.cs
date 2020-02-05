using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    [JsonConverter(typeof(DynamicPropertyNameConverter))]
    public class Last
    {
        #region parameter
        private double zeit;

        [JsonProperty(PropertyName = "zeit")]
        public double Zeit
        {
            get { return zeit; }
            set { zeit = value; }
        }

        private double aussentemp;

        [JsonProperty(PropertyName = "aussentemp")]
        public double Aussentemp
        {
            get { return aussentemp; }
            set { aussentemp = value; }
        }

        private ParameterDouble strahlung;
        
        [JsonProperty("strahlung_")]
        public ParameterDouble Strahlung
        {
            get { return strahlung; }
            set { strahlung = value; }
        }

        private ParameterDouble strahlung2;

        [JsonProperty(PropertyName = "strahlung2_")]
        public ParameterDouble Strahlung2
        {
            get { return strahlung2; }
            set { strahlung2 = value; }
        }

        private ParameterDouble temperatur;

        [JsonProperty(PropertyName = "temp_")]
        public ParameterDouble Temperatur
        {
            get { return temperatur; }
            set { temperatur = value; }
        }

        private ParameterDouble raumlueftung;
        
        [JsonProperty(PropertyName = "rlt_")]
        public ParameterDouble Raumlueftung
        {
            get { return raumlueftung; }
            set { raumlueftung = value; }
        }

        private ParameterDouble infiltration;

        [JsonProperty(PropertyName = "infiltration_")]
        public ParameterDouble Infiltration
        {
            get { return infiltration; }
            set { infiltration = value; }
        }

        private ParameterDouble ilastenGeraete;

        [JsonProperty(PropertyName = "ilastGer_")]
        public ParameterDouble IlastenGeraete
        {
            get { return ilastenGeraete; }
            set { ilastenGeraete = value; }
        }

        private ParameterDouble ilastenPersonen;

        [JsonProperty(PropertyName = "ilastPer_")]
        public ParameterDouble IlastenPersonen
        {
            get { return ilastenPersonen; }
            set { ilastenPersonen = value; }
        }

        private ParameterDouble lueftung;

        [JsonProperty(PropertyName = "lueftungsliste_")]
        public ParameterDouble Lueftung
        {
            get { return lueftung; }
            set { lueftung = value; }
        }

        private ParameterDouble warmwasseverbrauch;

        [JsonProperty(PropertyName = "wwbVol_")]
        public ParameterDouble Warmwasseverbrauch
        {
            get { return warmwasseverbrauch; }
            set { warmwasseverbrauch = value; }
        }

        #endregion

        /// <summary>
        /// Constructor for load
        /// </summary>
        /// <param name="zeit">instant of time when the other values are relevant</param>
        /// <param name="aussentemp">outside temperature</param>
        /// <param name="strahlung">radiation on a specific zone</param>
        /// <param name="strahlung2">radiation on a specific zone with clouding</param>
        /// <param name="temperatur">temperature of the specific element</param>
        /// <param name="raumlueftung">mechanical change of air in the zone</param>
        /// <param name="infiltration">infiltration</param>
        /// <param name="ilastGeraete">inside load of the machines of the zone</param>
        /// <param name="ilastPersonen">inside load of the persons of the zone</param>
        /// <param name="lueftung">window opening between 0 and 1 (0:closed, 1:open)</param>
        /// <param name="warmwasserverbrauch">use of hot water</param>
        public Last(double zeit, double aussentemp, ParameterDouble strahlung = null, ParameterDouble strahlung2 = null, ParameterDouble temperatur = null, ParameterDouble raumlueftung = null, ParameterDouble infiltration = null, ParameterDouble ilastGeraete = null, ParameterDouble ilastPersonen = null, ParameterDouble lueftung = null, ParameterDouble warmwasserverbrauch=null)
        {
            Zeit = zeit;
            Aussentemp = aussentemp;

            if (strahlung != null)
            {
                Strahlung= new ParameterDouble();
                Strahlung = strahlung;
            }

            if (strahlung2 != null)
            {
                Strahlung2 = new ParameterDouble();
                Strahlung2 = strahlung2;
            }

            if (temperatur != null)
            {
                Temperatur = new ParameterDouble();
                Temperatur = temperatur;
            }

            if (raumlueftung != null)
            {
                Raumlueftung = new ParameterDouble();
                Raumlueftung = raumlueftung;
            }

            if (infiltration != null)
            {
                Infiltration = new ParameterDouble();
                Infiltration = infiltration;
            }

            if (ilastenGeraete != null)
            {
                IlastenGeraete = new ParameterDouble();
                IlastenGeraete = ilastenGeraete;
            }

            if (ilastenPersonen != null)
            {
                IlastenPersonen = new ParameterDouble();
                IlastenPersonen = ilastenPersonen;
            }

            if (lueftung != null)
            {
                Lueftung = new ParameterDouble();
                Lueftung = lueftung;
            }

            if (warmwasserverbrauch != null)
            {
                Warmwasseverbrauch = new ParameterDouble();
                Warmwasseverbrauch = warmwasserverbrauch;
            }

        }

    }
}
