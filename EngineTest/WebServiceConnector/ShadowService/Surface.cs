using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace WebServiceConnector.ShadowService
{
    /// <summary>
    /// This class describes polygons for the shadow service.
    /// </summary>
    public class Surface
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

        private List<PointVS> points = new List<PointVS>();
        /// <summary>
        /// Basic points of the polygon
        /// </summary>
        [JsonProperty(PropertyName = "punkte", Required = Required.Default)]
        public List<PointVS> Points
        {
            get { return points; }
            set { points = value; }
        }

        private List<Surface> openings;
        /// <summary>
        /// List of all openings in the traverse
        /// </summary>
        [JsonProperty(PropertyName = "fenster", Required = Required.Default)]
        public List<Surface> Openings
        {
            get { return openings; }
            set { openings = value; }
        }

        /// <summary>
        /// Constructor for a polygon (surface or opening)
        /// </summary>
        /// <param name="window">True if the polygon is an opening otherwise false</param>
        /// <param name="id">Unique identifier for the polygon</param>
        /// <param name="points">Basic points of the polygon</param>
        /// <param name="openings">Optional: List of all openings in the traverse</param>
        public Surface(bool window, String id, List<Point3D> points, List<Surface> openings = null)
        {
            Id = id;
            if (points != null)
            {
                foreach (Point3D p in points)
                {
                    var p1 = new PointVS(p);
                    Points.Add(p1);
                }
            }
            if (openings != null || window == false)
            {
                this.openings = new List<Surface>();
                if (openings != null)
                {
                    Openings = openings;
                }
            }
        }

    }
}
