using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

using GeometryViewer.EntityDXF;
using GeometryViewer.ComponentReps;

namespace GeometryViewer.ComponentInteraction
{

    public enum MaterialPosToWallAxisPlane { OUT, IN, MIDDLE }

    // placeholder for BAUTEIL
    public class Material : INotifyPropertyChanged
    {
        #region STATIC

        private static readonly System.IFormatProvider FORMAT;
        private static readonly string SUP2;

        public const float MAX_GUI_W = 20f;
        public const string MAT_DEF_NAME = "Aufbau.Default";
        public static readonly Material Default;
        internal static long NR_MATERIALS = 0;

        public const double DEFAULT_HALF_THICKNESS = 0.125;

        static Material()
        {
            FORMAT = new System.Globalization.NumberFormatInfo();
            char sup2 = '\u00B2';
            SUP2 = sup2.ToString();

            Material.Default = new Material(MAT_DEF_NAME, 0.5f, MaterialPosToWallAxisPlane.MIDDLE);
        }

        #region MaterialPosToWallAxisPlane
        public static MaterialPosToWallAxisPlane String2MPTWAP(string _pos)
        {
            if (string.IsNullOrEmpty(_pos)) return MaterialPosToWallAxisPlane.MIDDLE;

            switch(_pos)
            {
                case "OUT":
                    return MaterialPosToWallAxisPlane.OUT;
                case "IN":
                    return MaterialPosToWallAxisPlane.IN;
                default:
                    return MaterialPosToWallAxisPlane.MIDDLE;
            }
        }

        public static string MPTWAP2String(MaterialPosToWallAxisPlane _pos)
        {
            switch(_pos)
            {
                case MaterialPosToWallAxisPlane.OUT:
                    return "OUT";
                case MaterialPosToWallAxisPlane.IN:
                    return "IN";
                default:
                    return "MIDDLE";
            }
        }
        #endregion

        #endregion

        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region PROPERTIES (ID, Name, Thickness, Position)
        // main properties

        private long id;
        public long ID
        {
            get { return this.id; }
            set
            { 
                this.id = value;
                this.RegisterPropertyChanged("ID");
            }
        }

        private string name;
        public string Name 
        {
            get { return this.name; } 
            private set
            {
                this.name = value;
                this.RegisterPropertyChanged("Name");
            }
        }

        private float thickness;
        public float Thickness
        {
            get { return this.thickness; }
            set 
            { 
                this.thickness = value;
                this.AdjustOffsets();
            }
        }

        private MaterialPosToWallAxisPlane position;
        public MaterialPosToWallAxisPlane Position
        {
            get { return this.position; }
            set 
            { 
                this.position = value;
                this.AdjustOffsets();
            }
        }

        #endregion

        #region PROPERTIES: Accumulation of Area

        private float acc_area;
        public float AccArea
        {
            get { return this.acc_area; }
            private set
            {
                this.acc_area = value;
                this.RegisterPropertyChanged("AccArea");
                this.AccAreaString = this.acc_area.ToString("##00.00", FORMAT) + " m" + SUP2;
                if (this.BoundCR != null)
                    this.BoundCR.SynchronizeWMaterial(this);
            }
        }

        public int NrSurfaces { get; private set; }

        //derived
        private string acc_area_str = "00.00 m" + SUP2;
        public string AccAreaString
        {
            get { return this.acc_area_str; }
            set 
            {
                this.acc_area_str = value;
                this.RegisterPropertyChanged("AccAreaString");
            }
        }

        #endregion

        #region PROPETIES for connecting w ComponentReps

