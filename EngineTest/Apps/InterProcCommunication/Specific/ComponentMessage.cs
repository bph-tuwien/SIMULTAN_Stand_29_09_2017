using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Collections.ObjectModel;

namespace InterProcCommunication.Specific
{

    public enum MessagePositionInSeq
    {
        SINGLE_MESSAGE = 0,
        SEQUENCE_START_MESSAGE = 1,
        SEQUENCE_END_MESSAGE = 2,
        MESSAGE_INSIDE_SEQUENCE = 3,
        UNKNOWN = 4
    }

    public enum MessageAction
    {
        NONE = 0,
        DELETE = 1,
        CREATE = 2,
        UPDATE = 3
    }

    public enum MessageCode
    {
        ENTITY_START = 0,

        // comp-specific
        COMP_ID = 1,
        COMP_AUTOM_GEN = 2,
        COMP_DESCR = 3,

        // parameter-specific
        PARAM_SEQUENCE = 4,
        PARAM_NAME = 5,
        PARAM_VALUE = 6,

        // geometric relationship-specific
        GR_SEQUENCE = 7,
        GR_ID = 8,
        GR_NAME = 9,
        GR_STATE_TYPE = 10,
        GR_STATE_ISREALIZED = 11,
        GR_GEOM_IDS_X = 12,
        GR_GEOM_IDS_Y = 13,
        GR_GEOM_IDS_Z = 14,
        GR_GEOM_IDS_W = 50,
        GR_GEOM_CS = 15,
        GR_TRANSF_WC2LC = 16,
        GR_TRANSF_LC2WC = 17,
   
        GR_INST_SIZE = 31,
        GR_INST_NWE_ID = 32,
        GR_INST_NWE_NAME = 33,
        GR_INST_PATH = 34,
        GR_INST_VAL_X = 35,
        GR_INST_VAL_Y = 36,
        GR_INST_VAL_Z = 37,

        // messaging pipeline specific
        MSG_POSITION = 18,
        MSG_COMP_PARENT_ID = 19,
        MSG_COMP_REPRESENTATION_ID = 20,           // of the representation -> for automatic generation
        MSG_COMP_REPRESENTATION_PARENT_ID = 21,    // of the parent representation -> for automatic generation
        MSG_COMP_REF_IDS_SEQUENCE = 22,
        MSG_COMP_REF_ID = 23,

        // action on transfer to original sender
        MSG_COMP_ACTION = 40
    }

    public class ComponentMessage
    {
        #region STATIC

        public const string MESSAGE = "MESSAGE";
        public const string END_OF_MESSAGE = "EOF";

        public static System.IFormatProvider NR_FORMATTER = new System.Globalization.NumberFormatInfo();

        public const string INFINITY = "\U0000221E";
        public const string NAN = "NaN";

        public static string MessagePositionInSeqToString(MessagePositionInSeq _mpis)
        {
            switch(_mpis)
            {
                case MessagePositionInSeq.SINGLE_MESSAGE:
                    return "SINGLE_MESSAGE";
                case MessagePositionInSeq.SEQUENCE_START_MESSAGE:
                    return "SEQUENCE_START_MESSAGE";
                case MessagePositionInSeq.SEQUENCE_END_MESSAGE:
                    return "SEQUENCE_END_MESSAGE";
                case MessagePositionInSeq.MESSAGE_INSIDE_SEQUENCE:
                    return "MESSAGE_INSIDE_SEQUENCE";
                default:
                    return "UNKNOWN";
            }

        }

        public static MessagePositionInSeq StringToMessagePositionInSeq(string _mpis)
        {
            if (string.IsNullOrEmpty(_mpis)) return MessagePositionInSeq.UNKNOWN;

            switch(_mpis)
            {
                case "SINGLE_MESSAGE":
                    return MessagePositionInSeq.SINGLE_MESSAGE;
                case "SEQUENCE_START_MESSAGE":
                    return MessagePositionInSeq.SEQUENCE_START_MESSAGE;
                case "SEQUENCE_END_MESSAGE":
                    return MessagePositionInSeq.SEQUENCE_END_MESSAGE;
                case "MESSAGE_INSIDE_SEQUENCE":
                    return MessagePositionInSeq.MESSAGE_INSIDE_SEQUENCE;
                default:
                    return MessagePositionInSeq.UNKNOWN;
            }
        }

