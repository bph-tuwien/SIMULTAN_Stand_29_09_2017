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
    public class ArchitecturalText : ArchitecturalLine
    {
        #region TEXT Properties
        
        private List<string> text;
        public override List<string> Text
        {
            get { return this.text; }
            set 
            { 
                this.text = value;
                this.TextChanged = true;
                RegisterPropertyChanged("Text");
            }
        }

        private SharpDX.Matrix textTransform;
        public SharpDX.Matrix TextTransform
        {
            get { return this.textTransform; }
            private set 
            { 
                this.textTransform = value;
                this.TextChanged = true;
                RegisterPropertyChanged("TextTransform");
            }
        }

        private bool textChanged;
        public bool TextChanged
        {
            get { return this.textChanged; }
            set
            { 
                this.textChanged = value;
                RegisterPropertyChanged("TextChanged");
            }
        }


        private BoundingBox textBounds;
        public BoundingBox TextBounds
        {
            get { return this.textBounds; }
            private set 
            { 
                this.textBounds = value;
                RegisterPropertyChanged("TextBounds");
            }
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ INITIALIZERS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INIT

        public ArchitecturalText(Layer _layer, List<Point3D> _coords0, List<Point3D> _coords1, List<int> _conn,
                                List<string> _text, SharpDX.Matrix _textTransform) 
            : base(_layer, _coords0, _coords1, _conn)
        {
            // text properties
            this.Text = _text;
            this.TextTransform = _textTransform;
        }

        public ArchitecturalText(string _name, Layer _layer, List<Point3D> _coords0, List<Point3D> _coords1, List<int> _conn,
                                List<string> _text, SharpDX.Matrix _textTransform) 
            : base(_name, _layer, _coords0, _coords1, _conn)
        {
            // text properties
            this.Text = _text;
            this.TextTransform = _textTransform;
        }

        public ArchitecturalText(ArchitecturalText _original)
            : base("Copy of " + _original.EntityName, _original.EntityLayer, _original.Coords0, _original.Coords1, _original.Connected)
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
            base.ValidateGeometry();
            this.LineThickness = _original.LineThickness;

            // text
            this.Text = new List<string>(_original.Text);
            this.TextTransform = _original.TextTransform;
        }

        public override string GetDafaultName()
        {
            return "Architectural Text";
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CLASS METHODS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GEOMETRY EXTRACTION

        private void TextToGeometry()
        {
            List<Point3D>  coords0 = new List<Point3D>();
            List<Point3D>  coords1 = new List<Point3D>();
            List<int> connected = new List<int>();

            // process text, if there is any
            int nrTextLines = this.Text.Count;
            for (int j = 0; j < nrTextLines; j++)
            {
                Utils.FontAssembler.ConvertTextToGeometry(this.Text[j], this.TextTransform,
                                                            ref coords0, ref coords1, ref connected, j);
            }

            this.Coords0 = new List<Point3D>(coords0);
            this.Coords1 = new List<Point3D>(coords1);
            this.Connected = new List<int>(connected);

            this.TextBounds = BoundingBox.FromPoints(Utils.CommonExtensions.ConvertPoint3DArToVector3Ar(Coords0.ToArray()));
            this.TextChanged = false;
        }

        public override LineGeometry3D Build(double _startMarkerSize = 0.1)
        {
            if (this.TextChanged)
                this.TextToGeometry();

            base.ValidateGeometry();
            if (!this.IsValid)
                return null;

            int n = this.Coords0.Count;
            LineBuilder b1 = new LineBuilder();
            for (int i = 1; i < n; i++)
            {
                b1.AddLine(this.Coords0[i].ToVector3(), this.Coords1[i].ToVector3());
            }

            return b1.ToLineGeometry3D();
        }

        public override LineGeometry3D BuildSelectionGeometry()
        {
            if (this.TextChanged)
                this.TextToGeometry();

            base.ValidateGeometry();
            if (!this.IsValid)
                return null;

            LineBuilder b1 = new LineBuilder();
            b1.AddLine(TextBounds.Minimum, new Vector3(TextBounds.Maximum.X, TextBounds.Minimum.Y, TextBounds.Minimum.Z));
            b1.AddLine(new Vector3(TextBounds.Maximum.X, TextBounds.Minimum.Y, TextBounds.Minimum.Z), TextBounds.Maximum);
            b1.AddLine(TextBounds.Maximum, new Vector3(TextBounds.Minimum.X, TextBounds.Maximum.Y, TextBounds.Maximum.Z));
            b1.AddLine(new Vector3(TextBounds.Minimum.X, TextBounds.Maximum.Y, TextBounds.Maximum.Z), TextBounds.Minimum);

            return b1.ToLineGeometry3D();
        }

        #endregion
    }
}
