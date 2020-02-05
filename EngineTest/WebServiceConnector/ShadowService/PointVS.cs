using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace WebServiceConnector.ShadowService
{
    /// <summary>
    /// Class for describing 3D Points or 3D Vectors for the shadow service
    /// </summary>
    public class PointVS
    {
        private double _x;

        public double x
        {
            get { return _x; }
            set { _x = value; }
        }

        private double _y;

        public double y
        {
            get { return _y; }
            set { _y = value; }
        }

        private double _z;

        public double z
        {
            get { return _z; }
            set { _z = value; }
        }

        /// <summary>
        /// Constructor of the 3D Point for the shadow service
        /// </summary>
        /// <param name="point">3D point (x,y,z)</param>
        public PointVS(Point3D point)
        {
            x = point.X;
            y = point.Y;
            z = point.Z;

        }

        /// <summary>
        /// Constructor of the 3D Vector for the sun vector
        /// </summary>
        /// <param name="sunVector">3D Vector representing the solar beam (x,y,z)</param>
        public PointVS(Vector3D sunVector)
        {
            x = sunVector.X;
            y = sunVector.Y;
            z = sunVector.Z;

        }

        /// <summary>
        /// Method for changing the values of x,y, and z
        /// </summary>
        /// <param name="sunVector">3D Vector representing the solar beam (x,y,z)</param>
        internal void change(Vector3D sunVector)
        {
            x = sunVector.X;
            y = sunVector.Y;
            z = sunVector.Z;
        }
    }
}