        public static string MessageActionToString(MessageAction _action)
        {
            switch(_action)
            {
                case MessageAction.CREATE:
                    return "CREATE";
                case MessageAction.DELETE:
                    return "DELETE";
                case MessageAction.UPDATE:
                    return "UPDATE";
                default:
                    return "NONE";
            }
        }

        public static MessageAction StringToMessageAction(string _action)
        {
            if (string.IsNullOrEmpty(_action)) return MessageAction.NONE;
            switch(_action)
            {
                case "CREATE":
                    return MessageAction.CREATE;
                case "DELETE":
                    return MessageAction.DELETE;
                case "UPDATE":
                    return MessageAction.UPDATE;
                default:
                    return MessageAction.NONE;
            }
        }

        public static ComponentMessage GetDummyMessage(MessagePositionInSeq _pos)
        {
            return new ComponentMessage(_pos, -1L, -1L, true, "Dummy Message", new List<long>(), new Dictionary<string, double>(),
                                new List<GeometricRelationship>{ new GeometricRelationship(-1L, "Dummy", new Relation2GeomState{ Type = Relation2GeomType.NONE, IsRealized = false}, 
                                                                            new Point4D(-1, -1, -1, -1),  Matrix3D.Identity, Matrix3D.Identity, Matrix3D.Identity)}, -1L, -1L, MessageAction.NONE);
        }

        public static ComponentMessage MessageForSelection(long _comp_id_to_select)
        {
            return new ComponentMessage(MessagePositionInSeq.SINGLE_MESSAGE, -1L, _comp_id_to_select, true, "Dummy Message", new List<long>(), new Dictionary<string, double>(),
                                new List<GeometricRelationship>{ new GeometricRelationship(-1L, "Dummy", new Relation2GeomState{ Type = Relation2GeomType.NONE, IsRealized = false}, 
                                                                            new Point4D(-1, -1, -1, -1),  Matrix3D.Identity, Matrix3D.Identity, Matrix3D.Identity)}, -1L, -1L, MessageAction.NONE);
        }

        #endregion

        #region STATIC COMPARISON

        /// <summary>
        /// <para>This method should be used to retrieve the id of an automatically generated component.</para>
        /// </summary>
        /// <param name="_cmsg_query"></param>
        /// <param name="_cmsg_test"></param>
        /// <returns></returns>
        public static bool HaveSameContent(ComponentMessage _cmsg_query, ComponentMessage _cmsg_test)
        {
            if (_cmsg_query == null && _cmsg_test == null) return true;
            if (_cmsg_query != null && _cmsg_test == null) return false;
            if (_cmsg_query == null && _cmsg_test != null) return false;

            // component properties
            if (_cmsg_query.comp_automatically_generated != _cmsg_test.comp_automatically_generated) return false;
            // if (_cmsg_query.comp_descr != _cmsg_test.comp_descr) return false; this cannot be checked

            if (_cmsg_query.comp_ref_ids != null && _cmsg_test.comp_ref_ids == null) return false;
            if (_cmsg_query.comp_ref_ids == null && _cmsg_test.comp_ref_ids != null) return false;
            if (_cmsg_query.comp_ref_ids != null && _cmsg_test.comp_ref_ids != null)
            {
                int nrRefs = _cmsg_query.comp_ref_ids.Count;
                if (_cmsg_test.comp_ref_ids.Count != nrRefs) return false;

                if (nrRefs > 0)
                {
                    bool same_comp_ref_ids = _cmsg_query.comp_ref_ids.Zip(_cmsg_test.comp_ref_ids, (x, y) => x == y).Aggregate(true, (a, x) => a && x);
                    if (!same_comp_ref_ids) return false;
                }               
            }

            bool same_comp_params = ComponentMessage.LogicalEquality(_cmsg_query.comp_params, _cmsg_test.comp_params);
            if (!same_comp_params) return false;

            // component geometric relationships
            bool same_gr = GeometricRelationship.HaveListsWSameContent(_cmsg_query.geom_relationships, _cmsg_test.geom_relationships);
            if (!same_gr) return false;

            return true;
        }

