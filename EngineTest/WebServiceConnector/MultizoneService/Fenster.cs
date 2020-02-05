using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServiceConnector.MultizoneService
{
    public class Fenster
    {
        #region parameter

        private String zone_id;

        [JsonProperty(PropertyName = "zone")]
        public String Zone_id
        {
            get { return zone_id; }
            set { zone_id = value; }
        }

        private double breite;

        [JsonProperty(PropertyName = "breite")]
        public double Breite
        {
            get { return breite; }
            set { breite = value; }
        }

        private double hoehe;

        [JsonProperty(PropertyName = "hoehe")]
        public double Hoehe
        {
            get { return hoehe; }
            set { hoehe = value; }
        }

        private double u;

        [JsonProperty(PropertyName = "u")]
        public double U
        {
            get { return u; }
            set { u = value; }
        }

        private double g;

        [JsonProperty(PropertyName = "g")]
        public double G
        {
            get { return g; }
            set { g = value; }
        }

        private double svf;

        [JsonProperty(PropertyName = "sf")]
        public double Svf
        {
            get { return svf; }
            set { svf = value; }
        }


        #endregion

        /// <summary>
        /// Constructor for window
        /// </summary>
        /// <param name="zone_id">specific id of the zone</param>
        /// <param name="breite">Width of the window</param>
        /// <param name="hoehe">Height of the window</param>
        /// <param name="u">U value</param>
        /// <param name="g">g value</param>
        /// <param name="svf"> Visibility [0-1] portion of the visible sky</param>
        public Fenster(String zone_id, double breite, double hoehe, double u, double g, double svf)
        {
            
            Zone_id = zone_id;
            Breite = breite;
            Hoehe = hoehe;
            U = u;
            G = g;
            Svf = svf;
        }
    }
}
