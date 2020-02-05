using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class Zone
    {
        #region parameter

        private String name_id;

        [JsonProperty(PropertyName = "name")]
        public String Name_id
        {
            get { return name_id; }
            set { name_id = value; }
        }

        private double grundflaeche;

        [JsonProperty(PropertyName = "grundflaeche")]
        public double Grundflaeche
        {
            get { return grundflaeche; }
            set { grundflaeche = value; }
        }

        private bool forceIdeal;

        [JsonProperty(PropertyName = "forceIdeal")]
        public bool ForceIdeal
        {
            get { return forceIdeal; }
            set { forceIdeal = value; }
        }

        private bool heizung;

        [JsonProperty(PropertyName = "heizung")]
        public bool Heizung
        {
            get { return heizung; }
            set { heizung = value; }
        }

        private double heizTemp;

        [JsonProperty(PropertyName = "heizTemp")]
        public double HeizTemp
        {
            get { return heizTemp; }
            set { heizTemp = value; }
        }

        private bool heizungIdeal;

        [JsonProperty(PropertyName = "heizungIdeal")]
        public bool HeizungIdeal
        {
            get { return heizungIdeal; }
            set { heizungIdeal = value; }
        }

        private double heizlastKonstant;

        [JsonProperty(PropertyName = "heizlastKonstant")]
        public double HeizlastKonstant
        {
            get { return heizlastKonstant; }
            set { heizlastKonstant = value; }
        }

        private bool kuehlung;

        [JsonProperty(PropertyName = "kuehlung")]
        public bool Kuehlung
        {
            get { return kuehlung; }
            set { kuehlung = value; }
        }

        private double kuehlTemp;

        [JsonProperty(PropertyName = "kuehlTemp")]
        public double KuehlTemp
        {
            get { return kuehlTemp; }
            set { kuehlTemp = value; }
        }

        private bool kuehlungIdeal;

        [JsonProperty(PropertyName = "kuehlungIdeal")]
        public bool KuehlungIdeal
        {
            get { return kuehlungIdeal; }
            set { kuehlungIdeal = value; }
        }

        private double kuehllastKonstant;

        [JsonProperty(PropertyName = "kuehllastKonstant")]
        public double KuehllastKonstant
        {
            get { return kuehllastKonstant; }
            set { kuehllastKonstant = value; }
        }

        private double fensterAufTemp;

        [JsonProperty(PropertyName = "fensterAufTemp")]
        public double FensterAufTemp
        {
            get { return fensterAufTemp; }
            set { fensterAufTemp = value; }
        }

        private double sonnenschutzpunkt;

        [JsonProperty(PropertyName = "sonnenschutzpunkt")]
        public double Sonnenschutzpunkt
        {
            get { return sonnenschutzpunkt; }
            set { sonnenschutzpunkt  = value; }
        }


        private double startTemp;

        [JsonProperty(PropertyName = "startTemp")]
        public double StartTemp
        {
            get { return startTemp; }
            set { startTemp = value; }
        }

        private double kapazitaetEinrichtung;

        [JsonProperty(PropertyName = "kapEinrichtung")]
        public double KapazitaetEinrichtung
        {
            get { return kapazitaetEinrichtung; }
            set { kapazitaetEinrichtung = value; }
        }

        #endregion

        /// <summary>
        /// Constructor for zone
        /// </summary>
        /// <param name="name_id">id of the zone</param>
        /// <param name="grundflaeche">surface area of the zone</param>
        /// <param name="startTemperatur">starting temperature for the simulation</param>
        /// <param name="kapazitaetEinrichtung">effective heat storage capacity for the furniture</param>
        /// <param name="forceIdeal">true: constant air temperature</param>
        /// <param name="heizung">true: heating on</param>
        /// <param name="heizTemp">heating temperature</param>
        /// <param name="heizungIdeal">true: temperature must not fall under heating temperature</param>
        /// <param name="heizlastKonstant">constant heating load</param>
        /// <param name="kuehlung">true: cooling on</param>
        /// <param name="kuehlTemp">cooling temperature</param>
        /// <param name="kuehlungIdeal">true: temperature must not rise over cooling temperature</param>
        /// <param name="kuehllastKonstant">constant cooling load</param>
        /// <param name="fensterAufTemp">temperature when windows should open</param>
        /// <param name="sonnenschutzpunkt">temperature when other radiation values should be used</param>
        public Zone(String name_id, double grundflaeche, double startTemperatur, double kapazitaetEinrichtung, bool forceIdeal = false, bool heizung = false, double heizTemp = 0, bool heizungIdeal = false, double heizlastKonstant=0, bool kuehlung =false, double kuehlTemp=0, bool kuehlungIdeal= false, double kuehllastKonstant=0, double fensterAufTemp=26, double sonnenschutzpunkt=26)
        {
            Name_id = name_id;

            Grundflaeche = grundflaeche;

            ForceIdeal = forceIdeal;

            Heizung = heizung;

            HeizTemp = heizTemp;

            HeizungIdeal = heizungIdeal;

            HeizlastKonstant = heizlastKonstant;

            Kuehlung = kuehlung;

            KuehlTemp = kuehlTemp;

            KuehlungIdeal = kuehlungIdeal;

            KuehllastKonstant = kuehllastKonstant;

            FensterAufTemp = fensterAufTemp;

            Sonnenschutzpunkt = sonnenschutzpunkt;

            StartTemp = startTemp;

            KapazitaetEinrichtung = kapazitaetEinrichtung;

        }

    }
}
