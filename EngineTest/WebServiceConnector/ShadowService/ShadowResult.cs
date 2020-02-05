using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServiceConnector.ShadowService
{
    public class ShadowResult
    {
        private String id;
        /// <summary>
        /// Unique identifier for the polygon
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public String Id
        {
            get { return id; }
            set { id = value; }
        }

        private double svf;
        /// <summary>
        /// Visibility [0-1] portion of the visible sky
        /// </summary>
        [JsonProperty(PropertyName = "svf")]
        public double Svf
        {
            get { return svf; }
            set { svf = value; }
        }

        private double verschattung;

        /// <summary>
        /// Shading factor [0-1]
        /// </summary>
        [JsonProperty(PropertyName = "verschattung")]
        public double Verschattung
        {
            get { return verschattung; }
            set { verschattung = value; }
        }


        /// <summary>
        /// Constructor for the result of the shadow service
        /// </summary>
        /// <param name="id">Unique identifier for the polygon</param>
        /// <param name="svf">Visibility [0-1] portion of the visible sky: 1 means total sky is visible (e.g., horizontal surface up without surfaces in the surrounding area)</param>
        /// <param name="verschattung">Shading factor [0-1]: 0 - unshaded 1 - shaded </param>
        public ShadowResult(String id, double svf, double verschattung)
        {
            Id = id;
            Svf = svf;
            Verschattung = verschattung;
        }
    }
}
