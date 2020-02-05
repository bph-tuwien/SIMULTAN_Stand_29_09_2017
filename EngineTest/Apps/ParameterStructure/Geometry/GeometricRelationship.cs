using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Media.Media3D;

using ParameterStructure.DXF;

namespace ParameterStructure.Geometry
{
    #region ENUMS

    // [1601 - 1700]
    public enum GeometricRelationshipSaveCode
    {
        NAME = 1601,
        STATE_TYPE = 1602,
        STATE_ISREALIZED = 1609,
        GEOM_IDS_X = 1603,
        GEOM_IDS_Y = 1604,
        GEOM_IDS_Z = 1605,
        GEOM_IDS_W = 1630,
        GEOM_CS = 1606,
        TRANSF_WC2LC = 1607,
        TRANSF_LC2WC = 1608,
        INST_SIZE = 1620,
        INST_NWE_ID = 1621,
        INST_NWE_NAME = 1622,
        INST_PATH = 1623,
        INST_SIZE_TRANSSETTINGS = 1640,
        INST_SIZE_TS_SOURCE = 1641,
        INST_SIZE_TS_INITVAL = 1642,
        INST_SIZE_TS_PARNAME = 1643,
        INST_SIZE_TS_CORRECT = 1644
    }

    public enum GeometricRelationshipUpdateState
    {
        NEUTRAL = 0,
        NEW = 1,
        TO_BE_DELETED = 2
    }

    #endregion

    #region STRUCT: size transfer

    public enum GeomSizeTransferSource
    {
        USER = 0,
        PARAMETER = 1,
        PATH = 2
    }

    public struct GeomSizeTransferDef
    {
        #region STATIC

        public static string GeomSizeTransferSourceToString(GeomSizeTransferSource _source)
        {
            switch(_source)
            {
                case GeomSizeTransferSource.PARAMETER:
                    return "PARAMETER";
                case GeomSizeTransferSource.PATH:
                    return "PATH";
                default:
                    return "USER";
            }
        }

        public static GeomSizeTransferSource StringToGeomSizeTransferSource(string _source_as_str)
        {
            if (string.IsNullOrEmpty(_source_as_str)) return GeomSizeTransferSource.USER;

            switch(_source_as_str)
            {
                case "PARAMETER":
                    return GeomSizeTransferSource.PARAMETER;
                case "PATH":
                    return GeomSizeTransferSource.PATH;
                default:
                    return GeomSizeTransferSource.USER;
            }
        }

        #endregion

        public GeomSizeTransferSource Source { get; set; }
        public double InitialValue { get; set; }
        public string ParameterName { get; set; }
        public double Correction { get; set; }
    }

    #endregion

    public class GeometricRelationship : INotifyPropertyChanged
    {
        #region STATIC

        protected static long NR_GEOMETRICRELATIONSHIPS = 0;
        public static System.IFormatProvider NR_FORMATTER = new System.Globalization.NumberFormatInfo();

        public static bool ReferenceSameGeometry(GeometricRelationship _gr1, GeometricRelationship _gr2)
        {
            if (_gr1 == null || _gr2 == null) return false;
            if (_gr1.State.Type != _gr2.State.Type) return false;
            if (!_gr1.State.IsRealized || !_gr2.State.IsRealized) return false;

            switch(_gr1.State.Type)
            {
                case Relation2GeomType.DESCRIBES_3D:               
                case Relation2GeomType.GROUPS:
                    return _gr1.GeomIDs.X == _gr2.GeomIDs.X;
                case Relation2GeomType.DESCRIBES_2DorLESS:
                    return _gr1.GeomIDs.X == _gr2.GeomIDs.X &&
                           _gr1.GeomIDs.Y == _gr2.GeomIDs.Y;
                case Relation2GeomType.ALIGNED_WITH:
                case Relation2GeomType.CONTAINED_IN:
                case Relation2GeomType.CONNECTS:
                    return  _gr1.GeomIDs.X == _gr2.GeomIDs.X && 
                            _gr1.GeomIDs.Y == _gr2.GeomIDs.Y && 
                            _gr1.GeomIDs.Z == _gr2.GeomIDs.Z;
                default:
                    return false;
            }
        }

        protected static string Point3DToString(Point3D _p, string _start_symbol, string _end_symbol, string _format = "F2")
        {
            return _start_symbol + Parameter.Parameter.ValueToString(_p.X, _format) +
                   ";" + Parameter.Parameter.ValueToString(_p.Y, _format) +
                   ";" + Parameter.Parameter.ValueToString(_p.Z, _format) +
                   _end_symbol;
        }

