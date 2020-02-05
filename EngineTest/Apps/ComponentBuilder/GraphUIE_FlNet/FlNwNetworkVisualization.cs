using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.ComponentModel;

using ComponentBuilder.WinUtils;
using ParameterStructure.Component;

namespace ComponentBuilder.GraphUIE_FlNet
{
    public class FlNwNetworkVisualization : FlNwNodeVisualization
    {
        #region STATIC: Symbols for the Main Node

        protected static List<PathSegment> GetVis1(double _height, double _width, out Point start)
        {
            // calculate geometry
            double offset_h0 = 3;
            double offset_h1 = _width * 0.2;
            double offset_h2 = _width * 0.8;
            double offset_h_last = _width - 3;

            double offset_v0 = 3;
            double offset_v1 = _height * 0.35;
            double offset_v2 = _height * 0.65;
            double offset_v_last = _height - 3;

            double offset_v1a = _height * 0.20;
            double offset_v2a = _height * 0.40;
            double offset_v3a = _height * 0.60;
            double offset_v4a = _height * 0.80;

            start = new Point(offset_h0, offset_v1);

            // construct polyline
            List<PathSegment> segments_1 = new List<PathSegment>();
            segments_1.Add(new LineSegment(new Point(offset_h1, offset_v1), true)); // 1
            segments_1.Add(new ArcSegment(new Point(offset_h1 + offset_v1, offset_v0), new Size(offset_v1, offset_v1),
                                            90, false, SweepDirection.Clockwise, true)); // 2
            segments_1.Add(new LineSegment(new Point(offset_h2, offset_v0), true)); // 3
            segments_1.Add(new LineSegment(new Point(offset_h2, offset_v1a), true)); // 4
            segments_1.Add(new LineSegment(new Point(offset_h_last, offset_v1a), true)); // 5
            segments_1.Add(new LineSegment(new Point(offset_h_last, offset_v2a), true)); // 6
            segments_1.Add(new LineSegment(new Point(offset_h2, offset_v2a), true)); // 7
            segments_1.Add(new LineSegment(new Point(offset_h2, offset_v3a), true)); // 8
            segments_1.Add(new LineSegment(new Point(offset_h_last, offset_v3a), true)); // 9
            segments_1.Add(new LineSegment(new Point(offset_h_last, offset_v4a), true)); // 10
            segments_1.Add(new LineSegment(new Point(offset_h2, offset_v4a), true)); // 11
            segments_1.Add(new LineSegment(new Point(offset_h2, offset_v_last), true)); // 12
            segments_1.Add(new LineSegment(new Point(offset_h1 + offset_v1, offset_v_last), true)); // 13
            segments_1.Add(new ArcSegment(new Point(offset_h1, offset_v2), new Size(offset_v1, offset_v1),
                                            90, false, SweepDirection.Clockwise, true)); // 14
            segments_1.Add(new LineSegment(new Point(offset_h0, offset_v2), true)); // 15

            return segments_1;
        }

