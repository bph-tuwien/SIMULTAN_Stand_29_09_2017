using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.EntityGeometry
{
    public class ZonedVolumeSurfaceVis : GeometricEntity
    {
        #region STATIC

        private static readonly System.IFormatProvider FORMAT;
        private static readonly string SUP2;
        static ZonedVolumeSurfaceVis()
        {
            FORMAT = new System.Globalization.NumberFormatInfo();
            char sup2 = '\u00B2';
            SUP2 = sup2.ToString();
        }

        #endregion

        #region OWNERSHIP Properties
        public ZonedVolume OwnerVolume { get; private set; }
        public bool IsWall { get; private set; }
        public int KeyInOwner { get; private set; }
        public long IDInOwner { get; private set; }
        public int OpeningIndex { get; private set; }

        #endregion

        #region DISPLAY Properties

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

        // material
        private ComponentInteraction.Material assoc_material;
        public ComponentInteraction.Material AssocMaterial
        {
            get { return this.assoc_material; }
            set 
            { 
                this.assoc_material = value;
                RegisterPropertyChanged("AssocMaterial");
            }
        }

        // area
        private float assoc_area;
        public float AssocArea
        {
            get { return this.assoc_area; }
            set 
            { 
                this.assoc_area = value;
                RegisterPropertyChanged("AssocArea");
                this.AssocAreaStr = this.assoc_area.ToString("00.00", FORMAT) + " m" + SUP2;
            }
        }

        private string assoc_area_str;
        public string AssocAreaStr 
        {
            get { return this.assoc_area_str; } 
            private set
            {
                this.assoc_area_str = value;
                RegisterPropertyChanged("AssocAreaStr");
            }
        }

        // geometry
        private List<Vector3> quad;
        private LineGeometry3D contour;

        #endregion

        #region CONSTRUCTORS
        public ZonedVolumeSurfaceVis(ZonedVolume _ownerV, bool _is_wall, long _idInOwner, int _key, 
                                     ICollection<Vector3> _quad, float _area, int _opening_ind = -1) 
            : base(null)
        {
            // ownership
            this.OwnerVolume = _ownerV;
            this.IsWall = _is_wall;
            this.KeyInOwner = _key;
            this.IDInOwner = _idInOwner;
            this.OpeningIndex = _opening_ind;

            // geometry
            this.quad = new List<Vector3>(_quad);
            
            this.AssocArea = _area;
            this.contour = null;
        }

        public void AdjustToVolumeChange(bool _is_wall, long _idInOwner, int _key, ICollection<Vector3> _quad, float _area, int _opening_ind = -1)
        {
            // ownership does not change
            this.IsWall = _is_wall;
            this.KeyInOwner = _key;
            this.IDInOwner = _idInOwner;
            this.OpeningIndex = _opening_ind;

            // geometry
            this.quad = new List<Vector3>(_quad);
            this.AssocArea = _area;
            this.contour = null;
        }

        #endregion

        #region OVERRIDES (Name, Geometry Visualization)
        public override string GetDafaultName()
        {
            return "Zone Surface";
        }

        public override LineGeometry3D Build(double _startMarkerSize = 0.1)
        {
            if (this.contour == null)
            {              
                if (this.quad == null)
                {
                    this.contour = null;
                    return null;
                }

                int n = this.quad.Count;
                if (n < 3)
                {
                    this.contour = null;
                    return null;
                }

                LineBuilder b = new LineBuilder();
                for(int i = 0; i < n; i++)
                {
                    b.AddLine(this.quad[i], this.quad[(i + 1) % n]);
                }
                this.contour = b.ToLineGeometry3D();
            }
            return this.contour;
        }

        #endregion

        #region METHODS: Size Info

        public void GetSize(out double height, out double width)
        {
            Utils.CommonExtensions.GetObjAlignedSizeOf(this.quad, out width, out height);
        }

        #endregion

    }
}
