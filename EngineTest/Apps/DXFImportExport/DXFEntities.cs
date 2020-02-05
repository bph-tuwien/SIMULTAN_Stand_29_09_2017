using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Globalization;

namespace DXFImportExport
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================= BASE TYPE: ENTITY ======================================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region DXF_Entity
    public class DXFEntity
    {
        public bool entIsVisible;
        protected bool entHasEntities;
        public string EntName { get; protected set; }
        internal DXFConverter Converter { get; set; }

        // for printing text
        protected NumberFormatInfo nfi;            

        public DXFEntity()
        {
            this.EntName = null;
            this.entIsVisible = true;
            this.entHasEntities = false;
            this.nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = " ";
        }

        public virtual void Invoke(DXFConverterProc _proc, DXFIterate _params)
        {
            _proc(this);
        }
        public virtual void ReadPoperty()
        {
            switch(this.Converter.FCode)
            {
                case 0:
                    // set the entity type (i.e. LINE) as name
                    // CANNOT BE REACHED!
                    this.EntName = this.Converter.FValue;
                    break;
                case 60:
                    int vis = this.Converter.IntValue();
                    if (vis == 0) // invisible
                        this.entIsVisible &= false;
                    break;
                case 67:
                    int space = this.Converter.IntValue();
                    if (space == 1) // paper space
                        this.entIsVisible &= false;
                    break;
            }
        }        
        public virtual void OnLoaded() { }

        public virtual bool AddEntity(DXFEntity e)
        {
            return false;
        }

        public override string ToString()
        {
            return this.GetType().ToString();
        }

        public virtual DXFGeometry ConvertToDrawable(string _note = "")
        {
            return new DXFGeometry();
        }

        public void ParseNext() 
        {
            // start parsing next entity
            this.ReadProperties();
            // if it contains entities itself, parse them next
            if (this.entHasEntities)
                this.ReadEntities();
        }

        protected void ReadProperties()
        {
            while(this.Converter.HasNext())
            {
                this.Converter.Next();
                switch(this.Converter.FCode)
                {
                    case 0:
                        // reached next entity
                        return;
                    default:
                        // otherwise continue parsing
                        this.ReadPoperty();
                        break;
                }
            }
        }

        protected void ReadEntities()
        {
            // debug
            DXFInsert test = this as DXFInsert;
            int test2;
            if (test != null)
                test2 = test.ecEntities.Count;
            DXFEntity e;
            do
            {
                if (this.Converter.FValue == "EOF")
                {
                    // end of file
                    this.Converter.ReleaseRessources();
                    return;
                }
                e = this.Converter.CreateEntity();
                if (e == null)
                {
                    // reached end of complex entity
                    this.Converter.Next();
                    break;
                }
                // -----------------------------------------------
                // check for special case: Insert w/o attributes
                if (CurrentEntityInsertWoAttribs(e))
                {
                    // reached end of Insert w/o attributes
                    // this.Converter.Next();
                    break;
                }
                // -----------------------------------------------
                e.ParseNext();
                if(e.GetType().IsSubclassOf(typeof(DXFEntity)))
                {
                    // complete parsing
                    e.OnLoaded();
                    // add to list of entities of this entity
                    this.AddEntity(e);
                }
            }
            while (this.Converter.HasNext());
        }

        private bool CurrentEntityInsertWoAttribs(DXFEntity e)
        {
            DXFInsert thisAsInsert = this as DXFInsert;
            if (thisAsInsert != null)
            {
                DXFAttribute eAsAttrib = e as DXFAttribute;
                if (eAsAttrib == null)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }    
        }
        
    }
    #endregion

    #region DXF_Dummy_Entity
    public class DXFDummy : DXFEntity
    {
        public DXFDummy()
        {
            this.EntName = null;
            this.entIsVisible = false;
            this.entHasEntities = false;
        }
        public DXFDummy(string _name)
        {
            this.EntName = _name;
            this.entIsVisible = false;
            this.entHasEntities = false;
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            if (this.EntName != null)
                dxfS += "[" + this.EntName + "]";

            return dxfS;
        }
    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================ SIMPLE ENTITY TYPES ======================================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ------------------------------------------------- DXFLayer --------------------------------------------- //

    #region DXF_Layer
    public class DXFLayer : DXFEntity
    {
        private static int counterAutomatic = 0;

        protected DXFColor layerColor;
        protected DXFColor layerTrueColor;
        public DXFColor LayerColor 
        { 
            get
            {
                if (this.layerTrueColor != DXFColor.clNone)
                    return this.layerTrueColor;
                else
                    return this.layerColor;
            }
        }

        public DXFLayer()
            :base()
        {
            this.layerColor = DXFColor.clNone;
            this.layerTrueColor = DXFColor.clNone;
        }
        public DXFLayer(string _name)
            : base()
        {
            if (_name != null && _name.Count() > 0)
                this.EntName = _name;
            else
            {
                DXFLayer.counterAutomatic++;
                this.EntName = "$" + DXFLayer.counterAutomatic.ToString() + "_autoGen";
            }
            this.layerColor = DXFColor.clNone;
            this.layerTrueColor = DXFColor.clNone;
        }

        public override void ReadPoperty()
        {
            base.ReadPoperty();
            switch (this.Converter.FCode)
            {     
                case 2:
                    this.EntName = this.Converter.FValue;
                    break;
                case 62:
                    this.layerColor = DXFColor.Index2DXFColor(this.Converter.IntValue());
                    break;
                case 420:
                    this.layerTrueColor = DXFColor.TrueColor2DXFColor(this.Converter.IntValue());
                    break;
                case 70:
                    byte flags = this.Converter.ByteValue();
                    if (flags == 1) // frozen posLayer
                        this.entIsVisible = false;
                    break;  
            }
        }

        public override void OnLoaded()
        {
            // set the layerColor of a frozen posLayer
            if (!this.entIsVisible)
            {
                this.layerColor = DXFColor.clNone;
                this.layerTrueColor = DXFColor.clNone;
            }
        }

        public override string ToString()
        {
            string dxfL = base.ToString();
            dxfL += " " + this.EntName + " |color: " + this.LayerColor.ToString() + " |visible: " + this.entIsVisible.ToString();
            return dxfL;
        }
    }
    #endregion

    // --------------------------------------------- DXFPositionable ------------------------------------------ //

    #region DXF_Positionable
    public class DXFPositionable : DXFEntity
    {
        protected DXFLayer posLayer;
        protected DXFColor posColor;
        private DXFColor posTrueColor;
        protected Point3D posBasePos;
        internal Point3D posPosStart;
        protected Vector3D posPlaneNormal;
        public DXFColor Color
        {
            get
            { return this.posColor; }
        }

        public DXFPositionable()
            : base()
        {
            this.posLayer = new DXFLayer();
            this.posColor = DXFColor.clByLayer;
            this.posTrueColor = DXFColor.clNone;
            this.posBasePos = new Point3D(0, 0, 0);
            this.posPosStart = new Point3D(0, 0, 0);
            this.posPlaneNormal = new Vector3D(0, 0, 1);
        }

        public override void ReadPoperty()
        {
            base.ReadPoperty(); // codes 0(type), 60+67(visibility)
            switch(this.Converter.FCode)
            {
                case 8:
                    this.posLayer = this.Converter.RetrieveLayer(this.Converter.FValue);
                    break;
                case 62:
                    this.posColor = DXFColor.Index2DXFColor(this.Converter.IntValue());
                    break;
                case 420:
                    this.posTrueColor = DXFColor.TrueColor2DXFColor(this.Converter.IntValue());
                    break;
                case 10:
                    this.posPosStart.X = this.Converter.FloatValue();
                    break;
                case 20:
                    this.posPosStart.Y = this.Converter.FloatValue();
                    break;
                case 30:
                    this.posPosStart.Z = this.Converter.FloatValue();
                    break;
                case 210:
                    this.posPlaneNormal.X = this.Converter.FloatValue();
                    break;
                case 220:
                    this.posPlaneNormal.Y = this.Converter.FloatValue();
                    break;
                case 230:
                    this.posPlaneNormal.Z = this.Converter.FloatValue();
                    break;
            }
        }

        public override void OnLoaded()
        {
            // set the layerColor
            if (this.posColor == DXFColor.clByLayer)
                this.posColor = this.posLayer.LayerColor;
            if (this.posTrueColor != DXFColor.clNone)
                this.posColor = this.posTrueColor;
            
            // set the layerColor dependent on visibility
            if (!this.entIsVisible)
                this.posColor = DXFColor.clNone;
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            dxfS += " |layer: " + this.posLayer.EntName + " |color: " + this.posColor.ToString() + "\n";
            dxfS += " |posS: " + Extensions.Point3DToString(this.posPosStart);
            dxfS += " |posN: " + Extensions.Vector3DToString(this.posPlaneNormal);
            return dxfS;
        }

        public override DXFGeometry ConvertToDrawable(string _note = "")
        {
            DXFGeometry drawable = new DXFGeometry();
            drawable.Color = this.Color;
            drawable.Name = _note + " Point";
            drawable.AddVertex(this.posPosStart, false, 1f, this.posPosStart, this.posPlaneNormal);
            return drawable;
        }
    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // =================== POSITIONABLE ENTITIES - I. E. GEOMETRIC OBJECT REPRESENTATIONS ===================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ------------------------------------------------ DXFLine ----------------------------------------------- //

    #region DXF_Line
    public class DXFLine : DXFPositionable
    {
        protected Point3D linePosEnd;

        public DXFLine()
            : base()
        {
            this.linePosEnd = new Point3D(0, 0, 0);
        }

        public override void ReadPoperty()
        {
            switch(this.Converter.FCode)
            {
                case 11:
                    this.linePosEnd.X = this.Converter.FloatValue();
                    break;
                case 21:
                    this.linePosEnd.Y = this.Converter.FloatValue();
                    break;
                case 31:
                    this.linePosEnd.Z = this.Converter.FloatValue();
                    break;
                default:
                    // codes 0(type), 60+67(visibility), 8(posLayer), 62+420(layerColor), 10,20,30 (posPosStart)
                    // 210,220,230 (posPlaneNormal)
                    base.ReadPoperty();
                    break;
            }
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            dxfS += " |posE: " + Extensions.Point3DToString(this.linePosEnd);
            return dxfS;
        }

        public override DXFGeometry ConvertToDrawable(string _note = "")
        {
            DXFGeometry drawable = new DXFGeometry();
            drawable.LayerName = this.posLayer.EntName;
            drawable.Color = this.Color;
            drawable.Name = _note + " Line";
            drawable.AddVertex(this.posPosStart, false, 0.25f);
            drawable.AddVertex(this.linePosEnd, false, 0.25f);
            return drawable;
        }
    }
    #endregion

    // --------------------------------------- DXFCircle (CIRCLE and ARC)-------------------------------------- //

    #region DXF_Circle
    public class DXFCircle : DXFPositionable
    {
        protected float circRadius;
        protected float circAngleStart;
        protected float circAngleEnd;

        public DXFCircle()
            : base()
        {
            this.circRadius = 1f;
            this.circAngleStart = 0;
            this.circAngleEnd = 360f; // (float)(2 * Math.PI);
        }

        public override void ReadPoperty()
        {
            base.ReadPoperty();
            switch(this.Converter.FCode)
            {
                case 40:
                    this.circRadius = this.Converter.FloatValue();
                    break;
                case 50:
                    this.circAngleStart = this.Converter.FloatValue();
                    break;
                case 51:
                    this.circAngleEnd = this.Converter.FloatValue();
                    break;
                default:
                    base.ReadPoperty();
                    break;
            }
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            dxfS += " |radius: " + String.Format(this.nfi, "{0:F2}", this.circRadius) +
                    " |angS: " + String.Format(this.nfi, "{0:F2}", this.circAngleStart) +
                    " |angE: " + String.Format(this.nfi, "{0:F2}", this.circAngleEnd);
            return dxfS;
        }

        public override DXFGeometry ConvertToDrawable(string _note = "")
        {
            DXFGeometry drawable = new DXFGeometry();
            drawable.LayerName = this.posLayer.EntName;
            drawable.Color = this.Color;
            drawable.Name = _note + "Circle Arc";
            drawable.AddCircleArc(this.posPosStart, this.posPlaneNormal, 
                this.circRadius, this.circAngleStart, this.circAngleEnd, 0.25f, 18);
            return drawable;
        }
    }
    #endregion

    // ---------------------------------------------- DXFEllipse ---------------------------------------------- //

    #region DXF_Ellipse
    public class DXFEllipse : DXFPositionable
    {
        protected Vector3D ellPosMajAxisEnd;
        protected float ellMin2majRatio;
        protected float ellParamStart;
        protected float ellParamEnd;        

        private float a; // for drawing the ellipse
        private float b; // for drawing the ellipse

        public DXFEllipse()
            : base()
        {
            this.ellPosMajAxisEnd = new Vector3D(0, 0, 0);
            this.ellMin2majRatio = 1f;
            this.ellParamStart = 0f;
            this.ellParamEnd = (float)(2 * Math.PI);            
        }

        public override void ReadPoperty()
        {
            switch(this.Converter.FCode)
            {
                case 11:
                    // realtive to posPosStart
                    this.ellPosMajAxisEnd.X = this.Converter.FloatValue();
                    break;
                case 21:
                    // realtive to posPosStart
                    this.ellPosMajAxisEnd.Y = this.Converter.FloatValue();
                    break;
                case 31:
                    // realtive to posPosStart
                    this.ellPosMajAxisEnd.Z = this.Converter.FloatValue();
                    break;
                case 40:
                    this.ellMin2majRatio = this.Converter.FloatValue();
                    break;
                case 41:
                    this.ellParamStart = this.Converter.FloatValue();
                    break;
                case 42:
                    this.ellParamEnd = this.Converter.FloatValue();
                    break;
                default:
                    base.ReadPoperty();
                    break;
            }
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            // calculate the coefficients for drawing the ellipse
            double majHalfLen = Math.Sqrt(  this.ellPosMajAxisEnd.X * this.ellPosMajAxisEnd.X + 
                                            this.ellPosMajAxisEnd.Y * this.ellPosMajAxisEnd.Y + 
                                            this.ellPosMajAxisEnd.Z * this.ellPosMajAxisEnd.Z);
            this.a = (float)majHalfLen * (-1f);
            this.b = this.a * this.ellMin2majRatio;

        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            dxfS += " |a: " + String.Format(this.nfi, "{0:F2}", this.a) +
                    " |b: " + String.Format(this.nfi, "{0:F2}", this.b) +
                    " |uS: " + String.Format(this.nfi, "{0:F2}", this.ellParamStart) +
                    " |uE: " + String.Format(this.nfi, "{0:F2}", this.ellParamEnd);
            return dxfS;
        }

        public override DXFGeometry ConvertToDrawable(string _note = "")
        {
            DXFGeometry drawable = new DXFGeometry();
            drawable.LayerName = this.posLayer.EntName;
            drawable.Color = this.Color;
            drawable.Name = _note + " Ellipse Arc";
            drawable.AddEllipseArc(this.posPosStart, this.posPlaneNormal,
                                   this.a, this.b, this.ellPosMajAxisEnd, 
                                   this.ellParamStart, this.ellParamEnd, 0.25f, 24);
            return drawable;
        }

    }
    #endregion

    // ---------------------------------------------- DXFLWPolyLine ------------------------------------------- //

    #region DXF_LWpolyLine
    public class DXFLWPolyLine : DXFPositionable
    {
        protected int plNrVertices;
        protected bool plIsClosed;
        protected List<Point3D> plVertices;
        protected List<Point3D> plWidths;

        // for reading plVertices
        private List<bool> readVertices;
        private Point3D vertex;
        private List<bool> readVertexWidths;
        private Point3D width;
        private float widthGlobal;
        private float elevation;

        public DXFLWPolyLine()
            : base()
        {
            this.plNrVertices = 0;
            this.plIsClosed = false;
            this.plVertices = new List<Point3D>();
            this.plWidths = new List<Point3D>();

            this.readVertices = new List<bool>();
            this.vertex = new Point3D(0, 0, 0);
            this.readVertexWidths = new List<bool>();
            this.width = new Point3D(0, 0, 0);
            this.widthGlobal = 0f;
            this.elevation = 0f;
        }

        public override void ReadPoperty()
        {
            int nV = this.readVertices.Count;
            int nVW = this.readVertexWidths.Count;
            switch (this.Converter.FCode)
            {
                case 10:
                    this.vertex.X = this.Converter.FloatValue();
                    this.ProcessVertex(nV);
                    break;
                case 20:
                    this.vertex.Y = this.Converter.FloatValue();
                    this.ProcessVertex(nV);
                    break;
                case 38:
                    this.elevation = this.Converter.FloatValue();
                    break;
                case 40:
                    this.width.X = this.Converter.FloatValue();
                    this.ProcessVertexWidth(nVW);
                    break;
                case 41:
                    this.width.Y = this.Converter.FloatValue();
                    this.ProcessVertexWidth(nVW);
                    break;
                case 43:
                    this.widthGlobal = this.Converter.FloatValue();
                    break;
                case 70:
                    byte flags = this.Converter.ByteValue();
                    if ((flags & 1) == 1)
                        this.plIsClosed = true;
                    break;
                case 90:
                    // nr plVertices not necessary
                    break;
                default:
                    base.ReadPoperty();
                    break;
            }
        }

        private void ProcessVertex(int _nV)
        {
            if (_nV == 0 || this.readVertices[_nV - 1])
            {
                // starts reading a new vertex
                this.readVertices.Add(false);
                this.plNrVertices++;
            }
            else
            {
                // completes reading a vertex
                this.readVertices[_nV - 1] = true;
                this.plVertices.Add(this.vertex);
                this.vertex = new Point3D(0, 0, 0);
            }
        }

        private void ProcessVertexWidth(int _nVW)
        {
            if (_nVW == 0 || this.readVertexWidths[_nVW - 1])
            {
                // starts reading a new vertex Width
                this.readVertexWidths.Add(false);
            }
            else
            {
                // completes reading a vertex Width
                this.readVertexWidths[_nVW - 1] = true;
                this.plWidths.Add(this.width);
                this.width = new Point3D(0, 0, 0);
            }
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            if (this.elevation != 0)
            {
                for (int i = 0; i < this.plNrVertices; i++)
                {
                    Point3D tmp = this.plVertices[i];
                    tmp.Z = this.elevation;
                    this.plVertices[i] = tmp;
                }
            }
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            for (int i = 0; i < this.plNrVertices; i++ )
            {
                dxfS += "\n" + Extensions.Point3DToString(this.plVertices[i]);
                if (this.plWidths.Count == this.plNrVertices)
                    dxfS += " w: " + Extensions.Point3DToString(this.plWidths[i],2);
            }
            return dxfS;
        }

        public override DXFGeometry ConvertToDrawable(string _note = "")
        {
            DXFGeometry drawable = new DXFGeometry();
            drawable.LayerName = this.posLayer.EntName;
            drawable.Color = this.Color;
            drawable.Name = _note + " Polyline";

            List<Point3D> plVerticesOut = new List<Point3D>(this.plVertices);
            List<Point3D> plWidthsOut = new List<Point3D>(this.plWidths);
            if (this.plIsClosed)
            {
                plVerticesOut.Add(this.plVertices[0]);
                if (this.plWidths.Count > 0)
                    plWidthsOut.Add(this.plWidths[0]);
            }

            if (this.plVertices.Count == this.plWidths.Count)
                drawable.AddLines(plVerticesOut, true, plWidthsOut, this.posPosStart, this.posPlaneNormal);
            else
                drawable.AddLines(plVerticesOut, true, this.widthGlobal, this.posPosStart, this.posPlaneNormal);
            return drawable;
        }

    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ========================= POSITIONABLE ENTITIES - TEXT OBJECT REPRESENTATIONS ========================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ------------------------------------------------- DXFText ---------------------------------------------- //

    #region DXF_Text
    public class DXFText : DXFLine
    {
        protected float txtHeight;
        protected float txtScaleX;
        protected float txtAngle; // in degrees
        protected byte txtFlags;
        protected int txtJustH;
        protected int txtJustV;
        protected string txtContent;

        public DXFText()
            : base()
        {
            this.txtHeight = 1f;
            this.txtScaleX = 1f;
            this.txtAngle = 0f;
            this.txtFlags = 0;
            this.txtJustH = 0;
            this.txtJustV = 0;
            this.txtContent = "";
        }

        public override void ReadPoperty()
        {
            switch(this.Converter.FCode)
            {
                case 1:
                    this.txtContent = this.Converter.FValue;
                    break;
                case 40:
                    this.txtHeight = this.Converter.FloatValue();
                    break;
                case 41:
                    this.txtScaleX = this.Converter.FloatValue();
                    break;
                case 50:
                    this.txtAngle = this.Converter.FloatValue();
                    break;
                case 71:
                    this.txtFlags = this.Converter.ByteValue();
                    break;
                case 72:
                    this.txtJustH = this.Converter.IntValue();
                    break;
                case 73:
                    this.txtJustV = this.Converter.IntValue();
                    break;
                default:
                    // DXFEntity:       codes 0(type), 60+67(visibility)
                    // DXFPositionable: codes 8(posLayer), 62+420(posColor), 10,20,30 (posPosStart), 210,220,230 (posPlaneNormal)
                    // DXFLine:         codes 11,21,31 (linePosEnd)
                    base.ReadPoperty();
                    break;
            }
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            // adjust rotation
            if (this.linePosEnd == new Point3D(0,0,0))
            {
                // take the rotation txtAngle
            }
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            dxfS += "\n_cont: " + this.txtContent +
                " |txtH: " + String.Format(this.nfi, "{0:F2}", this.txtHeight) +
                " |txtScaleX: " + String.Format(this.nfi, "{0:F2}", this.txtScaleX) +
                " |txtAngle: " + String.Format(this.nfi, "{0:F2}", this.txtAngle) +
                " |txtJustH: " + this.txtJustH +
                " |txtJustV: " + this.txtJustV + 
                " |txtFlags: " + this.txtFlags;
            return dxfS;
        }

        public override DXFGeometry ConvertToDrawable(string _note = "")
        {
            DXFGeometry drawable = new DXFGeometry();
            drawable.LayerName = this.posLayer.EntName;
            drawable.Color = this.Color;
            drawable.Name = _note + " Text";

            // assemble a rectangle as placeholder
            List<Point3D> verts = new List<Point3D>();

            Point3D rectBL = this.posPosStart;
            float txtAngRad = this.txtAngle * (float)Math.PI / 180f;
            float txtLen = this.txtContent.Count() * this.txtScaleX;
            Point3D rectBR = rectBL + new Vector3D(1, 0, 0) * Math.Cos(txtAngRad) * txtLen +
                                      new Vector3D(0, 1, 0) * Math.Sin(txtAngRad) * txtLen;

            Point3D rectTR = rectBR + new Vector3D(1, 0, 0) * Math.Sin(txtAngRad) * this.txtHeight -
                                      new Vector3D(0, 1, 0) * Math.Cos(txtAngRad) * this.txtHeight;

            Point3D rectTL = rectBL + new Vector3D(1, 0, 0) * Math.Sin(txtAngRad) * this.txtHeight -
                                      new Vector3D(0, 1, 0) * Math.Cos(txtAngRad) * this.txtHeight;

            // pass geometry
            verts.AddRange(new Point3D[] { rectBL, rectBR, rectTR, rectTL, rectBL});
            drawable.AddLines(verts, true, 0.25f, this.posPosStart, this.posPlaneNormal);

            // scale the text(based on verdana 1.2 regular)
            Matrix3D Mscale = Matrix3D.Identity;
            Mscale.ScaleAt(new Vector3D(this.txtHeight / 1.2f, this.txtHeight / 1.2f, 1), new Point3D(0, 0, 0));
            // rotate
            Matrix3D Mrot = Matrix3D.Identity;
            Mrot.Rotate(new Quaternion(new Vector3D(0, 0, 1), this.txtAngle));
            // pass text with the correct CS
            Vector3D axisX, axisY, axisZ;
            DXFGeometry.CalPlaneCS(this.posPosStart, this.posPlaneNormal, out axisX, out axisY, out axisZ);
            Matrix3D Mcs = new Matrix3D();
            Mcs.M11 = axisX.X; Mcs.M12 = axisX.Y; Mcs.M13 = axisX.Z;
            Mcs.M21 = axisY.X; Mcs.M22 = axisY.Y; Mcs.M23 = axisY.Z;
            Mcs.M31 = axisZ.X; Mcs.M32 = axisZ.Y; Mcs.M33 = axisZ.Z;
            Vector3D oText = rectTL.X * axisX + rectTL.Y * axisY + rectTL.Z * axisZ;
            Mcs.OffsetX = oText.X;
            Mcs.OffsetY = oText.Y;
            Mcs.OffsetZ = oText.Z;

            drawable.AddText(this.txtContent, Mscale * Mrot * Mcs);

            return drawable;
        }

    }
    #endregion

    // ------------------------------------------------- DXFMText --------------------------------------------- //

    #region DXF_MText
    public class DXFMText : DXFText
    {
        protected float mtTxtLineSpacing;
        protected bool mtBgShow;
        protected DXFColor mtBgColor;
        protected Rect3D mtBgBox;

        private List<string> contentChunks;
        private string[] mtContentLines;
        private float textBoxHeight;
        private float fillBoxScale;

        public DXFMText()
            : base()
        {
            this.linePosEnd = new Point3D(1, 0, 0);

            this.mtTxtLineSpacing = 1f;
            this.mtBgShow = false;
            this.mtBgColor = DXFColor.clNone;
            this.mtBgBox = new Rect3D(0, 0, 0, 1, 1, 1);

            this.contentChunks = new List<string>();
            this.textBoxHeight = 0f;
            this.fillBoxScale = 1f;
        }

        public override void ReadPoperty()
        {
            switch(this.Converter.FCode)
            {
                case 1:
                    this.contentChunks.Add(this.Converter.FValue);
                    break;
                case 3:
                    this.contentChunks.Add(this.Converter.FValue);
                    break;
                case 41:
                    // override: absolute, not realtive!
                    this.txtScaleX = this.Converter.FloatValue();
                    break;
                case 43:
                    // actual height of the text field
                    this.textBoxHeight = this.Converter.FloatValue();
                    break;
                case 44:
                    // line spacing factor
                    this.mtTxtLineSpacing = this.Converter.FloatValue();
                    break;
                case 45:
                    // fill box scale
                    this.fillBoxScale = this.Converter.FloatValue();
                    break;
                case 71:
                    // override
                    this.txtJustH = this.Converter.IntValue();
                    break;
                case 72:
                    // override
                    this.txtFlags = this.Converter.ByteValue();
                    break;
                case 73:
                    // override: do nothing
                    break;
                case 90:
                    byte flag = this.Converter.ByteValue();
                    if ((flag & 1) == 1)
                        this.mtBgShow = true;
                    break;
                case 63:
                    this.mtBgColor = DXFColor.Index2DXFColor(this.Converter.IntValue());
                    break;
                default:
                    // DXFEntity:       codes 0(type), 60+67(visibility)
                    // DXFPositionable: codes 8(posLayer), 62+420(posColor), 10,20,30 (posPosStart), 210,220,230 (posPlaneNormal)
                    // DXFLine:         codes 11,21,31 (linePosEnd) - HERE X-axis Direction Vector!!!
                    // DXFText:         codes 1(content),40(char height),41(scaleX),50(angle),71(flags),72(justH),73(justV)
                    base.ReadPoperty();
                    break;
            }
            
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            // assemble content
            foreach(var chunk in this.contentChunks)
            {
                this.txtContent += chunk;
            }
            // adjust horizontal and vertical justification
            switch(this.txtJustH)
            {
                case 1:                    
                    this.txtJustV = 3; // TOP
                    this.txtJustH = 0; // LEFT
                    break;
                case 2:
                    this.txtJustV = 3; // TOP
                    this.txtJustH = 1; // CENTER
                    break;
                case 3:
                    this.txtJustV = 3; // TOP
                    this.txtJustH = 2; // RIGHT
                    break;
                case 4:
                    this.txtJustV = 2; // MIDDLE
                    this.txtJustH = 0; // LEFT
                    break;
                case 5:
                    this.txtJustV = 2; // MIDDLE
                    this.txtJustH = 1; // CENTER
                    break;
                case 6:
                    this.txtJustV = 2; // MIDDLE
                    this.txtJustH = 2; // RIGHT
                    break;
                case 7:
                    this.txtJustV = 1; // BOTTOM
                    this.txtJustH = 0; // LEFT
                    break;
                case 8:
                    this.txtJustV = 1; // BOTTOM
                    this.txtJustH = 1; // CENTER
                    break;
                case 9:
                    this.txtJustV = 1; // BOTTOM
                    this.txtJustH = 2; // RIGHT
                    break;
            }

            // extract the actual text

            // remove the curly brackets, if any present
            int txtLen = this.txtContent.Count();
            string clearText;
            if (txtLen > 0 && this.txtContent[0] == '{')
                clearText = this.txtContent.Substring(1, txtLen - 2);
            else
                clearText = this.txtContent;
            // separate lines of text
            string[] lineSeparators = new string[] { "\\P" };
            string[] rawContentLines = clearText.Split(lineSeparators, StringSplitOptions.None);
            // determine the actual geometric length of each text line based on verdana 1.2 regular font
            // and actual char width
            float charW = 0.75f * this.txtHeight / 1.2f;
            // if any line is longer than the text box, break it
            this.mtContentLines = DXFGeometry.FitTextLines(rawContentLines, charW, this.txtScaleX);

            // determine vertical size
            if (this.textBoxHeight <= this.txtHeight)
            {
                int nrLines = this.mtContentLines.Count();
                this.textBoxHeight = nrLines * this.txtHeight + nrLines * this.mtTxtLineSpacing * this.txtHeight * 0.8f;
            }

            // adjust the size of the background box:
            float offsetX = this.txtScaleX * (1f - this.fillBoxScale) * 0.5f;
            float offsetY = this.textBoxHeight * (1f - this.fillBoxScale) * 0.5f;
            this.mtBgBox = new Rect3D(this.posPosStart.X - offsetX, this.posPosStart.Y - offsetY, this.posPosStart.Z,
                                      this.txtScaleX * this.fillBoxScale, this.textBoxHeight * this.fillBoxScale, 1);
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            dxfS += " |n_txtBox: " + this.mtBgShow.ToString() + " " + String.Format(this.nfi, "size: {0:F2}, {1:F2}, {2:F2}, {3:F2}", 
                                    this.mtBgBox.Location.X, this.mtBgBox.Location.Y, this.mtBgBox.SizeX, this.mtBgBox.SizeY) + 
                    " |bgColor: " + this.mtBgColor.ToString();
            return dxfS;
        }

        public override DXFGeometry ConvertToDrawable(string _note = "")
        {
            DXFGeometry drawable = new DXFGeometry();
            drawable.LayerName = this.posLayer.EntName;
            drawable.Color = this.Color;
            drawable.Name = _note + " MText";

            // assemble a rectangle as placeholder
            List<Point3D> verts = new List<Point3D>();

            // calculate ist coordinate system
            Vector3D bottomV = new Vector3D(this.linePosEnd.X, this.linePosEnd.Y, this.linePosEnd.Z);            
            Vector3D normal = this.posPlaneNormal;
            DXFGeometry.NormalizeVector(ref normal);
            Vector3D sideV = Vector3D.CrossProduct(normal, bottomV);
            DXFGeometry.NormalizeVector(ref sideV);

            Point3D rectTL = this.posPosStart;
            Point3D rectBL = rectTL - sideV * this.textBoxHeight;
            Point3D rectBR = rectBL + bottomV * this.txtScaleX;
            Point3D rectTR = rectBR + sideV * this.textBoxHeight;
            
            Matrix3D Mreflect = Matrix3D.Identity;
            if (this.txtFlags == 1)
            {
                // text right to left
                rectTL = this.posPosStart;
                rectBL = rectTL + sideV * this.textBoxHeight;
                rectBR = rectBL - bottomV * this.txtScaleX;
                rectTR = rectBR - sideV * this.textBoxHeight;
                Mreflect.M11 = -1;
                Mreflect.OffsetY = sideV.Y * this.textBoxHeight;
            }
            else if (this.txtFlags == 3)
            {
                // text bottom to top
                rectTL = this.posPosStart;
                rectBL = rectTL + sideV * this.textBoxHeight;
                rectBR = rectBL + bottomV * this.txtScaleX;
                rectTR = rectBR - sideV * this.textBoxHeight;
                Mreflect.M22 = -1;
            }

            // pass geometry
            verts.AddRange(new Point3D[] { rectBL, rectBR, rectTR, rectTL, rectBL });
            drawable.AddLines(verts, true, 0.25f);

            // scale the text(based on verdana 1.2 regular)
            Matrix3D Mscale = Matrix3D.Identity;
            Mscale.ScaleAt(new Vector3D(this.txtHeight / 1.2f, this.txtHeight / 1.2f, 1), new Point3D(0, 0, 0));
            // pass text with the correct CS
            Matrix3D Mcs = new Matrix3D();
            Mcs.M11 = bottomV.X; Mcs.M12 = bottomV.Y; Mcs.M13 = bottomV.Z;
            Mcs.M21 = sideV.X; Mcs.M22 = sideV.Y; Mcs.M23 = sideV.Z;
            Mcs.M31 = normal.X; Mcs.M32 = normal.Y; Mcs.M33 = normal.Z;
            Mcs.OffsetX = rectTL.X - sideV.X * this.txtHeight;
            Mcs.OffsetY = rectTL.Y - sideV.Y * this.txtHeight;
            Mcs.OffsetZ = rectTL.Z - sideV.Z * this.txtHeight;

            drawable.AddText(this.mtContentLines, Mscale * Mreflect * Mcs);
            return drawable;
        }

    }
    #endregion

    // ------------------------------------- DXFAttribute (ATTDEF and ATTRIB) --------------------------------- //

    #region DXF_Attribute
    public class DXFAttribute : DXFText
    {
        private string attrTag;
        private string attrPrompt;
        private bool attrVisible;
        private float attrScaleX;
        private float attrFieldLen;
        private int attrJustV;

        public DXFAttribute()
            : base()
        {
            this.attrTag = "";
            this.attrPrompt = "";
            this.attrVisible = true;
            this.attrScaleX = 1f;
            this.attrFieldLen = 1f;
            this.attrJustV = 0;
        }

        public override void ReadPoperty()
        {
            switch(this.Converter.FCode)
            {
                case 2:
                    this.attrTag = this.Converter.FValue;
                    break;
                case 3:
                    this.attrPrompt = this.Converter.FValue;
                    break;
                case 70:
                    byte flag = this.Converter.ByteValue();
                    if ((flag & 1) == 1)
                        this.attrVisible = false;
                    break;
                case 41:
                    this.attrScaleX = this.Converter.FloatValue();
                    break;
                case 73:
                    this.attrFieldLen = this.Converter.FloatValue();
                    break;
                case 74:
                    this.attrJustV = this.Converter.IntValue();
                    break;
                default:
                    // DXFEntity:       codes 0(type), 60+67(visibility)
                    // DXFPositionable: codes 8(posLayer), 62+420(layerColor), 10,20,30 (posPosStart), 210,220,230 (posPlaneNormal)
                    // DXFLine:         codes 11,21,31 (linePosEnd)
                    // DXFText:         codes 1(content),40(char height),41(scaleX),50(angle),71(flags),72(justH),73(justV)
                    base.ReadPoperty();
                    break;
            }
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            this.EntName += "_" + this.attrTag + "_(" + this.attrPrompt + "):";
            this.entIsVisible &= this.attrVisible;
            this.txtScaleX = this.attrScaleX;
            this.txtJustV = this.attrJustV;
            // this.txtContent = this.attrTag + ": " + this.txtContent;
        }
    }
    #endregion


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ========================================= COLLECTIONS OF ENTITIES ====================================== //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // -------------------------------------------- DXFEntityContainer ---------------------------------------- //

    #region DXF_Entity_Container
    public class DXFEntityContainer : DXFPositionable
    {
        internal List<DXFEntity> ecEntities;

        public DXFEntityContainer()
            : base()
        {
            this.entHasEntities = true;
            this.ecEntities = new List<DXFEntity>();
        }

        public override void ReadPoperty()
        {
            switch(this.Converter.FCode)
            { 
                case 2:
                    this.EntName = this.Converter.FValue;
                    break;
                default:
                    // DXFEntity:       codes 0(type), 60+67(visibility)
                    // DXFPositionable: codes 8(posLayer), 62+420(posColor), 10,20,30 (posPosStart), 210,220,230 (posPlaneNormal)
                    base.ReadPoperty();
                    break;
            }
        }

        public override bool AddEntity(DXFEntity e)
        {
            if (e != null)
                this.ecEntities.Add(e);
            return (e != null);
        }

        public void Iterate(DXFConverterProc _proc, DXFIterate _params)
        {
            if (_proc == null || _params == null)
                return;

            foreach(DXFEntity e in this.ecEntities)
            {
                e.Invoke(_proc, _params);
            }
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            if (this.EntName != null && this.EntName.Count() > 0)
                dxfS += ": " + this.EntName;
            int n = this.ecEntities.Count;
            dxfS += " has " + n.ToString() + " entities:\n";
            for (int i = 0; i < n; i++ )
            {
                dxfS += "_[ " + i + "]_" + this.ecEntities[i].ToString() + "\n";
            }
            dxfS += "\n";
            return dxfS;
        }

    }
    #endregion

    // ------------------------------------------------ DXFSection -------------------------------------------- //

    #region DXF_Section
    public class DXFSection : DXFEntityContainer
    {
        public override void ReadPoperty()
        {
            if ((this.EntName == null) && (this.Converter.FCode == 2))
            {
                this.EntName = this.Converter.FValue;
            }
            switch (this.EntName)
            {
                case "BLOCKS":
                    this.Converter.FBlocks = this;
                    break;
                case "ENTITIES":
                    this.Converter.FEntities = this;
                    break;
            }
        }
    }
    #endregion

    // ------------------------------------------------- DXFTable --------------------------------------------- //
    // used as a Layer-table

    #region DXF_Table
    public class DXFTable : DXFEntityContainer
    {
        public override void ReadPoperty()
        {
            if ((this.EntName == null) && (this.Converter.FCode == 2))
            {
                this.EntName = this.Converter.FValue.ToUpper();
            }
            if (this.EntName == "LAYER")
                this.Converter.FLayers = this;
        }
    }
    #endregion

    // ------------------------------------------------- DXFBlock --------------------------------------------- //

    #region DXF_Block
    public class DXFBlock : DXFEntityContainer
    {
        private string blkDescr;

        public DXFBlock()
            : base()
        {
            this.blkDescr = "";
        }

        public override void ReadPoperty()
        {
            switch(this.Converter.FCode)
            {
                case 4:
                    this.blkDescr = this.Converter.FValue;
                    break;
                default:
                    // DXFEntity:           codes 0(type), 60+67(visibility)
                    // DXFPositionable:     codes 8(posLayer), 62+420(posColor), 10,20,30 (posPosStart), 210,220,230 (posPlaneNormal)
                    // DXFEntityContainer:  codes 2 (EntName)
                    base.ReadPoperty();
                    break;
            }
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            // prevents the block from being found!
            //if (this.blkDescr.Count() > 0)
            //    this.EntName += ": " + this.blkDescr;
        }
    }
    #endregion

    // ------------------------------------------------ DXFInsert --------------------------------------------- //

    #region DXF_Insert
    public class DXFInsert : DXFEntityContainer
    {
        protected static int NR_INSERTS = 0;

        protected int insNr;
        protected DXFBlock insBlock;
        protected Point3D insScale;
        protected float insRotation;
        protected Point3D insSpacing;
        protected int insNrRows;
        protected int insNrColumns;

        public DXFInsert()
            : base()
        {
            this.insNr = (++DXFInsert.NR_INSERTS);
            this.insScale = new Point3D(1, 1, 1);
            this.insRotation = 0f;
            this.insSpacing = new Point3D(0, 0, 0);
            this.insNrRows = 1;
            this.insNrColumns = 1;
        }

        public override void ReadPoperty()
        {
            switch(this.Converter.FCode)
            {
                case 41:
                    this.insScale.X = this.Converter.FloatValue();
                    break;
                case 42:
                    this.insScale.Y = this.Converter.FloatValue();
                    break;
                case 43:
                    this.insScale.Z = this.Converter.FloatValue();
                    break;
                case 44:
                    this.insSpacing.X = this.Converter.FloatValue();
                    break;
                case 45:
                    this.insSpacing.Y = this.Converter.FloatValue();
                    break;
                case 50:
                    this.insRotation = this.Converter.FloatValue();
                    break;
                case 70:
                    this.insNrColumns = this.Converter.IntValue();
                    break;
                case 71:
                    this.insNrRows = this.Converter.IntValue();
                    break;
                default:
                    // DXFEntity:           codes 0(type), 60+67(visibility)
                    // DXFPositionable:     codes 8(posLayer), 62+420(posColor), 10,20,30 (posPosStart), 210,220,230 (posPlaneNormal)
                    // DXFEntityContainer:  codes 2(EntName)
                    base.ReadPoperty();
                    break;
            }
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            // look for the block definiton
            this.insBlock = this.Converter.RetrieveBlock(this.EntName);
        }

        public override string ToString()
        {
            string dxfS = base.ToString();
            dxfS += " |scale: " + Extensions.Point3DToString(this.insScale) +
                    " |rotation: " + String.Format(this.nfi, "{0:F2}", this.insRotation) +
                    " |spacing: " + Extensions.Point3DToString(this.insSpacing, 2) +
                    " |nr rows: " + this.insNrRows + " |nr cols: " + this.insNrColumns + "\n";
            if (this.insBlock != null)
                dxfS += "block\n" + "[\n" + this.insBlock.ToString() + "\n]";

            return dxfS;
        }

        public List<DXFGeometry> ConvertToDrawables(string _note = "")
        {
            List<DXFGeometry> geometry = new List<DXFGeometry>();

            if (this.insBlock == null)
                return geometry;

            // calculate instance transforms
            List<Matrix3D> transfs = DXFGeometry.CalInstanceTransforms(this.posPosStart, this.posPlaneNormal, 
                                        this.insScale, this.insRotation,
                                        this.insNrRows, (float)this.insSpacing.Y, this.insNrColumns, (float)this.insSpacing.X);

            // add Attributes
            foreach(var e in this.ecEntities)
            {
                DXFGeometry eG = e.ConvertToDrawable("Block '" + this.insBlock.EntName + "'" + this.insNr.ToString() + ":");
                if(!eG.IsEmpty())
                    geometry.Add(eG);
            }

            // add block components
            int n = this.insBlock.ecEntities.Count;
            for (int i = 0; i < n; i++ )
            {
                DXFEntity e = this.insBlock.ecEntities[i];
                Type t = e.GetType();
                if (t == typeof(DXFAttribute))
                    continue;
                DXFPositionable eAsP = e as DXFPositionable;
                if (eAsP == null)
                    continue;

                DXFColor eC = eAsP.Color;
                if (eC == DXFColor.clByBlock)
                    eC = this.Color;

                DXFGeometry eG = e.ConvertToDrawable("Block '" + this.insBlock.EntName + "'" + this.insNr.ToString() + ":");
                eG.Color = eC;
                if (!eG.IsEmpty())
                {
                    // transform by each matrix and add
                    foreach (Matrix3D tr in transfs)
                    {
                        DXFGeometry eGtmp = new DXFGeometry(eG);
                        eGtmp.Transform(tr);
                        geometry.Add(eGtmp);
                    }
                }
            }

            return geometry;
        }

        
    }
    #endregion

    // ----------------------------------------------- DXFPolyline -------------------------------------------- //

    #region DXF_Polyline
    public class DXFPolyLine : DXFEntityContainer
    {
        protected bool plIsClosed;

        public DXFPolyLine()
            : base()
        {
            this.plIsClosed = false;
        }
        public override void ReadPoperty()
        {
            switch (this.Converter.FCode)
            {
                case 70:
                    byte flag = this.Converter.ByteValue();
                    if ((flag & 1) == 1)
                        this.plIsClosed = true;
                    break;
                default:
                    // DXFEntity:           codes 0(type), 60+67(visibility)
                    // DXFPositionable:     codes 8(posLayer), 62+420(posColor), 10,20,30 (posPosStart), 210,220,230 (posPlaneNormal)
                    // DXFEntityContainer:  codes 2 (EntName)
                    base.ReadPoperty();
                    break;
            }
        }

        public override DXFGeometry ConvertToDrawable(string _note = "")
        {
            DXFGeometry drawable = new DXFGeometry();
            drawable.LayerName = this.posLayer.EntName;
            drawable.Color = this.Color;
            drawable.Name = _note + " 3D Polyline";

            int n = this.ecEntities.Count;
            List<Point3D> lines = new List<Point3D>(n);
            for (int i = 0; i < n; i++)
            {
                DXFPositionable p = this.ecEntities[i] as DXFPositionable;
                if (p != null)
                    lines.Add(p.posPosStart);
            }
            drawable.AddLines(lines, true, 1f, this.posPosStart, this.posPlaneNormal);
            return drawable;
        }
    }
    #endregion
}
