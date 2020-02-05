using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;


namespace GeometryViewer.EntityGeometry
{
    public class ArchitecturalLine : GeometricEntity
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================== PROPERTIES AND CLASS MEMBERS ================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region LINE GEOMETRIC PROPERTIES

        private List<Point3D> coords0;
        public List<Point3D> Coords0
        {
            get { return this.coords0; }
            set 
            { 
                this.coords0 = value;
                RegisterPropertyChanged("Coords0");
            }
        }

        private List<Point3D> coords1;
        public List<Point3D> Coords1
        {
            get { return this.coords1; }
            set 
            { 
                this.coords1 = value;
                RegisterPropertyChanged("Coords1");
            }
        }

        private List<int> connected;
        public List<int> Connected
        {
            get { return this.connected; }
            set 
            { 
                this.connected = value;
                RegisterPropertyChanged("Connected");
            }
        }

        private float lineThickness;
        public float LineThickness
        {
            get { return this.lineThickness; }
            set 
            { 
                this.lineThickness = value;
                this.LineThicknessGUI = Math.Min(value * 2f, 8f);
                RegisterPropertyChanged("LineThickness");
            }
        }

        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ INITIALIZERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INIT: Instance

        public ArchitecturalLine(Layer _layer, List<Point3D> _coords0, List<Point3D> _coords1, List<int> _conn) 
            : base(_layer)
        {
            this.HasGeometryType = EnityGeometryType.LINE;
            this.Coords0 = _coords0;
            this.Coords1 = _coords1;
            this.Connected = _conn;
            this.ValidateGeometry();
        }

        public ArchitecturalLine(string _name, Layer _layer, List<Point3D> _coords0, List<Point3D> _coords1, List<int> _conn) 
            : base(_name, _layer)
        {
            this.HasGeometryType = EnityGeometryType.LINE;
            this.Coords0 = _coords0;
            this.Coords1 = _coords1;
            this.Connected = _conn;
            this.ValidateGeometry();
        }

        public ArchitecturalLine(ArchitecturalLine _original)
            : base("Copy of " + _original.EntityName, _original.EntityLayer)
        {
            // entity (w/o ID, Name, IsValid, IsSelected, LineThicknessGUI)
            this.EntityColor = _original.EntityColor;
            this.Visibility = _original.Visibility;
            this.ContainedEntities = _original.ContainedEntities;
            this.ShowZones = _original.ShowZones;
            this.ShowCtrlPoints = _original.ShowCtrlPoints;
            this.IsExpanded = _original.IsExpanded;

            // geometric entity
            this.HasGeometry = true;
            this.ColorByLayer = _original.ColorByLayer;

            // architecural line
            this.HasGeometryType = EnityGeometryType.LINE;
            this.Coords0 = _original.Coords0;
            this.Coords1 = _original.Coords1;
            this.Connected = _original.Connected;
            this.ValidateGeometry();
            this.LineThickness = _original.LineThickness;
        }

        public override string GetDafaultName()
        {
            return "Architectural Line";
        }

        #endregion

        #region INIT: Static (incl. Subtypes)

        public static ArchitecturalLine CreateArchitecturalEntity(string _name, Layer _layer,
                                                List<Point3D> _coords0, List<Point3D> _coords1, List<int> _connected,
                                                List<string> _text, SharpDX.Matrix _textTransform)
        {
            ArchitecturalLine aLine;
            int nrTextLines = _text.Count;

            if (nrTextLines > 0)
            {
                aLine = new ArchitecturalText(_name, _layer, _coords0, _coords1, _connected, _text, _textTransform);
            }
            else
            {
                aLine = new ArchitecturalLine(_name, _layer, _coords0, _coords1, _connected);
            }

            return aLine;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CLASS METHODS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region VALIDATION

        protected void ValidateGeometry()
        {
            if (this.Coords0 == null || this.Coords1 == null || this.Connected == null)
            {
                this.IsValid = false;
            }
            else
            {
                int n = this.Coords0.Count;
                int m = this.Coords1.Count;
                int k = this.Connected.Count;
                if (n < 1 || n != m || n != k)
                    this.IsValid = false;
                else
                    this.IsValid = true;
            }
        }

        #endregion

        #region GEOMETRY EXTRACTION
        public override LineGeometry3D Build(double _startMarkerSize = 0.1)
        {
            this.ValidateGeometry();
            if (!this.IsValid)
                return null;

            int n = this.Coords0.Count;

            LineBuilder b1 = new LineBuilder();
            b1.AddBox(this.Coords0[0].ToVector3(), _startMarkerSize, _startMarkerSize, _startMarkerSize);
            b1.AddLine(this.Coords0[0].ToVector3(), this.Coords1[0].ToVector3());
            if (n == 1)
                b1.AddBox(this.Coords1[0].ToVector3(), _startMarkerSize, _startMarkerSize, _startMarkerSize);
            
            int nextInd = this.Connected[0];
            if (nextInd == -1 && n > 1)
                b1.AddBox(this.Coords1[0].ToVector3(), _startMarkerSize, _startMarkerSize, _startMarkerSize);

            for (int i = 1; i < n; i++)
            {
                // next segment regardless of connectivity
                b1.AddLine(this.Coords0[i].ToVector3(), this.Coords1[i].ToVector3());

                // mark end of connected segment
                nextInd = this.Connected[i];
                if (nextInd == -1)
                {
                    b1.AddBox(this.Coords1[i].ToVector3(), _startMarkerSize, _startMarkerSize, _startMarkerSize);
                    if (i < (n - 1))
                        b1.AddBox(this.Coords0[i + 1].ToVector3(), _startMarkerSize, _startMarkerSize, _startMarkerSize);
                }
            }

            return b1.ToLineGeometry3D();
        
        }

        public virtual LineGeometry3D BuildSelectionGeometry()
        {
            this.ValidateGeometry();
            if (!this.IsValid)
                return null;

            int n = this.Coords0.Count;
            LineBuilder b1 = new LineBuilder();
            for (int i = 0; i < n; i++)
            {
                b1.AddLine(this.Coords0[i].ToVector3(), this.Coords1[i].ToVector3());
            }

            return b1.ToLineGeometry3D();
        }

        public List<Point3D> ExtractPolygonChain()
        {
            List<Point3D> tmpPolygon = new List<Point3D>();

            int m = this.Coords0.Count;
            if (m > 1)
            {
                tmpPolygon.Add(this.Coords0[0]);
                int prevInd = -1;
                foreach (int nextInd in this.Connected)
                {
                    if (nextInd != -1)
                        tmpPolygon.Add(this.Coords0[nextInd]);
                    else
                    {
                        if (prevInd != -1)
                            tmpPolygon.Add(this.Coords1[prevInd]);
                        break;
                    }
                    prevInd = nextInd;
                }
            }

            return tmpPolygon;
        }

        #endregion

    }
}
