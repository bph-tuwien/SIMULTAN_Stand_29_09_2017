using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.EntityGeometry
{
    public class ZoneOpeningVis : GeometricEntity
    {

        #region OWNERSHIP Properties

        private ZonedPolygon ownerPolygon;
        public ZonedPolygon OwnerPolygon
        {
            get { return this.ownerPolygon; }
            private set
            {
                this.ownerPolygon = value;
                RegisterPropertyChanged("OwnerPolygon");
            }
        }

        private int indInOwner;
        public int IndInOwner
        {
            get { return this.indInOwner; }
            set { 
                this.indInOwner = value;
                RegisterPropertyChanged("IndInOwner");
            }
        }

        private int idInOwner;
        public int IDInOwner { get { return this.idInOwner; } }

        #endregion

        // alias for displaying purposes
        private string label;
        public string Label
        {
            get { return this.label; }
            set
            {
                this.label = value;
                RegisterPropertyChanged("Label");
            }
        }

        #region POSITION and SIZE Properties

        private float distFromVertex;
        public float DistFromVertex
        {
            get { return this.distFromVertex; }
            set 
            { 
                this.distFromVertex = value;
                RegisterPropertyChanged("DistFromVertex");
            }
        }

        private float length;
        public float Length
        {
            get { return this.length; }
            set 
            { 
                this.length = value;
                RegisterPropertyChanged("Length");
            }
        }

        private float distFromFloorPolygon;
        public float DistFromFloorPolygon
        {
            get { return this.distFromFloorPolygon; }
            set 
            {
                this.distFromFloorPolygon = value;
                RegisterPropertyChanged("DistFromFloorPolygon");
            }
        }

        private float height;
        public float Height
        {
            get { return this.height; }
            set 
            { 
                this.height = value;
                RegisterPropertyChanged("Height");
            }
        }

        #endregion

        public ZoneOpeningVis(ZonedPolygon _ownerP, int _indInOwner, int _idInOwner, Layer _layer,
                                  float _distFromVertex, float _length, float _distFromFloorPolygon, float _height)
            : base(_layer)
        {
            // ownership
            this.OwnerPolygon = _ownerP;
            this.IndInOwner = _indInOwner;
            this.idInOwner = _idInOwner;

            // position and size
            this.DistFromVertex = _distFromVertex;
            this.Length = _length;
            this.DistFromFloorPolygon = _distFromFloorPolygon;
            this.Height = _height;
        }

        public override string GetDafaultName()
        {
            return "Zone Opening";
        }

        public override LineGeometry3D Build(double _startMarkerSize = 0.1)
        {
            List<Vector3> coords = this.OwnerPolygon.ExtractOpeningGeometry(this.idInOwner);

            LineBuilder b = new LineBuilder();
            if (coords.Count > 1)
            {
                // show opening on polygon
                b.AddLine(coords[0], coords[1]);
                b.AddBox(coords[0], _startMarkerSize, _startMarkerSize, _startMarkerSize);
                b.AddBox(coords[1], _startMarkerSize, _startMarkerSize, _startMarkerSize);
                
                // show owner vertex
                b.AddBox(this.OwnerPolygon.Polygon_Coords[this.IndInOwner].ToVector3(), 
                    _startMarkerSize, _startMarkerSize, _startMarkerSize);
            }
            return b.ToLineGeometry3D();
        }

    }
}