        protected static List<PathSegment> GetVis2(double _height, double _width, out Point start)
        {
            // calculate geometry
            double offset_ha1 = 3;
            double offset_ha2 = _width * 0.0536;
            double offset_ha3 = _width * 0.1964;
            double offset_ha4 = _width * 0.2500;

            double offset_hb1 = _width * 0.4286;
            double offset_hb2 = _width * 0.4821;
            double offset_hb3 = _width * 0.6250;
            double offset_hb4 = _width * 0.6786;

            double offset_hc1 = _width * 0.7500;
            double offset_hc2 = _width * 0.8036;
            double offset_hc3 = _width * 0.9464;
            double offset_hc4 = _width - 3;

            double offset_hT1 = _width * 0.3214;
            double offset_hT2 = _width * 0.3571;
            double offset_hT3 = _width * 0.3929;


            double offset_va1 = _height * 0.2500;
            double offset_va2 = _height * 0.4375;
            double offset_va3 = _height * 0.5625;
            double offset_va4 = _height * 0.7500;

            double offset_vb1 = 3;
            double offset_vb2 = _height * 0.1875;
            double offset_vb3 = _height * 0.3125;

            double offset_vc1 = _height * 0.5000;
            double offset_vc2 = _height * 0.6875;
            double offset_vc3 = _height * 0.8125;
            double offset_vc4 = _height - 3;


            start = new Point(offset_ha1, offset_va2); // 0

            // construct polyline
            List<PathSegment> segments_1 = new List<PathSegment>();
            segments_1.Add(new ArcSegment(new Point(offset_ha2, offset_va1), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 1
            segments_1.Add(new LineSegment(new Point(offset_ha3, offset_va1), true)); // 2
            segments_1.Add(new ArcSegment(new Point(offset_ha4, offset_va2), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 3
            segments_1.Add(new LineSegment(new Point(offset_hT1, offset_va2), true)); // 4
            segments_1.Add(new ArcSegment(new Point(offset_hT3, offset_vb2), new Size(_width * 0.25, _width * 0.25),
                                            90, false, SweepDirection.Clockwise, true)); // 5
            segments_1.Add(new LineSegment(new Point(offset_hb1, offset_vb2), true)); // 6
            segments_1.Add(new ArcSegment(new Point(offset_hb2, offset_vb1), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 7
            segments_1.Add(new LineSegment(new Point(offset_hb3, offset_vb1), true)); // 8
            segments_1.Add(new ArcSegment(new Point(offset_hb4, offset_vb2), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 9
            segments_1.Add(new LineSegment(new Point(offset_hb4, offset_vb3), true)); // 10
            segments_1.Add(new ArcSegment(new Point(offset_hb3, offset_vc1), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 11
            segments_1.Add(new LineSegment(new Point(offset_hb2, offset_vc1), true)); // 12
            segments_1.Add(new ArcSegment(new Point(offset_hb1, offset_vb3), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 13
            segments_1.Add(new LineSegment(new Point(offset_hT3, offset_vb3), true)); // 14
            segments_1.Add(new ArcSegment(new Point(offset_hT2, offset_va2), new Size(_width * 0.125, _width * 0.125),
                                            90, false, SweepDirection.Clockwise, true)); // 15
            segments_1.Add(new LineSegment(new Point(offset_hT2, offset_va3), true)); // 16
            segments_1.Add(new ArcSegment(new Point(offset_hT3, offset_vc2), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 17
            segments_1.Add(new LineSegment(new Point(offset_hc1, offset_vc2), true)); // 18
            segments_1.Add(new ArcSegment(new Point(offset_hc2, offset_vc1), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 19
            segments_1.Add(new LineSegment(new Point(offset_hc3, offset_vc1), true)); // 20
            segments_1.Add(new ArcSegment(new Point(offset_hc4, offset_vc2), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 21
            segments_1.Add(new LineSegment(new Point(offset_hc4, offset_vc3), true)); // 22
            segments_1.Add(new ArcSegment(new Point(offset_hc3, offset_vc4), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 23
            segments_1.Add(new LineSegment(new Point(offset_hc2, offset_vc4), true)); // 24
            segments_1.Add(new ArcSegment(new Point(offset_hc1, offset_vc3), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 25
            segments_1.Add(new LineSegment(new Point(offset_hT3, offset_vc3), true)); // 26
            segments_1.Add(new ArcSegment(new Point(offset_hT1, offset_va3), new Size(_width * 0.25, _width * 0.25),
                                            90, false, SweepDirection.Clockwise, true)); // 27
            segments_1.Add(new LineSegment(new Point(offset_ha4, offset_va3), true)); // 28
            segments_1.Add(new ArcSegment(new Point(offset_ha3, offset_va4), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 29
            segments_1.Add(new LineSegment(new Point(offset_ha2, offset_va4), true)); // 30
            segments_1.Add(new ArcSegment(new Point(offset_ha1, offset_va3), new Size(offset_vb2, offset_vb2),
                                            90, false, SweepDirection.Clockwise, true)); // 31            


            return segments_1;
        }

        #endregion

        #region PROPERTIES : Display
        public override bool ElementLocked { get { return this.fn_nw != null && fn_nw.IsLocked; } }

        #endregion

        #region CLASS MEMBERS

        protected FlowNetwork fn_nw;
        public FlowNetwork FN_NW { get { return this.fn_nw; } }

        #endregion

        #region .CTOR

        // assumes that _fn_nw is not NULL
        public FlNwNetworkVisualization(FlowNwGraph _parent, FlowNetwork _fn_nw, NodePosInFlow _pos_in_flow)
            : base(_parent, _fn_nw, _pos_in_flow)
        {
            this.fn_nw = _fn_nw;
        }

        #endregion

        #region METHODS: Display Update

        protected override void RedefineGrid()
        {
            // reset grid
            this.Children.Clear();
            // no comlumns or rows to clear
        }

        protected override void PopulateGrid()
        {
            // SOURCE or SINK sigifier
            if (this.PosInFlow != NodePosInFlow.INTERIOR)
                this.AddSourceOrSinkVis();

            // MAIN NODE
            Rectangle rect = new Rectangle();
            rect.Width = this.Width;
            rect.Height = this.Height;
            rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            double radius = Math.Floor(this.Height * 0.25);
            rect.RadiusX = radius;
            rect.RadiusY = radius;
            rect.ContextMenu = this.BuildContextMenu();

            if (this.PosInFlow == NodePosInFlow.SOURCE)
                rect.ToolTip = "Erster Knoten";
            else if (this.PosInFlow == NodePosInFlow.SINK)
                rect.ToolTip = "Letzter Knoten";

            this.element_main = rect;
            this.Children.Add(rect);

            // define the network shape
            Point startP;
            List<PathSegment> segments_1 = FlNwNetworkVisualization.GetVis2(this.Height, this.Width, out startP);
            
            PathFigure fig = new PathFigure(startP, segments_1, true);
            fig.IsFilled = true;
            PathGeometry geom = new PathGeometry(new List<PathFigure> { fig });
            Path p = new Path();
            p.Data = geom;
            p.IsHitTestVisible = false;

            p.Fill = new LinearGradientBrush(FlNwElementVisualization.NODE_COLOR_FILL_INACTIVE_1, 
                                             FlNwElementVisualization.NODE_COLOR_FILL_INACTIVE_2, 
                                             new Point(0.5, 0), new Point(0.5, 1));
            p.Stroke = new SolidColorBrush(FlNwElementVisualization.NODE_COLOR_FILL_INACTIVE_1);
            p.StrokeThickness = 1;

            this.Children.Add(p);

            // MAIN TEXT
            TextBlock tb = new TextBlock();
            tb.FontSize = 12;
            tb.Text = (this.fn_nw != null) ? fn_nw.Name : "NW";
            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tb.IsHitTestVisible = false;

            this.Children.Add(tb);

            this.VisState = (this.fn_nw != null && this.fn_nw.Children != null && this.fn_nw.Children.Count > 0) ? ElementVisHighlight.Full : ElementVisHighlight.Empty;
        }

        protected override ContextMenu BuildContextMenu()
        {
            ContextMenu cm = new ContextMenu();
            cm.UseLayoutRounding = true;

            MenuItem mi1 = new MenuItem();
            mi1.Header = "Netzwerk anzeigen";
            mi1.Command = new RelayCommand((x) => this.parent_canvas.SelectNetwork(this),
                                           (x) => this.parent_canvas != null && this.parent_canvas.CompFactory != null);
            mi1.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_selected.png", UriKind.Relative)), Width = 16, Height = 16 };
            cm.Items.Add(mi1);

            MenuItem mi2 = new MenuItem();
            mi2.Header = "In Knoten umwandeln";
            mi2.Command = new RelayCommand((x) => this.parent_canvas.ConvertNetworkToNode(this),
                                           (x) => this.parent_canvas != null);
            mi2.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_simple.png", UriKind.Relative)), Width = 16, Height = 16 };
            cm.Items.Add(mi2);

            return cm;
        }

        protected override void AddSourceOrSinkVis()
        {
            Rectangle rect_CONTOUR = new Rectangle();
            rect_CONTOUR.Width = this.Width;
            rect_CONTOUR.Height = this.Height;
            rect_CONTOUR.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            rect_CONTOUR.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            double radius = Math.Floor(this.Height * 0.25);
            rect_CONTOUR.RadiusX = radius;
            rect_CONTOUR.RadiusY = radius;

            rect_CONTOUR.Stroke = new SolidColorBrush(FlNwElementVisualization.NODE_COLOR_STROKE_INACTIVE);
            rect_CONTOUR.StrokeThickness = 1;
            rect_CONTOUR.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33ffffff"));
            rect_CONTOUR.IsHitTestVisible = false;

            this.Children.Add(rect_CONTOUR);

            base.AddSourceOrSinkVis();
        }

        #endregion

        #region METHODS: Resizing OVERRIDE

        public override void TransferSize(double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        {
            // do nothing
        }

        public override void TransferSize(List<double> _size, List<ParameterStructure.Geometry.GeomSizeTransferDef> _size_transfer_settings)
        {
            // do nothing
        }

        #endregion

    }
}
