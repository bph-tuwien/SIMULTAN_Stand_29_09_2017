using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

using DXFImportExport;

namespace GeometryViewer.UIElements
{
    public class IndexColor
    {
        public static IndexColor ByLayer = new IndexColor(Colors.Black, -1);
        public Color Color { get; private set; }
        public int Index { get; private set; }

        public IndexColor(Color _color, int _index)
        {
            this.Color = _color;
            this.Index = _index;
        }
    }
    public class StaticResources
    {
        public static List<IndexColor> FirstColors { get; private set; }
        public static List<IndexColor> OddColors { get; private set; }
        public static List<IndexColor> EvenColors { get; private set; }
        public static List<IndexColor> LastColors { get; private set; }

        static StaticResources()
        {
            FirstColors = new List<IndexColor>();
            OddColors = new List<IndexColor>();
            EvenColors = new List<IndexColor>();
            LastColors = new List<IndexColor>();
            int i;

            FirstColors.Add(new IndexColor(Colors.Black, 0));

            for (i = 1; i < 10; i++)
            {
                DXFColor dxfCol = DXFColor.Index2DXFColor(i);
                byte ri = (byte)(dxfCol.R * 255);
                byte gi = (byte)(dxfCol.G * 255);
                byte bi = (byte)(dxfCol.B * 255);
                Color col = Color.FromRgb(ri, gi, bi);
                FirstColors.Add(new IndexColor(col, i));
            }
            FirstColors.Add(IndexColor.ByLayer);
            for (i = 10; i < 250; i++)
            {
                DXFColor dxfCol = DXFColor.Index2DXFColor(i);
                byte ri = (byte)(dxfCol.R * 255);
                byte gi = (byte)(dxfCol.G * 255);
                byte bi = (byte)(dxfCol.B * 255);
                Color col = Color.FromRgb(ri, gi, bi);
                if ((i % 2) == 0)
                    EvenColors.Add(new IndexColor(col, i));
                else
                    OddColors.Add(new IndexColor(col, i));
            }
            for (i = 250; i < 256; i++)
            {
                DXFColor dxfCol = DXFColor.Index2DXFColor(i);
                byte ri = (byte)(dxfCol.R * 255);
                byte gi = (byte)(dxfCol.G * 255);
                byte bi = (byte)(dxfCol.B * 255);
                Color col = Color.FromRgb(ri, gi, bi);
                LastColors.Add(new IndexColor(col, i));
            }
            EvenColors.Reverse();

        }

        public StaticResources()
        {

        }
    }
}
