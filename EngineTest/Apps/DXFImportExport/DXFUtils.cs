using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows.Media.Media3D;

namespace DXFImportExport
{
    public class DXFColor
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================== STATIC COLOR DEFINITIONS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static readonly float COMP_TOLERANCE = 0.0001f;
        public static readonly DXFColor clByLayer = new DXFColor(1f, 0.88f);
        public static readonly DXFColor clByBlock = new DXFColor(1f, 0.82f);       
        public static readonly DXFColor clNone = new DXFColor(1f, 0.75f);

        public static readonly DXFColor[] IndexColors_FirstTen = new DXFColor[]
        {
            DXFColor.clByBlock,                     // BY BLOCK
            new DXFColor(1f, 0f, 0f, 1f),           // RED
            new DXFColor(1f, 1f, 0f, 1f),           // YELLOW
            new DXFColor(0f, 1f, 0f, 1f),           // GREEN
            new DXFColor(0f, 1f, 1f, 1f),           // CYAN
            new DXFColor(0f, 0f, 1f, 1f),           // BLUE
            new DXFColor(1f, 0f, 1f, 1f),           // MAGENTA
            DXFColor.clNone,                        // WHITE
            new DXFColor(0.25f, 0.25f, 0.25f, 1f),  // DARK GREY
            new DXFColor(0.50f, 0.50f, 0.50f, 1f)   // MIDDLE GREY
        };

        public static readonly DXFColor[] IndexColors_LastSix = new DXFColor[]
        {
            new DXFColor(0.20f, 0.20f, 0.20f, 1f),
            new DXFColor(0.31f, 0.31f, 0.31f, 1f),
            new DXFColor(0.41f, 0.41f, 0.41f, 1f),
            new DXFColor(0.51f, 0.51f, 0.51f, 1f),
            new DXFColor(0.75f, 0.75f, 0.75f, 1f),
            new DXFColor(1f)   // WHITE
        };

        public static readonly DXFColor[] IndexColors_10_249;
        public static readonly int HueStep = 15;
        public static readonly int[] Saturation = new int[] { 100, 100, 100, 34, 100, 34, 100, 33, 100, 33 };
        public static readonly int[] Luminance  = new int[] {  50,  75,  32, 48,  25, 37,  15, 22,   7, 11 };

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== STATIC CONSTRUCTOR ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        static DXFColor()
        {
            IndexColors_10_249 = new DXFColor[240];
            for (int i = 10; i < 250; i++)
            {
                int iHue = ((i / 10) - 1) * DXFColor.HueStep;   // 0 - 345
                int iSaturation = DXFColor.Saturation[i % 10];  // 0 - 100
                int iLuminance = DXFColor.Luminance[i % 10];    // 0 - 100

                float fHue = (float)iHue / 360f;                // 0.0416 - 1.0
                float fSaturation = (float)iSaturation / 100f;  // 0.0 - 1.0
                float fLuminance = (float)iLuminance / 100f;    // 0.0 - 1.0

                float r, g, b;
                HLS2RGB(fHue, fSaturation, fLuminance, out r, out g, out b);

                IndexColors_10_249[i - 10] = new DXFColor(r, g, b, 1f);
            }
        }

        private static void HLS2RGB(float _fHue, float _fSat, float _fLum, out float r, out float g, out float b)
        {
            if (_fSat == 0f)
            {
                r = _fLum;
                g = _fLum;
                b = _fLum;
                return;
            }
            
            float v1, v2;

            if (_fLum < 0.5f)
                v2 = _fLum * (1f + _fSat);
            else
                v2 = (_fLum + _fSat) - (_fSat * _fLum);

            v1 = 2 * _fLum - v2;

            r = GetRGBComponent(v1, v2, _fHue + (1f / 3f));
            g = GetRGBComponent(v1, v2, _fHue);
            b = GetRGBComponent(v1, v2, _fHue - (1f / 3f));

        }

