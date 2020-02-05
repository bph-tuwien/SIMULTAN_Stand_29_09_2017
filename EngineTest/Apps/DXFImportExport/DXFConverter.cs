using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.IO;
using System.Globalization;

namespace DXFImportExport
{
    public delegate void DXFConverterProc(DXFEntity _entity);
    public class DXFIterate
    {
        public Point3D Scale { get; set; }
        public Matrix3D Matrix { get; set; }
        
        // TODO: entitiy to insert
    }

    public class DXFConverter
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== PRIVATE CLASS MEMBERS ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public StreamReader FStream { get; private set; }
        public NumberFormatInfo N { get; private set; }
        public string FValue { get; private set; }
        public int FCode { get; private set; }
        public DXFIterate FParams { get; private set; }

        public float FScale { get; set; }
        public Point3D Base { get; set; }
        public DXFTable FLayers { get; set; }
        public DXFSection FMainSect { get; set; }
        public DXFSection FEntities { get; set; }
        public DXFSection FBlocks { get; set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== CONSTRUCTORS ============================================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public DXFConverter()
        {
            N = new NumberFormatInfo();
            N.NumberDecimalSeparator = ".";
            FParams = new DXFIterate();
            FParams.Scale = new Point3D(1.0, 1.0, 1.0);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================= METHODS ============================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void LoadFromFile(string _fileName)
        {
            this.FMainSect = new DXFSection();
            this.FMainSect.Converter = this;
            if (this.FStream == null)
            {
                this.FStream = new StreamReader(_fileName);
            }
            this.FMainSect.ParseNext();
        }

        public DXFEntity CreateEntity()
        {
            DXFEntity E;
            switch(this.FValue)
            {
                case "ENDSEC":                
                case "ENDTAB":
                case "ENDBLK":
                case "SEQEND":
                    return null;
                case "SECTION":
                    E = new DXFSection();
                    break;
                case "TABLE":
                    E = new DXFTable();
                    break;
                case "BLOCK":
                    E = new DXFBlock();
                    break;
                case "ATTDEF":
                case "ATTRIB":
                    E = new DXFAttribute();
                    break;
                case "INSERT":
                    E = new DXFInsert();
                    break;
                case "LAYER":
                    E = new DXFLayer();
                    break;
                case "LINE":
                    E = new DXFLine();
                    break;
                case "CIRCLE":
                case "ARC":
                    E = new DXFCircle();
                    break;
                case "ELLIPSE":
                    E = new DXFEllipse();
                    break;
                case "LWPOLYLINE":
                    E = new DXFLWPolyLine();
                    break;
                case "TEXT":
                    E = new DXFText();
                    break;
                case "MTEXT":
                    E = new DXFMText();
                    break;     
                case "POLYLINE":
                    E = new DXFPolyLine();
                    break;
                case "VERTEX":
                    E = new DXFPositionable();
                    break;
                default:
                    E = new DXFDummy(this.FValue);
                    break;

            }
            E.Converter = this;
            return E;
        }

        public void Next()
        {
            int code;
            bool success = Int32.TryParse(this.FStream.ReadLine(), out code);
            if (success)
                this.FCode = code;
            else
                this.FCode = -6; // invalid DXF code

            this.FValue = this.FStream.ReadLine();
        }

        public bool HasNext()
        {
            if (this.FStream == null)
                return false;
            if (this.FStream.Peek() < 0)
                return false;
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
                catch(Exception ex)
                {
                    // don't care
                    string message = ex.Message;
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================= UTILITY METHODS ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public float FloatValue()
        {
            double f;
            bool success = Double.TryParse(this.FValue, NumberStyles.Float, this.N, out f);

            if (success)
                return (float)f;
            else
                return 0f;
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

        public byte ByteValue()
        {
            byte b;
            bool success = Byte.TryParse(this.FValue, out b);
            if (success)
                return b;
            else
                return 0;
        }


        public DXFLayer RetrieveLayer(string _name)
        {
            DXFLayer result = null;
            if (_name == null)
                return null;

            string queryName = _name.ToLower();
            foreach(var layer in this.FLayers.ecEntities)
            {
                string layerName = layer.EntName.ToLower();
                if (queryName.Equals(layerName))
                {
                    DXFLayer dxf_layer = layer as DXFLayer;
                    result = dxf_layer;
                    break;
                }       
            }

            // in case the posLayer could not be found
            if (result == null)
            {
                result = new DXFLayer(_name);
                this.FLayers.AddEntity(result);
            }

            return result;
        }

        public DXFBlock RetrieveBlock(string _name)
        {
            DXFBlock result = null;
            if (_name == null)
                return null;

            foreach (var block in this.FBlocks.ecEntities)
            {
                if (_name.Equals(block.EntName))
                {
                    DXFBlock dxf_block = block as DXFBlock;
                    result = dxf_block;
                    break;
                }
            }

            return result;
        }

        public override string ToString()
        {
            string dxfContent = "LAYERS:\n";
            int n = this.FLayers.ecEntities.Count;
            int i;
            if (this.FLayers != null && n > 0)
            {
                for (i = 0; i < n; i++ )
                {
                    dxfContent += "_[[" + i + "]]_ " + this.FLayers.ecEntities[i].ToString() + "\n";
                }
            }
            dxfContent += "\nBLOCKS:\n";
            n = this.FBlocks.ecEntities.Count;
            if (this.FBlocks != null && n > 0)
            {
                for (i = 0; i < n; i++ )
                {
                    dxfContent += "_[[" + i + "]]_ " + this.FBlocks.ecEntities[i].ToString() + "\n";
                }
            }
            dxfContent += "\nENTITIES:\n";
            n = this.FEntities.ecEntities.Count;
            if (this.FEntities != null && n > 0)
            {
                for (i = 0; i < n; i++ )
                {
                    dxfContent += "_[[" + i + "]]_ " + this.FEntities.ecEntities[i].ToString() + "\n\n";
                }
            }
            return dxfContent;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ====================================== GEOMETRY CONVERSION METHODS ===================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public List<DXFLayer> GetLayers()
        {
            List<DXFLayer> layers = new List<DXFLayer>();

            if (this.FLayers == null)
                return layers;

            int n = this.FLayers.ecEntities.Count;
            for(int i = 0; i < n; i++)
            {
                DXFEntity e = this.FLayers.ecEntities[i];
                DXFLayer layer = e as DXFLayer;
                if (layer != null)
                    layers.Add(layer);
            }

            return layers;
        }

        public List<DXFGeometry> ConvertToDrawable()
        {
            List<DXFGeometry> geometry = new List<DXFGeometry>();

            if (this.FEntities == null)
                return geometry;
            
            int n = this.FEntities.ecEntities.Count;          
            for (int i = 0; i < n; i++)
            {
                DXFEntity e = this.FEntities.ecEntities[i];
                Type t = e.GetType();

                DXFGeometry eG = new DXFGeometry();
                List<DXFGeometry> eGs = new List<DXFGeometry>();
                // type deistinction
                if (t == typeof(DXFInsert))
                {
                    DXFInsert ei = e as DXFInsert;
                    eGs = ei.ConvertToDrawables();
                    if (eGs.Count > 0)
                        geometry.AddRange(eGs);
                }
                else
                {
                    eG = e.ConvertToDrawable();
                    if (!eG.IsEmpty())
                        geometry.Add(eG);
                }

                
            }
            return geometry;
        }

    }
}
