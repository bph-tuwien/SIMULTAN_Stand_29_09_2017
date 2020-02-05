using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class Schicht
    {
        #region parameter
        private String name_id;
        [JsonProperty(PropertyName = "name")]
        public String Name_id
        {
            get { return name_id; }
            set { name_id = value; }
        }

        private String seite1_id;
        [JsonProperty(PropertyName = "seite1")]
        public String Seite1_id
        {
            get { return seite1_id; }
            set { seite1_id = value; }
        }

        private String seite2_id;
        [JsonProperty(PropertyName = "seite2")]
        public String Seite2_id
        {
            get { return seite2_id; }
            set { seite2_id = value; }
        }

        private double flaeche;
        [JsonProperty(PropertyName = "flaeche")]
        public double Flaeche
        {
            get { return flaeche; }
            set { flaeche = value; }
        }

        private double dicke;
        [JsonProperty(PropertyName = "d")]
        public double Dicke
        {
            get { return dicke; }
            set { dicke = value; }
        }

        private int anzahlElemente;
        [JsonProperty(PropertyName = "n")]
        public int AnzahlElemente
        {
            get { return anzahlElemente; }
            set { anzahlElemente = value; }
        }

        private double waermespeicherkapazitaet;

        [JsonProperty(PropertyName = "c")]
        public double Waermespeicherkapazitaet
        {
            get { return waermespeicherkapazitaet; }
            set { waermespeicherkapazitaet = value; }
        }

        private double waermeleitzahl;

        [JsonProperty(PropertyName = "l")]
        public double Waermeleitzahl
        {
            get { return waermeleitzahl; }
            set { waermeleitzahl = value; }
        }

        private double dichte;

        [JsonProperty(PropertyName = "r")]
        public double Dichte
        {
            get { return dichte; }
            set { dichte = value; }
        }

        private String speicher_id;

        [JsonProperty(PropertyName = "speicher")]
        public String Speicher_id
        {
            get { return speicher_id; }
            set { speicher_id = value; }
        }

        private String zone_id;

        [JsonProperty(PropertyName = "zone")]
        public String Zone_id
        {
            get { return zone_id; }
            set { zone_id = value; }
        }

        private bool isBoden;

        [JsonProperty(PropertyName = "isBoden")]
        public bool IsBoden
        {
            get { return isBoden; }
            set { isBoden = value; }
        }

        private bool isDecke;

        [JsonProperty(PropertyName = "isDecke")]
        public bool IsDecke
        {
            get { return isDecke; }
            set { isDecke = value; }
        }
        #endregion

        /// <summary>
        /// Constructor for one layer of a wall
        /// </summary>
        /// <param name="name_id">id of the specific layer of the wall</param>
        /// <param name="seite1_id">neighbour on the one side (e=outside)</param>
        /// <param name="seite2_id">neighbour on the other side (e=outside)</param>
        /// <param name="flaeche">surface area of the layer</param>
        /// <param name="dicke">thickness of the layer</param>
        /// <param name="waermekapazitaet">mass specific heat storage capacity of the layer</param>
        /// <param name="waermeleitzahl">coefficient of thermal conductivity of the layer</param>
        /// <param name="dichte">density of the layer</param>
        /// <param name="anzahlElemente">number how often the layer should be divided (default 1)</param>
        /// <param name="speicher_id">if component-activated: id of the boiler</param>
        /// <param name="zone_id">if component-activated: id of the zone for air temperature</param>
        /// <param name="isBoden">true:layer belongs to floor</param>
        /// <param name="isDecke">true:layer belongs to ceiling</param>
        public Schicht(String name_id, String seite1_id, String seite2_id, double dicke, double waermekapazitaet, double waermeleitzahl, double dichte, double flaeche = 0, int anzahlElemente = 1, String speicher_id = null, String zone_id = null, bool isBoden = false, bool isDecke = false)
        {
            Name_id = name_id;

            Seite1_id = seite1_id;

            Seite2_id = seite2_id;

            Flaeche = flaeche;

            Dicke = dicke;

            Waermespeicherkapazitaet = waermekapazitaet;

            Waermeleitzahl = waermeleitzahl;

            Dichte = dichte;

            AnzahlElemente = anzahlElemente;

            Speicher_id = speicher_id;

            Zone_id = zone_id;

            IsBoden = isBoden;

            IsDecke = isDecke;

        }
    }
}