        private static bool LogicalEquality(Dictionary<string, double> _d1, Dictionary<string, double> _d2)
        {
            if (_d1 == null && _d2 != null) return false;
            if (_d1 != null && _d2 == null) return false;
            if (_d1 == null && _d2 == null) return true;

            int nrD1 = _d1.Count;
            if (nrD1 != _d2.Count) return false;

            List<string> d1_keys = _d1.OrderBy(x => x.Key).Select(x => x.Key).ToList();
            List<string> d2_keys = _d2.OrderBy(x => x.Key).Select(x => x.Key).ToList();
            bool same_keys = d1_keys.SequenceEqual(d2_keys);
            if (!same_keys) return false;

            List<double> d1_values = _d1.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            List<double> d2_values = _d2.OrderBy(x => x.Key).Select(x => x.Value).ToList();
            bool same_values = d1_values.Zip(d2_values, (x, y) => GeometricTransformations.LogicalEquality(x, y)).Aggregate(true, (a, x) => a && x);
            if (!same_values) return false;

            return true;
        }

        #endregion

        #region CLASS MEMBERS

        // structure
        protected MessagePositionInSeq msg_pos;
        protected long comp_parent_id;
        protected List<long> comp_ref_ids; // saves the ids of referenced components

        // data
        protected long comp_id;
        protected string comp_descr;
        protected bool comp_automatically_generated;
        protected Dictionary<string, double> comp_params;

        protected List<GeometricRelationship> geom_relationships;

        // for automatically generated components with an automatically generated parent
        protected long comp_rep_id;
        protected long comp_rep_parent_id;

        // action
        protected MessageAction action_to_take;

        #endregion

        #region PROPERTIES (Get only)

        // structure
        public MessagePositionInSeq MsgPos { get { return this.msg_pos; } }
        public long CompParentID { get { return this.comp_parent_id; } }
        public ReadOnlyCollection<long> CompRefIds { get { return this.comp_ref_ids.AsReadOnly(); } }


        // data
        public long CompID { get { return this.comp_id; } }
        public string CompDescr { get { return this.comp_descr; } }
        public bool CompAutomaticallyGenerated { get { return this.comp_automatically_generated; } }
        public double this[string index]
        {
            get 
            {
                if (this.comp_params.ContainsKey(index))
                    return this.comp_params[index];
                else
                    return double.NaN;
            }
        }

        public ReadOnlyCollection<GeometricRelationship> GeomRelationships { get { return this.geom_relationships.AsReadOnly(); } }

        public Relation2GeomType GeomType
        {
            get
            {
                if (this.GeomRelationships.Count == 0)
                    return Relation2GeomType.NONE;
                else
                    return this.geom_relationships[0].GrState.Type;
            }
        }

        public bool GeomRelRealized
        {
            get
            {
                if (this.GeomRelationships.Count == 0)
                    return false;
                else
                    return this.geom_relationships[0].GrState.IsRealized;
            }
        }

        // for automatically generated components with an automatically generated parent
        public long CompRepID { get { return this.comp_rep_id; } }
        public long CompRepParentID { get { return this.comp_rep_parent_id; } }

        public MessageAction ActionToTake { get { return this.action_to_take; } }

        #endregion

        #region .CTOR