        protected static double GetPathLength(List<Point3D> _path)
        {
            if (_path == null) return 0.0;
            if (_path.Count < 3) return 0.0;

            double length = 0.0;
            for(int i = 1; i < _path.Count - 1; i++)
            {
                length += (_path[i] - _path[i + 1]).Length;
            }

            return length;
        }

        internal static void AdjustInstanceIds(ref List<GeometricRelationship> _instances)
        {
            if (_instances == null || _instances.Count == 0)
                return;

            // existing: 0 1 2 3 -> GeometricRelationship.NR_GEOMETRICRELATIONSHIPS = 4
            // new: 6 7 12 14
            List<long> all_ids = _instances.Select(x => x.ID).ToList();
            if (all_ids.Min() > GeometricRelationship.NR_GEOMETRICRELATIONSHIPS - 1)
                return;

            // existing: 0 1 2 3 -> GeometricRelationship.NR_GEOMETRICRELATIONSHIPS = 4
            // new: 2 3 6 7
            // shift all ids by an offset (4 - 2) = 2 -> 2 3 6 7 --> 4 5 8 9
            long offset = GeometricRelationship.NR_GEOMETRICRELATIONSHIPS - all_ids.Min();
            GeometricRelationship.NR_GEOMETRICRELATIONSHIPS = all_ids.Max() + offset + 1; // 10

            foreach(GeometricRelationship gr in _instances)
            {
                gr.ID += offset;
            }
        }

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

        #region PROPERTIES: Transforms

        // World Coordinates -> Local (Object) Coordinates
        protected Matrix3D tr_wc2lc;
        public Matrix3D TRm_WC2LC 
        {
            get { return this.tr_wc2lc; }
            set
            {
                this.tr_wc2lc = value;
                this.RegisterPropertyChanged("TRm_WC2LC");
            }
        }

        // Local (Object) Coordinates -> World Coordinates
        protected Matrix3D tr_lc2wc;
        public Matrix3D TRm_LC2WC
        {
            get { return this.tr_lc2wc; }
            set 
            { 
                this.tr_lc2wc = value;
                this.RegisterPropertyChanged("TRm_LC2WC");
            }
        }

        #endregion

        #region PROPERTIES: ID, TypeName, State

        protected long id;
        public long ID
        {
            get { return this.id; }
            private set 
            { 
                this.id = value;
                this.RegisterPropertyChanged("ID");
            }
        }


        protected string name;
        public string Name
        {
            get { return this.name; }
            set 
            { 
                this.name = value;
                this.RegisterPropertyChanged("Name");
            }
        }

        protected Relation2GeomState state;
        public Relation2GeomState State
        {
            get { return this.state; }
            set 
            {
                this.state = value;
                this.RegisterPropertyChanged("State");
            }
        }

        #endregion

        #region PROPERTIES: Referenced Geometry

        // [0]: id of ZonedVolume
        // [1]: label of the wall quad or index of the ZonedPolygonGroup
        // [2]: index of the opening in the wall
        // [3]: id of the lower defining polygon of a wall
        protected Point4D geom_ids;
        public Point4D GeomIDs
        {
            get { return this.geom_ids; }
            set 
            { 
                this.geom_ids = value; 
                this.RegisterPropertyChanged("GeomIDs");
            }
        }

        // the coordinate system (in WC) of the referenced geometry
        protected Matrix3D geom_cs;
        public Matrix3D GeomCS
        {
            get { return this.geom_cs; }
            set 
            { 
                this.geom_cs = value;
                this.RegisterPropertyChanged("GeomCS");
            }
        }

        #endregion

        #region PROPERTIES: For Instancing Components (e.g. HVAC placements)

        // REQUIRED SEQUENCE OF CALLS: InstanceSize.set, InstanceSizeTransferSettings.set, Component.UpdateCumulativeValuesFromInstances
        protected List<double> instance_size;
        public List<double> InstanceSize
        {
            get { return this.instance_size; }
            internal set 
            { 
                this.instance_size = value; // in the schema view
                this.RegisterPropertyChanged("InstanceSize");
            }
        }

        // added 12.08.2017
        // REQUIRED SEQUENCE OF CALLS: InstanceSize.set, InstanceSizeTransferSettings.set, Component.UpdateCumulativeValuesFromInstances
        protected List<GeomSizeTransferDef> instance_size_transfer_settings;
        public List<GeomSizeTransferDef> InstanceSizeTransferSettings 
        {
            get { return this.instance_size_transfer_settings; } 
            internal set
            {
                this.instance_size_transfer_settings = value;
                this.ApplySizeTransferSettings();
                // this requires that the component's cumulative values are re-calculated!!!
            }
        }