        private ComponentReps.CompRepAlignedWith bound_cr;
        /// <summary>
        /// Passes itself as WallConstr to the set value (CompRepAlignedWith).
        /// Adjusts the offsets according to the newly bound CR.
        /// </summary>
        public ComponentReps.CompRepAlignedWith BoundCR
        {
            get { return this.bound_cr; }
            set 
            { 
                if (this.bound_cr != null)
                {
                    this.bound_cr.WallConstr = null;
                }

                this.bound_cr = value;
                this.IsBound2CR = (this.bound_cr != null);
                
                if (this.bound_cr != null)
                {
                    this.bound_cr.WallConstr = this;
                    this.bound_cr_id = this.bound_cr.Comp_ID;
                    this.content = new List<ComponentReps.CompRepAlignedWith> { this.bound_cr };
                    this.name = this.bound_cr.Comp_Description;
                }
                else
                {
                    this.content = new List<ComponentReps.CompRepAlignedWith>();
                }
                this.RegisterPropertyChanged("BoundCR");
            }
        }
        // derived from above
        private List<ComponentReps.CompRepAlignedWith> content;
        public List<ComponentReps.CompRepAlignedWith> Content
        {
            get { return this.content; }
            private set
            {
                this.content = value;
                this.RegisterPropertyChanged("Content");
            }
        }

        // derived
        private bool is_bound_2_cr;
        public bool IsBound2CR
        {
            get { return this.is_bound_2_cr; }
            private set 
            { 
                this.is_bound_2_cr = value;
                this.RegisterPropertyChanged("IsBound2CR");
            }
        }

        // derived: for retrieval of the corresponding ComponentRepresentation
        private long bound_cr_id;
        public long BoundCRID { get { return this.bound_cr_id; } }

        #endregion

        #region PROPERTIES DERIVED (Offsets)
        // derived properties
        public float OffsetIn { get; private set; }
        public float OffsetOut { get; private set; }

        // derived display properties
        private float offset_in_gui;
        public float OffsetInGUI 
        {
            get { return this.offset_in_gui; }
            private set
            {
                this.offset_in_gui = value;
                this.RegisterPropertyChanged("OffsetInGUI");
            }
        }

        private float offset_out_gui;
        public float OffsetOutGUI 
        {
            get { return this.offset_out_gui; }
            private set
            {
                this.offset_out_gui = value;
                this.RegisterPropertyChanged("OffsetOutGUI");
            }
        }

        // derived: for communication to volumes
        private bool offsets_changed;
        public bool OffsetsChanged
        {
            get { return this.offsets_changed; }
            private set 
            { 
                this.offsets_changed = value;
                this.RegisterPropertyChanged("OffsetsChanged");
            }
        }


        #endregion


        #region .CTOR

        public Material(string _name, float _thickness, MaterialPosToWallAxisPlane _pos)
        {
            this.ID = (++Material.NR_MATERIALS);
            this.Name = _name;
            this.thickness = _thickness;
            this.Position = _pos;

            this.bound_cr = null;
            this.is_bound_2_cr = false;
            this.bound_cr_id = -1;

            this.acc_area = 0f;
            this.NrSurfaces = 0;
        }

        #endregion

        #region METHODS: Synchronization w CompReps

        internal void SetOffsets(CompRepAlignedWith _sender, double _offset_out, double _offset_in)
        {
            if (_sender == null) return;
            if (this.BoundCR == null) return;
            if (this.BoundCR.CR_ID != _sender.CR_ID) return;

            bool found_offset_change = false;

            found_offset_change = Math.Abs(this.Thickness - (float)(_offset_out + _offset_in)) > Utils.CommonExtensions.LINEDISTCALC_TOLERANCE;
            this.Thickness = (float)(_offset_out + _offset_in);

            MaterialPosToWallAxisPlane pos = MaterialPosToWallAxisPlane.MIDDLE;
            if (_offset_in * 10 < _offset_out)
                pos = MaterialPosToWallAxisPlane.IN;
            else if (_offset_out * 10 < _offset_in)
                pos = MaterialPosToWallAxisPlane.OUT;

            found_offset_change |= (this.Position == pos);
            this.Position = pos;

            this.OffsetsChanged = found_offset_change;
        }

        #endregion

        #region PARSER .CTOR

