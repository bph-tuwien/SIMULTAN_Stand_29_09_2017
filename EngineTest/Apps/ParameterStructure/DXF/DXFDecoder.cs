using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.IO;
using System.Globalization;

using ParameterStructure.Values;
using ParameterStructure.Parameter;
using ParameterStructure.Component;

namespace ParameterStructure.DXF
{
    public class DXFDecoder
    {
        #region CLASS MEMBERS
        public StreamReader FStream { get; private set; }
        public NumberFormatInfo N { get; private set; }
        public string FValue { get; private set; }
        public int FCode { get; private set; }

        public DXFSection FMainSect { get; set; }   // only for parsing purposes (contains nothing)
        public DXFSection FEntities { get; set; }   // contains the parsed entites

        public MultiValueFactory MV_Factory { get; private set; }
        public ParameterFactory P_Factory { get; private set; }
        public ComponentFactory COMP_Factory { get; private set; }

        // deferred loading of entities
        private Dictionary<DXFEntity, bool> for_deferred_loading;
        private Dictionary<DXFEntity, bool> for_deferred_addEntity;

        #endregion

        #region .CTOR

        public DXFDecoder(MultiValueFactory _mv_factory)
        {
            this.N = new NumberFormatInfo();
            N.NumberDecimalSeparator = ".";
            N.NumberGroupSeparator = " ";
            this.MV_Factory = _mv_factory;

            this.for_deferred_loading = new Dictionary<DXFEntity, bool>();
            this.for_deferred_addEntity = new Dictionary<DXFEntity, bool>();
        }

        public DXFDecoder(MultiValueFactory _mv_factory, ParameterFactory _p_factory)
        {
            this.N = new NumberFormatInfo();
            N.NumberDecimalSeparator = ".";
            N.NumberGroupSeparator = " ";
            this.MV_Factory = _mv_factory;
            this.P_Factory = _p_factory;

            this.for_deferred_loading = new Dictionary<DXFEntity, bool>();
            this.for_deferred_addEntity = new Dictionary<DXFEntity, bool>();
        }

        public DXFDecoder(MultiValueFactory _mv_factory, ParameterFactory _p_factory, ComponentFactory _c_factory)
        {
            this.N = new NumberFormatInfo();
            N.NumberDecimalSeparator = ".";
            N.NumberGroupSeparator = " ";
            this.MV_Factory = _mv_factory;
            this.P_Factory = _p_factory;
            this.COMP_Factory = _c_factory;

            this.for_deferred_loading = new Dictionary<DXFEntity, bool>();
            this.for_deferred_addEntity = new Dictionary<DXFEntity, bool>();
        }

        #endregion

        #region METHODS: Decoding

        public DXFEntity CreateEntity()
        {
            DXFEntity E;
            switch (this.FValue)
            {
                case ParamStructTypes.SECTION_END:
                case ParamStructTypes.SEQUENCE_END:
                    return null;
                case ParamStructTypes.SECTION_START:
                    E = new DXFSection();
                    break;
                case ParamStructTypes.ENTITY_SEQUENCE:
                    E = new DXFComponentSubContainer(); // helper for COMPONENT, FLOWNETWORK
                    break;
                case ParamStructTypes.ENTITY_CONTINUE:
                    E = new DXFContinue(); // helper for COMPONENT, FLOWNETWORK
                    break;
                case ParamStructTypes.COMPONENT:
                    E = new DXFComponent();
                    break;
                case ParamStructTypes.ACCESS_PROFILE:
                    E = new DXFAccessProfile();
                    break;
                case ParamStructTypes.ACCESS_TRACKER:
                    E = new DXFAccessTracker();
                    break;
                case ParamStructTypes.CALCULATION:
                    E = new DXFCalculation();
                    break;
                case ParamStructTypes.PARAMETER:
                    E = new DXFParameter();
                    break;
                case ParamStructTypes.VALUE_FIELD:
                    E = new DXFMultiValueField();
                    break;
                case ParamStructTypes.FUNCTION_FIELD:
                    E = new DXFMultiValueFunction();
                    break;
                case ParamStructTypes.BIG_TABLE:
                    E = new DXFMultiValueBigTable();
                    break;
                case ParamStructTypes.FLOWNETWORK:
                    E = new DXF_FLowNetwork();
                    break;
                case ParamStructTypes.FLOWNETWORK_NODE:
                    E = new DXF_FlNetNode();
                    break;
                case ParamStructTypes.FLOWNETWORK_EDGE:
                    E = new DXF_FlNetEdge();
                    break;
                case ParamStructTypes.GEOM_RELATION:
                    E = new DXFGeometricRelationship();
                    break;
                case ParamStructTypes.MAPPING_TO_COMP:
                    E = new DXFMapping2Component();
                    break;
                default:
                    E = new DXFDummy(this.FValue);
                    break;
            }
            E.Decoder = this;
            return E;
        }