        protected long instance_nwe_id;
        public long InstanceNWElementID
        {
            get { return this.instance_nwe_id; }
            protected set 
            { 
                this.instance_nwe_id = value; // in the schema view
                this.RegisterPropertyChanged("InstanceNWElementID");
            }
        }

        protected string instance_nwe_name;
        public string InstanceNWElementName
        {
            get { return this.instance_nwe_name; }
            protected set 
            { 
                this.instance_nwe_name = value; // in the schema view
                this.RegisterPropertyChanged("InstanceNWElementName");
            }
        }

        protected List<Point3D> instance_path;
        public List<Point3D> InstancePath
        {
            get { return this.instance_path; }
            set
            { 
                this.instance_path = value; // in the GeometryViewer
                this.instance_path_length = GeometricRelationship.GetPathLength(this.instance_path);
                this.ApplySizeTransferSettings();
                this.RegisterPropertyChanged("InstancePath");
            }
        }    
        // added 12.08.2017
        protected double instance_path_length;


        // derived - only for display
        public string DerivedSizeMinStr
        {
            get
            {
                if (this.InstanceSize.Count > 2)
                {
                    return Parameter.Parameter.ValueToString(this.InstanceSize[0], "F2") + " " +
                       Parameter.Parameter.ValueToString(this.InstanceSize[1], "F2") + " " +
                       Parameter.Parameter.ValueToString(this.InstanceSize[2], "F2");
                }
                else
                    return string.Empty;
            }
        }

        public string DerivedSizeMaxStr
        {
            get
            {
                if (this.InstanceSize.Count > 5)
                {
                    return Parameter.Parameter.ValueToString(this.InstanceSize[3], "F2") + " " +
                       Parameter.Parameter.ValueToString(this.InstanceSize[4], "F2") + " " +
                       Parameter.Parameter.ValueToString(this.InstanceSize[5], "F2");
                }
                else
                    return string.Empty;
            }
        }

        public Utils.NamedStringList DerivedInstancePathStr
        {
            get
            {
                List<string> path_vertices_as_str = new List<string>();
                if (this.InstancePath != null && this.InstancePath.Count > 1)
                {
                    // 1st entry has the connectivity information
                    path_vertices_as_str.Add("[N" + this.InstancePath[0].X + " → N" + this.InstancePath[0].Y + "]");
                    for(int i = 1; i < this.InstancePath.Count; i++)
                    {
                        if (i == 1 || i == this.InstancePath.Count - 1)
                            path_vertices_as_str.Add(GeometricRelationship.Point3DToString(this.InstancePath[i], "[", "]"));
                        else
                            path_vertices_as_str.Add(GeometricRelationship.Point3DToString(this.InstancePath[i], "(", ")"));
                    }
                }
                return new Utils.NamedStringList(path_vertices_as_str);
            }
        }

        #endregion

        #region PROPERTIES: Parameters for Instancing Components (NOT to be saved and NOT to be transferred to GV)

        protected Dictionary<string, double> instance_param_values;
        /// <summary>
        /// <para>To be used only for calculations within a flow network for saving intermediate values for each of the parent component's parameters.</para>
        /// <para>NOTE: Do not update every time a parameter in the parent component changes, update directly before a flow network calculation.</para>
        /// <para>HOWEVER: If a single value in this container changes, it has to be transferred to the recepients, if necessary (e.g. size).</para>
        /// </summary>
        public Dictionary<string, double> InstanceParamValues 
        {
            get { return this.instance_param_values; }
            internal set
            {
                this.instance_param_values = value;
                this.ApplySizeTransferSettings();
            }
        }

        // derived - for display only
        public Utils.NamedStringList DerivedInstanceParamValues
        {
            get
            {
                List<string> params_as_str = new List<string>();
                if (this.DerivedInstanceParamDisplayState == null || this.DerivedInstanceParamDisplayState.Count != this.InstanceParamValues.Count)
                {
                    foreach (var entry in this.InstanceParamValues)
                    {
                        Parameter.ParameterReservedNameRecord p_res = Parameter.Parameter.RESERVED_NAMES.FirstOrDefault(x => x.Name == entry.Key);
                        if (p_res != null) continue;
                        params_as_str.Add(entry.Key + ": " + Parameter.Parameter.ValueToString(entry.Value, "F2"));
                    }
                }
                else
                {
                    // hanlde the display of parameters in the instance
                    for(int i = 0; i < this.InstanceParamValues.Count; i++)
                    {
                        var entry = this.InstanceParamValues.ElementAt(i);
                        Parameter.ParameterReservedNameRecord p_res = Parameter.Parameter.RESERVED_NAMES.FirstOrDefault(x => x.Name == entry.Key);
                        if (p_res != null) continue;

                        if (this.DerivedInstanceParamDisplayState.ElementAt(i).Value)
                            params_as_str.Add(entry.Key + ": " + Parameter.Parameter.ValueToString(entry.Value, "F2"));
                    }
                }
                
                return new Utils.NamedStringList(params_as_str);
            }
        }