        internal Material(long _id, string _name, float _thickness, MaterialPosToWallAxisPlane _pos, float _accA, int _nr_surf, bool _is_bound2cr, long _bound_crid)
        {
            this.ID = _id;
            this.Name = _name;
            this.Thickness = _thickness;
            this.Position = _pos;

            this.AccArea = _accA;
            this.NrSurfaces = _nr_surf;
            this.bound_cr = null;
            this.is_bound_2_cr = _is_bound2cr;
            this.bound_cr_id = _bound_crid;
        }
        #endregion

        #region METHODS: Area accumulation

        public void AddSurface(float _area)
        {
            this.NrSurfaces++;
            this.AccArea += _area;
        }

        public void SubtractSurface(float _area)
        {
            this.NrSurfaces--;
            this.AccArea -= _area;
        }

        public void ResetSurfaceArea()
        {
            this.NrSurfaces = 0;
            this.AccArea = 0f;
        }

        #endregion

        #region UTILITY METHODS
        private void AdjustOffsets()
        {
            switch (this.position)
            {
                case MaterialPosToWallAxisPlane.IN:
                    this.OffsetIn = this.thickness;
                    this.OffsetOut = 0f;
                    break;
                case MaterialPosToWallAxisPlane.OUT:
                    this.OffsetIn = 0f;
                    this.OffsetOut = this.thickness;
                    break;
                case MaterialPosToWallAxisPlane.MIDDLE:
                    this.OffsetIn = this.thickness * 0.5f;
                    this.OffsetOut = this.thickness * 0.5f;
                    break;
            }
            if (this.thickness > 0)
            {
                float fact = (this.thickness > 1f) ? MAX_GUI_W / this.thickness : MAX_GUI_W;
                this.OffsetInGUI = this.OffsetIn * fact;
                this.OffsetOutGUI = this.OffsetOut * fact;
            }
        }

        #endregion

        #region UTILITY METHODS: To String

        public override string ToString()
        {
            int nrC = (this.content == null) ? 0 : this.content.Count;
            return this.name + " [" + nrC + "]";
        }

        public void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;
            string tmp = string.Empty;

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());  // 0
            _sb.AppendLine(DXFUtils.GV_MATERIAL);                           // GV_MATERIAL

            _sb.AppendLine(((int)EntitySaveCode.CLASS_NAME).ToString());    // 100 (subclass marker)
            _sb.AppendLine(this.GetType().ToString());                      // GeometryViewer.ComponentInteraction.Material

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_ID).ToString());     // 900 (custom code)
            _sb.AppendLine(this.ID.ToString());                             // e.g. 254

            _sb.AppendLine(((int)MaterialSaveCode.ID).ToString());          // 1301
            _sb.AppendLine(this.ID.ToString());

            _sb.AppendLine(((int)MaterialSaveCode.Name).ToString());        // 1302
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)MaterialSaveCode.THICKNESS).ToString());   // 1303
            _sb.AppendLine(DXFUtils.ValueToString(this.Thickness, "F8"));

            _sb.AppendLine(((int)MaterialSaveCode.POSITION).ToString());    // 1304
            _sb.AppendLine(Material.MPTWAP2String(this.Position));

            _sb.AppendLine(((int)MaterialSaveCode.ACC_AREA).ToString());    // 1305
            _sb.AppendLine(DXFUtils.ValueToString(this.AccArea, "F8"));

            tmp = (this.IsBound2CR) ? "1" : "0";
            _sb.AppendLine(((int)MaterialSaveCode.IS_BOUND_2CR).ToString()); // 1306
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)MaterialSaveCode.BOUND_CRID).ToString());    // 1307
            _sb.AppendLine(this.BoundCRID.ToString());

            _sb.AppendLine(((int)MaterialSaveCode.NR_ASSOC_SURFACES).ToString());    // 1308
            _sb.AppendLine(this.NrSurfaces.ToString());
        }

        #endregion
    }

    #region VALUE CONVERTERS
    [ValueConversion(typeof(Object), typeof(Boolean))]
    public class IsMaterialToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;

            return (value is Material);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }
    #endregion
}
