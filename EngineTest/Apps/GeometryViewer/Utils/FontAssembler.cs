using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Point3D = System.Windows.Media.Media3D.Point3D;

using DXFImportExport;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.Utils
{
    public class Line3
    {
        public Vector3 P0 { get; set; }
        public Vector3 P1 { get; set; }

        public Line3(Vector3 _p0, Vector3 _p1)
        {
            this.P0 = _p0;
            this.P1 = _p1;
        }

        public static void TransfromLines(ref List<Line3> _lines, Matrix _m)
        {
            if (_lines == null || _lines.Count < 1 || _m == null)
                return;

            int n = _lines.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = _lines[i].P0;
                Vector4 p04 = p0.ToVector4(1f);
                p04 = Vector4.Transform(p04, _m);

                Vector3 p1 = _lines[i].P1;
                Vector4 p14 = p1.ToVector4(1f);
                p14 = Vector4.Transform(p14, _m);

                _lines[i] = new Line3(p04.ToVector3(), p14.ToVector3());
            }
        }
    }
    class FontAssembler
    {
        public static readonly float VERDANA_12_REGULAR_SEP = 0.25f;
        public static readonly float VERDANA_12_REGULAR_SPACE = 1.0f;
        public static readonly float VERDANA_12_REGULAR_LINE_SPACE = 2.0f;

        public static readonly NumberFormatInfo NFI;

        static FontAssembler()
        {
            NFI = new NumberFormatInfo();
            NFI.NumberDecimalSeparator = ".";
            NFI.NumberGroupSeparator = " ";
        }

        // for defining new characters
        public static string GetCoordsOfFontChar(List<DXFGeometry> _g)
        {
            string definition = null;
            if (_g == null || _g.Count < 1)
                return definition;

            int n = _g.Count;
            definition = "List<Line3> b0 = new List<Line3>();\n";
            for (int i = 0; i < n; i++ )
            {
                DXFGeometry geom = _g[i];
                if (geom.Coords.Count < 1)
                    continue;

                int nrLines = geom.Coords.Count - 1;
                for (int k = 0; k < nrLines; k++)
                {
                    definition += "b0.Add(new Line3(new Vector3(" + 
                                                        String.Format(NFI, "{0:F6}", geom.Coords[k].X) + "f, " +
                                                        String.Format(NFI, "{0:F6}", geom.Coords[k].Y) + "f, " +
                                                        String.Format(NFI, "{0:F6}", geom.Coords[k].Z) + "f), " +
                                                    "new Vector3(" + 
                                                        String.Format(NFI, "{0:F6}", geom.Coords[k + 1].X) + "f, " +
                                                        String.Format(NFI, "{0:F6}", geom.Coords[k + 1].Y) + "f, " +
                                                        String.Format(NFI, "{0:F6}", geom.Coords[k + 1].Z) + "f)));\n";
                }
                definition += "\n";
            }
            definition += "\nreturn b0;\n";

            return definition;
        }

        // _ind is the index of the line in a multi-line text
        public static void ConvertTextToPointChains(string _text, Matrix _textTr, ref List<List<Vector3>> geometry, 
                                                    int _ind, bool _swapYZ = true)
        {
            if (geometry == null)
                geometry = new List<List<Vector3>>();

            List<Point3D> coords0 = new List<Point3D>();
            List<Point3D> coords1 = new List<Point3D>();
            List<int> conn = new List<int>();
            ConvertTextToGeometry(_text, _textTr, ref coords0, ref coords1, ref conn, _ind, _swapYZ);

            int n = coords0.Count;
            if (n < 1)
                return;

            List<Vector3> current = new List<Vector3>();
            current.Add(coords0[0].ToVector3());
            Point3DComparer p3dc = new Point3DComparer(0.0001);
            for(int i = 1; i < n; i++)
            {
                if (p3dc.Equals(coords1[i - 1], coords0[i]))
                {
                    current.Add(coords0[i].ToVector3());
                }
                else
                {
                    current.Add(coords1[i - 1].ToVector3());
                    geometry.Add(current);
                    current = new List<Vector3>();
                    current.Add(coords0[i].ToVector3());
                }
            }

            if(current.Count > 0)
                geometry.Add(current);

        }


        // _ind is the index of the line in a multi-line text
        public static void ConvertTextToGeometry(string _text, Matrix _textTr, 
                                                 ref List<Point3D> coords0, ref List<Point3D> coords1, ref List<int> connected,
                                                 int _ind, bool _swapYZ = true)
        {
            if (_text == null || _text.Count() < 1)
                return;

            if (coords0 == null)
                coords0 = new List<Point3D>();
            if (coords1 == null)
                coords1 = new List<Point3D>();
            if (connected == null)
                connected = new List<int>();

            List<Line3> lines;
            List<bool> linesCI;
            ConvertTextToLines(_text, _ind, out lines, out linesCI);
            if (lines == null || lines.Count < 1)
                return;

            int n = lines.Count;
            for (int i = 0; i < n; i++)
            { 
                // transform by the text matrix:
                Vector3 p0T = TransformByMatrix(lines[i].P0, _textTr);
                Vector3 p1T = TransformByMatrix(lines[i].P1, _textTr);
                if (_swapYZ)
                {
                    coords0.Add(new Point3D(-p0T.X, p0T.Z, p0T.Y));
                    coords1.Add(new Point3D(-p1T.X, p1T.Z, p1T.Y));
                }
                else
                {
                    coords0.Add(p0T.ToPoint3D());
                    coords1.Add(p1T.ToPoint3D());
                }
                if (linesCI[i])
                    connected.Add(connected.Count + 1);
                else
                    connected.Add(-1);
            }
        }

        private static void ConvertTextToLines(string _text, int _ind, out List<Line3> lines_All, out List<bool> lines_Conn)
        {            
            lines_All = new List<Line3>();
            lines_Conn = new List<bool>();
            if (_text == null || _text.Count() < 1)
                return;

            float advanceH = 0f;

            string str_rest = _text;
            int offset = 0; // by how many characters do we advance in the next iteration
            string str_current = "";

            float width_current = 0f;
            List<Line3> lines = null;

            while(str_rest.Count() > 0)
            {
                str_current = str_rest.Substring(0, 1);
                offset = 1;
                switch(str_current)
                {
                    case @"\":
                        if (str_rest.Count() >= 2)
                        {
                            str_current = str_rest.Substring(0, 2);
                            if (str_current == @"\f" || str_current == @"\H" || str_current == @"\W")
                            {
                                // style information: skip it
                                int ind_SC = str_rest.IndexOf(";");
                                if (ind_SC > -1)
                                {
                                    offset = ind_SC + 1;
                                    width_current = -VERDANA_12_REGULAR_SEP;
                                    lines = null;
                                    break;
                                }
                            }
                        }
                        if (str_rest.Count() >= 7)
                        {
                            str_current = str_rest.Substring(0, 3);
                            if (str_current == @"\U+")
                            {
                                // unicode character
                                str_current = str_rest.Substring(1, 6);
                                offset = 7;
                                lines = GetChar(str_current, out width_current);
                                break;
                            }
                        }
                        // backslash character
                        str_current = str_rest.Substring(0, 1);
                        lines = GetChar(str_current, out width_current);
                        break;
                    case "%": 
                        if (str_rest.Count() >= 3)
                        {
                            str_current = str_rest.Substring(0, 2);
                            if (str_current == "%%")
                            {
                                // AutoCAD specific symbols '%%P', '%%C', '%%D'
                                str_current = str_rest.Substring(0, 3);
                                offset = 3;
                                lines = GetChar(str_current, out width_current);
                                break;
                            }
                        }
                        // percent character
                        str_current = str_rest.Substring(0, 1);
                        lines = GetChar(str_current, out width_current);
                        break;
                    case " ":
                        // space
                        lines = new List<Line3>();
                        width_current = VERDANA_12_REGULAR_SPACE;
                        break;
                    default:
                        // regular characers (i.e. 'A')
                        lines = GetChar(str_current, out width_current);
                        break;
                }
                if (lines != null && lines.Count > 0)
                {     
                    // transform lines!
                    Matrix Tr = Matrix.Translation(new Vector3(advanceH, -_ind * VERDANA_12_REGULAR_LINE_SPACE, 0f));
                    Line3.TransfromLines(ref lines, Tr);
                    // save
                    lines_All.AddRange(lines);

                    int nrL = lines.Count;
                    List<bool> connInfo = Enumerable.Repeat(true, nrL - 1).ToList();
                    connInfo.Add(false);
                    lines_Conn.AddRange(connInfo);
                }
                advanceH += width_current + VERDANA_12_REGULAR_SEP;
                str_rest = str_rest.Substring(offset);

            }

        }

        private static Vector3 TransformByMatrix(Vector3 _inV, Matrix _m)
        {
            Vector3 outV = Vector3.Zero;
            if (_inV == null || _m == null)
                return outV;

            Vector4 inV4 = _inV.ToVector4(1f);
            Vector4 outV4 = Vector4.Transform(inV4, _m);
            outV = outV4.ToVector3();

            return outV;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =========================== CHARACTER DEFINITIONS AS COLLECTIONS OF LINES ============================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region choose_char
        public static List<Line3> GetChar(string c, out float charWidth)
        {
            charWidth = 0f;
            if (c == null)
                return null;

            switch(c)
            {
                case " ":
                case "U+0020":
                    charWidth = VERDANA_12_REGULAR_SPACE;
                    return new List<Line3>();
                case "0":
                case "U+0030":
                    charWidth = 0.8286f;
                    return getChar0();
                case "1":
                case "U+0031":
                    charWidth = 0.6502f;
                    return getChar1();
                case "2":
                case "U+0032":
                    charWidth = 0.8113f;
                    return getChar2();
                case "U+F007":
                    // 2 superscript
                    charWidth = 0.5554f;
                    return getChar2_sp();                   
                case "3":
                case "U+0033":
                    charWidth = 0.7902f;
                    return getChar3();                    
                case "U+F008":
                    // 3 superscript
                    charWidth = 0.5260f;
                    return getChar3_sp();                    
                case "4":
                case "U+0034":
                    charWidth = 0.9053f;
                    return getChar4();
                case "5":
                case "U+0035":
                    charWidth = 0.7826f;
                    return getChar5();
                case "6":
                case "U+0036":
                    charWidth = 0.8478f;
                    return getChar6();
                case "7":
                case "U+0037":
                    charWidth = 0.8190f;
                    return getChar7();
                case "8":
                case "U+0038":
                    charWidth = 0.8516f;
                    return getChar8();
                case "9":
                case "U+0039":
                    charWidth = 0.8478f;
                    return getChar9();
                case "U+2220":
                    charWidth = 0.8346f;
                    return getCharAngle();
                case "{":
                case "U+007B":
                    charWidth = 0.7557f;
                    return getCharBraceCurly_Left();
                case "}":
                case "U+007D":
                    charWidth = 0.7557f;
                    return getCharBraceCurly_Right();
                case "[":
                case "U+005B":
                    charWidth = 0.4181f;
                    return getCharBraceRect_Left();
                case "]":
                case "U+005D":
                    charWidth = 0.4181f;
                    return getCharBraceRect_Right();
                case "(":
                case "U+0028":
                    charWidth = 0.4853f;
                    return getCharBraceRound_Left();
                case ")":
                case "U+0029":
                    charWidth = 0.4853f;
                    return getCharBraceRound_Right();
                case "U+2300":
                case "%%C":
                    charWidth = 1.1108f;
                    return getCharDiameter();
                case "=":
                case "U+003D":
                    charWidth = 0.9552f;
                    return getCharEqual();
                case "/":
                case "U+002F":
                    charWidth = 0.7154f;
                    return getCharFractionSlash();
                case "U+2265":
                    charWidth = 0.9286f;
                    return getCharGreaterOrEqual();
                case ">":
                case "U+003E":
                    charWidth = 0.9283f;
                    return getCharGreater();
                case "U+2261":
                    charWidth = 0.5995f;
                    return getCharIdentity();
                case "U+221E":
                    charWidth = 1.3900f;
                    return getCharInfinity();
                case "U+222B":
                    charWidth = 0.8287f;
                    return getCharIntegral();
                case "U+2264":
                    charWidth = 0.9286f;
                    return getCharLessOrEqual();
                case "<":
                case "U+003C":
                    charWidth = 0.9302f;
                    return getCharLess();
                case "-":
                case "U+002D":
                    charWidth = 0.5025f;
                    return getCharMinus();
                case "U+2260":
                    charWidth = 0.9551f;
                    return getCharNotEqual();
                case "+":
                case "U+002B":
                    charWidth = 1.0108f;
                    return getCharPlus();
                case "U+00B1":
                case "%%P":
                    charWidth = 1.0021f;
                    return getCharPlusMinus();
                case "U+2248":
                    charWidth = 0.9551f;
                    return getCharSimilar();
                case "U+221A":
                    charWidth = 1.2930f;
                    return getCharSquareRoot();
                case "&":
                case "U+0026":
                    charWidth = 1.1604f;
                    return getCharAmpersand();
                case "`":
                case "U+0060":
                    charWidth = 0.3414f;
                    return getCharApostrophe();
                case "*":
                case "U+002A":
                    charWidth = 0.7806f;
                    return getCharAsterisk();
                case "@":
                case "U+0040":
                    charWidth = 1.3656f;
                    return getCharAt();
                case @"\":
                case "U+005C":
                    charWidth = 0.7173f;
                    return getCharBackslash();
                case "^":
                case "U+005E":
                    charWidth = 1.0492f;
                    return getCharCircumflex();
                case ":":
                case "U+003A":
                    charWidth = 0.1918f;
                    return getCharColon();
                case ",":
                case "U+002C":
                    charWidth = 0.3452f;
                    return getCharComma();
                case ";":
                case "U+003B":
                    charWidth = 0.3433f;
                    return getCharSemicolon();
                case "°":
                case "U+00B0":
                case "%%D":
                    charWidth = 0.6465f;
                    return getCharDegree();
                case "$":
                case "U+0024":
                    charWidth = 0.8094f;
                    return getCharDollar();
                case "!":
                case "U+0021":
                    charWidth = 0.1803f;
                    return getCharExclMark();
                case "#":
                case "U+0023":
                    charWidth = 1.0357f;
                    return getCharHash();
                case "%":
                case "U+0025":
                    charWidth = 1.5344f;
                    return getCharPercent(); 
                case ".":
                case "U+002E":
                    charWidth = 0.1918f;
                    return getCharPeriod();
                case "?":
                case "U+003F":
                    charWidth = 0.6809f;
                    return getCharQuestionMark();
                case "'":
                case "U+0027":
                    charWidth = 0.1784f;
                    return getCharQuoteSingle();
                case @"""":
                case "U+0022":
                    charWidth = 0.4872f;
                    return getCharQuotes();
                case "~":
                case "U+007E":
                    charWidth = 1.0472f;
                    return getCharTilde();
                case "_":
                case "U+005F":
                    charWidth = 1.0549f;
                    return getCharUnderscore();
                case "|":
                case "U+007C":
                    charWidth = 0.1400f;
                    return getCharVerticalBar();
                case "A":
                case "U+0041":
                    charWidth = 1.0856f;
                    return getCharA();
                case "a":
                case "U+0061":
                    charWidth = 0.7615f;
                    return getCharAs();
                case "B":
                case "U+0042":
                    charWidth = 0.9034f;
                    return getCharB();
                case "b":
                case "U+0062":
                    charWidth = 0.7902f;
                    return getCharBs();
                case "C":
                case "U+0043":
                    charWidth = 0.9916f;
                    return getCharC();
                case "c":
                case "U+0063":
                    charWidth = 0.7289f;
                    return getCharCs();
                case "D":
                case "U+0044":
                    charWidth = 1.0127f;
                    return getCharD();
                case "d":
                case "U+0064":
                    charWidth = 0.7902f;
                    return getCharDs();
                case "E":
                case "U+0045":
                    charWidth = 0.7902f;
                    return getCharE();
                case "e":
                case "U+0065":
                    charWidth = 0.8113f;
                    return getCharEs();
                case "F":
                case "U+0046":
                    charWidth = 0.7653f;
                    return getCharF();
                case "f":
                case "U+0066":
                    charWidth = 0.5773f;
                    return getCharFs();
                case "G":
                case "U+0047":
                    charWidth = 1.0683f;
                    return getCharG();
                case "g":
                case "U+0067":
                    charWidth = 0.7921f;
                    return getCharGs();
                case "H":
                case "U+0048":
                    charWidth = 0.9187f;
                    return getCharH();
                case "h":
                case "U+0068":
                    charWidth = 0.7519f;
                    return getCharHs();
                case "I":
                case "U+0049":
                    charWidth = 0.4738f;
                    return getCharI();
                case "i":
                case "U+0069":
                    charWidth = 0.1707f;
                    return getCharIs();
                case "J":
                case "U+004A":
                    charWidth = 0.5639f;
                    return getCharJ();
                case "j":
                case "U+006A":
                    charWidth = 0.4795f;
                    return getCharJs();
                case "K":
                case "U+004B":
                    charWidth = 0.9590f;
                    return getCharK();
                case "k":
                case "U+006B":
                    charWidth = 0.8094f;
                    return getCharKs();
                case "L":
                case "U+004C":
                    charWidth = 0.7576f;
                    return getCharL();
                case "l":
                case "U+006C":
                    charWidth = 0.1515f;
                    return getCharLs();
                case "M":
                case "U+004D":
                    charWidth = 1.0683f;
                    return getCharM();
                case "m":
                case "U+006D":
                    charWidth = 1.3139f;
                    return getCharMs();
                case "N":
                case "U+004E":
                    charWidth = 0.9149f;
                    return getCharN();
                case "n":
                case "U+006E":
                    charWidth = 0.7519f;
                    return getCharNs();
                case "O":
                case "U+004F":
                    charWidth = 1.1144f;
                    return getCharO();
                case "o":
                case "U+006F":
                    charWidth = 0.8286f;
                    return getCharOs();
                case "P":
                case "U+0050":
                    charWidth = 0.7826f;
                    return getCharP();
                case "p":
                case "U+0070":
                    charWidth = 0.7902f;
                    return getCharPs();
                case "Q":
                case "U+0051":
                    charWidth = 1.1374f;
                    return getCharQ();
                case "q":
                case "U+0071":
                    charWidth = 0.7902f;
                    return getCharQs();
                case "R":
                case "U+0052":
                    charWidth = 0.9916f;
                    return getCharR();
                case "r":
                case "U+0072":
                    charWidth = 0.5601f;
                    return getCharRs();
                case "S":
                case "U+0053":
                    charWidth = 0.9245f;
                    return getCharS();
                case "s":
                case "U+0073":
                    charWidth = 0.7020f;
                    return getCharSs();
                case "T":
                case "U+0054":
                    charWidth = 1.0166f;
                    return getCharT();
                case "t":
                case "U+0074":
                    charWidth = 0.5658f;
                    return getCharTs();
                case "U":
                case "U+0055":
                    charWidth = 0.9207f;
                    return getCharU();
                case "u":
                case "U+0075":
                    charWidth = 0.7519f;
                    return getCharUs();
                case "V":
                case "U+0056":
                    charWidth = 1.0837f;
                    return getCharV();
                case "v":
                case "U+0076":
                    charWidth = 0.8785f;
                    return getCharVs();
                case "W":
                case "U+0057":
                    charWidth = 1.4826f;
                    return getCharW();
                case "w":
                case "U+0077":
                    charWidth = 1.2122f;
                    return getCharWs();
                case "X":
                case "U+0058":
                    charWidth = 1.0204f;
                    return getCharX();
                case "x":
                case "U+0078":
                    charWidth = 0.8785f;
                    return getCharXs();
                case "Y":
                case "U+0059":
                    charWidth = 1.0050f;
                    return getCharY();
                case "y":
                case "U+0079":
                    charWidth = 0.8785f;
                    return getCharYs();
                case "Z":
                case "U+005A":
                    charWidth = 0.9360f;
                    return getCharZ();
                case "z":
                case "U+007A":
                    charWidth = 0.7250f;
                    return getCharZs();
                case "U+0391":
                    charWidth = 1.0844f;
                    return getCharAlpha();
                case "U+03B1":
                    charWidth = 0.7905f;
                    return getCharAlpha_small();
                case "U+0392":
                    charWidth = 0.9051f;
                    return getCharBeta();
                case "U+03B2":
                    charWidth = 0.7876f;
                    return getCharBeta_small();
                case "U+0393":
                    charWidth = 0.7729f;
                    return getCharGamma();
                case "U+03B3":
                    charWidth = 0.8757f;
                    return getCharGamma_small();
                case "U+0394":
                    charWidth = 1.1167f;
                    return getCharDelta();
                case "U+03B4":
                    charWidth = 0.8317f;
                    return getCharDelta_small();
                case "U+0395":
                    charWidth = 0.7905f;
                    return getCharEpsilon();
                case "U+03B5":
                    charWidth = 0.7229f;
                    return getCharEpsilon_small();
                case "U+0396":
                    charWidth = 0.9345f;
                    return getCharZeta();
                case "U+03B6":
                    charWidth = 0.7024f;
                    return getCharZeta_small();
                case "U+0397":
                    charWidth = 0.9169f;
                    return getCharEta();
                case "U+03B7":
                    charWidth = 0.7523f;
                    return getCharEta_small();
                case "U+0398":
                    charWidth = 1.1138f;
                    return getCharTheta();
                case "U+03B8":
                    charWidth = 0.8082f;
                    return getCharTheta_small();
                case "U+0399":
                    charWidth = 0.4731f;
                    return getCharIota();
                case "U+03B9":
                    charWidth = 0.1528f;
                    return getCharIota_small();
                case "U+039A":
                    charWidth = 0.9610f;
                    return getCharKappa();
                case "U+03BA":
                    charWidth = 0.8052f;
                    return getCharKappa_small();
                case "U+039B":
                    charWidth = 1.0873f;
                    return getCharLambda();
                case "U+03BB":
                    charWidth = 0.8787f;
                    return getCharLambda_small();
                case "U+039C":
                    charWidth = 1.0697f;
                    return getCharMu();
                case "U+03BC":
                    charWidth = 0.7582f;
                    return getCharMu_small();
                case "U+039D":
                    charWidth = 0.9139f;
                    return getCharNu();
                case "U+03BD":
                    charWidth = 0.8787f;
                    return getCharNu_small();
                case "U+039E":
                    charWidth = 0.8875f;
                    return getCharXi();
                case "U+03BE":
                    charWidth = 0.7553f;
                    return getCharXi_small();
                case "U+039F":
                    charWidth = 1.1144f;
                    return getCharOmicron();
                case "U+03BF":
                    charWidth = 0.8287f;
                    return getCharOmicron_small();
                case "U+03A0":
                    charWidth = 0.9198f;
                    return getCharPi();
                case "U+03C0":
                    charWidth = 0.7523f;
                    return getCharPi_small();
                case "U+03A1":
                    charWidth = 0.7826f;
                    return getCharRho();
                case "U+03C1":
                    charWidth = 0.7935f;
                    return getCharRho_small();
                case "U+03A3":
                    charWidth = 0.9345f;
                    return getCharSigma();
                case "U+03C3":
                    charWidth = 0.9580f;
                    return getCharSigma_small();
                case "U+03C2":
                    charWidth = 0.7347f;
                    return getCharSigma_smallFinal();
                case "U+03A4":
                    charWidth = 1.0168f;
                    return getCharTau();
                case "U+03C4":
                    charWidth = 0.8023f;
                    return getCharTau_small();
                case "U+03A5":
                    charWidth = 1.0050f;
                    return getCharUpsilon();
                case "U+03C5":
                    charWidth = 0.7582f;
                    return getCharUpsilon_small();
                case "U+03A6":
                    charWidth = 1.1814f;
                    return getCharPhi();
                case "U+03C6":
                    charWidth = 1.1343f;
                    return getCharPhi_small();
                case "U+03A7":
                    charWidth = 1.0227f;
                    return getCharChi();
                case "U+03C7":
                    charWidth = 0.8934f;
                    return getCharChi_small();
                case "U+03A8":
                    charWidth = 1.1461f;
                    return getCharPsi();
                case "U+03C8":
                    charWidth = 1.0726f;
                    return getCharPsi_small();
                case "U+03A9":
                    charWidth = 1.1755f;
                    return getCharOmega();
                case "U+03C9":
                    charWidth = 1.1579f;
                    return getCharOmega_small();
                default:
                    charWidth = 0.7582f;
                    return getCharUnknown();
            }
        }
        #endregion

        // --------------------------------------------- NUMBERS -------------------------------------------------- //

        #region verdana_12_regular_ZERO
        private static List<Line3> getChar0()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.306885f, -0.015344f, 0.000000f), new Vector3(0.414295f, -0.026852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, -0.026852f, 0.000000f), new Vector3(0.489098f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.489098f, -0.021098f, 0.000000f), new Vector3(0.585000f, 0.005754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 0.005754f, 0.000000f), new Vector3(0.665557f, 0.053705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 0.053705f, 0.000000f), new Vector3(0.726934f, 0.124672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.726934f, 0.124672f, 0.000000f), new Vector3(0.759540f, 0.182213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.182213f, 0.000000f), new Vector3(0.790229f, 0.264688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.264688f, 0.000000f), new Vector3(0.811327f, 0.362508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811327f, 0.362508f, 0.000000f), new Vector3(0.824754f, 0.473754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.824754f, 0.473754f, 0.000000f), new Vector3(0.828590f, 0.600344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.828590f, 0.600344f, 0.000000f), new Vector3(0.826672f, 0.698164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.826672f, 0.698164f, 0.000000f), new Vector3(0.815163f, 0.811327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.815163f, 0.811327f, 0.000000f), new Vector3(0.794065f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.794065f, 0.911065f, 0.000000f), new Vector3(0.767213f, 0.995458f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767213f, 0.995458f, 0.000000f), new Vector3(0.728852f, 1.068344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 1.068344f, 0.000000f), new Vector3(0.686655f, 1.123966f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.686655f, 1.123966f, 0.000000f), new Vector3(0.611852f, 1.179589f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611852f, 1.179589f, 0.000000f), new Vector3(0.521705f, 1.212196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.521705f, 1.212196f, 0.000000f), new Vector3(0.414295f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 1.223704f, 0.000000f), new Vector3(0.337574f, 1.217950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337574f, 1.217950f, 0.000000f), new Vector3(0.241672f, 1.191098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 1.191098f, 0.000000f), new Vector3(0.163033f, 1.141229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 1.141229f, 0.000000f), new Vector3(0.099738f, 1.070262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099738f, 1.070262f, 0.000000f), new Vector3(0.069049f, 1.012721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.069049f, 1.012721f, 0.000000f), new Vector3(0.038361f, 0.930245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.930245f, 0.000000f), new Vector3(0.017262f, 0.834344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017262f, 0.834344f, 0.000000f), new Vector3(0.003836f, 0.723098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.723098f, 0.000000f), new Vector3(0.000000f, 0.598426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.598426f, 0.000000f), new Vector3(0.001918f, 0.496770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.001918f, 0.496770f, 0.000000f), new Vector3(0.013426f, 0.383606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.013426f, 0.383606f, 0.000000f), new Vector3(0.032607f, 0.283869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032607f, 0.283869f, 0.000000f), new Vector3(0.061377f, 0.197557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.061377f, 0.197557f, 0.000000f), new Vector3(0.099738f, 0.126590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099738f, 0.126590f, 0.000000f), new Vector3(0.141934f, 0.074803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.141934f, 0.074803f, 0.000000f), new Vector3(0.214820f, 0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.214820f, 0.019180f, 0.000000f), new Vector3(0.306885f, -0.015344f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.414295f, 0.107410f, 0.000000f), new Vector3(0.515951f, 0.126590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.126590f, 0.000000f), new Vector3(0.538967f, 0.138098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.538967f, 0.138098f, 0.000000f), new Vector3(0.606098f, 0.210983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.210983f, 0.000000f), new Vector3(0.617606f, 0.234000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617606f, 0.234000f, 0.000000f), new Vector3(0.646377f, 0.327983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646377f, 0.327983f, 0.000000f), new Vector3(0.655967f, 0.387442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655967f, 0.387442f, 0.000000f), new Vector3(0.665557f, 0.592672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 0.592672f, 0.000000f), new Vector3(0.655967f, 0.805573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655967f, 0.805573f, 0.000000f), new Vector3(0.634868f, 0.916819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634868f, 0.916819f, 0.000000f), new Vector3(0.615688f, 0.964770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.615688f, 0.964770f, 0.000000f), new Vector3(0.556229f, 1.047245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, 1.047245f, 0.000000f), new Vector3(0.446901f, 1.089442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446901f, 1.089442f, 0.000000f), new Vector3(0.414295f, 1.089442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 1.089442f, 0.000000f), new Vector3(0.312639f, 1.070262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 1.070262f, 0.000000f), new Vector3(0.289623f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.289623f, 1.058753f, 0.000000f), new Vector3(0.222492f, 0.985868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.222492f, 0.985868f, 0.000000f), new Vector3(0.210983f, 0.962852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, 0.962852f, 0.000000f), new Vector3(0.182213f, 0.872704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.872704f, 0.000000f), new Vector3(0.172623f, 0.807491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.807491f, 0.000000f), new Vector3(0.164951f, 0.709672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.709672f, 0.000000f), new Vector3(0.163033f, 0.602262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 0.602262f, 0.000000f), new Vector3(0.164951f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.485262f, 0.000000f), new Vector3(0.170705f, 0.387442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.387442f, 0.000000f), new Vector3(0.191803f, 0.283869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.283869f, 0.000000f), new Vector3(0.209065f, 0.235918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.209065f, 0.235918f, 0.000000f), new Vector3(0.268524f, 0.153443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.268524f, 0.153443f, 0.000000f), new Vector3(0.285787f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.140016f, 0.000000f), new Vector3(0.375934f, 0.109328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.375934f, 0.109328f, 0.000000f), new Vector3(0.414295f, 0.107410f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_ONE
        private static List<Line3> getChar1()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.650213f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.650213f, 0.000000f, 0.000000f), new Vector3(0.650213f, 0.120836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.650213f, 0.120836f, 0.000000f), new Vector3(0.404705f, 0.120836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.404705f, 0.120836f, 0.000000f), new Vector3(0.404705f, 1.202606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.404705f, 1.202606f, 0.000000f), new Vector3(0.280033f, 1.202606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.280033f, 1.202606f, 0.000000f), new Vector3(0.255098f, 1.120130f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.255098f, 1.120130f, 0.000000f), new Vector3(0.197557f, 1.068344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 1.068344f, 0.000000f), new Vector3(0.109328f, 1.043409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.109328f, 1.043409f, 0.000000f), new Vector3(0.000000f, 1.035737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.035737f, 0.000000f), new Vector3(0.000000f, 0.926409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.926409f, 0.000000f), new Vector3(0.249344f, 0.926409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.926409f, 0.000000f), new Vector3(0.249344f, 0.120836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.120836f, 0.000000f), new Vector3(0.000000f, 0.120836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.120836f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_TWO
        private static List<Line3> getChar2()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.811327f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811327f, 0.000000f, 0.000000f), new Vector3(0.811327f, 0.136180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811327f, 0.136180f, 0.000000f), new Vector3(0.166869f, 0.136180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.136180f, 0.000000f), new Vector3(0.180295f, 0.149606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.180295f, 0.149606f, 0.000000f), new Vector3(0.257016f, 0.214820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.257016f, 0.214820f, 0.000000f), new Vector3(0.364426f, 0.308803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.364426f, 0.308803f, 0.000000f), new Vector3(0.441147f, 0.377852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.377852f, 0.000000f), new Vector3(0.515951f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.448819f, 0.000000f), new Vector3(0.619524f, 0.554311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.619524f, 0.554311f, 0.000000f), new Vector3(0.692409f, 0.654049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.692409f, 0.654049f, 0.000000f), new Vector3(0.744196f, 0.765295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.744196f, 0.765295f, 0.000000f), new Vector3(0.749950f, 0.790229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749950f, 0.790229f, 0.000000f), new Vector3(0.759540f, 0.893803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.893803f, 0.000000f), new Vector3(0.753786f, 0.970524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.753786f, 0.970524f, 0.000000f), new Vector3(0.719262f, 1.060671f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719262f, 1.060671f, 0.000000f), new Vector3(0.652131f, 1.139311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652131f, 1.139311f, 0.000000f), new Vector3(0.573491f, 1.185344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573491f, 1.185344f, 0.000000f), new Vector3(0.479508f, 1.214114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479508f, 1.214114f, 0.000000f), new Vector3(0.366344f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.366344f, 1.223704f, 0.000000f), new Vector3(0.285787f, 1.219868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 1.219868f, 0.000000f), new Vector3(0.182213f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 1.200688f, 0.000000f), new Vector3(0.117000f, 1.183425f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.117000f, 1.183425f, 0.000000f), new Vector3(0.028770f, 1.148901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.028770f, 1.148901f, 0.000000f), new Vector3(0.028770f, 0.980114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.028770f, 0.980114f, 0.000000f), new Vector3(0.036443f, 0.980114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.036443f, 0.980114f, 0.000000f), new Vector3(0.111246f, 1.022311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111246f, 1.022311f, 0.000000f), new Vector3(0.203311f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.203311f, 1.058753f, 0.000000f), new Vector3(0.264688f, 1.074098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.264688f, 1.074098f, 0.000000f), new Vector3(0.362508f, 1.083688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 1.083688f, 0.000000f), new Vector3(0.452655f, 1.074098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452655f, 1.074098f, 0.000000f), new Vector3(0.535131f, 1.031901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.535131f, 1.031901f, 0.000000f), new Vector3(0.573491f, 0.983950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573491f, 0.983950f, 0.000000f), new Vector3(0.596508f, 0.886131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.596508f, 0.886131f, 0.000000f), new Vector3(0.581164f, 0.788311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.581164f, 0.788311f, 0.000000f), new Vector3(0.535131f, 0.688573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.535131f, 0.688573f, 0.000000f), new Vector3(0.485262f, 0.623360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, 0.623360f, 0.000000f), new Vector3(0.416213f, 0.546639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.416213f, 0.546639f, 0.000000f), new Vector3(0.322229f, 0.450737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.322229f, 0.450737f, 0.000000f), new Vector3(0.247426f, 0.381688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.247426f, 0.381688f, 0.000000f), new Vector3(0.076721f, 0.234000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.076721f, 0.234000f, 0.000000f), new Vector3(0.000000f, 0.166869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.166869f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_TWO_SUPERSCRIPT
        private static List<Line3> getChar2_sp()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.501938f, 0.000000f), new Vector3(0.555420f, 0.501938f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 0.501938f, 0.000000f), new Vector3(0.555420f, 0.616548f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 0.616548f, 0.000000f), new Vector3(0.152814f, 0.616548f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.616548f, 0.000000f), new Vector3(0.255669f, 0.690016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.255669f, 0.690016f, 0.000000f), new Vector3(0.349709f, 0.769362f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349709f, 0.769362f, 0.000000f), new Vector3(0.402606f, 0.819320f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402606f, 0.819320f, 0.000000f), new Vector3(0.470196f, 0.904544f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.470196f, 0.904544f, 0.000000f), new Vector3(0.479013f, 0.922176f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479013f, 0.922176f, 0.000000f), new Vector3(0.502522f, 1.039725f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502522f, 1.039725f, 0.000000f), new Vector3(0.493706f, 1.104377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.493706f, 1.104377f, 0.000000f), new Vector3(0.434932f, 1.201355f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.434932f, 1.201355f, 0.000000f), new Vector3(0.361464f, 1.245436f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.361464f, 1.245436f, 0.000000f), new Vector3(0.238037f, 1.263068f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.238037f, 1.263068f, 0.000000f), new Vector3(0.117549f, 1.251313f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.117549f, 1.251313f, 0.000000f), new Vector3(0.002939f, 1.216049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.002939f, 1.216049f, 0.000000f), new Vector3(0.002939f, 1.072051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.002939f, 1.072051f, 0.000000f), new Vector3(0.014694f, 1.072051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.014694f, 1.072051f, 0.000000f), new Vector3(0.105794f, 1.122009f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.105794f, 1.122009f, 0.000000f), new Vector3(0.220405f, 1.145519f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 1.145519f, 0.000000f), new Vector3(0.317383f, 1.116132f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.317383f, 1.116132f, 0.000000f), new Vector3(0.352647f, 1.027970f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352647f, 1.027970f, 0.000000f), new Vector3(0.326199f, 0.919237f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326199f, 0.919237f, 0.000000f), new Vector3(0.305628f, 0.886911f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.305628f, 0.886911f, 0.000000f), new Vector3(0.211588f, 0.792872f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.211588f, 0.792872f, 0.000000f), new Vector3(0.096978f, 0.695894f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.096978f, 0.695894f, 0.000000f), new Vector3(0.000000f, 0.622426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.622426f, 0.000000f), new Vector3(0.000000f, 0.501938f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_THREE
        private static List<Line3> getChar3()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.049869f, 0.000000f), new Vector3(0.051787f, 0.028770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.051787f, 0.028770f, 0.000000f), new Vector3(0.159197f, -0.001918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, -0.001918f, 0.000000f), new Vector3(0.253180f, -0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.253180f, -0.019180f, 0.000000f), new Vector3(0.354836f, -0.026852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.354836f, -0.026852f, 0.000000f), new Vector3(0.437311f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437311f, -0.021098f, 0.000000f), new Vector3(0.535131f, 0.001918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.535131f, 0.001918f, 0.000000f), new Vector3(0.594590f, 0.026852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.594590f, 0.026852f, 0.000000f), new Vector3(0.675147f, 0.084393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675147f, 0.084393f, 0.000000f), new Vector3(0.705836f, 0.117000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.705836f, 0.117000f, 0.000000f), new Vector3(0.759540f, 0.203311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.203311f, 0.000000f), new Vector3(0.776803f, 0.251262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.776803f, 0.251262f, 0.000000f), new Vector3(0.790229f, 0.354836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.354836f, 0.000000f), new Vector3(0.788311f, 0.397033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 0.397033f, 0.000000f), new Vector3(0.765295f, 0.489098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 0.489098f, 0.000000f), new Vector3(0.761459f, 0.496770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.761459f, 0.496770f, 0.000000f), new Vector3(0.700082f, 0.577327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.700082f, 0.577327f, 0.000000f), new Vector3(0.615688f, 0.629114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.615688f, 0.629114f, 0.000000f), new Vector3(0.527459f, 0.655967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.527459f, 0.655967f, 0.000000f), new Vector3(0.527459f, 0.665557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.527459f, 0.665557f, 0.000000f), new Vector3(0.613770f, 0.698164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.613770f, 0.698164f, 0.000000f), new Vector3(0.694327f, 0.761459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.694327f, 0.761459f, 0.000000f), new Vector3(0.746114f, 0.838180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746114f, 0.838180f, 0.000000f), new Vector3(0.765295f, 0.934081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 0.934081f, 0.000000f), new Vector3(0.763377f, 0.968606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.763377f, 0.968606f, 0.000000f), new Vector3(0.734606f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.734606f, 1.062590f, 0.000000f), new Vector3(0.721180f, 1.085606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.721180f, 1.085606f, 0.000000f), new Vector3(0.646377f, 1.156573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646377f, 1.156573f, 0.000000f), new Vector3(0.621442f, 1.171917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.621442f, 1.171917f, 0.000000f), new Vector3(0.525541f, 1.208360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.525541f, 1.208360f, 0.000000f), new Vector3(0.477590f, 1.216032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.477590f, 1.216032f, 0.000000f), new Vector3(0.372098f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.372098f, 1.223704f, 0.000000f), new Vector3(0.289623f, 1.219868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.289623f, 1.219868f, 0.000000f), new Vector3(0.187967f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.187967f, 1.200688f, 0.000000f), new Vector3(0.122754f, 1.181507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.122754f, 1.181507f, 0.000000f), new Vector3(0.034525f, 1.148901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.034525f, 1.148901f, 0.000000f), new Vector3(0.034525f, 0.980114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.034525f, 0.980114f, 0.000000f), new Vector3(0.042197f, 0.980114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.042197f, 0.980114f, 0.000000f), new Vector3(0.111246f, 1.018475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111246f, 1.018475f, 0.000000f), new Vector3(0.205229f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205229f, 1.056835f, 0.000000f), new Vector3(0.268524f, 1.074098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.268524f, 1.074098f, 0.000000f), new Vector3(0.368262f, 1.083688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.368262f, 1.083688f, 0.000000f), new Vector3(0.456492f, 1.076016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 1.076016f, 0.000000f), new Vector3(0.531295f, 1.047245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531295f, 1.047245f, 0.000000f), new Vector3(0.583082f, 0.993540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.583082f, 0.993540f, 0.000000f), new Vector3(0.600344f, 0.916819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.916819f, 0.000000f), new Vector3(0.588836f, 0.842016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.588836f, 0.842016f, 0.000000f), new Vector3(0.529377f, 0.769131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, 0.769131f, 0.000000f), new Vector3(0.441147f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.728852f, 0.000000f), new Vector3(0.333737f, 0.717344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.333737f, 0.717344f, 0.000000f), new Vector3(0.276197f, 0.717344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276197f, 0.717344f, 0.000000f), new Vector3(0.276197f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276197f, 0.583082f, 0.000000f), new Vector3(0.349082f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349082f, 0.583082f, 0.000000f), new Vector3(0.460328f, 0.573491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.460328f, 0.573491f, 0.000000f), new Vector3(0.548557f, 0.538967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.548557f, 0.538967f, 0.000000f), new Vector3(0.606098f, 0.473754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.473754f, 0.000000f), new Vector3(0.608016f, 0.468000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608016f, 0.468000f, 0.000000f), new Vector3(0.625278f, 0.366344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625278f, 0.366344f, 0.000000f), new Vector3(0.625278f, 0.356754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625278f, 0.356754f, 0.000000f), new Vector3(0.608016f, 0.258934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608016f, 0.258934f, 0.000000f), new Vector3(0.552393f, 0.180295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552393f, 0.180295f, 0.000000f), new Vector3(0.460328f, 0.130426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.460328f, 0.130426f, 0.000000f), new Vector3(0.351000f, 0.113164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.351000f, 0.113164f, 0.000000f), new Vector3(0.274279f, 0.118918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.274279f, 0.118918f, 0.000000f), new Vector3(0.172623f, 0.143852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.143852f, 0.000000f), new Vector3(0.097820f, 0.170705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.097820f, 0.170705f, 0.000000f), new Vector3(0.011508f, 0.218656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011508f, 0.218656f, 0.000000f), new Vector3(0.000000f, 0.218656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.218656f, 0.000000f), new Vector3(0.000000f, 0.049869f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_THREE_SUPERSCRIPT
        private static List<Line3> getChar3_sp()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.522509f, 0.000000f), new Vector3(0.108733f, 0.490183f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.108733f, 0.490183f, 0.000000f), new Vector3(0.114610f, 0.487244f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.114610f, 0.487244f, 0.000000f), new Vector3(0.232160f, 0.475489f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232160f, 0.475489f, 0.000000f), new Vector3(0.346770f, 0.490183f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.346770f, 0.490183f, 0.000000f), new Vector3(0.437870f, 0.531325f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, 0.531325f, 0.000000f), new Vector3(0.499584f, 0.604793f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.499584f, 0.604793f, 0.000000f), new Vector3(0.526032f, 0.707649f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.526032f, 0.707649f, 0.000000f), new Vector3(0.523094f, 0.725281f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.523094f, 0.725281f, 0.000000f), new Vector3(0.479013f, 0.828137f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479013f, 0.828137f, 0.000000f), new Vector3(0.470196f, 0.834014f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.470196f, 0.834014f, 0.000000f), new Vector3(0.367341f, 0.883972f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.367341f, 0.883972f, 0.000000f), new Vector3(0.367341f, 0.889850f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.367341f, 0.889850f, 0.000000f), new Vector3(0.373218f, 0.892789f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.373218f, 0.892789f, 0.000000f), new Vector3(0.467258f, 0.957441f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.467258f, 0.957441f, 0.000000f), new Vector3(0.473135f, 0.963318f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473135f, 0.963318f, 0.000000f), new Vector3(0.505461f, 1.072051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.505461f, 1.072051f, 0.000000f), new Vector3(0.502522f, 1.113193f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502522f, 1.113193f, 0.000000f), new Vector3(0.437870f, 1.207232f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, 1.207232f, 0.000000f), new Vector3(0.370280f, 1.245436f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370280f, 1.245436f, 0.000000f), new Vector3(0.246853f, 1.263068f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.246853f, 1.263068f, 0.000000f), new Vector3(0.123427f, 1.251313f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.123427f, 1.251313f, 0.000000f), new Vector3(0.011755f, 1.216049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 1.216049f, 0.000000f), new Vector3(0.011755f, 1.074990f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 1.074990f, 0.000000f), new Vector3(0.026449f, 1.074990f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.026449f, 1.074990f, 0.000000f), new Vector3(0.117549f, 1.127887f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.117549f, 1.127887f, 0.000000f), new Vector3(0.232160f, 1.151397f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232160f, 1.151397f, 0.000000f), new Vector3(0.335015f, 1.124948f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335015f, 1.124948f, 0.000000f), new Vector3(0.367341f, 1.045602f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.367341f, 1.045602f, 0.000000f), new Vector3(0.329138f, 0.963318f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.329138f, 0.963318f, 0.000000f), new Vector3(0.220405f, 0.930992f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 0.930992f, 0.000000f), new Vector3(0.120488f, 0.930992f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120488f, 0.930992f, 0.000000f), new Vector3(0.120488f, 0.819320f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120488f, 0.819320f, 0.000000f), new Vector3(0.232160f, 0.819320f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232160f, 0.819320f, 0.000000f), new Vector3(0.346770f, 0.795811f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.346770f, 0.795811f, 0.000000f), new Vector3(0.387912f, 0.704710f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.387912f, 0.704710f, 0.000000f), new Vector3(0.340892f, 0.619487f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.340892f, 0.619487f, 0.000000f), new Vector3(0.229221f, 0.590100f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.229221f, 0.590100f, 0.000000f), new Vector3(0.220405f, 0.590100f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 0.590100f, 0.000000f), new Vector3(0.105794f, 0.613609f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.105794f, 0.613609f, 0.000000f), new Vector3(0.011755f, 0.663568f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.663568f, 0.000000f), new Vector3(0.000000f, 0.663568f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.663568f, 0.000000f), new Vector3(0.000000f, 0.522509f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_FOUR
        private static List<Line3> getChar4()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.573491f, 0.000000f, 0.000000f), new Vector3(0.728852f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 0.000000f, 0.000000f), new Vector3(0.728852f, 0.337574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 0.337574f, 0.000000f), new Vector3(0.905311f, 0.337574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.905311f, 0.337574f, 0.000000f), new Vector3(0.905311f, 0.466082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.905311f, 0.466082f, 0.000000f), new Vector3(0.728852f, 0.466082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 0.466082f, 0.000000f), new Vector3(0.728852f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 1.198770f, 0.000000f), new Vector3(0.579246f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.579246f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.521705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.521705f, 0.000000f), new Vector3(0.000000f, 0.337574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.337574f, 0.000000f), new Vector3(0.573491f, 0.337574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573491f, 0.337574f, 0.000000f), new Vector3(0.573491f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.109328f, 0.466082f, 0.000000f), new Vector3(0.573491f, 0.466082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573491f, 0.466082f, 0.000000f), new Vector3(0.573491f, 1.006967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573491f, 1.006967f, 0.000000f), new Vector3(0.109328f, 0.466082f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_FIVE
        private static List<Line3> getChar5()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.044115f, 0.000000f), new Vector3(0.049869f, 0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.049869f, 0.024934f, 0.000000f), new Vector3(0.155361f, -0.005754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, -0.005754f, 0.000000f), new Vector3(0.239754f, -0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.239754f, -0.019180f, 0.000000f), new Vector3(0.343328f, -0.026852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, -0.026852f, 0.000000f), new Vector3(0.435393f, -0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.435393f, -0.019180f, 0.000000f), new Vector3(0.529377f, 0.003836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, 0.003836f, 0.000000f), new Vector3(0.586918f, 0.030689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.586918f, 0.030689f, 0.000000f), new Vector3(0.667475f, 0.090147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.667475f, 0.090147f, 0.000000f), new Vector3(0.700082f, 0.128508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.700082f, 0.128508f, 0.000000f), new Vector3(0.751868f, 0.220574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.220574f, 0.000000f), new Vector3(0.769131f, 0.278115f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.769131f, 0.278115f, 0.000000f), new Vector3(0.782557f, 0.379770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, 0.379770f, 0.000000f), new Vector3(0.774885f, 0.466082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.774885f, 0.466082f, 0.000000f), new Vector3(0.746114f, 0.556229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746114f, 0.556229f, 0.000000f), new Vector3(0.719262f, 0.600344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719262f, 0.600344f, 0.000000f), new Vector3(0.646377f, 0.669393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646377f, 0.669393f, 0.000000f), new Vector3(0.608016f, 0.694327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608016f, 0.694327f, 0.000000f), new Vector3(0.512114f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.512114f, 0.728852f, 0.000000f), new Vector3(0.448819f, 0.740360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.448819f, 0.740360f, 0.000000f), new Vector3(0.341410f, 0.746114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.341410f, 0.746114f, 0.000000f), new Vector3(0.278115f, 0.744196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.278115f, 0.744196f, 0.000000f), new Vector3(0.207147f, 0.738442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.207147f, 0.738442f, 0.000000f), new Vector3(0.207147f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.207147f, 1.058753f, 0.000000f), new Vector3(0.772967f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772967f, 1.058753f, 0.000000f), new Vector3(0.772967f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772967f, 1.198770f, 0.000000f), new Vector3(0.051787f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.051787f, 1.198770f, 0.000000f), new Vector3(0.051787f, 0.581164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.051787f, 0.581164f, 0.000000f), new Vector3(0.063295f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.063295f, 0.583082f, 0.000000f), new Vector3(0.197557f, 0.604180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.604180f, 0.000000f), new Vector3(0.301131f, 0.609934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.301131f, 0.609934f, 0.000000f), new Vector3(0.337574f, 0.609934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337574f, 0.609934f, 0.000000f), new Vector3(0.437311f, 0.598426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437311f, 0.598426f, 0.000000f), new Vector3(0.446901f, 0.596508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446901f, 0.596508f, 0.000000f), new Vector3(0.537049f, 0.554311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537049f, 0.554311f, 0.000000f), new Vector3(0.596508f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.596508f, 0.485262f, 0.000000f), new Vector3(0.617606f, 0.381688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617606f, 0.381688f, 0.000000f), new Vector3(0.617606f, 0.368262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617606f, 0.368262f, 0.000000f), new Vector3(0.600344f, 0.268524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.268524f, 0.000000f), new Vector3(0.538967f, 0.178377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.538967f, 0.178377f, 0.000000f), new Vector3(0.450737f, 0.130426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, 0.130426f, 0.000000f), new Vector3(0.341410f, 0.113164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.341410f, 0.113164f, 0.000000f), new Vector3(0.268524f, 0.118918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.268524f, 0.118918f, 0.000000f), new Vector3(0.166869f, 0.143852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.143852f, 0.000000f), new Vector3(0.099738f, 0.168787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099738f, 0.168787f, 0.000000f), new Vector3(0.011508f, 0.214820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011508f, 0.214820f, 0.000000f), new Vector3(0.000000f, 0.214820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.214820f, 0.000000f), new Vector3(0.000000f, 0.044115f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_SIX
        private static List<Line3> getChar6()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.433475f, -0.026852f, 0.000000f), new Vector3(0.560065f, -0.009590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.560065f, -0.009590f, 0.000000f), new Vector3(0.648295f, 0.028770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.648295f, 0.028770f, 0.000000f), new Vector3(0.728852f, 0.090147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 0.090147f, 0.000000f), new Vector3(0.749950f, 0.111246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749950f, 0.111246f, 0.000000f), new Vector3(0.805573f, 0.191803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805573f, 0.191803f, 0.000000f), new Vector3(0.838180f, 0.283869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.838180f, 0.283869f, 0.000000f), new Vector3(0.847770f, 0.387442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.847770f, 0.387442f, 0.000000f), new Vector3(0.842016f, 0.479508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.842016f, 0.479508f, 0.000000f), new Vector3(0.813245f, 0.569655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.813245f, 0.569655f, 0.000000f), new Vector3(0.780639f, 0.625278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.780639f, 0.625278f, 0.000000f), new Vector3(0.705836f, 0.694327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.705836f, 0.694327f, 0.000000f), new Vector3(0.678983f, 0.713508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678983f, 0.713508f, 0.000000f), new Vector3(0.586918f, 0.751868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.586918f, 0.751868f, 0.000000f), new Vector3(0.556229f, 0.757622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, 0.757622f, 0.000000f), new Vector3(0.450737f, 0.765295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, 0.765295f, 0.000000f), new Vector3(0.397033f, 0.763377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.397033f, 0.763377f, 0.000000f), new Vector3(0.301131f, 0.744196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.301131f, 0.744196f, 0.000000f), new Vector3(0.258934f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.258934f, 0.728852f, 0.000000f), new Vector3(0.166869f, 0.680901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.680901f, 0.000000f), new Vector3(0.168787f, 0.703918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.168787f, 0.703918f, 0.000000f), new Vector3(0.191803f, 0.809409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.809409f, 0.000000f), new Vector3(0.230164f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.230164f, 0.899557f, 0.000000f), new Vector3(0.285787f, 0.974360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.974360f, 0.000000f), new Vector3(0.366344f, 1.035737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.366344f, 1.035737f, 0.000000f), new Vector3(0.456492f, 1.072180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 1.072180f, 0.000000f), new Vector3(0.561983f, 1.083688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.561983f, 1.083688f, 0.000000f), new Vector3(0.655967f, 1.072180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655967f, 1.072180f, 0.000000f), new Vector3(0.723098f, 1.049163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.723098f, 1.049163f, 0.000000f), new Vector3(0.732688f, 1.049163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.732688f, 1.049163f, 0.000000f), new Vector3(0.732688f, 1.202606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.732688f, 1.202606f, 0.000000f), new Vector3(0.667475f, 1.217950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.667475f, 1.217950f, 0.000000f), new Vector3(0.586918f, 1.221786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.586918f, 1.221786f, 0.000000f), new Vector3(0.517869f, 1.217950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517869f, 1.217950f, 0.000000f), new Vector3(0.416213f, 1.202606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.416213f, 1.202606f, 0.000000f), new Vector3(0.326065f, 1.169999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326065f, 1.169999f, 0.000000f), new Vector3(0.297295f, 1.156573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.297295f, 1.156573f, 0.000000f), new Vector3(0.212902f, 1.099032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.212902f, 1.099032f, 0.000000f), new Vector3(0.143852f, 1.028065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 1.028065f, 0.000000f), new Vector3(0.118918f, 0.995458f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.118918f, 0.995458f, 0.000000f), new Vector3(0.069049f, 0.907229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.069049f, 0.907229f, 0.000000f), new Vector3(0.034525f, 0.809409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.034525f, 0.809409f, 0.000000f), new Vector3(0.017262f, 0.736524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017262f, 0.736524f, 0.000000f), new Vector3(0.003836f, 0.636786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.636786f, 0.000000f), new Vector3(0.000000f, 0.525541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.525541f, 0.000000f), new Vector3(0.001918f, 0.454573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.001918f, 0.454573f, 0.000000f), new Vector3(0.015344f, 0.351000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.015344f, 0.351000f, 0.000000f), new Vector3(0.038361f, 0.260852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.260852f, 0.000000f), new Vector3(0.084393f, 0.159197f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.084393f, 0.159197f, 0.000000f), new Vector3(0.143852f, 0.082475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 0.082475f, 0.000000f), new Vector3(0.182213f, 0.049869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.049869f, 0.000000f), new Vector3(0.272360f, 0.001918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.272360f, 0.001918f, 0.000000f), new Vector3(0.329901f, -0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.329901f, -0.015344f, 0.000000f), new Vector3(0.433475f, -0.026852f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.164951f, 0.567737f, 0.000000f), new Vector3(0.163033f, 0.491016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 0.491016f, 0.000000f), new Vector3(0.168787f, 0.375934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.168787f, 0.375934f, 0.000000f), new Vector3(0.189885f, 0.291541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.189885f, 0.291541f, 0.000000f), new Vector3(0.237836f, 0.199475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.237836f, 0.199475f, 0.000000f), new Vector3(0.260852f, 0.174541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.260852f, 0.174541f, 0.000000f), new Vector3(0.341410f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.341410f, 0.122754f, 0.000000f), new Vector3(0.435393f, 0.105492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.435393f, 0.105492f, 0.000000f), new Vector3(0.537049f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537049f, 0.122754f, 0.000000f), new Vector3(0.615688f, 0.174541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.615688f, 0.174541f, 0.000000f), new Vector3(0.665557f, 0.255098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 0.255098f, 0.000000f), new Vector3(0.684737f, 0.358672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 0.358672f, 0.000000f), new Vector3(0.684737f, 0.381688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 0.381688f, 0.000000f), new Vector3(0.671311f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.671311f, 0.485262f, 0.000000f), new Vector3(0.663639f, 0.506360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.663639f, 0.506360f, 0.000000f), new Vector3(0.600344f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.583082f, 0.000000f), new Vector3(0.590754f, 0.590754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.590754f, 0.590754f, 0.000000f), new Vector3(0.508278f, 0.625278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, 0.625278f, 0.000000f), new Vector3(0.414295f, 0.632950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 0.632950f, 0.000000f), new Vector3(0.310721f, 0.623360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.310721f, 0.623360f, 0.000000f), new Vector3(0.285787f, 0.617606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.617606f, 0.000000f), new Vector3(0.164951f, 0.567737f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_SEVEN
        private static List<Line3> getChar7()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.105492f, 0.000000f, 0.000000f), new Vector3(0.278115f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.278115f, 0.000000f, 0.000000f), new Vector3(0.818999f, 1.018475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.818999f, 1.018475f, 0.000000f), new Vector3(0.818999f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.818999f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.058753f, 0.000000f), new Vector3(0.682819f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.682819f, 1.058753f, 0.000000f), new Vector3(0.105492f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular_EIGHT
        private static List<Line3> getChar8()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.425803f, -0.028770f, 0.000000f), new Vector3(0.454573f, -0.026852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.454573f, -0.026852f, 0.000000f), new Vector3(0.558147f, -0.013426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, -0.013426f, 0.000000f), new Vector3(0.650213f, 0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.650213f, 0.021098f, 0.000000f), new Vector3(0.730770f, 0.074803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.730770f, 0.074803f, 0.000000f), new Vector3(0.795983f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.795983f, 0.147688f, 0.000000f), new Vector3(0.838180f, 0.234000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.838180f, 0.234000f, 0.000000f), new Vector3(0.851606f, 0.333737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.851606f, 0.333737f, 0.000000f), new Vector3(0.838180f, 0.429639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.838180f, 0.429639f, 0.000000f), new Vector3(0.788311f, 0.519787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 0.519787f, 0.000000f), new Vector3(0.715426f, 0.581164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.715426f, 0.581164f, 0.000000f), new Vector3(0.619524f, 0.632950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.619524f, 0.632950f, 0.000000f), new Vector3(0.619524f, 0.638705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.619524f, 0.638705f, 0.000000f), new Vector3(0.703918f, 0.688573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.703918f, 0.688573f, 0.000000f), new Vector3(0.769131f, 0.761459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.769131f, 0.761459f, 0.000000f), new Vector3(0.801737f, 0.824754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.801737f, 0.824754f, 0.000000f), new Vector3(0.818999f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.818999f, 0.920655f, 0.000000f), new Vector3(0.815163f, 0.976278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.815163f, 0.976278f, 0.000000f), new Vector3(0.780639f, 1.064507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.780639f, 1.064507f, 0.000000f), new Vector3(0.711590f, 1.139311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711590f, 1.139311f, 0.000000f), new Vector3(0.631032f, 1.189180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631032f, 1.189180f, 0.000000f), new Vector3(0.537049f, 1.216032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537049f, 1.216032f, 0.000000f), new Vector3(0.425803f, 1.225622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.425803f, 1.225622f, 0.000000f), new Vector3(0.318393f, 1.216032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.318393f, 1.216032f, 0.000000f), new Vector3(0.224410f, 1.185344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.224410f, 1.185344f, 0.000000f), new Vector3(0.143852f, 1.135475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 1.135475f, 0.000000f), new Vector3(0.095902f, 1.091360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.095902f, 1.091360f, 0.000000f), new Vector3(0.047951f, 1.006967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047951f, 1.006967f, 0.000000f), new Vector3(0.032607f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032607f, 0.911065f, 0.000000f), new Vector3(0.038361f, 0.845852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.845852f, 0.000000f), new Vector3(0.078639f, 0.757622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.078639f, 0.757622f, 0.000000f), new Vector3(0.128508f, 0.705836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.128508f, 0.705836f, 0.000000f), new Vector3(0.220574f, 0.644459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220574f, 0.644459f, 0.000000f), new Vector3(0.220574f, 0.640623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220574f, 0.640623f, 0.000000f), new Vector3(0.124672f, 0.581164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.124672f, 0.581164f, 0.000000f), new Vector3(0.057541f, 0.512114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.057541f, 0.512114f, 0.000000f), new Vector3(0.015344f, 0.429639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.015344f, 0.429639f, 0.000000f), new Vector3(0.000000f, 0.329901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.329901f, 0.000000f), new Vector3(0.011508f, 0.235918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011508f, 0.235918f, 0.000000f), new Vector3(0.049869f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.049869f, 0.147688f, 0.000000f), new Vector3(0.115082f, 0.072885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.115082f, 0.072885f, 0.000000f), new Vector3(0.138098f, 0.053705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.138098f, 0.053705f, 0.000000f), new Vector3(0.218656f, 0.007672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.218656f, 0.007672f, 0.000000f), new Vector3(0.314557f, -0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.314557f, -0.019180f, 0.000000f), new Vector3(0.425803f, -0.028770f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.420049f, 0.092066f, 0.000000f), new Vector3(0.427721f, 0.092066f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.427721f, 0.092066f, 0.000000f), new Vector3(0.533213f, 0.107410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.533213f, 0.107410f, 0.000000f), new Vector3(0.615688f, 0.151524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.615688f, 0.151524f, 0.000000f), new Vector3(0.671311f, 0.230164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.671311f, 0.230164f, 0.000000f), new Vector3(0.684737f, 0.318393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 0.318393f, 0.000000f), new Vector3(0.665557f, 0.418131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 0.418131f, 0.000000f), new Vector3(0.654049f, 0.437311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.654049f, 0.437311f, 0.000000f), new Vector3(0.585000f, 0.494852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 0.494852f, 0.000000f), new Vector3(0.525541f, 0.527459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.525541f, 0.527459f, 0.000000f), new Vector3(0.443065f, 0.560065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 0.560065f, 0.000000f), new Vector3(0.322229f, 0.602262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.322229f, 0.602262f, 0.000000f), new Vector3(0.241672f, 0.540885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 0.540885f, 0.000000f), new Vector3(0.207147f, 0.494852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.207147f, 0.494852f, 0.000000f), new Vector3(0.168787f, 0.402787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.168787f, 0.402787f, 0.000000f), new Vector3(0.163033f, 0.341410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 0.341410f, 0.000000f), new Vector3(0.180295f, 0.243590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.180295f, 0.243590f, 0.000000f), new Vector3(0.237836f, 0.163033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.237836f, 0.163033f, 0.000000f), new Vector3(0.318393f, 0.111246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.318393f, 0.111246f, 0.000000f), new Vector3(0.420049f, 0.092066f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.519787f, 0.677065f, 0.000000f), new Vector3(0.602262f, 0.748032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602262f, 0.748032f, 0.000000f), new Vector3(0.631032f, 0.786393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631032f, 0.786393f, 0.000000f), new Vector3(0.657885f, 0.882295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.657885f, 0.882295f, 0.000000f), new Vector3(0.659803f, 0.918737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.659803f, 0.918737f, 0.000000f), new Vector3(0.634868f, 1.014639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634868f, 1.014639f, 0.000000f), new Vector3(0.594590f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.594590f, 1.056835f, 0.000000f), new Vector3(0.508278f, 1.097114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, 1.097114f, 0.000000f), new Vector3(0.423885f, 1.108622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 1.108622f, 0.000000f), new Vector3(0.320311f, 1.091360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.320311f, 1.091360f, 0.000000f), new Vector3(0.258934f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.258934f, 1.058753f, 0.000000f), new Vector3(0.201393f, 0.982032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.201393f, 0.982032f, 0.000000f), new Vector3(0.193721f, 0.928327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 0.928327f, 0.000000f), new Vector3(0.222492f, 0.834344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.222492f, 0.834344f, 0.000000f), new Vector3(0.301131f, 0.765295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.301131f, 0.765295f, 0.000000f), new Vector3(0.410459f, 0.715426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.410459f, 0.715426f, 0.000000f), new Vector3(0.519787f, 0.677065f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__NINE
        private static List<Line3> getChar9()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.115082f, -0.005754f, 0.000000f), new Vector3(0.182213f, -0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, -0.019180f, 0.000000f), new Vector3(0.260852f, -0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.260852f, -0.024934f, 0.000000f), new Vector3(0.327983f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, -0.021098f, 0.000000f), new Vector3(0.429639f, -0.005754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429639f, -0.005754f, 0.000000f), new Vector3(0.519787f, 0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.519787f, 0.024934f, 0.000000f), new Vector3(0.632950f, 0.095902f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.632950f, 0.095902f, 0.000000f), new Vector3(0.703918f, 0.166869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.703918f, 0.166869f, 0.000000f), new Vector3(0.776803f, 0.285787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.776803f, 0.285787f, 0.000000f), new Vector3(0.813245f, 0.385524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.813245f, 0.385524f, 0.000000f), new Vector3(0.830508f, 0.462246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.830508f, 0.462246f, 0.000000f), new Vector3(0.843934f, 0.563901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843934f, 0.563901f, 0.000000f), new Vector3(0.847770f, 0.671311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.847770f, 0.671311f, 0.000000f), new Vector3(0.845852f, 0.753786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.845852f, 0.753786f, 0.000000f), new Vector3(0.832426f, 0.857360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.832426f, 0.857360f, 0.000000f), new Vector3(0.811327f, 0.943672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811327f, 0.943672f, 0.000000f), new Vector3(0.765295f, 1.037655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 1.037655f, 0.000000f), new Vector3(0.705836f, 1.114376f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.705836f, 1.114376f, 0.000000f), new Vector3(0.665557f, 1.148901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 1.148901f, 0.000000f), new Vector3(0.575409f, 1.196852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575409f, 1.196852f, 0.000000f), new Vector3(0.517869f, 1.212196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517869f, 1.212196f, 0.000000f), new Vector3(0.414295f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 1.223704f, 0.000000f), new Vector3(0.289623f, 1.206442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.289623f, 1.206442f, 0.000000f), new Vector3(0.199475f, 1.168081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199475f, 1.168081f, 0.000000f), new Vector3(0.120836f, 1.108622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 1.108622f, 0.000000f), new Vector3(0.097820f, 1.085606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.097820f, 1.085606f, 0.000000f), new Vector3(0.044115f, 1.005049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.044115f, 1.005049f, 0.000000f), new Vector3(0.009590f, 0.912983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.009590f, 0.912983f, 0.000000f), new Vector3(0.000000f, 0.809409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.809409f, 0.000000f), new Vector3(0.005754f, 0.719262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005754f, 0.719262f, 0.000000f), new Vector3(0.036443f, 0.627196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.036443f, 0.627196f, 0.000000f), new Vector3(0.069049f, 0.573491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.069049f, 0.573491f, 0.000000f), new Vector3(0.141934f, 0.502524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.141934f, 0.502524f, 0.000000f), new Vector3(0.170705f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.485262f, 0.000000f), new Vector3(0.262770f, 0.446901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.262770f, 0.446901f, 0.000000f), new Vector3(0.291541f, 0.441147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.291541f, 0.441147f, 0.000000f), new Vector3(0.397033f, 0.431557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.397033f, 0.431557f, 0.000000f), new Vector3(0.441147f, 0.433475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.433475f, 0.000000f), new Vector3(0.540885f, 0.450737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.450737f, 0.000000f), new Vector3(0.586918f, 0.468000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.586918f, 0.468000f, 0.000000f), new Vector3(0.680901f, 0.515951f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.680901f, 0.515951f, 0.000000f), new Vector3(0.678983f, 0.492934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678983f, 0.492934f, 0.000000f), new Vector3(0.657885f, 0.387442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.657885f, 0.387442f, 0.000000f), new Vector3(0.619524f, 0.295377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.619524f, 0.295377f, 0.000000f), new Vector3(0.563901f, 0.220574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.563901f, 0.220574f, 0.000000f), new Vector3(0.483344f, 0.159197f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.483344f, 0.159197f, 0.000000f), new Vector3(0.393196f, 0.124672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.393196f, 0.124672f, 0.000000f), new Vector3(0.285787f, 0.115082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.115082f, 0.000000f), new Vector3(0.189885f, 0.124672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.189885f, 0.124672f, 0.000000f), new Vector3(0.124672f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.124672f, 0.147688f, 0.000000f), new Vector3(0.115082f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.115082f, 0.147688f, 0.000000f), new Vector3(0.115082f, -0.005754f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.684737f, 0.663639f, 0.000000f), new Vector3(0.684737f, 0.705836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 0.705836f, 0.000000f), new Vector3(0.678983f, 0.818999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678983f, 0.818999f, 0.000000f), new Vector3(0.659803f, 0.907229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.659803f, 0.907229f, 0.000000f), new Vector3(0.611852f, 0.999294f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611852f, 0.999294f, 0.000000f), new Vector3(0.588836f, 1.024229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.588836f, 1.024229f, 0.000000f), new Vector3(0.508278f, 1.076016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, 1.076016f, 0.000000f), new Vector3(0.412377f, 1.091360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 1.091360f, 0.000000f), new Vector3(0.310721f, 1.074098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.310721f, 1.074098f, 0.000000f), new Vector3(0.232082f, 1.020393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232082f, 1.020393f, 0.000000f), new Vector3(0.182213f, 0.939835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.939835f, 0.000000f), new Vector3(0.163033f, 0.838180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 0.838180f, 0.000000f), new Vector3(0.163033f, 0.815163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 0.815163f, 0.000000f), new Vector3(0.176459f, 0.711590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176459f, 0.711590f, 0.000000f), new Vector3(0.184131f, 0.690491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.184131f, 0.690491f, 0.000000f), new Vector3(0.247426f, 0.613770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.247426f, 0.613770f, 0.000000f), new Vector3(0.257016f, 0.606098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.257016f, 0.606098f, 0.000000f), new Vector3(0.337574f, 0.573491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337574f, 0.573491f, 0.000000f), new Vector3(0.433475f, 0.563901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 0.563901f, 0.000000f), new Vector3(0.535131f, 0.575409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.535131f, 0.575409f, 0.000000f), new Vector3(0.561983f, 0.581164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.561983f, 0.581164f, 0.000000f), new Vector3(0.682819f, 0.629114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.682819f, 0.629114f, 0.000000f), new Vector3(0.684737f, 0.663639f, 0.000000f)));

            return b0;
        }
        #endregion

        // --------------------------------------- MATHEMATICAL SYMBOLS ------------------------------------------- //

        #region verdana_12_regular__Angle
        private static List<Line3> getCharAngle()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.052897f, -0.099917f, 0.000000f), new Vector3(0.772885f, -0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772885f, -0.099917f, 0.000000f), new Vector3(0.816966f, -0.082284f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.816966f, -0.082284f, 0.000000f), new Vector3(0.834599f, -0.041142f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.834599f, -0.041142f, 0.000000f), new Vector3(0.816966f, 0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.816966f, 0.002939f, 0.000000f), new Vector3(0.772885f, 0.020571f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772885f, 0.020571f, 0.000000f), new Vector3(0.149875f, 0.020571f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.020571f, 0.000000f), new Vector3(0.617133f, 1.013861f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617133f, 1.013861f, 0.000000f), new Vector3(0.620072f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.620072f, 1.060881f, 0.000000f), new Vector3(0.587746f, 1.093207f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 1.093207f, 0.000000f), new Vector3(0.543665f, 1.096146f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.543665f, 1.096146f, 0.000000f), new Vector3(0.508400f, 1.063820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, 1.063820f, 0.000000f), new Vector3(0.000000f, -0.014694f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.014694f, 0.000000f), new Vector3(0.002939f, -0.073468f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.002939f, -0.073468f, 0.000000f), new Vector3(0.023510f, -0.094039f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023510f, -0.094039f, 0.000000f), new Vector3(0.052897f, -0.099917f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Braces_Curly
        private static List<Line3> getCharBraceCurly_Left()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.755704f, -0.205229f, 0.000000f), new Vector3(0.665557f, -0.205229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, -0.205229f, 0.000000f), new Vector3(0.585000f, -0.195639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, -0.195639f, 0.000000f), new Vector3(0.506360f, -0.153443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.506360f, -0.153443f, 0.000000f), new Vector3(0.471836f, -0.099738f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.471836f, -0.099738f, 0.000000f), new Vector3(0.456492f, 0.009590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.009590f, 0.000000f), new Vector3(0.456492f, 0.149606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.149606f, 0.000000f), new Vector3(0.441147f, 0.253180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.253180f, 0.000000f), new Vector3(0.389360f, 0.345246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.389360f, 0.345246f, 0.000000f), new Vector3(0.316475f, 0.408541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 0.408541f, 0.000000f), new Vector3(0.222492f, 0.458410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.222492f, 0.458410f, 0.000000f), new Vector3(0.222492f, 0.477590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.222492f, 0.477590f, 0.000000f), new Vector3(0.324147f, 0.533213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.324147f, 0.533213f, 0.000000f), new Vector3(0.393196f, 0.598426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.393196f, 0.598426f, 0.000000f), new Vector3(0.441147f, 0.684737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.684737f, 0.000000f), new Vector3(0.456492f, 0.786393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.786393f, 0.000000f), new Vector3(0.456492f, 0.928327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.928327f, 0.000000f), new Vector3(0.466082f, 1.012721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.466082f, 1.012721f, 0.000000f), new Vector3(0.506360f, 1.091360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.506360f, 1.091360f, 0.000000f), new Vector3(0.556229f, 1.125885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, 1.125885f, 0.000000f), new Vector3(0.665557f, 1.143147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 1.143147f, 0.000000f), new Vector3(0.755704f, 1.143147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 1.143147f, 0.000000f), new Vector3(0.755704f, 1.254393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 1.254393f, 0.000000f), new Vector3(0.634868f, 1.254393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634868f, 1.254393f, 0.000000f), new Vector3(0.579246f, 1.250557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.579246f, 1.250557f, 0.000000f), new Vector3(0.481426f, 1.225622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481426f, 1.225622f, 0.000000f), new Vector3(0.402787f, 1.173835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402787f, 1.173835f, 0.000000f), new Vector3(0.366344f, 1.135475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.366344f, 1.135475f, 0.000000f), new Vector3(0.326065f, 1.049163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326065f, 1.049163f, 0.000000f), new Vector3(0.312639f, 0.941754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.941754f, 0.000000f), new Vector3(0.312639f, 0.820917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.820917f, 0.000000f), new Vector3(0.310721f, 0.790229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.310721f, 0.790229f, 0.000000f), new Vector3(0.291541f, 0.688573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.291541f, 0.688573f, 0.000000f), new Vector3(0.245508f, 0.609934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.245508f, 0.609934f, 0.000000f), new Vector3(0.228246f, 0.592672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.228246f, 0.592672f, 0.000000f), new Vector3(0.147688f, 0.546639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.147688f, 0.546639f, 0.000000f), new Vector3(0.040279f, 0.531295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.040279f, 0.531295f, 0.000000f), new Vector3(0.000000f, 0.531295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.531295f, 0.000000f), new Vector3(0.000000f, 0.406623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.406623f, 0.000000f), new Vector3(0.040279f, 0.406623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.040279f, 0.406623f, 0.000000f), new Vector3(0.070967f, 0.404705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070967f, 0.404705f, 0.000000f), new Vector3(0.172623f, 0.381688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.381688f, 0.000000f), new Vector3(0.245508f, 0.329901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.245508f, 0.329901f, 0.000000f), new Vector3(0.260852f, 0.310721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.260852f, 0.310721f, 0.000000f), new Vector3(0.299213f, 0.224410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299213f, 0.224410f, 0.000000f), new Vector3(0.312639f, 0.115082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.115082f, 0.000000f), new Vector3(0.312639f, -0.003836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, -0.003836f, 0.000000f), new Vector3(0.316475f, -0.067131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, -0.067131f, 0.000000f), new Vector3(0.345246f, -0.163033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.345246f, -0.163033f, 0.000000f), new Vector3(0.402787f, -0.235918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402787f, -0.235918f, 0.000000f), new Vector3(0.441147f, -0.264688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, -0.264688f, 0.000000f), new Vector3(0.529377f, -0.303049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, -0.303049f, 0.000000f), new Vector3(0.634868f, -0.316475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634868f, -0.316475f, 0.000000f), new Vector3(0.755704f, -0.316475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, -0.316475f, 0.000000f), new Vector3(0.755704f, -0.205229f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharBraceCurly_Right()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.205229f, 0.000000f), new Vector3(0.090147f, -0.205229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.090147f, -0.205229f, 0.000000f), new Vector3(0.170705f, -0.195639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, -0.195639f, 0.000000f), new Vector3(0.249344f, -0.153443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, -0.153443f, 0.000000f), new Vector3(0.283869f, -0.099738f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.283869f, -0.099738f, 0.000000f), new Vector3(0.299213f, 0.009590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299213f, 0.009590f, 0.000000f), new Vector3(0.299213f, 0.149606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299213f, 0.149606f, 0.000000f), new Vector3(0.314557f, 0.253180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.314557f, 0.253180f, 0.000000f), new Vector3(0.366344f, 0.345246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.366344f, 0.345246f, 0.000000f), new Vector3(0.439229f, 0.408541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 0.408541f, 0.000000f), new Vector3(0.533213f, 0.458410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.533213f, 0.458410f, 0.000000f), new Vector3(0.533213f, 0.477590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.533213f, 0.477590f, 0.000000f), new Vector3(0.431557f, 0.533213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.431557f, 0.533213f, 0.000000f), new Vector3(0.362508f, 0.598426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 0.598426f, 0.000000f), new Vector3(0.314557f, 0.684737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.314557f, 0.684737f, 0.000000f), new Vector3(0.299213f, 0.786393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299213f, 0.786393f, 0.000000f), new Vector3(0.299213f, 0.928327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299213f, 0.928327f, 0.000000f), new Vector3(0.289623f, 1.012721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.289623f, 1.012721f, 0.000000f), new Vector3(0.249344f, 1.091360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 1.091360f, 0.000000f), new Vector3(0.199475f, 1.125885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199475f, 1.125885f, 0.000000f), new Vector3(0.090147f, 1.143147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.090147f, 1.143147f, 0.000000f), new Vector3(0.000000f, 1.143147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.143147f, 0.000000f), new Vector3(0.000000f, 1.254393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.254393f, 0.000000f), new Vector3(0.120836f, 1.254393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 1.254393f, 0.000000f), new Vector3(0.176459f, 1.250557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176459f, 1.250557f, 0.000000f), new Vector3(0.274279f, 1.225622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.274279f, 1.225622f, 0.000000f), new Vector3(0.352918f, 1.173835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352918f, 1.173835f, 0.000000f), new Vector3(0.389360f, 1.135475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.389360f, 1.135475f, 0.000000f), new Vector3(0.429639f, 1.049163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429639f, 1.049163f, 0.000000f), new Vector3(0.443065f, 0.941754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 0.941754f, 0.000000f), new Vector3(0.443065f, 0.820917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 0.820917f, 0.000000f), new Vector3(0.444983f, 0.790229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.444983f, 0.790229f, 0.000000f), new Vector3(0.464164f, 0.688573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.464164f, 0.688573f, 0.000000f), new Vector3(0.510196f, 0.609934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.510196f, 0.609934f, 0.000000f), new Vector3(0.527459f, 0.592672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.527459f, 0.592672f, 0.000000f), new Vector3(0.608016f, 0.546639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608016f, 0.546639f, 0.000000f), new Vector3(0.715426f, 0.531295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.715426f, 0.531295f, 0.000000f), new Vector3(0.755704f, 0.531295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 0.531295f, 0.000000f), new Vector3(0.755704f, 0.406623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 0.406623f, 0.000000f), new Vector3(0.715426f, 0.406623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.715426f, 0.406623f, 0.000000f), new Vector3(0.684737f, 0.404705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 0.404705f, 0.000000f), new Vector3(0.583082f, 0.381688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.583082f, 0.381688f, 0.000000f), new Vector3(0.510196f, 0.329901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.510196f, 0.329901f, 0.000000f), new Vector3(0.494852f, 0.310721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.494852f, 0.310721f, 0.000000f), new Vector3(0.456492f, 0.224410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.224410f, 0.000000f), new Vector3(0.443065f, 0.115082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 0.115082f, 0.000000f), new Vector3(0.443065f, -0.003836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, -0.003836f, 0.000000f), new Vector3(0.439229f, -0.067131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, -0.067131f, 0.000000f), new Vector3(0.410459f, -0.163033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.410459f, -0.163033f, 0.000000f), new Vector3(0.352918f, -0.235918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352918f, -0.235918f, 0.000000f), new Vector3(0.314557f, -0.264688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.314557f, -0.264688f, 0.000000f), new Vector3(0.226328f, -0.303049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226328f, -0.303049f, 0.000000f), new Vector3(0.120836f, -0.316475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, -0.316475f, 0.000000f), new Vector3(0.000000f, -0.316475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.316475f, 0.000000f), new Vector3(0.000000f, -0.205229f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Braces_Rect
        private static List<Line3> getCharBraceRect_Left()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.418131f, -0.316475f, 0.000000f), new Vector3(0.418131f, -0.201393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, -0.201393f, 0.000000f), new Vector3(0.140016f, -0.201393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, -0.201393f, 0.000000f), new Vector3(0.140016f, 1.137393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 1.137393f, 0.000000f), new Vector3(0.418131f, 1.137393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 1.137393f, 0.000000f), new Vector3(0.418131f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 1.252475f, 0.000000f), new Vector3(0.000000f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.252475f, 0.000000f), new Vector3(0.000000f, -0.316475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.316475f, 0.000000f), new Vector3(0.418131f, -0.316475f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharBraceRect_Right()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.316475f, 0.000000f), new Vector3(0.418131f, -0.316475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, -0.316475f, 0.000000f), new Vector3(0.418131f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 1.252475f, 0.000000f), new Vector3(0.000000f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.252475f, 0.000000f), new Vector3(0.000000f, 1.137393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.137393f, 0.000000f), new Vector3(0.278115f, 1.137393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.278115f, 1.137393f, 0.000000f), new Vector3(0.278115f, -0.201393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.278115f, -0.201393f, 0.000000f), new Vector3(0.000000f, -0.201393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.201393f, 0.000000f), new Vector3(0.000000f, -0.316475f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Braces_Round
        private static List<Line3> getCharBraceRound_Left()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.301131f, 1.277409f, 0.000000f), new Vector3(0.228246f, 1.187262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.228246f, 1.187262f, 0.000000f), new Vector3(0.170705f, 1.100950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 1.100950f, 0.000000f), new Vector3(0.120836f, 1.012721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 1.012721f, 0.000000f), new Vector3(0.078639f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.078639f, 0.920655f, 0.000000f), new Vector3(0.038361f, 0.795983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.795983f, 0.000000f), new Vector3(0.017262f, 0.696245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017262f, 0.696245f, 0.000000f), new Vector3(0.000000f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.485262f, 0.000000f), new Vector3(0.007672f, 0.337574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.337574f, 0.000000f), new Vector3(0.023016f, 0.235918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023016f, 0.235918f, 0.000000f), new Vector3(0.047951f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047951f, 0.140016f, 0.000000f), new Vector3(0.084393f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.084393f, 0.038361f, 0.000000f), new Vector3(0.126590f, -0.053705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126590f, -0.053705f, 0.000000f), new Vector3(0.178377f, -0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.178377f, -0.141934f, 0.000000f), new Vector3(0.235918f, -0.226328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235918f, -0.226328f, 0.000000f), new Vector3(0.301131f, -0.308803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.301131f, -0.308803f, 0.000000f), new Vector3(0.485262f, -0.308803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, -0.308803f, 0.000000f), new Vector3(0.485262f, -0.299213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, -0.299213f, 0.000000f), new Vector3(0.429639f, -0.245508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429639f, -0.245508f, 0.000000f), new Vector3(0.358672f, -0.159197f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.358672f, -0.159197f, 0.000000f), new Vector3(0.301131f, -0.076721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.301131f, -0.076721f, 0.000000f), new Vector3(0.251262f, 0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.251262f, 0.015344f, 0.000000f), new Vector3(0.239754f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.239754f, 0.038361f, 0.000000f), new Vector3(0.201393f, 0.134262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.201393f, 0.134262f, 0.000000f), new Vector3(0.172623f, 0.232082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.232082f, 0.000000f), new Vector3(0.161115f, 0.278115f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.278115f, 0.000000f), new Vector3(0.147688f, 0.377852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.147688f, 0.377852f, 0.000000f), new Vector3(0.141934f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.141934f, 0.485262f, 0.000000f), new Vector3(0.143852f, 0.535131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 0.535131f, 0.000000f), new Vector3(0.153443f, 0.638705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.153443f, 0.638705f, 0.000000f), new Vector3(0.172623f, 0.736524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.736524f, 0.000000f), new Vector3(0.210983f, 0.863114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, 0.863114f, 0.000000f), new Vector3(0.253180f, 0.960934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.253180f, 0.960934f, 0.000000f), new Vector3(0.304967f, 1.052999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.304967f, 1.052999f, 0.000000f), new Vector3(0.360590f, 1.133557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 1.133557f, 0.000000f), new Vector3(0.414295f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 1.198770f, 0.000000f), new Vector3(0.485262f, 1.269737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, 1.269737f, 0.000000f), new Vector3(0.485262f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, 1.277409f, 0.000000f), new Vector3(0.301131f, 1.277409f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharBraceRound_Right()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.184131f, 1.277409f, 0.000000f), new Vector3(0.257016f, 1.187262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.257016f, 1.187262f, 0.000000f), new Vector3(0.314557f, 1.100950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.314557f, 1.100950f, 0.000000f), new Vector3(0.364426f, 1.012721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.364426f, 1.012721f, 0.000000f), new Vector3(0.406623f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.406623f, 0.920655f, 0.000000f), new Vector3(0.446901f, 0.795983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446901f, 0.795983f, 0.000000f), new Vector3(0.468000f, 0.696245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.468000f, 0.696245f, 0.000000f), new Vector3(0.485262f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, 0.485262f, 0.000000f), new Vector3(0.477590f, 0.337574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.477590f, 0.337574f, 0.000000f), new Vector3(0.462246f, 0.235918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.462246f, 0.235918f, 0.000000f), new Vector3(0.437311f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437311f, 0.140016f, 0.000000f), new Vector3(0.400869f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.400869f, 0.038361f, 0.000000f), new Vector3(0.358672f, -0.053705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.358672f, -0.053705f, 0.000000f), new Vector3(0.306885f, -0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.306885f, -0.141934f, 0.000000f), new Vector3(0.249344f, -0.226328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, -0.226328f, 0.000000f), new Vector3(0.184131f, -0.308803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.184131f, -0.308803f, 0.000000f), new Vector3(0.000000f, -0.308803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.308803f, 0.000000f), new Vector3(0.000000f, -0.299213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.299213f, 0.000000f), new Vector3(0.055623f, -0.245508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.055623f, -0.245508f, 0.000000f), new Vector3(0.126590f, -0.159197f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126590f, -0.159197f, 0.000000f), new Vector3(0.184131f, -0.076721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.184131f, -0.076721f, 0.000000f), new Vector3(0.234000f, 0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.234000f, 0.015344f, 0.000000f), new Vector3(0.245508f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.245508f, 0.038361f, 0.000000f), new Vector3(0.283869f, 0.134262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.283869f, 0.134262f, 0.000000f), new Vector3(0.312639f, 0.232082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.232082f, 0.000000f), new Vector3(0.324147f, 0.278115f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.324147f, 0.278115f, 0.000000f), new Vector3(0.337574f, 0.377852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337574f, 0.377852f, 0.000000f), new Vector3(0.343328f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.485262f, 0.000000f), new Vector3(0.341410f, 0.535131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.341410f, 0.535131f, 0.000000f), new Vector3(0.331819f, 0.638705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.331819f, 0.638705f, 0.000000f), new Vector3(0.312639f, 0.736524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.736524f, 0.000000f), new Vector3(0.274279f, 0.863114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.274279f, 0.863114f, 0.000000f), new Vector3(0.232082f, 0.960934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232082f, 0.960934f, 0.000000f), new Vector3(0.180295f, 1.052999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.180295f, 1.052999f, 0.000000f), new Vector3(0.124672f, 1.133557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.124672f, 1.133557f, 0.000000f), new Vector3(0.070967f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070967f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.269737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.269737f, 0.000000f), new Vector3(0.000000f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.277409f, 0.000000f), new Vector3(0.184131f, 1.277409f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Diameter
        private static List<Line3> getCharDiameter()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.002939f, -0.217466f, 0.000000f), new Vector3(0.108733f, -0.217466f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.108733f, -0.217466f, 0.000000f), new Vector3(0.238037f, -0.035265f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.238037f, -0.035265f, 0.000000f), new Vector3(0.273302f, -0.055836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.273302f, -0.055836f, 0.000000f), new Vector3(0.382035f, -0.102855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.382035f, -0.102855f, 0.000000f), new Vector3(0.434932f, -0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.434932f, -0.114610f, 0.000000f), new Vector3(0.555420f, -0.126365f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, -0.126365f, 0.000000f), new Vector3(0.678846f, -0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678846f, -0.114610f, 0.000000f), new Vector3(0.787579f, -0.085223f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, -0.085223f, 0.000000f), new Vector3(0.872802f, -0.038203f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.872802f, -0.038203f, 0.000000f), new Vector3(0.960964f, 0.038203f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.960964f, 0.038203f, 0.000000f), new Vector3(1.022677f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.022677f, 0.123427f, 0.000000f), new Vector3(1.072636f, 0.235098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.072636f, 0.235098f, 0.000000f), new Vector3(1.104962f, 0.376157f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.104962f, 0.376157f, 0.000000f), new Vector3(1.113778f, 0.499584f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.113778f, 0.499584f, 0.000000f), new Vector3(1.110839f, 0.555420f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.110839f, 0.555420f, 0.000000f), new Vector3(1.096146f, 0.675907f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.096146f, 0.675907f, 0.000000f), new Vector3(1.069697f, 0.781702f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.069697f, 0.781702f, 0.000000f), new Vector3(1.013861f, 0.893373f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.013861f, 0.893373f, 0.000000f), new Vector3(0.940393f, 0.981535f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.940393f, 0.981535f, 0.000000f), new Vector3(1.093207f, 1.199001f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.093207f, 1.199001f, 0.000000f), new Vector3(0.987413f, 1.199001f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.987413f, 1.199001f, 0.000000f), new Vector3(0.872802f, 1.037371f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.872802f, 1.037371f, 0.000000f), new Vector3(0.837537f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.837537f, 1.057942f, 0.000000f), new Vector3(0.728805f, 1.102023f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728805f, 1.102023f, 0.000000f), new Vector3(0.675907f, 1.113778f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675907f, 1.113778f, 0.000000f), new Vector3(0.555420f, 1.125533f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 1.125533f, 0.000000f), new Vector3(0.437870f, 1.113778f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, 1.113778f, 0.000000f), new Vector3(0.326199f, 1.081452f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326199f, 1.081452f, 0.000000f), new Vector3(0.238037f, 1.037371f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.238037f, 1.037371f, 0.000000f), new Vector3(0.149875f, 0.960964f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.960964f, 0.000000f), new Vector3(0.085223f, 0.872802f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.085223f, 0.872802f, 0.000000f), new Vector3(0.038203f, 0.764069f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038203f, 0.764069f, 0.000000f), new Vector3(0.005877f, 0.623010f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.623010f, 0.000000f), new Vector3(0.000000f, 0.499584f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.499584f, 0.000000f), new Vector3(0.000000f, 0.446687f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.446687f, 0.000000f), new Vector3(0.014694f, 0.326199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.014694f, 0.326199f, 0.000000f), new Vector3(0.044081f, 0.220405f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.044081f, 0.220405f, 0.000000f), new Vector3(0.096978f, 0.108733f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.096978f, 0.108733f, 0.000000f), new Vector3(0.167507f, 0.017632f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.167507f, 0.017632f, 0.000000f), new Vector3(0.002939f, -0.217466f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.320321f, 0.085223f, 0.000000f), new Vector3(0.423177f, 0.029387f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423177f, 0.029387f, 0.000000f), new Vector3(0.540726f, 0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, 0.011755f, 0.000000f), new Vector3(0.672969f, 0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.672969f, 0.026449f, 0.000000f), new Vector3(0.719988f, 0.044081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719988f, 0.044081f, 0.000000f), new Vector3(0.816966f, 0.108733f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.816966f, 0.108733f, 0.000000f), new Vector3(0.843415f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843415f, 0.135181f, 0.000000f), new Vector3(0.902189f, 0.235098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.902189f, 0.235098f, 0.000000f), new Vector3(0.922761f, 0.293873f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.922761f, 0.293873f, 0.000000f), new Vector3(0.943332f, 0.405544f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943332f, 0.405544f, 0.000000f), new Vector3(0.949209f, 0.499584f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.949209f, 0.499584f, 0.000000f), new Vector3(0.940393f, 0.620072f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.940393f, 0.620072f, 0.000000f), new Vector3(0.925699f, 0.702356f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.925699f, 0.702356f, 0.000000f), new Vector3(0.881618f, 0.816966f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.881618f, 0.816966f, 0.000000f), new Vector3(0.855170f, 0.855170f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.855170f, 0.855170f, 0.000000f), new Vector3(0.320321f, 0.085223f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.255669f, 0.143998f, 0.000000f), new Vector3(0.790518f, 0.913944f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.913944f, 0.000000f), new Vector3(0.687662f, 0.969780f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.687662f, 0.969780f, 0.000000f), new Vector3(0.570113f, 0.987413f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.570113f, 0.987413f, 0.000000f), new Vector3(0.437870f, 0.972719f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, 0.972719f, 0.000000f), new Vector3(0.393790f, 0.958025f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.393790f, 0.958025f, 0.000000f), new Vector3(0.296812f, 0.893373f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.296812f, 0.893373f, 0.000000f), new Vector3(0.270363f, 0.863986f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270363f, 0.863986f, 0.000000f), new Vector3(0.211588f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.211588f, 0.767008f, 0.000000f), new Vector3(0.191017f, 0.711172f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.711172f, 0.000000f), new Vector3(0.167507f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.167507f, 0.599500f, 0.000000f), new Vector3(0.161630f, 0.499584f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.499584f, 0.000000f), new Vector3(0.170446f, 0.376157f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170446f, 0.376157f, 0.000000f), new Vector3(0.185140f, 0.296812f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.185140f, 0.296812f, 0.000000f), new Vector3(0.232160f, 0.185140f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232160f, 0.185140f, 0.000000f), new Vector3(0.255669f, 0.143998f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Equal
        private static List<Line3> getCharEqual()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.757622f, 0.000000f), new Vector3(0.955180f, 0.757622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955180f, 0.757622f, 0.000000f), new Vector3(0.955180f, 0.629114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955180f, 0.629114f, 0.000000f), new Vector3(0.000000f, 0.629114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.629114f, 0.000000f), new Vector3(0.000000f, 0.757622f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.000000f, 0.420049f, 0.000000f), new Vector3(0.955180f, 0.420049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955180f, 0.420049f, 0.000000f), new Vector3(0.955180f, 0.291541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955180f, 0.291541f, 0.000000f), new Vector3(0.000000f, 0.291541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.291541f, 0.000000f), new Vector3(0.000000f, 0.420049f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Fraction_Slash
        private static List<Line3> getCharFractionSlash()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.220574f, 0.000000f), new Vector3(0.138098f, -0.220574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.138098f, -0.220574f, 0.000000f), new Vector3(0.715426f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.715426f, 1.277409f, 0.000000f), new Vector3(0.573491f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573491f, 1.277409f, 0.000000f), new Vector3(0.000000f, -0.220574f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Greater_or_Equal
        private static List<Line3> getCharGreaterOrEqual()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.040557f, 0.000000f), new Vector3(0.928638f, 0.040557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.040557f, 0.000000f), new Vector3(0.928638f, 0.169862f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.169862f, 0.000000f), new Vector3(0.000000f, 0.169862f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.169862f, 0.000000f), new Vector3(0.000000f, 0.040557f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.000000f, 0.284472f, 0.000000f), new Vector3(0.928638f, 0.675323f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.675323f, 0.000000f), new Vector3(0.928638f, 0.775239f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.775239f, 0.000000f), new Vector3(0.000000f, 1.163152f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.163152f, 0.000000f), new Vector3(0.000000f, 1.016215f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.016215f, 0.000000f), new Vector3(0.717050f, 0.725281f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, 0.725281f, 0.000000f), new Vector3(0.000000f, 0.434347f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.434347f, 0.000000f), new Vector3(0.000000f, 0.284472f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Greater_Than
        private static List<Line3> getCharGreater()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.062854f, 0.000000f), new Vector3(0.928327f, 0.475230f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928327f, 0.475230f, 0.000000f), new Vector3(0.928327f, 0.574968f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928327f, 0.574968f, 0.000000f), new Vector3(0.000000f, 0.989263f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.989263f, 0.000000f), new Vector3(0.000000f, 0.843493f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.843493f, 0.000000f), new Vector3(0.732688f, 0.525099f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.732688f, 0.525099f, 0.000000f), new Vector3(0.000000f, 0.206706f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.206706f, 0.000000f), new Vector3(0.000000f, 0.062854f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Identity
        private static List<Line3> getCharIdentity()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.061713f, -0.041142f, 0.000000f), new Vector3(0.540726f, -0.041142f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, -0.041142f, 0.000000f), new Vector3(0.599500f, 0.020571f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.599500f, 0.020571f, 0.000000f), new Vector3(0.540726f, 0.079346f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, 0.079346f, 0.000000f), new Vector3(0.061713f, 0.079346f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.061713f, 0.079346f, 0.000000f), new Vector3(0.000000f, 0.020571f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.020571f, 0.000000f), new Vector3(0.061713f, -0.041142f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.061713f, 0.258608f, 0.000000f), new Vector3(0.540726f, 0.258608f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, 0.258608f, 0.000000f), new Vector3(0.599500f, 0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.599500f, 0.320321f, 0.000000f), new Vector3(0.540726f, 0.379096f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, 0.379096f, 0.000000f), new Vector3(0.061713f, 0.379096f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.061713f, 0.379096f, 0.000000f), new Vector3(0.000000f, 0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.320321f, 0.000000f), new Vector3(0.061713f, 0.258608f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.061713f, 0.558358f, 0.000000f), new Vector3(0.540726f, 0.558358f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, 0.558358f, 0.000000f), new Vector3(0.599500f, 0.620072f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.599500f, 0.620072f, 0.000000f), new Vector3(0.540726f, 0.678846f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, 0.678846f, 0.000000f), new Vector3(0.061713f, 0.678846f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.061713f, 0.678846f, 0.000000f), new Vector3(0.000000f, 0.620072f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.620072f, 0.000000f), new Vector3(0.061713f, 0.558358f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Infinity
        private static List<Line3> getCharInfinity()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.434932f, 0.038203f, 0.000000f), new Vector3(0.537787f, 0.085223f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537787f, 0.085223f, 0.000000f), new Vector3(0.617133f, 0.161630f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617133f, 0.161630f, 0.000000f), new Vector3(0.684724f, 0.267424f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684724f, 0.267424f, 0.000000f), new Vector3(0.749376f, 0.164569f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749376f, 0.164569f, 0.000000f), new Vector3(0.831660f, 0.088162f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.831660f, 0.088162f, 0.000000f), new Vector3(0.913944f, 0.044081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.913944f, 0.044081f, 0.000000f), new Vector3(1.028555f, 0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.028555f, 0.026449f, 0.000000f), new Vector3(1.096146f, 0.032326f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.096146f, 0.032326f, 0.000000f), new Vector3(1.204878f, 0.067591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.204878f, 0.067591f, 0.000000f), new Vector3(1.293040f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.293040f, 0.135181f, 0.000000f), new Vector3(1.340060f, 0.202772f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.340060f, 0.202772f, 0.000000f), new Vector3(1.378263f, 0.305628f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.378263f, 0.305628f, 0.000000f), new Vector3(1.390018f, 0.434932f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.390018f, 0.434932f, 0.000000f), new Vector3(1.387080f, 0.511339f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.387080f, 0.511339f, 0.000000f), new Vector3(1.357692f, 0.623010f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.357692f, 0.623010f, 0.000000f), new Vector3(1.298918f, 0.717050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.298918f, 0.717050f, 0.000000f), new Vector3(1.266592f, 0.749376f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.266592f, 0.749376f, 0.000000f), new Vector3(1.172552f, 0.805211f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.172552f, 0.805211f, 0.000000f), new Vector3(1.055003f, 0.825783f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.055003f, 0.825783f, 0.000000f), new Vector3(0.958025f, 0.814028f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.958025f, 0.814028f, 0.000000f), new Vector3(0.855170f, 0.764069f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.855170f, 0.764069f, 0.000000f), new Vector3(0.775824f, 0.687662f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.775824f, 0.687662f, 0.000000f), new Vector3(0.705295f, 0.584807f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.705295f, 0.584807f, 0.000000f), new Vector3(0.643581f, 0.687662f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.643581f, 0.687662f, 0.000000f), new Vector3(0.561297f, 0.764069f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.561297f, 0.764069f, 0.000000f), new Vector3(0.479013f, 0.805211f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479013f, 0.805211f, 0.000000f), new Vector3(0.364402f, 0.825783f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.364402f, 0.825783f, 0.000000f), new Vector3(0.299750f, 0.819905f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299750f, 0.819905f, 0.000000f), new Vector3(0.191017f, 0.784640f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.784640f, 0.000000f), new Vector3(0.102855f, 0.717050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.102855f, 0.717050f, 0.000000f), new Vector3(0.052897f, 0.649459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.052897f, 0.649459f, 0.000000f), new Vector3(0.014694f, 0.543665f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.014694f, 0.543665f, 0.000000f), new Vector3(0.000000f, 0.417299f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.417299f, 0.000000f), new Vector3(0.005877f, 0.340892f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.340892f, 0.000000f), new Vector3(0.035265f, 0.229221f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.035265f, 0.229221f, 0.000000f), new Vector3(0.091101f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.091101f, 0.135181f, 0.000000f), new Vector3(0.126365f, 0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126365f, 0.099917f, 0.000000f), new Vector3(0.220405f, 0.044081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 0.044081f, 0.000000f), new Vector3(0.337954f, 0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337954f, 0.026449f, 0.000000f), new Vector3(0.434932f, 0.038203f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.743498f, 0.364402f, 0.000000f), new Vector3(0.811089f, 0.267424f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811089f, 0.267424f, 0.000000f), new Vector3(0.866925f, 0.223343f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.866925f, 0.223343f, 0.000000f), new Vector3(0.975658f, 0.179262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.975658f, 0.179262f, 0.000000f), new Vector3(1.016800f, 0.176324f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.016800f, 0.176324f, 0.000000f), new Vector3(1.134349f, 0.199833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.134349f, 0.199833f, 0.000000f), new Vector3(1.193124f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.193124f, 0.240976f, 0.000000f), new Vector3(1.243082f, 0.335015f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.243082f, 0.335015f, 0.000000f), new Vector3(1.254837f, 0.434932f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.254837f, 0.434932f, 0.000000f), new Vector3(1.234266f, 0.552481f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.234266f, 0.552481f, 0.000000f), new Vector3(1.204878f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.204878f, 0.608317f, 0.000000f), new Vector3(1.110839f, 0.667091f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.110839f, 0.667091f, 0.000000f), new Vector3(1.057942f, 0.675907f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.057942f, 0.675907f, 0.000000f), new Vector3(0.943332f, 0.649459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943332f, 0.649459f, 0.000000f), new Vector3(0.858109f, 0.573052f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.858109f, 0.573052f, 0.000000f), new Vector3(0.778763f, 0.440809f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778763f, 0.440809f, 0.000000f), new Vector3(0.743498f, 0.364402f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.649459f, 0.487829f, 0.000000f), new Vector3(0.578929f, 0.584807f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.578929f, 0.584807f, 0.000000f), new Vector3(0.526032f, 0.628888f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.526032f, 0.628888f, 0.000000f), new Vector3(0.414361f, 0.672969f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414361f, 0.672969f, 0.000000f), new Vector3(0.373218f, 0.675907f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.373218f, 0.675907f, 0.000000f), new Vector3(0.258608f, 0.652398f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.258608f, 0.652398f, 0.000000f), new Vector3(0.199833f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199833f, 0.608317f, 0.000000f), new Vector3(0.149875f, 0.514277f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.514277f, 0.000000f), new Vector3(0.138120f, 0.417299f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.138120f, 0.417299f, 0.000000f), new Vector3(0.158691f, 0.296812f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.296812f, 0.000000f), new Vector3(0.188079f, 0.243914f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.188079f, 0.243914f, 0.000000f), new Vector3(0.279179f, 0.182201f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.279179f, 0.182201f, 0.000000f), new Vector3(0.335015f, 0.176324f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335015f, 0.176324f, 0.000000f), new Vector3(0.452564f, 0.199833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452564f, 0.199833f, 0.000000f), new Vector3(0.473135f, 0.214527f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473135f, 0.214527f, 0.000000f), new Vector3(0.549542f, 0.299750f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.549542f, 0.299750f, 0.000000f), new Vector3(0.570113f, 0.329138f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.570113f, 0.329138f, 0.000000f), new Vector3(0.611255f, 0.405544f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611255f, 0.405544f, 0.000000f), new Vector3(0.649459f, 0.487829f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Integral
        private static List<Line3> getCharIntegral()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.423177f, 0.000000f), new Vector3(0.085223f, -0.434932f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.085223f, -0.434932f, 0.000000f), new Vector3(0.202772f, -0.440809f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.202772f, -0.440809f, 0.000000f), new Vector3(0.317383f, -0.414361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.317383f, -0.414361f, 0.000000f), new Vector3(0.405544f, -0.352647f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.405544f, -0.352647f, 0.000000f), new Vector3(0.429054f, -0.323260f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429054f, -0.323260f, 0.000000f), new Vector3(0.473135f, -0.226282f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473135f, -0.226282f, 0.000000f), new Vector3(0.490768f, -0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.490768f, -0.099917f, 0.000000f), new Vector3(0.490768f, 0.805211f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.490768f, 0.805211f, 0.000000f), new Vector3(0.493706f, 0.881618f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.493706f, 0.881618f, 0.000000f), new Vector3(0.531910f, 0.975658f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531910f, 0.975658f, 0.000000f), new Vector3(0.561297f, 0.999168f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.561297f, 0.999168f, 0.000000f), new Vector3(0.678846f, 1.025616f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678846f, 1.025616f, 0.000000f), new Vector3(0.749376f, 1.016800f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749376f, 1.016800f, 0.000000f), new Vector3(0.822844f, 1.002106f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.822844f, 1.002106f, 0.000000f), new Vector3(0.828721f, 1.002106f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.828721f, 1.002106f, 0.000000f), new Vector3(0.828721f, 1.146104f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.828721f, 1.146104f, 0.000000f), new Vector3(0.746437f, 1.157859f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746437f, 1.157859f, 0.000000f), new Vector3(0.658275f, 1.160798f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.658275f, 1.160798f, 0.000000f), new Vector3(0.625949f, 1.160798f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625949f, 1.160798f, 0.000000f), new Vector3(0.511339f, 1.134349f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 1.134349f, 0.000000f), new Vector3(0.423177f, 1.072636f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423177f, 1.072636f, 0.000000f), new Vector3(0.396728f, 1.043248f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.396728f, 1.043248f, 0.000000f), new Vector3(0.352647f, 0.946270f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352647f, 0.946270f, 0.000000f), new Vector3(0.337954f, 0.819905f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337954f, 0.819905f, 0.000000f), new Vector3(0.337954f, -0.085223f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337954f, -0.085223f, 0.000000f), new Vector3(0.332076f, -0.161630f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.332076f, -0.161630f, 0.000000f), new Vector3(0.296812f, -0.255669f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.296812f, -0.255669f, 0.000000f), new Vector3(0.267424f, -0.279179f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.267424f, -0.279179f, 0.000000f), new Vector3(0.149875f, -0.305628f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, -0.305628f, 0.000000f), new Vector3(0.079346f, -0.296812f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.079346f, -0.296812f, 0.000000f), new Vector3(0.008816f, -0.282118f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.008816f, -0.282118f, 0.000000f), new Vector3(0.000000f, -0.282118f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.282118f, 0.000000f), new Vector3(0.000000f, -0.423177f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Less_or_Equal
        private static List<Line3> getCharLessOrEqual()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.040557f, 0.000000f), new Vector3(0.928638f, 0.040557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.040557f, 0.000000f), new Vector3(0.928638f, 0.169862f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.169862f, 0.000000f), new Vector3(0.000000f, 0.169862f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.169862f, 0.000000f), new Vector3(0.000000f, 0.040557f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.928638f, 0.284472f, 0.000000f), new Vector3(0.928638f, 0.434347f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.434347f, 0.000000f), new Vector3(0.214527f, 0.725281f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.214527f, 0.725281f, 0.000000f), new Vector3(0.928638f, 1.016215f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 1.016215f, 0.000000f), new Vector3(0.928638f, 1.163152f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 1.163152f, 0.000000f), new Vector3(0.000000f, 0.775239f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.775239f, 0.000000f), new Vector3(0.000000f, 0.675323f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.675323f, 0.000000f), new Vector3(0.928638f, 0.284472f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Less_Than
        private static List<Line3> getCharLess()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.930245f, 0.061377f, 0.000000f), new Vector3(0.930245f, 0.205229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.930245f, 0.205229f, 0.000000f), new Vector3(0.197557f, 0.523623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.523623f, 0.000000f), new Vector3(0.930245f, 0.842016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.930245f, 0.842016f, 0.000000f), new Vector3(0.930245f, 0.987786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.930245f, 0.987786f, 0.000000f), new Vector3(0.000000f, 0.573491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.573491f, 0.000000f), new Vector3(0.000000f, 0.473754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.473754f, 0.000000f), new Vector3(0.930245f, 0.061377f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Minus
        private static List<Line3> getCharMinus()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.475672f, 0.000000f), new Vector3(0.502524f, 0.475672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502524f, 0.475672f, 0.000000f), new Vector3(0.502524f, 0.621442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502524f, 0.621442f, 0.000000f), new Vector3(0.000000f, 0.621442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.621442f, 0.000000f), new Vector3(0.000000f, 0.475672f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Not_Equal
        private static List<Line3> getCharNotEqual()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.191017f, 0.000000f), new Vector3(0.343831f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343831f, 0.191017f, 0.000000f), new Vector3(0.258608f, -0.079346f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.258608f, -0.079346f, 0.000000f), new Vector3(0.382035f, -0.079346f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.382035f, -0.079346f, 0.000000f), new Vector3(0.467258f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.467258f, 0.191017f, 0.000000f), new Vector3(0.955087f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955087f, 0.191017f, 0.000000f), new Vector3(0.955087f, 0.317383f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955087f, 0.317383f, 0.000000f), new Vector3(0.505461f, 0.317383f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.505461f, 0.317383f, 0.000000f), new Vector3(0.573052f, 0.531910f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573052f, 0.531910f, 0.000000f), new Vector3(0.955087f, 0.531910f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955087f, 0.531910f, 0.000000f), new Vector3(0.955087f, 0.658275f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955087f, 0.658275f, 0.000000f), new Vector3(0.611255f, 0.658275f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611255f, 0.658275f, 0.000000f), new Vector3(0.696479f, 0.928638f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.696479f, 0.928638f, 0.000000f), new Vector3(0.573052f, 0.928638f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573052f, 0.928638f, 0.000000f), new Vector3(0.487829f, 0.658275f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.487829f, 0.658275f, 0.000000f), new Vector3(0.000000f, 0.658275f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.658275f, 0.000000f), new Vector3(0.000000f, 0.531910f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.531910f, 0.000000f), new Vector3(0.449625f, 0.531910f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.449625f, 0.531910f, 0.000000f), new Vector3(0.382035f, 0.317383f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.382035f, 0.317383f, 0.000000f), new Vector3(0.000000f, 0.317383f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.317383f, 0.000000f), new Vector3(0.000000f, 0.191017f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Plus
        private static List<Line3> getCharPlus()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.439229f, 0.485262f, 0.000000f), new Vector3(0.439229f, 0.046033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 0.046033f, 0.000000f), new Vector3(0.571573f, 0.046033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.571573f, 0.046033f, 0.000000f), new Vector3(0.571573f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.571573f, 0.485262f, 0.000000f), new Vector3(1.010803f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.010803f, 0.485262f, 0.000000f), new Vector3(1.010803f, 0.613770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.010803f, 0.613770f, 0.000000f), new Vector3(0.571573f, 0.613770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.571573f, 0.613770f, 0.000000f), new Vector3(0.571573f, 1.052999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.571573f, 1.052999f, 0.000000f), new Vector3(0.439229f, 1.052999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 1.052999f, 0.000000f), new Vector3(0.439229f, 0.613770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 0.613770f, 0.000000f), new Vector3(0.000000f, 0.613770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.613770f, 0.000000f), new Vector3(0.000000f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.485262f, 0.000000f), new Vector3(0.439229f, 0.485262f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Plus_Minus
        private static List<Line3> getCharPlusMinus()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.044081f, 0.000000f), new Vector3(1.002106f, 0.044081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.002106f, 0.044081f, 0.000000f), new Vector3(1.002106f, 0.170446f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.002106f, 0.170446f, 0.000000f), new Vector3(0.567174f, 0.170446f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.170446f, 0.000000f), new Vector3(0.567174f, 0.511339f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.511339f, 0.000000f), new Vector3(1.002106f, 0.511339f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.002106f, 0.511339f, 0.000000f), new Vector3(1.002106f, 0.637704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.002106f, 0.637704f, 0.000000f), new Vector3(0.567174f, 0.637704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.637704f, 0.000000f), new Vector3(0.567174f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 1.078513f, 0.000000f), new Vector3(0.434932f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.434932f, 1.078513f, 0.000000f), new Vector3(0.434932f, 0.637704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.434932f, 0.637704f, 0.000000f), new Vector3(0.000000f, 0.637704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.637704f, 0.000000f), new Vector3(0.000000f, 0.511339f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.511339f, 0.000000f), new Vector3(0.434932f, 0.511339f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.434932f, 0.511339f, 0.000000f), new Vector3(0.434932f, 0.170446f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.434932f, 0.170446f, 0.000000f), new Vector3(0.000000f, 0.170446f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.170446f, 0.000000f), new Vector3(0.000000f, 0.044081f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Similar
        private static List<Line3> getCharSimilar()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.061713f, 0.000000f), new Vector3(0.126365f, 0.061713f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126365f, 0.061713f, 0.000000f), new Vector3(0.135181f, 0.105794f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.135181f, 0.105794f, 0.000000f), new Vector3(0.176324f, 0.205711f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.205711f, 0.000000f), new Vector3(0.273302f, 0.249792f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.273302f, 0.249792f, 0.000000f), new Vector3(0.361464f, 0.220405f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.361464f, 0.220405f, 0.000000f), new Vector3(0.446687f, 0.164569f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446687f, 0.164569f, 0.000000f), new Vector3(0.549542f, 0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.549542f, 0.099917f, 0.000000f), new Vector3(0.664153f, 0.067591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.664153f, 0.067591f, 0.000000f), new Vector3(0.778763f, 0.085223f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778763f, 0.085223f, 0.000000f), new Vector3(0.866925f, 0.146936f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.866925f, 0.146936f, 0.000000f), new Vector3(0.925699f, 0.249792f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.925699f, 0.249792f, 0.000000f), new Vector3(0.958025f, 0.373218f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.958025f, 0.373218f, 0.000000f), new Vector3(0.831660f, 0.373218f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.831660f, 0.373218f, 0.000000f), new Vector3(0.819905f, 0.335015f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.819905f, 0.335015f, 0.000000f), new Vector3(0.775824f, 0.235098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.775824f, 0.235098f, 0.000000f), new Vector3(0.681785f, 0.196895f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.681785f, 0.196895f, 0.000000f), new Vector3(0.587746f, 0.226282f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 0.226282f, 0.000000f), new Vector3(0.508400f, 0.282118f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, 0.282118f, 0.000000f), new Vector3(0.405544f, 0.349709f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.405544f, 0.349709f, 0.000000f), new Vector3(0.287995f, 0.379096f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287995f, 0.379096f, 0.000000f), new Vector3(0.179262f, 0.358525f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.358525f, 0.000000f), new Vector3(0.088162f, 0.296812f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.088162f, 0.296812f, 0.000000f), new Vector3(0.026449f, 0.188079f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.026449f, 0.188079f, 0.000000f), new Vector3(0.000000f, 0.061713f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.123427f, 0.479013f, 0.000000f), new Vector3(0.135181f, 0.520155f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.135181f, 0.520155f, 0.000000f), new Vector3(0.179262f, 0.617133f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.617133f, 0.000000f), new Vector3(0.276240f, 0.655336f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276240f, 0.655336f, 0.000000f), new Vector3(0.352647f, 0.634765f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352647f, 0.634765f, 0.000000f), new Vector3(0.446687f, 0.570113f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446687f, 0.570113f, 0.000000f), new Vector3(0.546603f, 0.505461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546603f, 0.505461f, 0.000000f), new Vector3(0.667091f, 0.473135f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.667091f, 0.473135f, 0.000000f), new Vector3(0.775824f, 0.490768f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.775824f, 0.490768f, 0.000000f), new Vector3(0.866925f, 0.552481f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.866925f, 0.552481f, 0.000000f), new Vector3(0.878680f, 0.567174f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878680f, 0.567174f, 0.000000f), new Vector3(0.928638f, 0.664153f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.664153f, 0.000000f), new Vector3(0.955087f, 0.790518f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.955087f, 0.790518f, 0.000000f), new Vector3(0.828721f, 0.790518f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.828721f, 0.790518f, 0.000000f), new Vector3(0.819905f, 0.743498f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.819905f, 0.743498f, 0.000000f), new Vector3(0.778763f, 0.646520f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778763f, 0.646520f, 0.000000f), new Vector3(0.681785f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.681785f, 0.599500f, 0.000000f), new Vector3(0.602439f, 0.625949f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602439f, 0.625949f, 0.000000f), new Vector3(0.511339f, 0.687662f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 0.687662f, 0.000000f), new Vector3(0.405544f, 0.755253f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.405544f, 0.755253f, 0.000000f), new Vector3(0.290934f, 0.784640f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.290934f, 0.784640f, 0.000000f), new Vector3(0.179262f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.767008f, 0.000000f), new Vector3(0.091101f, 0.705295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.091101f, 0.705295f, 0.000000f), new Vector3(0.082284f, 0.696479f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.082284f, 0.696479f, 0.000000f), new Vector3(0.029387f, 0.602439f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.029387f, 0.602439f, 0.000000f), new Vector3(0.000000f, 0.479013f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.479013f, 0.000000f), new Vector3(0.123427f, 0.479013f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Square_Root
        private static List<Line3> getCharSquareRoot()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.719404f, 0.000000f), new Vector3(0.191017f, 0.719404f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.719404f, 0.000000f), new Vector3(0.502522f, -0.079930f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502522f, -0.079930f, 0.000000f), new Vector3(0.581868f, -0.079930f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.581868f, -0.079930f, 0.000000f), new Vector3(1.293040f, 1.483473f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.293040f, 1.483473f, 0.000000f), new Vector3(1.157859f, 1.483473f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.157859f, 1.483473f, 0.000000f), new Vector3(0.570113f, 0.169862f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.570113f, 0.169862f, 0.000000f), new Vector3(0.317383f, 0.831075f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.317383f, 0.831075f, 0.000000f), new Vector3(0.000000f, 0.831075f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.831075f, 0.000000f), new Vector3(0.000000f, 0.719404f, 0.000000f)));

            return b0;
        }
        #endregion

        // ---------------------------------------- GENERAL PUNKTUATION ------------------------------------------- //

        #region verdana_12_regular__Ampersand
        private static List<Line3> getCharAmpersand()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.389360f, 0.000000f, 0.000000f), new Vector3(0.491016f, 0.013426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.491016f, 0.013426f, 0.000000f), new Vector3(0.586918f, 0.046033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.586918f, 0.046033f, 0.000000f), new Vector3(0.623360f, 0.065213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.623360f, 0.065213f, 0.000000f), new Vector3(0.700082f, 0.124672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.700082f, 0.124672f, 0.000000f), new Vector3(0.776803f, 0.207147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.776803f, 0.207147f, 0.000000f), new Vector3(0.960934f, 0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.960934f, 0.024934f, 0.000000f), new Vector3(1.160409f, 0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.160409f, 0.024934f, 0.000000f), new Vector3(0.855442f, 0.322229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.855442f, 0.322229f, 0.000000f), new Vector3(0.895721f, 0.408541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.895721f, 0.408541f, 0.000000f), new Vector3(0.924491f, 0.506360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.924491f, 0.506360f, 0.000000f), new Vector3(0.935999f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.935999f, 0.583082f, 0.000000f), new Vector3(0.943672f, 0.680901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943672f, 0.680901f, 0.000000f), new Vector3(0.943672f, 0.757622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943672f, 0.757622f, 0.000000f), new Vector3(0.786393f, 0.757622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.786393f, 0.757622f, 0.000000f), new Vector3(0.788311f, 0.663639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 0.663639f, 0.000000f), new Vector3(0.786393f, 0.563901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.786393f, 0.563901f, 0.000000f), new Vector3(0.778721f, 0.483344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778721f, 0.483344f, 0.000000f), new Vector3(0.763377f, 0.412377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.763377f, 0.412377f, 0.000000f), new Vector3(0.464164f, 0.703918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.464164f, 0.703918f, 0.000000f), new Vector3(0.500606f, 0.717344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.500606f, 0.717344f, 0.000000f), new Vector3(0.586918f, 0.765295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.586918f, 0.765295f, 0.000000f), new Vector3(0.659803f, 0.842016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.659803f, 0.842016f, 0.000000f), new Vector3(0.692409f, 0.922573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.692409f, 0.922573f, 0.000000f), new Vector3(0.702000f, 1.001212f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.702000f, 1.001212f, 0.000000f), new Vector3(0.680901f, 1.102868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.680901f, 1.102868f, 0.000000f), new Vector3(0.617606f, 1.181507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617606f, 1.181507f, 0.000000f), new Vector3(0.586918f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.586918f, 1.200688f, 0.000000f), new Vector3(0.498688f, 1.239048f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.498688f, 1.239048f, 0.000000f), new Vector3(0.391278f, 1.250557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.391278f, 1.250557f, 0.000000f), new Vector3(0.347164f, 1.248639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.347164f, 1.248639f, 0.000000f), new Vector3(0.249344f, 1.225622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 1.225622f, 0.000000f), new Vector3(0.226328f, 1.217950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226328f, 1.217950f, 0.000000f), new Vector3(0.138098f, 1.158491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.138098f, 1.158491f, 0.000000f), new Vector3(0.080557f, 1.074098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.080557f, 1.074098f, 0.000000f), new Vector3(0.057541f, 0.972442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.057541f, 0.972442f, 0.000000f), new Vector3(0.063295f, 0.912983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.063295f, 0.912983f, 0.000000f), new Vector3(0.097820f, 0.822836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.097820f, 0.822836f, 0.000000f), new Vector3(0.138098f, 0.774885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.138098f, 0.774885f, 0.000000f), new Vector3(0.224410f, 0.705836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.224410f, 0.705836f, 0.000000f), new Vector3(0.140016f, 0.648295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 0.648295f, 0.000000f), new Vector3(0.070967f, 0.577327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070967f, 0.577327f, 0.000000f), new Vector3(0.021098f, 0.483344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.021098f, 0.483344f, 0.000000f), new Vector3(0.013426f, 0.458410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.013426f, 0.458410f, 0.000000f), new Vector3(0.000000f, 0.354836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.354836f, 0.000000f), new Vector3(0.011508f, 0.262770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011508f, 0.262770f, 0.000000f), new Vector3(0.047951f, 0.174541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047951f, 0.174541f, 0.000000f), new Vector3(0.111246f, 0.099738f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111246f, 0.099738f, 0.000000f), new Vector3(0.191803f, 0.044115f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.044115f, 0.000000f), new Vector3(0.283869f, 0.011508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.283869f, 0.011508f, 0.000000f), new Vector3(0.389360f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.385524f, 0.761459f, 0.000000f), new Vector3(0.475672f, 0.813245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.475672f, 0.813245f, 0.000000f), new Vector3(0.504442f, 0.843934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.504442f, 0.843934f, 0.000000f), new Vector3(0.540885f, 0.932163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.932163f, 0.000000f), new Vector3(0.544721f, 0.985868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.544721f, 0.985868f, 0.000000f), new Vector3(0.519787f, 1.083688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.519787f, 1.083688f, 0.000000f), new Vector3(0.500606f, 1.106704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.500606f, 1.106704f, 0.000000f), new Vector3(0.412377f, 1.148901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 1.148901f, 0.000000f), new Vector3(0.383606f, 1.150819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.383606f, 1.150819f, 0.000000f), new Vector3(0.287705f, 1.122048f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287705f, 1.122048f, 0.000000f), new Vector3(0.262770f, 1.100950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.262770f, 1.100950f, 0.000000f), new Vector3(0.218656f, 1.012721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.218656f, 1.012721f, 0.000000f), new Vector3(0.216738f, 0.978196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.216738f, 0.978196f, 0.000000f), new Vector3(0.241672f, 0.882295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 0.882295f, 0.000000f), new Vector3(0.308803f, 0.813245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308803f, 0.813245f, 0.000000f), new Vector3(0.385524f, 0.761459f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.702000f, 0.280033f, 0.000000f), new Vector3(0.316475f, 0.655967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 0.655967f, 0.000000f), new Vector3(0.266606f, 0.623360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.266606f, 0.623360f, 0.000000f), new Vector3(0.216738f, 0.569655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.216738f, 0.569655f, 0.000000f), new Vector3(0.180295f, 0.494852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.180295f, 0.494852f, 0.000000f), new Vector3(0.164951f, 0.393196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.393196f, 0.000000f), new Vector3(0.182213f, 0.291541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.291541f, 0.000000f), new Vector3(0.234000f, 0.205229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.234000f, 0.205229f, 0.000000f), new Vector3(0.310721f, 0.153443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.310721f, 0.153443f, 0.000000f), new Vector3(0.412377f, 0.132344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 0.132344f, 0.000000f), new Vector3(0.429639f, 0.132344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429639f, 0.132344f, 0.000000f), new Vector3(0.527459f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.527459f, 0.147688f, 0.000000f), new Vector3(0.577327f, 0.168787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.577327f, 0.168787f, 0.000000f), new Vector3(0.657885f, 0.228246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.657885f, 0.228246f, 0.000000f), new Vector3(0.702000f, 0.280033f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Apostrophe
        private static List<Line3> getCharApostrophe()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 1.350294f, 0.000000f), new Vector3(0.220574f, 1.049163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220574f, 1.049163f, 0.000000f), new Vector3(0.341410f, 1.049163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.341410f, 1.049163f, 0.000000f), new Vector3(0.195639f, 1.350294f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.195639f, 1.350294f, 0.000000f), new Vector3(0.000000f, 1.350294f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Asterisk
        private static List<Line3> getCharAsterisk()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.343328f, 0.820917f, 0.000000f), new Vector3(0.339492f, 0.531295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.339492f, 0.531295f, 0.000000f), new Vector3(0.443065f, 0.531295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 0.531295f, 0.000000f), new Vector3(0.439229f, 0.820917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 0.820917f, 0.000000f), new Vector3(0.730770f, 0.650213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.730770f, 0.650213f, 0.000000f), new Vector3(0.780639f, 0.738442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.780639f, 0.738442f, 0.000000f), new Vector3(0.475672f, 0.905311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.475672f, 0.905311f, 0.000000f), new Vector3(0.780639f, 1.070262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.780639f, 1.070262f, 0.000000f), new Vector3(0.730770f, 1.158491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.730770f, 1.158491f, 0.000000f), new Vector3(0.437311f, 0.987786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437311f, 0.987786f, 0.000000f), new Vector3(0.443065f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 1.277409f, 0.000000f), new Vector3(0.339492f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.339492f, 1.277409f, 0.000000f), new Vector3(0.343328f, 0.987786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.987786f, 0.000000f), new Vector3(0.051787f, 1.160409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.051787f, 1.160409f, 0.000000f), new Vector3(0.000000f, 1.072180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.072180f, 0.000000f), new Vector3(0.306885f, 0.905311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.306885f, 0.905311f, 0.000000f), new Vector3(0.000000f, 0.738442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.738442f, 0.000000f), new Vector3(0.051787f, 0.648295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.051787f, 0.648295f, 0.000000f), new Vector3(0.343328f, 0.820917f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__At
        private static List<Line3> getCharAt()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.884213f, 0.126149f, 0.000000f), new Vector3(1.239048f, 0.126149f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.239048f, 0.126149f, 0.000000f), new Vector3(1.244802f, 0.133821f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.244802f, 0.133821f, 0.000000f), new Vector3(1.294671f, 0.222050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.294671f, 0.222050f, 0.000000f), new Vector3(1.333032f, 0.317952f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.333032f, 0.317952f, 0.000000f), new Vector3(1.357966f, 0.431116f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.357966f, 0.431116f, 0.000000f), new Vector3(1.365638f, 0.534689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.365638f, 0.534689f, 0.000000f), new Vector3(1.361802f, 0.621001f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.361802f, 0.621001f, 0.000000f), new Vector3(1.346458f, 0.722657f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.346458f, 0.722657f, 0.000000f), new Vector3(1.319606f, 0.816640f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.319606f, 0.816640f, 0.000000f), new Vector3(1.300425f, 0.866509f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.300425f, 0.866509f, 0.000000f), new Vector3(1.250557f, 0.956656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.250557f, 0.956656f, 0.000000f), new Vector3(1.189180f, 1.033378f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.189180f, 1.033378f, 0.000000f), new Vector3(1.152737f, 1.067902f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.152737f, 1.067902f, 0.000000f), new Vector3(1.072180f, 1.127361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.072180f, 1.127361f, 0.000000f), new Vector3(0.978196f, 1.173394f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.978196f, 1.173394f, 0.000000f), new Vector3(0.903393f, 1.198328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.903393f, 1.198328f, 0.000000f), new Vector3(0.805573f, 1.217509f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805573f, 1.217509f, 0.000000f), new Vector3(0.698164f, 1.223263f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.698164f, 1.223263f, 0.000000f), new Vector3(0.619524f, 1.219427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.619524f, 1.219427f, 0.000000f), new Vector3(0.519787f, 1.202164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.519787f, 1.202164f, 0.000000f), new Vector3(0.423885f, 1.169558f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 1.169558f, 0.000000f), new Vector3(0.364426f, 1.142705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.364426f, 1.142705f, 0.000000f), new Vector3(0.278115f, 1.087083f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.278115f, 1.087083f, 0.000000f), new Vector3(0.201393f, 1.021870f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.201393f, 1.021870f, 0.000000f), new Vector3(0.159197f, 0.975837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.975837f, 0.000000f), new Vector3(0.101656f, 0.891443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 0.891443f, 0.000000f), new Vector3(0.055623f, 0.799378f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.055623f, 0.799378f, 0.000000f), new Vector3(0.028770f, 0.724575f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.028770f, 0.724575f, 0.000000f), new Vector3(0.007672f, 0.624837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.624837f, 0.000000f), new Vector3(0.000000f, 0.521263f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.521263f, 0.000000f), new Vector3(0.005754f, 0.429198f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005754f, 0.429198f, 0.000000f), new Vector3(0.023016f, 0.327542f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023016f, 0.327542f, 0.000000f), new Vector3(0.051787f, 0.233559f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.051787f, 0.233559f, 0.000000f), new Vector3(0.078639f, 0.176018f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.078639f, 0.176018f, 0.000000f), new Vector3(0.132344f, 0.087788f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.132344f, 0.087788f, 0.000000f), new Vector3(0.195639f, 0.012985f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.195639f, 0.012985f, 0.000000f), new Vector3(0.239754f, -0.027294f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.239754f, -0.027294f, 0.000000f), new Vector3(0.322229f, -0.084835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.322229f, -0.084835f, 0.000000f), new Vector3(0.416213f, -0.130867f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.416213f, -0.130867f, 0.000000f), new Vector3(0.489098f, -0.155802f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.489098f, -0.155802f, 0.000000f), new Vector3(0.588836f, -0.174982f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.588836f, -0.174982f, 0.000000f), new Vector3(0.694327f, -0.182654f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.694327f, -0.182654f, 0.000000f), new Vector3(0.730770f, -0.180736f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.730770f, -0.180736f, 0.000000f), new Vector3(0.874622f, -0.171146f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.874622f, -0.171146f, 0.000000f), new Vector3(0.978196f, -0.153884f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.978196f, -0.153884f, 0.000000f), new Vector3(0.978196f, -0.038802f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.978196f, -0.038802f, 0.000000f), new Vector3(0.939835f, -0.048392f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.939835f, -0.048392f, 0.000000f), new Vector3(0.838180f, -0.067572f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.838180f, -0.067572f, 0.000000f), new Vector3(0.795983f, -0.071408f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.795983f, -0.071408f, 0.000000f), new Vector3(0.659803f, -0.075245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.659803f, -0.075245f, 0.000000f), new Vector3(0.556229f, -0.061818f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, -0.061818f, 0.000000f), new Vector3(0.441147f, -0.025376f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, -0.025376f, 0.000000f), new Vector3(0.352918f, 0.022575f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352918f, 0.022575f, 0.000000f), new Vector3(0.274279f, 0.087788f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.274279f, 0.087788f, 0.000000f), new Vector3(0.260852f, 0.103132f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.260852f, 0.103132f, 0.000000f), new Vector3(0.201393f, 0.183690f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.201393f, 0.183690f, 0.000000f), new Vector3(0.155361f, 0.277673f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, 0.277673f, 0.000000f), new Vector3(0.120836f, 0.413853f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 0.413853f, 0.000000f), new Vector3(0.115082f, 0.519345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.115082f, 0.519345f, 0.000000f), new Vector3(0.115082f, 0.555788f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.115082f, 0.555788f, 0.000000f), new Vector3(0.128508f, 0.657444f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.128508f, 0.657444f, 0.000000f), new Vector3(0.159197f, 0.753345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.753345f, 0.000000f), new Vector3(0.166869f, 0.774443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.774443f, 0.000000f), new Vector3(0.216738f, 0.864591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.216738f, 0.864591f, 0.000000f), new Vector3(0.280033f, 0.943230f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.280033f, 0.943230f, 0.000000f), new Vector3(0.372098f, 1.019951f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.372098f, 1.019951f, 0.000000f), new Vector3(0.462246f, 1.067902f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.462246f, 1.067902f, 0.000000f), new Vector3(0.496770f, 1.081329f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.496770f, 1.081329f, 0.000000f), new Vector3(0.592672f, 1.106263f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.592672f, 1.106263f, 0.000000f), new Vector3(0.698164f, 1.115853f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.698164f, 1.115853f, 0.000000f), new Vector3(0.738442f, 1.113935f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.738442f, 1.113935f, 0.000000f), new Vector3(0.842016f, 1.102427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.842016f, 1.102427f, 0.000000f), new Vector3(0.935999f, 1.073656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.935999f, 1.073656f, 0.000000f), new Vector3(1.035737f, 1.019951f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.035737f, 1.019951f, 0.000000f), new Vector3(1.118212f, 0.947066f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.118212f, 0.947066f, 0.000000f), new Vector3(1.175753f, 0.864591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.175753f, 0.864591f, 0.000000f), new Vector3(1.219868f, 0.768689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.219868f, 0.768689f, 0.000000f), new Vector3(1.229458f, 0.734165f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.229458f, 0.734165f, 0.000000f), new Vector3(1.248639f, 0.636345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.248639f, 0.636345f, 0.000000f), new Vector3(1.254393f, 0.528935f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.254393f, 0.528935f, 0.000000f), new Vector3(1.252475f, 0.473312f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.252475f, 0.473312f, 0.000000f), new Vector3(1.239048f, 0.367821f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.239048f, 0.367821f, 0.000000f), new Vector3(1.223704f, 0.306444f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.223704f, 0.306444f, 0.000000f), new Vector3(1.187262f, 0.216296f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.187262f, 0.216296f, 0.000000f), new Vector3(0.991622f, 0.216296f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.991622f, 0.216296f, 0.000000f), new Vector3(0.991622f, 0.895279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.991622f, 0.895279f, 0.000000f), new Vector3(0.863114f, 0.895279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.863114f, 0.895279f, 0.000000f), new Vector3(0.863114f, 0.855001f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.863114f, 0.855001f, 0.000000f), new Vector3(0.861196f, 0.856919f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.861196f, 0.856919f, 0.000000f), new Vector3(0.765295f, 0.895279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 0.895279f, 0.000000f), new Vector3(0.661721f, 0.908706f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.661721f, 0.908706f, 0.000000f), new Vector3(0.594590f, 0.901034f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.594590f, 0.901034f, 0.000000f), new Vector3(0.504442f, 0.866509f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.504442f, 0.866509f, 0.000000f), new Vector3(0.416213f, 0.789788f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.416213f, 0.789788f, 0.000000f), new Vector3(0.366344f, 0.711148f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.366344f, 0.711148f, 0.000000f), new Vector3(0.335656f, 0.617165f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335656f, 0.617165f, 0.000000f), new Vector3(0.326065f, 0.507837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326065f, 0.507837f, 0.000000f), new Vector3(0.335656f, 0.394673f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335656f, 0.394673f, 0.000000f), new Vector3(0.362508f, 0.298772f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 0.298772f, 0.000000f), new Vector3(0.406623f, 0.222050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.406623f, 0.222050f, 0.000000f), new Vector3(0.441147f, 0.185608f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.185608f, 0.000000f), new Vector3(0.523623f, 0.135739f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.523623f, 0.135739f, 0.000000f), new Vector3(0.623360f, 0.120395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.623360f, 0.120395f, 0.000000f), new Vector3(0.652131f, 0.120395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652131f, 0.120395f, 0.000000f), new Vector3(0.748032f, 0.145329f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.748032f, 0.145329f, 0.000000f), new Vector3(0.776803f, 0.160673f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.776803f, 0.160673f, 0.000000f), new Vector3(0.863114f, 0.220132f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.863114f, 0.220132f, 0.000000f), new Vector3(0.884213f, 0.126149f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.861196f, 0.329460f, 0.000000f), new Vector3(0.861196f, 0.745673f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.861196f, 0.745673f, 0.000000f), new Vector3(0.771049f, 0.780198f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.771049f, 0.780198f, 0.000000f), new Vector3(0.684737f, 0.789788f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 0.789788f, 0.000000f), new Vector3(0.585000f, 0.768689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 0.768689f, 0.000000f), new Vector3(0.521705f, 0.716902f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.521705f, 0.716902f, 0.000000f), new Vector3(0.479508f, 0.636345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479508f, 0.636345f, 0.000000f), new Vector3(0.464164f, 0.530853f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.464164f, 0.530853f, 0.000000f), new Vector3(0.462246f, 0.511673f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.462246f, 0.511673f, 0.000000f), new Vector3(0.473754f, 0.398509f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, 0.398509f, 0.000000f), new Vector3(0.508278f, 0.316034f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, 0.316034f, 0.000000f), new Vector3(0.583082f, 0.256575f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.583082f, 0.256575f, 0.000000f), new Vector3(0.655967f, 0.245067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655967f, 0.245067f, 0.000000f), new Vector3(0.763377f, 0.270001f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.763377f, 0.270001f, 0.000000f), new Vector3(0.861196f, 0.329460f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Backslash
        private static List<Line3> getCharBackslash()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.577327f, -0.245508f, 0.000000f), new Vector3(0.717344f, -0.245508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717344f, -0.245508f, 0.000000f), new Vector3(0.141934f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.141934f, 1.252475f, 0.000000f), new Vector3(0.000000f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.252475f, 0.000000f), new Vector3(0.577327f, -0.245508f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Circumflex
        private static List<Line3> getCharCircumflex()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.548557f, 0.000000f), new Vector3(0.157279f, 0.548557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.157279f, 0.548557f, 0.000000f), new Vector3(0.523623f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.523623f, 1.056835f, 0.000000f), new Vector3(0.889967f, 0.550475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.889967f, 0.550475f, 0.000000f), new Vector3(1.049163f, 0.550475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.049163f, 0.550475f, 0.000000f), new Vector3(0.575409f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575409f, 1.198770f, 0.000000f), new Vector3(0.473754f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.548557f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Colon
        private static List<Line3> getCharColon()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.228246f, 0.000000f), new Vector3(0.191803f, 0.228246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.228246f, 0.000000f), new Vector3(0.191803f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.000000f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.000000f, 0.228246f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.191803f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.899557f, 0.000000f), new Vector3(0.191803f, 0.669393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.669393f, 0.000000f), new Vector3(0.000000f, 0.669393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.669393f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Comma
        private static List<Line3> getCharComma()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.274279f, 0.000000f), new Vector3(0.117000f, -0.274279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.117000f, -0.274279f, 0.000000f), new Vector3(0.345246f, 0.253180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.345246f, 0.253180f, 0.000000f), new Vector3(0.140016f, 0.253180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 0.253180f, 0.000000f), new Vector3(0.000000f, -0.274279f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Degree
        private static List<Line3> getCharDegree()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.349709f, 0.476074f, 0.000000f), new Vector3(0.458442f, 0.505461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458442f, 0.505461f, 0.000000f), new Vector3(0.552481f, 0.570113f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552481f, 0.570113f, 0.000000f), new Vector3(0.570113f, 0.587746f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.570113f, 0.587746f, 0.000000f), new Vector3(0.625949f, 0.684724f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625949f, 0.684724f, 0.000000f), new Vector3(0.646520f, 0.799334f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646520f, 0.799334f, 0.000000f), new Vector3(0.646520f, 0.822844f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646520f, 0.822844f, 0.000000f), new Vector3(0.620072f, 0.934515f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.620072f, 0.934515f, 0.000000f), new Vector3(0.555420f, 1.028555f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 1.028555f, 0.000000f), new Vector3(0.534849f, 1.046187f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.534849f, 1.046187f, 0.000000f), new Vector3(0.437870f, 1.102023f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, 1.102023f, 0.000000f), new Vector3(0.323260f, 1.122594f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.323260f, 1.122594f, 0.000000f), new Vector3(0.299750f, 1.119655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299750f, 1.119655f, 0.000000f), new Vector3(0.188079f, 1.093207f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.188079f, 1.093207f, 0.000000f), new Vector3(0.094039f, 1.028555f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.094039f, 1.028555f, 0.000000f), new Vector3(0.079346f, 1.010922f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.079346f, 1.010922f, 0.000000f), new Vector3(0.020571f, 0.913944f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.020571f, 0.913944f, 0.000000f), new Vector3(0.000000f, 0.799334f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.799334f, 0.000000f), new Vector3(0.002939f, 0.775824f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.002939f, 0.775824f, 0.000000f), new Vector3(0.029387f, 0.664153f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.029387f, 0.664153f, 0.000000f), new Vector3(0.094039f, 0.570113f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.094039f, 0.570113f, 0.000000f), new Vector3(0.111672f, 0.552481f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111672f, 0.552481f, 0.000000f), new Vector3(0.208650f, 0.496645f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.208650f, 0.496645f, 0.000000f), new Vector3(0.323260f, 0.476074f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.323260f, 0.476074f, 0.000000f), new Vector3(0.349709f, 0.476074f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.323260f, 0.605378f, 0.000000f), new Vector3(0.431993f, 0.637704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.431993f, 0.637704f, 0.000000f), new Vector3(0.458442f, 0.661214f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458442f, 0.661214f, 0.000000f), new Vector3(0.511339f, 0.761131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 0.761131f, 0.000000f), new Vector3(0.514277f, 0.799334f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.514277f, 0.799334f, 0.000000f), new Vector3(0.481951f, 0.908067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481951f, 0.908067f, 0.000000f), new Vector3(0.458442f, 0.937454f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458442f, 0.937454f, 0.000000f), new Vector3(0.361464f, 0.990351f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.361464f, 0.990351f, 0.000000f), new Vector3(0.323260f, 0.993290f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.323260f, 0.993290f, 0.000000f), new Vector3(0.214527f, 0.960964f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.214527f, 0.960964f, 0.000000f), new Vector3(0.188079f, 0.937454f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.188079f, 0.937454f, 0.000000f), new Vector3(0.138120f, 0.837537f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.138120f, 0.837537f, 0.000000f), new Vector3(0.132243f, 0.799334f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.132243f, 0.799334f, 0.000000f), new Vector3(0.164569f, 0.690601f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.690601f, 0.000000f), new Vector3(0.188079f, 0.661214f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.188079f, 0.661214f, 0.000000f), new Vector3(0.287995f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287995f, 0.608317f, 0.000000f), new Vector3(0.323260f, 0.605378f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Dollar
        private static List<Line3> getCharDollar()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.092066f, 0.000000f), new Vector3(0.057541f, 0.069049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.057541f, 0.069049f, 0.000000f), new Vector3(0.161115f, 0.042197f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.042197f, 0.000000f), new Vector3(0.257016f, 0.026852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.257016f, 0.026852f, 0.000000f), new Vector3(0.360590f, 0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 0.021098f, 0.000000f), new Vector3(0.360590f, -0.266606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, -0.266606f, 0.000000f), new Vector3(0.456492f, -0.266606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, -0.266606f, 0.000000f), new Vector3(0.456492f, 0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.024934f, 0.000000f), new Vector3(0.537049f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537049f, 0.038361f, 0.000000f), new Vector3(0.632950f, 0.070967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.632950f, 0.070967f, 0.000000f), new Vector3(0.711590f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711590f, 0.122754f, 0.000000f), new Vector3(0.740360f, 0.149606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.740360f, 0.149606f, 0.000000f), new Vector3(0.792147f, 0.232082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.792147f, 0.232082f, 0.000000f), new Vector3(0.809409f, 0.329901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.809409f, 0.329901f, 0.000000f), new Vector3(0.794065f, 0.421967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.794065f, 0.421967f, 0.000000f), new Vector3(0.744196f, 0.502524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.744196f, 0.502524f, 0.000000f), new Vector3(0.661721f, 0.556229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.661721f, 0.556229f, 0.000000f), new Vector3(0.558147f, 0.590754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 0.590754f, 0.000000f), new Vector3(0.456492f, 0.611852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.611852f, 0.000000f), new Vector3(0.456492f, 0.959016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.959016f, 0.000000f), new Vector3(0.538967f, 0.949426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.538967f, 0.949426f, 0.000000f), new Vector3(0.629114f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.629114f, 0.924491f, 0.000000f), new Vector3(0.665557f, 0.907229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 0.907229f, 0.000000f), new Vector3(0.755704f, 0.857360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 0.857360f, 0.000000f), new Vector3(0.767213f, 0.857360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767213f, 0.857360f, 0.000000f), new Vector3(0.767213f, 1.016557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767213f, 1.016557f, 0.000000f), new Vector3(0.726934f, 1.029983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.726934f, 1.029983f, 0.000000f), new Vector3(0.623360f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.623360f, 1.056835f, 0.000000f), new Vector3(0.558147f, 1.068344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 1.068344f, 0.000000f), new Vector3(0.456492f, 1.076016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 1.076016f, 0.000000f), new Vector3(0.456492f, 1.294671f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 1.294671f, 0.000000f), new Vector3(0.360590f, 1.294671f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 1.294671f, 0.000000f), new Vector3(0.360590f, 1.074098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 1.074098f, 0.000000f), new Vector3(0.270442f, 1.060671f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270442f, 1.060671f, 0.000000f), new Vector3(0.176459f, 1.028065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176459f, 1.028065f, 0.000000f), new Vector3(0.099738f, 0.978196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099738f, 0.978196f, 0.000000f), new Vector3(0.076721f, 0.957098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.076721f, 0.957098f, 0.000000f), new Vector3(0.023016f, 0.874622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023016f, 0.874622f, 0.000000f), new Vector3(0.005754f, 0.776803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005754f, 0.776803f, 0.000000f), new Vector3(0.015344f, 0.698164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.015344f, 0.698164f, 0.000000f), new Vector3(0.063295f, 0.613770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.063295f, 0.613770f, 0.000000f), new Vector3(0.143852f, 0.552393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 0.552393f, 0.000000f), new Vector3(0.251262f, 0.514032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.251262f, 0.514032f, 0.000000f), new Vector3(0.360590f, 0.489098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 0.489098f, 0.000000f), new Vector3(0.360590f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 0.140016f, 0.000000f), new Vector3(0.343328f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.140016f, 0.000000f), new Vector3(0.241672f, 0.155361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 0.155361f, 0.000000f), new Vector3(0.143852f, 0.184131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 0.184131f, 0.000000f), new Vector3(0.067131f, 0.220574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.067131f, 0.220574f, 0.000000f), new Vector3(0.013426f, 0.251262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.013426f, 0.251262f, 0.000000f), new Vector3(0.000000f, 0.251262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.251262f, 0.000000f), new Vector3(0.000000f, 0.092066f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.456492f, 0.141934f, 0.000000f), new Vector3(0.561983f, 0.166869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.561983f, 0.166869f, 0.000000f), new Vector3(0.606098f, 0.189885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.189885f, 0.000000f), new Vector3(0.654049f, 0.270442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.654049f, 0.270442f, 0.000000f), new Vector3(0.657885f, 0.308803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.657885f, 0.308803f, 0.000000f), new Vector3(0.627196f, 0.404705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.627196f, 0.404705f, 0.000000f), new Vector3(0.609934f, 0.420049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.609934f, 0.420049f, 0.000000f), new Vector3(0.529377f, 0.458410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, 0.458410f, 0.000000f), new Vector3(0.456492f, 0.475672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 0.475672f, 0.000000f), new Vector3(0.456492f, 0.141934f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.278115f, 0.648295f, 0.000000f), new Vector3(0.360590f, 0.625278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 0.625278f, 0.000000f), new Vector3(0.360590f, 0.959016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 0.959016f, 0.000000f), new Vector3(0.258934f, 0.935999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.258934f, 0.935999f, 0.000000f), new Vector3(0.216738f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.216738f, 0.911065f, 0.000000f), new Vector3(0.161115f, 0.834344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.834344f, 0.000000f), new Vector3(0.157279f, 0.797901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.157279f, 0.797901f, 0.000000f), new Vector3(0.186049f, 0.703918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.186049f, 0.703918f, 0.000000f), new Vector3(0.201393f, 0.688573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.201393f, 0.688573f, 0.000000f), new Vector3(0.278115f, 0.648295f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Exclamation_Mark
        private static List<Line3> getCharExclMark()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.021098f, 0.354836f, 0.000000f), new Vector3(0.159197f, 0.354836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.354836f, 0.000000f), new Vector3(0.180295f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.180295f, 1.223704f, 0.000000f), new Vector3(0.000000f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.223704f, 0.000000f), new Vector3(0.021098f, 0.354836f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.007672f, 0.023016f, 0.000000f), new Vector3(0.170705f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.023016f, 0.000000f), new Vector3(0.170705f, 0.193721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.193721f, 0.000000f), new Vector3(0.007672f, 0.193721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.193721f, 0.000000f), new Vector3(0.007672f, 0.023016f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Hash_Sign
        private static List<Line3> getCharHash()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.157279f, 0.023016f, 0.000000f), new Vector3(0.260852f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.260852f, 0.023016f, 0.000000f), new Vector3(0.343328f, 0.362508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.362508f, 0.000000f), new Vector3(0.561983f, 0.362508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.561983f, 0.362508f, 0.000000f), new Vector3(0.477590f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.477590f, 0.023016f, 0.000000f), new Vector3(0.581164f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.581164f, 0.023016f, 0.000000f), new Vector3(0.665557f, 0.362508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 0.362508f, 0.000000f), new Vector3(0.941754f, 0.362508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.941754f, 0.362508f, 0.000000f), new Vector3(0.941754f, 0.471836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.941754f, 0.471836f, 0.000000f), new Vector3(0.694327f, 0.471836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.694327f, 0.471836f, 0.000000f), new Vector3(0.767213f, 0.774885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767213f, 0.774885f, 0.000000f), new Vector3(1.035737f, 0.774885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.035737f, 0.774885f, 0.000000f), new Vector3(1.035737f, 0.884213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.035737f, 0.884213f, 0.000000f), new Vector3(0.795983f, 0.884213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.795983f, 0.884213f, 0.000000f), new Vector3(0.880376f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.880376f, 1.223704f, 0.000000f), new Vector3(0.776803f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.776803f, 1.223704f, 0.000000f), new Vector3(0.692409f, 0.884213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.692409f, 0.884213f, 0.000000f), new Vector3(0.473754f, 0.884213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, 0.884213f, 0.000000f), new Vector3(0.560065f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.560065f, 1.223704f, 0.000000f), new Vector3(0.454573f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.454573f, 1.223704f, 0.000000f), new Vector3(0.370180f, 0.884213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370180f, 0.884213f, 0.000000f), new Vector3(0.093984f, 0.884213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.093984f, 0.884213f, 0.000000f), new Vector3(0.093984f, 0.774885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.093984f, 0.774885f, 0.000000f), new Vector3(0.343328f, 0.774885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.774885f, 0.000000f), new Vector3(0.268524f, 0.471836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.268524f, 0.471836f, 0.000000f), new Vector3(0.000000f, 0.471836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.471836f, 0.000000f), new Vector3(0.000000f, 0.362508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.362508f, 0.000000f), new Vector3(0.239754f, 0.362508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.239754f, 0.362508f, 0.000000f), new Vector3(0.157279f, 0.023016f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.370180f, 0.469918f, 0.000000f), new Vector3(0.590754f, 0.469918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.590754f, 0.469918f, 0.000000f), new Vector3(0.665557f, 0.776803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 0.776803f, 0.000000f), new Vector3(0.444983f, 0.776803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.444983f, 0.776803f, 0.000000f), new Vector3(0.370180f, 0.469918f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Percent
        private static List<Line3> getCharPercent()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.297295f, 1.246721f, 0.000000f), new Vector3(0.402787f, 1.231376f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402787f, 1.231376f, 0.000000f), new Vector3(0.485262f, 1.187262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, 1.187262f, 0.000000f), new Vector3(0.517869f, 1.158491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517869f, 1.158491f, 0.000000f), new Vector3(0.560065f, 1.083688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.560065f, 1.083688f, 0.000000f), new Vector3(0.585000f, 0.985868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 0.985868f, 0.000000f), new Vector3(0.592672f, 0.891885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.592672f, 0.891885f, 0.000000f), new Vector3(0.583082f, 0.774885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.583082f, 0.774885f, 0.000000f), new Vector3(0.554311f, 0.680901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.554311f, 0.680901f, 0.000000f), new Vector3(0.515951f, 0.623360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.623360f, 0.000000f), new Vector3(0.443065f, 0.565819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 0.565819f, 0.000000f), new Vector3(0.347164f, 0.537049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.347164f, 0.537049f, 0.000000f), new Vector3(0.297295f, 0.535131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.297295f, 0.535131f, 0.000000f), new Vector3(0.189885f, 0.548557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.189885f, 0.548557f, 0.000000f), new Vector3(0.105492f, 0.592672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.105492f, 0.592672f, 0.000000f), new Vector3(0.074803f, 0.623360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.074803f, 0.623360f, 0.000000f), new Vector3(0.030689f, 0.696245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.030689f, 0.696245f, 0.000000f), new Vector3(0.005754f, 0.794065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005754f, 0.794065f, 0.000000f), new Vector3(0.000000f, 0.889967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.889967f, 0.000000f), new Vector3(0.009590f, 1.006967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.009590f, 1.006967f, 0.000000f), new Vector3(0.038361f, 1.099032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 1.099032f, 0.000000f), new Vector3(0.076721f, 1.158491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.076721f, 1.158491f, 0.000000f), new Vector3(0.149606f, 1.216032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 1.216032f, 0.000000f), new Vector3(0.245508f, 1.242884f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.245508f, 1.242884f, 0.000000f), new Vector3(0.297295f, 1.246721f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.297295f, 1.139311f, 0.000000f), new Vector3(0.335656f, 1.135475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335656f, 1.135475f, 0.000000f), new Vector3(0.412377f, 1.083688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 1.083688f, 0.000000f), new Vector3(0.439229f, 1.010803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 1.010803f, 0.000000f), new Vector3(0.448819f, 0.891885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.448819f, 0.891885f, 0.000000f), new Vector3(0.439229f, 0.774885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 0.774885f, 0.000000f), new Vector3(0.412377f, 0.696245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 0.696245f, 0.000000f), new Vector3(0.391278f, 0.671311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.391278f, 0.671311f, 0.000000f), new Vector3(0.297295f, 0.642541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.297295f, 0.642541f, 0.000000f), new Vector3(0.255098f, 0.646377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.255098f, 0.646377f, 0.000000f), new Vector3(0.178377f, 0.696245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.178377f, 0.696245f, 0.000000f), new Vector3(0.153443f, 0.769131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.153443f, 0.769131f, 0.000000f), new Vector3(0.143852f, 0.889967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 0.889967f, 0.000000f), new Vector3(0.151524f, 1.006967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 1.006967f, 0.000000f), new Vector3(0.178377f, 1.083688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.178377f, 1.083688f, 0.000000f), new Vector3(0.201393f, 1.110540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.201393f, 1.110540f, 0.000000f), new Vector3(0.297295f, 1.139311f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.387442f, 0.023016f, 0.000000f), new Vector3(0.519787f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.519787f, 0.023016f, 0.000000f), new Vector3(1.148901f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.148901f, 1.223704f, 0.000000f), new Vector3(1.014639f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.014639f, 1.223704f, 0.000000f), new Vector3(0.387442f, 0.023016f, 0.000000f)));

            b0.Add(new Line3(new Vector3(1.131639f, 0.015344f, 0.000000f), new Vector3(1.239048f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.239048f, 0.000000f, 0.000000f), new Vector3(1.288917f, 0.003836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.288917f, 0.003836f, 0.000000f), new Vector3(1.384819f, 0.030689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.384819f, 0.030689f, 0.000000f), new Vector3(1.457704f, 0.088229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.457704f, 0.088229f, 0.000000f), new Vector3(1.496065f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.496065f, 0.147688f, 0.000000f), new Vector3(1.524835f, 0.239754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.524835f, 0.239754f, 0.000000f), new Vector3(1.534425f, 0.356754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.534425f, 0.356754f, 0.000000f), new Vector3(1.528671f, 0.452655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.528671f, 0.452655f, 0.000000f), new Vector3(1.503737f, 0.550475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.503737f, 0.550475f, 0.000000f), new Vector3(1.459622f, 0.623360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.459622f, 0.623360f, 0.000000f), new Vector3(1.428934f, 0.654049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.428934f, 0.654049f, 0.000000f), new Vector3(1.344540f, 0.698164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.344540f, 0.698164f, 0.000000f), new Vector3(1.239048f, 0.711590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.239048f, 0.711590f, 0.000000f), new Vector3(1.189180f, 0.709672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.189180f, 0.709672f, 0.000000f), new Vector3(1.093278f, 0.680901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.093278f, 0.680901f, 0.000000f), new Vector3(1.020393f, 0.623360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.020393f, 0.623360f, 0.000000f), new Vector3(0.982032f, 0.563901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.982032f, 0.563901f, 0.000000f), new Vector3(0.953262f, 0.471836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.953262f, 0.471836f, 0.000000f), new Vector3(0.943672f, 0.356754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943672f, 0.356754f, 0.000000f), new Vector3(0.949426f, 0.260852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.949426f, 0.260852f, 0.000000f), new Vector3(0.974360f, 0.163033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.974360f, 0.163033f, 0.000000f), new Vector3(1.018475f, 0.088229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.018475f, 0.088229f, 0.000000f), new Vector3(1.049163f, 0.059459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.049163f, 0.059459f, 0.000000f), new Vector3(1.131639f, 0.015344f, 0.000000f)));

            b0.Add(new Line3(new Vector3(1.198770f, 0.111246f, 0.000000f), new Vector3(1.239048f, 0.107410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.239048f, 0.107410f, 0.000000f), new Vector3(1.333032f, 0.136180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.333032f, 0.136180f, 0.000000f), new Vector3(1.356048f, 0.163033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.356048f, 0.163033f, 0.000000f), new Vector3(1.382901f, 0.239754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.382901f, 0.239754f, 0.000000f), new Vector3(1.390573f, 0.356754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.390573f, 0.356754f, 0.000000f), new Vector3(1.382901f, 0.475672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.382901f, 0.475672f, 0.000000f), new Vector3(1.356048f, 0.550475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.356048f, 0.550475f, 0.000000f), new Vector3(1.279327f, 0.600344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.279327f, 0.600344f, 0.000000f), new Vector3(1.239048f, 0.604180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.239048f, 0.604180f, 0.000000f), new Vector3(1.145065f, 0.575409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.145065f, 0.575409f, 0.000000f), new Vector3(1.122048f, 0.550475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.122048f, 0.550475f, 0.000000f), new Vector3(1.095196f, 0.471836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.095196f, 0.471836f, 0.000000f), new Vector3(1.085606f, 0.356754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.085606f, 0.356754f, 0.000000f), new Vector3(1.095196f, 0.235918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.095196f, 0.235918f, 0.000000f), new Vector3(1.122048f, 0.163033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.122048f, 0.163033f, 0.000000f), new Vector3(1.198770f, 0.111246f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Period
        private static List<Line3> getCharPeriod()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.253180f, 0.000000f), new Vector3(0.191803f, 0.253180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.253180f, 0.000000f), new Vector3(0.191803f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.023016f, 0.000000f), new Vector3(0.000000f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.023016f, 0.000000f), new Vector3(0.000000f, 0.253180f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Question_Mark
        private static List<Line3> getCharQuestionMark()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.197557f, 0.170705f, 0.000000f), new Vector3(0.362508f, 0.170705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 0.170705f, 0.000000f), new Vector3(0.362508f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 0.000000f, 0.000000f), new Vector3(0.197557f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.000000f, 0.000000f), new Vector3(0.197557f, 0.170705f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.205229f, 0.322229f, 0.000000f), new Vector3(0.349082f, 0.322229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349082f, 0.322229f, 0.000000f), new Vector3(0.349082f, 0.504442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349082f, 0.504442f, 0.000000f), new Vector3(0.473754f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, 0.583082f, 0.000000f), new Vector3(0.577327f, 0.669393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.577327f, 0.669393f, 0.000000f), new Vector3(0.598426f, 0.690491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.598426f, 0.690491f, 0.000000f), new Vector3(0.652131f, 0.778721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652131f, 0.778721f, 0.000000f), new Vector3(0.665557f, 0.815163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, 0.815163f, 0.000000f), new Vector3(0.680901f, 0.918737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.680901f, 0.918737f, 0.000000f), new Vector3(0.675147f, 0.974360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675147f, 0.974360f, 0.000000f), new Vector3(0.642541f, 1.066426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.642541f, 1.066426f, 0.000000f), new Vector3(0.579246f, 1.141229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.579246f, 1.141229f, 0.000000f), new Vector3(0.510196f, 1.185344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.510196f, 1.185344f, 0.000000f), new Vector3(0.416213f, 1.214114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.416213f, 1.214114f, 0.000000f), new Vector3(0.306885f, 1.223704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.306885f, 1.223704f, 0.000000f), new Vector3(0.247426f, 1.221786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.247426f, 1.221786f, 0.000000f), new Vector3(0.143852f, 1.208360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 1.208360f, 0.000000f), new Vector3(0.092066f, 1.196852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.092066f, 1.196852f, 0.000000f), new Vector3(0.000000f, 1.168081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.168081f, 0.000000f), new Vector3(0.000000f, 1.003131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.003131f, 0.000000f), new Vector3(0.007672f, 1.003131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 1.003131f, 0.000000f), new Vector3(0.030689f, 1.016557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.030689f, 1.016557f, 0.000000f), new Vector3(0.130426f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.130426f, 1.058753f, 0.000000f), new Vector3(0.191803f, 1.076016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 1.076016f, 0.000000f), new Vector3(0.295377f, 1.087524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.295377f, 1.087524f, 0.000000f), new Vector3(0.366344f, 1.079852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.366344f, 1.079852f, 0.000000f), new Vector3(0.454573f, 1.041491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.454573f, 1.041491f, 0.000000f), new Vector3(0.491016f, 1.003131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.491016f, 1.003131f, 0.000000f), new Vector3(0.515951f, 0.905311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.905311f, 0.000000f), new Vector3(0.515951f, 0.888049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.888049f, 0.000000f), new Vector3(0.489098f, 0.792147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.489098f, 0.792147f, 0.000000f), new Vector3(0.418131f, 0.705836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 0.705836f, 0.000000f), new Vector3(0.404705f, 0.694327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.404705f, 0.694327f, 0.000000f), new Vector3(0.318393f, 0.632950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.318393f, 0.632950f, 0.000000f), new Vector3(0.293459f, 0.619524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293459f, 0.619524f, 0.000000f), new Vector3(0.205229f, 0.567737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205229f, 0.567737f, 0.000000f), new Vector3(0.205229f, 0.322229f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Quote_single
        private static List<Line3> getCharQuoteSingle()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.034525f, 0.811327f, 0.000000f), new Vector3(0.143852f, 0.811327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, 0.811327f, 0.000000f), new Vector3(0.178377f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.178377f, 1.277409f, 0.000000f), new Vector3(0.000000f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.277409f, 0.000000f), new Vector3(0.034525f, 0.811327f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Quotes
        private static List<Line3> getCharQuotes()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.034525f, 0.811327f, 0.000000f), new Vector3(0.141934f, 0.811327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.141934f, 0.811327f, 0.000000f), new Vector3(0.176459f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176459f, 1.277409f, 0.000000f), new Vector3(0.000000f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.277409f, 0.000000f), new Vector3(0.034525f, 0.811327f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.345246f, 0.811327f, 0.000000f), new Vector3(0.452655f, 0.811327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452655f, 0.811327f, 0.000000f), new Vector3(0.487180f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.487180f, 1.277409f, 0.000000f), new Vector3(0.310721f, 1.277409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.310721f, 1.277409f, 0.000000f), new Vector3(0.345246f, 0.811327f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Semicolon
        private static List<Line3> getCharSemicolon()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.092066f, 0.899557f, 0.000000f), new Vector3(0.285787f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.899557f, 0.000000f), new Vector3(0.285787f, 0.669393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.669393f, 0.000000f), new Vector3(0.092066f, 0.669393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.092066f, 0.669393f, 0.000000f), new Vector3(0.092066f, 0.899557f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.000000f, -0.299213f, 0.000000f), new Vector3(0.117000f, -0.299213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.117000f, -0.299213f, 0.000000f), new Vector3(0.343328f, 0.228246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.228246f, 0.000000f), new Vector3(0.140016f, 0.228246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 0.228246f, 0.000000f), new Vector3(0.000000f, -0.299213f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Tilde
        private static List<Line3> getCharTilde()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.318393f, 0.000000f), new Vector3(0.134262f, 0.318393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.134262f, 0.318393f, 0.000000f), new Vector3(0.136180f, 0.358672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.136180f, 0.358672f, 0.000000f), new Vector3(0.151524f, 0.469918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.469918f, 0.000000f), new Vector3(0.180295f, 0.546639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.180295f, 0.546639f, 0.000000f), new Vector3(0.209065f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.209065f, 0.583082f, 0.000000f), new Vector3(0.297295f, 0.617606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.297295f, 0.617606f, 0.000000f), new Vector3(0.374016f, 0.590754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.374016f, 0.590754f, 0.000000f), new Vector3(0.412377f, 0.558147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 0.558147f, 0.000000f), new Vector3(0.492934f, 0.464164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.492934f, 0.464164f, 0.000000f), new Vector3(0.546639f, 0.406623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546639f, 0.406623f, 0.000000f), new Vector3(0.619524f, 0.351000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.619524f, 0.351000f, 0.000000f), new Vector3(0.654049f, 0.335656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.654049f, 0.335656f, 0.000000f), new Vector3(0.753786f, 0.318393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.753786f, 0.318393f, 0.000000f), new Vector3(0.795983f, 0.322229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.795983f, 0.322229f, 0.000000f), new Vector3(0.888049f, 0.354836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.888049f, 0.354836f, 0.000000f), new Vector3(0.911065f, 0.372098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.911065f, 0.372098f, 0.000000f), new Vector3(0.976278f, 0.452655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.976278f, 0.452655f, 0.000000f), new Vector3(0.997376f, 0.491016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.997376f, 0.491016f, 0.000000f), new Vector3(1.029983f, 0.588836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.029983f, 0.588836f, 0.000000f), new Vector3(1.039573f, 0.644459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.039573f, 0.644459f, 0.000000f), new Vector3(1.047245f, 0.746114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.047245f, 0.746114f, 0.000000f), new Vector3(0.912983f, 0.746114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.912983f, 0.746114f, 0.000000f), new Vector3(0.912983f, 0.725016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.912983f, 0.725016f, 0.000000f), new Vector3(0.899557f, 0.615688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.899557f, 0.615688f, 0.000000f), new Vector3(0.872704f, 0.529377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.872704f, 0.529377f, 0.000000f), new Vector3(0.836262f, 0.479508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.836262f, 0.479508f, 0.000000f), new Vector3(0.749950f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749950f, 0.448819f, 0.000000f), new Vector3(0.659803f, 0.485262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.659803f, 0.485262f, 0.000000f), new Vector3(0.629114f, 0.514032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.629114f, 0.514032f, 0.000000f), new Vector3(0.554311f, 0.600344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.554311f, 0.600344f, 0.000000f), new Vector3(0.498688f, 0.659803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.498688f, 0.659803f, 0.000000f), new Vector3(0.425803f, 0.715426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.425803f, 0.715426f, 0.000000f), new Vector3(0.393196f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.393196f, 0.728852f, 0.000000f), new Vector3(0.291541f, 0.746114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.291541f, 0.746114f, 0.000000f), new Vector3(0.251262f, 0.744196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.251262f, 0.744196f, 0.000000f), new Vector3(0.159197f, 0.709672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.709672f, 0.000000f), new Vector3(0.134262f, 0.690491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.134262f, 0.690491f, 0.000000f), new Vector3(0.069049f, 0.613770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.069049f, 0.613770f, 0.000000f), new Vector3(0.049869f, 0.573491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.049869f, 0.573491f, 0.000000f), new Vector3(0.017262f, 0.475672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017262f, 0.475672f, 0.000000f), new Vector3(0.007672f, 0.420049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.420049f, 0.000000f), new Vector3(0.000000f, 0.318393f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Underscore
        private static List<Line3> getCharUnderscore()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.241672f, 0.000000f), new Vector3(1.054917f, -0.241672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.054917f, -0.241672f, 0.000000f), new Vector3(1.054917f, -0.145770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.054917f, -0.145770f, 0.000000f), new Vector3(0.000000f, -0.145770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.145770f, 0.000000f), new Vector3(0.000000f, -0.241672f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Unknown
        private static List<Line3> getCharUnknown()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.040557f, 0.000000f), new Vector3(0.758192f, 0.040557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.040557f, 0.000000f), new Vector3(0.758192f, 0.801688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.801688f, 0.000000f), new Vector3(0.000000f, 0.801688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.801688f, 0.000000f), new Vector3(0.000000f, 0.040557f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.129304f, 0.169862f, 0.000000f), new Vector3(0.628888f, 0.169862f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.628888f, 0.169862f, 0.000000f), new Vector3(0.628888f, 0.672384f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.628888f, 0.672384f, 0.000000f), new Vector3(0.129304f, 0.672384f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.129304f, 0.672384f, 0.000000f), new Vector3(0.129304f, 0.169862f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Vertical_Bar
        private static List<Line3> getCharVerticalBar()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.316475f, 0.000000f), new Vector3(0.140016f, -0.316475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, -0.316475f, 0.000000f), new Vector3(0.140016f, 1.254393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 1.254393f, 0.000000f), new Vector3(0.000000f, 1.254393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.254393f, 0.000000f), new Vector3(0.000000f, -0.316475f, 0.000000f)));

            return b0;
        }
        #endregion

        // ------------------------------------------ LATIN ALPHABET ---------------------------------------------- //

        #region verdana_12_regular__A
        private static List<Line3> getCharA()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.163033f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 0.000000f, 0.000000f), new Vector3(0.280033f, 0.335656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.280033f, 0.335656f, 0.000000f), new Vector3(0.797901f, 0.335656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.797901f, 0.335656f, 0.000000f), new Vector3(0.914901f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.914901f, 0.000000f, 0.000000f), new Vector3(1.085606f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.085606f, 0.000000f, 0.000000f), new Vector3(0.650213f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.650213f, 1.200688f, 0.000000f), new Vector3(0.437311f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437311f, 1.200688f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.327983f, 0.471836f, 0.000000f), new Vector3(0.749950f, 0.471836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749950f, 0.471836f, 0.000000f), new Vector3(0.538967f, 1.060671f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.538967f, 1.060671f, 0.000000f), new Vector3(0.327983f, 0.471836f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharAs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.611852f, 0.000000f, 0.000000f), new Vector3(0.761459f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.761459f, 0.000000f, 0.000000f), new Vector3(0.761459f, 0.609934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.761459f, 0.609934f, 0.000000f), new Vector3(0.759540f, 0.661721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.661721f, 0.000000f), new Vector3(0.734606f, 0.755704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.734606f, 0.755704f, 0.000000f), new Vector3(0.725016f, 0.776803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.725016f, 0.776803f, 0.000000f), new Vector3(0.657885f, 0.851606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.657885f, 0.851606f, 0.000000f), new Vector3(0.632950f, 0.868868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.632950f, 0.868868f, 0.000000f), new Vector3(0.537049f, 0.905311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537049f, 0.905311f, 0.000000f), new Vector3(0.477590f, 0.914901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.477590f, 0.914901f, 0.000000f), new Vector3(0.370180f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370180f, 0.920655f, 0.000000f), new Vector3(0.308803f, 0.918737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308803f, 0.918737f, 0.000000f), new Vector3(0.205229f, 0.907229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205229f, 0.907229f, 0.000000f), new Vector3(0.170705f, 0.901475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.901475f, 0.000000f), new Vector3(0.076721f, 0.880376f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.076721f, 0.880376f, 0.000000f), new Vector3(0.076721f, 0.726934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.076721f, 0.726934f, 0.000000f), new Vector3(0.086311f, 0.726934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.086311f, 0.726934f, 0.000000f), new Vector3(0.140016f, 0.746114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 0.746114f, 0.000000f), new Vector3(0.237836f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.237836f, 0.772967f, 0.000000f), new Vector3(0.270442f, 0.778721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270442f, 0.778721f, 0.000000f), new Vector3(0.370180f, 0.788311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370180f, 0.788311f, 0.000000f), new Vector3(0.464164f, 0.780639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.464164f, 0.780639f, 0.000000f), new Vector3(0.540885f, 0.755704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.755704f, 0.000000f), new Vector3(0.594590f, 0.703918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.594590f, 0.703918f, 0.000000f), new Vector3(0.611852f, 0.617606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611852f, 0.617606f, 0.000000f), new Vector3(0.611852f, 0.594590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611852f, 0.594590f, 0.000000f), new Vector3(0.567737f, 0.592672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567737f, 0.592672f, 0.000000f), new Vector3(0.462246f, 0.585000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.462246f, 0.585000f, 0.000000f), new Vector3(0.364426f, 0.575409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.364426f, 0.575409f, 0.000000f), new Vector3(0.257016f, 0.556229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.257016f, 0.556229f, 0.000000f), new Vector3(0.166869f, 0.525541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.525541f, 0.000000f), new Vector3(0.111246f, 0.494852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111246f, 0.494852f, 0.000000f), new Vector3(0.042197f, 0.425803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.042197f, 0.425803f, 0.000000f), new Vector3(0.013426f, 0.366344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.013426f, 0.366344f, 0.000000f), new Vector3(-0.001918f, 0.260852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(-0.001918f, 0.260852f, 0.000000f), new Vector3(0.000000f, 0.230164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.230164f, 0.000000f), new Vector3(0.024934f, 0.134262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.024934f, 0.134262f, 0.000000f), new Vector3(0.082475f, 0.055623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.082475f, 0.055623f, 0.000000f), new Vector3(0.101656f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 0.038361f, 0.000000f), new Vector3(0.187967f, -0.009590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.187967f, -0.009590f, 0.000000f), new Vector3(0.287705f, -0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287705f, -0.024934f, 0.000000f), new Vector3(0.303049f, -0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.303049f, -0.024934f, 0.000000f), new Vector3(0.402787f, -0.013426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402787f, -0.013426f, 0.000000f), new Vector3(0.491016f, 0.017262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.491016f, 0.017262f, 0.000000f), new Vector3(0.558147f, 0.057541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 0.057541f, 0.000000f), new Vector3(0.611852f, 0.095902f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611852f, 0.095902f, 0.000000f), new Vector3(0.611852f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.611852f, 0.220574f, 0.000000f), new Vector3(0.611852f, 0.471836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611852f, 0.471836f, 0.000000f), new Vector3(0.514032f, 0.464164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.514032f, 0.464164f, 0.000000f), new Vector3(0.448819f, 0.460328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.448819f, 0.460328f, 0.000000f), new Vector3(0.339492f, 0.444983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.339492f, 0.444983f, 0.000000f), new Vector3(0.299213f, 0.435393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299213f, 0.435393f, 0.000000f), new Vector3(0.207147f, 0.391278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.207147f, 0.391278f, 0.000000f), new Vector3(0.193721f, 0.379770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 0.379770f, 0.000000f), new Vector3(0.155361f, 0.291541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, 0.291541f, 0.000000f), new Vector3(0.153443f, 0.270442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.153443f, 0.270442f, 0.000000f), new Vector3(0.182213f, 0.174541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.174541f, 0.000000f), new Vector3(0.201393f, 0.155361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.201393f, 0.155361f, 0.000000f), new Vector3(0.285787f, 0.120836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.120836f, 0.000000f), new Vector3(0.343328f, 0.115082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.115082f, 0.000000f), new Vector3(0.444983f, 0.130426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.444983f, 0.130426f, 0.000000f), new Vector3(0.489098f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.489098f, 0.147688f, 0.000000f), new Vector3(0.579246f, 0.197557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.579246f, 0.197557f, 0.000000f), new Vector3(0.611852f, 0.220574f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__B
        private static List<Line3> getCharB()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.425803f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.425803f, 0.000000f, 0.000000f), new Vector3(0.540885f, 0.005754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.005754f, 0.000000f), new Vector3(0.631032f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631032f, 0.023016f, 0.000000f), new Vector3(0.692409f, 0.046033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.692409f, 0.046033f, 0.000000f), new Vector3(0.778721f, 0.097820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778721f, 0.097820f, 0.000000f), new Vector3(0.811327f, 0.128508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811327f, 0.128508f, 0.000000f), new Vector3(0.870786f, 0.210983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.870786f, 0.210983f, 0.000000f), new Vector3(0.889967f, 0.266606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.889967f, 0.266606f, 0.000000f), new Vector3(0.903393f, 0.370180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.903393f, 0.370180f, 0.000000f), new Vector3(0.903393f, 0.393196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.903393f, 0.393196f, 0.000000f), new Vector3(0.882295f, 0.492934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.882295f, 0.492934f, 0.000000f), new Vector3(0.834344f, 0.577327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.834344f, 0.577327f, 0.000000f), new Vector3(0.820917f, 0.590754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.820917f, 0.590754f, 0.000000f), new Vector3(0.742278f, 0.648295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.742278f, 0.648295f, 0.000000f), new Vector3(0.642541f, 0.682819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.642541f, 0.682819f, 0.000000f), new Vector3(0.642541f, 0.688573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.642541f, 0.688573f, 0.000000f), new Vector3(0.686655f, 0.715426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.686655f, 0.715426f, 0.000000f), new Vector3(0.757622f, 0.786393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.757622f, 0.786393f, 0.000000f), new Vector3(0.782557f, 0.830508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, 0.830508f, 0.000000f), new Vector3(0.801737f, 0.932163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.801737f, 0.932163f, 0.000000f), new Vector3(0.799819f, 0.966688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.799819f, 0.966688f, 0.000000f), new Vector3(0.772967f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772967f, 1.062590f, 0.000000f), new Vector3(0.755704f, 1.085606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 1.085606f, 0.000000f), new Vector3(0.677065f, 1.150819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.677065f, 1.150819f, 0.000000f), new Vector3(0.550475f, 1.191098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.550475f, 1.191098f, 0.000000f), new Vector3(0.354836f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.354836f, 1.200688f, 0.000000f), new Vector3(0.000000f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.200688f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.159197f, 0.136180f, 0.000000f), new Vector3(0.370180f, 0.136180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370180f, 0.136180f, 0.000000f), new Vector3(0.481426f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481426f, 0.140016f, 0.000000f), new Vector3(0.540885f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.147688f, 0.000000f), new Vector3(0.640623f, 0.180295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 0.180295f, 0.000000f), new Vector3(0.652131f, 0.187967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652131f, 0.187967f, 0.000000f), new Vector3(0.717344f, 0.258934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717344f, 0.258934f, 0.000000f), new Vector3(0.738442f, 0.356754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.738442f, 0.356754f, 0.000000f), new Vector3(0.717344f, 0.477590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717344f, 0.477590f, 0.000000f), new Vector3(0.646377f, 0.546639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646377f, 0.546639f, 0.000000f), new Vector3(0.544721f, 0.577327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.544721f, 0.577327f, 0.000000f), new Vector3(0.410459f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.410459f, 0.583082f, 0.000000f), new Vector3(0.159197f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.583082f, 0.000000f), new Vector3(0.159197f, 0.136180f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.159197f, 0.717344f, 0.000000f), new Vector3(0.366344f, 0.717344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.366344f, 0.717344f, 0.000000f), new Vector3(0.471836f, 0.723098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.471836f, 0.723098f, 0.000000f), new Vector3(0.485262f, 0.725016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, 0.725016f, 0.000000f), new Vector3(0.565819f, 0.757622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.565819f, 0.757622f, 0.000000f), new Vector3(0.621442f, 0.820917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.621442f, 0.820917f, 0.000000f), new Vector3(0.636786f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.636786f, 0.911065f, 0.000000f), new Vector3(0.621442f, 0.983950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.621442f, 0.983950f, 0.000000f), new Vector3(0.575409f, 1.031901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575409f, 1.031901f, 0.000000f), new Vector3(0.483344f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.483344f, 1.058753f, 0.000000f), new Vector3(0.385524f, 1.064507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.385524f, 1.064507f, 0.000000f), new Vector3(0.159197f, 1.064507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.064507f, 0.000000f), new Vector3(0.159197f, 0.717344f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharBs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 1.252475f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.140016f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 0.000000f, 0.000000f), new Vector3(0.149606f, 0.042197f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.042197f, 0.000000f), new Vector3(0.168787f, 0.030689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.168787f, 0.030689f, 0.000000f), new Vector3(0.262770f, -0.007672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.262770f, -0.007672f, 0.000000f), new Vector3(0.291541f, -0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.291541f, -0.015344f, 0.000000f), new Vector3(0.397033f, -0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.397033f, -0.024934f, 0.000000f), new Vector3(0.450737f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, -0.021098f, 0.000000f), new Vector3(0.546639f, 0.005754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546639f, 0.005754f, 0.000000f), new Vector3(0.594590f, 0.032607f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.594590f, 0.032607f, 0.000000f), new Vector3(0.673229f, 0.101656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.673229f, 0.101656f, 0.000000f), new Vector3(0.713508f, 0.157279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.713508f, 0.157279f, 0.000000f), new Vector3(0.759540f, 0.253180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.253180f, 0.000000f), new Vector3(0.782557f, 0.349082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, 0.349082f, 0.000000f), new Vector3(0.790229f, 0.456492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.456492f, 0.000000f), new Vector3(0.788311f, 0.527459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 0.527459f, 0.000000f), new Vector3(0.772967f, 0.634868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772967f, 0.634868f, 0.000000f), new Vector3(0.742278f, 0.726934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.742278f, 0.726934f, 0.000000f), new Vector3(0.698164f, 0.801737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.698164f, 0.801737f, 0.000000f), new Vector3(0.632950f, 0.866950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.632950f, 0.866950f, 0.000000f), new Vector3(0.548557f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.548557f, 0.911065f, 0.000000f), new Vector3(0.446901f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446901f, 0.924491f, 0.000000f), new Vector3(0.381688f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.381688f, 0.920655f, 0.000000f), new Vector3(0.285787f, 0.891885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.891885f, 0.000000f), new Vector3(0.234000f, 0.865032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.234000f, 0.865032f, 0.000000f), new Vector3(0.149606f, 0.805573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.805573f, 0.000000f), new Vector3(0.149606f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 1.252475f, 0.000000f), new Vector3(0.000000f, 1.252475f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.149606f, 0.163033f, 0.000000f), new Vector3(0.249344f, 0.124672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.124672f, 0.000000f), new Vector3(0.362508f, 0.111246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 0.111246f, 0.000000f), new Vector3(0.468000f, 0.126590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.468000f, 0.126590f, 0.000000f), new Vector3(0.548557f, 0.178377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.548557f, 0.178377f, 0.000000f), new Vector3(0.563901f, 0.193721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.563901f, 0.193721f, 0.000000f), new Vector3(0.606098f, 0.266606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.266606f, 0.000000f), new Vector3(0.629114f, 0.366344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.629114f, 0.366344f, 0.000000f), new Vector3(0.634868f, 0.452655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634868f, 0.452655f, 0.000000f), new Vector3(0.625278f, 0.569655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625278f, 0.569655f, 0.000000f), new Vector3(0.600344f, 0.659803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.659803f, 0.000000f), new Vector3(0.581164f, 0.698164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.581164f, 0.698164f, 0.000000f), new Vector3(0.512114f, 0.759540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.512114f, 0.759540f, 0.000000f), new Vector3(0.412377f, 0.780639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 0.780639f, 0.000000f), new Vector3(0.310721f, 0.765295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.310721f, 0.765295f, 0.000000f), new Vector3(0.276197f, 0.751868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276197f, 0.751868f, 0.000000f), new Vector3(0.186049f, 0.703918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.186049f, 0.703918f, 0.000000f), new Vector3(0.149606f, 0.677065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.677065f, 0.000000f), new Vector3(0.149606f, 0.163033f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__C
        private static List<Line3> getCharC()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.991622f, 0.087788f, 0.000000f), new Vector3(0.991622f, 0.273837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.991622f, 0.273837f, 0.000000f), new Vector3(0.980114f, 0.273837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.980114f, 0.273837f, 0.000000f), new Vector3(0.882295f, 0.199034f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.882295f, 0.199034f, 0.000000f), new Vector3(0.792147f, 0.154919f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.792147f, 0.154919f, 0.000000f), new Vector3(0.678983f, 0.124231f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678983f, 0.124231f, 0.000000f), new Vector3(0.575409f, 0.116559f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575409f, 0.116559f, 0.000000f), new Vector3(0.515951f, 0.120395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.120395f, 0.000000f), new Vector3(0.418131f, 0.145329f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 0.145329f, 0.000000f), new Vector3(0.364426f, 0.170263f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.364426f, 0.170263f, 0.000000f), new Vector3(0.285787f, 0.233559f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.233559f, 0.000000f), new Vector3(0.245508f, 0.285345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.245508f, 0.285345f, 0.000000f), new Vector3(0.197557f, 0.381247f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.381247f, 0.000000f), new Vector3(0.170705f, 0.488657f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.488657f, 0.000000f), new Vector3(0.163033f, 0.597984f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 0.597984f, 0.000000f), new Vector3(0.172623f, 0.718821f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.718821f, 0.000000f), new Vector3(0.193721f, 0.812804f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 0.812804f, 0.000000f), new Vector3(0.224410f, 0.883771f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.224410f, 0.883771f, 0.000000f), new Vector3(0.280033f, 0.964329f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.280033f, 0.964329f, 0.000000f), new Vector3(0.324147f, 1.006525f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.324147f, 1.006525f, 0.000000f), new Vector3(0.410459f, 1.056394f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.410459f, 1.056394f, 0.000000f), new Vector3(0.469918f, 1.075574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.469918f, 1.075574f, 0.000000f), new Vector3(0.575409f, 1.085165f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575409f, 1.085165f, 0.000000f), new Vector3(0.686655f, 1.075574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.686655f, 1.075574f, 0.000000f), new Vector3(0.782557f, 1.046804f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, 1.046804f, 0.000000f), new Vector3(0.805573f, 1.039132f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805573f, 1.039132f, 0.000000f), new Vector3(0.891885f, 0.991181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.891885f, 0.991181f, 0.000000f), new Vector3(0.980114f, 0.925968f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.980114f, 0.925968f, 0.000000f), new Vector3(0.991622f, 0.925968f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.991622f, 0.925968f, 0.000000f), new Vector3(0.991622f, 1.115853f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.991622f, 1.115853f, 0.000000f), new Vector3(0.868868f, 1.169558f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.868868f, 1.169558f, 0.000000f), new Vector3(0.778721f, 1.198328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778721f, 1.198328f, 0.000000f), new Vector3(0.686655f, 1.215591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.686655f, 1.215591f, 0.000000f), new Vector3(0.585000f, 1.221345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 1.221345f, 0.000000f), new Vector3(0.542803f, 1.221345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.542803f, 1.221345f, 0.000000f), new Vector3(0.439229f, 1.207919f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 1.207919f, 0.000000f), new Vector3(0.345246f, 1.181066f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.345246f, 1.181066f, 0.000000f), new Vector3(0.237836f, 1.125443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.237836f, 1.125443f, 0.000000f), new Vector3(0.159197f, 1.062148f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.062148f, 0.000000f), new Vector3(0.082475f, 0.958574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.082475f, 0.958574f, 0.000000f), new Vector3(0.038361f, 0.864591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.864591f, 0.000000f), new Vector3(0.021098f, 0.805132f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.021098f, 0.805132f, 0.000000f), new Vector3(0.003836f, 0.707312f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.707312f, 0.000000f), new Vector3(-0.001918f, 0.597984f, 0.000000f)));
            b0.Add(new Line3(new Vector3(-0.001918f, 0.597984f, 0.000000f), new Vector3(0.000000f, 0.517427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.517427f, 0.000000f), new Vector3(0.015344f, 0.415772f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.015344f, 0.415772f, 0.000000f), new Vector3(0.040279f, 0.325624f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.040279f, 0.325624f, 0.000000f), new Vector3(0.099738f, 0.204788f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099738f, 0.204788f, 0.000000f), new Vector3(0.161115f, 0.129985f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.129985f, 0.000000f), new Vector3(0.255098f, 0.057100f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.255098f, 0.057100f, 0.000000f), new Vector3(0.347164f, 0.014903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.347164f, 0.014903f, 0.000000f), new Vector3(0.477590f, -0.015786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.477590f, -0.015786f, 0.000000f), new Vector3(0.585000f, -0.021540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, -0.021540f, 0.000000f), new Vector3(0.711590f, -0.010031f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711590f, -0.010031f, 0.000000f), new Vector3(0.817081f, 0.014903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.817081f, 0.014903f, 0.000000f), new Vector3(0.912983f, 0.051346f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.912983f, 0.051346f, 0.000000f), new Vector3(0.991622f, 0.087788f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharCs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.728852f, 0.055623f, 0.000000f), new Vector3(0.728852f, 0.222492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 0.222492f, 0.000000f), new Vector3(0.721180f, 0.222492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.721180f, 0.222492f, 0.000000f), new Vector3(0.675147f, 0.189885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675147f, 0.189885f, 0.000000f), new Vector3(0.613770f, 0.153443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.613770f, 0.153443f, 0.000000f), new Vector3(0.527459f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.527459f, 0.122754f, 0.000000f), new Vector3(0.433475f, 0.111246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 0.111246f, 0.000000f), new Vector3(0.398951f, 0.113164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.398951f, 0.113164f, 0.000000f), new Vector3(0.303049f, 0.138098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.303049f, 0.138098f, 0.000000f), new Vector3(0.230164f, 0.197557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.230164f, 0.197557f, 0.000000f), new Vector3(0.197557f, 0.245508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.245508f, 0.000000f), new Vector3(0.166869f, 0.335656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.335656f, 0.000000f), new Vector3(0.155361f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, 0.448819f, 0.000000f), new Vector3(0.161115f, 0.529377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.529377f, 0.000000f), new Vector3(0.186049f, 0.627196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.186049f, 0.627196f, 0.000000f), new Vector3(0.232082f, 0.703918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232082f, 0.703918f, 0.000000f), new Vector3(0.251262f, 0.723098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.251262f, 0.723098f, 0.000000f), new Vector3(0.331819f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.331819f, 0.772967f, 0.000000f), new Vector3(0.433475f, 0.788311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 0.788311f, 0.000000f), new Vector3(0.477590f, 0.786393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.477590f, 0.786393f, 0.000000f), new Vector3(0.577327f, 0.759540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.577327f, 0.759540f, 0.000000f), new Vector3(0.632950f, 0.734606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.632950f, 0.734606f, 0.000000f), new Vector3(0.721180f, 0.677065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.721180f, 0.677065f, 0.000000f), new Vector3(0.728852f, 0.677065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 0.677065f, 0.000000f), new Vector3(0.728852f, 0.845852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 0.845852f, 0.000000f), new Vector3(0.690491f, 0.863114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.690491f, 0.863114f, 0.000000f), new Vector3(0.592672f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.592672f, 0.899557f, 0.000000f), new Vector3(0.542803f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.542803f, 0.911065f, 0.000000f), new Vector3(0.441147f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.920655f, 0.000000f), new Vector3(0.381688f, 0.916819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.381688f, 0.916819f, 0.000000f), new Vector3(0.281951f, 0.897639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.281951f, 0.897639f, 0.000000f), new Vector3(0.195639f, 0.855442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.195639f, 0.855442f, 0.000000f), new Vector3(0.120836f, 0.794065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 0.794065f, 0.000000f), new Vector3(0.076721f, 0.740360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.076721f, 0.740360f, 0.000000f), new Vector3(0.034525f, 0.655967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.034525f, 0.655967f, 0.000000f), new Vector3(0.007672f, 0.560065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.560065f, 0.000000f), new Vector3(0.000000f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.448819f, 0.000000f), new Vector3(0.007672f, 0.333737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.333737f, 0.000000f), new Vector3(0.030689f, 0.241672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.030689f, 0.241672f, 0.000000f), new Vector3(0.063295f, 0.172623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.063295f, 0.172623f, 0.000000f), new Vector3(0.122754f, 0.093984f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.122754f, 0.093984f, 0.000000f), new Vector3(0.170705f, 0.053705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.053705f, 0.000000f), new Vector3(0.260852f, 0.007672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.260852f, 0.007672f, 0.000000f), new Vector3(0.335656f, -0.011508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335656f, -0.011508f, 0.000000f), new Vector3(0.441147f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, -0.021098f, 0.000000f), new Vector3(0.485262f, -0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, -0.019180f, 0.000000f), new Vector3(0.585000f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 0.000000f, 0.000000f), new Vector3(0.631032f, 0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631032f, 0.015344f, 0.000000f), new Vector3(0.728852f, 0.055623f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__D
        private static List<Line3> getCharD()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.301131f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.301131f, 0.000000f, 0.000000f), new Vector3(0.375934f, 0.001918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.375934f, 0.001918f, 0.000000f), new Vector3(0.481426f, 0.009590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481426f, 0.009590f, 0.000000f), new Vector3(0.567737f, 0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567737f, 0.023016f, 0.000000f), new Vector3(0.661721f, 0.051787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.661721f, 0.051787f, 0.000000f), new Vector3(0.751868f, 0.095902f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.095902f, 0.000000f), new Vector3(0.815163f, 0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.815163f, 0.141934f, 0.000000f), new Vector3(0.884213f, 0.214820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.884213f, 0.214820f, 0.000000f), new Vector3(0.941754f, 0.303049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.941754f, 0.303049f, 0.000000f), new Vector3(0.982032f, 0.397033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.982032f, 0.397033f, 0.000000f), new Vector3(1.005049f, 0.494852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.005049f, 0.494852f, 0.000000f), new Vector3(1.012721f, 0.598426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.012721f, 0.598426f, 0.000000f), new Vector3(1.003131f, 0.715426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.003131f, 0.715426f, 0.000000f), new Vector3(0.980114f, 0.813245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.980114f, 0.813245f, 0.000000f), new Vector3(0.943672f, 0.901475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943672f, 0.901475f, 0.000000f), new Vector3(0.901475f, 0.968606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.901475f, 0.968606f, 0.000000f), new Vector3(0.834344f, 1.043409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.834344f, 1.043409f, 0.000000f), new Vector3(0.753786f, 1.104786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.753786f, 1.104786f, 0.000000f), new Vector3(0.680901f, 1.141229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.680901f, 1.141229f, 0.000000f), new Vector3(0.577327f, 1.175753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.577327f, 1.175753f, 0.000000f), new Vector3(0.515951f, 1.187262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 1.187262f, 0.000000f), new Vector3(0.418131f, 1.196852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 1.196852f, 0.000000f), new Vector3(0.299213f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299213f, 1.200688f, 0.000000f), new Vector3(0.000000f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.200688f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.159197f, 0.138098f, 0.000000f), new Vector3(0.308803f, 0.138098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308803f, 0.138098f, 0.000000f), new Vector3(0.418131f, 0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 0.141934f, 0.000000f), new Vector3(0.510196f, 0.153443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.510196f, 0.153443f, 0.000000f), new Vector3(0.608016f, 0.186049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608016f, 0.186049f, 0.000000f), new Vector3(0.667475f, 0.216738f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.667475f, 0.216738f, 0.000000f), new Vector3(0.744196f, 0.283869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.744196f, 0.283869f, 0.000000f), new Vector3(0.799819f, 0.366344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.799819f, 0.366344f, 0.000000f), new Vector3(0.830508f, 0.456492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.830508f, 0.456492f, 0.000000f), new Vector3(0.843934f, 0.560065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843934f, 0.560065f, 0.000000f), new Vector3(0.845852f, 0.602262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.845852f, 0.602262f, 0.000000f), new Vector3(0.836262f, 0.711590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.836262f, 0.711590f, 0.000000f), new Vector3(0.811327f, 0.805573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811327f, 0.805573f, 0.000000f), new Vector3(0.795983f, 0.840098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.795983f, 0.840098f, 0.000000f), new Vector3(0.738442f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.738442f, 0.920655f, 0.000000f), new Vector3(0.659803f, 0.985868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.659803f, 0.985868f, 0.000000f), new Vector3(0.650213f, 0.991622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.650213f, 0.991622f, 0.000000f), new Vector3(0.556229f, 1.033819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, 1.033819f, 0.000000f), new Vector3(0.498688f, 1.047245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.498688f, 1.047245f, 0.000000f), new Vector3(0.404705f, 1.060671f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.404705f, 1.060671f, 0.000000f), new Vector3(0.308803f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308803f, 1.062590f, 0.000000f), new Vector3(0.159197f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.062590f, 0.000000f), new Vector3(0.159197f, 0.138098f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharDs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.640623f, 0.093984f, 0.000000f), new Vector3(0.640623f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 0.000000f, 0.000000f), new Vector3(0.790229f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.000000f, 0.000000f), new Vector3(0.790229f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 1.252475f, 0.000000f), new Vector3(0.640623f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 1.252475f, 0.000000f), new Vector3(0.640623f, 0.863114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 0.863114f, 0.000000f), new Vector3(0.619524f, 0.874622f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.619524f, 0.874622f, 0.000000f), new Vector3(0.525541f, 0.909147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.525541f, 0.909147f, 0.000000f), new Vector3(0.502524f, 0.914901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502524f, 0.914901f, 0.000000f), new Vector3(0.398951f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.398951f, 0.924491f, 0.000000f), new Vector3(0.347164f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.347164f, 0.920655f, 0.000000f), new Vector3(0.249344f, 0.891885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.891885f, 0.000000f), new Vector3(0.197557f, 0.865032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.865032f, 0.000000f), new Vector3(0.120836f, 0.799819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 0.799819f, 0.000000f), new Vector3(0.078639f, 0.740360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.078639f, 0.740360f, 0.000000f), new Vector3(0.032607f, 0.646377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032607f, 0.646377f, 0.000000f), new Vector3(0.007672f, 0.548557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.548557f, 0.000000f), new Vector3(0.000000f, 0.443065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.443065f, 0.000000f), new Vector3(0.001918f, 0.372098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.001918f, 0.372098f, 0.000000f), new Vector3(0.019180f, 0.266606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.019180f, 0.266606f, 0.000000f), new Vector3(0.049869f, 0.174541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.049869f, 0.174541f, 0.000000f), new Vector3(0.093984f, 0.097820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.093984f, 0.097820f, 0.000000f), new Vector3(0.163033f, 0.030689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 0.030689f, 0.000000f), new Vector3(0.249344f, -0.011508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, -0.011508f, 0.000000f), new Vector3(0.349082f, -0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349082f, -0.024934f, 0.000000f), new Vector3(0.406623f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.406623f, -0.021098f, 0.000000f), new Vector3(0.504442f, 0.005754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.504442f, 0.005754f, 0.000000f), new Vector3(0.556229f, 0.032607f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, 0.032607f, 0.000000f), new Vector3(0.640623f, 0.093984f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.640623f, 0.220574f, 0.000000f), new Vector3(0.640623f, 0.738442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 0.738442f, 0.000000f), new Vector3(0.540885f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.772967f, 0.000000f), new Vector3(0.423885f, 0.786393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 0.786393f, 0.000000f), new Vector3(0.324147f, 0.769131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.324147f, 0.769131f, 0.000000f), new Vector3(0.243590f, 0.717344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.243590f, 0.717344f, 0.000000f), new Vector3(0.226328f, 0.698164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226328f, 0.698164f, 0.000000f), new Vector3(0.182213f, 0.619524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.619524f, 0.000000f), new Vector3(0.159197f, 0.519787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.519787f, 0.000000f), new Vector3(0.155361f, 0.444983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, 0.444983f, 0.000000f), new Vector3(0.164951f, 0.329901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.329901f, 0.000000f), new Vector3(0.191803f, 0.237836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.237836f, 0.000000f), new Vector3(0.210983f, 0.203311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, 0.203311f, 0.000000f), new Vector3(0.280033f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.280033f, 0.140016f, 0.000000f), new Vector3(0.379770f, 0.118918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.379770f, 0.118918f, 0.000000f), new Vector3(0.485262f, 0.136180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, 0.136180f, 0.000000f), new Vector3(0.515951f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.147688f, 0.000000f), new Vector3(0.606098f, 0.195639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.195639f, 0.000000f), new Vector3(0.640623f, 0.220574f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__E
        private static List<Line3> getCharE()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.790229f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.000000f, 0.000000f), new Vector3(0.790229f, 0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.141934f, 0.000000f), new Vector3(0.159197f, 0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.141934f, 0.000000f), new Vector3(0.159197f, 0.588836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.588836f, 0.000000f), new Vector3(0.790229f, 0.588836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.588836f, 0.000000f), new Vector3(0.790229f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.728852f, 0.000000f), new Vector3(0.159197f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.728852f, 0.000000f), new Vector3(0.159197f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.058753f, 0.000000f), new Vector3(0.790229f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 1.058753f, 0.000000f), new Vector3(0.790229f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 1.200688f, 0.000000f), new Vector3(0.000000f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.200688f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharEs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.794065f, 0.053705f, 0.000000f), new Vector3(0.794065f, 0.218656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.794065f, 0.218656f, 0.000000f), new Vector3(0.786393f, 0.218656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.786393f, 0.218656f, 0.000000f), new Vector3(0.755704f, 0.195639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 0.195639f, 0.000000f), new Vector3(0.654049f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.654049f, 0.147688f, 0.000000f), new Vector3(0.567737f, 0.120836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567737f, 0.120836f, 0.000000f), new Vector3(0.468000f, 0.111246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.468000f, 0.111246f, 0.000000f), new Vector3(0.441147f, 0.111246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.111246f, 0.000000f), new Vector3(0.343328f, 0.130426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.130426f, 0.000000f), new Vector3(0.327983f, 0.134262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 0.134262f, 0.000000f), new Vector3(0.241672f, 0.187967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 0.187967f, 0.000000f), new Vector3(0.228246f, 0.203311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.228246f, 0.203311f, 0.000000f), new Vector3(0.174541f, 0.289623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.174541f, 0.289623f, 0.000000f), new Vector3(0.161115f, 0.329901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.329901f, 0.000000f), new Vector3(0.149606f, 0.433475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.433475f, 0.000000f), new Vector3(0.811327f, 0.433475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811327f, 0.433475f, 0.000000f), new Vector3(0.811327f, 0.544721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.811327f, 0.544721f, 0.000000f), new Vector3(0.797901f, 0.652131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.797901f, 0.652131f, 0.000000f), new Vector3(0.765295f, 0.744196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 0.744196f, 0.000000f), new Vector3(0.713508f, 0.818999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.713508f, 0.818999f, 0.000000f), new Vector3(0.632950f, 0.880376f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.632950f, 0.880376f, 0.000000f), new Vector3(0.540885f, 0.912983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.912983f, 0.000000f), new Vector3(0.433475f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 0.924491f, 0.000000f), new Vector3(0.375934f, 0.922573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.375934f, 0.922573f, 0.000000f), new Vector3(0.276197f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276197f, 0.899557f, 0.000000f), new Vector3(0.189885f, 0.857360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.189885f, 0.857360f, 0.000000f), new Vector3(0.117000f, 0.794065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.117000f, 0.794065f, 0.000000f), new Vector3(0.070967f, 0.736524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070967f, 0.736524f, 0.000000f), new Vector3(0.028770f, 0.652131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.028770f, 0.652131f, 0.000000f), new Vector3(0.003836f, 0.554311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.554311f, 0.000000f), new Vector3(-0.003836f, 0.444983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(-0.003836f, 0.444983f, 0.000000f), new Vector3(0.000000f, 0.358672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.358672f, 0.000000f), new Vector3(0.023016f, 0.257016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023016f, 0.257016f, 0.000000f), new Vector3(0.063295f, 0.172623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.063295f, 0.172623f, 0.000000f), new Vector3(0.120836f, 0.099738f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 0.099738f, 0.000000f), new Vector3(0.176459f, 0.055623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176459f, 0.055623f, 0.000000f), new Vector3(0.260852f, 0.013426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.260852f, 0.013426f, 0.000000f), new Vector3(0.358672f, -0.013426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.358672f, -0.013426f, 0.000000f), new Vector3(0.469918f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.469918f, -0.021098f, 0.000000f), new Vector3(0.538967f, -0.017262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.538967f, -0.017262f, 0.000000f), new Vector3(0.638705f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.638705f, 0.000000f, 0.000000f), new Vector3(0.794065f, 0.053705f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.149606f, 0.550475f, 0.000000f), new Vector3(0.663639f, 0.550475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.663639f, 0.550475f, 0.000000f), new Vector3(0.648295f, 0.657885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.648295f, 0.657885f, 0.000000f), new Vector3(0.604180f, 0.734606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.604180f, 0.734606f, 0.000000f), new Vector3(0.529377f, 0.784475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, 0.784475f, 0.000000f), new Vector3(0.423885f, 0.799819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 0.799819f, 0.000000f), new Vector3(0.320311f, 0.784475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.320311f, 0.784475f, 0.000000f), new Vector3(0.232082f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232082f, 0.728852f, 0.000000f), new Vector3(0.174541f, 0.646377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.174541f, 0.646377f, 0.000000f), new Vector3(0.149606f, 0.550475f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__F
        private static List<Line3> getCharF()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.577327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.577327f, 0.000000f), new Vector3(0.678983f, 0.577327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678983f, 0.577327f, 0.000000f), new Vector3(0.678983f, 0.719262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678983f, 0.719262f, 0.000000f), new Vector3(0.159197f, 0.719262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.719262f, 0.000000f), new Vector3(0.159197f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.058753f, 0.000000f), new Vector3(0.765295f, 1.058753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 1.058753f, 0.000000f), new Vector3(0.765295f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 1.200688f, 0.000000f), new Vector3(0.000000f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.200688f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharFs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.101656f, 0.000000f, 0.000000f), new Vector3(0.253180f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.253180f, 0.000000f, 0.000000f), new Vector3(0.253180f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.253180f, 0.772967f, 0.000000f), new Vector3(0.521705f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.521705f, 0.772967f, 0.000000f), new Vector3(0.521705f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.521705f, 0.899557f, 0.000000f), new Vector3(0.249344f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.899557f, 0.000000f), new Vector3(0.249344f, 0.930245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.930245f, 0.000000f), new Vector3(0.255098f, 1.008885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.255098f, 1.008885f, 0.000000f), new Vector3(0.293459f, 1.085606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293459f, 1.085606f, 0.000000f), new Vector3(0.327983f, 1.110540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 1.110540f, 0.000000f), new Vector3(0.433475f, 1.129721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 1.129721f, 0.000000f), new Vector3(0.504442f, 1.122048f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.504442f, 1.122048f, 0.000000f), new Vector3(0.569655f, 1.106704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.569655f, 1.106704f, 0.000000f), new Vector3(0.577327f, 1.106704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.577327f, 1.106704f, 0.000000f), new Vector3(0.577327f, 1.244802f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.577327f, 1.244802f, 0.000000f), new Vector3(0.504442f, 1.256311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.504442f, 1.256311f, 0.000000f), new Vector3(0.412377f, 1.262065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 1.262065f, 0.000000f), new Vector3(0.351000f, 1.258229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.351000f, 1.258229f, 0.000000f), new Vector3(0.255098f, 1.229458f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.255098f, 1.229458f, 0.000000f), new Vector3(0.182213f, 1.175753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 1.175753f, 0.000000f), new Vector3(0.147688f, 1.129721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.147688f, 1.129721f, 0.000000f), new Vector3(0.113164f, 1.039573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.113164f, 1.039573f, 0.000000f), new Vector3(0.101656f, 0.930245f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 0.930245f, 0.000000f), new Vector3(0.101656f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.772967f, 0.000000f), new Vector3(0.101656f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 0.772967f, 0.000000f), new Vector3(0.101656f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__G
        private static List<Line3> getCharG()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(1.068344f, 0.087788f, 0.000000f), new Vector3(1.068344f, 0.603739f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.068344f, 0.603739f, 0.000000f), new Vector3(0.588836f, 0.603739f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.588836f, 0.603739f, 0.000000f), new Vector3(0.588836f, 0.463722f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.588836f, 0.463722f, 0.000000f), new Vector3(0.911065f, 0.463722f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.911065f, 0.463722f, 0.000000f), new Vector3(0.911065f, 0.168345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.911065f, 0.168345f, 0.000000f), new Vector3(0.878458f, 0.156837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878458f, 0.156837f, 0.000000f), new Vector3(0.776803f, 0.129985f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.776803f, 0.129985f, 0.000000f), new Vector3(0.723098f, 0.120395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.723098f, 0.120395f, 0.000000f), new Vector3(0.621442f, 0.114641f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.621442f, 0.114641f, 0.000000f), new Vector3(0.546639f, 0.118477f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546639f, 0.118477f, 0.000000f), new Vector3(0.446901f, 0.141493f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446901f, 0.141493f, 0.000000f), new Vector3(0.360590f, 0.183690f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.360590f, 0.183690f, 0.000000f), new Vector3(0.287705f, 0.245067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287705f, 0.245067f, 0.000000f), new Vector3(0.237836f, 0.308362f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.237836f, 0.308362f, 0.000000f), new Vector3(0.197557f, 0.394673f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.394673f, 0.000000f), new Vector3(0.174541f, 0.492493f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.174541f, 0.492493f, 0.000000f), new Vector3(0.166869f, 0.605657f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.605657f, 0.000000f), new Vector3(0.170705f, 0.691968f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.691968f, 0.000000f), new Vector3(0.191803f, 0.793624f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.793624f, 0.000000f), new Vector3(0.230164f, 0.881853f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.230164f, 0.881853f, 0.000000f), new Vector3(0.283869f, 0.956656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.283869f, 0.956656f, 0.000000f), new Vector3(0.322229f, 0.993099f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.322229f, 0.993099f, 0.000000f), new Vector3(0.400869f, 1.044886f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.400869f, 1.044886f, 0.000000f), new Vector3(0.494852f, 1.075574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.494852f, 1.075574f, 0.000000f), new Vector3(0.602262f, 1.085165f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602262f, 1.085165f, 0.000000f), new Vector3(0.663639f, 1.083246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.663639f, 1.083246f, 0.000000f), new Vector3(0.761459f, 1.069820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.761459f, 1.069820f, 0.000000f), new Vector3(0.792147f, 1.062148f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.792147f, 1.062148f, 0.000000f), new Vector3(0.888049f, 1.027624f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.888049f, 1.027624f, 0.000000f), new Vector3(0.980114f, 0.975837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.980114f, 0.975837f, 0.000000f), new Vector3(1.052999f, 0.924050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.052999f, 0.924050f, 0.000000f), new Vector3(1.068344f, 0.924050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.068344f, 0.924050f, 0.000000f), new Vector3(1.068344f, 1.113935f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.068344f, 1.113935f, 0.000000f), new Vector3(1.028065f, 1.131197f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.028065f, 1.131197f, 0.000000f), new Vector3(0.930245f, 1.171476f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.930245f, 1.171476f, 0.000000f), new Vector3(0.838180f, 1.198328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.838180f, 1.198328f, 0.000000f), new Vector3(0.728852f, 1.215591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728852f, 1.215591f, 0.000000f), new Vector3(0.629114f, 1.221345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.629114f, 1.221345f, 0.000000f), new Vector3(0.515951f, 1.215591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 1.215591f, 0.000000f), new Vector3(0.414295f, 1.194492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 1.194492f, 0.000000f), new Vector3(0.322229f, 1.161886f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.322229f, 1.161886f, 0.000000f), new Vector3(0.239754f, 1.115853f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.239754f, 1.115853f, 0.000000f), new Vector3(0.168787f, 1.056394f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.168787f, 1.056394f, 0.000000f), new Vector3(0.109328f, 0.985427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.109328f, 0.985427f, 0.000000f), new Vector3(0.061377f, 0.904870f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.061377f, 0.904870f, 0.000000f), new Vector3(0.026852f, 0.812804f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.026852f, 0.812804f, 0.000000f), new Vector3(0.007672f, 0.711148f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.711148f, 0.000000f), new Vector3(0.000000f, 0.601821f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.601821f, 0.000000f), new Vector3(0.003836f, 0.519345f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.519345f, 0.000000f), new Vector3(0.019180f, 0.417690f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.019180f, 0.417690f, 0.000000f), new Vector3(0.046033f, 0.327542f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.046033f, 0.327542f, 0.000000f), new Vector3(0.057541f, 0.296854f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.057541f, 0.296854f, 0.000000f), new Vector3(0.109328f, 0.206706f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.109328f, 0.206706f, 0.000000f), new Vector3(0.172623f, 0.131903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.131903f, 0.000000f), new Vector3(0.193721f, 0.112723f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 0.112723f, 0.000000f), new Vector3(0.276197f, 0.057100f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276197f, 0.057100f, 0.000000f), new Vector3(0.372098f, 0.014903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.372098f, 0.014903f, 0.000000f), new Vector3(0.423885f, -0.000441f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, -0.000441f, 0.000000f), new Vector3(0.523623f, -0.017704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.523623f, -0.017704f, 0.000000f), new Vector3(0.629114f, -0.023458f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.629114f, -0.023458f, 0.000000f), new Vector3(0.652131f, -0.023458f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652131f, -0.023458f, 0.000000f), new Vector3(0.751868f, -0.013868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, -0.013868f, 0.000000f), new Vector3(0.876540f, 0.016821f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.876540f, 0.016821f, 0.000000f), new Vector3(0.976278f, 0.051346f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.976278f, 0.051346f, 0.000000f), new Vector3(1.068344f, 0.087788f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharGs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.093984f, -0.303049f, 0.000000f), new Vector3(0.126590f, -0.310721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126590f, -0.310721f, 0.000000f), new Vector3(0.228246f, -0.331819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.228246f, -0.331819f, 0.000000f), new Vector3(0.264688f, -0.335656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.264688f, -0.335656f, 0.000000f), new Vector3(0.368262f, -0.341410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.368262f, -0.341410f, 0.000000f), new Vector3(0.423885f, -0.339492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, -0.339492f, 0.000000f), new Vector3(0.529377f, -0.322229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, -0.322229f, 0.000000f), new Vector3(0.617606f, -0.287705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617606f, -0.287705f, 0.000000f), new Vector3(0.686655f, -0.234000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.686655f, -0.234000f, 0.000000f), new Vector3(0.719262f, -0.195639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719262f, -0.195639f, 0.000000f), new Vector3(0.759540f, -0.115082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, -0.115082f, 0.000000f), new Vector3(0.782557f, -0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, -0.015344f, 0.000000f), new Vector3(0.792147f, 0.101656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.792147f, 0.101656f, 0.000000f), new Vector3(0.792147f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.792147f, 0.899557f, 0.000000f), new Vector3(0.650213f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.650213f, 0.899557f, 0.000000f), new Vector3(0.640623f, 0.861196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 0.861196f, 0.000000f), new Vector3(0.621442f, 0.870786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.621442f, 0.870786f, 0.000000f), new Vector3(0.529377f, 0.909147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, 0.909147f, 0.000000f), new Vector3(0.502524f, 0.914901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502524f, 0.914901f, 0.000000f), new Vector3(0.397033f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.397033f, 0.924491f, 0.000000f), new Vector3(0.347164f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.347164f, 0.920655f, 0.000000f), new Vector3(0.249344f, 0.893803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.893803f, 0.000000f), new Vector3(0.199475f, 0.866950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199475f, 0.866950f, 0.000000f), new Vector3(0.120836f, 0.803655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 0.803655f, 0.000000f), new Vector3(0.080557f, 0.749950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.080557f, 0.749950f, 0.000000f), new Vector3(0.032607f, 0.657885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032607f, 0.657885f, 0.000000f), new Vector3(0.007672f, 0.565819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.565819f, 0.000000f), new Vector3(0.000000f, 0.458410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.458410f, 0.000000f), new Vector3(0.001918f, 0.404705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.001918f, 0.404705f, 0.000000f), new Vector3(0.017262f, 0.297295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017262f, 0.297295f, 0.000000f), new Vector3(0.047951f, 0.205229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047951f, 0.205229f, 0.000000f), new Vector3(0.093984f, 0.130426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.093984f, 0.130426f, 0.000000f), new Vector3(0.161115f, 0.067131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.067131f, 0.000000f), new Vector3(0.247426f, 0.026852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.247426f, 0.026852f, 0.000000f), new Vector3(0.349082f, 0.013426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349082f, 0.013426f, 0.000000f), new Vector3(0.418131f, 0.017262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 0.017262f, 0.000000f), new Vector3(0.510196f, 0.040279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.510196f, 0.040279f, 0.000000f), new Vector3(0.552393f, 0.061377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552393f, 0.061377f, 0.000000f), new Vector3(0.640623f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 0.122754f, 0.000000f), new Vector3(0.640623f, 0.040279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 0.040279f, 0.000000f), new Vector3(0.627196f, -0.059459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.627196f, -0.059459f, 0.000000f), new Vector3(0.586918f, -0.134262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.586918f, -0.134262f, 0.000000f), new Vector3(0.508278f, -0.186049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, -0.186049f, 0.000000f), new Vector3(0.485262f, -0.193721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, -0.193721f, 0.000000f), new Vector3(0.379770f, -0.205229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.379770f, -0.205229f, 0.000000f), new Vector3(0.320311f, -0.201393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.320311f, -0.201393f, 0.000000f), new Vector3(0.218656f, -0.184131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.218656f, -0.184131f, 0.000000f), new Vector3(0.101656f, -0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, -0.147688f, 0.000000f), new Vector3(0.093984f, -0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.093984f, -0.147688f, 0.000000f), new Vector3(0.093984f, -0.303049f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.640623f, 0.247426f, 0.000000f), new Vector3(0.640623f, 0.738442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 0.738442f, 0.000000f), new Vector3(0.542803f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.542803f, 0.772967f, 0.000000f), new Vector3(0.423885f, 0.788311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 0.788311f, 0.000000f), new Vector3(0.322229f, 0.771049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.322229f, 0.771049f, 0.000000f), new Vector3(0.241672f, 0.719262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 0.719262f, 0.000000f), new Vector3(0.228246f, 0.703918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.228246f, 0.703918f, 0.000000f), new Vector3(0.182213f, 0.627196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.627196f, 0.000000f), new Vector3(0.159197f, 0.527459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.527459f, 0.000000f), new Vector3(0.155361f, 0.460328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, 0.460328f, 0.000000f), new Vector3(0.164951f, 0.345246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.345246f, 0.000000f), new Vector3(0.193721f, 0.257016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 0.257016f, 0.000000f), new Vector3(0.209065f, 0.232082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.209065f, 0.232082f, 0.000000f), new Vector3(0.278115f, 0.174541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.278115f, 0.174541f, 0.000000f), new Vector3(0.381688f, 0.153443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.381688f, 0.153443f, 0.000000f), new Vector3(0.485262f, 0.168787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.485262f, 0.168787f, 0.000000f), new Vector3(0.517869f, 0.178377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517869f, 0.178377f, 0.000000f), new Vector3(0.609934f, 0.224410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.609934f, 0.224410f, 0.000000f), new Vector3(0.640623f, 0.247426f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__H
        private static List<Line3> getCharH()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.161115f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.000000f, 0.000000f), new Vector3(0.161115f, 0.588836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.588836f, 0.000000f), new Vector3(0.757622f, 0.588836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.757622f, 0.588836f, 0.000000f), new Vector3(0.757622f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.757622f, 0.000000f, 0.000000f), new Vector3(0.918737f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.918737f, 0.000000f, 0.000000f), new Vector3(0.918737f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.918737f, 1.200688f, 0.000000f), new Vector3(0.757622f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.757622f, 1.200688f, 0.000000f), new Vector3(0.757622f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.757622f, 0.728852f, 0.000000f), new Vector3(0.161115f, 0.728852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.728852f, 0.000000f), new Vector3(0.161115f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 1.200688f, 0.000000f), new Vector3(0.000000f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.200688f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharHs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.671311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.671311f, 0.000000f), new Vector3(0.191803f, 0.700082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.700082f, 0.000000f), new Vector3(0.280033f, 0.749950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.280033f, 0.749950f, 0.000000f), new Vector3(0.312639f, 0.763377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.763377f, 0.000000f), new Vector3(0.410459f, 0.780639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.410459f, 0.780639f, 0.000000f), new Vector3(0.508278f, 0.763377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, 0.763377f, 0.000000f), new Vector3(0.565819f, 0.713508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.565819f, 0.713508f, 0.000000f), new Vector3(0.592672f, 0.629114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.592672f, 0.629114f, 0.000000f), new Vector3(0.600344f, 0.512114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.512114f, 0.000000f), new Vector3(0.600344f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.000000f, 0.000000f), new Vector3(0.751868f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.000000f, 0.000000f), new Vector3(0.751868f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.583082f, 0.000000f), new Vector3(0.746114f, 0.663639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746114f, 0.663639f, 0.000000f), new Vector3(0.721180f, 0.759540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.721180f, 0.759540f, 0.000000f), new Vector3(0.675147f, 0.836262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675147f, 0.836262f, 0.000000f), new Vector3(0.642541f, 0.866950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.642541f, 0.866950f, 0.000000f), new Vector3(0.560065f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.560065f, 0.911065f, 0.000000f), new Vector3(0.452655f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452655f, 0.924491f, 0.000000f), new Vector3(0.395115f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.395115f, 0.920655f, 0.000000f), new Vector3(0.297295f, 0.891885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.297295f, 0.891885f, 0.000000f), new Vector3(0.235918f, 0.859278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235918f, 0.859278f, 0.000000f), new Vector3(0.151524f, 0.799819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.799819f, 0.000000f), new Vector3(0.151524f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 1.252475f, 0.000000f), new Vector3(0.000000f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.252475f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));
            
            return b0;
        }
        #endregion

        #region verdana_12_regular__I
        private static List<Line3> getCharI()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.473754f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, 0.000000f, 0.000000f), new Vector3(0.473754f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, 0.122754f, 0.000000f), new Vector3(0.316475f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 0.122754f, 0.000000f), new Vector3(0.316475f, 1.077934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 1.077934f, 0.000000f), new Vector3(0.473754f, 1.077934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, 1.077934f, 0.000000f), new Vector3(0.473754f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, 1.200688f, 0.000000f), new Vector3(0.000000f, 1.200688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.200688f, 0.000000f), new Vector3(0.000000f, 1.077934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.077934f, 0.000000f), new Vector3(0.157279f, 1.077934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.157279f, 1.077934f, 0.000000f), new Vector3(0.157279f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.157279f, 0.122754f, 0.000000f), new Vector3(0.000000f, 0.122754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.122754f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharIs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.009590f, 0.000000f, 0.000000f), new Vector3(0.161115f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.000000f, 0.000000f), new Vector3(0.161115f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.899557f, 0.000000f), new Vector3(0.009590f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.009590f, 0.899557f, 0.000000f), new Vector3(0.009590f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.000000f, 1.051081f, 0.000000f), new Vector3(0.170705f, 1.051081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 1.051081f, 0.000000f), new Vector3(0.170705f, 1.208360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 1.208360f, 0.000000f), new Vector3(0.000000f, 1.208360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.208360f, 0.000000f), new Vector3(0.000000f, 1.051081f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__J
        private static List<Line3> getCharJ()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.151524f, 1.199805f, 0.000000f), new Vector3(0.563901f, 1.199805f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.563901f, 1.199805f, 0.000000f), new Vector3(0.563901f, 0.311756f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.563901f, 0.311756f, 0.000000f), new Vector3(0.550475f, 0.204347f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.550475f, 0.204347f, 0.000000f), new Vector3(0.512114f, 0.118035f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.512114f, 0.118035f, 0.000000f), new Vector3(0.468000f, 0.068166f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.468000f, 0.068166f, 0.000000f), new Vector3(0.389360f, 0.018298f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.389360f, 0.018298f, 0.000000f), new Vector3(0.295377f, -0.010473f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.295377f, -0.010473f, 0.000000f), new Vector3(0.210983f, -0.016227f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, -0.016227f, 0.000000f), new Vector3(0.107410f, -0.010473f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.107410f, -0.010473f, 0.000000f), new Vector3(0.003836f, 0.004871f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.004871f, 0.000000f), new Vector3(0.000000f, 0.006789f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.006789f, 0.000000f), new Vector3(0.000000f, 0.156396f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.156396f, 0.000000f), new Vector3(0.007672f, 0.156396f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.156396f, 0.000000f), new Vector3(0.090147f, 0.131462f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.090147f, 0.131462f, 0.000000f), new Vector3(0.187967f, 0.119953f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.187967f, 0.119953f, 0.000000f), new Vector3(0.293459f, 0.133380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293459f, 0.133380f, 0.000000f), new Vector3(0.306885f, 0.137216f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.306885f, 0.137216f, 0.000000f), new Vector3(0.370180f, 0.185166f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370180f, 0.185166f, 0.000000f), new Vector3(0.398951f, 0.263806f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.398951f, 0.263806f, 0.000000f), new Vector3(0.404705f, 0.373134f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.404705f, 0.373134f, 0.000000f), new Vector3(0.404705f, 1.073215f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.404705f, 1.073215f, 0.000000f), new Vector3(0.151524f, 1.073215f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 1.073215f, 0.000000f), new Vector3(0.151524f, 1.199805f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharJs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.308803f, 1.051081f, 0.000000f), new Vector3(0.479508f, 1.051081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479508f, 1.051081f, 0.000000f), new Vector3(0.479508f, 1.208360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479508f, 1.208360f, 0.000000f), new Vector3(0.308803f, 1.208360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308803f, 1.208360f, 0.000000f), new Vector3(0.308803f, 1.051081f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.130426f, 0.772967f, 0.000000f), new Vector3(0.318393f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.318393f, 0.772967f, 0.000000f), new Vector3(0.318393f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.318393f, 0.038361f, 0.000000f), new Vector3(0.312639f, -0.063295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, -0.063295f, 0.000000f), new Vector3(0.289623f, -0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.289623f, -0.141934f, 0.000000f), new Vector3(0.237836f, -0.189885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.237836f, -0.189885f, 0.000000f), new Vector3(0.143852f, -0.205229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143852f, -0.205229f, 0.000000f), new Vector3(0.069049f, -0.195639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.069049f, -0.195639f, 0.000000f), new Vector3(0.007672f, -0.174541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, -0.174541f, 0.000000f), new Vector3(0.000000f, -0.174541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.174541f, 0.000000f), new Vector3(0.000000f, -0.318393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, -0.318393f, 0.000000f), new Vector3(0.088229f, -0.335656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.088229f, -0.335656f, 0.000000f), new Vector3(0.172623f, -0.341410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, -0.341410f, 0.000000f), new Vector3(0.212902f, -0.339492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.212902f, -0.339492f, 0.000000f), new Vector3(0.310721f, -0.314557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.310721f, -0.314557f, 0.000000f), new Vector3(0.389360f, -0.260852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.389360f, -0.260852f, 0.000000f), new Vector3(0.421967f, -0.220574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.421967f, -0.220574f, 0.000000f), new Vector3(0.458410f, -0.132344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458410f, -0.132344f, 0.000000f), new Vector3(0.469918f, -0.023016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.469918f, -0.023016f, 0.000000f), new Vector3(0.469918f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.469918f, 0.899557f, 0.000000f), new Vector3(0.130426f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.130426f, 0.899557f, 0.000000f), new Vector3(0.130426f, 0.772967f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__K
        private static List<Line3> getCharK()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.406623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.406623f, 0.000000f), new Vector3(0.278115f, 0.533213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.278115f, 0.533213f, 0.000000f), new Vector3(0.751868f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.000000f, 0.000000f), new Vector3(0.959016f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.959016f, 0.000000f, 0.000000f), new Vector3(0.398951f, 0.634868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.398951f, 0.634868f, 0.000000f), new Vector3(0.934081f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.934081f, 1.198770f, 0.000000f), new Vector3(0.742278f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.742278f, 1.198770f, 0.000000f), new Vector3(0.159197f, 0.573491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.573491f, 0.000000f), new Vector3(0.159197f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharKs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.299213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.299213f, 0.000000f), new Vector3(0.249344f, 0.393196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.393196f, 0.000000f), new Vector3(0.609934f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.609934f, 0.000000f, 0.000000f), new Vector3(0.809409f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.809409f, 0.000000f, 0.000000f), new Vector3(0.362508f, 0.483344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 0.483344f, 0.000000f), new Vector3(0.778721f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778721f, 0.899557f, 0.000000f), new Vector3(0.588836f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.588836f, 0.899557f, 0.000000f), new Vector3(0.151524f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.448819f, 0.000000f), new Vector3(0.151524f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 1.252475f, 0.000000f), new Vector3(0.000000f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.252475f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__L
        private static List<Line3> getCharL()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.757622f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.757622f, 0.000000f, 0.000000f), new Vector3(0.757622f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.757622f, 0.140016f, 0.000000f), new Vector3(0.159197f, 0.140016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.140016f, 0.000000f), new Vector3(0.159197f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharLs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.000000f, 0.000000f), new Vector3(0.151524f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 1.252475f, 0.000000f), new Vector3(0.000000f, 1.252475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.252475f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__M
        private static List<Line3> getCharM()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.149606f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.000000f, 0.000000f), new Vector3(0.149606f, 1.033819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 1.033819f, 0.000000f), new Vector3(0.479508f, 0.329901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479508f, 0.329901f, 0.000000f), new Vector3(0.575409f, 0.329901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575409f, 0.329901f, 0.000000f), new Vector3(0.909147f, 1.033819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.909147f, 1.033819f, 0.000000f), new Vector3(0.909147f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.909147f, 0.000000f, 0.000000f), new Vector3(1.068344f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.068344f, 0.000000f, 0.000000f), new Vector3(1.068344f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.068344f, 1.198770f, 0.000000f), new Vector3(0.845852f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.845852f, 1.198770f, 0.000000f), new Vector3(0.537049f, 0.531295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537049f, 0.531295f, 0.000000f), new Vector3(0.216738f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.216738f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharMs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.671311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.671311f, 0.000000f), new Vector3(0.189885f, 0.698164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.189885f, 0.698164f, 0.000000f), new Vector3(0.276197f, 0.749950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276197f, 0.749950f, 0.000000f), new Vector3(0.303049f, 0.761459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.303049f, 0.761459f, 0.000000f), new Vector3(0.400869f, 0.780639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.400869f, 0.780639f, 0.000000f), new Vector3(0.500606f, 0.763377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.500606f, 0.763377f, 0.000000f), new Vector3(0.554311f, 0.711590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.554311f, 0.711590f, 0.000000f), new Vector3(0.577327f, 0.625278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.577327f, 0.625278f, 0.000000f), new Vector3(0.581164f, 0.512114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.581164f, 0.512114f, 0.000000f), new Vector3(0.581164f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.581164f, 0.000000f, 0.000000f), new Vector3(0.732688f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.732688f, 0.000000f, 0.000000f), new Vector3(0.732688f, 0.575409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.732688f, 0.575409f, 0.000000f), new Vector3(0.730770f, 0.625278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.730770f, 0.625278f, 0.000000f), new Vector3(0.726934f, 0.667475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.726934f, 0.667475f, 0.000000f), new Vector3(0.767213f, 0.698164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767213f, 0.698164f, 0.000000f), new Vector3(0.853524f, 0.748032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.853524f, 0.748032f, 0.000000f), new Vector3(0.884213f, 0.761459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.884213f, 0.761459f, 0.000000f), new Vector3(0.982032f, 0.780639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.982032f, 0.780639f, 0.000000f), new Vector3(1.081770f, 0.763377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.081770f, 0.763377f, 0.000000f), new Vector3(1.135475f, 0.709672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.135475f, 0.709672f, 0.000000f), new Vector3(1.158491f, 0.615688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.158491f, 0.615688f, 0.000000f), new Vector3(1.162327f, 0.512114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.162327f, 0.512114f, 0.000000f), new Vector3(1.162327f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.162327f, 0.000000f, 0.000000f), new Vector3(1.313852f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.313852f, 0.000000f, 0.000000f), new Vector3(1.313852f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.313852f, 0.583082f, 0.000000f), new Vector3(1.310016f, 0.655967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.310016f, 0.655967f, 0.000000f), new Vector3(1.286999f, 0.755704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.286999f, 0.755704f, 0.000000f), new Vector3(1.242884f, 0.834344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.242884f, 0.834344f, 0.000000f), new Vector3(1.214114f, 0.865032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.214114f, 0.865032f, 0.000000f), new Vector3(1.131639f, 0.909147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.131639f, 0.909147f, 0.000000f), new Vector3(1.024229f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.024229f, 0.924491f, 0.000000f), new Vector3(0.959016f, 0.918737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.959016f, 0.918737f, 0.000000f), new Vector3(0.863114f, 0.889967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.863114f, 0.889967f, 0.000000f), new Vector3(0.784475f, 0.845852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.784475f, 0.845852f, 0.000000f), new Vector3(0.694327f, 0.778721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.694327f, 0.778721f, 0.000000f), new Vector3(0.673229f, 0.817081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.673229f, 0.817081f, 0.000000f), new Vector3(0.600344f, 0.886131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.886131f, 0.000000f), new Vector3(0.546639f, 0.909147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546639f, 0.909147f, 0.000000f), new Vector3(0.443065f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 0.924491f, 0.000000f), new Vector3(0.391278f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.391278f, 0.920655f, 0.000000f), new Vector3(0.293459f, 0.891885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293459f, 0.891885f, 0.000000f), new Vector3(0.237836f, 0.861196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.237836f, 0.861196f, 0.000000f), new Vector3(0.151524f, 0.799819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.799819f, 0.000000f), new Vector3(0.151524f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__N
        private static List<Line3> getCharN()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.147688f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.147688f, 0.000000f, 0.000000f), new Vector3(0.147688f, 1.072180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.147688f, 1.072180f, 0.000000f), new Vector3(0.717344f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717344f, 0.000000f, 0.000000f), new Vector3(0.914901f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.914901f, 0.000000f, 0.000000f), new Vector3(0.914901f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.914901f, 1.198770f, 0.000000f), new Vector3(0.765295f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 1.198770f, 0.000000f), new Vector3(0.765295f, 0.218656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.765295f, 0.218656f, 0.000000f), new Vector3(0.247426f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.247426f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharNs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.000000f, 0.000000f), new Vector3(0.151524f, 0.671311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.671311f, 0.000000f), new Vector3(0.191803f, 0.700082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.700082f, 0.000000f), new Vector3(0.280033f, 0.749950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.280033f, 0.749950f, 0.000000f), new Vector3(0.312639f, 0.763377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.763377f, 0.000000f), new Vector3(0.410459f, 0.780639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.410459f, 0.780639f, 0.000000f), new Vector3(0.508278f, 0.763377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, 0.763377f, 0.000000f), new Vector3(0.565819f, 0.713508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.565819f, 0.713508f, 0.000000f), new Vector3(0.592672f, 0.629114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.592672f, 0.629114f, 0.000000f), new Vector3(0.600344f, 0.512114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.512114f, 0.000000f), new Vector3(0.600344f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.000000f, 0.000000f), new Vector3(0.751868f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.000000f, 0.000000f), new Vector3(0.751868f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.583082f, 0.000000f), new Vector3(0.746114f, 0.663639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746114f, 0.663639f, 0.000000f), new Vector3(0.721180f, 0.759540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.721180f, 0.759540f, 0.000000f), new Vector3(0.675147f, 0.836262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675147f, 0.836262f, 0.000000f), new Vector3(0.642541f, 0.866950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.642541f, 0.866950f, 0.000000f), new Vector3(0.560065f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.560065f, 0.911065f, 0.000000f), new Vector3(0.452655f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452655f, 0.924491f, 0.000000f), new Vector3(0.395115f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.395115f, 0.920655f, 0.000000f), new Vector3(0.297295f, 0.891885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.297295f, 0.891885f, 0.000000f), new Vector3(0.235918f, 0.859278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235918f, 0.859278f, 0.000000f), new Vector3(0.151524f, 0.799819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.799819f, 0.000000f), new Vector3(0.151524f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__O
        private static List<Line3> getCharO()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.590754f, -0.023899f, 0.000000f), new Vector3(0.694327f, -0.012391f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.694327f, -0.012391f, 0.000000f), new Vector3(0.788311f, 0.016380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 0.016380f, 0.000000f), new Vector3(0.886131f, 0.070085f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.886131f, 0.070085f, 0.000000f), new Vector3(0.962852f, 0.139134f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.962852f, 0.139134f, 0.000000f), new Vector3(1.031901f, 0.236953f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.031901f, 0.236953f, 0.000000f), new Vector3(1.074098f, 0.334773f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.074098f, 0.334773f, 0.000000f), new Vector3(1.091360f, 0.392314f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.091360f, 0.392314f, 0.000000f), new Vector3(1.108622f, 0.492051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.108622f, 0.492051f, 0.000000f), new Vector3(1.114376f, 0.599461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.114376f, 0.599461f, 0.000000f), new Vector3(1.112458f, 0.668510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.112458f, 0.668510f, 0.000000f), new Vector3(1.099032f, 0.772084f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.099032f, 0.772084f, 0.000000f), new Vector3(1.076016f, 0.864150f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.076016f, 0.864150f, 0.000000f), new Vector3(1.066426f, 0.889084f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.066426f, 0.889084f, 0.000000f), new Vector3(1.020393f, 0.983068f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.020393f, 0.983068f, 0.000000f), new Vector3(0.962852f, 1.061707f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.962852f, 1.061707f, 0.000000f), new Vector3(0.878458f, 1.134592f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878458f, 1.134592f, 0.000000f), new Vector3(0.788311f, 1.182543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 1.182543f, 0.000000f), new Vector3(0.759540f, 1.194051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 1.194051f, 0.000000f), new Vector3(0.663639f, 1.217067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.663639f, 1.217067f, 0.000000f), new Vector3(0.556229f, 1.224740f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, 1.224740f, 0.000000f), new Vector3(0.525541f, 1.224740f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.525541f, 1.224740f, 0.000000f), new Vector3(0.423885f, 1.211313f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 1.211313f, 0.000000f), new Vector3(0.327983f, 1.182543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 1.182543f, 0.000000f), new Vector3(0.226328f, 1.128838f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226328f, 1.128838f, 0.000000f), new Vector3(0.151524f, 1.061707f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 1.061707f, 0.000000f), new Vector3(0.136180f, 1.042526f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.136180f, 1.042526f, 0.000000f), new Vector3(0.080557f, 0.960051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.080557f, 0.960051f, 0.000000f), new Vector3(0.038361f, 0.864150f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.864150f, 0.000000f), new Vector3(0.023016f, 0.806609f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023016f, 0.806609f, 0.000000f), new Vector3(0.005754f, 0.706871f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005754f, 0.706871f, 0.000000f), new Vector3(0.000000f, 0.599461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.599461f, 0.000000f), new Vector3(0.001918f, 0.530412f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.001918f, 0.530412f, 0.000000f), new Vector3(0.015344f, 0.426838f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.015344f, 0.426838f, 0.000000f), new Vector3(0.040279f, 0.334773f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.040279f, 0.334773f, 0.000000f), new Vector3(0.047951f, 0.311756f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047951f, 0.311756f, 0.000000f), new Vector3(0.092066f, 0.217773f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.092066f, 0.217773f, 0.000000f), new Vector3(0.151524f, 0.139134f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.139134f, 0.000000f), new Vector3(0.159197f, 0.129543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.129543f, 0.000000f), new Vector3(0.235918f, 0.064330f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235918f, 0.064330f, 0.000000f), new Vector3(0.327983f, 0.016380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 0.016380f, 0.000000f), new Vector3(0.354836f, 0.006789f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.354836f, 0.006789f, 0.000000f), new Vector3(0.450737f, -0.016227f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, -0.016227f, 0.000000f), new Vector3(0.556229f, -0.023899f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, -0.023899f, 0.000000f), new Vector3(0.590754f, -0.023899f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.661721f, 0.125707f, 0.000000f), new Vector3(0.749950f, 0.160232f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749950f, 0.160232f, 0.000000f), new Vector3(0.826672f, 0.221609f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.826672f, 0.221609f, 0.000000f), new Vector3(0.889967f, 0.311756f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.889967f, 0.311756f, 0.000000f), new Vector3(0.924491f, 0.399986f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.924491f, 0.399986f, 0.000000f), new Vector3(0.943672f, 0.505478f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943672f, 0.505478f, 0.000000f), new Vector3(0.947508f, 0.599461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.947508f, 0.599461f, 0.000000f), new Vector3(0.939835f, 0.714543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.939835f, 0.714543f, 0.000000f), new Vector3(0.918737f, 0.816199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.918737f, 0.816199f, 0.000000f), new Vector3(0.882295f, 0.902510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.882295f, 0.902510f, 0.000000f), new Vector3(0.843934f, 0.960051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843934f, 0.960051f, 0.000000f), new Vector3(0.771049f, 1.027182f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.771049f, 1.027182f, 0.000000f), new Vector3(0.684737f, 1.069379f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 1.069379f, 0.000000f), new Vector3(0.585000f, 1.086641f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 1.086641f, 0.000000f), new Vector3(0.558147f, 1.086641f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 1.086641f, 0.000000f), new Vector3(0.452655f, 1.075133f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452655f, 1.075133f, 0.000000f), new Vector3(0.362508f, 1.040609f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 1.040609f, 0.000000f), new Vector3(0.287705f, 0.981149f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287705f, 0.981149f, 0.000000f), new Vector3(0.270442f, 0.960051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270442f, 0.960051f, 0.000000f), new Vector3(0.222492f, 0.887166f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.222492f, 0.887166f, 0.000000f), new Vector3(0.189885f, 0.797018f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.189885f, 0.797018f, 0.000000f), new Vector3(0.170705f, 0.693445f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.693445f, 0.000000f), new Vector3(0.166869f, 0.599461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.599461f, 0.000000f), new Vector3(0.172623f, 0.484379f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.484379f, 0.000000f), new Vector3(0.195639f, 0.382724f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.195639f, 0.382724f, 0.000000f), new Vector3(0.232082f, 0.296412f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232082f, 0.296412f, 0.000000f), new Vector3(0.272360f, 0.238871f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.272360f, 0.238871f, 0.000000f), new Vector3(0.345246f, 0.173658f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.345246f, 0.173658f, 0.000000f), new Vector3(0.431557f, 0.131462f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.431557f, 0.131462f, 0.000000f), new Vector3(0.531295f, 0.114199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531295f, 0.114199f, 0.000000f), new Vector3(0.558147f, 0.112281f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 0.112281f, 0.000000f), new Vector3(0.661721f, 0.125707f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharOs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.456492f, -0.023016f, 0.000000f), new Vector3(0.556229f, -0.003836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, -0.003836f, 0.000000f), new Vector3(0.644459f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.644459f, 0.038361f, 0.000000f), new Vector3(0.717344f, 0.101656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717344f, 0.101656f, 0.000000f), new Vector3(0.755704f, 0.153443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 0.153443f, 0.000000f), new Vector3(0.795983f, 0.239754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.795983f, 0.239754f, 0.000000f), new Vector3(0.820917f, 0.337574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.820917f, 0.337574f, 0.000000f), new Vector3(0.828590f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.828590f, 0.448819f, 0.000000f), new Vector3(0.824754f, 0.531295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.824754f, 0.531295f, 0.000000f), new Vector3(0.805573f, 0.632950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805573f, 0.632950f, 0.000000f), new Vector3(0.769131f, 0.721180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.769131f, 0.721180f, 0.000000f), new Vector3(0.717344f, 0.797901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717344f, 0.797901f, 0.000000f), new Vector3(0.690491f, 0.824754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.690491f, 0.824754f, 0.000000f), new Vector3(0.611852f, 0.880376f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611852f, 0.880376f, 0.000000f), new Vector3(0.519787f, 0.912983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.519787f, 0.912983f, 0.000000f), new Vector3(0.414295f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 0.924491f, 0.000000f), new Vector3(0.370180f, 0.922573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370180f, 0.922573f, 0.000000f), new Vector3(0.270442f, 0.903393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270442f, 0.903393f, 0.000000f), new Vector3(0.184131f, 0.861196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.184131f, 0.861196f, 0.000000f), new Vector3(0.111246f, 0.797901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111246f, 0.797901f, 0.000000f), new Vector3(0.072885f, 0.744196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.072885f, 0.744196f, 0.000000f), new Vector3(0.032607f, 0.659803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032607f, 0.659803f, 0.000000f), new Vector3(0.007672f, 0.560065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.560065f, 0.000000f), new Vector3(0.000000f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.448819f, 0.000000f), new Vector3(0.003836f, 0.368262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.368262f, 0.000000f), new Vector3(0.023016f, 0.266606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023016f, 0.266606f, 0.000000f), new Vector3(0.059459f, 0.176459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.059459f, 0.176459f, 0.000000f), new Vector3(0.111246f, 0.101656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111246f, 0.101656f, 0.000000f), new Vector3(0.140016f, 0.072885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 0.072885f, 0.000000f), new Vector3(0.216738f, 0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.216738f, 0.019180f, 0.000000f), new Vector3(0.308803f, -0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308803f, -0.015344f, 0.000000f), new Vector3(0.414295f, -0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, -0.024934f, 0.000000f), new Vector3(0.456492f, -0.023016f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.514032f, 0.122754f, 0.000000f), new Vector3(0.592672f, 0.178377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.592672f, 0.178377f, 0.000000f), new Vector3(0.604180f, 0.191803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.604180f, 0.191803f, 0.000000f), new Vector3(0.646377f, 0.268524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646377f, 0.268524f, 0.000000f), new Vector3(0.669393f, 0.368262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.669393f, 0.368262f, 0.000000f), new Vector3(0.673229f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.673229f, 0.448819f, 0.000000f), new Vector3(0.663639f, 0.565819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.663639f, 0.565819f, 0.000000f), new Vector3(0.636786f, 0.657885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.636786f, 0.657885f, 0.000000f), new Vector3(0.604180f, 0.709672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.604180f, 0.709672f, 0.000000f), new Vector3(0.529377f, 0.769131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, 0.769131f, 0.000000f), new Vector3(0.433475f, 0.794065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 0.794065f, 0.000000f), new Vector3(0.414295f, 0.794065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 0.794065f, 0.000000f), new Vector3(0.312639f, 0.776803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.776803f, 0.000000f), new Vector3(0.235918f, 0.723098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235918f, 0.723098f, 0.000000f), new Vector3(0.224410f, 0.709672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.224410f, 0.709672f, 0.000000f), new Vector3(0.182213f, 0.634868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.634868f, 0.000000f), new Vector3(0.161115f, 0.535131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161115f, 0.535131f, 0.000000f), new Vector3(0.155361f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, 0.448819f, 0.000000f), new Vector3(0.164951f, 0.333737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.333737f, 0.000000f), new Vector3(0.193721f, 0.241672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 0.241672f, 0.000000f), new Vector3(0.224410f, 0.193721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.224410f, 0.193721f, 0.000000f), new Vector3(0.297295f, 0.130426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.297295f, 0.130426f, 0.000000f), new Vector3(0.393196f, 0.105492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.393196f, 0.105492f, 0.000000f), new Vector3(0.414295f, 0.105492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414295f, 0.105492f, 0.000000f), new Vector3(0.514032f, 0.122754f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__P
        private static List<Line3> getCharP()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.446901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.446901f, 0.000000f), new Vector3(0.331819f, 0.446901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.331819f, 0.446901f, 0.000000f), new Vector3(0.441147f, 0.454573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.454573f, 0.000000f), new Vector3(0.531295f, 0.477590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531295f, 0.477590f, 0.000000f), new Vector3(0.598426f, 0.508278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.598426f, 0.508278f, 0.000000f), new Vector3(0.677065f, 0.569655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.677065f, 0.569655f, 0.000000f), new Vector3(0.703918f, 0.600344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.703918f, 0.600344f, 0.000000f), new Vector3(0.755704f, 0.688573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 0.688573f, 0.000000f), new Vector3(0.771049f, 0.734606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.771049f, 0.734606f, 0.000000f), new Vector3(0.782557f, 0.836262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, 0.836262f, 0.000000f), new Vector3(0.776803f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.776803f, 0.920655f, 0.000000f), new Vector3(0.746114f, 1.008885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746114f, 1.008885f, 0.000000f), new Vector3(0.715426f, 1.054917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.715426f, 1.054917f, 0.000000f), new Vector3(0.640623f, 1.123966f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 1.123966f, 0.000000f), new Vector3(0.604180f, 1.145065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.604180f, 1.145065f, 0.000000f), new Vector3(0.506360f, 1.181507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.506360f, 1.181507f, 0.000000f), new Vector3(0.433475f, 1.193016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 1.193016f, 0.000000f), new Vector3(0.324147f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.324147f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.159197f, 0.583082f, 0.000000f), new Vector3(0.293459f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293459f, 0.583082f, 0.000000f), new Vector3(0.402787f, 0.590754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402787f, 0.590754f, 0.000000f), new Vector3(0.450737f, 0.600344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, 0.600344f, 0.000000f), new Vector3(0.540885f, 0.646377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.646377f, 0.000000f), new Vector3(0.602262f, 0.736524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602262f, 0.736524f, 0.000000f), new Vector3(0.617606f, 0.832426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617606f, 0.832426f, 0.000000f), new Vector3(0.598426f, 0.932163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.598426f, 0.932163f, 0.000000f), new Vector3(0.596508f, 0.939835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.596508f, 0.939835f, 0.000000f), new Vector3(0.529377f, 1.014639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, 1.014639f, 0.000000f), new Vector3(0.441147f, 1.051081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 1.051081f, 0.000000f), new Vector3(0.343328f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 1.062590f, 0.000000f), new Vector3(0.316475f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 1.062590f, 0.000000f), new Vector3(0.159197f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.062590f, 0.000000f), new Vector3(0.159197f, 0.583082f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharPs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.331819f, 0.000000f), new Vector3(0.149606f, -0.331819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, -0.331819f, 0.000000f), new Vector3(0.149606f, 0.044115f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.044115f, 0.000000f), new Vector3(0.172623f, 0.032607f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.032607f, 0.000000f), new Vector3(0.266606f, -0.003836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.266606f, -0.003836f, 0.000000f), new Vector3(0.393196f, -0.019180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.393196f, -0.019180f, 0.000000f), new Vector3(0.448819f, -0.013426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.448819f, -0.013426f, 0.000000f), new Vector3(0.546639f, 0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546639f, 0.015344f, 0.000000f), new Vector3(0.592672f, 0.040279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.592672f, 0.040279f, 0.000000f), new Vector3(0.671311f, 0.107410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.671311f, 0.107410f, 0.000000f), new Vector3(0.715426f, 0.164951f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.715426f, 0.164951f, 0.000000f), new Vector3(0.759540f, 0.260852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.260852f, 0.000000f), new Vector3(0.782557f, 0.354836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, 0.354836f, 0.000000f), new Vector3(0.790229f, 0.460328f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.460328f, 0.000000f), new Vector3(0.788311f, 0.525541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 0.525541f, 0.000000f), new Vector3(0.772967f, 0.632950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772967f, 0.632950f, 0.000000f), new Vector3(0.744196f, 0.725016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.744196f, 0.725016f, 0.000000f), new Vector3(0.700082f, 0.801737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.700082f, 0.801737f, 0.000000f), new Vector3(0.634868f, 0.868868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634868f, 0.868868f, 0.000000f), new Vector3(0.548557f, 0.911065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.548557f, 0.911065f, 0.000000f), new Vector3(0.446901f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446901f, 0.924491f, 0.000000f), new Vector3(0.383606f, 0.918737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.383606f, 0.918737f, 0.000000f), new Vector3(0.285787f, 0.889967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285787f, 0.889967f, 0.000000f), new Vector3(0.234000f, 0.863114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.234000f, 0.863114f, 0.000000f), new Vector3(0.149606f, 0.805573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.805573f, 0.000000f), new Vector3(0.149606f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.000000f, -0.331819f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.149606f, 0.168787f, 0.000000f), new Vector3(0.249344f, 0.132344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, 0.132344f, 0.000000f), new Vector3(0.260852f, 0.128508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.260852f, 0.128508f, 0.000000f), new Vector3(0.362508f, 0.117000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 0.117000f, 0.000000f), new Vector3(0.468000f, 0.134262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.468000f, 0.134262f, 0.000000f), new Vector3(0.548557f, 0.186049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.548557f, 0.186049f, 0.000000f), new Vector3(0.563901f, 0.203311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.563901f, 0.203311f, 0.000000f), new Vector3(0.606098f, 0.280033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.280033f, 0.000000f), new Vector3(0.631032f, 0.377852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631032f, 0.377852f, 0.000000f), new Vector3(0.634868f, 0.456492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634868f, 0.456492f, 0.000000f), new Vector3(0.625278f, 0.573491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625278f, 0.573491f, 0.000000f), new Vector3(0.598426f, 0.663639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.598426f, 0.663639f, 0.000000f), new Vector3(0.579246f, 0.700082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.579246f, 0.700082f, 0.000000f), new Vector3(0.508278f, 0.759540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, 0.759540f, 0.000000f), new Vector3(0.408541f, 0.780639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.408541f, 0.780639f, 0.000000f), new Vector3(0.308803f, 0.763377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308803f, 0.763377f, 0.000000f), new Vector3(0.276197f, 0.751868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276197f, 0.751868f, 0.000000f), new Vector3(0.187967f, 0.703918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.187967f, 0.703918f, 0.000000f), new Vector3(0.149606f, 0.677065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.677065f, 0.000000f), new Vector3(0.149606f, 0.168787f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Q
        private static List<Line3> getCharQ()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.629114f, -0.020063f, 0.000000f), new Vector3(0.634868f, -0.069932f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634868f, -0.069932f, 0.000000f), new Vector3(0.665557f, -0.165833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.665557f, -0.165833f, 0.000000f), new Vector3(0.721180f, -0.238719f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.721180f, -0.238719f, 0.000000f), new Vector3(0.753786f, -0.263653f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.753786f, -0.263653f, 0.000000f), new Vector3(0.840098f, -0.303932f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.840098f, -0.303932f, 0.000000f), new Vector3(0.947508f, -0.317358f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.947508f, -0.317358f, 0.000000f), new Vector3(1.043409f, -0.311604f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.043409f, -0.311604f, 0.000000f), new Vector3(1.137393f, -0.294341f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.137393f, -0.294341f, 0.000000f), new Vector3(1.137393f, -0.146653f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.137393f, -0.146653f, 0.000000f), new Vector3(1.116294f, -0.146653f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.116294f, -0.146653f, 0.000000f), new Vector3(1.052999f, -0.165833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.052999f, -0.165833f, 0.000000f), new Vector3(0.972442f, -0.175424f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.972442f, -0.175424f, 0.000000f), new Vector3(0.901475f, -0.167751f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.901475f, -0.167751f, 0.000000f), new Vector3(0.826672f, -0.125555f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.826672f, -0.125555f, 0.000000f), new Vector3(0.803655f, -0.087194f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.803655f, -0.087194f, 0.000000f), new Vector3(0.782557f, 0.018298f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, 0.018298f, 0.000000f), new Vector3(0.805573f, 0.027888f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805573f, 0.027888f, 0.000000f), new Vector3(0.889967f, 0.077757f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.889967f, 0.077757f, 0.000000f), new Vector3(0.962852f, 0.144888f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.962852f, 0.144888f, 0.000000f), new Vector3(1.024229f, 0.229281f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.024229f, 0.229281f, 0.000000f), new Vector3(1.058753f, 0.294494f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.058753f, 0.294494f, 0.000000f), new Vector3(1.089442f, 0.386560f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.089442f, 0.386560f, 0.000000f), new Vector3(1.108622f, 0.488215f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.108622f, 0.488215f, 0.000000f), new Vector3(1.114376f, 0.599461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.114376f, 0.599461f, 0.000000f), new Vector3(1.112458f, 0.668510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.112458f, 0.668510f, 0.000000f), new Vector3(1.099032f, 0.772084f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.099032f, 0.772084f, 0.000000f), new Vector3(1.076016f, 0.864150f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.076016f, 0.864150f, 0.000000f), new Vector3(1.022311f, 0.983068f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.022311f, 0.983068f, 0.000000f), new Vector3(0.962852f, 1.061707f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.962852f, 1.061707f, 0.000000f), new Vector3(0.880376f, 1.134592f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.880376f, 1.134592f, 0.000000f), new Vector3(0.788311f, 1.182543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 1.182543f, 0.000000f), new Vector3(0.759540f, 1.194051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 1.194051f, 0.000000f), new Vector3(0.663639f, 1.217067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.663639f, 1.217067f, 0.000000f), new Vector3(0.556229f, 1.224740f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, 1.224740f, 0.000000f), new Vector3(0.525541f, 1.224740f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.525541f, 1.224740f, 0.000000f), new Vector3(0.423885f, 1.211313f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 1.211313f, 0.000000f), new Vector3(0.327983f, 1.182543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 1.182543f, 0.000000f), new Vector3(0.316475f, 1.178707f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 1.178707f, 0.000000f), new Vector3(0.226328f, 1.128838f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226328f, 1.128838f, 0.000000f), new Vector3(0.151524f, 1.061707f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 1.061707f, 0.000000f), new Vector3(0.080557f, 0.960051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.080557f, 0.960051f, 0.000000f), new Vector3(0.038361f, 0.864150f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.864150f, 0.000000f), new Vector3(0.023016f, 0.806609f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023016f, 0.806609f, 0.000000f), new Vector3(0.005754f, 0.706871f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005754f, 0.706871f, 0.000000f), new Vector3(0.000000f, 0.599461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.599461f, 0.000000f), new Vector3(0.001918f, 0.530412f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.001918f, 0.530412f, 0.000000f), new Vector3(0.015344f, 0.426838f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.015344f, 0.426838f, 0.000000f), new Vector3(0.047951f, 0.311756f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047951f, 0.311756f, 0.000000f), new Vector3(0.092066f, 0.217773f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.092066f, 0.217773f, 0.000000f), new Vector3(0.151524f, 0.139134f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.139134f, 0.000000f), new Vector3(0.159197f, 0.129543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.129543f, 0.000000f), new Vector3(0.235918f, 0.064330f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235918f, 0.064330f, 0.000000f), new Vector3(0.327983f, 0.016380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 0.016380f, 0.000000f), new Vector3(0.354836f, 0.006789f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.354836f, 0.006789f, 0.000000f), new Vector3(0.450737f, -0.016227f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, -0.016227f, 0.000000f), new Vector3(0.556229f, -0.023899f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, -0.023899f, 0.000000f), new Vector3(0.592672f, -0.023899f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.592672f, -0.023899f, 0.000000f), new Vector3(0.629114f, -0.020063f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.558147f, 0.112281f, 0.000000f), new Vector3(0.661721f, 0.125707f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.661721f, 0.125707f, 0.000000f), new Vector3(0.751868f, 0.160232f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.160232f, 0.000000f), new Vector3(0.826672f, 0.221609f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.826672f, 0.221609f, 0.000000f), new Vector3(0.842016f, 0.238871f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.842016f, 0.238871f, 0.000000f), new Vector3(0.889967f, 0.311756f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.889967f, 0.311756f, 0.000000f), new Vector3(0.924491f, 0.399986f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.924491f, 0.399986f, 0.000000f), new Vector3(0.943672f, 0.505478f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943672f, 0.505478f, 0.000000f), new Vector3(0.947508f, 0.599461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.947508f, 0.599461f, 0.000000f), new Vector3(0.941754f, 0.714543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.941754f, 0.714543f, 0.000000f), new Vector3(0.918737f, 0.816199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.918737f, 0.816199f, 0.000000f), new Vector3(0.882295f, 0.902510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.882295f, 0.902510f, 0.000000f), new Vector3(0.843934f, 0.960051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843934f, 0.960051f, 0.000000f), new Vector3(0.771049f, 1.027182f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.771049f, 1.027182f, 0.000000f), new Vector3(0.686655f, 1.069379f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.686655f, 1.069379f, 0.000000f), new Vector3(0.585000f, 1.086641f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 1.086641f, 0.000000f), new Vector3(0.558147f, 1.086641f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 1.086641f, 0.000000f), new Vector3(0.452655f, 1.075133f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452655f, 1.075133f, 0.000000f), new Vector3(0.362508f, 1.040609f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 1.040609f, 0.000000f), new Vector3(0.287705f, 0.981149f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287705f, 0.981149f, 0.000000f), new Vector3(0.270442f, 0.960051f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270442f, 0.960051f, 0.000000f), new Vector3(0.222492f, 0.887166f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.222492f, 0.887166f, 0.000000f), new Vector3(0.189885f, 0.797018f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.189885f, 0.797018f, 0.000000f), new Vector3(0.170705f, 0.693445f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.693445f, 0.000000f), new Vector3(0.166869f, 0.599461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.599461f, 0.000000f), new Vector3(0.174541f, 0.484379f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.174541f, 0.484379f, 0.000000f), new Vector3(0.195639f, 0.382724f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.195639f, 0.382724f, 0.000000f), new Vector3(0.232082f, 0.296412f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232082f, 0.296412f, 0.000000f), new Vector3(0.272360f, 0.238871f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.272360f, 0.238871f, 0.000000f), new Vector3(0.345246f, 0.173658f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.345246f, 0.173658f, 0.000000f), new Vector3(0.431557f, 0.131462f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.431557f, 0.131462f, 0.000000f), new Vector3(0.531295f, 0.114199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531295f, 0.114199f, 0.000000f), new Vector3(0.558147f, 0.112281f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharQs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.638705f, -0.331819f, 0.000000f), new Vector3(0.790229f, -0.331819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, -0.331819f, 0.000000f), new Vector3(0.790229f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790229f, 0.899557f, 0.000000f), new Vector3(0.648295f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.648295f, 0.899557f, 0.000000f), new Vector3(0.638705f, 0.861196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.638705f, 0.861196f, 0.000000f), new Vector3(0.525541f, 0.909147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.525541f, 0.909147f, 0.000000f), new Vector3(0.397033f, 0.924491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.397033f, 0.924491f, 0.000000f), new Vector3(0.343328f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 0.920655f, 0.000000f), new Vector3(0.245508f, 0.891885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.245508f, 0.891885f, 0.000000f), new Vector3(0.197557f, 0.866950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.866950f, 0.000000f), new Vector3(0.120836f, 0.801737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120836f, 0.801737f, 0.000000f), new Vector3(0.078639f, 0.746114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.078639f, 0.746114f, 0.000000f), new Vector3(0.032607f, 0.652131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032607f, 0.652131f, 0.000000f), new Vector3(0.007672f, 0.554311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.554311f, 0.000000f), new Vector3(0.000000f, 0.446901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.446901f, 0.000000f), new Vector3(0.001918f, 0.379770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.001918f, 0.379770f, 0.000000f), new Vector3(0.017262f, 0.274279f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017262f, 0.274279f, 0.000000f), new Vector3(0.047951f, 0.182213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047951f, 0.182213f, 0.000000f), new Vector3(0.095902f, 0.105492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.095902f, 0.105492f, 0.000000f), new Vector3(0.164951f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.038361f, 0.000000f), new Vector3(0.249344f, -0.003836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249344f, -0.003836f, 0.000000f), new Vector3(0.351000f, -0.017262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.351000f, -0.017262f, 0.000000f), new Vector3(0.402787f, -0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402787f, -0.015344f, 0.000000f), new Vector3(0.500606f, 0.011508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.500606f, 0.011508f, 0.000000f), new Vector3(0.552393f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552393f, 0.038361f, 0.000000f), new Vector3(0.638705f, 0.101656f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.638705f, 0.101656f, 0.000000f), new Vector3(0.638705f, -0.331819f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.638705f, 0.228246f, 0.000000f), new Vector3(0.638705f, 0.738442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.638705f, 0.738442f, 0.000000f), new Vector3(0.540885f, 0.774885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.774885f, 0.000000f), new Vector3(0.425803f, 0.788311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.425803f, 0.788311f, 0.000000f), new Vector3(0.320311f, 0.771049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.320311f, 0.771049f, 0.000000f), new Vector3(0.241672f, 0.721180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 0.721180f, 0.000000f), new Vector3(0.224410f, 0.700082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.224410f, 0.700082f, 0.000000f), new Vector3(0.180295f, 0.619524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.180295f, 0.619524f, 0.000000f), new Vector3(0.159197f, 0.519787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.519787f, 0.000000f), new Vector3(0.155361f, 0.454573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, 0.454573f, 0.000000f), new Vector3(0.164951f, 0.339492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.339492f, 0.000000f), new Vector3(0.191803f, 0.247426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.247426f, 0.000000f), new Vector3(0.210983f, 0.210983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, 0.210983f, 0.000000f), new Vector3(0.280033f, 0.149606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.280033f, 0.149606f, 0.000000f), new Vector3(0.377852f, 0.126590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.377852f, 0.126590f, 0.000000f), new Vector3(0.483344f, 0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.483344f, 0.141934f, 0.000000f), new Vector3(0.515951f, 0.155361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.155361f, 0.000000f), new Vector3(0.606098f, 0.203311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.203311f, 0.000000f), new Vector3(0.638705f, 0.228246f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__R
        private static List<Line3> getCharR()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.475672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.475672f, 0.000000f), new Vector3(0.383606f, 0.475672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.383606f, 0.475672f, 0.000000f), new Vector3(0.784475f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.784475f, 0.000000f, 0.000000f), new Vector3(0.991622f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.991622f, 0.000000f, 0.000000f), new Vector3(0.540885f, 0.523623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.523623f, 0.000000f), new Vector3(0.563901f, 0.533213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.563901f, 0.533213f, 0.000000f), new Vector3(0.652131f, 0.585000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652131f, 0.585000f, 0.000000f), new Vector3(0.719262f, 0.655967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719262f, 0.655967f, 0.000000f), new Vector3(0.734606f, 0.677065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.734606f, 0.677065f, 0.000000f), new Vector3(0.772967f, 0.767213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772967f, 0.767213f, 0.000000f), new Vector3(0.784475f, 0.872704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.784475f, 0.872704f, 0.000000f), new Vector3(0.780639f, 0.941754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.780639f, 0.941754f, 0.000000f), new Vector3(0.748032f, 1.031901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.748032f, 1.031901f, 0.000000f), new Vector3(0.723098f, 1.066426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.723098f, 1.066426f, 0.000000f), new Vector3(0.646377f, 1.135475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646377f, 1.135475f, 0.000000f), new Vector3(0.613770f, 1.152737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.613770f, 1.152737f, 0.000000f), new Vector3(0.515951f, 1.185344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 1.185344f, 0.000000f), new Vector3(0.446901f, 1.194934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446901f, 1.194934f, 0.000000f), new Vector3(0.335656f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335656f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.159197f, 0.609934f, 0.000000f), new Vector3(0.320311f, 0.609934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.320311f, 0.609934f, 0.000000f), new Vector3(0.425803f, 0.617606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.425803f, 0.617606f, 0.000000f), new Vector3(0.452655f, 0.623360f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452655f, 0.623360f, 0.000000f), new Vector3(0.548557f, 0.671311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.548557f, 0.671311f, 0.000000f), new Vector3(0.602262f, 0.749950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602262f, 0.749950f, 0.000000f), new Vector3(0.617606f, 0.847770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617606f, 0.847770f, 0.000000f), new Vector3(0.600344f, 0.953262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.953262f, 0.000000f), new Vector3(0.540885f, 1.020393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 1.020393f, 0.000000f), new Vector3(0.458410f, 1.052999f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458410f, 1.052999f, 0.000000f), new Vector3(0.356754f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.356754f, 1.062590f, 0.000000f), new Vector3(0.159197f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.062590f, 0.000000f), new Vector3(0.159197f, 0.609934f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharRs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.149606f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.000000f, 0.000000f), new Vector3(0.149606f, 0.638705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.638705f, 0.000000f), new Vector3(0.187967f, 0.667475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.187967f, 0.667475f, 0.000000f), new Vector3(0.276197f, 0.719262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276197f, 0.719262f, 0.000000f), new Vector3(0.312639f, 0.734606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.734606f, 0.000000f), new Vector3(0.410459f, 0.749950f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.410459f, 0.749950f, 0.000000f), new Vector3(0.487180f, 0.746114f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.487180f, 0.746114f, 0.000000f), new Vector3(0.552393f, 0.734606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552393f, 0.734606f, 0.000000f), new Vector3(0.560065f, 0.734606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.560065f, 0.734606f, 0.000000f), new Vector3(0.560065f, 0.889967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.560065f, 0.889967f, 0.000000f), new Vector3(0.506360f, 0.897639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.506360f, 0.897639f, 0.000000f), new Vector3(0.450737f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, 0.899557f, 0.000000f), new Vector3(0.408541f, 0.897639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.408541f, 0.897639f, 0.000000f), new Vector3(0.310721f, 0.870786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.310721f, 0.870786f, 0.000000f), new Vector3(0.241672f, 0.832426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 0.832426f, 0.000000f), new Vector3(0.149606f, 0.767213f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.767213f, 0.000000f), new Vector3(0.149606f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149606f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__S
        private static List<Line3> getCharS()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.073921f, 0.000000f), new Vector3(0.115082f, 0.029806f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.115082f, 0.029806f, 0.000000f), new Vector3(0.210983f, 0.002953f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, 0.002953f, 0.000000f), new Vector3(0.335656f, -0.016227f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335656f, -0.016227f, 0.000000f), new Vector3(0.443065f, -0.021981f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, -0.021981f, 0.000000f), new Vector3(0.558147f, -0.014309f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, -0.014309f, 0.000000f), new Vector3(0.650213f, 0.006789f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.650213f, 0.006789f, 0.000000f), new Vector3(0.715426f, 0.033642f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.715426f, 0.033642f, 0.000000f), new Vector3(0.799819f, 0.087347f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.799819f, 0.087347f, 0.000000f), new Vector3(0.834344f, 0.119953f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.834344f, 0.119953f, 0.000000f), new Vector3(0.891885f, 0.204347f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.891885f, 0.204347f, 0.000000f), new Vector3(0.907229f, 0.244625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.907229f, 0.244625f, 0.000000f), new Vector3(0.924491f, 0.342445f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.924491f, 0.342445f, 0.000000f), new Vector3(0.905311f, 0.465199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.905311f, 0.465199f, 0.000000f), new Vector3(0.855442f, 0.543838f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.855442f, 0.543838f, 0.000000f), new Vector3(0.759540f, 0.612887f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.612887f, 0.000000f), new Vector3(0.654049f, 0.651248f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.654049f, 0.651248f, 0.000000f), new Vector3(0.494852f, 0.683855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.494852f, 0.683855f, 0.000000f), new Vector3(0.347164f, 0.710707f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.347164f, 0.710707f, 0.000000f), new Vector3(0.287705f, 0.727969f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287705f, 0.727969f, 0.000000f), new Vector3(0.210983f, 0.775920f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, 0.775920f, 0.000000f), new Vector3(0.197557f, 0.795100f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.197557f, 0.795100f, 0.000000f), new Vector3(0.172623f, 0.892920f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.892920f, 0.000000f), new Vector3(0.186049f, 0.956215f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.186049f, 0.956215f, 0.000000f), new Vector3(0.251262f, 1.031018f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.251262f, 1.031018f, 0.000000f), new Vector3(0.347164f, 1.073215f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.347164f, 1.073215f, 0.000000f), new Vector3(0.456492f, 1.084723f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 1.084723f, 0.000000f), new Vector3(0.585000f, 1.071297f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 1.071297f, 0.000000f), new Vector3(0.684737f, 1.046363f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 1.046363f, 0.000000f), new Vector3(0.784475f, 1.002248f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.784475f, 1.002248f, 0.000000f), new Vector3(0.865032f, 0.948543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.865032f, 0.948543f, 0.000000f), new Vector3(0.874622f, 0.948543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.874622f, 0.948543f, 0.000000f), new Vector3(0.874622f, 1.138428f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.874622f, 1.138428f, 0.000000f), new Vector3(0.792147f, 1.169117f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.792147f, 1.169117f, 0.000000f), new Vector3(0.688573f, 1.197887f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.688573f, 1.197887f, 0.000000f), new Vector3(0.563901f, 1.217067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.563901f, 1.217067f, 0.000000f), new Vector3(0.456492f, 1.220903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.456492f, 1.220903f, 0.000000f), new Vector3(0.412377f, 1.220903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.412377f, 1.220903f, 0.000000f), new Vector3(0.306885f, 1.205559f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.306885f, 1.205559f, 0.000000f), new Vector3(0.214820f, 1.174871f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.214820f, 1.174871f, 0.000000f), new Vector3(0.134262f, 1.125002f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.134262f, 1.125002f, 0.000000f), new Vector3(0.067131f, 1.059789f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.067131f, 1.059789f, 0.000000f), new Vector3(0.021098f, 0.975395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.021098f, 0.975395f, 0.000000f), new Vector3(0.007672f, 0.879494f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.879494f, 0.000000f), new Vector3(0.007672f, 0.844969f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.844969f, 0.000000f), new Vector3(0.032607f, 0.745232f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032607f, 0.745232f, 0.000000f), new Vector3(0.084393f, 0.666592f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.084393f, 0.666592f, 0.000000f), new Vector3(0.126590f, 0.628232f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126590f, 0.628232f, 0.000000f), new Vector3(0.210983f, 0.582199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, 0.582199f, 0.000000f), new Vector3(0.320311f, 0.549592f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.320311f, 0.549592f, 0.000000f), new Vector3(0.370180f, 0.540002f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370180f, 0.540002f, 0.000000f), new Vector3(0.500606f, 0.518904f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.500606f, 0.518904f, 0.000000f), new Vector3(0.600344f, 0.495888f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.495888f, 0.000000f), new Vector3(0.636786f, 0.486297f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.636786f, 0.486297f, 0.000000f), new Vector3(0.719262f, 0.438347f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719262f, 0.438347f, 0.000000f), new Vector3(0.734606f, 0.419166f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.734606f, 0.419166f, 0.000000f), new Vector3(0.757622f, 0.319429f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.757622f, 0.319429f, 0.000000f), new Vector3(0.742278f, 0.244625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.742278f, 0.244625f, 0.000000f), new Vector3(0.675147f, 0.171740f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675147f, 0.171740f, 0.000000f), new Vector3(0.648295f, 0.156396f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.648295f, 0.156396f, 0.000000f), new Vector3(0.558147f, 0.125707f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 0.125707f, 0.000000f), new Vector3(0.444983f, 0.116117f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.444983f, 0.116117f, 0.000000f), new Vector3(0.421967f, 0.116117f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.421967f, 0.116117f, 0.000000f), new Vector3(0.324147f, 0.129543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.324147f, 0.129543f, 0.000000f), new Vector3(0.220574f, 0.156396f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220574f, 0.156396f, 0.000000f), new Vector3(0.184131f, 0.171740f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.184131f, 0.171740f, 0.000000f), new Vector3(0.092066f, 0.215855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.092066f, 0.215855f, 0.000000f), new Vector3(0.011508f, 0.273396f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011508f, 0.273396f, 0.000000f), new Vector3(0.000000f, 0.273396f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.273396f, 0.000000f), new Vector3(0.000000f, 0.073921f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharSs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.053705f, 0.000000f), new Vector3(0.038361f, 0.036443f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.036443f, 0.000000f), new Vector3(0.140016f, 0.001918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.140016f, 0.001918f, 0.000000f), new Vector3(0.216738f, -0.015344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.216738f, -0.015344f, 0.000000f), new Vector3(0.322229f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.322229f, -0.021098f, 0.000000f), new Vector3(0.425803f, -0.013426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.425803f, -0.013426f, 0.000000f), new Vector3(0.521705f, 0.011508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.521705f, 0.011508f, 0.000000f), new Vector3(0.602262f, 0.057541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602262f, 0.057541f, 0.000000f), new Vector3(0.631032f, 0.082475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631032f, 0.082475f, 0.000000f), new Vector3(0.684737f, 0.163033f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 0.163033f, 0.000000f), new Vector3(0.702000f, 0.258934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.702000f, 0.258934f, 0.000000f), new Vector3(0.692409f, 0.333737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.692409f, 0.333737f, 0.000000f), new Vector3(0.646377f, 0.418131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646377f, 0.418131f, 0.000000f), new Vector3(0.585000f, 0.464164f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 0.464164f, 0.000000f), new Vector3(0.483344f, 0.500606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.483344f, 0.500606f, 0.000000f), new Vector3(0.397033f, 0.517869f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.397033f, 0.517869f, 0.000000f), new Vector3(0.303049f, 0.537049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.303049f, 0.537049f, 0.000000f), new Vector3(0.281951f, 0.542803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.281951f, 0.542803f, 0.000000f), new Vector3(0.193721f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 0.583082f, 0.000000f), new Vector3(0.159197f, 0.667475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.667475f, 0.000000f), new Vector3(0.159197f, 0.686655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.686655f, 0.000000f), new Vector3(0.214820f, 0.761459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.214820f, 0.761459f, 0.000000f), new Vector3(0.251262f, 0.778721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.251262f, 0.778721f, 0.000000f), new Vector3(0.354836f, 0.794065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.354836f, 0.794065f, 0.000000f), new Vector3(0.418131f, 0.790229f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 0.790229f, 0.000000f), new Vector3(0.517869f, 0.765295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517869f, 0.765295f, 0.000000f), new Vector3(0.573491f, 0.742278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573491f, 0.742278f, 0.000000f), new Vector3(0.659803f, 0.692409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.659803f, 0.692409f, 0.000000f), new Vector3(0.667475f, 0.692409f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.667475f, 0.692409f, 0.000000f), new Vector3(0.667475f, 0.853524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.667475f, 0.853524f, 0.000000f), new Vector3(0.632950f, 0.870786f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.632950f, 0.870786f, 0.000000f), new Vector3(0.531295f, 0.901475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531295f, 0.901475f, 0.000000f), new Vector3(0.468000f, 0.914901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.468000f, 0.914901f, 0.000000f), new Vector3(0.366344f, 0.922573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.366344f, 0.922573f, 0.000000f), new Vector3(0.308803f, 0.918737f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308803f, 0.918737f, 0.000000f), new Vector3(0.182213f, 0.889967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182213f, 0.889967f, 0.000000f), new Vector3(0.095902f, 0.840098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.095902f, 0.840098f, 0.000000f), new Vector3(0.026852f, 0.751868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.026852f, 0.751868f, 0.000000f), new Vector3(0.003836f, 0.646377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.646377f, 0.000000f), new Vector3(0.011508f, 0.585000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011508f, 0.585000f, 0.000000f), new Vector3(0.053705f, 0.498688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.053705f, 0.498688f, 0.000000f), new Vector3(0.117000f, 0.444983f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.117000f, 0.444983f, 0.000000f), new Vector3(0.220574f, 0.404705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220574f, 0.404705f, 0.000000f), new Vector3(0.316475f, 0.383606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 0.383606f, 0.000000f), new Vector3(0.433475f, 0.358672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 0.358672f, 0.000000f), new Vector3(0.517869f, 0.320311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517869f, 0.320311f, 0.000000f), new Vector3(0.548557f, 0.239754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.548557f, 0.239754f, 0.000000f), new Vector3(0.546639f, 0.214820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546639f, 0.214820f, 0.000000f), new Vector3(0.491016f, 0.138098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.491016f, 0.138098f, 0.000000f), new Vector3(0.444983f, 0.118918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.444983f, 0.118918f, 0.000000f), new Vector3(0.335656f, 0.105492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335656f, 0.105492f, 0.000000f), new Vector3(0.268524f, 0.111246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.268524f, 0.111246f, 0.000000f), new Vector3(0.166869f, 0.138098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.138098f, 0.000000f), new Vector3(0.093984f, 0.170705f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.093984f, 0.170705f, 0.000000f), new Vector3(0.007672f, 0.224410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.224410f, 0.000000f), new Vector3(0.000000f, 0.224410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.224410f, 0.000000f), new Vector3(0.000000f, 0.053705f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__T
        private static List<Line3> getCharT()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.429639f, 0.000000f, 0.000000f), new Vector3(0.588836f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.588836f, 0.000000f, 0.000000f), new Vector3(0.588836f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.588836f, 1.056835f, 0.000000f), new Vector3(1.016557f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.016557f, 1.056835f, 0.000000f), new Vector3(1.016557f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.016557f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.056835f, 0.000000f), new Vector3(0.429639f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429639f, 1.056835f, 0.000000f), new Vector3(0.429639f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharTs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.565819f, 0.007672f, 0.000000f), new Vector3(0.565819f, 0.143852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.565819f, 0.143852f, 0.000000f), new Vector3(0.558147f, 0.143852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 0.143852f, 0.000000f), new Vector3(0.498688f, 0.126590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.498688f, 0.126590f, 0.000000f), new Vector3(0.421967f, 0.115082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.421967f, 0.115082f, 0.000000f), new Vector3(0.327983f, 0.130426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 0.130426f, 0.000000f), new Vector3(0.278115f, 0.176459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.278115f, 0.176459f, 0.000000f), new Vector3(0.257016f, 0.251262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.257016f, 0.251262f, 0.000000f), new Vector3(0.253180f, 0.362508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.253180f, 0.362508f, 0.000000f), new Vector3(0.253180f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.253180f, 0.772967f, 0.000000f), new Vector3(0.565819f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.565819f, 0.772967f, 0.000000f), new Vector3(0.565819f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.565819f, 0.899557f, 0.000000f), new Vector3(0.253180f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.253180f, 0.899557f, 0.000000f), new Vector3(0.253180f, 1.158491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.253180f, 1.158491f, 0.000000f), new Vector3(0.101656f, 1.158491f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 1.158491f, 0.000000f), new Vector3(0.101656f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.772967f, 0.000000f), new Vector3(0.101656f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 0.772967f, 0.000000f), new Vector3(0.101656f, 0.293459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.101656f, 0.293459f, 0.000000f), new Vector3(0.105492f, 0.228246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.105492f, 0.228246f, 0.000000f), new Vector3(0.128508f, 0.128508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.128508f, 0.128508f, 0.000000f), new Vector3(0.172623f, 0.055623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.055623f, 0.000000f), new Vector3(0.191803f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191803f, 0.038361f, 0.000000f), new Vector3(0.274279f, -0.003836f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.274279f, -0.003836f, 0.000000f), new Vector3(0.383606f, -0.017262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.383606f, -0.017262f, 0.000000f), new Vector3(0.473754f, -0.011508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473754f, -0.011508f, 0.000000f), new Vector3(0.565819f, 0.007672f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__U
        private static List<Line3> getCharU()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 1.199805f, 0.000000f), new Vector3(0.000000f, 0.482461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.482461f, 0.000000f), new Vector3(0.000000f, 0.449855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.449855f, 0.000000f), new Vector3(0.007672f, 0.342445f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.007672f, 0.342445f, 0.000000f), new Vector3(0.028770f, 0.252298f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.028770f, 0.252298f, 0.000000f), new Vector3(0.061377f, 0.173658f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.061377f, 0.173658f, 0.000000f), new Vector3(0.122754f, 0.093101f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.122754f, 0.093101f, 0.000000f), new Vector3(0.170705f, 0.050904f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.050904f, 0.000000f), new Vector3(0.262770f, 0.004871f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.262770f, 0.004871f, 0.000000f), new Vector3(0.351000f, -0.016227f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.351000f, -0.016227f, 0.000000f), new Vector3(0.458410f, -0.023899f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458410f, -0.023899f, 0.000000f), new Vector3(0.558147f, -0.018145f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, -0.018145f, 0.000000f), new Vector3(0.652131f, 0.002953f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652131f, 0.002953f, 0.000000f), new Vector3(0.717344f, 0.031724f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717344f, 0.031724f, 0.000000f), new Vector3(0.797901f, 0.093101f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.797901f, 0.093101f, 0.000000f), new Vector3(0.849688f, 0.160232f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.849688f, 0.160232f, 0.000000f), new Vector3(0.891885f, 0.254216f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.891885f, 0.254216f, 0.000000f), new Vector3(0.914901f, 0.373134f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.914901f, 0.373134f, 0.000000f), new Vector3(0.920655f, 0.482461f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.920655f, 0.482461f, 0.000000f), new Vector3(0.920655f, 1.199805f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.920655f, 1.199805f, 0.000000f), new Vector3(0.759540f, 1.199805f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 1.199805f, 0.000000f), new Vector3(0.759540f, 0.469035f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.469035f, 0.000000f), new Vector3(0.759540f, 0.419166f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 0.419166f, 0.000000f), new Vector3(0.748032f, 0.323265f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.748032f, 0.323265f, 0.000000f), new Vector3(0.744196f, 0.309838f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.744196f, 0.309838f, 0.000000f), new Vector3(0.702000f, 0.217773f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.702000f, 0.217773f, 0.000000f), new Vector3(0.686655f, 0.198593f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.686655f, 0.198593f, 0.000000f), new Vector3(0.606098f, 0.139134f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.139134f, 0.000000f), new Vector3(0.563901f, 0.123789f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.563901f, 0.123789f, 0.000000f), new Vector3(0.458410f, 0.112281f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458410f, 0.112281f, 0.000000f), new Vector3(0.408541f, 0.114199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.408541f, 0.114199f, 0.000000f), new Vector3(0.312639f, 0.139134f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.312639f, 0.139134f, 0.000000f), new Vector3(0.289623f, 0.150642f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.289623f, 0.150642f, 0.000000f), new Vector3(0.216738f, 0.217773f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.216738f, 0.217773f, 0.000000f), new Vector3(0.210983f, 0.225445f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.210983f, 0.225445f, 0.000000f), new Vector3(0.172623f, 0.319429f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.319429f, 0.000000f), new Vector3(0.164951f, 0.363543f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.363543f, 0.000000f), new Vector3(0.159197f, 0.474789f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.474789f, 0.000000f), new Vector3(0.159197f, 1.199805f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.199805f, 0.000000f), new Vector3(0.000000f, 1.199805f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharUs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.600344f, 0.228246f, 0.000000f), new Vector3(0.560065f, 0.199475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.560065f, 0.199475f, 0.000000f), new Vector3(0.469918f, 0.149606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.469918f, 0.149606f, 0.000000f), new Vector3(0.439229f, 0.136180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.439229f, 0.136180f, 0.000000f), new Vector3(0.341410f, 0.118918f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.341410f, 0.118918f, 0.000000f), new Vector3(0.241672f, 0.134262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.241672f, 0.134262f, 0.000000f), new Vector3(0.184131f, 0.186049f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.184131f, 0.186049f, 0.000000f), new Vector3(0.155361f, 0.281951f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155361f, 0.281951f, 0.000000f), new Vector3(0.151524f, 0.387442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.387442f, 0.000000f), new Vector3(0.151524f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.316475f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.316475f, 0.000000f), new Vector3(0.005754f, 0.234000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005754f, 0.234000f, 0.000000f), new Vector3(0.030689f, 0.136180f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.030689f, 0.136180f, 0.000000f), new Vector3(0.078639f, 0.061377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.078639f, 0.061377f, 0.000000f), new Vector3(0.109328f, 0.032607f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.109328f, 0.032607f, 0.000000f), new Vector3(0.193721f, -0.011508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, -0.011508f, 0.000000f), new Vector3(0.299213f, -0.024934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299213f, -0.024934f, 0.000000f), new Vector3(0.358672f, -0.021098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.358672f, -0.021098f, 0.000000f), new Vector3(0.454573f, 0.007672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.454573f, 0.007672f, 0.000000f), new Vector3(0.512114f, 0.038361f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.512114f, 0.038361f, 0.000000f), new Vector3(0.600344f, 0.099738f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.099738f, 0.000000f), new Vector3(0.600344f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.000000f, 0.000000f), new Vector3(0.751868f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.000000f, 0.000000f), new Vector3(0.751868f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.751868f, 0.899557f, 0.000000f), new Vector3(0.600344f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.600344f, 0.899557f, 0.000000f), new Vector3(0.600344f, 0.228246f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__V
        private static List<Line3> getCharV()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.435393f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.435393f, 0.000000f, 0.000000f), new Vector3(0.648295f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.648295f, 0.000000f, 0.000000f), new Vector3(1.083688f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.083688f, 1.198770f, 0.000000f), new Vector3(0.922573f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.922573f, 1.198770f, 0.000000f), new Vector3(0.546639f, 0.143852f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546639f, 0.143852f, 0.000000f), new Vector3(0.170705f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharVs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.362508f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 0.000000f, 0.000000f), new Vector3(0.514032f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.514032f, 0.000000f, 0.000000f), new Vector3(0.878458f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878458f, 0.899557f, 0.000000f), new Vector3(0.719262f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719262f, 0.899557f, 0.000000f), new Vector3(0.443065f, 0.184131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443065f, 0.184131f, 0.000000f), new Vector3(0.164951f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__W
        private static List<Line3> getCharW()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.316475f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 0.000000f, 0.000000f), new Vector3(0.492934f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.492934f, 0.000000f, 0.000000f), new Vector3(0.738442f, 0.995458f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.738442f, 0.995458f, 0.000000f), new Vector3(0.991622f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.991622f, 0.000000f, 0.000000f), new Vector3(1.169999f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.169999f, 0.000000f, 0.000000f), new Vector3(1.482638f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.482638f, 1.198770f, 0.000000f), new Vector3(1.325360f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.325360f, 1.198770f, 0.000000f), new Vector3(1.076016f, 0.191803f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.076016f, 0.191803f, 0.000000f), new Vector3(0.824754f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.824754f, 1.198770f, 0.000000f), new Vector3(0.663639f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.663639f, 1.198770f, 0.000000f), new Vector3(0.416213f, 0.201393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.416213f, 0.201393f, 0.000000f), new Vector3(0.163033f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.163033f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharWs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.237836f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.237836f, 0.000000f, 0.000000f), new Vector3(0.375934f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.375934f, 0.000000f, 0.000000f), new Vector3(0.606098f, 0.694327f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.606098f, 0.694327f, 0.000000f), new Vector3(0.838180f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.838180f, 0.000000f, 0.000000f), new Vector3(0.978196f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.978196f, 0.000000f, 0.000000f), new Vector3(1.212196f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.212196f, 0.899557f, 0.000000f), new Vector3(1.058753f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.058753f, 0.899557f, 0.000000f), new Vector3(0.903393f, 0.203311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.903393f, 0.203311f, 0.000000f), new Vector3(0.673229f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.673229f, 0.899557f, 0.000000f), new Vector3(0.548557f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.548557f, 0.899557f, 0.000000f), new Vector3(0.324147f, 0.203311f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.324147f, 0.203311f, 0.000000f), new Vector3(0.159197f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__X
        private static List<Line3> getCharX()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.009590f, 1.198770f, 0.000000f), new Vector3(0.418131f, 0.598426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.418131f, 0.598426f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.174541f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.174541f, 0.000000f, 0.000000f), new Vector3(0.508278f, 0.492934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508278f, 0.492934f, 0.000000f), new Vector3(0.836262f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.836262f, 0.000000f, 0.000000f), new Vector3(1.020393f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.020393f, 0.000000f, 0.000000f), new Vector3(0.608016f, 0.606098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608016f, 0.606098f, 0.000000f), new Vector3(1.020393f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.020393f, 1.198770f, 0.000000f), new Vector3(0.845852f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.845852f, 1.198770f, 0.000000f), new Vector3(0.515951f, 0.711590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.515951f, 0.711590f, 0.000000f), new Vector3(0.193721f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 1.198770f, 0.000000f), new Vector3(0.009590f, 1.198770f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharXs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.176459f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176459f, 0.000000f, 0.000000f), new Vector3(0.433475f, 0.345246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 0.345246f, 0.000000f), new Vector3(0.688573f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.688573f, 0.000000f, 0.000000f), new Vector3(0.878458f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878458f, 0.000000f, 0.000000f), new Vector3(0.525541f, 0.456492f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.525541f, 0.456492f, 0.000000f), new Vector3(0.878458f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878458f, 0.899557f, 0.000000f), new Vector3(0.702000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.702000f, 0.899557f, 0.000000f), new Vector3(0.446901f, 0.560065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446901f, 0.560065f, 0.000000f), new Vector3(0.193721f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193721f, 0.899557f, 0.000000f), new Vector3(0.003836f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.003836f, 0.899557f, 0.000000f), new Vector3(0.351000f, 0.448819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.351000f, 0.448819f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Y
        private static List<Line3> getCharY()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.423885f, 0.508278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 0.508278f, 0.000000f), new Vector3(0.423885f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 0.000000f, 0.000000f), new Vector3(0.583082f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.583082f, 0.000000f, 0.000000f), new Vector3(0.583082f, 0.525541f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.583082f, 0.525541f, 0.000000f), new Vector3(1.005049f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.005049f, 1.198770f, 0.000000f), new Vector3(0.834344f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.834344f, 1.198770f, 0.000000f), new Vector3(0.504442f, 0.663639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.504442f, 0.663639f, 0.000000f), new Vector3(0.176459f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176459f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharYs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.191803f, -0.331819f, 0.000000f), new Vector3(0.352918f, -0.331819f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352918f, -0.331819f, 0.000000f), new Vector3(0.878458f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878458f, 0.899557f, 0.000000f), new Vector3(0.719262f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719262f, 0.899557f, 0.000000f), new Vector3(0.441147f, 0.232082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.232082f, 0.000000f), new Vector3(0.164951f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164951f, 0.899557f, 0.000000f), new Vector3(0.000000f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899557f, 0.000000f), new Vector3(0.358672f, 0.044115f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.358672f, 0.044115f, 0.000000f), new Vector3(0.191803f, -0.331819f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__Z
        private static List<Line3> getCharZ()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.028770f, 1.056835f, 0.000000f), new Vector3(0.736524f, 1.056835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.736524f, 1.056835f, 0.000000f), new Vector3(0.000000f, 0.147688f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.147688f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.935999f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.935999f, 0.000000f, 0.000000f), new Vector3(0.935999f, 0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.935999f, 0.141934f, 0.000000f), new Vector3(0.176459f, 0.141934f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176459f, 0.141934f, 0.000000f), new Vector3(0.918737f, 1.054917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.918737f, 1.054917f, 0.000000f), new Vector3(0.918737f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.918737f, 1.198770f, 0.000000f), new Vector3(0.028770f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.028770f, 1.198770f, 0.000000f), new Vector3(0.028770f, 1.056835f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharZs()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.725016f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.725016f, 0.000000f, 0.000000f), new Vector3(0.725016f, 0.128508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.725016f, 0.128508f, 0.000000f), new Vector3(0.184131f, 0.128508f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.184131f, 0.128508f, 0.000000f), new Vector3(0.711590f, 0.792147f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711590f, 0.792147f, 0.000000f), new Vector3(0.711590f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711590f, 0.899557f, 0.000000f), new Vector3(0.009590f, 0.899557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.009590f, 0.899557f, 0.000000f), new Vector3(0.009590f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.009590f, 0.772967f, 0.000000f), new Vector3(0.523623f, 0.772967f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.523623f, 0.772967f, 0.000000f), new Vector3(0.000000f, 0.111246f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.111246f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        // ------------------------------------------ GREEK ALPHABET ---------------------------------------------- //

        #region verdana_12_regular__ALPHA
        private static List<Line3> getCharAlpha()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.161630f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.000000f, 0.000000f), new Vector3(0.279179f, 0.335015f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.279179f, 0.335015f, 0.000000f), new Vector3(0.796395f, 0.335015f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.796395f, 0.335015f, 0.000000f), new Vector3(0.913944f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.913944f, 0.000000f, 0.000000f), new Vector3(1.084391f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.084391f, 0.000000f, 0.000000f), new Vector3(0.649459f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.649459f, 1.201940f, 0.000000f), new Vector3(0.434932f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.434932f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.326199f, 0.473135f, 0.000000f), new Vector3(0.749376f, 0.473135f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749376f, 0.473135f, 0.000000f), new Vector3(0.537787f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537787f, 1.060881f, 0.000000f), new Vector3(0.326199f, 0.473135f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharAlpha_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.640643f, 0.000000f, 0.000000f), new Vector3(0.790518f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.000000f, 0.000000f), new Vector3(0.790518f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.902189f, 0.000000f), new Vector3(0.640643f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640643f, 0.902189f, 0.000000f), new Vector3(0.640643f, 0.861047f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640643f, 0.861047f, 0.000000f), new Vector3(0.526032f, 0.908067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.526032f, 0.908067f, 0.000000f), new Vector3(0.399667f, 0.925699f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.399667f, 0.925699f, 0.000000f), new Vector3(0.361464f, 0.925699f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.361464f, 0.925699f, 0.000000f), new Vector3(0.249792f, 0.893373f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249792f, 0.893373f, 0.000000f), new Vector3(0.211588f, 0.875741f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.211588f, 0.875741f, 0.000000f), new Vector3(0.120488f, 0.802273f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120488f, 0.802273f, 0.000000f), new Vector3(0.085223f, 0.755253f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.085223f, 0.755253f, 0.000000f), new Vector3(0.032326f, 0.649459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032326f, 0.649459f, 0.000000f), new Vector3(0.011755f, 0.567174f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.567174f, 0.000000f), new Vector3(0.000000f, 0.443748f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.443748f, 0.000000f), new Vector3(0.000000f, 0.429054f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.429054f, 0.000000f), new Vector3(0.011755f, 0.299750f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.299750f, 0.000000f), new Vector3(0.044081f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.044081f, 0.191017f, 0.000000f), new Vector3(0.094039f, 0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.094039f, 0.099917f, 0.000000f), new Vector3(0.141059f, 0.049958f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.141059f, 0.049958f, 0.000000f), new Vector3(0.235098f, -0.005877f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235098f, -0.005877f, 0.000000f), new Vector3(0.349709f, -0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349709f, -0.023510f, 0.000000f), new Vector3(0.390851f, -0.020571f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, -0.020571f, 0.000000f), new Vector3(0.502522f, 0.008816f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502522f, 0.008816f, 0.000000f), new Vector3(0.540726f, 0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, 0.026449f, 0.000000f), new Vector3(0.640643f, 0.096978f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640643f, 0.096978f, 0.000000f), new Vector3(0.640643f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.640643f, 0.223343f, 0.000000f), new Vector3(0.640643f, 0.734682f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640643f, 0.734682f, 0.000000f), new Vector3(0.528971f, 0.775824f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.528971f, 0.775824f, 0.000000f), new Vector3(0.426116f, 0.787579f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.426116f, 0.787579f, 0.000000f), new Vector3(0.308566f, 0.764069f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308566f, 0.764069f, 0.000000f), new Vector3(0.226282f, 0.699417f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226282f, 0.699417f, 0.000000f), new Vector3(0.179262f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.608317f, 0.000000f), new Vector3(0.155753f, 0.487829f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.487829f, 0.000000f), new Vector3(0.155753f, 0.446687f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.446687f, 0.000000f), new Vector3(0.167507f, 0.317383f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.167507f, 0.317383f, 0.000000f), new Vector3(0.202772f, 0.217466f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.202772f, 0.217466f, 0.000000f), new Vector3(0.211588f, 0.205711f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.211588f, 0.205711f, 0.000000f), new Vector3(0.293873f, 0.138120f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293873f, 0.138120f, 0.000000f), new Vector3(0.384973f, 0.120488f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.384973f, 0.120488f, 0.000000f), new Vector3(0.517216f, 0.149875f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517216f, 0.149875f, 0.000000f), new Vector3(0.620072f, 0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.620072f, 0.208650f, 0.000000f), new Vector3(0.640643f, 0.223343f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__BETA
        private static List<Line3> getCharBeta()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.426116f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.426116f, 0.000000f, 0.000000f), new Vector3(0.526032f, 0.005877f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.526032f, 0.005877f, 0.000000f), new Vector3(0.631827f, 0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631827f, 0.023510f, 0.000000f), new Vector3(0.678846f, 0.041142f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678846f, 0.041142f, 0.000000f), new Vector3(0.778763f, 0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778763f, 0.099917f, 0.000000f), new Vector3(0.802273f, 0.117549f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.802273f, 0.117549f, 0.000000f), new Vector3(0.869863f, 0.211588f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.869863f, 0.211588f, 0.000000f), new Vector3(0.887496f, 0.252731f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.887496f, 0.252731f, 0.000000f), new Vector3(0.905128f, 0.370280f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.905128f, 0.370280f, 0.000000f), new Vector3(0.887496f, 0.479013f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.887496f, 0.479013f, 0.000000f), new Vector3(0.834599f, 0.578929f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.834599f, 0.578929f, 0.000000f), new Vector3(0.755253f, 0.643581f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755253f, 0.643581f, 0.000000f), new Vector3(0.643581f, 0.684724f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.643581f, 0.684724f, 0.000000f), new Vector3(0.643581f, 0.690601f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.643581f, 0.690601f, 0.000000f), new Vector3(0.675907f, 0.708233f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675907f, 0.708233f, 0.000000f), new Vector3(0.758192f, 0.787579f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.787579f, 0.000000f), new Vector3(0.778763f, 0.816966f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778763f, 0.816966f, 0.000000f), new Vector3(0.802273f, 0.931577f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.802273f, 0.931577f, 0.000000f), new Vector3(0.802273f, 0.952148f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.802273f, 0.952148f, 0.000000f), new Vector3(0.772885f, 1.063820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772885f, 1.063820f, 0.000000f), new Vector3(0.764069f, 1.075574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.764069f, 1.075574f, 0.000000f), new Vector3(0.675907f, 1.151981f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675907f, 1.151981f, 0.000000f), new Vector3(0.552481f, 1.190185f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552481f, 1.190185f, 0.000000f), new Vector3(0.490768f, 1.199001f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.490768f, 1.199001f, 0.000000f), new Vector3(0.355586f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.355586f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.158691f, 0.138120f, 0.000000f), new Vector3(0.370280f, 0.138120f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370280f, 0.138120f, 0.000000f), new Vector3(0.496645f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.496645f, 0.143998f, 0.000000f), new Vector3(0.543665f, 0.149875f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.543665f, 0.149875f, 0.000000f), new Vector3(0.652398f, 0.188079f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652398f, 0.188079f, 0.000000f), new Vector3(0.717050f, 0.258608f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, 0.258608f, 0.000000f), new Vector3(0.737621f, 0.364402f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.737621f, 0.364402f, 0.000000f), new Vector3(0.717050f, 0.479013f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, 0.479013f, 0.000000f), new Vector3(0.637704f, 0.552481f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.637704f, 0.552481f, 0.000000f), new Vector3(0.543665f, 0.578929f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.543665f, 0.578929f, 0.000000f), new Vector3(0.429054f, 0.584807f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429054f, 0.584807f, 0.000000f), new Vector3(0.158691f, 0.584807f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.584807f, 0.000000f), new Vector3(0.158691f, 0.138120f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.158691f, 0.719988f, 0.000000f), new Vector3(0.367341f, 0.719988f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.367341f, 0.719988f, 0.000000f), new Vector3(0.484890f, 0.725866f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.484890f, 0.725866f, 0.000000f), new Vector3(0.567174f, 0.758192f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.758192f, 0.000000f), new Vector3(0.620072f, 0.819905f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.620072f, 0.819905f, 0.000000f), new Vector3(0.637704f, 0.911006f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.637704f, 0.911006f, 0.000000f), new Vector3(0.623010f, 0.984474f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.623010f, 0.984474f, 0.000000f), new Vector3(0.575991f, 1.031494f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575991f, 1.031494f, 0.000000f), new Vector3(0.484890f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.484890f, 1.060881f, 0.000000f), new Vector3(0.349709f, 1.063820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349709f, 1.063820f, 0.000000f), new Vector3(0.158691f, 1.063820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 1.063820f, 0.000000f), new Vector3(0.158691f, 0.719988f, 0.000000f)));
            
            return b0;
        }
        private static List<Line3> getCharBeta_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.332076f, 0.000000f), new Vector3(0.149875f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, -0.332076f, 0.000000f), new Vector3(0.149875f, 0.052897f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.052897f, 0.000000f), new Vector3(0.161630f, 0.047020f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.047020f, 0.000000f), new Vector3(0.273302f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.273302f, 0.000000f, 0.000000f), new Vector3(0.405544f, -0.017632f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.405544f, -0.017632f, 0.000000f), new Vector3(0.479013f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479013f, -0.011755f, 0.000000f), new Vector3(0.587746f, 0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 0.023510f, 0.000000f), new Vector3(0.678846f, 0.091101f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678846f, 0.091101f, 0.000000f), new Vector3(0.722927f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.722927f, 0.141059f, 0.000000f), new Vector3(0.772885f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772885f, 0.240976f, 0.000000f), new Vector3(0.787579f, 0.358525f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, 0.358525f, 0.000000f), new Vector3(0.769947f, 0.470196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.769947f, 0.470196f, 0.000000f), new Vector3(0.711172f, 0.564236f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711172f, 0.564236f, 0.000000f), new Vector3(0.623010f, 0.628888f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.623010f, 0.628888f, 0.000000f), new Vector3(0.508400f, 0.667091f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, 0.667091f, 0.000000f), new Vector3(0.508400f, 0.678846f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, 0.678846f, 0.000000f), new Vector3(0.584807f, 0.711172f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.584807f, 0.711172f, 0.000000f), new Vector3(0.667091f, 0.787579f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.667091f, 0.787579f, 0.000000f), new Vector3(0.702356f, 0.849292f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.702356f, 0.849292f, 0.000000f), new Vector3(0.722927f, 0.963903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.722927f, 0.963903f, 0.000000f), new Vector3(0.722927f, 0.981535f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.722927f, 0.981535f, 0.000000f), new Vector3(0.696479f, 1.093207f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.696479f, 1.093207f, 0.000000f), new Vector3(0.623010f, 1.184307f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.623010f, 1.184307f, 0.000000f), new Vector3(0.499584f, 1.243082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.499584f, 1.243082f, 0.000000f), new Vector3(0.382035f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.382035f, 1.254837f, 0.000000f), new Vector3(0.296812f, 1.248959f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.296812f, 1.248959f, 0.000000f), new Vector3(0.188079f, 1.216633f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.188079f, 1.216633f, 0.000000f), new Vector3(0.099917f, 1.151981f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099917f, 1.151981f, 0.000000f), new Vector3(0.055836f, 1.096146f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.055836f, 1.096146f, 0.000000f), new Vector3(0.014694f, 0.993290f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.014694f, 0.993290f, 0.000000f), new Vector3(0.000000f, 0.869863f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.869863f, 0.000000f), new Vector3(0.000000f, -0.332076f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.149875f, 0.176324f, 0.000000f), new Vector3(0.261547f, 0.132243f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.261547f, 0.132243f, 0.000000f), new Vector3(0.382035f, 0.120488f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.382035f, 0.120488f, 0.000000f), new Vector3(0.502522f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.502522f, 0.141059f, 0.000000f), new Vector3(0.567174f, 0.182201f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.182201f, 0.000000f), new Vector3(0.620072f, 0.276240f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.620072f, 0.276240f, 0.000000f), new Vector3(0.631827f, 0.364402f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631827f, 0.364402f, 0.000000f), new Vector3(0.605378f, 0.479013f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.479013f, 0.000000f), new Vector3(0.528971f, 0.549542f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.528971f, 0.549542f, 0.000000f), new Vector3(0.423177f, 0.584807f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423177f, 0.584807f, 0.000000f), new Vector3(0.305628f, 0.593623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.305628f, 0.593623f, 0.000000f), new Vector3(0.276240f, 0.593623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276240f, 0.593623f, 0.000000f), new Vector3(0.276240f, 0.722927f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276240f, 0.722927f, 0.000000f), new Vector3(0.305628f, 0.722927f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.305628f, 0.722927f, 0.000000f), new Vector3(0.411422f, 0.734682f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.411422f, 0.734682f, 0.000000f), new Vector3(0.493706f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.493706f, 0.769947f, 0.000000f), new Vector3(0.549542f, 0.837537f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.549542f, 0.837537f, 0.000000f), new Vector3(0.567174f, 0.943332f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.943332f, 0.000000f), new Vector3(0.537787f, 1.055003f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537787f, 1.055003f, 0.000000f), new Vector3(0.511339f, 1.081452f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 1.081452f, 0.000000f), new Vector3(0.411422f, 1.125533f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.411422f, 1.125533f, 0.000000f), new Vector3(0.370280f, 1.128471f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370280f, 1.128471f, 0.000000f), new Vector3(0.267424f, 1.107900f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.267424f, 1.107900f, 0.000000f), new Vector3(0.199833f, 1.049126f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199833f, 1.049126f, 0.000000f), new Vector3(0.161630f, 0.966842f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.966842f, 0.000000f), new Vector3(0.149875f, 0.869863f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.869863f, 0.000000f), new Vector3(0.149875f, 0.176324f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__GAMMA
        private static List<Line3> getCharGamma()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.158691f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.000000f, 0.000000f), new Vector3(0.158691f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 1.060881f, 0.000000f), new Vector3(0.772885f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772885f, 1.060881f, 0.000000f), new Vector3(0.772885f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.772885f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharGamma_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.358525f, -0.332076f, 0.000000f), new Vector3(0.508400f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, -0.332076f, 0.000000f), new Vector3(0.508400f, 0.047020f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, 0.047020f, 0.000000f), new Vector3(0.875741f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.875741f, 0.902189f, 0.000000f), new Vector3(0.717050f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, 0.902189f, 0.000000f), new Vector3(0.440809f, 0.235098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.440809f, 0.235098f, 0.000000f), new Vector3(0.164569f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.902189f, 0.000000f), new Vector3(0.358525f, 0.047020f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.358525f, 0.047020f, 0.000000f), new Vector3(0.358525f, -0.332076f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__DELTA
        private static List<Line3> getCharDelta()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(1.116717f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.116717f, 0.000000f, 0.000000f), new Vector3(0.664153f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.664153f, 1.201940f, 0.000000f), new Vector3(0.452564f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452564f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.208650f, 0.138120f, 0.000000f), new Vector3(0.902189f, 0.138120f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.902189f, 0.138120f, 0.000000f), new Vector3(0.555420f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 1.060881f, 0.000000f), new Vector3(0.208650f, 0.138120f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharDelta_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.417299f, -0.023510f, 0.000000f), new Vector3(0.531910f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531910f, -0.011755f, 0.000000f), new Vector3(0.634765f, 0.032326f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634765f, 0.032326f, 0.000000f), new Vector3(0.719988f, 0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719988f, 0.099917f, 0.000000f), new Vector3(0.734682f, 0.117549f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.734682f, 0.117549f, 0.000000f), new Vector3(0.787579f, 0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, 0.208650f, 0.000000f), new Vector3(0.819905f, 0.317383f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.819905f, 0.317383f, 0.000000f), new Vector3(0.831660f, 0.446687f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.831660f, 0.446687f, 0.000000f), new Vector3(0.819905f, 0.573052f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.819905f, 0.573052f, 0.000000f), new Vector3(0.784640f, 0.678846f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.784640f, 0.678846f, 0.000000f), new Vector3(0.769947f, 0.705295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.769947f, 0.705295f, 0.000000f), new Vector3(0.708233f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.708233f, 0.796395f, 0.000000f), new Vector3(0.617133f, 0.890435f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617133f, 0.890435f, 0.000000f), new Vector3(0.549542f, 0.946270f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.549542f, 0.946270f, 0.000000f), new Vector3(0.452564f, 1.019739f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452564f, 1.019739f, 0.000000f), new Vector3(0.396728f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.396728f, 1.057942f, 0.000000f), new Vector3(0.308566f, 1.119655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308566f, 1.119655f, 0.000000f), new Vector3(0.308566f, 1.128471f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308566f, 1.128471f, 0.000000f), new Vector3(0.761131f, 1.128471f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.761131f, 1.128471f, 0.000000f), new Vector3(0.761131f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.761131f, 1.254837f, 0.000000f), new Vector3(0.108733f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.108733f, 1.254837f, 0.000000f), new Vector3(0.108733f, 1.110839f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.108733f, 1.110839f, 0.000000f), new Vector3(0.167507f, 1.066758f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.167507f, 1.066758f, 0.000000f), new Vector3(0.261547f, 0.999168f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.261547f, 0.999168f, 0.000000f), new Vector3(0.305628f, 0.966842f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.305628f, 0.966842f, 0.000000f), new Vector3(0.396728f, 0.893373f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.396728f, 0.893373f, 0.000000f), new Vector3(0.361464f, 0.887496f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.361464f, 0.887496f, 0.000000f), new Vector3(0.249792f, 0.846354f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249792f, 0.846354f, 0.000000f), new Vector3(0.214527f, 0.828721f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.214527f, 0.828721f, 0.000000f), new Vector3(0.120488f, 0.755253f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.120488f, 0.755253f, 0.000000f), new Vector3(0.096978f, 0.728805f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.096978f, 0.728805f, 0.000000f), new Vector3(0.035265f, 0.625949f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.035265f, 0.625949f, 0.000000f), new Vector3(0.011755f, 0.555420f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.555420f, 0.000000f), new Vector3(0.000000f, 0.431993f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.431993f, 0.000000f), new Vector3(0.000000f, 0.417299f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.417299f, 0.000000f), new Vector3(0.014694f, 0.293873f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.014694f, 0.293873f, 0.000000f), new Vector3(0.052897f, 0.188079f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.052897f, 0.188079f, 0.000000f), new Vector3(0.111672f, 0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111672f, 0.099917f, 0.000000f), new Vector3(0.191017f, 0.032326f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.032326f, 0.000000f), new Vector3(0.293873f, -0.008816f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293873f, -0.008816f, 0.000000f), new Vector3(0.417299f, -0.023510f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.420238f, 0.105794f, 0.000000f), new Vector3(0.534849f, 0.132243f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.534849f, 0.132243f, 0.000000f), new Vector3(0.608317f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, 0.191017f, 0.000000f), new Vector3(0.655336f, 0.282118f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655336f, 0.282118f, 0.000000f), new Vector3(0.675907f, 0.405544f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675907f, 0.405544f, 0.000000f), new Vector3(0.675907f, 0.446687f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675907f, 0.446687f, 0.000000f), new Vector3(0.661214f, 0.564236f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.661214f, 0.564236f, 0.000000f), new Vector3(0.631827f, 0.646520f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631827f, 0.646520f, 0.000000f), new Vector3(0.567174f, 0.740559f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.740559f, 0.000000f), new Vector3(0.499584f, 0.808150f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.499584f, 0.808150f, 0.000000f), new Vector3(0.387912f, 0.781702f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.387912f, 0.781702f, 0.000000f), new Vector3(0.285057f, 0.725866f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285057f, 0.725866f, 0.000000f), new Vector3(0.276240f, 0.719988f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276240f, 0.719988f, 0.000000f), new Vector3(0.202772f, 0.631827f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.202772f, 0.631827f, 0.000000f), new Vector3(0.191017f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.608317f, 0.000000f), new Vector3(0.161630f, 0.502522f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.502522f, 0.000000f), new Vector3(0.155753f, 0.437870f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.437870f, 0.000000f), new Vector3(0.170446f, 0.308566f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170446f, 0.308566f, 0.000000f), new Vector3(0.211588f, 0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.211588f, 0.208650f, 0.000000f), new Vector3(0.226282f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226282f, 0.191017f, 0.000000f), new Vector3(0.311505f, 0.126365f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.311505f, 0.126365f, 0.000000f), new Vector3(0.420238f, 0.105794f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__EPSILON
        private static List<Line3> getCharEpsilon()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.790518f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.000000f, 0.000000f), new Vector3(0.790518f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.143998f, 0.000000f), new Vector3(0.158691f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.143998f, 0.000000f), new Vector3(0.158691f, 0.587746f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.587746f, 0.000000f), new Vector3(0.790518f, 0.587746f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.587746f, 0.000000f), new Vector3(0.790518f, 0.731743f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.731743f, 0.000000f), new Vector3(0.158691f, 0.731743f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.731743f, 0.000000f), new Vector3(0.158691f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 1.060881f, 0.000000f), new Vector3(0.790518f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 1.060881f, 0.000000f), new Vector3(0.790518f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharEpsilon_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.379096f, -0.020571f, 0.000000f), new Vector3(0.437870f, -0.017632f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, -0.017632f, 0.000000f), new Vector3(0.552481f, -0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552481f, -0.002939f, 0.000000f), new Vector3(0.608317f, 0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, 0.011755f, 0.000000f), new Vector3(0.722927f, 0.052897f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.722927f, 0.052897f, 0.000000f), new Vector3(0.722927f, 0.220405f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.722927f, 0.220405f, 0.000000f), new Vector3(0.711172f, 0.220405f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711172f, 0.220405f, 0.000000f), new Vector3(0.672969f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.672969f, 0.191017f, 0.000000f), new Vector3(0.561297f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.561297f, 0.141059f, 0.000000f), new Vector3(0.499584f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.499584f, 0.123427f, 0.000000f), new Vector3(0.384973f, 0.111672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.384973f, 0.111672f, 0.000000f), new Vector3(0.299750f, 0.120488f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299750f, 0.120488f, 0.000000f), new Vector3(0.229221f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.229221f, 0.141059f, 0.000000f), new Vector3(0.176324f, 0.188079f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.188079f, 0.000000f), new Vector3(0.155753f, 0.261547f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.261547f, 0.000000f), new Vector3(0.176324f, 0.337954f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.337954f, 0.000000f), new Vector3(0.226282f, 0.382035f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226282f, 0.382035f, 0.000000f), new Vector3(0.302689f, 0.399667f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.302689f, 0.399667f, 0.000000f), new Vector3(0.390851f, 0.402606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, 0.402606f, 0.000000f), new Vector3(0.520155f, 0.402606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.520155f, 0.402606f, 0.000000f), new Vector3(0.520155f, 0.537787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.520155f, 0.537787f, 0.000000f), new Vector3(0.429054f, 0.537787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429054f, 0.537787f, 0.000000f), new Vector3(0.343831f, 0.537787f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343831f, 0.537787f, 0.000000f), new Vector3(0.267424f, 0.552481f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.267424f, 0.552481f, 0.000000f), new Vector3(0.205711f, 0.590684f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 0.590684f, 0.000000f), new Vector3(0.182201f, 0.667091f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182201f, 0.667091f, 0.000000f), new Vector3(0.202772f, 0.728805f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.202772f, 0.728805f, 0.000000f), new Vector3(0.252731f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.252731f, 0.769947f, 0.000000f), new Vector3(0.317383f, 0.790518f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.317383f, 0.790518f, 0.000000f), new Vector3(0.429054f, 0.793457f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429054f, 0.793457f, 0.000000f), new Vector3(0.543665f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.543665f, 0.769947f, 0.000000f), new Vector3(0.573052f, 0.761131f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573052f, 0.761131f, 0.000000f), new Vector3(0.675907f, 0.708233f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675907f, 0.708233f, 0.000000f), new Vector3(0.687662f, 0.708233f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.687662f, 0.708233f, 0.000000f), new Vector3(0.687662f, 0.875741f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.687662f, 0.875741f, 0.000000f), new Vector3(0.546603f, 0.911006f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546603f, 0.911006f, 0.000000f), new Vector3(0.523094f, 0.913944f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.523094f, 0.913944f, 0.000000f), new Vector3(0.402606f, 0.922761f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402606f, 0.922761f, 0.000000f), new Vector3(0.390851f, 0.922761f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, 0.922761f, 0.000000f), new Vector3(0.273302f, 0.911006f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.273302f, 0.911006f, 0.000000f), new Vector3(0.158691f, 0.869863f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.869863f, 0.000000f), new Vector3(0.067591f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.067591f, 0.796395f, 0.000000f), new Vector3(0.061713f, 0.787579f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.061713f, 0.787579f, 0.000000f), new Vector3(0.029387f, 0.678846f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.029387f, 0.678846f, 0.000000f), new Vector3(0.029387f, 0.655336f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.029387f, 0.655336f, 0.000000f), new Vector3(0.070529f, 0.549542f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070529f, 0.549542f, 0.000000f), new Vector3(0.088162f, 0.534849f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.088162f, 0.534849f, 0.000000f), new Vector3(0.188079f, 0.479013f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.188079f, 0.479013f, 0.000000f), new Vector3(0.188079f, 0.473135f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.188079f, 0.473135f, 0.000000f), new Vector3(0.132243f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.132243f, 0.449625f, 0.000000f), new Vector3(0.047020f, 0.382035f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047020f, 0.382035f, 0.000000f), new Vector3(0.029387f, 0.358525f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.029387f, 0.358525f, 0.000000f), new Vector3(0.000000f, 0.246853f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.246853f, 0.000000f), new Vector3(0.000000f, 0.229221f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.229221f, 0.000000f), new Vector3(0.035265f, 0.120488f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.035265f, 0.120488f, 0.000000f), new Vector3(0.126365f, 0.038203f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126365f, 0.038203f, 0.000000f), new Vector3(0.138120f, 0.032326f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.138120f, 0.032326f, 0.000000f), new Vector3(0.246853f, -0.005877f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.246853f, -0.005877f, 0.000000f), new Vector3(0.261547f, -0.008816f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.261547f, -0.008816f, 0.000000f), new Vector3(0.379096f, -0.020571f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__ZETA
        private static List<Line3> getCharZeta()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.934515f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.934515f, 0.000000f, 0.000000f), new Vector3(0.934515f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.934515f, 0.143998f, 0.000000f), new Vector3(0.173385f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.173385f, 0.143998f, 0.000000f), new Vector3(0.916883f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.916883f, 1.057942f, 0.000000f), new Vector3(0.916883f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.916883f, 1.201940f, 0.000000f), new Vector3(0.026449f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.026449f, 1.201940f, 0.000000f), new Vector3(0.026449f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.026449f, 1.060881f, 0.000000f), new Vector3(0.734682f, 1.060881f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.734682f, 1.060881f, 0.000000f), new Vector3(0.000000f, 0.149875f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.149875f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharZeta_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.431993f, -0.332076f, 0.000000f), new Vector3(0.575991f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575991f, -0.332076f, 0.000000f), new Vector3(0.599500f, -0.308566f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.599500f, -0.308566f, 0.000000f), new Vector3(0.667091f, -0.211588f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.667091f, -0.211588f, 0.000000f), new Vector3(0.678846f, -0.188079f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678846f, -0.188079f, 0.000000f), new Vector3(0.702356f, -0.076407f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.702356f, -0.076407f, 0.000000f), new Vector3(0.690601f, -0.005877f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.690601f, -0.005877f, 0.000000f), new Vector3(0.655336f, 0.061713f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655336f, 0.061713f, 0.000000f), new Vector3(0.587746f, 0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 0.114610f, 0.000000f), new Vector3(0.490768f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.490768f, 0.135181f, 0.000000f), new Vector3(0.420238f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.420238f, 0.135181f, 0.000000f), new Vector3(0.326199f, 0.138120f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326199f, 0.138120f, 0.000000f), new Vector3(0.249792f, 0.158691f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249792f, 0.158691f, 0.000000f), new Vector3(0.199833f, 0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199833f, 0.208650f, 0.000000f), new Vector3(0.170446f, 0.267424f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170446f, 0.267424f, 0.000000f), new Vector3(0.158691f, 0.329138f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.329138f, 0.000000f), new Vector3(0.155753f, 0.384973f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.384973f, 0.000000f), new Vector3(0.167507f, 0.499584f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.167507f, 0.499584f, 0.000000f), new Vector3(0.202772f, 0.614194f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.202772f, 0.614194f, 0.000000f), new Vector3(0.255669f, 0.722927f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.255669f, 0.722927f, 0.000000f), new Vector3(0.320321f, 0.819905f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.320321f, 0.819905f, 0.000000f), new Vector3(0.399667f, 0.916883f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.399667f, 0.916883f, 0.000000f), new Vector3(0.484890f, 1.002106f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.484890f, 1.002106f, 0.000000f), new Vector3(0.575991f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575991f, 1.078513f, 0.000000f), new Vector3(0.670030f, 1.146104f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 1.146104f, 0.000000f), new Vector3(0.670030f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 1.254837f, 0.000000f), new Vector3(0.070529f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070529f, 1.254837f, 0.000000f), new Vector3(0.070529f, 1.128471f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070529f, 1.128471f, 0.000000f), new Vector3(0.440809f, 1.128471f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.440809f, 1.128471f, 0.000000f), new Vector3(0.440809f, 1.119655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.440809f, 1.119655f, 0.000000f), new Vector3(0.358525f, 1.046187f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.358525f, 1.046187f, 0.000000f), new Vector3(0.276240f, 0.963903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.276240f, 0.963903f, 0.000000f), new Vector3(0.205711f, 0.884557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 0.884557f, 0.000000f), new Vector3(0.135181f, 0.790518f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.135181f, 0.790518f, 0.000000f), new Vector3(0.082284f, 0.699417f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.082284f, 0.699417f, 0.000000f), new Vector3(0.035265f, 0.590684f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.035265f, 0.590684f, 0.000000f), new Vector3(0.008816f, 0.493706f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.008816f, 0.493706f, 0.000000f), new Vector3(0.000000f, 0.373218f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.373218f, 0.000000f), new Vector3(0.002939f, 0.296812f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.002939f, 0.296812f, 0.000000f), new Vector3(0.032326f, 0.182201f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032326f, 0.182201f, 0.000000f), new Vector3(0.088162f, 0.096978f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.088162f, 0.096978f, 0.000000f), new Vector3(0.132243f, 0.058775f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.132243f, 0.058775f, 0.000000f), new Vector3(0.232160f, 0.017632f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232160f, 0.017632f, 0.000000f), new Vector3(0.361464f, 0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.361464f, 0.002939f, 0.000000f), new Vector3(0.479013f, 0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479013f, 0.002939f, 0.000000f), new Vector3(0.540726f, -0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, -0.026449f, 0.000000f), new Vector3(0.564236f, -0.102855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.564236f, -0.102855f, 0.000000f), new Vector3(0.534849f, -0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.534849f, -0.208650f, 0.000000f), new Vector3(0.520155f, -0.229221f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.520155f, -0.229221f, 0.000000f), new Vector3(0.431993f, -0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.431993f, -0.320321f, 0.000000f), new Vector3(0.431993f, -0.332076f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__ETA
        private static List<Line3> getCharEta()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.161630f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.000000f, 0.000000f), new Vector3(0.161630f, 0.587746f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.587746f, 0.000000f), new Vector3(0.758192f, 0.587746f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.587746f, 0.000000f), new Vector3(0.758192f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.000000f, 0.000000f), new Vector3(0.916883f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.916883f, 0.000000f, 0.000000f), new Vector3(0.916883f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.916883f, 1.201940f, 0.000000f), new Vector3(0.758192f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 1.201940f, 0.000000f), new Vector3(0.758192f, 0.731743f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.731743f, 0.000000f), new Vector3(0.161630f, 0.731743f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.731743f, 0.000000f), new Vector3(0.161630f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharEta_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.152814f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.000000f, 0.000000f), new Vector3(0.152814f, 0.672969f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.672969f, 0.000000f), new Vector3(0.179262f, 0.693540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.693540f, 0.000000f), new Vector3(0.282118f, 0.752314f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.282118f, 0.752314f, 0.000000f), new Vector3(0.411422f, 0.781702f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.411422f, 0.781702f, 0.000000f), new Vector3(0.511339f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 0.767008f, 0.000000f), new Vector3(0.567174f, 0.714111f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.714111f, 0.000000f), new Vector3(0.593623f, 0.631827f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.593623f, 0.631827f, 0.000000f), new Vector3(0.602439f, 0.514277f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602439f, 0.514277f, 0.000000f), new Vector3(0.602439f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602439f, -0.332076f, 0.000000f), new Vector3(0.752314f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.752314f, -0.332076f, 0.000000f), new Vector3(0.752314f, 0.584807f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.752314f, 0.584807f, 0.000000f), new Vector3(0.752314f, 0.631827f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.752314f, 0.631827f, 0.000000f), new Vector3(0.728805f, 0.749376f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728805f, 0.749376f, 0.000000f), new Vector3(0.675907f, 0.837537f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675907f, 0.837537f, 0.000000f), new Vector3(0.575991f, 0.908067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575991f, 0.908067f, 0.000000f), new Vector3(0.455503f, 0.925699f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.455503f, 0.925699f, 0.000000f), new Vector3(0.411422f, 0.925699f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.411422f, 0.925699f, 0.000000f), new Vector3(0.299750f, 0.893373f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299750f, 0.893373f, 0.000000f), new Vector3(0.252731f, 0.869863f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.252731f, 0.869863f, 0.000000f), new Vector3(0.152814f, 0.802273f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.802273f, 0.000000f), new Vector3(0.152814f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__THETA
        private static List<Line3> getCharTheta()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.555420f, -0.023510f, 0.000000f), new Vector3(0.678846f, -0.014694f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678846f, -0.014694f, 0.000000f), new Vector3(0.787579f, 0.017632f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, 0.017632f, 0.000000f), new Vector3(0.872802f, 0.061713f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.872802f, 0.061713f, 0.000000f), new Vector3(0.960964f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.960964f, 0.141059f, 0.000000f), new Vector3(1.022677f, 0.223343f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.022677f, 0.223343f, 0.000000f), new Vector3(1.072636f, 0.335015f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.072636f, 0.335015f, 0.000000f), new Vector3(1.104962f, 0.476074f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.104962f, 0.476074f, 0.000000f), new Vector3(1.113778f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.113778f, 0.599500f, 0.000000f), new Vector3(1.102023f, 0.758192f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.102023f, 0.758192f, 0.000000f), new Vector3(1.075574f, 0.866925f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.075574f, 0.866925f, 0.000000f), new Vector3(1.028555f, 0.969780f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.028555f, 0.969780f, 0.000000f), new Vector3(0.960964f, 1.063820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.960964f, 1.063820f, 0.000000f), new Vector3(0.890435f, 1.125533f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.890435f, 1.125533f, 0.000000f), new Vector3(0.787579f, 1.184307f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, 1.184307f, 0.000000f), new Vector3(0.678846f, 1.216633f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678846f, 1.216633f, 0.000000f), new Vector3(0.555420f, 1.225450f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 1.225450f, 0.000000f), new Vector3(0.437870f, 1.216633f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, 1.216633f, 0.000000f), new Vector3(0.326199f, 1.184307f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326199f, 1.184307f, 0.000000f), new Vector3(0.238037f, 1.137288f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.238037f, 1.137288f, 0.000000f), new Vector3(0.149875f, 1.063820f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 1.063820f, 0.000000f), new Vector3(0.088162f, 0.975658f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.088162f, 0.975658f, 0.000000f), new Vector3(0.038203f, 0.863986f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038203f, 0.863986f, 0.000000f), new Vector3(0.005877f, 0.725866f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.725866f, 0.000000f), new Vector3(0.000000f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.599500f, 0.000000f), new Vector3(0.000000f, 0.564236f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.564236f, 0.000000f), new Vector3(0.011755f, 0.443748f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.443748f, 0.000000f), new Vector3(0.038203f, 0.335015f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038203f, 0.335015f, 0.000000f), new Vector3(0.085223f, 0.232160f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.085223f, 0.232160f, 0.000000f), new Vector3(0.149875f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.141059f, 0.000000f), new Vector3(0.223343f, 0.076407f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.223343f, 0.076407f, 0.000000f), new Vector3(0.326199f, 0.017632f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326199f, 0.017632f, 0.000000f), new Vector3(0.434932f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.434932f, -0.011755f, 0.000000f), new Vector3(0.555420f, -0.023510f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.675907f, 0.129304f, 0.000000f), new Vector3(0.775824f, 0.179262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.775824f, 0.179262f, 0.000000f), new Vector3(0.840476f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.840476f, 0.240976f, 0.000000f), new Vector3(0.896312f, 0.326199f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.896312f, 0.326199f, 0.000000f), new Vector3(0.931577f, 0.431993f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.931577f, 0.431993f, 0.000000f), new Vector3(0.946270f, 0.558358f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.946270f, 0.558358f, 0.000000f), new Vector3(0.946270f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.946270f, 0.599500f, 0.000000f), new Vector3(0.937454f, 0.731743f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.937454f, 0.731743f, 0.000000f), new Vector3(0.908067f, 0.846354f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.908067f, 0.846354f, 0.000000f), new Vector3(0.861047f, 0.937454f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.861047f, 0.937454f, 0.000000f), new Vector3(0.843415f, 0.960964f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843415f, 0.960964f, 0.000000f), new Vector3(0.758192f, 1.034432f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 1.034432f, 0.000000f), new Vector3(0.655336f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655336f, 1.078513f, 0.000000f), new Vector3(0.555420f, 1.087329f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 1.087329f, 0.000000f), new Vector3(0.437870f, 1.072636f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, 1.072636f, 0.000000f), new Vector3(0.337954f, 1.025616f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337954f, 1.025616f, 0.000000f), new Vector3(0.270363f, 0.960964f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270363f, 0.960964f, 0.000000f), new Vector3(0.217466f, 0.875741f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.217466f, 0.875741f, 0.000000f), new Vector3(0.182201f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182201f, 0.767008f, 0.000000f), new Vector3(0.164569f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.599500f, 0.000000f), new Vector3(0.176324f, 0.467258f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.467258f, 0.000000f), new Vector3(0.202772f, 0.355586f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.202772f, 0.355586f, 0.000000f), new Vector3(0.252731f, 0.264486f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.252731f, 0.264486f, 0.000000f), new Vector3(0.270363f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270363f, 0.240976f, 0.000000f), new Vector3(0.355586f, 0.167507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.355586f, 0.167507f, 0.000000f), new Vector3(0.458442f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458442f, 0.123427f, 0.000000f), new Vector3(0.555420f, 0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 0.114610f, 0.000000f), new Vector3(0.675907f, 0.129304f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.302689f, 0.575991f, 0.000000f), new Vector3(0.808150f, 0.575991f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.808150f, 0.575991f, 0.000000f), new Vector3(0.808150f, 0.719988f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.808150f, 0.719988f, 0.000000f), new Vector3(0.302689f, 0.719988f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.302689f, 0.719988f, 0.000000f), new Vector3(0.302689f, 0.575991f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharTheta_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.511339f, -0.011755f, 0.000000f), new Vector3(0.611255f, 0.032326f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611255f, 0.032326f, 0.000000f), new Vector3(0.681785f, 0.091101f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.681785f, 0.091101f, 0.000000f), new Vector3(0.743498f, 0.196895f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.743498f, 0.196895f, 0.000000f), new Vector3(0.761131f, 0.243914f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.761131f, 0.243914f, 0.000000f), new Vector3(0.790518f, 0.364402f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.364402f, 0.000000f), new Vector3(0.805211f, 0.490768f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805211f, 0.490768f, 0.000000f), new Vector3(0.808150f, 0.620072f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.808150f, 0.620072f, 0.000000f), new Vector3(0.805211f, 0.758192f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805211f, 0.758192f, 0.000000f), new Vector3(0.790518f, 0.866925f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 0.866925f, 0.000000f), new Vector3(0.775824f, 0.934515f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.775824f, 0.934515f, 0.000000f), new Vector3(0.743498f, 1.040310f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.743498f, 1.040310f, 0.000000f), new Vector3(0.693540f, 1.122594f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.693540f, 1.122594f, 0.000000f), new Vector3(0.611255f, 1.199001f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611255f, 1.199001f, 0.000000f), new Vector3(0.526032f, 1.240143f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.526032f, 1.240143f, 0.000000f), new Vector3(0.405544f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.405544f, 1.254837f, 0.000000f), new Vector3(0.302689f, 1.243082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.302689f, 1.243082f, 0.000000f), new Vector3(0.199833f, 1.199001f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199833f, 1.199001f, 0.000000f), new Vector3(0.132243f, 1.140226f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.132243f, 1.140226f, 0.000000f), new Vector3(0.067591f, 1.040310f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.067591f, 1.040310f, 0.000000f), new Vector3(0.049958f, 0.990351f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.049958f, 0.990351f, 0.000000f), new Vector3(0.017632f, 0.858109f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017632f, 0.858109f, 0.000000f), new Vector3(0.005877f, 0.746437f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.746437f, 0.000000f), new Vector3(0.000000f, 0.620072f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.620072f, 0.000000f), new Vector3(0.000000f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.599500f, 0.000000f), new Vector3(0.005877f, 0.470196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.470196f, 0.000000f), new Vector3(0.020571f, 0.364402f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.020571f, 0.364402f, 0.000000f), new Vector3(0.032326f, 0.302689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032326f, 0.302689f, 0.000000f), new Vector3(0.067591f, 0.196895f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.067591f, 0.196895f, 0.000000f), new Vector3(0.117549f, 0.108733f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.117549f, 0.108733f, 0.000000f), new Vector3(0.199833f, 0.032326f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199833f, 0.032326f, 0.000000f), new Vector3(0.285057f, -0.008816f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285057f, -0.008816f, 0.000000f), new Vector3(0.405544f, -0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.405544f, -0.023510f, 0.000000f), new Vector3(0.511339f, -0.011755f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.405544f, 0.105794f, 0.000000f), new Vector3(0.514277f, 0.129304f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.514277f, 0.129304f, 0.000000f), new Vector3(0.590684f, 0.211588f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.590684f, 0.211588f, 0.000000f), new Vector3(0.628888f, 0.314444f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.628888f, 0.314444f, 0.000000f), new Vector3(0.655336f, 0.467258f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655336f, 0.467258f, 0.000000f), new Vector3(0.658275f, 0.564236f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.658275f, 0.564236f, 0.000000f), new Vector3(0.152814f, 0.564236f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.564236f, 0.000000f), new Vector3(0.158691f, 0.440809f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.440809f, 0.000000f), new Vector3(0.170446f, 0.355586f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170446f, 0.355586f, 0.000000f), new Vector3(0.205711f, 0.235098f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 0.235098f, 0.000000f), new Vector3(0.220405f, 0.211588f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 0.211588f, 0.000000f), new Vector3(0.296812f, 0.129304f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.296812f, 0.129304f, 0.000000f), new Vector3(0.405544f, 0.105794f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.152814f, 0.693540f, 0.000000f), new Vector3(0.658275f, 0.693540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.658275f, 0.693540f, 0.000000f), new Vector3(0.649459f, 0.816966f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.649459f, 0.816966f, 0.000000f), new Vector3(0.634765f, 0.893373f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634765f, 0.893373f, 0.000000f), new Vector3(0.596562f, 1.010922f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.596562f, 1.010922f, 0.000000f), new Vector3(0.511339f, 1.104962f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 1.104962f, 0.000000f), new Vector3(0.405544f, 1.125533f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.405544f, 1.125533f, 0.000000f), new Vector3(0.296812f, 1.104962f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.296812f, 1.104962f, 0.000000f), new Vector3(0.220405f, 1.025616f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 1.025616f, 0.000000f), new Vector3(0.182201f, 0.919822f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182201f, 0.919822f, 0.000000f), new Vector3(0.176324f, 0.893373f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.893373f, 0.000000f), new Vector3(0.158691f, 0.784640f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.784640f, 0.000000f), new Vector3(0.152814f, 0.693540f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__IOTA
        private static List<Line3> getCharIota()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.473135f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473135f, 0.000000f, 0.000000f), new Vector3(0.473135f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473135f, 0.123427f, 0.000000f), new Vector3(0.314444f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.314444f, 0.123427f, 0.000000f), new Vector3(0.314444f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.314444f, 1.078513f, 0.000000f), new Vector3(0.473135f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473135f, 1.078513f, 0.000000f), new Vector3(0.473135f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.473135f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.078513f, 0.000000f), new Vector3(0.155753f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 1.078513f, 0.000000f), new Vector3(0.155753f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.123427f, 0.000000f), new Vector3(0.000000f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.123427f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharIota_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.152814f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.000000f, 0.000000f), new Vector3(0.152814f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__KAPPA
        private static List<Line3> getCharKappa()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.161630f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.000000f, 0.000000f), new Vector3(0.161630f, 0.408483f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.408483f, 0.000000f), new Vector3(0.279179f, 0.534849f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.279179f, 0.534849f, 0.000000f), new Vector3(0.752314f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.752314f, 0.000000f, 0.000000f), new Vector3(0.960964f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.960964f, 0.000000f, 0.000000f), new Vector3(0.399667f, 0.637704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.399667f, 0.637704f, 0.000000f), new Vector3(0.934515f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.934515f, 1.201940f, 0.000000f), new Vector3(0.743498f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.743498f, 1.201940f, 0.000000f), new Vector3(0.161630f, 0.575991f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.575991f, 0.000000f), new Vector3(0.161630f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharKappa_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.149875f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.000000f, 0.000000f), new Vector3(0.149875f, 0.308566f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.308566f, 0.000000f), new Vector3(0.240976f, 0.396728f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.240976f, 0.396728f, 0.000000f), new Vector3(0.605378f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.000000f, 0.000000f), new Vector3(0.805211f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805211f, 0.000000f, 0.000000f), new Vector3(0.352647f, 0.487829f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352647f, 0.487829f, 0.000000f), new Vector3(0.555420f, 0.705295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.555420f, 0.705295f, 0.000000f), new Vector3(0.608317f, 0.752314f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, 0.752314f, 0.000000f), new Vector3(0.678846f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.678846f, 0.769947f, 0.000000f), new Vector3(0.708233f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.708233f, 0.769947f, 0.000000f), new Vector3(0.746437f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746437f, 0.767008f, 0.000000f), new Vector3(0.746437f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746437f, 0.902189f, 0.000000f), new Vector3(0.717050f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, 0.902189f, 0.000000f), new Vector3(0.670030f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 0.902189f, 0.000000f), new Vector3(0.570113f, 0.881618f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.570113f, 0.881618f, 0.000000f), new Vector3(0.552481f, 0.872802f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552481f, 0.872802f, 0.000000f), new Vector3(0.461380f, 0.793457f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.461380f, 0.793457f, 0.000000f), new Vector3(0.149875f, 0.458442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.458442f, 0.000000f), new Vector3(0.149875f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__LAMBDA
        private static List<Line3> getCharLambda()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.164569f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.000000f, 0.000000f), new Vector3(0.540726f, 1.037371f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540726f, 1.037371f, 0.000000f), new Vector3(0.916883f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.916883f, 0.000000f, 0.000000f), new Vector3(1.087329f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.087329f, 0.000000f, 0.000000f), new Vector3(0.640643f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640643f, 1.201940f, 0.000000f), new Vector3(0.446687f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446687f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharLambda_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.158691f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.000000f, 0.000000f), new Vector3(0.446687f, 0.670030f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446687f, 0.670030f, 0.000000f), new Vector3(0.711172f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711172f, 0.000000f, 0.000000f), new Vector3(0.878680f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878680f, 0.000000f, 0.000000f), new Vector3(0.370280f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370280f, 1.254837f, 0.000000f), new Vector3(0.199833f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199833f, 1.254837f, 0.000000f), new Vector3(0.373218f, 0.852231f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.373218f, 0.852231f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__MU
        private static List<Line3> getCharMu()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.149875f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.000000f, 0.000000f), new Vector3(0.149875f, 1.034432f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 1.034432f, 0.000000f), new Vector3(0.481951f, 0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481951f, 0.332076f, 0.000000f), new Vector3(0.575991f, 0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575991f, 0.332076f, 0.000000f), new Vector3(0.908067f, 1.034432f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.908067f, 1.034432f, 0.000000f), new Vector3(0.908067f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.908067f, 0.000000f, 0.000000f), new Vector3(1.069697f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.069697f, 0.000000f, 0.000000f), new Vector3(1.069697f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.069697f, 1.201940f, 0.000000f), new Vector3(0.846354f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.846354f, 1.201940f, 0.000000f), new Vector3(0.537787f, 0.531910f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537787f, 0.531910f, 0.000000f), new Vector3(0.217466f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.217466f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharMu_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.332076f, 0.000000f), new Vector3(0.149875f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, -0.332076f, 0.000000f), new Vector3(0.149875f, 0.091101f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.091101f, 0.000000f), new Vector3(0.176324f, 0.070529f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.070529f, 0.000000f), new Vector3(0.267424f, 0.005877f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.267424f, 0.005877f, 0.000000f), new Vector3(0.376157f, -0.014694f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.376157f, -0.014694f, 0.000000f), new Vector3(0.493706f, 0.008816f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.493706f, 0.008816f, 0.000000f), new Vector3(0.514277f, 0.020571f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.514277f, 0.020571f, 0.000000f), new Vector3(0.608317f, 0.096978f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, 0.096978f, 0.000000f), new Vector3(0.608317f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, 0.000000f, 0.000000f), new Vector3(0.758192f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.000000f, 0.000000f), new Vector3(0.758192f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.902189f, 0.000000f), new Vector3(0.605378f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.902189f, 0.000000f), new Vector3(0.605378f, 0.232160f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.232160f, 0.000000f), new Vector3(0.508400f, 0.155753f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, 0.155753f, 0.000000f), new Vector3(0.376157f, 0.129304f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.376157f, 0.129304f, 0.000000f), new Vector3(0.352647f, 0.129304f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352647f, 0.129304f, 0.000000f), new Vector3(0.240976f, 0.161630f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.240976f, 0.161630f, 0.000000f), new Vector3(0.149875f, 0.226282f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.226282f, 0.000000f), new Vector3(0.149875f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.902189f, 0.000000f), new Vector3(0.000000f, -0.332076f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__NU
        private static List<Line3> getCharNu()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.149875f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.000000f, 0.000000f), new Vector3(0.149875f, 1.075574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 1.075574f, 0.000000f), new Vector3(0.717050f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, 0.000000f, 0.000000f), new Vector3(0.913944f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.913944f, 0.000000f, 0.000000f), new Vector3(0.913944f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.913944f, 1.201940f, 0.000000f), new Vector3(0.764069f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.764069f, 1.201940f, 0.000000f), new Vector3(0.764069f, 0.220405f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.764069f, 0.220405f, 0.000000f), new Vector3(0.246853f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.246853f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharNu_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.361464f, 0.000000f, 0.000000f), new Vector3(0.514277f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.514277f, 0.000000f, 0.000000f), new Vector3(0.878680f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878680f, 0.902189f, 0.000000f), new Vector3(0.719988f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719988f, 0.902189f, 0.000000f), new Vector3(0.443748f, 0.185140f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443748f, 0.185140f, 0.000000f), new Vector3(0.164569f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.902189f, 0.000000f), new Vector3(0.000000f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.902189f, 0.000000f), new Vector3(0.361464f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__XI
        private static List<Line3> getCharXi()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.887496f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.887496f, 0.000000f, 0.000000f), new Vector3(0.887496f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.887496f, 0.143998f, 0.000000f), new Vector3(0.000000f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.143998f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.041142f, 0.587746f, 0.000000f), new Vector3(0.843415f, 0.587746f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843415f, 0.587746f, 0.000000f), new Vector3(0.843415f, 0.731743f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843415f, 0.731743f, 0.000000f), new Vector3(0.041142f, 0.731743f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.041142f, 0.731743f, 0.000000f), new Vector3(0.041142f, 0.587746f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.000000f, 1.057942f, 0.000000f), new Vector3(0.887496f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.887496f, 1.057942f, 0.000000f), new Vector3(0.887496f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.887496f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.057942f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharXi_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.628888f, -0.332076f, 0.000000f), new Vector3(0.649459f, -0.308566f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.649459f, -0.308566f, 0.000000f), new Vector3(0.717050f, -0.211588f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, -0.211588f, 0.000000f), new Vector3(0.728805f, -0.188079f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728805f, -0.188079f, 0.000000f), new Vector3(0.755253f, -0.076407f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755253f, -0.076407f, 0.000000f), new Vector3(0.740559f, 0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.740559f, 0.002939f, 0.000000f), new Vector3(0.702356f, 0.067591f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.702356f, 0.067591f, 0.000000f), new Vector3(0.634765f, 0.117549f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.634765f, 0.117549f, 0.000000f), new Vector3(0.537787f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537787f, 0.135181f, 0.000000f), new Vector3(0.382035f, 0.138120f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.382035f, 0.138120f, 0.000000f), new Vector3(0.267424f, 0.161630f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.267424f, 0.161630f, 0.000000f), new Vector3(0.188079f, 0.229221f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.188079f, 0.229221f, 0.000000f), new Vector3(0.179262f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.240976f, 0.000000f), new Vector3(0.155753f, 0.355586f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.355586f, 0.000000f), new Vector3(0.176324f, 0.446687f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.446687f, 0.000000f), new Vector3(0.232160f, 0.520155f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232160f, 0.520155f, 0.000000f), new Vector3(0.311505f, 0.567174f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.311505f, 0.567174f, 0.000000f), new Vector3(0.408483f, 0.581868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.408483f, 0.581868f, 0.000000f), new Vector3(0.646520f, 0.581868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646520f, 0.581868f, 0.000000f), new Vector3(0.646520f, 0.714111f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.646520f, 0.714111f, 0.000000f), new Vector3(0.508400f, 0.714111f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, 0.714111f, 0.000000f), new Vector3(0.396728f, 0.722927f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.396728f, 0.722927f, 0.000000f), new Vector3(0.311505f, 0.752314f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.311505f, 0.752314f, 0.000000f), new Vector3(0.246853f, 0.811089f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.246853f, 0.811089f, 0.000000f), new Vector3(0.220405f, 0.913944f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 0.913944f, 0.000000f), new Vector3(0.240976f, 1.010922f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.240976f, 1.010922f, 0.000000f), new Vector3(0.299750f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299750f, 1.078513f, 0.000000f), new Vector3(0.382035f, 1.116717f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.382035f, 1.116717f, 0.000000f), new Vector3(0.479013f, 1.128471f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479013f, 1.128471f, 0.000000f), new Vector3(0.687662f, 1.128471f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.687662f, 1.128471f, 0.000000f), new Vector3(0.687662f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.687662f, 1.254837f, 0.000000f), new Vector3(0.023510f, 1.254837f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023510f, 1.254837f, 0.000000f), new Vector3(0.023510f, 1.131410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023510f, 1.131410f, 0.000000f), new Vector3(0.205711f, 1.131410f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 1.131410f, 0.000000f), new Vector3(0.205711f, 1.125533f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 1.125533f, 0.000000f), new Vector3(0.179262f, 1.107900f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 1.107900f, 0.000000f), new Vector3(0.099917f, 1.025616f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099917f, 1.025616f, 0.000000f), new Vector3(0.094039f, 1.016800f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.094039f, 1.016800f, 0.000000f), new Vector3(0.064652f, 0.905128f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.064652f, 0.905128f, 0.000000f), new Vector3(0.070529f, 0.855170f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070529f, 0.855170f, 0.000000f), new Vector3(0.123427f, 0.755253f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.123427f, 0.755253f, 0.000000f), new Vector3(0.176324f, 0.711172f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.711172f, 0.000000f), new Vector3(0.285057f, 0.664153f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285057f, 0.664153f, 0.000000f), new Vector3(0.285057f, 0.655336f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285057f, 0.655336f, 0.000000f), new Vector3(0.279179f, 0.655336f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.279179f, 0.655336f, 0.000000f), new Vector3(0.170446f, 0.614194f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170446f, 0.614194f, 0.000000f), new Vector3(0.079346f, 0.546603f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.079346f, 0.546603f, 0.000000f), new Vector3(0.020571f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.020571f, 0.449625f, 0.000000f), new Vector3(0.000000f, 0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.332076f, 0.000000f), new Vector3(0.000000f, 0.314444f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.314444f, 0.000000f), new Vector3(0.026449f, 0.202772f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.026449f, 0.202772f, 0.000000f), new Vector3(0.035265f, 0.185140f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.035265f, 0.185140f, 0.000000f), new Vector3(0.108733f, 0.091101f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.108733f, 0.091101f, 0.000000f), new Vector3(0.126365f, 0.076407f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126365f, 0.076407f, 0.000000f), new Vector3(0.235098f, 0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235098f, 0.023510f, 0.000000f), new Vector3(0.293873f, 0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293873f, 0.011755f, 0.000000f), new Vector3(0.420238f, 0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.420238f, 0.002939f, 0.000000f), new Vector3(0.534849f, 0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.534849f, 0.002939f, 0.000000f), new Vector3(0.593623f, -0.029387f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.593623f, -0.029387f, 0.000000f), new Vector3(0.617133f, -0.102855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617133f, -0.102855f, 0.000000f), new Vector3(0.587746f, -0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, -0.208650f, 0.000000f), new Vector3(0.481951f, -0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481951f, -0.320321f, 0.000000f), new Vector3(0.481951f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481951f, -0.332076f, 0.000000f), new Vector3(0.628888f, -0.332076f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__OMICRON
        private static List<Line3> getCharOmicron()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.590754f, -0.021570f, 0.000000f), new Vector3(0.694327f, -0.010062f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.694327f, -0.010062f, 0.000000f), new Vector3(0.788311f, 0.018709f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 0.018709f, 0.000000f), new Vector3(0.886131f, 0.072414f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.886131f, 0.072414f, 0.000000f), new Vector3(0.962852f, 0.141463f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.962852f, 0.141463f, 0.000000f), new Vector3(1.031901f, 0.239282f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.031901f, 0.239282f, 0.000000f), new Vector3(1.074098f, 0.337102f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.074098f, 0.337102f, 0.000000f), new Vector3(1.091360f, 0.394643f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.091360f, 0.394643f, 0.000000f), new Vector3(1.108622f, 0.494381f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.108622f, 0.494381f, 0.000000f), new Vector3(1.114376f, 0.601790f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.114376f, 0.601790f, 0.000000f), new Vector3(1.112458f, 0.670840f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.112458f, 0.670840f, 0.000000f), new Vector3(1.099032f, 0.774413f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.099032f, 0.774413f, 0.000000f), new Vector3(1.076016f, 0.866479f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.076016f, 0.866479f, 0.000000f), new Vector3(1.066426f, 0.891413f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.066426f, 0.891413f, 0.000000f), new Vector3(1.020393f, 0.985397f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.020393f, 0.985397f, 0.000000f), new Vector3(0.962852f, 1.064036f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.962852f, 1.064036f, 0.000000f), new Vector3(0.878458f, 1.136921f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878458f, 1.136921f, 0.000000f), new Vector3(0.788311f, 1.184872f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.788311f, 1.184872f, 0.000000f), new Vector3(0.759540f, 1.196380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.759540f, 1.196380f, 0.000000f), new Vector3(0.663639f, 1.219397f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.663639f, 1.219397f, 0.000000f), new Vector3(0.556229f, 1.227069f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, 1.227069f, 0.000000f), new Vector3(0.525541f, 1.227069f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.525541f, 1.227069f, 0.000000f), new Vector3(0.423885f, 1.213642f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.423885f, 1.213642f, 0.000000f), new Vector3(0.327983f, 1.184872f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 1.184872f, 0.000000f), new Vector3(0.226328f, 1.131167f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226328f, 1.131167f, 0.000000f), new Vector3(0.151524f, 1.064036f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 1.064036f, 0.000000f), new Vector3(0.136180f, 1.044856f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.136180f, 1.044856f, 0.000000f), new Vector3(0.080557f, 0.962380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.080557f, 0.962380f, 0.000000f), new Vector3(0.038361f, 0.866479f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038361f, 0.866479f, 0.000000f), new Vector3(0.023016f, 0.808938f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.023016f, 0.808938f, 0.000000f), new Vector3(0.005754f, 0.709200f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005754f, 0.709200f, 0.000000f), new Vector3(0.000000f, 0.601790f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.601790f, 0.000000f), new Vector3(0.001918f, 0.532741f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.001918f, 0.532741f, 0.000000f), new Vector3(0.015344f, 0.429168f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.015344f, 0.429168f, 0.000000f), new Vector3(0.040279f, 0.337102f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.040279f, 0.337102f, 0.000000f), new Vector3(0.047951f, 0.314086f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047951f, 0.314086f, 0.000000f), new Vector3(0.092066f, 0.220102f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.092066f, 0.220102f, 0.000000f), new Vector3(0.151524f, 0.141463f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.151524f, 0.141463f, 0.000000f), new Vector3(0.159197f, 0.131873f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.131873f, 0.000000f), new Vector3(0.235918f, 0.066660f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235918f, 0.066660f, 0.000000f), new Vector3(0.327983f, 0.018709f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.327983f, 0.018709f, 0.000000f), new Vector3(0.354836f, 0.009119f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.354836f, 0.009119f, 0.000000f), new Vector3(0.450737f, -0.013898f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, -0.013898f, 0.000000f), new Vector3(0.556229f, -0.021570f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.556229f, -0.021570f, 0.000000f), new Vector3(0.590754f, -0.021570f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.661721f, 0.128037f, 0.000000f), new Vector3(0.749950f, 0.162561f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749950f, 0.162561f, 0.000000f), new Vector3(0.826672f, 0.223938f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.826672f, 0.223938f, 0.000000f), new Vector3(0.889967f, 0.314086f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.889967f, 0.314086f, 0.000000f), new Vector3(0.924491f, 0.402315f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.924491f, 0.402315f, 0.000000f), new Vector3(0.943672f, 0.507807f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.943672f, 0.507807f, 0.000000f), new Vector3(0.947508f, 0.601790f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.947508f, 0.601790f, 0.000000f), new Vector3(0.939835f, 0.716872f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.939835f, 0.716872f, 0.000000f), new Vector3(0.918737f, 0.818528f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.918737f, 0.818528f, 0.000000f), new Vector3(0.882295f, 0.904839f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.882295f, 0.904839f, 0.000000f), new Vector3(0.843934f, 0.962380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.843934f, 0.962380f, 0.000000f), new Vector3(0.771049f, 1.029511f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.771049f, 1.029511f, 0.000000f), new Vector3(0.684737f, 1.071708f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.684737f, 1.071708f, 0.000000f), new Vector3(0.585000f, 1.088970f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.585000f, 1.088970f, 0.000000f), new Vector3(0.558147f, 1.088970f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 1.088970f, 0.000000f), new Vector3(0.452655f, 1.077462f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.452655f, 1.077462f, 0.000000f), new Vector3(0.362508f, 1.042938f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.362508f, 1.042938f, 0.000000f), new Vector3(0.287705f, 0.983479f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287705f, 0.983479f, 0.000000f), new Vector3(0.270442f, 0.962380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270442f, 0.962380f, 0.000000f), new Vector3(0.222492f, 0.889495f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.222492f, 0.889495f, 0.000000f), new Vector3(0.189885f, 0.799348f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.189885f, 0.799348f, 0.000000f), new Vector3(0.170705f, 0.695774f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170705f, 0.695774f, 0.000000f), new Vector3(0.166869f, 0.601790f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.166869f, 0.601790f, 0.000000f), new Vector3(0.172623f, 0.486709f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.172623f, 0.486709f, 0.000000f), new Vector3(0.195639f, 0.385053f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.195639f, 0.385053f, 0.000000f), new Vector3(0.232082f, 0.298741f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.232082f, 0.298741f, 0.000000f), new Vector3(0.272360f, 0.241200f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.272360f, 0.241200f, 0.000000f), new Vector3(0.345246f, 0.175987f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.345246f, 0.175987f, 0.000000f), new Vector3(0.431557f, 0.133791f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.431557f, 0.133791f, 0.000000f), new Vector3(0.531295f, 0.116528f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531295f, 0.116528f, 0.000000f), new Vector3(0.558147f, 0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.558147f, 0.114610f, 0.000000f), new Vector3(0.661721f, 0.128037f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharOmicron_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.414361f, -0.026449f, 0.000000f), new Vector3(0.528971f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.528971f, -0.011755f, 0.000000f), new Vector3(0.631827f, 0.029387f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631827f, 0.029387f, 0.000000f), new Vector3(0.731743f, 0.120488f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.731743f, 0.120488f, 0.000000f), new Vector3(0.787579f, 0.211588f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, 0.211588f, 0.000000f), new Vector3(0.819905f, 0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.819905f, 0.320321f, 0.000000f), new Vector3(0.828721f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.828721f, 0.449625f, 0.000000f), new Vector3(0.828721f, 0.479013f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.828721f, 0.479013f, 0.000000f), new Vector3(0.814028f, 0.602439f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.814028f, 0.602439f, 0.000000f), new Vector3(0.775824f, 0.708233f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.775824f, 0.708233f, 0.000000f), new Vector3(0.717050f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, 0.796395f, 0.000000f), new Vector3(0.637704f, 0.866925f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.637704f, 0.866925f, 0.000000f), new Vector3(0.534849f, 0.911006f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.534849f, 0.911006f, 0.000000f), new Vector3(0.414361f, 0.925699f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414361f, 0.925699f, 0.000000f), new Vector3(0.299750f, 0.911006f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299750f, 0.911006f, 0.000000f), new Vector3(0.196895f, 0.869863f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.196895f, 0.869863f, 0.000000f), new Vector3(0.111672f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111672f, 0.796395f, 0.000000f), new Vector3(0.044081f, 0.687662f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.044081f, 0.687662f, 0.000000f), new Vector3(0.011755f, 0.575991f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.575991f, 0.000000f), new Vector3(0.000000f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.449625f, 0.000000f), new Vector3(0.000000f, 0.420238f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.420238f, 0.000000f), new Vector3(0.014694f, 0.296812f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.014694f, 0.296812f, 0.000000f), new Vector3(0.052897f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.052897f, 0.191017f, 0.000000f), new Vector3(0.111672f, 0.102855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111672f, 0.102855f, 0.000000f), new Vector3(0.193956f, 0.032326f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193956f, 0.032326f, 0.000000f), new Vector3(0.293873f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293873f, -0.011755f, 0.000000f), new Vector3(0.414361f, -0.026449f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.414361f, 0.105794f, 0.000000f), new Vector3(0.528971f, 0.129304f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.528971f, 0.129304f, 0.000000f), new Vector3(0.605378f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.191017f, 0.000000f), new Vector3(0.652398f, 0.282118f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652398f, 0.282118f, 0.000000f), new Vector3(0.672969f, 0.402606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.672969f, 0.402606f, 0.000000f), new Vector3(0.672969f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.672969f, 0.449625f, 0.000000f), new Vector3(0.661214f, 0.581868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.661214f, 0.581868f, 0.000000f), new Vector3(0.623010f, 0.681785f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.623010f, 0.681785f, 0.000000f), new Vector3(0.605378f, 0.711172f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.711172f, 0.000000f), new Vector3(0.517216f, 0.775824f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517216f, 0.775824f, 0.000000f), new Vector3(0.414361f, 0.793457f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414361f, 0.793457f, 0.000000f), new Vector3(0.299750f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.299750f, 0.769947f, 0.000000f), new Vector3(0.223343f, 0.711172f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.223343f, 0.711172f, 0.000000f), new Vector3(0.179262f, 0.620072f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.620072f, 0.000000f), new Vector3(0.158691f, 0.499584f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.499584f, 0.000000f), new Vector3(0.155753f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.449625f, 0.000000f), new Vector3(0.167507f, 0.317383f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.167507f, 0.317383f, 0.000000f), new Vector3(0.205711f, 0.217466f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 0.217466f, 0.000000f), new Vector3(0.223343f, 0.193956f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.223343f, 0.193956f, 0.000000f), new Vector3(0.311505f, 0.126365f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.311505f, 0.126365f, 0.000000f), new Vector3(0.414361f, 0.105794f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__PI
        private static List<Line3> getCharPi()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.161630f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.000000f, 0.000000f), new Vector3(0.161630f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 1.057942f, 0.000000f), new Vector3(0.758192f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 1.057942f, 0.000000f), new Vector3(0.758192f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.000000f, 0.000000f), new Vector3(0.919822f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.919822f, 0.000000f, 0.000000f), new Vector3(0.919822f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.919822f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharPi_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.152814f, 0.000000f, 0.000000f), new Vector3(0.152814f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.769947f, 0.000000f), new Vector3(0.602439f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602439f, 0.769947f, 0.000000f), new Vector3(0.602439f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602439f, 0.000000f, 0.000000f), new Vector3(0.752314f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.752314f, 0.000000f, 0.000000f), new Vector3(0.752314f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.752314f, 0.899251f, 0.000000f), new Vector3(0.000000f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899251f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.152814f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__RHO
        private static List<Line3> getCharRho()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.000000f, 0.000000f), new Vector3(0.159197f, 0.446901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 0.446901f, 0.000000f), new Vector3(0.331819f, 0.446901f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.331819f, 0.446901f, 0.000000f), new Vector3(0.441147f, 0.454573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 0.454573f, 0.000000f), new Vector3(0.531295f, 0.477590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.531295f, 0.477590f, 0.000000f), new Vector3(0.598426f, 0.508278f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.598426f, 0.508278f, 0.000000f), new Vector3(0.677065f, 0.569655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.677065f, 0.569655f, 0.000000f), new Vector3(0.703918f, 0.600344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.703918f, 0.600344f, 0.000000f), new Vector3(0.755704f, 0.688573f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755704f, 0.688573f, 0.000000f), new Vector3(0.771049f, 0.734606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.771049f, 0.734606f, 0.000000f), new Vector3(0.782557f, 0.836262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.782557f, 0.836262f, 0.000000f), new Vector3(0.776803f, 0.920655f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.776803f, 0.920655f, 0.000000f), new Vector3(0.746114f, 1.008885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746114f, 1.008885f, 0.000000f), new Vector3(0.715426f, 1.054917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.715426f, 1.054917f, 0.000000f), new Vector3(0.640623f, 1.123966f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640623f, 1.123966f, 0.000000f), new Vector3(0.604180f, 1.145065f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.604180f, 1.145065f, 0.000000f), new Vector3(0.506360f, 1.181507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.506360f, 1.181507f, 0.000000f), new Vector3(0.433475f, 1.193016f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.433475f, 1.193016f, 0.000000f), new Vector3(0.324147f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.324147f, 1.198770f, 0.000000f), new Vector3(0.000000f, 1.198770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.198770f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.159197f, 0.583082f, 0.000000f), new Vector3(0.293459f, 0.583082f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293459f, 0.583082f, 0.000000f), new Vector3(0.402787f, 0.590754f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402787f, 0.590754f, 0.000000f), new Vector3(0.450737f, 0.600344f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.450737f, 0.600344f, 0.000000f), new Vector3(0.540885f, 0.646377f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.540885f, 0.646377f, 0.000000f), new Vector3(0.602262f, 0.736524f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.602262f, 0.736524f, 0.000000f), new Vector3(0.617606f, 0.832426f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617606f, 0.832426f, 0.000000f), new Vector3(0.598426f, 0.932163f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.598426f, 0.932163f, 0.000000f), new Vector3(0.596508f, 0.939835f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.596508f, 0.939835f, 0.000000f), new Vector3(0.529377f, 1.014639f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.529377f, 1.014639f, 0.000000f), new Vector3(0.441147f, 1.051081f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.441147f, 1.051081f, 0.000000f), new Vector3(0.343328f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343328f, 1.062590f, 0.000000f), new Vector3(0.316475f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.316475f, 1.062590f, 0.000000f), new Vector3(0.159197f, 1.062590f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.159197f, 1.062590f, 0.000000f), new Vector3(0.159197f, 0.583082f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharRho_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.332076f, 0.000000f), new Vector3(0.149875f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, -0.332076f, 0.000000f), new Vector3(0.149875f, 0.047020f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.047020f, 0.000000f), new Vector3(0.164569f, 0.038203f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.038203f, 0.000000f), new Vector3(0.273302f, -0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.273302f, -0.002939f, 0.000000f), new Vector3(0.390851f, -0.017632f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, -0.017632f, 0.000000f), new Vector3(0.496645f, -0.005877f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.496645f, -0.005877f, 0.000000f), new Vector3(0.596562f, 0.041142f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.596562f, 0.041142f, 0.000000f), new Vector3(0.681785f, 0.120488f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.681785f, 0.120488f, 0.000000f), new Vector3(0.696479f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.696479f, 0.135181f, 0.000000f), new Vector3(0.749376f, 0.232160f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.749376f, 0.232160f, 0.000000f), new Vector3(0.781702f, 0.340892f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.781702f, 0.340892f, 0.000000f), new Vector3(0.793457f, 0.464319f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.793457f, 0.464319f, 0.000000f), new Vector3(0.781702f, 0.614194f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.781702f, 0.614194f, 0.000000f), new Vector3(0.746437f, 0.719988f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.746437f, 0.719988f, 0.000000f), new Vector3(0.690601f, 0.805211f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.690601f, 0.805211f, 0.000000f), new Vector3(0.620072f, 0.866925f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.620072f, 0.866925f, 0.000000f), new Vector3(0.517216f, 0.911006f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517216f, 0.911006f, 0.000000f), new Vector3(0.390851f, 0.925699f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, 0.925699f, 0.000000f), new Vector3(0.349709f, 0.922761f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.349709f, 0.922761f, 0.000000f), new Vector3(0.235098f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235098f, 0.899251f, 0.000000f), new Vector3(0.205711f, 0.887496f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 0.887496f, 0.000000f), new Vector3(0.108733f, 0.822844f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.108733f, 0.822844f, 0.000000f), new Vector3(0.088162f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.088162f, 0.796395f, 0.000000f), new Vector3(0.029387f, 0.693540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.029387f, 0.693540f, 0.000000f), new Vector3(0.011755f, 0.640643f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.640643f, 0.000000f), new Vector3(0.000000f, 0.520155f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.520155f, 0.000000f), new Vector3(0.000000f, -0.332076f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.149875f, 0.167507f, 0.000000f), new Vector3(0.258608f, 0.129304f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.258608f, 0.129304f, 0.000000f), new Vector3(0.364402f, 0.117549f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.364402f, 0.117549f, 0.000000f), new Vector3(0.481951f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481951f, 0.141059f, 0.000000f), new Vector3(0.567174f, 0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.208650f, 0.000000f), new Vector3(0.617133f, 0.299750f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.617133f, 0.299750f, 0.000000f), new Vector3(0.637704f, 0.420238f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.637704f, 0.420238f, 0.000000f), new Vector3(0.637704f, 0.455503f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.637704f, 0.455503f, 0.000000f), new Vector3(0.625949f, 0.587746f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625949f, 0.587746f, 0.000000f), new Vector3(0.590684f, 0.687662f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.590684f, 0.687662f, 0.000000f), new Vector3(0.573052f, 0.714111f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573052f, 0.714111f, 0.000000f), new Vector3(0.484890f, 0.778763f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.484890f, 0.778763f, 0.000000f), new Vector3(0.390851f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, 0.796395f, 0.000000f), new Vector3(0.273302f, 0.772885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.273302f, 0.772885f, 0.000000f), new Vector3(0.208650f, 0.717050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.208650f, 0.717050f, 0.000000f), new Vector3(0.164569f, 0.623010f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.623010f, 0.000000f), new Vector3(0.149875f, 0.496645f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.496645f, 0.000000f), new Vector3(0.149875f, 0.167507f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__SIGMA
        private static List<Line3> getCharSigma()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.934515f, 0.000000f, 0.000000f), new Vector3(0.934515f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.934515f, 0.143998f, 0.000000f), new Vector3(0.193956f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193956f, 0.143998f, 0.000000f), new Vector3(0.699417f, 0.631827f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.699417f, 0.631827f, 0.000000f), new Vector3(0.699417f, 0.652398f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.699417f, 0.652398f, 0.000000f), new Vector3(0.229221f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.229221f, 1.057942f, 0.000000f), new Vector3(0.899251f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.899251f, 1.057942f, 0.000000f), new Vector3(0.899251f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.899251f, 1.201940f, 0.000000f), new Vector3(0.017632f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017632f, 1.201940f, 0.000000f), new Vector3(0.017632f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017632f, 1.057942f, 0.000000f), new Vector3(0.499584f, 0.631827f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.499584f, 0.631827f, 0.000000f), new Vector3(0.000000f, 0.149875f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.149875f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.934515f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharSigma_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.414361f, -0.026449f, 0.000000f), new Vector3(0.526032f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.526032f, -0.011755f, 0.000000f), new Vector3(0.631827f, 0.029387f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.631827f, 0.029387f, 0.000000f), new Vector3(0.717050f, 0.099917f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.717050f, 0.099917f, 0.000000f), new Vector3(0.734682f, 0.117549f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.734682f, 0.117549f, 0.000000f), new Vector3(0.787579f, 0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, 0.208650f, 0.000000f), new Vector3(0.819905f, 0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.819905f, 0.320321f, 0.000000f), new Vector3(0.828721f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.828721f, 0.449625f, 0.000000f), new Vector3(0.825783f, 0.520155f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.825783f, 0.520155f, 0.000000f), new Vector3(0.802273f, 0.628888f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.802273f, 0.628888f, 0.000000f), new Vector3(0.787579f, 0.667091f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, 0.667091f, 0.000000f), new Vector3(0.725866f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.725866f, 0.767008f, 0.000000f), new Vector3(0.958025f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.958025f, 0.767008f, 0.000000f), new Vector3(0.958025f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.958025f, 0.899251f, 0.000000f), new Vector3(0.570113f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.570113f, 0.899251f, 0.000000f), new Vector3(0.505461f, 0.916883f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.505461f, 0.916883f, 0.000000f), new Vector3(0.417299f, 0.925699f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.417299f, 0.925699f, 0.000000f), new Vector3(0.302689f, 0.911006f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.302689f, 0.911006f, 0.000000f), new Vector3(0.199833f, 0.869863f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199833f, 0.869863f, 0.000000f), new Vector3(0.111672f, 0.799334f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111672f, 0.799334f, 0.000000f), new Vector3(0.044081f, 0.687662f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.044081f, 0.687662f, 0.000000f), new Vector3(0.011755f, 0.575991f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.575991f, 0.000000f), new Vector3(0.000000f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.449625f, 0.000000f), new Vector3(0.000000f, 0.420238f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.420238f, 0.000000f), new Vector3(0.014694f, 0.296812f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.014694f, 0.296812f, 0.000000f), new Vector3(0.052897f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.052897f, 0.191017f, 0.000000f), new Vector3(0.111672f, 0.102855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.111672f, 0.102855f, 0.000000f), new Vector3(0.191017f, 0.032326f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.032326f, 0.000000f), new Vector3(0.293873f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293873f, -0.011755f, 0.000000f), new Vector3(0.414361f, -0.026449f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.414361f, 0.105794f, 0.000000f), new Vector3(0.528971f, 0.129304f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.528971f, 0.129304f, 0.000000f), new Vector3(0.605378f, 0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.191017f, 0.000000f), new Vector3(0.652398f, 0.282118f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652398f, 0.282118f, 0.000000f), new Vector3(0.672969f, 0.402606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.672969f, 0.402606f, 0.000000f), new Vector3(0.672969f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.672969f, 0.449625f, 0.000000f), new Vector3(0.661214f, 0.581868f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.661214f, 0.581868f, 0.000000f), new Vector3(0.625949f, 0.681785f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625949f, 0.681785f, 0.000000f), new Vector3(0.608317f, 0.708233f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, 0.708233f, 0.000000f), new Vector3(0.520155f, 0.775824f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.520155f, 0.775824f, 0.000000f), new Vector3(0.417299f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.417299f, 0.796395f, 0.000000f), new Vector3(0.302689f, 0.772885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.302689f, 0.772885f, 0.000000f), new Vector3(0.220405f, 0.705295f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 0.705295f, 0.000000f), new Vector3(0.176324f, 0.611255f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.611255f, 0.000000f), new Vector3(0.155753f, 0.490768f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.490768f, 0.000000f), new Vector3(0.155753f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.449625f, 0.000000f), new Vector3(0.167507f, 0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.167507f, 0.320321f, 0.000000f), new Vector3(0.205711f, 0.217466f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 0.217466f, 0.000000f), new Vector3(0.223343f, 0.193956f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.223343f, 0.193956f, 0.000000f), new Vector3(0.308566f, 0.126365f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308566f, 0.126365f, 0.000000f), new Vector3(0.414361f, 0.105794f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharSigma_smallFinal()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.467258f, -0.332076f, 0.000000f), new Vector3(0.608317f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, -0.332076f, 0.000000f), new Vector3(0.625949f, -0.314444f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.625949f, -0.314444f, 0.000000f), new Vector3(0.696479f, -0.217466f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.696479f, -0.217466f, 0.000000f), new Vector3(0.708233f, -0.191017f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.708233f, -0.191017f, 0.000000f), new Vector3(0.734682f, -0.076407f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.734682f, -0.076407f, 0.000000f), new Vector3(0.728805f, -0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.728805f, -0.026449f, 0.000000f), new Vector3(0.675907f, 0.073468f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.675907f, 0.073468f, 0.000000f), new Vector3(0.637704f, 0.105794f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.637704f, 0.105794f, 0.000000f), new Vector3(0.526032f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.526032f, 0.135181f, 0.000000f), new Vector3(0.446687f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.446687f, 0.135181f, 0.000000f), new Vector3(0.335015f, 0.149875f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.335015f, 0.149875f, 0.000000f), new Vector3(0.240976f, 0.199833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.240976f, 0.199833f, 0.000000f), new Vector3(0.179262f, 0.293873f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.293873f, 0.000000f), new Vector3(0.170446f, 0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170446f, 0.320321f, 0.000000f), new Vector3(0.155753f, 0.440809f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.440809f, 0.000000f), new Vector3(0.155753f, 0.481951f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.481951f, 0.000000f), new Vector3(0.179262f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.599500f, 0.000000f), new Vector3(0.238037f, 0.702356f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.238037f, 0.702356f, 0.000000f), new Vector3(0.326199f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326199f, 0.767008f, 0.000000f), new Vector3(0.443748f, 0.790518f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443748f, 0.790518f, 0.000000f), new Vector3(0.561297f, 0.769947f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.561297f, 0.769947f, 0.000000f), new Vector3(0.590684f, 0.758192f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.590684f, 0.758192f, 0.000000f), new Vector3(0.699417f, 0.699417f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.699417f, 0.699417f, 0.000000f), new Vector3(0.708233f, 0.699417f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.708233f, 0.699417f, 0.000000f), new Vector3(0.708233f, 0.869863f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.708233f, 0.869863f, 0.000000f), new Vector3(0.590684f, 0.905128f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.590684f, 0.905128f, 0.000000f), new Vector3(0.575991f, 0.908067f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.575991f, 0.908067f, 0.000000f), new Vector3(0.458442f, 0.919822f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458442f, 0.919822f, 0.000000f), new Vector3(0.431993f, 0.919822f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.431993f, 0.919822f, 0.000000f), new Vector3(0.314444f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.314444f, 0.902189f, 0.000000f), new Vector3(0.211588f, 0.858109f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.211588f, 0.858109f, 0.000000f), new Vector3(0.123427f, 0.790518f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.123427f, 0.790518f, 0.000000f), new Vector3(0.102855f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.102855f, 0.767008f, 0.000000f), new Vector3(0.044081f, 0.675907f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.044081f, 0.675907f, 0.000000f), new Vector3(0.011755f, 0.567174f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.567174f, 0.000000f), new Vector3(0.000000f, 0.440809f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.440809f, 0.000000f), new Vector3(0.005877f, 0.346770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.346770f, 0.000000f), new Vector3(0.032326f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032326f, 0.240976f, 0.000000f), new Vector3(0.052897f, 0.193956f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.052897f, 0.193956f, 0.000000f), new Vector3(0.123427f, 0.102855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.123427f, 0.102855f, 0.000000f), new Vector3(0.158691f, 0.076407f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.076407f, 0.000000f), new Vector3(0.264486f, 0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.264486f, 0.026449f, 0.000000f), new Vector3(0.317383f, 0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.317383f, 0.011755f, 0.000000f), new Vector3(0.437870f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.437870f, 0.000000f, 0.000000f), new Vector3(0.511339f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 0.000000f, 0.000000f), new Vector3(0.573052f, -0.029387f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573052f, -0.029387f, 0.000000f), new Vector3(0.596562f, -0.105794f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.596562f, -0.105794f, 0.000000f), new Vector3(0.567174f, -0.211588f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, -0.211588f, 0.000000f), new Vector3(0.552481f, -0.232160f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.552481f, -0.232160f, 0.000000f), new Vector3(0.467258f, -0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.467258f, -0.320321f, 0.000000f), new Vector3(0.467258f, -0.332076f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__TAU
        private static List<Line3> getCharTau()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.429054f, 0.000000f, 0.000000f), new Vector3(0.587746f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 0.000000f, 0.000000f), new Vector3(0.587746f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 1.057942f, 0.000000f), new Vector3(1.016800f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.016800f, 1.057942f, 0.000000f), new Vector3(1.016800f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.016800f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.057942f, 0.000000f), new Vector3(0.429054f, 1.057942f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.429054f, 1.057942f, 0.000000f), new Vector3(0.429054f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharTau_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.326199f, 0.000000f, 0.000000f), new Vector3(0.479013f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479013f, 0.000000f, 0.000000f), new Vector3(0.479013f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.479013f, 0.767008f, 0.000000f), new Vector3(0.802273f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.802273f, 0.767008f, 0.000000f), new Vector3(0.802273f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.802273f, 0.899251f, 0.000000f), new Vector3(0.000000f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899251f, 0.000000f), new Vector3(0.000000f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.767008f, 0.000000f), new Vector3(0.326199f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.326199f, 0.767008f, 0.000000f), new Vector3(0.326199f, 0.000000f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__UPSILON
        private static List<Line3> getCharUpsilon()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.426116f, 0.511339f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.426116f, 0.511339f, 0.000000f), new Vector3(0.426116f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.426116f, 0.000000f, 0.000000f), new Vector3(0.584807f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.584807f, 0.000000f, 0.000000f), new Vector3(0.584807f, 0.526032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.584807f, 0.526032f, 0.000000f), new Vector3(1.005045f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.005045f, 1.201940f, 0.000000f), new Vector3(0.837537f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.837537f, 1.201940f, 0.000000f), new Vector3(0.505461f, 0.664153f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.505461f, 0.664153f, 0.000000f), new Vector3(0.176324f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharUpsilon_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.899251f, 0.000000f), new Vector3(0.000000f, 0.329138f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.329138f, 0.000000f), new Vector3(0.005877f, 0.255669f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.255669f, 0.000000f), new Vector3(0.038203f, 0.143998f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038203f, 0.143998f, 0.000000f), new Vector3(0.099917f, 0.061713f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099917f, 0.061713f, 0.000000f), new Vector3(0.146936f, 0.029387f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.146936f, 0.029387f, 0.000000f), new Vector3(0.252731f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.252731f, -0.011755f, 0.000000f), new Vector3(0.379096f, -0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.379096f, -0.023510f, 0.000000f), new Vector3(0.458442f, -0.020571f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458442f, -0.020571f, 0.000000f), new Vector3(0.570113f, 0.008816f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.570113f, 0.008816f, 0.000000f), new Vector3(0.658275f, 0.064652f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.658275f, 0.064652f, 0.000000f), new Vector3(0.696479f, 0.102855f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.696479f, 0.102855f, 0.000000f), new Vector3(0.740559f, 0.202772f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.740559f, 0.202772f, 0.000000f), new Vector3(0.758192f, 0.329138f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.329138f, 0.000000f), new Vector3(0.758192f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.758192f, 0.899251f, 0.000000f), new Vector3(0.605378f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.899251f, 0.000000f), new Vector3(0.605378f, 0.382035f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.605378f, 0.382035f, 0.000000f), new Vector3(0.599500f, 0.261547f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.599500f, 0.261547f, 0.000000f), new Vector3(0.567174f, 0.176324f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.567174f, 0.176324f, 0.000000f), new Vector3(0.496645f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.496645f, 0.123427f, 0.000000f), new Vector3(0.379096f, 0.105794f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.379096f, 0.105794f, 0.000000f), new Vector3(0.264486f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.264486f, 0.123427f, 0.000000f), new Vector3(0.193956f, 0.173385f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193956f, 0.173385f, 0.000000f), new Vector3(0.158691f, 0.264486f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.264486f, 0.000000f), new Vector3(0.152814f, 0.382035f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.382035f, 0.000000f), new Vector3(0.152814f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.899251f, 0.000000f), new Vector3(0.000000f, 0.899251f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__PHI
        private static List<Line3> getCharPhi()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.511339f, -0.011755f, 0.000000f), new Vector3(0.670030f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, -0.011755f, 0.000000f), new Vector3(0.670030f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 0.123427f, 0.000000f), new Vector3(0.767008f, 0.132243f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767008f, 0.132243f, 0.000000f), new Vector3(0.878680f, 0.161630f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878680f, 0.161630f, 0.000000f), new Vector3(0.952148f, 0.196895f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.952148f, 0.196895f, 0.000000f), new Vector3(1.046187f, 0.264486f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.046187f, 0.264486f, 0.000000f), new Vector3(1.084391f, 0.305628f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.084391f, 0.305628f, 0.000000f), new Vector3(1.146104f, 0.408483f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.146104f, 0.408483f, 0.000000f), new Vector3(1.169614f, 0.487829f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.169614f, 0.487829f, 0.000000f), new Vector3(1.181369f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.181369f, 0.608317f, 0.000000f), new Vector3(1.175491f, 0.693540f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.175491f, 0.693540f, 0.000000f), new Vector3(1.146104f, 0.802273f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.146104f, 0.802273f, 0.000000f), new Vector3(1.119655f, 0.852231f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.119655f, 0.852231f, 0.000000f), new Vector3(1.049126f, 0.946270f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.049126f, 0.946270f, 0.000000f), new Vector3(0.996229f, 0.990351f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.996229f, 0.990351f, 0.000000f), new Vector3(0.887496f, 1.046187f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.887496f, 1.046187f, 0.000000f), new Vector3(0.790518f, 1.075574f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.790518f, 1.075574f, 0.000000f), new Vector3(0.670030f, 1.090268f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 1.090268f, 0.000000f), new Vector3(0.670030f, 1.213695f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 1.213695f, 0.000000f), new Vector3(0.511339f, 1.213695f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 1.213695f, 0.000000f), new Vector3(0.511339f, 1.090268f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 1.090268f, 0.000000f), new Vector3(0.402606f, 1.078513f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.402606f, 1.078513f, 0.000000f), new Vector3(0.293873f, 1.049126f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293873f, 1.049126f, 0.000000f), new Vector3(0.220405f, 1.013861f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.220405f, 1.013861f, 0.000000f), new Vector3(0.129304f, 0.946270f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.129304f, 0.946270f, 0.000000f), new Vector3(0.094039f, 0.902189f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.094039f, 0.902189f, 0.000000f), new Vector3(0.035265f, 0.802273f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.035265f, 0.802273f, 0.000000f), new Vector3(0.011755f, 0.731743f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.731743f, 0.000000f), new Vector3(0.000000f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.608317f, 0.000000f), new Vector3(0.005877f, 0.520155f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.520155f, 0.000000f), new Vector3(0.035265f, 0.408483f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.035265f, 0.408483f, 0.000000f), new Vector3(0.061713f, 0.352647f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.061713f, 0.352647f, 0.000000f), new Vector3(0.135181f, 0.264486f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.135181f, 0.264486f, 0.000000f), new Vector3(0.196895f, 0.214527f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.196895f, 0.214527f, 0.000000f), new Vector3(0.302689f, 0.161630f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.302689f, 0.161630f, 0.000000f), new Vector3(0.393790f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.393790f, 0.135181f, 0.000000f), new Vector3(0.511339f, 0.123427f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 0.123427f, 0.000000f), new Vector3(0.511339f, -0.011755f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.670030f, 0.246853f, 0.000000f), new Vector3(0.787579f, 0.264486f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.787579f, 0.264486f, 0.000000f), new Vector3(0.808150f, 0.273302f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.808150f, 0.273302f, 0.000000f), new Vector3(0.908067f, 0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.908067f, 0.332076f, 0.000000f), new Vector3(0.978596f, 0.426116f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.978596f, 0.426116f, 0.000000f), new Vector3(0.987413f, 0.449625f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.987413f, 0.449625f, 0.000000f), new Vector3(1.010922f, 0.561297f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.010922f, 0.561297f, 0.000000f), new Vector3(1.013861f, 0.617133f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.013861f, 0.617133f, 0.000000f), new Vector3(0.999168f, 0.734682f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.999168f, 0.734682f, 0.000000f), new Vector3(0.987413f, 0.764069f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.987413f, 0.764069f, 0.000000f), new Vector3(0.928638f, 0.863986f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.928638f, 0.863986f, 0.000000f), new Vector3(0.916883f, 0.875741f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.916883f, 0.875741f, 0.000000f), new Vector3(0.816966f, 0.940393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.816966f, 0.940393f, 0.000000f), new Vector3(0.699417f, 0.963903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.699417f, 0.963903f, 0.000000f), new Vector3(0.670030f, 0.963903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 0.963903f, 0.000000f), new Vector3(0.670030f, 0.246853f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.511339f, 0.246853f, 0.000000f), new Vector3(0.511339f, 0.963903f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.511339f, 0.963903f, 0.000000f), new Vector3(0.390851f, 0.949209f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, 0.949209f, 0.000000f), new Vector3(0.367341f, 0.940393f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.367341f, 0.940393f, 0.000000f), new Vector3(0.267424f, 0.878680f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.267424f, 0.878680f, 0.000000f), new Vector3(0.264486f, 0.875741f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.264486f, 0.875741f, 0.000000f), new Vector3(0.199833f, 0.781702f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.199833f, 0.781702f, 0.000000f), new Vector3(0.191017f, 0.764069f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.764069f, 0.000000f), new Vector3(0.167507f, 0.649459f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.167507f, 0.649459f, 0.000000f), new Vector3(0.164569f, 0.617133f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.617133f, 0.000000f), new Vector3(0.179262f, 0.496645f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.496645f, 0.000000f), new Vector3(0.191017f, 0.452564f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.452564f, 0.000000f), new Vector3(0.252731f, 0.349709f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.252731f, 0.349709f, 0.000000f), new Vector3(0.270363f, 0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.270363f, 0.332076f, 0.000000f), new Vector3(0.370280f, 0.273302f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370280f, 0.273302f, 0.000000f), new Vector3(0.487829f, 0.246853f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.487829f, 0.246853f, 0.000000f), new Vector3(0.511339f, 0.246853f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharPhi_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.202772f, 0.899251f, 0.000000f), new Vector3(0.173385f, 0.869863f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.173385f, 0.869863f, 0.000000f), new Vector3(0.099917f, 0.775824f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.099917f, 0.775824f, 0.000000f), new Vector3(0.047020f, 0.678846f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.047020f, 0.678846f, 0.000000f), new Vector3(0.011755f, 0.558358f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.011755f, 0.558358f, 0.000000f), new Vector3(0.000000f, 0.440809f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.440809f, 0.000000f), new Vector3(0.005877f, 0.361464f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.361464f, 0.000000f), new Vector3(0.038203f, 0.252731f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038203f, 0.252731f, 0.000000f), new Vector3(0.064652f, 0.202772f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.064652f, 0.202772f, 0.000000f), new Vector3(0.143998f, 0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.143998f, 0.114610f, 0.000000f), new Vector3(0.191017f, 0.079346f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.079346f, 0.000000f), new Vector3(0.296812f, 0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.296812f, 0.026449f, 0.000000f), new Vector3(0.373218f, 0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.373218f, 0.002939f, 0.000000f), new Vector3(0.490768f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.490768f, -0.011755f, 0.000000f), new Vector3(0.490768f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.490768f, -0.332076f, 0.000000f), new Vector3(0.640643f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640643f, -0.332076f, 0.000000f), new Vector3(0.640643f, -0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640643f, -0.011755f, 0.000000f), new Vector3(0.687662f, -0.008816f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.687662f, -0.008816f, 0.000000f), new Vector3(0.805211f, 0.017632f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.805211f, 0.017632f, 0.000000f), new Vector3(0.908067f, 0.061713f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.908067f, 0.061713f, 0.000000f), new Vector3(1.016800f, 0.146936f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.016800f, 0.146936f, 0.000000f), new Vector3(1.081452f, 0.238037f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.081452f, 0.238037f, 0.000000f), new Vector3(1.122594f, 0.343831f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.122594f, 0.343831f, 0.000000f), new Vector3(1.134349f, 0.464319f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.134349f, 0.464319f, 0.000000f), new Vector3(1.134349f, 0.502522f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.134349f, 0.502522f, 0.000000f), new Vector3(1.113778f, 0.620072f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.113778f, 0.620072f, 0.000000f), new Vector3(1.066758f, 0.719988f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.066758f, 0.719988f, 0.000000f), new Vector3(0.993290f, 0.802273f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.993290f, 0.802273f, 0.000000f), new Vector3(0.949209f, 0.837537f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.949209f, 0.837537f, 0.000000f), new Vector3(0.852231f, 0.884557f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.852231f, 0.884557f, 0.000000f), new Vector3(0.737621f, 0.913944f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.737621f, 0.913944f, 0.000000f), new Vector3(0.608317f, 0.922761f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, 0.922761f, 0.000000f), new Vector3(0.546603f, 0.922761f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.546603f, 0.922761f, 0.000000f), new Vector3(0.490768f, 0.919822f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.490768f, 0.919822f, 0.000000f), new Vector3(0.490768f, 0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.490768f, 0.114610f, 0.000000f), new Vector3(0.449625f, 0.117549f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.449625f, 0.117549f, 0.000000f), new Vector3(0.337954f, 0.149875f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.337954f, 0.149875f, 0.000000f), new Vector3(0.246853f, 0.211588f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.246853f, 0.211588f, 0.000000f), new Vector3(0.223343f, 0.238037f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.223343f, 0.238037f, 0.000000f), new Vector3(0.173385f, 0.337954f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.173385f, 0.337954f, 0.000000f), new Vector3(0.155753f, 0.458442f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.458442f, 0.000000f), new Vector3(0.173385f, 0.593623f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.173385f, 0.593623f, 0.000000f), new Vector3(0.217466f, 0.699417f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.217466f, 0.699417f, 0.000000f), new Vector3(0.226282f, 0.714111f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226282f, 0.714111f, 0.000000f), new Vector3(0.293873f, 0.805211f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.293873f, 0.805211f, 0.000000f), new Vector3(0.384973f, 0.887496f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.384973f, 0.887496f, 0.000000f), new Vector3(0.384973f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.384973f, 0.899251f, 0.000000f), new Vector3(0.202772f, 0.899251f, 0.000000f)));

            b0.Add(new Line3(new Vector3(0.640643f, 0.114610f, 0.000000f), new Vector3(0.767008f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767008f, 0.135181f, 0.000000f), new Vector3(0.861047f, 0.188079f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.861047f, 0.188079f, 0.000000f), new Vector3(0.893373f, 0.217466f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.893373f, 0.217466f, 0.000000f), new Vector3(0.949209f, 0.308566f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.949209f, 0.308566f, 0.000000f), new Vector3(0.975658f, 0.423177f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.975658f, 0.423177f, 0.000000f), new Vector3(0.978596f, 0.473135f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.978596f, 0.473135f, 0.000000f), new Vector3(0.960964f, 0.590684f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.960964f, 0.590684f, 0.000000f), new Vector3(0.908067f, 0.687662f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.908067f, 0.687662f, 0.000000f), new Vector3(0.887496f, 0.708233f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.887496f, 0.708233f, 0.000000f), new Vector3(0.796395f, 0.767008f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.796395f, 0.767008f, 0.000000f), new Vector3(0.681785f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.681785f, 0.796395f, 0.000000f), new Vector3(0.640643f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.640643f, 0.796395f, 0.000000f), new Vector3(0.640643f, 0.114610f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__CHI
        private static List<Line3> getCharChi()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.173385f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.173385f, 0.000000f, 0.000000f), new Vector3(0.508400f, 0.496645f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.508400f, 0.496645f, 0.000000f), new Vector3(0.834599f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.834599f, 0.000000f, 0.000000f), new Vector3(1.019739f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.019739f, 0.000000f, 0.000000f), new Vector3(0.608317f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.608317f, 0.608317f, 0.000000f), new Vector3(1.022677f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.022677f, 1.201940f, 0.000000f), new Vector3(0.846354f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.846354f, 1.201940f, 0.000000f), new Vector3(0.517216f, 0.714111f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.517216f, 0.714111f, 0.000000f), new Vector3(0.193956f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.193956f, 1.201940f, 0.000000f), new Vector3(0.008816f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.008816f, 1.201940f, 0.000000f), new Vector3(0.417299f, 0.599500f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.417299f, 0.599500f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharChi_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, -0.332076f, 0.000000f), new Vector3(0.161630f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, -0.332076f, 0.000000f), new Vector3(0.443748f, 0.167507f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.443748f, 0.167507f, 0.000000f), new Vector3(0.722927f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.722927f, -0.332076f, 0.000000f), new Vector3(0.893373f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.893373f, -0.332076f, 0.000000f), new Vector3(0.537787f, 0.293873f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.537787f, 0.293873f, 0.000000f), new Vector3(0.884557f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.884557f, 0.899251f, 0.000000f), new Vector3(0.722927f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.722927f, 0.899251f, 0.000000f), new Vector3(0.449625f, 0.417299f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.449625f, 0.417299f, 0.000000f), new Vector3(0.179262f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.899251f, 0.000000f), new Vector3(0.008816f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.008816f, 0.899251f, 0.000000f), new Vector3(0.355586f, 0.287995f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.355586f, 0.287995f, 0.000000f), new Vector3(0.000000f, -0.332076f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__PSI
        private static List<Line3> getCharPsi()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.493706f, 0.000000f, 0.000000f), new Vector3(0.652398f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652398f, 0.000000f, 0.000000f), new Vector3(0.652398f, 0.261547f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652398f, 0.261547f, 0.000000f), new Vector3(0.755253f, 0.273302f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.755253f, 0.273302f, 0.000000f), new Vector3(0.863986f, 0.296812f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.863986f, 0.296812f, 0.000000f), new Vector3(0.922761f, 0.320321f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.922761f, 0.320321f, 0.000000f), new Vector3(1.019739f, 0.382035f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.019739f, 0.382035f, 0.000000f), new Vector3(1.055003f, 0.417299f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.055003f, 0.417299f, 0.000000f), new Vector3(1.113778f, 0.523094f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.113778f, 0.523094f, 0.000000f), new Vector3(1.137288f, 0.608317f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.137288f, 0.608317f, 0.000000f), new Vector3(1.146104f, 0.734682f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.146104f, 0.734682f, 0.000000f), new Vector3(1.146104f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.146104f, 1.201940f, 0.000000f), new Vector3(0.987413f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.987413f, 1.201940f, 0.000000f), new Vector3(0.987413f, 0.717050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.987413f, 0.717050f, 0.000000f), new Vector3(0.987413f, 0.681785f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.987413f, 0.681785f, 0.000000f), new Vector3(0.966842f, 0.573052f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.966842f, 0.573052f, 0.000000f), new Vector3(0.896312f, 0.476074f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.896312f, 0.476074f, 0.000000f), new Vector3(0.793457f, 0.423177f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.793457f, 0.423177f, 0.000000f), new Vector3(0.769947f, 0.417299f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.769947f, 0.417299f, 0.000000f), new Vector3(0.652398f, 0.399667f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652398f, 0.399667f, 0.000000f), new Vector3(0.652398f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.652398f, 1.201940f, 0.000000f), new Vector3(0.493706f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.493706f, 1.201940f, 0.000000f), new Vector3(0.493706f, 0.399667f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.493706f, 0.399667f, 0.000000f), new Vector3(0.467258f, 0.399667f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.467258f, 0.399667f, 0.000000f), new Vector3(0.352647f, 0.423177f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352647f, 0.423177f, 0.000000f), new Vector3(0.249792f, 0.479013f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249792f, 0.479013f, 0.000000f), new Vector3(0.182201f, 0.570113f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182201f, 0.570113f, 0.000000f), new Vector3(0.173385f, 0.596562f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.173385f, 0.596562f, 0.000000f), new Vector3(0.158691f, 0.717050f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 0.717050f, 0.000000f), new Vector3(0.158691f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.158691f, 1.201940f, 0.000000f), new Vector3(0.000000f, 1.201940f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 1.201940f, 0.000000f), new Vector3(0.000000f, 0.734682f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.734682f, 0.000000f), new Vector3(0.005877f, 0.628888f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.005877f, 0.628888f, 0.000000f), new Vector3(0.032326f, 0.523094f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.032326f, 0.523094f, 0.000000f), new Vector3(0.055836f, 0.470196f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.055836f, 0.470196f, 0.000000f), new Vector3(0.126365f, 0.382035f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.126365f, 0.382035f, 0.000000f), new Vector3(0.170446f, 0.349709f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170446f, 0.349709f, 0.000000f), new Vector3(0.279179f, 0.299750f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.279179f, 0.299750f, 0.000000f), new Vector3(0.370280f, 0.276240f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.370280f, 0.276240f, 0.000000f), new Vector3(0.493706f, 0.261547f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.493706f, 0.261547f, 0.000000f), new Vector3(0.493706f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharPsi_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.461380f, -0.332076f, 0.000000f), new Vector3(0.611255f, -0.332076f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611255f, -0.332076f, 0.000000f), new Vector3(0.611255f, -0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611255f, -0.002939f, 0.000000f), new Vector3(0.670030f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 0.000000f, 0.000000f), new Vector3(0.784640f, 0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.784640f, 0.023510f, 0.000000f), new Vector3(0.825783f, 0.035265f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.825783f, 0.035265f, 0.000000f), new Vector3(0.931577f, 0.088162f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.931577f, 0.088162f, 0.000000f), new Vector3(0.958025f, 0.108733f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.958025f, 0.108733f, 0.000000f), new Vector3(1.031494f, 0.199833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.031494f, 0.199833f, 0.000000f), new Vector3(1.052065f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.052065f, 0.240976f, 0.000000f), new Vector3(1.072636f, 0.358525f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.072636f, 0.358525f, 0.000000f), new Vector3(1.072636f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.072636f, 0.899251f, 0.000000f), new Vector3(0.919822f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.919822f, 0.899251f, 0.000000f), new Vector3(0.919822f, 0.405544f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.919822f, 0.405544f, 0.000000f), new Vector3(0.919822f, 0.367341f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.919822f, 0.367341f, 0.000000f), new Vector3(0.893373f, 0.261547f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.893373f, 0.261547f, 0.000000f), new Vector3(0.816966f, 0.179262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.816966f, 0.179262f, 0.000000f), new Vector3(0.719988f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.719988f, 0.135181f, 0.000000f), new Vector3(0.611255f, 0.120488f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611255f, 0.120488f, 0.000000f), new Vector3(0.611255f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.611255f, 0.899251f, 0.000000f), new Vector3(0.461380f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.461380f, 0.899251f, 0.000000f), new Vector3(0.461380f, 0.120488f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.461380f, 0.120488f, 0.000000f), new Vector3(0.352647f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.352647f, 0.135181f, 0.000000f), new Vector3(0.252731f, 0.179262f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.252731f, 0.179262f, 0.000000f), new Vector3(0.182201f, 0.261547f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.182201f, 0.261547f, 0.000000f), new Vector3(0.170446f, 0.287995f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.170446f, 0.287995f, 0.000000f), new Vector3(0.149875f, 0.405544f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.405544f, 0.000000f), new Vector3(0.149875f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 0.899251f, 0.000000f), new Vector3(0.000000f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.899251f, 0.000000f), new Vector3(0.000000f, 0.358525f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.358525f, 0.000000f), new Vector3(0.002939f, 0.308566f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.002939f, 0.308566f, 0.000000f), new Vector3(0.038203f, 0.199833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038203f, 0.199833f, 0.000000f), new Vector3(0.055836f, 0.170446f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.055836f, 0.170446f, 0.000000f), new Vector3(0.141059f, 0.088162f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.141059f, 0.088162f, 0.000000f), new Vector3(0.179262f, 0.064652f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.179262f, 0.064652f, 0.000000f), new Vector3(0.287995f, 0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.287995f, 0.023510f, 0.000000f), new Vector3(0.340892f, 0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.340892f, 0.011755f, 0.000000f), new Vector3(0.461380f, -0.002939f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.461380f, -0.002939f, 0.000000f), new Vector3(0.461380f, -0.332076f, 0.000000f)));

            return b0;
        }
        #endregion

        #region verdana_12_regular__OMEGA
        private static List<Line3> getCharOmega()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.464319f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.464319f, 0.000000f, 0.000000f), new Vector3(0.464319f, 0.314444f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.464319f, 0.314444f, 0.000000f), new Vector3(0.367341f, 0.379096f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.367341f, 0.379096f, 0.000000f), new Vector3(0.285057f, 0.461380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.285057f, 0.461380f, 0.000000f), new Vector3(0.279179f, 0.467258f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.279179f, 0.467258f, 0.000000f), new Vector3(0.226282f, 0.573052f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.226282f, 0.573052f, 0.000000f), new Vector3(0.217466f, 0.602439f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.217466f, 0.602439f, 0.000000f), new Vector3(0.205711f, 0.722927f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.205711f, 0.722927f, 0.000000f), new Vector3(0.211588f, 0.790518f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.211588f, 0.790518f, 0.000000f), new Vector3(0.243914f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.243914f, 0.899251f, 0.000000f), new Vector3(0.308566f, 0.987413f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.308566f, 0.987413f, 0.000000f), new Vector3(0.361464f, 1.031494f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.361464f, 1.031494f, 0.000000f), new Vector3(0.464319f, 1.072636f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.464319f, 1.072636f, 0.000000f), new Vector3(0.587746f, 1.087329f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 1.087329f, 0.000000f), new Vector3(0.670030f, 1.081452f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.670030f, 1.081452f, 0.000000f), new Vector3(0.778763f, 1.049126f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.778763f, 1.049126f, 0.000000f), new Vector3(0.866925f, 0.987413f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.866925f, 0.987413f, 0.000000f), new Vector3(0.905128f, 0.943332f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.905128f, 0.943332f, 0.000000f), new Vector3(0.952148f, 0.843415f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.952148f, 0.843415f, 0.000000f), new Vector3(0.969780f, 0.722927f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.969780f, 0.722927f, 0.000000f), new Vector3(0.969780f, 0.684724f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.969780f, 0.684724f, 0.000000f), new Vector3(0.949209f, 0.573052f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.949209f, 0.573052f, 0.000000f), new Vector3(0.890435f, 0.461380f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.890435f, 0.461380f, 0.000000f), new Vector3(0.808150f, 0.379096f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.808150f, 0.379096f, 0.000000f), new Vector3(0.711172f, 0.314444f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711172f, 0.314444f, 0.000000f), new Vector3(0.711172f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.711172f, 0.000000f, 0.000000f), new Vector3(1.175491f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.175491f, 0.000000f, 0.000000f), new Vector3(1.175491f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.175491f, 0.141059f, 0.000000f), new Vector3(0.846354f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.846354f, 0.141059f, 0.000000f), new Vector3(0.846354f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.846354f, 0.240976f, 0.000000f), new Vector3(0.893373f, 0.270363f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.893373f, 0.270363f, 0.000000f), new Vector3(0.981535f, 0.346770f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.981535f, 0.346770f, 0.000000f), new Vector3(1.055003f, 0.440809f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.055003f, 0.440809f, 0.000000f), new Vector3(1.084391f, 0.493706f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.084391f, 0.493706f, 0.000000f), new Vector3(1.122594f, 0.602439f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.122594f, 0.602439f, 0.000000f), new Vector3(1.134349f, 0.722927f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.134349f, 0.722927f, 0.000000f), new Vector3(1.131410f, 0.784640f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.131410f, 0.784640f, 0.000000f), new Vector3(1.107900f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.107900f, 0.899251f, 0.000000f), new Vector3(1.057942f, 0.999168f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.057942f, 0.999168f, 0.000000f), new Vector3(0.984474f, 1.084391f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.984474f, 1.084391f, 0.000000f), new Vector3(0.922761f, 1.134349f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.922761f, 1.134349f, 0.000000f), new Vector3(0.822844f, 1.184307f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.822844f, 1.184307f, 0.000000f), new Vector3(0.714111f, 1.216633f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.714111f, 1.216633f, 0.000000f), new Vector3(0.587746f, 1.225450f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 1.225450f, 0.000000f), new Vector3(0.496645f, 1.219572f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.496645f, 1.219572f, 0.000000f), new Vector3(0.382035f, 1.196062f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.382035f, 1.196062f, 0.000000f), new Vector3(0.279179f, 1.151981f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.279179f, 1.151981f, 0.000000f), new Vector3(0.191017f, 1.084391f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 1.084391f, 0.000000f), new Vector3(0.149875f, 1.040310f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.149875f, 1.040310f, 0.000000f), new Vector3(0.088162f, 0.949209f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.088162f, 0.949209f, 0.000000f), new Vector3(0.052897f, 0.840476f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.052897f, 0.840476f, 0.000000f), new Vector3(0.041142f, 0.722927f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.041142f, 0.722927f, 0.000000f), new Vector3(0.044081f, 0.655336f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.044081f, 0.655336f, 0.000000f), new Vector3(0.070529f, 0.543665f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.070529f, 0.543665f, 0.000000f), new Vector3(0.123427f, 0.440809f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.123427f, 0.440809f, 0.000000f), new Vector3(0.155753f, 0.393790f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.393790f, 0.000000f), new Vector3(0.235098f, 0.308566f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.235098f, 0.308566f, 0.000000f), new Vector3(0.329138f, 0.240976f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.329138f, 0.240976f, 0.000000f), new Vector3(0.329138f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.329138f, 0.141059f, 0.000000f), new Vector3(0.000000f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.141059f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f)));

            return b0;
        }
        private static List<Line3> getCharOmega_small()
        {
            List<Line3> b0 = new List<Line3>();

            b0.Add(new Line3(new Vector3(0.205711f, 0.899251f, 0.000000f), new Vector3(0.164569f, 0.846354f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.164569f, 0.846354f, 0.000000f), new Vector3(0.094039f, 0.746437f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.094039f, 0.746437f, 0.000000f), new Vector3(0.044081f, 0.655336f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.044081f, 0.655336f, 0.000000f), new Vector3(0.038203f, 0.637704f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.038203f, 0.637704f, 0.000000f), new Vector3(0.008816f, 0.526032f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.008816f, 0.526032f, 0.000000f), new Vector3(0.000000f, 0.405544f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.405544f, 0.000000f), new Vector3(0.000000f, 0.367341f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.000000f, 0.367341f, 0.000000f), new Vector3(0.017632f, 0.249792f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.017632f, 0.249792f, 0.000000f), new Vector3(0.029387f, 0.217466f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.029387f, 0.217466f, 0.000000f), new Vector3(0.079346f, 0.111672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.079346f, 0.111672f, 0.000000f), new Vector3(0.096978f, 0.088162f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.096978f, 0.088162f, 0.000000f), new Vector3(0.185140f, 0.014694f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.185140f, 0.014694f, 0.000000f), new Vector3(0.223343f, -0.005877f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.223343f, -0.005877f, 0.000000f), new Vector3(0.340892f, -0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.340892f, -0.023510f, 0.000000f), new Vector3(0.376157f, -0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.376157f, -0.023510f, 0.000000f), new Vector3(0.481951f, 0.014694f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.481951f, 0.014694f, 0.000000f), new Vector3(0.496645f, 0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.496645f, 0.023510f, 0.000000f), new Vector3(0.573052f, 0.108733f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.573052f, 0.108733f, 0.000000f), new Vector3(0.578929f, 0.108733f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.578929f, 0.108733f, 0.000000f), new Vector3(0.587746f, 0.094039f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.587746f, 0.094039f, 0.000000f), new Vector3(0.672969f, 0.014694f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.672969f, 0.014694f, 0.000000f), new Vector3(0.702356f, 0.000000f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.702356f, 0.000000f, 0.000000f), new Vector3(0.816966f, -0.023510f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.816966f, -0.023510f, 0.000000f), new Vector3(0.866925f, -0.020571f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.866925f, -0.020571f, 0.000000f), new Vector3(0.972719f, 0.011755f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.972719f, 0.011755f, 0.000000f), new Vector3(0.999168f, 0.026449f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.999168f, 0.026449f, 0.000000f), new Vector3(1.081452f, 0.111672f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.081452f, 0.111672f, 0.000000f), new Vector3(1.096146f, 0.141059f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.096146f, 0.141059f, 0.000000f), new Vector3(1.140226f, 0.249792f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.140226f, 0.249792f, 0.000000f), new Vector3(1.149043f, 0.287995f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.149043f, 0.287995f, 0.000000f), new Vector3(1.157859f, 0.405544f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.157859f, 0.405544f, 0.000000f), new Vector3(1.157859f, 0.426116f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.157859f, 0.426116f, 0.000000f), new Vector3(1.146104f, 0.546603f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.146104f, 0.546603f, 0.000000f), new Vector3(1.113778f, 0.655336f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.113778f, 0.655336f, 0.000000f), new Vector3(1.093207f, 0.699417f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.093207f, 0.699417f, 0.000000f), new Vector3(1.031494f, 0.796395f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.031494f, 0.796395f, 0.000000f), new Vector3(0.952148f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.952148f, 0.899251f, 0.000000f), new Vector3(0.767008f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767008f, 0.899251f, 0.000000f), new Vector3(0.767008f, 0.887496f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.767008f, 0.887496f, 0.000000f), new Vector3(0.799334f, 0.858109f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.799334f, 0.858109f, 0.000000f), new Vector3(0.878680f, 0.772885f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.878680f, 0.772885f, 0.000000f), new Vector3(0.940393f, 0.672969f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.940393f, 0.672969f, 0.000000f), new Vector3(0.960964f, 0.634765f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.960964f, 0.634765f, 0.000000f), new Vector3(0.993290f, 0.523094f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.993290f, 0.523094f, 0.000000f), new Vector3(1.005045f, 0.402606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(1.005045f, 0.402606f, 0.000000f), new Vector3(0.996229f, 0.302689f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.996229f, 0.302689f, 0.000000f), new Vector3(0.966842f, 0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.966842f, 0.208650f, 0.000000f), new Vector3(0.908067f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.908067f, 0.135181f, 0.000000f), new Vector3(0.814028f, 0.105794f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.814028f, 0.105794f, 0.000000f), new Vector3(0.743498f, 0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.743498f, 0.114610f, 0.000000f), new Vector3(0.699417f, 0.138120f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.699417f, 0.138120f, 0.000000f), new Vector3(0.672969f, 0.170446f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.672969f, 0.170446f, 0.000000f), new Vector3(0.655336f, 0.199833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655336f, 0.199833f, 0.000000f), new Vector3(0.655336f, 0.672969f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.655336f, 0.672969f, 0.000000f), new Vector3(0.505461f, 0.672969f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.505461f, 0.672969f, 0.000000f), new Vector3(0.505461f, 0.199833f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.505461f, 0.199833f, 0.000000f), new Vector3(0.487829f, 0.170446f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.487829f, 0.170446f, 0.000000f), new Vector3(0.458442f, 0.138120f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.458442f, 0.138120f, 0.000000f), new Vector3(0.414361f, 0.114610f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.414361f, 0.114610f, 0.000000f), new Vector3(0.343831f, 0.105794f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.343831f, 0.105794f, 0.000000f), new Vector3(0.249792f, 0.135181f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.249792f, 0.135181f, 0.000000f), new Vector3(0.191017f, 0.208650f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.191017f, 0.208650f, 0.000000f), new Vector3(0.161630f, 0.305628f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.161630f, 0.305628f, 0.000000f), new Vector3(0.152814f, 0.402606f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.152814f, 0.402606f, 0.000000f), new Vector3(0.155753f, 0.452564f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.155753f, 0.452564f, 0.000000f), new Vector3(0.176324f, 0.567174f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.176324f, 0.567174f, 0.000000f), new Vector3(0.217466f, 0.675907f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.217466f, 0.675907f, 0.000000f), new Vector3(0.238037f, 0.711172f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.238037f, 0.711172f, 0.000000f), new Vector3(0.305628f, 0.805211f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.305628f, 0.805211f, 0.000000f), new Vector3(0.390851f, 0.887496f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, 0.887496f, 0.000000f), new Vector3(0.390851f, 0.899251f, 0.000000f)));
            b0.Add(new Line3(new Vector3(0.390851f, 0.899251f, 0.000000f), new Vector3(0.205711f, 0.899251f, 0.000000f)));

            return b0;
        }
        #endregion

    }
}
