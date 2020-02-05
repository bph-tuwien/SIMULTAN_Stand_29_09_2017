using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Windows.Media.Media3D;

namespace InterProcCommunication.Specific
{
    internal class ComponentMessageParser
    {
        #region CLASS MEMBERS
        // reading
        public StreamReader FStream { get; private set; }
        public string FValue { get; private set; }
        public int FCode { get; private set; }

        // parsed message STRUCTURE
        private MessagePositionInSeq p_msg_pos;
        private long p_comp_parent_id;
        private long p_comp_repres_id;
        private long p_comp_repres_parent_id;

        private int nr_comp_ref_ids;
        private int nr_comp_ref_ids_read;
        private List<long> p_comp_ref_ids;

        private MessageAction p_msg_action_to_take;

        // parsed message CONTENT
        private long p_comp_id;
        private bool p_comp_autom_gen;
        private string p_comp_descr;
        private Dictionary<string, double> p_comp_params;

        int nr_comp_params;
        int nr_comp_params_read;
        string param_name;


        int nr_geom_relationships;
        int nr_geom_relationships_read;

        private long p_gr_id;
        private string p_gr_name;
        private Relation2GeomType p_gr_state_type;
        private bool p_gr_state_isRealized;
        private Point4D p_gr_ids;
        private Matrix3D p_gr_ucs;
        private Matrix3D p_gr_trWC2LC; // comes next to last
        private Matrix3D p_gr_trLC2WC; // comes last for parsing purposes

        private List<double> p_gr_inst_size;
        private int pp_gr_nr_inst_size;
        private long p_gr_inst_nwe_id;
        private string p_gr_inst_nwe_name;
        private List<Point3D> p_gr_inst_path;
        private int pp_gr_nr_inst_path;
        private Point3D pp_gr_inst_path_current_vertex;

        private List<GeometricRelationship> geom_relationships;

        // result
        public ComponentMessage ParsedMsg { get; private set; }

        #endregion

        #region .CTOR

        internal ComponentMessageParser()
        {
            this.Reset();
        }

        private void Reset()
        {
            // parsed message STRUCTURE
            this.p_msg_pos = MessagePositionInSeq.UNKNOWN;
            this.p_comp_parent_id = -1;
            this.p_comp_repres_id = -1;
            this.p_comp_repres_parent_id = -1;

            this.nr_comp_ref_ids = 0;
            this.nr_comp_ref_ids_read = 0;
            this.p_comp_ref_ids = new List<long>();

            this.p_msg_action_to_take = MessageAction.NONE;

            // parsed message CONTENT
            this.p_comp_id = -1;
            this.p_comp_autom_gen = false;
            this.p_comp_descr = "ComponentMessage";
            this.p_comp_params = new Dictionary<string, double>();

            this.nr_comp_params = 0;
            this.nr_comp_params_read = 0;
            this.param_name = string.Empty;

            this.nr_geom_relationships = 0;
            this.nr_geom_relationships_read = 0;

            this.p_gr_id = -1;
            this.p_gr_name = "Geometric Relationship";
            this.p_gr_state_type = Relation2GeomType.NONE;
            this.p_gr_state_isRealized = false;
            this.p_gr_ids = new Point4D(-1, -1, -1, -1);
            this.p_gr_ucs = Matrix3D.Identity;
            this.p_gr_trWC2LC = Matrix3D.Identity;
            this.p_gr_trLC2WC = Matrix3D.Identity;

            this.p_gr_inst_size = new List<double>();
            this.pp_gr_nr_inst_size = 0;
            this.p_gr_inst_nwe_id = -1L;
            this.p_gr_inst_nwe_name = "NW_Element";
            this.p_gr_inst_path = new List<Point3D>();
            this.pp_gr_nr_inst_path = 0;
            this.pp_gr_inst_path_current_vertex = new Point3D(0, 0, 0);

            this.geom_relationships = new List<GeometricRelationship>();
        }

        #endregion

