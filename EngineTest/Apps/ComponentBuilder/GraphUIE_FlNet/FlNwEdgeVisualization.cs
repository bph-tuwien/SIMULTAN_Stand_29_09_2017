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
using System.Windows.Documents;

using ComponentBuilder.WinUtils;
using ParameterStructure.Component;
using ParameterStructure.Parameter;

namespace ComponentBuilder.GraphUIE_FlNet
{
    public class FlNwEdgeVisualization : FlNwElementVisualization
    {
        #region PROPERTIES : Display
        public override bool ElementLocked { get { return (this.fn_edge != null && fn_edge.IsLocked) || (this.fn_edge != null && this.fn_edge.Content != null && this.fn_edge.Content.IsLocked); } }

        #endregion

        #region PROPERTIES: Start, End

        private FlNetEdge fn_edge;
        public FlNetEdge FN_Edge { get { return this.fn_edge; } }


        private FlNwNodeVisualization start_vis;
        public FlNwNodeVisualization StartVis 
        {
            get { return this.start_vis; }
            private set
            {
                if (this.start_vis != null)
                    this.start_vis.PropertyChanged -= node_PropertyChanged;
                this.start_vis = value;
                if (this.start_vis != null)
                    this.start_vis.PropertyChanged += node_PropertyChanged;
            }
        }

        private FlNwNodeVisualization end_vis;
        public FlNwNodeVisualization EndVis
        {
            get { return this.end_vis; }
            private set
            {
                if (this.end_vis != null)
                    this.end_vis.PropertyChanged -= node_PropertyChanged;
                this.end_vis = value;
                if (this.end_vis != null)
                    this.end_vis.PropertyChanged += node_PropertyChanged;
            }
        }

        #endregion

        #region CLASS MEMBERS

        private Point pointing_from;
        private Point pointing_to;
        private Thickness arrow_margin;
        private bool hrzt_orientation;

        #endregion

        #region .CTOR

        public FlNwEdgeVisualization(FlowNwGraph _parent, FlNetEdge _fn_edge, FlNwNodeVisualization _start_vis, FlNwNodeVisualization _end_vis)
            : base(_parent, FlNwElementVisualization.NODE_WIDTH_DEFAULT, FlNwElementVisualization.NODE_HEIGHT_DEFAULT,
                    _fn_edge.Start.Position.X + FlNwElementVisualization.NODE_WIDTH_DEFAULT * 0.5,
                    _fn_edge.Start.Position.Y + FlNwElementVisualization.NODE_HEIGHT_DEFAULT * 0.5)
        {
            this.fn_edge = _fn_edge;
            this.fn_edge.PropertyChanged += fn_edge_PropertyChanged;

            this.UpdateGeometry();

            this.StartVis = _start_vis;
            this.EndVis = _end_vis;
        }

        #endregion

        #region METHODS: Display Update

        internal void UpdateVisual()
        {
            this.UpdateGeometry();
            this.UpdateContent();
        }