        public Dictionary<string, bool> DerivedInstanceParamDisplayState { get; internal set; }

        #endregion

        #region PROPERTIES (derived): for displaying the geometry saved in the path

        public List<HierarchicalContainer> GeometricContent
        {
            get
            {
                if (this.instance_path == null || this.instance_path.Count < 2)
                    return new List<HierarchicalContainer>();
                else
                {
                    List<HierarchicalContainer> geom_content = new List<HierarchicalContainer>();
                    for(int i = 1; i < this.instance_path.Count; i++)
                    {
                        geom_content.Add(new Point3DContainer(this.ID, i, this.instance_path[i]));
                    }
                    return geom_content;
                }
            }
        }

        #endregion

        #region PROPERTIES: UpdateState (only for internal purposes)

        internal GeometricRelationshipUpdateState UpdateState { get; set; }

        #endregion

        #region .CTOR

        public GeometricRelationship(string _name, Relation2GeomState _state, Point4D _ids, Matrix3D _cs, Matrix3D _tr_WC2LC, Matrix3D _tr_LC2WC)
        {
            // general
            this.ID = (++GeometricRelationship.NR_GEOMETRICRELATIONSHIPS);
            this.Name = (string.IsNullOrEmpty(_name)) ? "Geometry" : _name;
            this.State = _state;
            this.GeomIDs = _ids;
            this.GeomCS = _cs;
            this.TRm_WC2LC = _tr_WC2LC;
            this.TRm_LC2WC = _tr_LC2WC;

            // instance information
            this.instance_size = new List<double>();
            this.instance_size_transfer_settings = new List<GeomSizeTransferDef>();
            this.instance_nwe_id = -1L;
            this.instance_nwe_name = "NW_Element";
            this.InstancePath = new List<Point3D>();
            this.InstanceParamValues = new Dictionary<string, double>(); // transient!

            this.UpdateState = GeometricRelationshipUpdateState.NEUTRAL;
        }

        public GeometricRelationship(string _name, Relation2GeomState _state, Point4D _ids, Matrix3D _cs)
            :this(_name, _state, _ids, _cs, Matrix3D.Identity, Matrix3D.Identity)
        { }

        public GeometricRelationship(string _name, Relation2GeomState _state, Point4D _ids)
            :this(_name, _state, _ids, Matrix3D.Identity, Matrix3D.Identity, Matrix3D.Identity)
        { }

        public GeometricRelationship()
            : this("Geometry", new Relation2GeomState(), new Point4D(-1, -1, -1, -1), 
                    Matrix3D.Identity, Matrix3D.Identity, Matrix3D.Identity)
        {  }

        #endregion

        #region .CTOR PARSING

        internal GeometricRelationship(long _id, string _name, Relation2GeomState _state, Point4D _ids, 
                                        Matrix3D _cs, Matrix3D _tr_WC2LC, Matrix3D _tr_LC2WC)
        {
            this.ID = _id;
            GeometricRelationship.NR_GEOMETRICRELATIONSHIPS = Math.Max(GeometricRelationship.NR_GEOMETRICRELATIONSHIPS, _id);
            
            this.Name = _name;
            this.State = _state;
            this.GeomIDs = _ids;
            this.GeomCS = _cs;
            this.TRm_WC2LC = _tr_WC2LC;
            this.TRm_LC2WC = _tr_LC2WC;
        }

        internal GeometricRelationship(long _id, string _name, Relation2GeomState _state, Point4D _ids,
                                        Matrix3D _cs, Matrix3D _tr_WC2LC, Matrix3D _tr_LC2WC,
                                        List<double> _i_size, List<GeomSizeTransferDef> _i_size_tr, long _i_nwe_id, string _i_nwe_name, List<Point3D> _i_path)
        {
            this.ID = _id;
            GeometricRelationship.NR_GEOMETRICRELATIONSHIPS = Math.Max(GeometricRelationship.NR_GEOMETRICRELATIONSHIPS, _id);

            this.Name = _name;
            this.State = _state;
            this.GeomIDs = _ids;
            this.GeomCS = _cs;
            this.TRm_WC2LC = _tr_WC2LC;
            this.TRm_LC2WC = _tr_LC2WC;

            this.InstanceSize = new List<double>(_i_size);
            this.InstanceNWElementID = _i_nwe_id;
            this.InstanceNWElementName = _i_nwe_name;
            this.InstancePath = new List<Point3D>(_i_path);
            this.InstanceParamValues = new Dictionary<string, double>(); // transient!

            // last, so that everything is transferred after initialization
            if (_i_size_tr != null && _i_size_tr.Count > 5)
                this.InstanceSizeTransferSettings = new List<GeomSizeTransferDef>(_i_size_tr); // setter calls snychronization with size
            else
            {
                // LEGACY HELPER: for earlier versions of GeometricRelationships w/o transfer settings
                this.instance_size_transfer_settings = new List<GeomSizeTransferDef>();
                for(int i = 0; i < 6; i++)
                {
                    this.instance_size_transfer_settings.Add(new GeomSizeTransferDef
                    {
                        Source = GeomSizeTransferSource.USER,
                        ParameterName = string.Empty,
                        Correction = 0.0,
                        InitialValue = 0.0
                    });
                }
            }
             
            // LEGACY HELPER: for connecting GR w/o check for IsRealized after the placement of the sart and end contained_in instances
            // ???
        }


