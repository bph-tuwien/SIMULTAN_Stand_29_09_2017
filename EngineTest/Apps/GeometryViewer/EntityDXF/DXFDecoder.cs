using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Diagnostics;

using GeometryViewer.EntityGeometry;
using GeometryViewer.ComponentInteraction;

namespace GeometryViewer.EntityDXF
{
    public class DXFDecoder
    {
        #region CALSS MEMBERS

        public StreamReader FStream { get; private set; }
        public NumberFormatInfo N { get; private set; }
        public string FValue { get; private set; }
        public int FCode { get; private set; }

        public DXFSection FMainSect { get; set; }   // only for parsing purposes (contains nothing)
        public DXFSection FEntities { get; set; }   // contains the parsed entites
        public DXFSection FMaterials { get; set; }   // contains the parsed materials

        public EntityManager EManager { get; private set; }
        public MaterialManager MLManager { get; private set; }

        private List<DXFEntity> for_deferred_OnLoaded_execution;
        private List<DXFLayer> for_deferred_AdEntity_execution;

        // debugging
        private int at_line_in_file = 0;

        #endregion

        #region DEBUGGING

        internal void PositionToOutputWindow(string _prefix)
        {
            //Debug.WriteLine("{0} - Line NR: {1}", _prefix, this.at_line_in_file);
        }

        #endregion

        #region .CTOR

        public DXFDecoder(EntityManager _em, MaterialManager _mlm)
        {
            this.N = new NumberFormatInfo();
            N.NumberDecimalSeparator = ".";
            N.NumberGroupSeparator = " ";

            this.EManager = _em;
            this.MLManager = _mlm;

            if (_em != null && _mlm != null)
            {
                _em.Reset(false);
                _mlm.Reset();

                _em.ResetStaticCounters();
                _mlm.ResetMaterialCounter();
            }

            this.for_deferred_OnLoaded_execution = new List<DXFEntity>();
            this.for_deferred_AdEntity_execution = new List<DXFLayer>();
        }

        #endregion

        #region METHODS: Decoding

        public DXFEntity CreateEntity(string _name_prefix = "")
        {            
            DXFEntity E;
            switch(this.FValue)
            {
                case DXFUtils.SECTION_END:
                case DXFUtils.SEQUENCE_END:
                    this.PositionToOutputWindow("Decoder NULL");
                    return null;
                case DXFUtils.SECTION_START:
                    E = new DXFSection();
                    this.PositionToOutputWindow("Decoder DXFSection");
                    break;
                case DXFUtils.ENTITY_SEQUENCE:
                    E = new DXFHierarchicalContainer("of " + _name_prefix); // helper for ENTITIES containing hierarchical structures (e.g. Layer)
                    this.PositionToOutputWindow("Decoder DXFHierarchicalContainer");
                    break;
                case DXFUtils.ENTITY_CONTINUE:
                    E = new DXFContinue(); // helper for ENTITIES containing hierarchical structures (e.g. Layer)
                    this.PositionToOutputWindow("Decoder DXFContinue");
                    break;
                case DXFUtils.GV_LAYER:
                    E = new DXFLayer();
                    this.PositionToOutputWindow("Decoder DXFLayer");
                    break;
                case DXFUtils.GV_ZONEDPOLY:
                    E = new DXFZonedPolygon();
                    this.PositionToOutputWindow("Decoder DXFZonedPolygon");
                    break;
                case DXFUtils.GV_ZONEDPOLY_OPENING:
                    E = new DXFZoneOpening();
                    this.PositionToOutputWindow("Decoder DXFZoneOpening");
                    break;
                case DXFUtils.GV_ZONEDLEVEL:
                    E = new DXFZonedPolygonGroup();
                    this.PositionToOutputWindow("Decoder DXFZonedPolygonGroup");
                    break;
                case DXFUtils.GV_ZONEDVOL:
                    E = new DXFZonedVolume();
                    this.PositionToOutputWindow("Decoder DXFZonedVolume");
                    break;
                case DXFUtils.GV_MATERIAL:
                    E = new DXFMaterial();
                    this.PositionToOutputWindow("Decoder DXFMaterial");
                    break;
                default:
                    E = new DXFDummy(this.FValue);
                    this.PositionToOutputWindow("Decoder DXFDummy");
                    break;
            }
            E.Decoder = this;
            return E;
        }

        public void DeferOnLoadedExecution(DXFEntity _e)
        {
            if (_e != null && !(_e is DXFDummy))
            {
                this.for_deferred_OnLoaded_execution.Add(_e);
            }
        }

        public void DeferAddEntityExecution(DXFLayer _lay)
        {
            if (_lay != null)
            {
                this.for_deferred_AdEntity_execution.Add(_lay);
            }
        }

        private void ExecuteDeferredOnLoaded()
        {
            // sort so that TonedPolygonGroups come before ZonedVolumes
            List<DXFZonedPolygonGroup> zpgs = new List<DXFZonedPolygonGroup>();
            List<DXFZonedVolume> zvs = new List<DXFZonedVolume>();
            foreach (DXFEntity e in this.for_deferred_OnLoaded_execution)
            {
                if (e is DXFZonedPolygonGroup)
                    zpgs.Add(e as DXFZonedPolygonGroup);
                else if (e is DXFZonedVolume)
                    zvs.Add(e as DXFZonedVolume);
            }

            foreach (DXFZonedPolygonGroup zpg in zpgs)
            {
                zpg.deferred_execute_OnLoad = false;
                zpg.OnLoaded();
            }
            foreach (DXFZonedVolume zv in zvs)
            {
                zv.deferred_execute_OnLoad = false;
                zv.OnLoaded();
            }

            // assign layers (deferred)
            foreach (DXFLayer lay in this.for_deferred_AdEntity_execution)
            {                
                lay.AddDeferred();
            }

            // clean-up
            this.for_deferred_OnLoaded_execution = new List<DXFEntity>();
        }

        #endregion

        #region METHODS: Reading File

        public void LoadFromFile(string _fileName, bool _lock_parsed_components = false)
        {
            this.FMainSect = new DXFSection();
            this.FMainSect.Decoder = this;
            if (this.FStream == null)
            {
                this.FStream = new StreamReader(_fileName);
            }
            this.FMainSect.ParseNext();

            this.ExecuteDeferredOnLoaded();
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
                this.FCode = (int)EntitySaveCode.INVALID_CODE;

            this.FValue = this.FStream.ReadLine();
            this.at_line_in_file += 2;            
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

        #region Utility METHODS: Simple Value Parsing

        public double DoubleValue()
        {
            return DXFUtils.StringToDouble(this.FValue);
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