        #endregion

        #region DEFERRED LOADING AND ADDING

        internal void AddForDeferredAddEntity(DXFEntity e)
        {
            if (e != null)
            {
                DXFEntity found = this.for_deferred_addEntity.Keys.FirstOrDefault(x => x.ENT_ID == e.ENT_ID);
                if (found == null)
                    this.for_deferred_addEntity.Add(e, this.COMP_Factory.LockParsedComponents);
            }
        }

        internal void AddForDeferredOnLoad(DXFEntity e)
        {
            if (e != null)
                this.for_deferred_loading.Add(e, this.COMP_Factory.LockParsedComponents);
        }

        private void DoDeferredLoading()
        {
            foreach(var entry in this.for_deferred_loading)
            {
                DXFEntity e = entry.Key;
                e.defer_OnLoading = false;
                this.COMP_Factory.LockParsedComponents = entry.Value;
                e.OnLoaded();
            }
            this.for_deferred_loading = new Dictionary<DXFEntity, bool>();
        }

        private void DoDeferredEntityAdding()
        {
            List<long> done = new List<long>();
            while (done.Count < this.for_deferred_addEntity.Count)
            {
                foreach (var entry in this.for_deferred_addEntity)
                {
                    // test if entry should be processed
                    DXFEntity e = entry.Key;
                    if (done.Contains(e.ENT_ID))
                        continue;

                    DXFEntityContainer ec = e as DXFEntityContainer;
                    if (ec == null)
                    {
                        done.Add(e.ENT_ID);
                        continue;
                    }

                    // test if any children need to be processed first
                    bool waiting_for_children = false;
                    foreach(long id in ec.dxf_ids_of_children_for_deferred_adding)
                    {
                        if (!done.Contains(id))
                        {
                            waiting_for_children = true;
                            break;
                        }
                    }
                    if (waiting_for_children)
                        continue;

                    // process
                    ec.defer_AddEntity = false;
                    ec.defer_OnLoading = false;
                    ec.AddDeferredEntities();
                    this.COMP_Factory.LockParsedComponents = entry.Value;
                    ec.OnLoaded();

                    // mark as processed
                    done.Add(ec.ENT_ID);
                }
            }
            this.for_deferred_addEntity = new Dictionary<DXFEntity, bool>();
        }

        #endregion

        #region METHODS: Reading File

        public void LoadFromFile(string _fileName, bool _lock_parsed_components = false)
        {
            if (this.COMP_Factory != null)
                this.COMP_Factory.LockParsedComponents = _lock_parsed_components;

            this.FMainSect = new DXFSection();
            this.FMainSect.Decoder = this;
            if (this.FStream == null)
            {
                this.FStream = new StreamReader(_fileName);
            }
            this.FMainSect.ParseNext();
            this.ReleaseRessources();
        }

        public void DoDeferredOperations()
        {
            this.DoDeferredLoading();
            this.DoDeferredEntityAdding();
        }

        // processes 2 lines: 
        // 1. the line containing the DXF CODE
        // 2. the line containing the INFORMATION saved under said code
        public void Next()
        {
            int code;
            bool success = Int32.TryParse(this.FStream.ReadLine(), out code);
            if (success)
                this.FCode = code;
            else
                this.FCode = (int)ParamStructCommonSaveCode.INVALID_CODE;

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
            this.FStream = null;
        }


        #endregion

        #region Utility METHODS: Simple Value Parsing

        public double DoubleValue()
        {
            return Parameter.Parameter.StringToDouble(this.FValue);
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