        #endregion

        #region ToString

        public override string ToString()
        {
            //// VERSION 1.
            //string output = "GR[" + this.ID + "][ zv:" + this.GeomIDs.X + " zvs:" + this.GeomIDs.Y + " v:" + this.GeomIDs.Z + "]";
            //return output;

            // VERSION 2.
            string output = "{" + this.ID + ": { ";
            foreach(double s in this.InstanceSize)
            {
                output += Parameter.Parameter.ValueToString(s, "F2") + " ";
            }
            output += "}, { ";

            foreach(var entry in this.InstanceParamValues)
            {
                output += "\"" + entry.Key + "\": " + Parameter.Parameter.ValueToString(entry.Value, "F2") + " ";
            }

            output += "} }";

            return output;
        }

        public string GetSizeInfoString(bool _max)
        {
            string size_info = "[]";
            if(_max)
            {
                if (this.InstanceSize.Count > 5)
                {
                    size_info = "[ H:" + Parameter.Parameter.ValueToString(this.InstanceSize[3], "F2") +
                                 " B:" + Parameter.Parameter.ValueToString(this.InstanceSize[4], "F2") +
                                 " L:" + Parameter.Parameter.ValueToString(this.InstanceSize[5], "F2") + " ]";
                }
            }
            else
            {
                if (this.InstanceSize.Count > 2)
                {
                    size_info = "[ h:" + Parameter.Parameter.ValueToString(this.InstanceSize[0], "F2") +
                                 " b:" + Parameter.Parameter.ValueToString(this.InstanceSize[1], "F2") +
                                 " l:" + Parameter.Parameter.ValueToString(this.InstanceSize[2], "F2") + " ]";
                }
            }

            return size_info;
        }