        private static float GetRGBComponent(float _v1, float _v2, float _vh)
        {
            if (_vh < 0f) _vh += 1f;
            if (_vh > 1f) _vh -= 1f;

            if (_vh < (1f / 6f))
                return (_v1 + (_v2 - _v1) * 6f * _vh);
            else if (_vh < 0.5f)
                return _v2;
            else if (_vh < (2f / 3f))
                return (_v1 + (_v2 - _v1) * 6f * ((2f / 3f) - _vh));
            else
                return _v1;

        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================ STATIC METHODS ============================================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static DXFColor Index2DXFColor(int _index)
        {
            if (_index < 0)
                return DXFColor.clNone;
            else if (_index < 10)
                return IndexColors_FirstTen[_index];
            else if (_index < 250)
                return IndexColors_10_249[_index - 10];
            else if (_index < 256)
                return IndexColors_LastSix[_index - 250];
            else if (_index == 256)
                return DXFColor.clByLayer;
            else
                return DXFColor.clNone;
        }

        public static DXFColor TrueColor2DXFColor(int _color)
        {
            int blue = _color & 255;
            int green = (_color >> 8) & 255;
            int red = (_color >> 16) & 255;
            return new DXFColor((float)red, (float)green, (float)blue, 255f, true);
        }

        public static int DXFColor2Index(DXFColor _color)
        {
            if (DXFColor.EqualRGB(_color, new DXFColor(0f,1f), 0.05f))
            {
                return 7;
            }

            for(int i = 0; i < 10; i++)
            {
                if (DXFColor.EqualRGB(_color, DXFColor.IndexColors_FirstTen[i], 0.05f))
                    return i;
            }
            for (int i = 0; i < 6; i++ )
            {
                if (DXFColor.EqualRGB(_color, DXFColor.IndexColors_LastSix[i], 0.05f))
                    return 250 + i;
            }
            for (int i = 10; i < 250; i++ )
            {
                if (DXFColor.EqualRGB(_color, DXFColor.IndexColors_10_249[i - 10], 0.05f))
                    return i;
            }

            return 7; // black
        }

        public static int DXFColor2TrueColor(DXFColor _color)
        {
            int red = (int)(_color.r * 255);
            int green = (int)(_color.g * 255);
            int blue = (int)(_color.b * 255);
            int tc = (195 << 24) + (red << 16) + (green << 8) + blue;
            return tc;
        }

        public static bool operator ==(DXFColor _c1, DXFColor _c2)
        {
            if ((object)_c1 == null && (object)_c2 != null)
                return false;
            if ((object)_c1 != null && (object)_c2 == null)
                return false;
            if ((object)_c1 == null && (object)_c2 == null)
                return true;

            return ((Math.Abs(_c1.R - _c2.R) < DXFColor.COMP_TOLERANCE) &&
                    (Math.Abs(_c1.G - _c2.G) < DXFColor.COMP_TOLERANCE) &&
                    (Math.Abs(_c1.B - _c2.B) < DXFColor.COMP_TOLERANCE) &&
                    (Math.Abs(_c1.A - _c2.A) < DXFColor.COMP_TOLERANCE));
        }

        public static bool operator !=(DXFColor _c1, DXFColor _c2)
        {
            if ((object)_c1 == null && (object)_c2 != null)
                return true;
            if ((object)_c1 != null && (object)_c2 == null)
                return true;
            if ((object)_c1 == null && (object)_c2 == null)
                return false;

            return ((Math.Abs(_c1.R - _c2.R) >= DXFColor.COMP_TOLERANCE) ||
                    (Math.Abs(_c1.G - _c2.G) >= DXFColor.COMP_TOLERANCE) ||
                    (Math.Abs(_c1.B - _c2.B) >= DXFColor.COMP_TOLERANCE) ||
                    (Math.Abs(_c1.A - _c2.A) >= DXFColor.COMP_TOLERANCE));
        }

        public static bool EqualRGB(DXFColor _c1, DXFColor _c2, float _tolerance)
        {
            if ((object)_c1 == null && (object)_c2 != null)
                return false;
            if ((object)_c1 != null && (object)_c2 == null)
                return false;
            if ((object)_c1 == null && (object)_c2 == null)
                return true;

            return ((Math.Abs(_c1.R - _c2.R) < _tolerance) &&
                    (Math.Abs(_c1.G - _c2.G) < _tolerance) &&
                    (Math.Abs(_c1.B - _c2.B) < _tolerance));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================ MEMBER PROPERTIES ========================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private float r;
        public float R 
        {
            get { return this.r; }
            private set
            {
                if (value >= 0f && value <= 1f)
                    this.r = value;
                else if (value < 0f)
                    this.r = 0f;
                else
                    this.r = 1f;
            }
        }

        private float g;
        public float G
        {
            get { return this.g; }
            private set 
            {
                if (value >= 0f && value <= 1f)
                    this.g = value;
                else if (value < 0f)
                    this.g = 0f;
                else
                    this.g = 1f;
            }
        }

        private float b;
        public float B
        {
            get { return this.b; }
            private set
            {
                if (value >= 0f && value <= 1f)
                    this.b = value;
                else if (value < 0f)
                    this.b = 0f;
                else
                    this.b = 1f;
            }
        }

        private float a;
        public float A
        {
            get { return this.a; }
            private set
            {
                if (value >= 0f && value <= 1f)
                    this.a = value;
                else if (value < 0f)
                    this.a = 0f;
                else
                    this.a = 1f;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CONSTRUCTORS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public DXFColor(float _v, bool _as8bit = false)
        {
            float value = _v;
            if (_as8bit)
                value /= 255f;

            this.R = value;
            this.G = value;
            this.B = value;
            this.A = value;
        }

        public DXFColor(float _v, float _a, bool _as8bit = false)
        {
            float colValue = _v;
            float alphaValue = _a;
            if (_as8bit)
            {
                colValue /= 255f;
                alphaValue /= 255f;
            }

            this.R = colValue;
            this.G = colValue;
            this.B = colValue;
            this.A = alphaValue;
        }

        public DXFColor(float _r, float _g, float _b, float _a, bool _as8bit = false)
        {
            float colRValue = _r;
            float colGValue = _g;
            float colBValue = _b;
            float alphaValue = _a;
            if (_as8bit)
            {
                colRValue /= 255f;
                colGValue /= 255f;
                colBValue /= 255f;
                alphaValue /= 255f;
            }

            this.R = colRValue;
            this.G = colGValue;
            this.B = colBValue;
            this.A = alphaValue;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================= METHOD OVERRIDES ========================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Equals(object obj)
        {
            DXFColor col = obj as DXFColor;
            if (col == null)
                return false;

            return ((Math.Abs(this.R - col.R) < DXFColor.COMP_TOLERANCE) &&
                    (Math.Abs(this.G - col.G) < DXFColor.COMP_TOLERANCE) &&
                    (Math.Abs(this.B - col.B) < DXFColor.COMP_TOLERANCE) &&
                    (Math.Abs(this.A - col.A) < DXFColor.COMP_TOLERANCE));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            if (this == DXFColor.clByBlock)
                return "ByBlock";
            else if (this == DXFColor.clByLayer)
                return "ByLayer";
            else if (this == DXFColor.clNone)
                return "None";

            System.Globalization.NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = " ";
            string col = String.Format(nfi, "R {0:F2}", this.R) + " " +
                         String.Format(nfi, "G {0:F2}", this.G) + " " +
                         String.Format(nfi, "B {0:F2}", this.B) + " " +
                         String.Format(nfi, "A {0:F2}", this.A);
            return col;
        }

        public string ToHexString()
        {
            if (this == DXFColor.clByBlock)
                return "ByBlock";
            else if (this == DXFColor.clByLayer)
                return "ByLayer";
            else if (this == DXFColor.clNone)
                return "None";

            int ri = (int)(this.R * 255);
            int gi = (int)(this.G * 255);
            int bi = (int)(this.B * 255);

            string col = "#" + ri.ToString("X2") + gi.ToString("X2") + bi.ToString("X2");
            return col;
        }
    }

    public class Extensions
    {
        public static string Point3DToString(Point3D p, int nrPos = 3)
        {
            if (p == null)
                return "";

            System.Globalization.NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = " ";
            string str = "[";
            switch(nrPos)
            { 
                case 1:
                    str += String.Format(nfi, "{0:F2}", p.X);
                    break;
                case 2:
                    str += String.Format(nfi, "{0:F2}", p.X) + " " +
                           String.Format(nfi, "{0:F2}", p.Y);
                    break;
                default:
                    str += String.Format(nfi, "{0:F2}", p.X) + " " +
                           String.Format(nfi, "{0:F2}", p.Y) + " " +
                           String.Format(nfi, "{0:F2}", p.Z);
                    break;
            }
            str += "]";
            return str;
        }

        public static string Vector3DToString(Vector3D v, int nrPos = 3)
        {
            if (v == null)
                return "";

            Point3D vp = new Point3D(v.X, v.Y, v.Z);
            return Point3DToString(vp, nrPos);
        }
    }
}