        protected override void RedefineGrid()
        {
            // reset grid
            this.Children.Clear();
            // no comlumns or rows to clear

            //// DEBUG
            //this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#550000ff"));
            //this.IsHitTestVisible = false; // TMP!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }

        protected override void PopulateGrid()
        {
            // set geometric frame
            Vector direction = this.pointing_to - this.pointing_from;
            if (Math.Abs(direction.X) < 0.1 && Math.Abs(direction.Y) < 0.1)
                return;

            Vector direction_n = new Vector(-direction.Y, direction.X);
            direction_n.Normalize();
            double rel_arrow_head = 0.4;
            double rel_arrow_stem = 0.4;

            // MAIN NODE
            Polygon poly = new Polygon();

            PointCollection poly_points = new PointCollection();
            Point p0 = new Point(0, 0) + direction_n * FlNwElementVisualization.NODE_HEIGHT_DEFAULT * rel_arrow_stem * 0.5;
            Point p1 = p0 + direction * (1 - rel_arrow_head);
            Point p2 = p1 + direction_n * FlNwElementVisualization.NODE_HEIGHT_DEFAULT * (0.5 - rel_arrow_stem * 0.5);
            Point p3 = new Point(0, 0) + direction;
            Point p4 = p2 - direction_n * FlNwElementVisualization.NODE_HEIGHT_DEFAULT;
            Point p5 = p1 - direction_n * FlNwElementVisualization.NODE_HEIGHT_DEFAULT * rel_arrow_stem;
            Point p6 = new Point(0, 0) - direction_n * FlNwElementVisualization.NODE_HEIGHT_DEFAULT * rel_arrow_stem * 0.5;
            poly_points.Add(p0);
            poly_points.Add(p1);
            poly_points.Add(p2);
            poly_points.Add(p3);
            poly_points.Add(p4);
            poly_points.Add(p5);
            poly_points.Add(p6);

            poly.Points = poly_points;

            poly.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            poly.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            poly.Margin = this.arrow_margin;
            poly.ContextMenu = this.BuildContextMenu();

            this.element_main = poly;
            this.Children.Add(poly);

            // Transform for the MAIN TEXT
            RotateTransform rotation = new RotateTransform(90);
            TranslateTransform translation_y = new TranslateTransform(0, -FlNwElementVisualization.NODE_HEIGHT_DEFAULT);
            TranslateTransform translation_x = new TranslateTransform(5, 0);

            TransformGroup tr_hrzt = new TransformGroup();
            tr_hrzt.Children.Add(translation_y);

            TransformGroup tr_vert = new TransformGroup();
            tr_vert.Children.Add(rotation);
            tr_vert.Children.Add(translation_x);

            if (this.fn_edge != null && this.fn_edge.Content != null)
            {
                // mark if the visualized instance is realised (i.e. placed in geometry)
                if (this.fn_edge.GetBoundInstanceRealizedStatus())
                {
                    DropShadowEffect effect = new DropShadowEffect();
                    effect.BlurRadius = 3;
                    effect.Opacity = 0.5;
                    effect.Color = Colors.Blue;
                    this.element_main.Effect = effect;
                }
                else
                    this.element_main.Effect = null;
            }            

            // MAIN TEXT
            if (this.fn_edge != null && this.fn_edge.Content != null)
            {
                Canvas canv = new Canvas();
                canv.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                canv.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                canv.Width = FlNwElementVisualization.NODE_WIDTH_DEFAULT;
                canv.Height = FlNwElementVisualization.NODE_HEIGHT_DEFAULT;
                canv.Margin = this.arrow_margin;
                canv.Background = new SolidColorBrush(FlNwElementVisualization.NODE_BACKGROUND);
                canv.IsHitTestVisible = false;
                canv.RenderTransformOrigin = new Point(1, 1);
                if (this.hrzt_orientation)
                    canv.RenderTransform = tr_hrzt;
                else
                    canv.RenderTransform = tr_vert;

                Rectangle bgr = new Rectangle();
                bgr.Stroke = new SolidColorBrush(FlNwElementVisualization.NODE_FOREGROUND_1);
                bgr.StrokeThickness = 1;
                bgr.Width = FlNwElementVisualization.NODE_WIDTH_DEFAULT;
                bgr.Height = FlNwElementVisualization.NODE_HEIGHT_DEFAULT;
                bgr.IsHitTestVisible = false;
                canv.Children.Add(bgr);

                TextBlock tb_slot = new TextBlock();
                tb_slot.Padding = new Thickness(5);
                tb_slot.Text = this.fn_edge.Content.CurrentSlot + ": " + this.fn_edge.Content.Name;
                tb_slot.FontSize = 10;
                tb_slot.FontWeight = FontWeights.Bold;
                tb_slot.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_FOREGROUND);
                tb_slot.IsHitTestVisible = false;
                canv.Children.Add(tb_slot);

                //TextBlock tb_flow = new TextBlock();
                //tb_flow.Padding = new Thickness(5);
                //tb_flow.Text = this.GetFlowInfo(this.fn_edge.Content); // tobe replaced 
                //tb_flow.FontSize = 10;
                //tb_flow.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_FOREGROUND);
                //tb_flow.IsHitTestVisible = false;
                //tb_flow.RenderTransform = new TranslateTransform(0, 18);
                //canv.Children.Add(tb_flow);

                TextBlock tb_cs = new TextBlock();
                tb_cs.Padding = new Thickness(5);
                tb_cs.Text = this.fn_edge.GetContentInstanceSizeInfo(); // old: this.GetCrossSectionInfo(this.fn_edge.Content);
                tb_cs.FontSize = 10;
                tb_cs.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_FOREGROUND);
                tb_cs.IsHitTestVisible = false;
                tb_cs.RenderTransform = new TranslateTransform(0, 18); //36
                canv.Children.Add(tb_cs);

                // for displaying param values according to SUFFIX
                TextBlock tb_pS = new TextBlock();
                tb_pS.Padding = new Thickness(5);
                tb_pS.Text = (this.fn_edge.ParamValueToDisplay == null) ? "---" : this.fn_edge.ParamValueToDisplay;
                tb_pS.FontSize = 12;
                tb_pS.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_WHITE_TEXT);
                tb_pS.IsHitTestVisible = false;
                tb_pS.RenderTransform = new TranslateTransform(FlNwElementVisualization.NODE_WIDTH_DEFAULT - 30, 18);
                canv.Children.Add(tb_pS);