        public ComponentMessage(MessagePositionInSeq _pos, long _comp_parent_id, long _comp_id, bool _comp_ag, string _comp_descr,
                                List<long> _ref_ids, Dictionary<string, double> _comp_params, List<GeometricRelationship> _geom_rel,
                                long _comp_rep_id, long _comp_rep_parent_id, MessageAction _action_to_take)
        {
            // structural info
            this.msg_pos = _pos;
            this.comp_parent_id = _comp_parent_id;
            this.comp_rep_id = _comp_rep_id;
            this.comp_rep_parent_id = _comp_rep_parent_id;
            this.action_to_take = _action_to_take;

            // component info
            this.comp_id = _comp_id;
            this.comp_automatically_generated = _comp_ag;
            this.comp_descr = _comp_descr;
            this.comp_ref_ids = _ref_ids;

            this.comp_params = _comp_params;

            this.geom_relationships = new List<GeometricRelationship>();
            if (_geom_rel != null && _geom_rel.Count > 0)
                this.geom_relationships = new List<GeometricRelationship>(_geom_rel);  
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // mark start
            sb.AppendLine(((int)MessageCode.ENTITY_START).ToString()); // 0
            sb.AppendLine(ComponentMessage.MESSAGE);                   // MESSAGE

            // message position in sequence
            sb.AppendLine(((int)MessageCode.MSG_POSITION).ToString());
            sb.AppendLine(ComponentMessage.MessagePositionInSeqToString(this.msg_pos));

            // parent component id
            sb.AppendLine(((int)MessageCode.MSG_COMP_PARENT_ID).ToString());
            sb.AppendLine(this.comp_parent_id.ToString());

            // ids of referenced components
            sb.AppendLine(((int)MessageCode.MSG_COMP_REF_IDS_SEQUENCE).ToString());
            sb.AppendLine(this.comp_ref_ids.Count.ToString());

            foreach(long id in this.comp_ref_ids)
            {
                sb.AppendLine(((int)MessageCode.MSG_COMP_REF_ID).ToString());
                sb.AppendLine(id.ToString());
            }

            // comp
            sb.AppendLine(((int)MessageCode.COMP_ID).ToString());
            sb.AppendLine(this.comp_id.ToString());

            sb.AppendLine(((int)MessageCode.COMP_AUTOM_GEN).ToString());
            string tmp = (this.comp_automatically_generated) ? "1" : "0";
            sb.AppendLine(tmp);

            sb.AppendLine(((int)MessageCode.COMP_DESCR).ToString());
            sb.AppendLine(this.comp_descr);

            // comp parameters
            if (this.comp_params != null && this.comp_params.Count > 0)
            {
                sb.AppendLine(((int)MessageCode.PARAM_SEQUENCE).ToString());
                sb.AppendLine(this.comp_params.Count.ToString());

                foreach (var entry in this.comp_params)
                {
                    sb.AppendLine(((int)MessageCode.PARAM_NAME).ToString());
                    sb.AppendLine(entry.Key);
                    sb.AppendLine(((int)MessageCode.PARAM_VALUE).ToString());
                    sb.AppendLine(entry.Value.ToString(ComponentMessage.NR_FORMATTER));
                }
            }

            // geometric relationships
            sb.AppendLine(((int)MessageCode.GR_SEQUENCE).ToString());
            sb.AppendLine(this.geom_relationships.Count.ToString());

            foreach(GeometricRelationship gr in this.geom_relationships)
            {
                sb.AppendLine(((int)MessageCode.GR_ID).ToString());
                sb.AppendLine(gr.GrID.ToString());

                sb.AppendLine(((int)MessageCode.GR_NAME).ToString());
                sb.AppendLine(gr.GrName);

                sb.AppendLine(((int)MessageCode.GR_STATE_TYPE).ToString());
                sb.AppendLine(GeometryUtils.Relationship2GeometryToString(gr.GrState.Type));

                sb.AppendLine(((int)MessageCode.GR_STATE_ISREALIZED).ToString());
                string tmp_gr = (gr.GrState.IsRealized) ? "1" : "0";
                sb.AppendLine(tmp_gr);

                sb.AppendLine(((int)MessageCode.GR_GEOM_IDS_X).ToString());
                sb.AppendLine(gr.GrIds.X.ToString());

                sb.AppendLine(((int)MessageCode.GR_GEOM_IDS_Y).ToString());
                sb.AppendLine(gr.GrIds.Y.ToString());

                sb.AppendLine(((int)MessageCode.GR_GEOM_IDS_Z).ToString());
                sb.AppendLine(gr.GrIds.Z.ToString());

                sb.AppendLine(((int)MessageCode.GR_GEOM_IDS_W).ToString());
                sb.AppendLine(gr.GrIds.W.ToString());

                sb.AppendLine(((int)MessageCode.GR_GEOM_CS).ToString());
                sb.AppendLine(gr.GrUCS.ToString(ComponentMessage.NR_FORMATTER));

                // instance information
                sb.AppendLine(((int)MessageCode.GR_INST_SIZE).ToString());
                sb.AppendLine(gr.InstSize.Count.ToString());
                foreach (double entry in gr.InstSize)
                {
                    sb.AppendLine(((int)MessageCode.GR_INST_VAL_X).ToString());
                    sb.AppendLine(entry.ToString(ComponentMessage.NR_FORMATTER));
                }

                sb.AppendLine(((int)MessageCode.GR_INST_NWE_ID).ToString());
                sb.AppendLine(gr.InstNWeId.ToString());

                sb.AppendLine(((int)MessageCode.GR_INST_NWE_NAME).ToString());
                sb.AppendLine(gr.InstNWeName);

                sb.AppendLine(((int)MessageCode.GR_INST_PATH).ToString());
                sb.AppendLine(gr.InstPath.Count.ToString());
                foreach (Point3D vertex in gr.InstPath)
                {
                    sb.AppendLine(((int)MessageCode.GR_INST_VAL_X).ToString());
                    sb.AppendLine(vertex.X.ToString(ComponentMessage.NR_FORMATTER));
                    sb.AppendLine(((int)MessageCode.GR_INST_VAL_Y).ToString());
                    sb.AppendLine(vertex.Y.ToString(ComponentMessage.NR_FORMATTER));
                    sb.AppendLine(((int)MessageCode.GR_INST_VAL_Z).ToString());
                    sb.AppendLine(vertex.Z.ToString(ComponentMessage.NR_FORMATTER));
                }

                // transforms
                sb.AppendLine(((int)MessageCode.GR_TRANSF_WC2LC).ToString());
                sb.AppendLine(gr.GrTrWC2LC.ToString(ComponentMessage.NR_FORMATTER));

                // SHOULD COME LAST!
                sb.AppendLine(((int)MessageCode.GR_TRANSF_LC2WC).ToString());
                sb.AppendLine(gr.GrTrLC2WC.ToString(ComponentMessage.NR_FORMATTER));

            }
   
            // structural info from the representations (for automatically generated components)
            sb.AppendLine(((int)MessageCode.MSG_COMP_REPRESENTATION_ID).ToString());
            sb.AppendLine(this.comp_rep_id.ToString());

            sb.AppendLine(((int)MessageCode.MSG_COMP_REPRESENTATION_PARENT_ID).ToString());
            sb.AppendLine(this.comp_rep_parent_id.ToString());

            // action to take on return to original sender
            sb.AppendLine(((int)MessageCode.MSG_COMP_ACTION).ToString());
            sb.AppendLine(ComponentMessage.MessageActionToString(this.action_to_take));

            // done
            sb.AppendLine(((int)MessageCode.ENTITY_START).ToString()); // 0
            sb.AppendLine(ComponentMessage.END_OF_MESSAGE);            // EOF

            return sb.ToString();
        }


        #endregion

        #region FromString

        public static ComponentMessage FromString(string _msg)
        {
            ComponentMessageParser parser = new ComponentMessageParser();
            parser.TranslateMessage(_msg);

            return parser.ParsedMsg;
        }

        #endregion

    }
}
