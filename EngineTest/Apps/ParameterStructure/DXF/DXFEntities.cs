using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

using ParameterStructure.Values;
using ParameterStructure.Parameter;
using ParameterStructure.Component;
using ParameterStructure.EXCEL;
using ParameterStructure.Geometry;
using ParameterStructure.Mapping;

namespace ParameterStructure.DXF
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================= BASE TYPE: ENTITY ======================================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region DXF_Entity
    public class DXFEntity
    {
        #region CLASS MEMBERS
        public string ENT_Name { get; protected set; }
        public string ENT_ClassName { get; protected set; }
        public long ENT_ID { get; protected set; }
        public string ENT_KEY { get; protected set; }
        public bool ENT_HasEntites { get; protected set; }
        internal DXFDecoder Decoder { get; set; }

        internal bool defer_OnLoading;
        internal bool defer_AddEntity;

        #endregion

        #region .CTOR

        public DXFEntity()
        {
            this.ENT_Name = null;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;

            this.defer_OnLoading = false;
            this.defer_OnLoading = false;
        }

        #endregion

        #region METHODS: Entity parsing

        public void ParseNext()
        {
            // start parsing next entity
            this.ReadProperties();
            // if it contains entities itself, parse them next
            if (this.ENT_HasEntites)
                this.ReadEntities();
        }

        protected void ReadEntities()
        {
            DXFEntity e;
            do
            {
                if (this.Decoder.FValue == ParamStructTypes.EOF)
                {
                    // end of file
                    this.Decoder.ReleaseRessources();
                    return;
                }
                e = this.Decoder.CreateEntity();
                if (e == null)
                {
                    // reached end of complex entity
                    this.Decoder.Next();
                    break;
                }
                if (e is DXFContinue)
                {
                    // carry on parsing the same entity
                    this.ParseNext();
                    break;
                }
                e.ParseNext();
                if (e.GetType().IsSubclassOf(typeof(DXFEntity)))
                {
                    // complete parsing
                    e.OnLoaded();
                    // add to list of entities of this entity
                    this.AddEntity(e);
                }
            }
            while (this.Decoder.HasNext());
        }

        #endregion

        #region METHODS: Property parsing

        protected void ReadProperties()
        {
            while (this.Decoder.HasNext())
            {
                this.Decoder.Next();
                switch (this.Decoder.FCode)
                {
                    case (int)ParamStructCommonSaveCode.ENTITY_START:
                        // reached next entity
                        return;
                    default:
                        // otherwise continue parsing
                        this.ReadPoperty();
                        break;
                }
            }
        }

        public virtual void ReadPoperty()
        {
            switch(this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.CLASS_NAME:
                    this.ENT_ClassName = this.Decoder.FValue;
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_ID:
                    this.ENT_ID = this.Decoder.LongValue();
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_KEY:
                    this.ENT_KEY = this.Decoder.FValue;
                    break;
            }
        }

        #endregion

        #region METHODS: For Subtypes

        public virtual void OnLoaded() { }
        public virtual bool AddEntity(DXFEntity _e)
        {
            return false;
        }

        internal virtual void AddDeferredEntities()
        { }

        #endregion

    }
    #endregion

    #region DXF_Dummy_Entity
    public class DXFDummy : DXFEntity
    {
        public DXFDummy()
        {
            this.ENT_Name = null;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;
        }
        public DXFDummy(string _name)
        {
            this.ENT_Name = _name;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            if (this.ENT_Name != null)
                dxfS += "[" + this.ENT_Name + "]";

            return dxfS;
        }
    }

    #endregion

    #region DXF_CONTINUE

    public class DXFContinue : DXFEntity
    {
        public DXFContinue()
        {
            this.ENT_Name = ParamStructTypes.ENTITY_CONTINUE;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;
        }
    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================== CUSTOM ENTITIES ========================================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // abstract ancestor of DXF_ValueField, DXF_FunctionField, DXF_BigTable
    #region DXF_Field

    public abstract class DXFField : DXFEntity
    {
        #region CLASS MEMBERS
        
        // general
        public long dxf_MVID { get; protected set; }
        public MultiValueType dxf_MVType { get; protected set; }
        public string dxf_MVName { get; protected set; }
        public bool dxf_MVCanInterpolate { get; protected set; }

        // display vector (for choosing values or interpolation)
        protected int dxf_mvdv_num_dim;
        protected List<int> dxf_mvdv_cell_indices;
        protected Point dxf_mvdv_cell_size;
        protected Point dxf_mvdv_pos_rel;
        protected Point dxf_mvdv_pos_abs;
        protected double dxf_mvdv_value;
        public MultiValPointer dxf_MVDisplayVector { get; private set; }

        // general info
        public string dxf_MVUnitX { get; protected set; }
        public string dxf_MVUnitY { get; protected set; }
        public string dxf_MVUnitZ { get; protected set; }

        #endregion

        public DXFField()
            :base()
        {
            this.dxf_MVName = string.Empty;
            this.dxf_mvdv_num_dim = 0;
            this.dxf_mvdv_cell_indices = new List<int> { -1, -1, -1, -1 };
            this.dxf_mvdv_cell_size = new Point(0, 0);
            this.dxf_mvdv_pos_rel = new Point(0, 0);
            this.dxf_mvdv_pos_abs = new Point(0, 0);
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)MultiValueSaveCode.MVType:
                    this.dxf_MVType = (MultiValueType)this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVName:
                    this.dxf_MVName = this.Decoder.FValue;
                    break;
                case (int)MultiValueSaveCode.MVCanInterpolate:
                    this.dxf_MVCanInterpolate = (this.Decoder.IntValue() == 1) ? true : false;
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_NUMDIM:
                    this.dxf_mvdv_num_dim = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_X:
                    this.dxf_mvdv_cell_indices[0] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Y:
                    this.dxf_mvdv_cell_indices[1] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Z:
                    this.dxf_mvdv_cell_indices[2] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_W:
                    this.dxf_mvdv_cell_indices[3] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_W:
                    this.dxf_mvdv_cell_size.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_H:
                    this.dxf_mvdv_cell_size.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_X:
                    this.dxf_mvdv_pos_rel.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Y:
                    this.dxf_mvdv_pos_rel.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Z:
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_X:
                    this.dxf_mvdv_pos_abs.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Y:
                    this.dxf_mvdv_pos_abs.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Z:
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_VALUE:
                    this.dxf_mvdv_value = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVUnitX:
                    this.dxf_MVUnitX = this.Decoder.FValue;
                    break;
                case (int)MultiValueSaveCode.MVUnitY:
                    this.dxf_MVUnitY = this.Decoder.FValue;
                    break;
                case (int)MultiValueSaveCode.MVUnitZ:
                    this.dxf_MVUnitZ = this.Decoder.FValue;
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_MVID = this.ENT_ID;
                    break;
            }
        }

        #endregion
    }

    #endregion

    // wrapper for class MultiValueTable
    #region DXF_ValueField

    public class DXFMultiValueField : DXFField
    {
        #region CLASS MEMBERS
        
        // specific info
        public int dxf_NrX { get; protected set; }
        public double dxf_MinX { get; protected set; }
        public double dxf_MaxX { get; protected set; }

        public int dxf_NrY { get; protected set; }
        public double dxf_MinY { get; protected set; }
        public double dxf_MaxY { get; protected set; }

        public int dxf_NrZ { get; protected set; }
        public double dxf_MinZ { get; protected set; }
        public double dxf_MaxZ { get; protected set; }

        // actual value field
        protected int dxf_nr_xs;
        protected int dxf_nr_xs_read;

        protected int dxf_nr_ys;
        protected int dxf_nr_ys_read;

        protected int dxf_nr_zs;
        protected int dxf_nr_zs_read;

        protected int dxf_nr_field_vals;
        protected int dxf_nr_field_vals_read;
        protected Point4D dxf_field_entry_current;
        protected Point4D dxf_field_entry_current_read;

        public List<double> dxf_Xs { get; private set; }
        public List<double> dxf_Ys { get; private set; }
        public List<double> dxf_Zs { get; private set; }
        public Dictionary<Point3D, double> dxf_Field { get; protected set; }

        #endregion

        public DXFMultiValueField()
            :base()
        {
            this.dxf_nr_xs = 0;
            this.dxf_nr_xs_read = 0;

            this.dxf_nr_ys = 0;
            this.dxf_nr_ys_read = 0;

            this.dxf_nr_zs = 0;
            this.dxf_nr_zs_read = 0;

            this.dxf_nr_field_vals = 0;
            this.dxf_nr_field_vals_read = 0;
            this.dxf_field_entry_current = new Point4D(0, 0, 0, 0);
            this.dxf_field_entry_current_read = new Point4D(0, 0, 0, 0);

            this.dxf_Xs = new List<double>();
            this.dxf_Ys = new List<double>();
            this.dxf_Zs = new List<double>();
            this.dxf_Field = new Dictionary<Point3D, double>();
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)MultiValueSaveCode.NrX:
                    this.dxf_NrX = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MinX:
                    this.dxf_MinX = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxX:
                    this.dxf_MaxX = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.NrY:
                    this.dxf_NrY = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MinY:
                    this.dxf_MinY = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxY:
                    this.dxf_MaxY = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.NrZ:
                    this.dxf_NrZ = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MinZ:
                    this.dxf_MinZ = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxZ:
                    this.dxf_MaxZ = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.XS:
                    // marks the start of the sequence of values along the X axis
                    this.dxf_nr_xs = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.YS:
                    // marks the start of the sequence of values along the Y axis
                    this.dxf_nr_ys = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.ZS:
                    // marks the start of the sequence of values along the Z axis
                    this.dxf_nr_zs = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.FIELD:
                    // marks the start of the sequence of values in the FIELD
                    this.dxf_nr_field_vals = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_xs > this.dxf_nr_xs_read)
                    {
                        this.dxf_Xs.Add(this.Decoder.DoubleValue());
                        this.dxf_nr_xs_read++;
                    }
                    else if (this.dxf_nr_ys > this.dxf_nr_ys_read)
                    {
                        this.dxf_Ys.Add(this.Decoder.DoubleValue());
                        this.dxf_nr_ys_read++;
                    }
                    else if (this.dxf_nr_zs > this.dxf_nr_zs_read)
                    {
                        this.dxf_Zs.Add(this.Decoder.DoubleValue());
                        this.dxf_nr_zs_read++;
                    }
                    else if (this.dxf_nr_field_vals > this.dxf_nr_field_vals_read)
                    {
                        if (this.dxf_field_entry_current_read.X == 0)
                        {
                            this.dxf_field_entry_current.X = this.Decoder.DoubleValue();
                            this.dxf_field_entry_current_read.X = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_field_vals > this.dxf_nr_field_vals_read)
                    {
                        if (this.dxf_field_entry_current_read.Y == 0)
                        {
                            this.dxf_field_entry_current.Y = this.Decoder.DoubleValue();
                            this.dxf_field_entry_current_read.Y = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    if (this.dxf_nr_field_vals > this.dxf_nr_field_vals_read)
                    {
                        if (this.dxf_field_entry_current_read.Z == 0)
                        {
                            this.dxf_field_entry_current.Z = this.Decoder.DoubleValue();
                            this.dxf_field_entry_current_read.Z = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.W_VALUE:
                    if (this.dxf_nr_field_vals > this.dxf_nr_field_vals_read)
                    {
                        if (this.dxf_field_entry_current_read.W == 0)
                        {
                            this.dxf_field_entry_current.W = this.Decoder.DoubleValue();
                            this.dxf_field_entry_current_read.W = 1;
                        }
                        // check if the entry was parsed completely
                        if (this.dxf_field_entry_current_read.X == 1 &&
                            this.dxf_field_entry_current_read.Y == 1 &&
                            this.dxf_field_entry_current_read.Z == 1 &&
                            this.dxf_field_entry_current_read.W == 1)
                        {
                            Point3D key = new Point3D(this.dxf_field_entry_current.X,
                                                      this.dxf_field_entry_current.Y,
                                                      this.dxf_field_entry_current.Z);
                            if (!this.dxf_Field.ContainsKey(key))
                                this.dxf_Field.Add(key, this.dxf_field_entry_current.W);
                            this.dxf_field_entry_current_read = new Point4D(0, 0, 0, 0); 
                            this.dxf_nr_field_vals_read++;
                        }
                    }
                    break;
                default:
                    // DXFField: ENTITY_NAME, MVID, MVType, MVName, MVCanInterpolate,
                    // MVDisplayVector_NUMDIM, MVDisplayVector_CELL_INDEX_X, MVDisplayVector_CELL_INDEX_Y, MVDisplayVector_CELL_INDEX_Z, MVDisplayVector_CELL_INDEX_W,
                    // MVDisplayVector_POS_IN_CELL_REL_X, MVDisplayVector_POS_IN_CELL_REL_Y, MVDisplayVector_POS_IN_CELL_REL_Z,
                    // MVDisplayVector_POS_IN_CELL_ABS_X, MVDisplayVector_POS_IN_CELL_ABS_Y, MVDisplayVector_POS_IN_CELL_ABS_Z,
                    // MVDisplayVector_VALUE,
                    // MVUnitX, MVUnitY, MVUnitZ
                    //
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }


        #endregion

        #region OVERRIDES : Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.MV_Factory == null)
                return;

            // construct the pointer into the value field
            List<int> actual_indices = null;
            if (this.dxf_mvdv_num_dim <= 4)
                actual_indices = this.dxf_mvdv_cell_indices.Take(this.dxf_mvdv_num_dim).ToList();

            MultiValPointer pointer = new MultiValPointer(this.dxf_mvdv_num_dim, actual_indices, this.dxf_mvdv_cell_size,
                                                          this.dxf_mvdv_pos_rel, this.dxf_mvdv_pos_abs, this.dxf_mvdv_value);
            // check value lists for consistency
            bool data_consistent = true;
            data_consistent &= (this.dxf_nr_xs == this.dxf_nr_xs_read);
            data_consistent &= (this.dxf_nr_xs == this.dxf_NrX);
            data_consistent &= (this.dxf_nr_ys == this.dxf_nr_ys_read);
            data_consistent &= (this.dxf_nr_ys == this.dxf_NrY);
            data_consistent &= (this.dxf_nr_zs == this.dxf_nr_zs_read);
            data_consistent &= (this.dxf_nr_zs == this.dxf_NrZ);
            data_consistent &= (this.dxf_nr_field_vals == this.dxf_nr_field_vals_read);
            
            if (!data_consistent) return;

            // construct the value field
            this.Decoder.MV_Factory.ReconstructTable(this.ENT_ID, this.dxf_MVName, this.dxf_MVCanInterpolate, pointer,
                                                  this.dxf_NrX, this.dxf_MinX, this.dxf_MaxX, this.dxf_MVUnitX,
                                                  this.dxf_NrY, this.dxf_MinY, this.dxf_MaxY, this.dxf_MVUnitY,
                                                  this.dxf_NrZ, this.dxf_MinZ, this.dxf_MaxZ, this.dxf_MVUnitZ,
                                                  this.dxf_Xs, this.dxf_Ys, this.dxf_Zs, this.dxf_Field);
        }

        #endregion
    }

    #endregion

    // wrapper for class MultiValueFunction
    #region DXF_FunctionField

    public class DXFMultiValueFunction : DXFField
    {
        #region CLASS MEMBERS

        // specific info
        public double dxf_MinX { get; protected set; }
        public double dxf_MaxX { get; protected set; }
        public double dxf_MinY { get; protected set; }
        public double dxf_MaxY { get; protected set; }

        public int dxf_NrZ { get; protected set; }
        public double dxf_MinZ { get; protected set; }
        public double dxf_MaxZ { get; protected set; }

        // actual function field
        protected int dxf_nr_zs;
        protected int dxf_nr_zs_read;

        protected int dxf_nr_functions;
        protected int dxf_nr_functions_read;
        protected Point4D dxf_funct_point_current;
        protected Point4D dxf_funct_point_current_read;
        protected List<Point3D> dxf_funct_current;

        public List<double> dxf_Zs { get; protected set; }
        public List<List<Point3D>> dxf_FunctionGraphs { get; protected set; }

        protected int dxf_nr_fct_names;
        protected int dxf_nr_fct_names_read;

        public List<string> dxf_FunctionNames { get; protected set; }

        #endregion

        public DXFMultiValueFunction()
        {
            this.dxf_nr_zs = 0;
            this.dxf_nr_zs_read = 0;

            this.dxf_nr_functions = 0;
            this.dxf_nr_functions_read = 0;
            this.dxf_funct_point_current = new Point4D(0, 0, 0, 0);
            this.dxf_funct_point_current_read = new Point4D(0, 0, 0, 0);
            this.dxf_funct_current = new List<Point3D>();

            this.dxf_Zs = new List<double>();
            this.dxf_FunctionGraphs = new List<List<Point3D>>();

            this.dxf_nr_fct_names = 0;
            this.dxf_nr_fct_names_read = 0;

            this.dxf_FunctionNames = new List<string>();
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch(this.Decoder.FCode)
            {
                case (int)MultiValueSaveCode.MinX:
                    this.dxf_MinX = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxX:
                    this.dxf_MaxX = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MinY:
                    this.dxf_MinY = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxY:
                    this.dxf_MaxY = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.NrZ:
                    this.dxf_NrZ = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MinZ:
                    this.dxf_MinZ = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxZ:
                    this.dxf_MaxZ = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.ZS:
                    // marks the start of the sequence of values along the Z axis
                    this.dxf_nr_zs = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.FIELD:
                    // marks the start of the sequence of values in the FIELD
                    this.dxf_nr_functions = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.ROW_NAMES:
                    // marks the strt of the sequence of function names
                    this.dxf_nr_fct_names = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_zs > this.dxf_nr_zs_read)
                    {
                        this.dxf_Zs.Add(this.Decoder.DoubleValue());
                        this.dxf_nr_zs_read++;
                    }
                    else if (this.dxf_nr_functions > this.dxf_nr_functions_read)
                    {
                        if (this.dxf_funct_point_current_read.X == 0)
                        {
                            this.dxf_funct_point_current.X = this.Decoder.DoubleValue();
                            this.dxf_funct_point_current_read.X = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_functions > this.dxf_nr_functions_read)
                    {
                        if (this.dxf_funct_point_current_read.Y == 0)
                        {
                            this.dxf_funct_point_current.Y = this.Decoder.DoubleValue();
                            this.dxf_funct_point_current_read.Y = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    if (this.dxf_nr_functions > this.dxf_nr_functions_read)
                    {
                        if (this.dxf_funct_point_current_read.Z == 0)
                        {
                            this.dxf_funct_point_current.Z = this.Decoder.DoubleValue();
                            this.dxf_funct_point_current_read.Z = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.W_VALUE:
                    if (this.dxf_nr_functions > this.dxf_nr_functions_read)
                    {
                        if (this.dxf_funct_point_current_read.W == 0)
                        {
                            this.dxf_funct_point_current.W = this.Decoder.DoubleValue();
                            this.dxf_funct_point_current_read.W = 1;
                        }
                        // check if the entry was parsed completely
                        if (this.dxf_funct_point_current_read.X == 1 &&
                            this.dxf_funct_point_current_read.Y == 1 &&
                            this.dxf_funct_point_current_read.Z == 1 &&
                            this.dxf_funct_point_current_read.W == 1)
                        {
                            Point3D fpoint = new Point3D(this.dxf_funct_point_current.X,
                                                         this.dxf_funct_point_current.Y,
                                                         this.dxf_funct_point_current.Z);
                            this.dxf_funct_current.Add(fpoint);
                            this.dxf_funct_point_current_read = new Point4D(0, 0, 0, 0);

                            // finalize function
                            if (this.dxf_funct_point_current.W == ParamStructTypes.END_OF_LIST)
                            {
                                this.dxf_FunctionGraphs.Add(this.dxf_funct_current);
                                this.dxf_funct_current = new List<Point3D>();
                                this.dxf_nr_functions_read++;
                            }                               
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_fct_names > this.dxf_nr_fct_names_read)
                    {
                        this.dxf_FunctionNames.Add(this.Decoder.FValue);
                        this.dxf_nr_fct_names_read++;
                    }
                    break;
                default:
                    // DXFField: ENTITY_NAME, MVID, MVType, MVName, MVCanInterpolate,
                    // MVDisplayVector_NUMDIM, MVDisplayVector_CELL_INDEX_X, MVDisplayVector_CELL_INDEX_Y, MVDisplayVector_CELL_INDEX_Z, MVDisplayVector_CELL_INDEX_W,
                    // MVDisplayVector_POS_IN_CELL_REL_X, MVDisplayVector_POS_IN_CELL_REL_Y, MVDisplayVector_POS_IN_CELL_REL_Z,
                    // MVDisplayVector_POS_IN_CELL_ABS_X, MVDisplayVector_POS_IN_CELL_ABS_Y, MVDisplayVector_POS_IN_CELL_ABS_Z,
                    // MVDisplayVector_VALUE,
                    // MVUnitX, MVUnitY, MVUnitZ
                    //
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.MV_Factory == null)
                return;

            // construct the pointer into the value field
            List<int> actual_indices = null;
            if (this.dxf_mvdv_num_dim <= 4)
                actual_indices = this.dxf_mvdv_cell_indices.Take(this.dxf_mvdv_num_dim).ToList();

            MultiValPointer pointer = new MultiValPointer(this.dxf_mvdv_num_dim, actual_indices, this.dxf_mvdv_cell_size,
                                                          this.dxf_mvdv_pos_rel, this.dxf_mvdv_pos_abs, this.dxf_mvdv_value);
            // check value lists for consistency
            bool data_consistent = true;
            data_consistent &= (this.dxf_nr_zs == this.dxf_nr_zs_read);
            data_consistent &= (this.dxf_nr_zs == this.dxf_NrZ);
            data_consistent &= (this.dxf_nr_functions == this.dxf_nr_functions_read);

            if (!data_consistent) return;

            // construct the function field
            if (this.dxf_FunctionNames.Count != this.dxf_FunctionGraphs.Count)
            {
                for(int i = 0; i < this.dxf_FunctionGraphs.Count; i++)
                {
                    this.dxf_FunctionNames.Add("Fct " + i);
                }
            }
            Point4D bounds = new Point4D(this.dxf_MinX, this.dxf_MaxX, this.dxf_MinY, this.dxf_MaxY);
            this.Decoder.MV_Factory.ReconstructFunction(this.ENT_ID, this.dxf_MVName, this.dxf_MVCanInterpolate, pointer,
                                                        this.dxf_MVUnitX, this.dxf_MVUnitY, this.dxf_MVUnitZ, bounds,
                                                        this.dxf_Zs, this.dxf_FunctionGraphs, this.dxf_FunctionNames);
        }

        #endregion
    }

    #endregion

    // wrapper of class MultiValueBigTable
    #region DXF_BigTable

    public class DXFMultiValueBigTable : DXFField
    {
        #region CLASS MEMBER

        //specific info
        protected int dxf_nr_names;
        protected int dxf_nr_names_read;
        protected int dxf_nr_units;
        protected int dxf_nr_units_read;

        public List<string> dxf_Names { get; protected set; }
        public List<string> dxf_Units { get; protected set; }

        // actual values
        protected int dxf_nr_values_per_row;
        protected int dxf_nr_table_rows;
        protected int dxf_nr_table_rows_read;

        protected List<double> dxf_table_row_values;
        protected List<bool> dxf_table_row_values_read;

        public List<List<double>> dxf_Values { get; protected set; }

        // row names
        protected int dxf_nr_row_names;
        protected int dxf_nr_row_names_read;

        public List<string> dxf_RowNames { get; protected set; }

        #endregion

        public DXFMultiValueBigTable()
        {
            this.dxf_nr_names = 0;
            this.dxf_nr_names_read = 0;
            this.dxf_nr_units = 0;
            this.dxf_nr_units_read = 0;

            this.dxf_Names = new List<string>();
            this.dxf_Units = new List<string>();

            this.dxf_nr_values_per_row = 0;
            this.dxf_nr_table_rows = 0;
            this.dxf_nr_table_rows_read = 0;
            this.dxf_table_row_values = Enumerable.Repeat(double.NaN, ExcelStandardImporter.MAX_NR_VALUE_COLUMNS).ToList();
            this.dxf_table_row_values_read = Enumerable.Repeat(false, ExcelStandardImporter.MAX_NR_VALUE_COLUMNS).ToList();

            this.dxf_Values = new List<List<double>>();

            this.dxf_nr_row_names = 0;
            this.dxf_nr_row_names_read = 0;

            this.dxf_RowNames = new List<string>();
        }

        #region METHODS

        protected void AddEntryToTableRowAt(int _index)
        {
            // add value to the row
            if (!this.dxf_table_row_values_read[_index])
            {
                this.dxf_table_row_values[_index] = this.Decoder.DoubleValue();
                this.dxf_table_row_values_read[_index] = true;
            }

            // check if the row has been completed
            int nr_values_read = this.dxf_table_row_values_read.Sum(x => x ? 1 : 0);
            if (nr_values_read == this.dxf_nr_values_per_row)
            {
                this.dxf_Values.Add(this.dxf_table_row_values.Take(this.dxf_nr_values_per_row).ToList());
                this.dxf_nr_table_rows_read++;

                this.dxf_table_row_values = Enumerable.Repeat(double.NaN, ExcelStandardImporter.MAX_NR_VALUE_COLUMNS).ToList();
                this.dxf_table_row_values_read = Enumerable.Repeat(false, ExcelStandardImporter.MAX_NR_VALUE_COLUMNS).ToList();
            }
        }

        #endregion

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch(this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.NUMBER_OF:
                    this.dxf_nr_values_per_row = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.XS:
                    // marks the start of the sequence of names (column headers)
                    this.dxf_nr_names = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.YS:
                    // marks the start of the sequence of units (also column headers)
                    this.dxf_nr_units = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.FIELD:
                    // marks the start of the sequence of values in the BIG TABLE
                    this.dxf_nr_table_rows = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.ROW_NAMES:
                    // marks the start of the sequence of row names in the BIG TABLE
                    this.dxf_nr_row_names = this.Decoder.IntValue();
                    if (this.dxf_nr_row_names == 0)
                        this.dxf_RowNames = null;
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_names > this.dxf_nr_names_read)
                    {
                        this.dxf_Names.Add(this.Decoder.FValue);
                        this.dxf_nr_names_read++;
                    }
                    if (this.dxf_nr_units > this.dxf_nr_units_read)
                    {
                        this.dxf_Units.Add(this.Decoder.FValue);
                        this.dxf_nr_units_read++;
                    }
                    else if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                    {
                        this.AddEntryToTableRowAt(0);
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(1);
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(2);
                    break;
                case (int)ParamStructCommonSaveCode.W_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(3);
                    break;
                case (int)ParamStructCommonSaveCode.V5_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(4);
                    break;
                case (int)ParamStructCommonSaveCode.V6_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(5);
                    break;
                case (int)ParamStructCommonSaveCode.V7_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(6);
                    break;
                case (int)ParamStructCommonSaveCode.V8_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(7);
                    break;
                case (int)ParamStructCommonSaveCode.V9_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(8);
                    break;
                case (int)ParamStructCommonSaveCode.V10_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(9);
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_row_names > this.dxf_nr_row_names_read)
                    {
                        this.dxf_RowNames.Add(this.Decoder.FValue);
                        this.dxf_nr_row_names_read++;
                    }
                    break;
                default:
                    // DXFField: ENTITY_NAME, MVID, MVType, MVName, MVCanInterpolate,
                    // MVDisplayVector_NUMDIM, MVDisplayVector_CELL_INDEX_X, MVDisplayVector_CELL_INDEX_Y, MVDisplayVector_CELL_INDEX_Z, MVDisplayVector_CELL_INDEX_W,
                    // MVDisplayVector_POS_IN_CELL_REL_X, MVDisplayVector_POS_IN_CELL_REL_Y, MVDisplayVector_POS_IN_CELL_REL_Z,
                    // MVDisplayVector_POS_IN_CELL_ABS_X, MVDisplayVector_POS_IN_CELL_ABS_Y, MVDisplayVector_POS_IN_CELL_ABS_Z,
                    // MVDisplayVector_VALUE,
                    // MVUnitX, MVUnitY, MVUnitZ
                    //
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.MV_Factory == null)
                return;

            // construct the pointer into the value field
            List<int> actual_indices = null;
            if (this.dxf_mvdv_num_dim <= 4)
                actual_indices = this.dxf_mvdv_cell_indices.Take(this.dxf_mvdv_num_dim).ToList();

            MultiValPointer pointer = new MultiValPointer(this.dxf_mvdv_num_dim, actual_indices, this.dxf_mvdv_cell_size,
                                                          this.dxf_mvdv_pos_rel, this.dxf_mvdv_pos_abs, this.dxf_mvdv_value);
            // check value lists for consistency
            bool data_consistent = true;
            data_consistent &= (this.dxf_nr_names == this.dxf_nr_names_read);
            data_consistent &= (this.dxf_nr_names == this.dxf_Names.Count);
            data_consistent &= (this.dxf_nr_units == this.dxf_nr_units_read);
            data_consistent &= (this.dxf_nr_units == this.dxf_Units.Count);
            data_consistent &= (this.dxf_nr_table_rows == this.dxf_nr_table_rows_read);
            data_consistent &= (this.dxf_nr_table_rows == this.dxf_Values.Count);
            data_consistent &= (this.dxf_nr_values_per_row == this.dxf_Values[0].Count);

            if (!data_consistent) return;

            // construct the table
            this.Decoder.MV_Factory.ReconstructBigTable(this.ENT_ID, this.dxf_MVName, pointer,
                                                        this.dxf_MVUnitX, this.dxf_MVUnitY, this.dxf_MVUnitZ,
                                                        this.dxf_Names, this.dxf_Units ,this.dxf_Values, this.dxf_RowNames);
        }

        #endregion
    }

    #endregion

    // wrapper of class Parameter
    #region DXF_Parameter

    public class DXFParameter : DXFEntity
    {
        #region CLASS MEMBERS

        // general
        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public string dxf_Unit { get; protected set; }
        public Category dxf_Category { get; protected set; }
        public InfoFlow dxf_Propagation { get; protected set; }

        // value management
        public double dxf_ValueMin { get; protected set; }
        public double dxf_ValueMax { get; protected set; }
        public double dxf_ValueCurrent { get; protected set; }
        public bool dxf_IsWithinBounds { get; protected set; }

        // value field management
        public long dxf_ValueFieldRef { get; protected set; }

        // display vector (for choosing values or interpolation of the referenced value field)
        protected int dxf_mvdv_num_dim;
        protected List<int> dxf_mvdv_cell_indices;
        protected Point dxf_mvdv_cell_size;
        protected Point dxf_mvdv_pos_rel;
        protected Point dxf_mvdv_pos_abs;
        protected double dxf_mvdv_value;
        public MultiValPointer dxf_MValPointer { get; private set; }

        // time stamp
        public DateTime dxf_TimeStamp { get; private set; }

        // string value
        public string dxf_TextValue { get; private set; }

        // for being included in components
        internal Parameter.Parameter dxf_parsed;

        #endregion

        public DXFParameter()
        {
            this.dxf_mvdv_num_dim = 0;
            this.dxf_mvdv_cell_indices = new List<int> { -1, -1, -1, -1 };
            this.dxf_mvdv_cell_size = new Point(0, 0);
            this.dxf_mvdv_pos_rel = new Point(0, 0);
            this.dxf_mvdv_pos_abs = new Point(0, 0);

            this.dxf_TextValue = string.Empty;
        }

        #region OVERRIDES : Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)ParameterSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)ParameterSaveCode.UNIT:
                    this.dxf_Unit = this.Decoder.FValue;
                    break;               
                case (int)ParameterSaveCode.CATEGORY:
                    string cat_as_str = this.Decoder.FValue;
                    this.dxf_Category = ComponentUtils.StringToCategory(cat_as_str);
                    break;
                case (int)ParameterSaveCode.PROPAGATION:                    
                    string prop_as_str = this.Decoder.FValue;
                    this.dxf_Propagation = ComponentUtils.StringToInfoFlow(prop_as_str);
                    break;
                case (int)ParameterSaveCode.VALUE_MIN:
                    this.dxf_ValueMin = this.Decoder.DoubleValue();
                    break;
                case (int)ParameterSaveCode.VALUE_MAX:
                    this.dxf_ValueMax = this.Decoder.DoubleValue();
                    break;
                case (int)ParameterSaveCode.VALUE_CURRENT:
                    this.dxf_ValueCurrent = this.Decoder.DoubleValue();
                    break;
                case (int)ParameterSaveCode.IS_WITHIN_BOUNDS:
                    this.dxf_IsWithinBounds = (this.Decoder.IntValue() == 1) ? true : false;
                    break;
                case (int)ParamStructCommonSaveCode.TIME_STAMP:
                    DateTime dt_tmp = DateTime.Now;
                    bool dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_TimeStamp = dt_tmp;
                    break;
                case (int)ParameterSaveCode.VALUE_TEXT:
                    this.dxf_TextValue = this.Decoder.FValue;
                    break;
                case (int)ParameterSaveCode.VALUE_FIELD_REF:
                    this.dxf_ValueFieldRef = this.Decoder.LongValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_NUMDIM:
                    this.dxf_mvdv_num_dim = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_X:
                    this.dxf_mvdv_cell_indices[0] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Y:
                    this.dxf_mvdv_cell_indices[1] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Z:
                    this.dxf_mvdv_cell_indices[2] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_W:
                    this.dxf_mvdv_cell_indices[3] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_W:
                    this.dxf_mvdv_cell_size.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_H:
                    this.dxf_mvdv_cell_size.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_X:
                    this.dxf_mvdv_pos_rel.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Y:
                    this.dxf_mvdv_pos_rel.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Z:
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_X:
                    this.dxf_mvdv_pos_abs.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Y:
                    this.dxf_mvdv_pos_abs.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Z:
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_VALUE:
                    this.dxf_mvdv_value = this.Decoder.DoubleValue();
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.MV_Factory == null || this.Decoder.P_Factory == null)
                return;

            // look for the associated value field
            MultiValue field = this.Decoder.MV_Factory.GetByID(this.dxf_ValueFieldRef);

            // construct the pointer into the value field
            List<int> actual_indices = null;
            if (this.dxf_mvdv_num_dim <= 4)
                actual_indices = this.dxf_mvdv_cell_indices.Take(this.dxf_mvdv_num_dim).ToList();

            this.dxf_MValPointer = new MultiValPointer(this.dxf_mvdv_num_dim, actual_indices, this.dxf_mvdv_cell_size,
                                                       this.dxf_mvdv_pos_rel, this.dxf_mvdv_pos_abs, this.dxf_mvdv_value);

            // create the parameter (and save it internally)
            this.dxf_parsed = 
            this.Decoder.P_Factory.ReconstructParameter(this.dxf_ID, this.dxf_Name, this.dxf_Unit, this.dxf_Category, this.dxf_Propagation,
                                                        this.dxf_ValueMin, this.dxf_ValueMax, this.dxf_ValueCurrent, this.dxf_IsWithinBounds,
                                                        field, this.dxf_ValueFieldRef, this.dxf_MValPointer, this.dxf_TimeStamp, this.dxf_TextValue);
        }

        #endregion
    }

    #endregion

    // wrapper of class Calculation
    #region DXF_Calculation

    public class DXFCalculation : DXFEntity
    {
        #region CLASS MEMBERS

        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public string dxf_Expression { get; protected set; }

        public Dictionary<string, long> dxf_InputParams_Ref { get; protected set; }
        private int dxf_nr_InputParams;
        private int dxf_nr_InputParams_read;
        private string dxf_IP_key;
        private long dxf_IP_id;
        private Point dxf_IP_read;
        public Dictionary<string, long> dxf_ReturnParams_Ref { get; protected set; }
        private int dxf_nr_ReturnParams;
        private int dxf_nr_ReturnParams_read;
        private string dxf_RP_key;
        private long dxf_RP_id;
        private Point dxf_RP_read;

        // wrap the underlying type
        internal CalculationPreview dxf_parsed;

        #endregion

        #region .CTOR

        public DXFCalculation()
            :base()
        {
            this.dxf_InputParams_Ref = new Dictionary<string, long>();
            this.dxf_nr_InputParams = 0;
            this.dxf_nr_InputParams_read = 0;
            this.dxf_IP_key = string.Empty;
            this.dxf_IP_id = -1;
            this.dxf_IP_read = new Point(0, 0);

            this.dxf_ReturnParams_Ref = new Dictionary<string, long>();
            this.dxf_nr_ReturnParams = 0;
            this.dxf_nr_ReturnParams_read = 0;
            this.dxf_RP_key = string.Empty;
            this.dxf_RP_id = -1;
            this.dxf_RP_read = new Point(0, 0);
        }

        #endregion

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)CalculationSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)CalculationSaveCode.EXPRESSION:
                    this.dxf_Expression = this.Decoder.FValue;
                    break;
                case (int)CalculationSaveCode.PARAMS_INPUT:
                    this.dxf_nr_InputParams = this.Decoder.IntValue();
                    break;
                case (int)CalculationSaveCode.PARAMS_OUTPUT:
                    this.dxf_nr_ReturnParams = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_InputParams > this.dxf_nr_InputParams_read && this.dxf_IP_read.X == 0)
                    {
                        this.dxf_IP_key = this.Decoder.FValue;
                        this.dxf_IP_read.X = 1;
                    }
                    else if (this.dxf_nr_ReturnParams > this.dxf_nr_ReturnParams_read && this.dxf_RP_read.X == 0)
                    {
                        this.dxf_RP_key = this.Decoder.FValue;
                        this.dxf_RP_read.X = 1;
                    }
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_REF:
                    if (this.dxf_nr_InputParams > this.dxf_nr_InputParams_read && this.dxf_IP_read.Y == 0)
                    {
                        this.dxf_IP_id = this.Decoder.LongValue();
                        this.dxf_IP_read.Y = 1;
                    }
                    if (this.dxf_IP_read.X == 1 && this.dxf_IP_read.Y == 1)
                    {
                        if (!(this.dxf_InputParams_Ref.ContainsKey(this.dxf_IP_key)))
                            this.dxf_InputParams_Ref.Add(this.dxf_IP_key, this.dxf_IP_id);
                        this.dxf_nr_InputParams_read++;
                        this.dxf_IP_read = new Point(0, 0);
                    }
                    if (this.dxf_nr_ReturnParams > this.dxf_nr_ReturnParams_read && this.dxf_RP_read.Y == 0)
                    {
                        this.dxf_RP_id = this.Decoder.LongValue();
                        this.dxf_RP_read.Y = 1;
                    }
                    if (this.dxf_RP_read.X == 1 && this.dxf_RP_read.Y == 1)
                    {
                        if (!(this.dxf_ReturnParams_Ref.ContainsKey(this.dxf_IP_key)))
                            this.dxf_ReturnParams_Ref.Add(this.dxf_RP_key, this.dxf_RP_id);
                        this.dxf_nr_ReturnParams_read++;
                        this.dxf_RP_read = new Point(0, 0);
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            this.dxf_parsed = new CalculationPreview(this.dxf_Name, this.dxf_Expression, this.dxf_InputParams_Ref, 
                                                                                         this.dxf_ReturnParams_Ref);
        }

        #endregion
    }

    #endregion

    // wrapper of class ComponentAccessTracker
    #region DXF_ComponentAccessTracker

    public class DXFAccessTracker : DXFEntity
    {
        #region CLASS MEMBERS

        public ComponentAccessType dxf_AccessTypeFlags;

        public DateTime dxf_prev_access_write;
        public DateTime dxf_last_access_write;

        public DateTime dxf_prev_access_supervize;
        public DateTime dxf_last_access_supervize;

        public DateTime dxf_prev_access_release;
        public DateTime dxf_last_access_release;

        public ComponentAccessTracker dxf_parsed;

        #endregion

        public DXFAccessTracker()
            :base()
        {
            this.dxf_AccessTypeFlags = ComponentAccessType.NO_ACCESS;

            this.dxf_prev_access_write = DateTime.MinValue;
            this.dxf_last_access_write = DateTime.MinValue;

            this.dxf_prev_access_supervize = DateTime.MinValue;
            this.dxf_last_access_supervize = DateTime.MinValue;

            this.dxf_prev_access_release = DateTime.MinValue;
            this.dxf_last_access_release = DateTime.MinValue;
        }

        #region OVERRIDES : Read Poperty

        public override void ReadPoperty()
        {
            DateTime dt_tmp = DateTime.Now;
            bool dt_p_success = false;

            switch (this.Decoder.FCode)
            {
                case (int)ComponentAccessTrackerSaveCode.FLAGS:
                    this.dxf_AccessTypeFlags = ComponentUtils.StringToComponentAccessType(this.Decoder.FValue);
                    break;
                case (int)ComponentAccessTrackerSaveCode.WRITE_PREV:
                    dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_prev_access_write = dt_tmp;
                    break;
                case (int)ComponentAccessTrackerSaveCode.WRITE_LAST:
                    dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_last_access_write = dt_tmp;
                    break;
                case (int)ComponentAccessTrackerSaveCode.SUPERVIZE_PREV:
                    dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_prev_access_supervize = dt_tmp;
                    break;
                case (int)ComponentAccessTrackerSaveCode.SUPERVIZE_LAST:
                    dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_last_access_supervize= dt_tmp;
                    break;
                case (int)ComponentAccessTrackerSaveCode.RELEASE_PREV:
                    dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_prev_access_release = dt_tmp;
                    break;
                case (int)ComponentAccessTrackerSaveCode.RELEASE_LAST:
                    dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_last_access_release = dt_tmp;
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.COMP_Factory == null) return;

            // create the new access tracker
            this.dxf_parsed = new ComponentAccessTracker(this.dxf_AccessTypeFlags,
                                                        this.dxf_prev_access_write, this.dxf_last_access_write,
                                                        this.dxf_prev_access_supervize, this.dxf_last_access_supervize,
                                                        this.dxf_prev_access_release, this.dxf_last_access_release);
        }

        #endregion
    }

    #endregion

    // wrapper of class FlNetElement
    #region DXF_FlNetElement

    public class DXF_FlNetElement : DXFEntity
    {
        #region CLASS MEMBERS

        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public string dxf_Description { get; protected set; }
        public long dxf_Content_ID { get; protected set; }
        public bool dxf_IsValid { get; protected set; }

        #endregion

        public DXF_FlNetElement()
        {
            this.dxf_ID = -1;
            this.dxf_Name = string.Empty;
            this.dxf_Description = string.Empty;
            this.dxf_Content_ID = -1;
            this.dxf_IsValid = false;
        }

        #region OVERRIDES : Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)FlowNetworkSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)FlowNetworkSaveCode.DESCRIPTION:
                    this.dxf_Description = this.Decoder.FValue;
                    break;
                case (int)FlowNetworkSaveCode.CONTENT_ID:
                    this.dxf_Content_ID = this.Decoder.LongValue();
                    break;
                case (int)FlowNetworkSaveCode.IS_VALID:
                    this.dxf_IsValid = (this.Decoder.IntValue() == 1) ? true : false;
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

    }

    #endregion

    // wrapper of class FlNetNode
    #region DXF_FlNetNode

    public class DXF_FlNetNode : DXF_FlNetElement
    {
        #region CLASS MEMBERS

        protected Point dxf_Position;
        protected List<FlowNetworkCalcRule> dxf_CalculationRules;
        protected int dxf_nr_CalculationRules;
        protected int dxf_nr_CalculationRules_read;
        protected FlowNetworkCalcRule dxf_current_rule;
        // create the node (and save it internally)
        internal FlNetNode dxf_parsed;

        #endregion

        public DXF_FlNetNode()
        {
            this.dxf_Position = new Point(0, 0);

            this.dxf_CalculationRules = new List<FlowNetworkCalcRule>();
            this.dxf_nr_CalculationRules = 0;
            this.dxf_nr_CalculationRules_read = 0;
            this.dxf_current_rule = new FlowNetworkCalcRule();
        }

        #region OVERRIDES: Read Property
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)FlowNetworkSaveCode.POSITION_X:
                    this.dxf_Position.X = this.Decoder.DoubleValue();
                    break;
                case (int)FlowNetworkSaveCode.POSITION_Y:
                    this.dxf_Position.Y = this.Decoder.DoubleValue();
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULES:
                    this.dxf_nr_CalculationRules = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Suffix_Operands = this.Decoder.FValue;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Suffix_Result = this.Decoder.FValue;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_DIRECTION:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Direction = (this.Decoder.FValue == "1") ? FlowNetworkCalcDirection.FORWARD : FlowNetworkCalcDirection.BACKWARD;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_OPERATOR:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Operator = FlowNetworkCalcRule.StringToOperator(this.Decoder.FValue);
                        this.dxf_CalculationRules.Add(this.dxf_current_rule);
                        this.dxf_current_rule = new FlowNetworkCalcRule();
                        this.dxf_nr_CalculationRules_read++;
                    }
                    break;
                default:
                    // DXF_FlNetNode: ENTITY_ID, NAME, DESCRIPTION, CONTENT_ID, IS_VALID
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.COMP_Factory == null) return;
            if (this.defer_OnLoading) return;

            // look for the associated component
            Component.Component content = null;
            if (this.dxf_Content_ID > -1)
                content = this.Decoder.COMP_Factory.GetByID(this.dxf_Content_ID);

            // if content could not be found, defer
            if (this.dxf_Content_ID > -1 && content == null)
            {
                this.defer_OnLoading = true;
                this.Decoder.AddForDeferredOnLoad(this);
                return;
            }
            // create and save the node ...
            this.dxf_parsed = new FlNetNode(this.dxf_ID, this.dxf_Name, this.dxf_Description, content, this.dxf_IsValid, this.dxf_Position, this.dxf_CalculationRules);    
        
        }
        #endregion
        
    }

    #endregion

    // wrapper of classes FlNetEdge
    #region DXF_FlNetEdge_Preparsed

    internal class DXF_FlNetEdge_Preparsed
    {
        internal long dxf_ID { get; private set; }
        internal string dxf_Name { get; private set; }
        internal string dxf_Description { get; private set; }
        internal Component.Component dxf_Content { get; private set; }
        internal bool dxf_IsValid { get; private set; }
        internal long dxf_StartNode_ID { get; private set; }
        internal long dxf_EndNode_ID { get; private set; }

        internal DXF_FlNetEdge_Preparsed(long _id, string _name, string _description, Component.Component _content,
                                         bool _is_valid, long _start_node_id, long _end_node_id)
        {
            this.dxf_ID = _id;
            this.dxf_Name = _name;
            this.dxf_Description = _description;
            this.dxf_Content = _content;
            this.dxf_IsValid = _is_valid;
            this.dxf_StartNode_ID = _start_node_id;
            this.dxf_EndNode_ID = _end_node_id;
        }
    }

    #endregion

    #region DXF_FlNetEdge

    public class DXF_FlNetEdge : DXF_FlNetElement
    {
        #region CLASS MEMBERS

        private long dxf_start_node_id;
        private long dxf_end_node_id;

        // create the edge (and save it internally)
        internal DXF_FlNetEdge_Preparsed dxf_preparsed;

        #endregion

        public DXF_FlNetEdge()
        {
            this.dxf_start_node_id = -1;
            this.dxf_end_node_id = -1;
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)FlowNetworkSaveCode.START_NODE:
                    this.dxf_start_node_id = this.Decoder.LongValue();
                    break;
                case (int)FlowNetworkSaveCode.END_NODE:
                    this.dxf_end_node_id = this.Decoder.LongValue();
                    break;
                default:
                    // DXF_FlNetNode: ENTITY_ID, NAME, DESCRIPTION, CONTENT_ID, IS_VALID
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.COMP_Factory == null) return;
            if (this.defer_OnLoading) return;

            // look for the associated component
            Component.Component content = null;
            if (this.dxf_Content_ID > -1)
                content = this.Decoder.COMP_Factory.GetByID(this.dxf_Content_ID);

            // if content could not be found, defer
            if (this.dxf_Content_ID > -1 && content == null)
            {
                this.defer_OnLoading = true;
                this.Decoder.AddForDeferredOnLoad(this);
                return;
            }

            // save the pre-parsed state
            // in order to find the start and end nodes, they need to have been processed already ->
            // full parsing in the FlowNetwork this edge belongs to
            this.dxf_preparsed = new DXF_FlNetEdge_Preparsed(this.dxf_ID, this.dxf_Name, this.dxf_Description, content, 
                                                             this.dxf_IsValid, this.dxf_start_node_id, this.dxf_end_node_id);
        }

        #endregion
    }

    #endregion

    // wrapper class for Geometric Relationships
    #region DXF_GeometricRelationship
    public class DXFGeometricRelationship : DXFEntity
    {
        #region CLASS MEMBERS

        // name, type
        public long dxf_ID { get; private set; }
        public string dxf_Name { get; private set; }
        public Relation2GeomType dxf_Type { get; private set; }
        public bool dxf_rel2geom_IsRealized { get; private set; }

        // rerferenced geometry
        private Point4D dxf_GeomIDs;
        private Matrix3D dxf_GeomCS;

        // transforms
        private Matrix3D dxf_TRm_WC2LC;
        private Matrix3D dxf_TRm_LC2WC;

        // instance information
        private List<double> dxf_inst_size;
        private int dxf_nr_inst_size;
        private List<GeomSizeTransferDef> dxf_inst_size_transfer_settings;
        private GeomSizeTransferDef dxf_inst_sts_entry_current;
        private int dxf_nr_inst_size_transfer_settings;
        private long dxf_inst_nwe_id;
        private string dxf_inst_nwe_name;
        private List<Point3D> dxf_inst_path;
        private int dxf_nr_inst_path;
        private Point3D dxf_inst_path_current_vertex;

        // for being included in components
        internal GeometricRelationship dxf_parsed;

        #endregion

        public DXFGeometricRelationship()
        {
            this.dxf_ID = -1;
            this.dxf_Name = string.Empty;
            this.dxf_Type = Relation2GeomType.NONE;
            this.dxf_rel2geom_IsRealized = false;

            this.dxf_GeomIDs = new Point4D(-1, -1, -1, -1);
            this.dxf_GeomCS = Matrix3D.Identity;
            this.dxf_TRm_WC2LC = Matrix3D.Identity;
            this.dxf_TRm_LC2WC = Matrix3D.Identity;

            this.dxf_inst_size = new List<double>();
            this.dxf_nr_inst_size = 0;
            this.dxf_inst_size_transfer_settings = new List<GeomSizeTransferDef>();
            this.dxf_inst_sts_entry_current = new GeomSizeTransferDef { Source = GeomSizeTransferSource.USER, InitialValue = 0.0, ParameterName = string.Empty, Correction = 0.0 };
            this.dxf_nr_inst_size_transfer_settings = 0;

            this.dxf_inst_nwe_id = -1L;
            this.dxf_inst_nwe_name = "NW_Element";
            this.dxf_inst_path = new List<Point3D>();
            this.dxf_nr_inst_path = 0;
            this.dxf_inst_path_current_vertex = new Point3D(0, 0, 0);
        }

        #region OVERRIDES : Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)GeometricRelationshipSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)GeometricRelationshipSaveCode.STATE_TYPE:
                    string type_as_str = this.Decoder.FValue;
                    this.dxf_Type = GeometryUtils.StringToRelationship2Geometry(type_as_str);
                    break;
                case (int)GeometricRelationshipSaveCode.STATE_ISREALIZED:
                    this.dxf_rel2geom_IsRealized = (this.Decoder.IntValue() == 1);
                    break;
                case (int)GeometricRelationshipSaveCode.GEOM_IDS_X:
                    this.dxf_GeomIDs.X = this.Decoder.DoubleValue();
                    break;
                case (int)GeometricRelationshipSaveCode.GEOM_IDS_Y:
                    this.dxf_GeomIDs.Y = this.Decoder.DoubleValue();
                    break;
                case (int)GeometricRelationshipSaveCode.GEOM_IDS_Z:
                    this.dxf_GeomIDs.Z = this.Decoder.DoubleValue();
                    break;
                case (int)GeometricRelationshipSaveCode.GEOM_IDS_W:
                    this.dxf_GeomIDs.W = this.Decoder.DoubleValue();
                    break;
                case (int)GeometricRelationshipSaveCode.GEOM_CS:
                    this.dxf_GeomCS = Matrix3D.Parse(this.Decoder.FValue);
                    break;
                case (int)GeometricRelationshipSaveCode.TRANSF_WC2LC:
                    this.dxf_TRm_WC2LC = Matrix3D.Parse(this.Decoder.FValue);
                    break;
                case (int)GeometricRelationshipSaveCode.TRANSF_LC2WC:
                    this.dxf_TRm_LC2WC = Matrix3D.Parse(this.Decoder.FValue);
                    break;
                // instance insformation
                case (int)GeometricRelationshipSaveCode.INST_SIZE:
                    this.dxf_nr_inst_size = this.Decoder.IntValue();
                    break;
                case (int)GeometricRelationshipSaveCode.INST_SIZE_TRANSSETTINGS:
                    this.dxf_nr_inst_size_transfer_settings = this.Decoder.IntValue();
                    break;
                case (int)GeometricRelationshipSaveCode.INST_NWE_ID:
                    this.dxf_inst_nwe_id = this.Decoder.LongValue();
                    break;
                case (int)GeometricRelationshipSaveCode.INST_NWE_NAME:
                    this.dxf_inst_nwe_name = this.Decoder.FValue;
                    break;
                case (int)GeometricRelationshipSaveCode.INST_PATH:
                    this.dxf_nr_inst_path = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_inst_size > this.dxf_inst_size.Count)
                    {
                        this.dxf_inst_size.Add(this.Decoder.DoubleValue());
                    }
                    else if (this.dxf_nr_inst_path > this.dxf_inst_path.Count)
                    {
                        this.dxf_inst_path_current_vertex.X = this.Decoder.DoubleValue();
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_inst_path > this.dxf_inst_path.Count)
                    {
                        this.dxf_inst_path_current_vertex.Y = this.Decoder.DoubleValue();
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    if (this.dxf_nr_inst_path > this.dxf_inst_path.Count)
                    {
                        this.dxf_inst_path_current_vertex.Z = this.Decoder.DoubleValue();
                        this.dxf_inst_path.Add(this.dxf_inst_path_current_vertex);
                        this.dxf_inst_path_current_vertex = new Point3D(0, 0, 0);
                    }
                    break;
                case (int)GeometricRelationshipSaveCode.INST_SIZE_TS_SOURCE:
                    if (this.dxf_nr_inst_size_transfer_settings > this.dxf_inst_size_transfer_settings.Count)
                    {
                        this.dxf_inst_sts_entry_current.Source = GeomSizeTransferDef.StringToGeomSizeTransferSource(this.Decoder.FValue);
                    }
                    break;
                case (int)GeometricRelationshipSaveCode.INST_SIZE_TS_INITVAL:
                    if (this.dxf_nr_inst_size_transfer_settings > this.dxf_inst_size_transfer_settings.Count)
                    {
                        this.dxf_inst_sts_entry_current.InitialValue = this.Decoder.DoubleValue();
                    }
                    break;
                case (int)GeometricRelationshipSaveCode.INST_SIZE_TS_PARNAME:
                    if (this.dxf_nr_inst_size_transfer_settings > this.dxf_inst_size_transfer_settings.Count)
                    {
                        this.dxf_inst_sts_entry_current.ParameterName = this.Decoder.FValue;
                    }
                    break;
                case (int)GeometricRelationshipSaveCode.INST_SIZE_TS_CORRECT:
                    if (this.dxf_nr_inst_size_transfer_settings > this.dxf_inst_size_transfer_settings.Count)
                    {
                        this.dxf_inst_sts_entry_current.Correction = this.Decoder.DoubleValue();
                        this.dxf_inst_size_transfer_settings.Add(this.dxf_inst_sts_entry_current);
                        this.dxf_inst_sts_entry_current = new GeomSizeTransferDef { Source = GeomSizeTransferSource.USER, InitialValue = 0.0, ParameterName = string.Empty, Correction = 0.0 };
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            // create the geometric relationship and save it internally
            Relation2GeomState state = new Relation2GeomState { IsRealized = this.dxf_rel2geom_IsRealized, Type = this.dxf_Type };

            //// OLD
            //this.dxf_parsed = new GeometricRelationship(dxf_ID, dxf_Name, state, dxf_GeomIDs, dxf_GeomCS, dxf_TRm_WC2LC, dxf_TRm_LC2WC);

            // NEW
            this.dxf_parsed = new GeometricRelationship(dxf_ID, dxf_Name, state, dxf_GeomIDs, dxf_GeomCS, dxf_TRm_WC2LC, dxf_TRm_LC2WC,
                                                        dxf_inst_size, dxf_inst_size_transfer_settings, dxf_inst_nwe_id, dxf_inst_nwe_name, dxf_inst_path);
        }

        #endregion
    }
    #endregion

    // wrapper class for Geometric Relationship
    #region DXF_Mapping2Component

    public class DXFMapping2Component : DXFEntity
    {
        #region CLASS MEMBERS

        public string dxf_Name { get; private set; }
        public long dxf_Calculator_ID { get; private set; }
        public Dictionary<long, long> dxf_InputMapping { get; private set; }
        private int dxf_nr_InputMapping;
        private long dxf_input_entry_Key;
        private long dxf_input_entry_Value;
        public Dictionary<long, long> dxf_OutputMapping { get; private set; }
        private int dxf_nr_OutputMapping;
        private long dxf_output_entry_Key;
        private long dxf_output_entry_Value;

        // for being included in components
        internal Mapping2Component dxf_parsed;

        #endregion

        public DXFMapping2Component()
        {
            this.dxf_Name = string.Empty;
            this.dxf_Calculator_ID = -1L;
            
            this.dxf_InputMapping = new Dictionary<long, long>();
            this.dxf_nr_InputMapping = 0;
            this.dxf_input_entry_Key = -1;
            this.dxf_input_entry_Value = -1;

            this.dxf_OutputMapping = new Dictionary<long, long>();
            this.dxf_nr_OutputMapping = 0;
            this.dxf_output_entry_Key = -1;
            this.dxf_output_entry_Value = -1;

        }

        #region OVERRIDES: Read Property
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)Mapping2ComponentSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)Mapping2ComponentSaveCode.CALCULATOR:
                    this.dxf_Calculator_ID = this.Decoder.LongValue();
                    break;
                case (int)Mapping2ComponentSaveCode.INPUT_MAPPING:
                    this.dxf_nr_InputMapping = this.Decoder.IntValue();
                    break;
                case (int)Mapping2ComponentSaveCode.INPUT_MAPPING_KEY:
                    if (this.dxf_nr_InputMapping > this.dxf_InputMapping.Count)
                    {
                        this.dxf_input_entry_Key = this.Decoder.LongValue();
                    }
                    break;
                case (int)Mapping2ComponentSaveCode.INPUT_MAPPING_VALUE:
                    if (this.dxf_nr_InputMapping > this.dxf_InputMapping.Count)
                    {
                        this.dxf_input_entry_Value = this.Decoder.LongValue();
                        this.dxf_InputMapping.Add(this.dxf_input_entry_Key, this.dxf_input_entry_Value);
                        this.dxf_input_entry_Key = -1L;
                        this.dxf_input_entry_Value = -1L;
                    }
                    break;
                case (int)Mapping2ComponentSaveCode.OUTPUT_MAPPING:
                    this.dxf_nr_OutputMapping = this.Decoder.IntValue();
                    break;
                case (int)Mapping2ComponentSaveCode.OUTPUT_MAPPING_KEY:
                    if (this.dxf_nr_OutputMapping > this.dxf_OutputMapping.Count)
                    {
                        this.dxf_output_entry_Key = this.Decoder.LongValue();
                    }
                    break;
                case (int)Mapping2ComponentSaveCode.OUTPUT_MAPPING_VALUE:
                    if (this.dxf_nr_OutputMapping > this.dxf_OutputMapping.Count)
                    {
                        this.dxf_output_entry_Value = this.Decoder.LongValue();
                        this.dxf_OutputMapping.Add(this.dxf_output_entry_Key, this.dxf_output_entry_Value);
                        this.dxf_output_entry_Key = -1L;
                        this.dxf_output_entry_Value = -1L;
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            // create the mapping and save it internally
            this.dxf_parsed = new Mapping2Component(this.dxf_Name, this.dxf_Calculator_ID, this.dxf_InputMapping, this.dxf_OutputMapping);
        }

        #endregion
    }


    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ========================================== COLLECTIONS OF ENTITIES ===================================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region DXF_Entity_Container

    public class DXFEntityContainer : DXFEntity
    {
        #region CLASS MEMBERS

        internal List<DXFEntity> EC_Entities;

        internal List<long> dxf_ids_of_children_for_deferred_adding;

        #endregion

        #region .CTOR

        public DXFEntityContainer()
            :base()
        {
            this.ENT_HasEntites = true;
            this.EC_Entities = new List<DXFEntity>();

            this.dxf_ids_of_children_for_deferred_adding = new List<long>();
        }

        #endregion

        #region OVERRIDES

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        public override bool AddEntity(DXFEntity _e)
        {
            if (_e != null)
                this.EC_Entities.Add(_e);
            return (_e != null);
        }

        public override string ToString()
        {
            string dxfS = "DXF EXTITY CONTAINER:";
            if (this.ENT_Name != null && this.ENT_Name.Count() > 0)
                dxfS += ": " + this.ENT_Name;
            int n = this.EC_Entities.Count;
            dxfS += " has " + n.ToString() + " entities:\n";
            for (int i = 0; i < n; i++)
            {
                dxfS += "_[ " + i + "]_" + this.EC_Entities[i].ToString() + "\n";
            }
            dxfS += "\n";
            return dxfS;
        }

        #endregion
    }

    #endregion

    // ------------------------------------------------ DXFSection -------------------------------------------- //

    #region DXF_Section

    public class DXFSection : DXFEntityContainer
    {
        public override void ReadPoperty()
        {
            if ((this.ENT_Name == null) && (this.Decoder.FCode == (int)ParamStructCommonSaveCode.ENTITY_NAME))
            {
                this.ENT_Name = this.Decoder.FValue;
            }
            switch (this.ENT_Name)
            {
                case ParamStructTypes.ENTITY_SECTION:
                    this.Decoder.FEntities = this;
                    break;
            }
        }
    }

    #endregion

    // --------------------------------------------- DXFAccessProfile ----------------------------------------- //

    #region DXF_Access_Profile

    public class DXFAccessProfile : DXFEntityContainer
    {
        #region CLASS MEMBERS

        public ComponentValidity dxf_profile_state;
        private int nr_entries;
        public ComponentAccessProfile dxf_parsed;

        #endregion

        #region OVERRIDES

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ComponentAccessProfileSaveCode.STATE:
                    this.dxf_profile_state = ComponentUtils.StringToComponentValidity(this.Decoder.FValue);
                    break;
                case (int)ComponentAccessProfileSaveCode.PROFILE:
                    this.nr_entries = this.Decoder.IntValue();
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        public override bool AddEntity(DXFEntity _e)
        {
            if (_e == null) return false;

            DXFAccessTracker at = _e as DXFAccessTracker;
            if (at == null) return false;

            if (this.nr_entries <= this.EC_Entities.Count) return false;

            this.EC_Entities.Add(_e);
            return true;
        }

        public override void OnLoaded()
        {
            base.OnLoaded();

            Dictionary<ComponentManagerType, ComponentAccessTracker> profile = new Dictionary<ComponentManagerType, ComponentAccessTracker>();
            foreach(DXFEntity e in this.EC_Entities)
            {
                DXFAccessTracker at = e as DXFAccessTracker;
                if (at == null) continue;
                if (at.dxf_parsed == null) continue;

                profile.Add(ComponentUtils.StringToComponentManagerType( at.ENT_KEY), at.dxf_parsed);
            }

            if (this.nr_entries != profile.Count) return;
            this.dxf_parsed = new ComponentAccessProfile(profile, ComponentManagerType.ADMINISTRATOR);
        }

        #endregion
    }

    #endregion

    // ----------------------------------------------- DXFComponent ------------------------------------------- //

    #region DXF_Component_List_Container

    public class DXFComponentSubContainer : DXFEntityContainer
    {

    }

    #endregion

    #region DXF_Component

    public class DXFComponent : DXFEntityContainer
    {
        #region CLASS MEMBERS

        // general
        public long dxf_ID;
        public string dxf_Name;
        public string dxf_Description;
        public bool dxf_IsAutomaticallyGenerated;

        // management
        public Category dxf_Category;
        public ComponentAccessProfile dxf_AccessLocal;
        public List<string> dxf_FitsInSlots;
        private int dxf_nr_FitsInSlots;
        private int dxf_nr_FitsInSlots_read;
        public string dxf_CurrentSlot;

        // contained components
        public Dictionary<string, Component.Component> dxf_ContainedComponents;
        private int dxf_nr_ContainedComponents;
        private int dxf_nr_ContainedComponent_Slots;
        private int dxf_nr_ContainedComponent_Slots_read;

        // referenced components
        public Dictionary<string, long> dxf_ReferencedComponents;
        private int dxf_nr_ReferencedComponents;
        private int dxf_nr_ReferencedComponents_read;
        private Point dxf_RefComp_read;
        private string dxf_RefComp_slot;
        private long dxf_RefComp_id;

        // contained parameters
        public List<Parameter.Parameter> dxf_ContainedParameters;
        private int dxf_nr_ContainedParameters;

        // contained calculations
        internal List<CalculationPreview> dxf_ContainedCalculations_Ref;
        private int dxf_nr_ContainedCalculations;

        // relationships to geometry
        private List<GeometricRelationship> dxf_R2GInstances;
        private int dxf_nr_R2GInstances;

        // mappings to other components (for shared usage of calculations)
        private List<Mapping2Component> dxf_Mapping2Comps;
        private int dxf_nr_Mapping2Comps;

        // timestamp
        public DateTime dxf_TimeStamp;

        // symbol
        public long dxf_SymbolId;

        // parsed encapsulated class
        internal Component.Component dxf_parsed;

        #endregion

        #region .CTOR
        public DXFComponent()
            :base()
        {
            this.dxf_IsAutomaticallyGenerated = false;

            this.dxf_FitsInSlots = new List<string>();
            this.dxf_nr_FitsInSlots = 0;
            this.dxf_nr_FitsInSlots_read = 0;

            this.dxf_ContainedComponents = new Dictionary<string,Component.Component>();
            this.dxf_nr_ContainedComponents = 0;
            this.dxf_nr_ContainedComponent_Slots = 0;
            this.dxf_nr_ContainedComponent_Slots_read = 0;

            this.dxf_ReferencedComponents = new Dictionary<string, long>();
            this.dxf_nr_ReferencedComponents = 0;
            this.dxf_nr_ReferencedComponents_read = 0;
            this.dxf_RefComp_read = new Point(0, 0);

            this.dxf_ContainedParameters = new List<Parameter.Parameter>();
            this.dxf_nr_ContainedParameters = 0;

            this.dxf_ContainedCalculations_Ref = new List<CalculationPreview>();
            this.dxf_nr_ContainedCalculations = 0;

            this.dxf_R2GInstances = new List<GeometricRelationship>();
            this.dxf_nr_R2GInstances = 0;

            this.dxf_Mapping2Comps = new List<Mapping2Component>();
            this.dxf_nr_Mapping2Comps = 0;

            this.dxf_TimeStamp = DateTime.MinValue;
            this.dxf_SymbolId = -1;
        }
        #endregion

        #region OVERRIDES : Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ComponentSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)ComponentSaveCode.DESCRIPTION:
                    this.dxf_Description = this.Decoder.FValue;
                    break;
                case (int)ComponentSaveCode.GENERATED_AUTOMATICALLY:
                    this.dxf_IsAutomaticallyGenerated = (this.Decoder.IntValue() == 1);
                    break;
                case (int)ComponentSaveCode.CATEGORY:
                    this.dxf_Category = ComponentUtils.StringToCategory(this.Decoder.FValue);
                    break;
                case (int)ComponentSaveCode.FUNCTION_SLOTS_ALL:
                    this.dxf_nr_FitsInSlots = this.Decoder.IntValue();
                    break;               
                case (int)ComponentSaveCode.FUNCTION_SLOT_CURRENT:
                    this.dxf_CurrentSlot = this.Decoder.FValue;
                    break;
                case (int)ComponentSaveCode.CONTAINED_COMPONENTS:
                    this.dxf_nr_ContainedComponents = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.CONTAINED_COMPONENT_SLOTS:
                    this.dxf_nr_ContainedComponent_Slots = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.REFERENCED_COMPONENTS:
                    this.dxf_nr_ReferencedComponents = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.CONTAINED_PARAMETERS:
                    this.dxf_nr_ContainedParameters = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.CONTAINED_CALCULATIONS:
                    this.dxf_nr_ContainedCalculations = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.RELATIONSHIPS_TO_GEOMETRY:
                    this.dxf_nr_R2GInstances = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.MAPPINGS_TO_COMPONENTS:
                    this.dxf_nr_Mapping2Comps = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.TIME_STAMP:
                    DateTime dt_tmp = DateTime.Now;
                    bool dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_TimeStamp = dt_tmp;
                    break;
                case (int)ComponentSaveCode.SYMBOL_ID:
                    this.dxf_SymbolId = this.Decoder.LongValue();
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_FitsInSlots > this.dxf_nr_FitsInSlots_read)
                    {
                        this.dxf_FitsInSlots.Add(this.Decoder.FValue);
                        this.dxf_nr_FitsInSlots_read++;
                    }
                    else if (this.dxf_nr_ContainedComponent_Slots > this.dxf_nr_ContainedComponent_Slots_read)
                    {
                        if (!(this.dxf_ContainedComponents.ContainsKey(this.Decoder.FValue)))
                        {
                            this.dxf_ContainedComponents.Add(this.Decoder.FValue, null);
                            this.dxf_nr_ContainedComponent_Slots_read++;
                        }
                    }
                    else if (this.dxf_nr_ReferencedComponents > this.dxf_nr_ReferencedComponents_read)
                    {
                        if (this.dxf_RefComp_read.X == 0)
                        {
                            this.dxf_RefComp_slot = this.Decoder.FValue;
                            this.dxf_RefComp_read.X = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_ReferencedComponents > this.dxf_nr_ReferencedComponents_read)
                    {
                        if (this.dxf_RefComp_read.Y == 0)
                        {
                            this.dxf_RefComp_id = this.Decoder.LongValue();
                            this.dxf_RefComp_read.Y = 1;   
                        }   
                        if (this.dxf_RefComp_read.X == 1 && this.dxf_RefComp_read.Y == 1)
                        {
                            this.dxf_ReferencedComponents.Add(this.dxf_RefComp_slot, this.dxf_RefComp_id);
                            this.dxf_nr_ReferencedComponents_read++;
                            this.dxf_RefComp_read = new Point(0, 0);
                        }
                    }
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }
        #endregion

        #region OVERRIDES: Adding Entities

        public override bool AddEntity(DXFEntity _e)
        {
            // handle depending on type
            if (_e == null) return false;
            bool add_successful = false;

            DXFAccessProfile ap = _e as DXFAccessProfile;
            if (ap != null)
            {
                this.dxf_AccessLocal = ap.dxf_parsed;
                return true;
            }

            DXFComponentSubContainer container = _e as DXFComponentSubContainer;
            if (container != null)
            {
                add_successful = true;
                foreach(DXFEntity sE in container.EC_Entities)
                {
                    DXFParameter param = sE as DXFParameter;
                    if (param != null && this.dxf_nr_ContainedParameters > this.dxf_ContainedParameters.Count)
                    {
                        // take the parsed parameter
                        this.dxf_ContainedParameters.Add(param.dxf_parsed);
                        // delete it from the parameter factory
                        this.Decoder.P_Factory.DeleteRecord(param.dxf_parsed.ID);
                        add_successful &= true;
                    }

                    DXFComponent sComp = sE as DXFComponent;
                    if (sComp != null)
                    {
                        // take the parsed component
                        string key = sComp.ENT_KEY;
                        if (string.IsNullOrEmpty(key) || this.dxf_ContainedComponents.ContainsKey(key))
                        {
                            add_successful &= false;
                            continue;
                        }

                        // take the parsed component
                        this.dxf_ContainedComponents.Add(key, sComp.dxf_parsed);
                        // delete it from the component factory
                        this.Decoder.COMP_Factory.RemoveComponent(sComp.dxf_parsed, true, false);
                        add_successful &= true;
                    }

                    DXFCalculation sCalc = sE as DXFCalculation;
                    if (sCalc != null && sCalc.dxf_parsed != null)
                    {
                        if (this.dxf_nr_ContainedCalculations > this.dxf_ContainedCalculations_Ref.Count)
                            this.dxf_ContainedCalculations_Ref.Add(sCalc.dxf_parsed);
                    }

                    DXFGeometricRelationship gr = sE as DXFGeometricRelationship;
                    if (gr != null && gr.dxf_parsed != null &&
                        this.dxf_nr_R2GInstances > this.dxf_R2GInstances.Count)
                    {
                        this.dxf_R2GInstances.Add(gr.dxf_parsed);
                    }

                    DXFMapping2Component map = sE as DXFMapping2Component;
                    if (map != null && map.dxf_parsed != null &&
                        this.dxf_nr_Mapping2Comps > this.dxf_Mapping2Comps.Count)
                    {
                        this.dxf_Mapping2Comps.Add(map.dxf_parsed);
                    }
                }
            }

            return add_successful;
        }

        #endregion
        
        #region OVERRIDES: Post-Processing
        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.MV_Factory == null || this.Decoder.P_Factory == null || this.Decoder.COMP_Factory == null) return;
            
            // gather information and save component
                this.dxf_parsed =
                this.Decoder.COMP_Factory.ReconstructComponent(this.dxf_ID, this.dxf_Name, this.dxf_Description, this.dxf_IsAutomaticallyGenerated,
                                                this.dxf_Category, this.dxf_AccessLocal, this.dxf_FitsInSlots, this.dxf_CurrentSlot,
                                                this.dxf_ContainedComponents, this.dxf_ReferencedComponents,
                                                this.dxf_ContainedParameters, this.dxf_ContainedCalculations_Ref,
                                                this.dxf_R2GInstances, this.dxf_Mapping2Comps, this.dxf_TimeStamp, this.dxf_SymbolId, true);
                        
        }

        #endregion

        #region OVERRIDES: To String
        public override string ToString()
        {
            string dxfS = "DXFComponent ";
            if (!(string.IsNullOrEmpty(this.dxf_Name)))
                dxfS += this.dxf_Name + ": ";

            int n = this.dxf_ContainedParameters.Count();
            dxfS += " has " + n.ToString() + " parameters:\n";
            for (int i = 0; i < n; i++)
            {
                dxfS += "_[ " + i + "]_" + this.EC_Entities[i].ToString() + "\n";
            }
            dxfS += "\n";
            return dxfS;
        }

        #endregion
    }

    #endregion

    // ---------------------------------------------- DXFFlowNetwork ------------------------------------------ //

    #region DXF_FlowNetwork

    public class DXF_FLowNetwork : DXFEntityContainer
    {
        #region CLASS MEMBERS

        // FlNetElement
        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public string dxf_Description { get; protected set; }
        public long dxf_Content_ID { get; protected set; }
        public bool dxf_IsValid { get; protected set; }

        // FlNetNode
        protected Point dxf_Position;

        protected List<FlowNetworkCalcRule> dxf_CalculationRules;
        protected int dxf_nr_CalculationRules;
        protected int dxf_nr_CalculationRules_read;
        protected FlowNetworkCalcRule dxf_current_rule;

        // FlowNetwork
        private List<FlNetNode> dxf_contained_nodes;
        private int dxf_nr_contained_nodes;
        
        private List<FlowNetwork> dxf_contained_networks;
        private int dxf_nr_contained_networks;
        
        private List<DXF_FlNetEdge_Preparsed> dxf_contained_edges_preparsed;
        private int dxf_nr_contained_edges;
        
        public ComponentManagerType dxf_Manager { get; private set; }
        public DateTime dxf_TimeStamp { get; private set; }
        public long dxf_Node_Start_ID { get; private set; }
        public long dxf_Node_End_ID { get; private set; }

        // parsed encapsulated class
        internal FlowNetwork dxf_parsed;

        // for nodes and edges that have their OnLoad method deferred
        List<DXF_FlNetNode> for_deferred_N_AddEntity;
        List<DXF_FLowNetwork> for_deferred_NW_AddEntity;
        List<DXF_FlNetEdge> for_deferred_E_AddEntity;

        #endregion

        #region .CTOR

        public DXF_FLowNetwork()
        {
            // FlNetElement
            this.dxf_ID = -1;
            this.dxf_Name = string.Empty;
            this.dxf_Description = string.Empty;
            this.dxf_Content_ID = -1;
            this.dxf_IsValid = false;

            // FlNetNode
            this.dxf_Position = new Point(0, 0);

            this.dxf_CalculationRules = new List<FlowNetworkCalcRule>();
            this.dxf_nr_CalculationRules = 0;
            this.dxf_nr_CalculationRules_read = 0;
            this.dxf_current_rule = new FlowNetworkCalcRule();

            // FlowNetwork
            this.dxf_contained_nodes = new List<FlNetNode>();
            this.dxf_nr_contained_nodes = 0;
            
            this.dxf_contained_networks = new List<FlowNetwork>();
            this.dxf_nr_contained_networks = 0;

            this.dxf_contained_edges_preparsed = new List<DXF_FlNetEdge_Preparsed>();
            this.dxf_nr_contained_edges = 0;
            
            this.dxf_Manager = ComponentManagerType.ADMINISTRATOR;
            this.dxf_TimeStamp = DateTime.MinValue;
            this.dxf_Node_Start_ID = -1;
            this.dxf_Node_End_ID = -1;

            // for nodes and edges that have their OnLoad method deferred
            this.for_deferred_N_AddEntity = new List<DXF_FlNetNode>();
            this.for_deferred_NW_AddEntity = new List<DXF_FLowNetwork>();
            this.for_deferred_E_AddEntity = new List<DXF_FlNetEdge>();
        }

        #endregion

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)FlowNetworkSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)FlowNetworkSaveCode.DESCRIPTION:
                    this.dxf_Description = this.Decoder.FValue;
                    break;
                case (int)FlowNetworkSaveCode.CONTENT_ID:
                    this.dxf_Content_ID = this.Decoder.LongValue();
                    break;
                case (int)FlowNetworkSaveCode.IS_VALID:
                    this.dxf_IsValid = (this.Decoder.IntValue() == 1) ? true : false;
                    break;
                case (int)FlowNetworkSaveCode.POSITION_X:
                    this.dxf_Position.X = this.Decoder.DoubleValue();
                    break;
                case (int)FlowNetworkSaveCode.POSITION_Y:
                    this.dxf_Position.Y = this.Decoder.DoubleValue();
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULES:
                    this.dxf_nr_CalculationRules = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Suffix_Operands = this.Decoder.FValue;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Suffix_Result = this.Decoder.FValue;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_DIRECTION:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Direction = (this.Decoder.FValue == "1") ? FlowNetworkCalcDirection.FORWARD : FlowNetworkCalcDirection.BACKWARD;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_OPERATOR:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Operator = FlowNetworkCalcRule.StringToOperator(this.Decoder.FValue);
                        this.dxf_CalculationRules.Add(this.dxf_current_rule);
                        this.dxf_current_rule = new FlowNetworkCalcRule();
                        this.dxf_nr_CalculationRules_read++;
                    }
                    break;
                case (int)FlowNetworkSaveCode.MANAGER:
                    this.dxf_Manager = ComponentUtils.StringToComponentManagerType(this.Decoder.FValue);
                    break;
                case (int)FlowNetworkSaveCode.TIMESTAMP:
                    DateTime dt_tmp = DateTime.Now;
                    bool dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_TimeStamp = dt_tmp;
                    break;
                case (int)FlowNetworkSaveCode.CONTAINED_NODES:
                    this.dxf_nr_contained_nodes = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CONTIANED_NETW:
                    this.dxf_nr_contained_networks = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CONTAINED_EDGES:
                    this.dxf_nr_contained_edges = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.NODE_SOURCE:
                    this.dxf_Node_Start_ID = this.Decoder.LongValue();
                    break;
                case (int)FlowNetworkSaveCode.NODE_SINK:
                    this.dxf_Node_End_ID = this.Decoder.LongValue();
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }
        #endregion

        #region OVERRIDES: Adding Entities

        public override bool AddEntity(DXFEntity _e)
        {
            // handle depending on type
            if (_e == null) return false;
            bool add_successful = false;

            DXFComponentSubContainer container = _e as DXFComponentSubContainer;
            if (container != null)
            {
                add_successful = true;
                foreach (DXFEntity sE in container.EC_Entities)
                {
                    DXF_FlNetNode node = sE as DXF_FlNetNode;
                    DXF_FLowNetwork nw = sE as DXF_FLowNetwork;
                    DXF_FlNetEdge edge = sE as DXF_FlNetEdge;
                    if (node != null && nw == null && this.dxf_nr_contained_nodes > this.dxf_contained_nodes.Count)
                    {
                        if (node.defer_OnLoading)
                        {
                            this.for_deferred_N_AddEntity.Add(node);
                            this.defer_AddEntity = true;
                        }
                        else
                        {
                            if (node.dxf_parsed != null)
                            {
                                this.dxf_contained_nodes.Add(node.dxf_parsed);
                                add_successful &= true;
                            }
                            else
                                add_successful = false;
                        }
                    }
                    else if(node == null && nw != null && this.dxf_nr_contained_networks > this.dxf_contained_networks.Count)
                    {
                        if (nw.defer_OnLoading)
                        {
                            this.dxf_ids_of_children_for_deferred_adding.Add(nw.ENT_ID);
                            this.for_deferred_NW_AddEntity.Add(nw);
                            this.defer_AddEntity = true;
                        }
                        else
                        {
                            if (nw.dxf_parsed != null)
                            {
                                // remove from the Factory record first (the record contains only top-level networks)
                                this.Decoder.COMP_Factory.RemoveNetworkRegardlessOfLocking(nw.dxf_parsed, false);
                                this.dxf_contained_networks.Add(nw.dxf_parsed);
                                add_successful &= true;
                            }
                            else
                                add_successful = false;
                        }
                    }
                    if (edge != null && this.dxf_nr_contained_edges > this.dxf_contained_edges_preparsed.Count)
                    {
                        if (edge.defer_OnLoading)
                        {
                            this.for_deferred_E_AddEntity.Add(edge);
                            this.defer_AddEntity = true;
                        }
                        else
                        {
                            if (edge.dxf_preparsed != null)
                            {
                                this.dxf_contained_edges_preparsed.Add(edge.dxf_preparsed);
                                add_successful &= true;
                            }
                            else
                                add_successful = false;
                        }
                    }
                }
            }

            if (this.defer_AddEntity)
            {
                this.Decoder.AddForDeferredAddEntity(this);
                this.defer_OnLoading = true;
            }

            return add_successful;
        }

        // to be called AFTER the OnLoad method for the deferred entities
        internal override void AddDeferredEntities()
        {
            foreach(DXF_FlNetNode n in this.for_deferred_N_AddEntity)
            {
                if (n != null && n.dxf_parsed != null && this.dxf_nr_contained_nodes > this.dxf_contained_nodes.Count)
                    this.dxf_contained_nodes.Add(n.dxf_parsed);
            }
            
            foreach(DXF_FLowNetwork nw in this.for_deferred_NW_AddEntity)
            {
                if (nw != null && nw.dxf_parsed != null && this.dxf_nr_contained_networks > this.dxf_contained_networks.Count)
                {
                    // remove from the Factory record first (the record contains only top-level networks)
                    this.Decoder.COMP_Factory.RemoveNetworkRegardlessOfLocking(nw.dxf_parsed, false);
                    this.dxf_contained_networks.Add(nw.dxf_parsed);
                }
            }

            foreach (DXF_FlNetEdge e in this.for_deferred_E_AddEntity)
            {
                if (e != null && e.dxf_preparsed != null && this.dxf_nr_contained_edges > this.dxf_contained_edges_preparsed.Count)
                    this.dxf_contained_edges_preparsed.Add(e.dxf_preparsed);
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        public override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.COMP_Factory == null) return;
            if (this.defer_OnLoading) return;

            // look for the associated component... (NOT IN USE)
            Component.Component content = null;
            if (this.dxf_Content_ID > -1)
                content = this.Decoder.COMP_Factory.GetByID(this.dxf_Content_ID);

            // complete the edge parsing...
            List<FlNetEdge> contained_edges = new List<FlNetEdge>();
            foreach(DXF_FlNetEdge_Preparsed ep in this.dxf_contained_edges_preparsed)
            {
                // look for the start and end nodes
                FlNetNode start = this.dxf_contained_nodes.Find(x => x.ID == ep.dxf_StartNode_ID);
                if (start == null)
                    start = this.dxf_contained_networks.Find(x => x.ID == ep.dxf_StartNode_ID);

                FlNetNode end = this.dxf_contained_nodes.Find(x => x.ID == ep.dxf_EndNode_ID);
                if (end == null)
                    end = this.dxf_contained_networks.Find(x => x.ID == ep.dxf_EndNode_ID);

                if (start != null && end != null)
                {
                    contained_edges.Add(new FlNetEdge(ep.dxf_ID, ep.dxf_Name, ep.dxf_Description, ep.dxf_Content, ep.dxf_IsValid, start, end));
                }
            }

            // parse the FlowNetwork
            this.dxf_parsed = this.Decoder.COMP_Factory.ReconstructNetwork(this.dxf_ID, this.dxf_Name, this.dxf_Description, content, this.dxf_IsValid,
                                                        this.dxf_Position, this.dxf_Manager, this.dxf_TimeStamp,
                                                        this.dxf_contained_nodes, contained_edges, this.dxf_contained_networks,
                                                        this.dxf_Node_Start_ID, this.dxf_Node_End_ID, this.dxf_CalculationRules, true);
        }

        #endregion

        #region OVERRIDES: To String

        public override string ToString()
        {
            string dxfS = "DXF_FlowNetwork ";
            if (!(string.IsNullOrEmpty(this.dxf_Name)))
                dxfS += this.dxf_Name + " ";
            if (!(string.IsNullOrEmpty(this.dxf_Description)))
                dxfS += "(" + this.dxf_Description + ")";

            dxfS += ": ";

            int n0 = this.dxf_contained_nodes.Count();
            int n1 = this.dxf_contained_networks.Count();
            int n2 = this.dxf_contained_edges_preparsed.Count();
            dxfS += "[ " + n0.ToString() + " nodes, " + n1.ToString() + " networks, " + n2.ToString() + " edges ]\n";
            
            dxfS += "\n";
            return dxfS;
        }

        #endregion

    }


    #endregion
}
