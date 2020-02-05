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
    public class FlNwNodeVisualization : FlNwElementVisualization
    {
        #region PROPERTIES : Display
        public override bool ElementLocked { get{ return (this.fn_node != null && fn_node.IsLocked) || (this.fn_node != null && this.fn_node.Content != null && this.fn_node.Content.IsLocked); } }

        public NodePosInFlow PosInFlow { get; set; }

        #endregion

        #region CLASS MEMBERS

        protected FlNetNode fn_node;
        public FlNetNode FN_Node { get { return this.fn_node; } }

        #endregion

        #region .CTOR

        // assumes that _fn_node is not NULL
        public FlNwNodeVisualization(FlowNwGraph _parent, FlNetNode _fn_node, NodePosInFlow _pos_in_flow)
            :base(_parent, FlNwElementVisualization.NODE_WIDTH_DEFAULT, FlNwElementVisualization.NODE_HEIGHT_DEFAULT,
                    _fn_node.Position.X, _fn_node.Position.Y)
        {
            this.fn_node = _fn_node;
            this.fn_node.PropertyChanged += node_PropertyChanged;
            this.PosInFlow = _pos_in_flow;
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

            // SOURCE or SINK sigifier
            if (this.PosInFlow != NodePosInFlow.INTERIOR)
                this.AddSourceOrSinkVis();

            if (this.fn_node != null && this.fn_node.Content != null)
            {
                // mark if the visualized instance is realised (i.e. placed in geometry)
                if (this.fn_node.GetBoundInstanceRealizedStatus())
                {
                    DropShadowEffect effect = new DropShadowEffect();
                    effect.BlurRadius = 3;
                    effect.Opacity = 0.5;
                    effect.Color = Colors.Blue;
                    this.element_main.Effect = effect;
                }
                else
                    this.element_main.Effect = null;

                // MAIN TEXT
                TextBlock tb_slot = new TextBlock();
                tb_slot.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                tb_slot.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                tb_slot.Padding = new Thickness(5, 5, 5, 0);
                tb_slot.Text = this.fn_node.Content.CurrentSlot + ": " + this.fn_node.Content.Name;
                tb_slot.FontSize = 10;
                tb_slot.FontWeight = FontWeights.Bold;
                tb_slot.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_FOREGROUND);
                tb_slot.IsHitTestVisible = false;

                this.Children.Add(tb_slot);

                TextBlock tb = new TextBlock();
                tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                tb.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                tb.Padding = new Thickness(5);
                tb.Text = this.fn_node.GetContentInstanceSizeInfo(); // old: this.GetFlowInfo(this.fn_node.Content);
                tb.FontSize = 10;
                tb.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_FOREGROUND);
                tb.IsHitTestVisible = false;

                this.Children.Add(tb);

                // for displaying param values according to SUFFIX
                TextBlock tb_pS = new TextBlock();
                tb_pS.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                tb_pS.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                tb_pS.Margin = new Thickness(0, 0, 5, 0);
                tb_pS.Padding = new Thickness(5);
                tb_pS.Text = (this.fn_node.ParamValueToDisplay == null) ? "---" : this.fn_node.ParamValueToDisplay;
                tb_pS.FontSize = 12;
                tb_pS.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_WHITE_TEXT);
                tb_pS.IsHitTestVisible = false;

                this.Children.Add(tb_pS);

                // SYMBOL
                if (this.fn_node.Content.SymbolImage != null)
                {
                    Image symb = new Image();
                    symb.Source = this.fn_node.Content.SymbolImage;
                    symb.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    symb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    symb.Margin = new Thickness(0, 0, 5, 0);
                    symb.Width = FlNwElementVisualization.NODE_IMG_SIZE;
                    symb.Height = FlNwElementVisualization.NODE_IMG_SIZE;
                    symb.IsHitTestVisible = false;

                    this.Children.Add(symb);
                }

                // bound instance id
                TextBlock tb_IID = new TextBlock();
                tb_IID.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                tb_IID.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                tb_IID.Padding = new Thickness(5, 5, 5, 0);
                tb_IID.Margin = new Thickness(0, -20, 0, 0);
                Run r1 = new Run(this.fn_node.GetBoundInstanceId().ToString());
                r1.FontWeight = FontWeights.Bold;
                r1.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_HIGHLIGHT_TEXT);
                Run r2 = new Run(" in N" + this.fn_node.ID.ToString());
                r2.FontWeight = FontWeights.Normal;
                r2.FontStyle = FontStyles.Italic;
                r2.Foreground = new SolidColorBrush(FlNwElementVisualization.NODE_HIGHLIGHT_TEXT);
                tb_IID.Inlines.Add(r1);
                tb_IID.Inlines.Add(r2);
                tb_IID.FontSize = 10;
                tb_IID.IsHitTestVisible = false;

                this.Children.Add(tb_IID);
            }

            this.VisState = (this.fn_node != null && this.fn_node.Content != null) ? ElementVisHighlight.Full : ElementVisHighlight.Empty;
        }

        protected virtual ContextMenu BuildContextMenu()
        {
            ContextMenu cm = new ContextMenu();
            cm.UseLayoutRounding = true;

            MenuItem mi1 = new MenuItem();
            mi1.Header = "Komponente in Liste anzeigen";
            mi1.Command = new RelayCommand((x) => this.parent_canvas.SelectContent(this.FN_Node.Content),
                                           (x) => this.parent_canvas != null && this.parent_canvas.CompFactory != null && this.FN_Node != null && this.FN_Node.Content != null);
            mi1.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_selected.png", UriKind.Relative)), Width = 16, Height = 16 };
            cm.Items.Add(mi1);

            MenuItem mi2 = new MenuItem();
            mi2.Header = "In Netzwerk umwandeln";
            mi2.Command = new RelayCommand((x) => this.parent_canvas.ConvertNodeToNetwork(this),
                                           (x) => this.parent_canvas != null);
            mi2.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_complex.png", UriKind.Relative)), Width = 16, Height = 16 };
            cm.Items.Add(mi2);

            return cm;
        }

        protected virtual void AddSourceOrSinkVis()
        {
            Rectangle rect_S = new Rectangle();
            rect_S.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            rect_S.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            rect_S.Stroke = new SolidColorBrush(FlNwElementVisualization.NODE_COLOR_STROKE_INACTIVE);
            rect_S.IsHitTestVisible = false;

            if (this.PosInFlow == NodePosInFlow.SOURCE)
            {
                rect_S.Width = this.Width - 2;
                rect_S.Height = this.Height - 2;
                rect_S.StrokeThickness = 3;
            }
            else if (this.PosInFlow == NodePosInFlow.SINK)
            {
                rect_S.Width = this.Width - 10;
                rect_S.Height = this.Height - 10;
                rect_S.StrokeThickness = 1;
            }

            double radius_S = Math.Floor(rect_S.Height * 0.25);
            rect_S.RadiusX = radius_S;
            rect_S.RadiusY = radius_S;

            this.Children.Add(rect_S);
        }

        #endregion

        #region METHODS: Transform

        public override void Translate(Vector _offset)
        {
            base.Translate(_offset);
            if (this.fn_node != null)
                this.fn_node.Position = this.position;
        }

        #endregion

        #region METHODS: Resizing OVERRIDE

        public override void TransferSize(double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        {
            if (this.FN_Node == null) return;
            this.FN_Node.UpdateContentInstanceSize(_min_h, _min_b, _min_L, _max_h, _max_b, _max_L);
            this.UpdateContent();
        }

        public override void TransferSize(List<double> _size, List<ParameterStructure.Geometry.GeomSizeTransferDef> _size_transfer_settings)
        {
            if (this.FN_Node == null) return;
            this.FN_Node.UpdateContentInstanceSizeAndSettings(_size, _size_transfer_settings);
            this.UpdateContent();
        }

        public override double RetrieveSingleSizeValue(int _at_index, double _size, ParameterStructure.Geometry.GeomSizeTransferDef _transfer_setting)
        {
            if (this.FN_Node == null) return 0.0;
            return this.FN_Node.UpdateSingleSizeValue(_at_index, _size, _transfer_setting);
        }

        public override List<double> RetrieveSize()
        {
            List<double> sizes = new List<double>();
            if (this.FN_Node == null) return sizes;
            if (this.FN_Node.Content == null) return sizes;

            return this.FN_Node.GetInstanceSize();
        }

        public override List<ParameterStructure.Geometry.GeomSizeTransferDef> RetrieveSizeTransferSettings()
        {
            List<ParameterStructure.Geometry.GeomSizeTransferDef> settings = new List<ParameterStructure.Geometry.GeomSizeTransferDef>();
            if (this.FN_Node == null) return settings;
            if (this.FN_Node.Content == null) return settings;

            return this.FN_Node.GetInstanceSizeTransferSettings();
        }

        public override System.Windows.Data.CompositeCollection RetrieveParamToSelectForSizeTransfer()
        {
            if (this.fn_node == null || this.fn_node.Content == null)
                return new System.Windows.Data.CompositeCollection();
            else
                return this.fn_node.Content.ParamChildrenNonAutomatic;
        }

        #endregion

        #region EVENT HANDLERS

        private void node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            FlNetNode node = sender as FlNetNode;
            if (node == null || e == null) return;

            if (e.PropertyName == "Content" || e.PropertyName == "ParamValueToDisplay")
            {
                this.UpdateContent();
            }
        }

        #endregion
    }
}