                // SYMBOL
                if (this.fn_edge.Content.SymbolImage != null)
                {
                    Image symb = new Image();
                    symb.Source = this.fn_edge.Content.SymbolImage;
                    symb.Margin = new Thickness(0, 0, 5, 0);
                    symb.Width = FlNwElementVisualization.NODE_IMG_SIZE;
                    symb.Height = FlNwElementVisualization.NODE_IMG_SIZE;
                    symb.IsHitTestVisible = false;
                    symb.RenderTransform = new TranslateTransform(bgr.Width - 28, bgr.Height * 0.5 - 12);
                    canv.Children.Add(symb);
                }

                // bound instance id
                TextBlock tb_IID = new TextBlock();
                tb_IID.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                tb_IID.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                tb_IID.Padding = new Thickness(5, 5, 5, 0);
                Run r1 = new Run(this.fn_edge.GetBoundInstanceId().ToString());
                r1.FontWeight = FontWeights.Bold;
                r1.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_HIGHLIGHT_TEXT);
                Run r2 = new Run(" in E" + this.fn_edge.ID.ToString());
                r2.FontWeight = FontWeights.Normal;
                r2.FontStyle = FontStyles.Italic;
                r2.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_HIGHLIGHT_TEXT);
                tb_IID.Inlines.Add(r1);
                tb_IID.Inlines.Add(r2);
                tb_IID.FontSize = 10;
                tb_IID.RenderTransform = new TranslateTransform(0, -20);
                tb_IID.IsHitTestVisible = false;
                canv.Children.Add(tb_IID);

                this.Children.Add(canv);               
            }

            this.VisState = (this.fn_edge != null && this.fn_edge.Content != null) ? ElementVisHighlight.Full : ElementVisHighlight.Empty;
        }

        protected ContextMenu BuildContextMenu()
        {
            ContextMenu cm = new ContextMenu();
            cm.UseLayoutRounding = true;

            MenuItem mi1 = new MenuItem();
            mi1.Header = "Komponente in Liste anzeigen";
            mi1.Command = new RelayCommand((x) => this.parent_canvas.SelectContent(this.FN_Edge.Content),
                                           (x) => this.parent_canvas != null && this.parent_canvas.CompFactory != null && this.FN_Edge != null && this.FN_Edge.Content != null);
            mi1.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_selected.png", UriKind.Relative)), Width = 16, Height = 16 };
            cm.Items.Add(mi1);

            return cm;
        }

        #endregion

        #region METHODS: Geometry Update

        protected void UpdateGeometry()
        {
            bool start_before_end = (this.fn_edge.Start.Position.X < this.fn_edge.End.Position.X);
            bool start_above_end = (this.fn_edge.Start.Position.Y < this.fn_edge.End.Position.Y);
            this.hrzt_orientation = Math.Abs(this.fn_edge.End.Position.X - this.fn_edge.Start.Position.X) > Math.Abs(this.fn_edge.End.Position.Y - this.fn_edge.Start.Position.Y);

            // size
            this.element_height = Math.Abs(this.fn_edge.End.Position.Y - this.fn_edge.Start.Position.Y) + FlNwElementVisualization.NODE_HEIGHT_DEFAULT;
            this.element_width = Math.Abs(this.fn_edge.End.Position.X - this.fn_edge.Start.Position.X) + FlNwElementVisualization.NODE_WIDTH_DEFAULT;
            this.Height = this.element_height;
            this.Width = this.element_width;

            // position
            this.element_offset_hrzt = Math.Min(this.fn_edge.Start.Position.X, this.fn_edge.End.Position.X);
            this.element_offset_vert = Math.Min(this.fn_edge.Start.Position.Y, this.fn_edge.End.Position.Y);
            this.Position = new Point(this.element_offset_hrzt, this.element_offset_vert);
            TranslateTransform transf = new TranslateTransform(this.element_offset_hrzt, this.element_offset_vert);
            this.RenderTransform = transf;

            // arrow orientation
            this.pointing_from = new Point(this.fn_edge.Start.Position.X - this.Position.X, this.fn_edge.Start.Position.Y - this.Position.Y);
            this.pointing_to   = new Point(this.fn_edge.End.Position.X - this.Position.X, this.fn_edge.End.Position.Y - this.Position.Y);
            Vector dist = this.pointing_to - this.pointing_from;

            // margin
            this.arrow_margin = new Thickness(0, 0, Math.Min(FlNwElementVisualization.NODE_WIDTH_DEFAULT * 0.5, Math.Abs(dist.X) * 0.5 + FlNwElementVisualization.NODE_HEIGHT_DEFAULT * 1.25),
                                                    Math.Min(FlNwElementVisualization.NODE_HEIGHT_DEFAULT * 0.5, Math.Abs(dist.Y) * 0.5));
            
            // final adjustment
            if (this.hrzt_orientation)
            {
                this.arrow_margin.Right += FlNwElementVisualization.NODE_WIDTH_DEFAULT * 0.5;
                if (start_before_end)
                    this.pointing_from += new Vector(FlNwElementVisualization.NODE_WIDTH_DEFAULT, 0);
                else
                    this.pointing_from += new Vector(-FlNwElementVisualization.NODE_WIDTH_DEFAULT, 0);
            }
            else
            {
                this.arrow_margin.Bottom += FlNwElementVisualization.NODE_HEIGHT_DEFAULT * 0.5;
                if (start_above_end)
                    this.pointing_from += new Vector(0, FlNwElementVisualization.NODE_HEIGHT_DEFAULT);
                else
                    this.pointing_from += new Vector(0, -FlNwElementVisualization.NODE_HEIGHT_DEFAULT);
            }

        }

        #endregion

        #region METHODS: Before Deleting

        public void Detach()
        {
            if (this.StartVis != null)
                this.StartVis.PropertyChanged -= node_PropertyChanged;
            if (this.EndVis != null)
                this.EndVis.PropertyChanged -= node_PropertyChanged;
        }

        #endregion

        #region METHODS: Redirection

        public bool IsCloserToStart(Point _p)
        {
            Point rel_to = this.element_main.TranslatePoint(this.pointing_to, this);
            Point rel_from = this.element_main.TranslatePoint(this.pointing_from, this);

            double dist_to_start = (rel_from - _p).Length;
            double dist_to_end = (rel_to - _p).Length;

            return (dist_to_start < dist_to_end);
        }

        public void RedirectEdge(FlNwNodeVisualization _to_node, bool _rerout_start, FlowNetwork _owner)
        {
            if (_to_node == null || _owner == null) return;

            // perform redirection in the data
            FlNetEdge edge_data = this.FN_Edge;
            FlNetNode node_data = _to_node.FN_Node;
            bool success = false;
            if (edge_data != null && node_data != null)
                success = _owner.RedirectEdge(edge_data, _rerout_start, node_data);

            // perform redirection in the visualizations
            if (success)
            {
                if (_rerout_start)
                    this.StartVis = _to_node;
                else
                    this.EndVis = _to_node;
            }
        }


        public void RedirectAfterConversion(FlNwNodeVisualization _to_node, bool _rerout_start)
        {
            // old node was deleted !
            if (_rerout_start)
                this.StartVis = _to_node;
            else
                this.EndVis = _to_node;
        }

        #endregion

        #region METHODS: Resizing OVERRIDE

        public override void TransferSize(double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        {
            if (this.FN_Edge == null) return;
            this.FN_Edge.UpdateContentInstanceSize(_min_h, _min_b, _min_L, _max_h, _max_b, _max_L);
            this.UpdateContent();
        }

        public override void TransferSize(List<double> _size, List<ParameterStructure.Geometry.GeomSizeTransferDef> _size_transfer_settings)
        {
            if (this.FN_Edge == null) return;
            this.FN_Edge.UpdateContentInstanceSizeAndSettings(_size, _size_transfer_settings);
            this.UpdateContent();
        }

        public override double RetrieveSingleSizeValue(int _at_index, double _size, ParameterStructure.Geometry.GeomSizeTransferDef _transfer_setting)
        {
            if (this.FN_Edge == null) return 0.0;
            return this.FN_Edge.UpdateSingleSizeValue(_at_index, _size, _transfer_setting);
        }

        public override List<double> RetrieveSize()
        {
            List<double> sizes = new List<double>();
            if (this.FN_Edge == null) return sizes;
            if(this.FN_Edge.Content == null) return sizes;

            return this.FN_Edge.GetInstanceSize();
        }

        public override List<ParameterStructure.Geometry.GeomSizeTransferDef> RetrieveSizeTransferSettings()
        {
            List<ParameterStructure.Geometry.GeomSizeTransferDef> settings = new List<ParameterStructure.Geometry.GeomSizeTransferDef>();
            if (this.FN_Edge == null) return settings;
            if (this.FN_Edge.Content == null) return settings;

            return this.FN_Edge.GetInstanceSizeTransferSettings();
        }

        public override System.Windows.Data.CompositeCollection RetrieveParamToSelectForSizeTransfer()
        {
            if (this.fn_edge == null || this.fn_edge.Content == null)
                return new System.Windows.Data.CompositeCollection();
            else
                return this.fn_edge.Content.ParamChildrenNonAutomatic;
        }

        public override bool ContainsPath()
        {
            if (this.fn_edge == null) return false;
            if (this.fn_edge.Content == null) return false;

            return this.fn_edge.InstanceHasPath();
        }

        public override bool ContainsValidPath()
        {
            if (this.fn_edge == null) return false;
            if (this.fn_edge.Content == null) return false;

            return this.fn_edge.InstanceHasValidPath();
        }

        #endregion

        #region EVENT HANDLERS

        private void fn_edge_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            FlNetEdge edge = sender as FlNetEdge;
            if (edge == null || e == null) return;

            if (e.PropertyName == "Start" || e.PropertyName == "End")
            {
                this.UpdateGeometry();
                this.UpdateContent();
            }
            else if (e.PropertyName == "Content" || e.PropertyName == "ParamValueToDisplay")
            {
                this.UpdateContent();
            }
        }

        private void node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            FlNwNodeVisualization node = sender as FlNwNodeVisualization;
            if (node == null || e == null) return;

            if (e.PropertyName == "Position")
            {
                this.UpdateGeometry();
                this.UpdateContent();
            }            
        }

        #endregion
    }
}