        #region PARSING
        public void TranslateMessage(string _msg)
        {            
            try
            {
                // read and parse message
                Stream stream = ComponentMessageParser.GenerateStream(_msg, Encoding.UTF8);
                this.FStream = new StreamReader(stream);
                bool reached_eof = false;
                while (this.HasNext())
                {
                    this.Next();
                    if (this.FValue == ComponentMessage.END_OF_MESSAGE)
                    {
                        reached_eof = true;
                        this.ReleaseRessources();
                        break;
                    }
                    this.ParseComponentMessage();
                }
                if (!reached_eof)
                    this.ReleaseRessources();
                
                // finalize
                this.ParsedMsg = new ComponentMessage(this.p_msg_pos, this.p_comp_parent_id, this.p_comp_id, this.p_comp_autom_gen, 
                                                      this.p_comp_descr, this.p_comp_ref_ids, this.p_comp_params, this.geom_relationships,
                                                      this.p_comp_repres_id, this.p_comp_repres_parent_id, this.p_msg_action_to_take);
            }
            catch (Exception ex)
            {
                this.ReleaseRessources();
                MessageBox.Show(ex.Message, "Error reading message!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ParseComponentMessage()
        {
            switch (this.FCode)
            {
                case (int)MessageCode.ENTITY_START:
                    // start of entity ... do nothing
                    break;
                case (int)MessageCode.MSG_POSITION:
                    this.p_msg_pos = ComponentMessage.StringToMessagePositionInSeq(this.FValue);
                    break;
                case (int)MessageCode.MSG_COMP_PARENT_ID:
                    this.p_comp_parent_id = this.LongValue();
                    break;
                case (int)MessageCode.MSG_COMP_REPRESENTATION_ID:
                    this.p_comp_repres_id = this.LongValue();
                    break;
                case (int)MessageCode.MSG_COMP_REPRESENTATION_PARENT_ID:
                    this.p_comp_repres_parent_id = this.LongValue();
                    break;
                case (int)MessageCode.MSG_COMP_REF_IDS_SEQUENCE:
                    this.nr_comp_ref_ids = this.IntValue();
                    break;
                case (int)MessageCode.MSG_COMP_REF_ID:
                    if (this.nr_comp_ref_ids > this.nr_comp_ref_ids_read)
                    {
                        this.p_comp_ref_ids.Add(this.LongValue());
                        this.nr_comp_ref_ids_read++;
                    }
                    break;
                case (int)MessageCode.MSG_COMP_ACTION:
                    this.p_msg_action_to_take = ComponentMessage.StringToMessageAction(this.FValue);
                    break;
                case (int)MessageCode.COMP_ID:
                    this.p_comp_id = this.LongValue();
                    break;
                case (int)MessageCode.COMP_AUTOM_GEN:
                    this.p_comp_autom_gen = (this.IntValue() == 1);
                    break;
                case (int)MessageCode.COMP_DESCR:
                    this.p_comp_descr = this.FValue;
                    break;
                case (int)MessageCode.PARAM_SEQUENCE:
                    this.nr_comp_params = this.IntValue();
                    break;
                case (int)MessageCode.PARAM_NAME:
                    this.param_name = this.FValue;
                    break;
                case (int)MessageCode.PARAM_VALUE:
                    if (!string.IsNullOrEmpty(this.param_name) && this.nr_comp_params > this.nr_comp_params_read)
                    {
                        double param_value = this.DoubleValue();
                        this.p_comp_params.Add(this.param_name, param_value);
                        this.param_name = string.Empty;
                        this.nr_comp_params_read++;
                    }
                    break;
                case (int)MessageCode.GR_SEQUENCE:
                    this.nr_geom_relationships = this.IntValue();
                    break;
                case (int)MessageCode.GR_ID:
                    this.p_gr_id = this.LongValue();
                    break;
                case (int)MessageCode.GR_NAME:
                    this.p_gr_name = this.FValue;
                    break;
                case (int)MessageCode.GR_STATE_TYPE:
                    this.p_gr_state_type = GeometryUtils.StringToRelationship2Geometry(this.FValue);
                    break;
                case (int)MessageCode.GR_STATE_ISREALIZED:
                    this.p_gr_state_isRealized = (this.IntValue() == 1);
                    break;
                case (int)MessageCode.GR_GEOM_IDS_X:
                    this.p_gr_ids.X = this.IntValue();
                    break;
                case (int)MessageCode.GR_GEOM_IDS_Y:
                    this.p_gr_ids.Y = this.IntValue();
                    break;
                case (int)MessageCode.GR_GEOM_IDS_Z:
                    this.p_gr_ids.Z = this.IntValue();
                    break;
                case (int)MessageCode.GR_GEOM_IDS_W:
                    this.p_gr_ids.W = this.IntValue();
                    break;
                case (int)MessageCode.GR_GEOM_CS:
                    this.p_gr_ucs = Matrix3D.Parse(this.FValue);
                    break;
                // instance information
                case (int)MessageCode.GR_INST_SIZE:
                    this.pp_gr_nr_inst_size = this.IntValue();
                    break;
                case (int)MessageCode.GR_INST_NWE_ID:
                    this.p_gr_inst_nwe_id = this.LongValue();
                    break;
                case (int)MessageCode.GR_INST_NWE_NAME:
                    this.p_gr_inst_nwe_name = this.FValue;
                    break;
                case (int)MessageCode.GR_INST_PATH:
                    this.pp_gr_nr_inst_path = this.IntValue();
                    break;
                case (int)MessageCode.GR_INST_VAL_X:
                    if (this.pp_gr_nr_inst_size > this.p_gr_inst_size.Count)
                    {
                        this.p_gr_inst_size.Add(this.DoubleValue());
                    }
                    else if (this.pp_gr_nr_inst_path > this.p_gr_inst_path.Count)
                    {
                        this.pp_gr_inst_path_current_vertex.X = this.DoubleValue();
                    }
                    break;
                case (int)MessageCode.GR_INST_VAL_Y:
                    if (this.pp_gr_nr_inst_path > this.p_gr_inst_path.Count)
                    {
                        this.pp_gr_inst_path_current_vertex.Y = this.DoubleValue();
                    }
                    break;
                case (int)MessageCode.GR_INST_VAL_Z:
                    if (this.pp_gr_nr_inst_path > this.p_gr_inst_path.Count)
                    {
                        this.pp_gr_inst_path_current_vertex.Z = this.DoubleValue();
                        this.p_gr_inst_path.Add(this.pp_gr_inst_path_current_vertex);
                        this.pp_gr_inst_path_current_vertex = new Point3D(0, 0, 0);
                    }
                    break;
                // transforms
                case (int)MessageCode.GR_TRANSF_WC2LC:
                    this.p_gr_trWC2LC = Matrix3D.Parse(this.FValue);
                    break;
                case (int)MessageCode.GR_TRANSF_LC2WC:
                    // should come last for every GeometricRelationship                    
                    this.p_gr_trLC2WC = Matrix3D.Parse(this.FValue);
                    if (this.nr_geom_relationships > this.nr_geom_relationships_read)
                    {
                        Relation2GeomState gr_state = new Relation2GeomState { Type = this.p_gr_state_type, IsRealized = this.p_gr_state_isRealized };
                        GeometricRelationship gr = new GeometricRelationship(this.p_gr_id, this.p_gr_name, gr_state, this.p_gr_ids, 
                                                                             this.p_gr_ucs, this.p_gr_trWC2LC, this.p_gr_trLC2WC,
                                                                             this.p_gr_inst_size, this.p_gr_inst_nwe_id, this.p_gr_inst_nwe_name, this.p_gr_inst_path);
                        this.geom_relationships.Add(gr);
                        this.nr_geom_relationships_read++;
                        this.p_gr_inst_size = new List<double>();
                        this.p_gr_inst_path = new List<Point3D>();
                    }
                    break;
            }
        }
        #endregion

        #region UTILS: Reading a coded message

        public static Stream GenerateStream(string _s, Encoding _enc)
        {
            return new MemoryStream(_enc.GetBytes(_s ?? string.Empty));
        }

        // processes 2 lines: 
        // 1. the line containing the NUMERICAL CODE
        // 2. the line containing the INFORMATION saved under said code
        public void Next()
        {
            int code;
            bool success = Int32.TryParse(this.FStream.ReadLine(), out code);
            if (success)
                this.FCode = code;
            else
                this.FCode = -1;

            if (this.HasNext())
                this.FValue = this.FStream.ReadLine();
        }

        public bool HasNext()
        {
            if (this.FStream == null) return false;
            if (this.FStream.Peek() < 0) return false;
            return true;
        }

        public void ReleaseRessources()
        {
            if (this.FStream != null)
            {
                this.FStream.Close();
                try
                {
                    FStream.Dispose();
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }
        }
        #endregion

        #region UTILS: Simple Value Parsing

        public double DoubleValue()
        {
            switch (this.FValue)
            {
                case ComponentMessage.NAN:
                    return double.NaN;
                case "+" + ComponentMessage.INFINITY:
                    return double.MaxValue;
                case "-" + ComponentMessage.INFINITY:
                    return double.MinValue;
                default:
                    double f;
                    bool success = Double.TryParse(this.FValue, NumberStyles.Float, ComponentMessage.NR_FORMATTER, out f);
                    if (success)
                        return f;
                    else
                        return 0.0;
            }
        }

        public int IntValue()
        {
            int i;
            bool success = Int32.TryParse(this.FValue, out i);
            if (success)
                return i;
            else
                return 0;
        }

        public long LongValue()
        {
            long l;
            bool success = Int64.TryParse(this.FValue, out l);
            if (success)
                return l;
            else
                return 0;
        }

        public byte ByteValue()
        {
            byte b;
            bool success = Byte.TryParse(this.FValue, out b);
            if (success)
                return b;
            else
                return 0;
        }

        #endregion
    }

    
}