        public virtual void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.GEOM_RELATION);                           // GEOM_RELATIONSHIP

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // id, name, state
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.ID.ToString());

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.STATE_TYPE).ToString());
            _sb.AppendLine(GeometryUtils.Relationship2GeometryToString(this.State.Type));

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.STATE_ISREALIZED).ToString());
            string tmp = (this.State.IsRealized) ? "1" : "0";
            _sb.AppendLine(tmp);

            // referenced geometry
            _sb.AppendLine(((int)GeometricRelationshipSaveCode.GEOM_IDS_X).ToString());
            _sb.AppendLine(this.GeomIDs.X.ToString());

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.GEOM_IDS_Y).ToString());
            _sb.AppendLine(this.GeomIDs.Y.ToString());

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.GEOM_IDS_Z).ToString());
            _sb.AppendLine(this.GeomIDs.Z.ToString());

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.GEOM_IDS_W).ToString());
            _sb.AppendLine(this.GeomIDs.W.ToString());

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.GEOM_CS).ToString());
            _sb.AppendLine(this.GeomCS.ToString(GeometricRelationship.NR_FORMATTER));

            // transforms
            _sb.AppendLine(((int)GeometricRelationshipSaveCode.TRANSF_WC2LC).ToString());
            _sb.AppendLine(this.TRm_WC2LC.ToString(GeometricRelationship.NR_FORMATTER));

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.TRANSF_LC2WC).ToString());
            _sb.AppendLine(this.TRm_LC2WC.ToString(GeometricRelationship.NR_FORMATTER));

            // ---------------------- INSTANCE INFORMATION ----------------------------- //
            //  size
            _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_SIZE).ToString());
            _sb.AppendLine(this.instance_size.Count.ToString());

            foreach(double size_value in this.instance_size)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(Parameter.Parameter.ValueToString(size_value, "F8"));
            }

            // size transfer settings
            _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_SIZE_TRANSSETTINGS).ToString());
            _sb.AppendLine(this.instance_size_transfer_settings.Count.ToString());

            foreach(GeomSizeTransferDef size_trs in this.instance_size_transfer_settings)
            {
                _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_SIZE_TS_SOURCE).ToString());
                _sb.AppendLine(GeomSizeTransferDef.GeomSizeTransferSourceToString(size_trs.Source));

                _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_SIZE_TS_INITVAL).ToString());
                _sb.AppendLine(Parameter.Parameter.ValueToString(size_trs.InitialValue, "F8"));

                _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_SIZE_TS_PARNAME).ToString());
                _sb.AppendLine(size_trs.ParameterName);

                _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_SIZE_TS_CORRECT).ToString());
                _sb.AppendLine(Parameter.Parameter.ValueToString(size_trs.Correction, "F8"));
            }

            // network element info
            _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_NWE_ID).ToString());
            _sb.AppendLine(this.instance_nwe_id.ToString());

            _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_NWE_NAME).ToString());
            _sb.AppendLine(this.instance_nwe_name.ToString());

            // path
            _sb.AppendLine(((int)GeometricRelationshipSaveCode.INST_PATH).ToString());
            _sb.AppendLine(this.instance_path.Count.ToString());

            foreach(Point3D vertex in this.instance_path)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(Parameter.Parameter.ValueToString(vertex.X, "F8"));

                _sb.AppendLine(((int)ParamStructCommonSaveCode.Y_VALUE).ToString());
                _sb.AppendLine(Parameter.Parameter.ValueToString(vertex.Y, "F8"));

                _sb.AppendLine(((int)ParamStructCommonSaveCode.Z_VALUE).ToString());
                _sb.AppendLine(Parameter.Parameter.ValueToString(vertex.Z, "F8"));
            }
        }


        #endregion


        #region METHODS: Reset

        internal void Reset()
        {
            // general
            Relation2GeomState state_old = this.State;
            this.state = new Relation2GeomState { IsRealized = false, Type = state_old.Type };
            this.GeomIDs = new Point4D(-1, -1, -1, -1);
            this.GeomCS = Matrix3D.Identity;
            this.TRm_WC2LC = Matrix3D.Identity;
            this.TRm_LC2WC = Matrix3D.Identity;

            // instance information
            this.instance_size = new List<double>();
            this.instance_size_transfer_settings = new List<GeomSizeTransferDef>();
            this.instance_nwe_id = -1L;
            this.instance_nwe_name = "NW_Element";
            this.InstancePath = new List<Point3D>();
            this.InstanceParamValues = new Dictionary<string, double>(); // transient!

            this.UpdateState = GeometricRelationshipUpdateState.NEUTRAL;
        }

        #endregion

        #region METHODS: Instance Definition / Update

        /// <summary>
        /// Bad method! Use only when merging networks from different files!
        /// </summary>
        /// <param name="_id_new"></param>
        /// <param name="_name_new"></param>
        internal void UpdateContainerInfo(long _id_new, string _name_new)
        {
            this.InstanceNWElementID = _id_new;
            this.InstanceNWElementName = _name_new;
        }

        /// <summary>
        /// <para>Updates the local coordinate system if _nwe is a node.</para>
        /// <para>If _nwe is an edge, it also updates the path. If the geometric relationship is already realized nothing changes.</para>
        /// </summary>
        internal void UpdatePositionFrom(Component.FlNetElement _nwe, System.Windows.Point _offset, double _scale)
        {
            if (this.State.IsRealized) return;
            if (_nwe == null) return;

            List<Point3D> gr_path;
            Matrix3D gr_ucs;
            GeometricRelationship.DeriveGeomPositionOutOfPlacementIn(_nwe, _offset, _scale, out gr_ucs, out gr_path);

            this.GeomCS = gr_ucs;
            if (this.State.Type == Relation2GeomType.CONNECTS)
                this.InstancePath = gr_path;
        }

        internal void PlaceInFlowNetworkElement(Component.FlNetElement _nwe, System.Windows.Point _offset, double _scale, List<double> _sizes, Dictionary<string, double> _params)
        {
            if (this.State.IsRealized) return;

            if (_nwe == null || _sizes == null || _params == null) return;
            if (_sizes.Count < 6) return;
            
            List<Point3D> gr_path;
            Matrix3D gr_ucs;
            GeometricRelationship.DeriveGeomPositionOutOfPlacementIn(_nwe, _offset, _scale, out gr_ucs, out gr_path);
            this.GeomCS = gr_ucs;
            if (this.State.Type == Relation2GeomType.CONNECTS)
                this.InstancePath = gr_path;           

            this.InstanceNWElementID = _nwe.ID;
            this.InstanceNWElementName = _nwe.Name;
            this.InstanceSize = new List<double>(_sizes);
            this.instance_size_transfer_settings = new List<GeomSizeTransferDef>(); // per default NONE (i.e. direct user input into the InstanceSize list)
            this.InstanceParamValues = new Dictionary<string, double>(_params);
        }

        internal static GeometricRelationship CreateAndPlaceInFlowNetworkElement(Component.FlNetElement _nwe, System.Windows.Point _offset, double _scale, Dictionary<string, double> _params)
        {
            if (_nwe == null) return null;

            Relation2GeomState gr_state = new Relation2GeomState
            {
                IsRealized = false,
                Type = (_nwe is Component.FlNetEdge) ? Relation2GeomType.CONNECTS : Relation2GeomType.CONTAINED_IN
            };

            List<Point3D> gr_path;
            Matrix3D gr_ucs;
            GeometricRelationship.DeriveGeomPositionOutOfPlacementIn(_nwe, _offset, _scale, out gr_ucs, out gr_path);

            GeometricRelationship gr_placement = new GeometricRelationship("placement", gr_state, new Point4D(-1, -1, -1, -1),
                                                                           gr_ucs, Matrix3D.Identity, Matrix3D.Identity); // todo: adjust the transforms...

            gr_placement.instance_size = new List<double> { 0, 0, 0, 0, 0, 0};
            gr_placement.instance_size_transfer_settings = new List<GeomSizeTransferDef>();
            gr_placement.instance_nwe_id = _nwe.ID;
            gr_placement.instance_nwe_name = _nwe.Name;
            gr_placement.InstancePath = gr_path;
            gr_placement.InstanceParamValues = (_params == null) ? new Dictionary<string, double>() : new Dictionary<string, double>(_params);

            return gr_placement;
        }

        /// <summary>
        /// <para>Calculates the local coordinate system based on the position on the editor canvas.</para>
        /// <para>Calculates a simple path for edges which contains the ids of the start and end nodes in the first path entry.</para>
        /// </summary>
        private static void DeriveGeomPositionOutOfPlacementIn(Component.FlNetElement _nwe, System.Windows.Point _offset, double _scale, out Matrix3D ucs, out List<Point3D> path)
        {
            ucs = Matrix3D.Identity;
            path = new List<Point3D>();
            if (_nwe == null) return;

            Point3D pivot = new Point3D(0, 0, 0);
            System.Windows.Point offset = new System.Windows.Point(_offset.X * _scale, _offset.Y * _scale);
            List<Vector3D> axes = new List<Vector3D>(); // x, y, z
            if (_nwe is Component.FlNetNode)
            {
                Component.FlNetNode node = _nwe as Component.FlNetNode;
                if (!(node.IsValid)) return;

                pivot.X = node.Position.X * _scale + offset.X;
                pivot.Z = node.Position.Y * _scale + offset.Y;

                axes.Add(new Vector3D(1, 0, 0));
                axes.Add(new Vector3D(0, 0, 1));
                axes.Add(new Vector3D(0, 1, 0));
            }
            else if (_nwe is Component.FlNetEdge)
            {
                Component.FlNetEdge edge = _nwe as Component.FlNetEdge;
                if (!(edge.IsValid)) return;

                pivot.X = edge.Start.Position.X * _scale + offset.X;
                pivot.Z = edge.Start.Position.Y * _scale + offset.Y;

                // the first entry in the path saves the ids of the start and end nodes to 
                // communicate connectivity to the GeometryViewer
                long start_id = edge.Start.ID;
                long end_id = edge.End.ID;
                if (edge.Start is Component.FlowNetwork)
                {
                    Component.FlowNetwork nw_start = edge.Start as Component.FlowNetwork;
                    Component.FlNetNode nN = nw_start.SortAndGetLastNode();
                    if (nN != null)
                        start_id = nN.ID;
                }
                if (edge.End is Component.FlowNetwork)
                {
                    Component.FlowNetwork nw_end = edge.End as Component.FlowNetwork;
                    Component.FlNetNode n1 = nw_end.SortAndGetFirstNode();
                    if (n1 != null)
                        end_id = n1.ID;
                }

                path.Add(new Point3D(start_id, end_id, -1));
                path.Add(new Point3D(edge.Start.Position.X * _scale + offset.X, 0, edge.Start.Position.Y * _scale + offset.Y));
                path.Add(new Point3D(edge.End.Position.X * _scale + offset.X, 0, edge.End.Position.Y * _scale + offset.Y));

                Vector3D axis_x = path[1] - path[0];
                axis_x.Normalize();
                axes.Add(axis_x);
                axes.Add(new Vector3D(0, 0, 1));
                Vector3D axis_z = Vector3D.CrossProduct(axes[0], axes[1]);
                axis_z.Normalize();
                axes.Add(axis_z);

            }

            ucs = GeometricTransforms.PackUCS(pivot, axes[0], axes[1], axes[2]);
        }

        #endregion

        #region METHODS: SIZE TRANSFER

        /// <summary>
        /// <para>Depending on the information in this.instance_size_transfer_settings</para>
        /// <para>the size values are transferred from the appropriate resources (parameters, path or not at all).</para>
        /// <para>This method is called in InstaneSizeTransferSettings.set and InstanceParamValues.set</para>
        /// <para>It needs to be called after a change in: InstancePath and every time a single entry in InstanceParamValues is updated.</para>
        /// </summary>
        internal void ApplySizeTransferSettings()
        {
            if (this.instance_size == null || this.instance_size_transfer_settings == null) return;

            int nrS = this.instance_size.Count;
            if (nrS < 6 || nrS != this.instance_size_transfer_settings.Count) return;

            for(int i = 0; i < nrS; i++)
            {
                if (this.instance_size_transfer_settings[i].Source == GeomSizeTransferSource.USER) continue;

                double new_value = this.instance_size[i];
                if (this.instance_size_transfer_settings[i].Source == GeomSizeTransferSource.PARAMETER &&
                    !string.IsNullOrEmpty(this.instance_size_transfer_settings[i].ParameterName))
                {
                    if (this.InstanceParamValues.ContainsKey(this.instance_size_transfer_settings[i].ParameterName))
                    {
                        new_value = this.InstanceParamValues[this.instance_size_transfer_settings[i].ParameterName] +
                                    this.instance_size_transfer_settings[i].Correction;
                    }
                }
                else if (this.instance_size_transfer_settings[i].Source == GeomSizeTransferSource.PATH &&
                         this.instance_path != null && this.instance_path.Count > 2)
                {
                    new_value = this.instance_path_length + this.instance_size_transfer_settings[i].Correction;
                }

                this.instance_size[i] = new_value;
            }
        }

        public double ApplySizeTransferSettingsTo(int _index_in_size, double _size, GeomSizeTransferDef _transfer_settings)
        {
            if (_index_in_size < 0 || _index_in_size > 5) return 0.0;
            if (this.instance_size == null || this.instance_size_transfer_settings == null) return 0.0;

            int nrS = this.instance_size.Count;
            if (nrS < _index_in_size + 1) return 0.0;
            if (this.instance_size_transfer_settings.Count < _index_in_size + 1) return this.instance_size[_index_in_size];

            // save to internal containers
            this.instance_size[_index_in_size] = _size;
            this.instance_size_transfer_settings[_index_in_size] = _transfer_settings;

            // perform transfer
            if (this.instance_size_transfer_settings[_index_in_size].Source == GeomSizeTransferSource.USER)
                return this.instance_size[_index_in_size];

            double new_value = this.instance_size[_index_in_size];
            if (this.instance_size_transfer_settings[_index_in_size].Source == GeomSizeTransferSource.PARAMETER &&
                !string.IsNullOrEmpty(this.instance_size_transfer_settings[_index_in_size].ParameterName))
            {
                if (this.InstanceParamValues.ContainsKey(this.instance_size_transfer_settings[_index_in_size].ParameterName))
                {
                    new_value = this.InstanceParamValues[this.instance_size_transfer_settings[_index_in_size].ParameterName] +
                                this.instance_size_transfer_settings[_index_in_size].Correction;
                }
            }
            else if (this.instance_size_transfer_settings[_index_in_size].Source == GeomSizeTransferSource.PATH &&
                     this.instance_path != null && this.instance_path.Count > 2)
            {
                new_value = this.instance_path_length + this.instance_size_transfer_settings[_index_in_size].Correction;
            }

            this.instance_size[_index_in_size] = new_value;
            return new_value;
        }

        #endregion

        #region METHODS: CONNECTIVITY BASED RELAIZATION

        internal static void UpdateConnectivityBasedRelaization(GeometricRelationship _n1, GeometricRelationship _n2, ref GeometricRelationship _connector)
        {
            if (_n1 == null || _n2 == null || _connector == null) return;

            if (_n1.State.Type == Geometry.Relation2GeomType.CONTAINED_IN &&
                _n2.State.Type == Geometry.Relation2GeomType.CONTAINED_IN &&
                _connector.State.Type == Geometry.Relation2GeomType.CONNECTS)
            {
                if (_n1.State.IsRealized && _n2.State.IsRealized)
                    _connector.State = new Relation2GeomState { Type = Geometry.Relation2GeomType.CONNECTS, IsRealized = true };
                else
                    _connector.State = new Relation2GeomState { Type = Geometry.Relation2GeomType.CONNECTS, IsRealized = false };
            }
        }

        #endregion
    }
}
