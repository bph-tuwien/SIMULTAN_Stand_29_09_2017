using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Effects;
using System.Globalization;
using System.ComponentModel;

using ParameterStructure.Component;
using ParameterStructure.Parameter;

namespace ComponentBuilder.GraphUIE_FlNet
{
    #region ENUMS

    [Flags]
    public enum ElementVisHighlight
    {
        Empty = 0,                  // no component is associated with the underlying Flow Network Element
        Full = 1,                   // component is associated with it
        Manipulated = 2,            // during movement or rotation
        Picked = 4,                 // e.g. nodes picked for edge definition
        Highlighted = 8             // for showing the instances of a component
    }

    public enum NodePosInFlow
    {
        INTERIOR = 0,
        SOURCE = 1,
        SINK = 2
    }

    #endregion

    public class FlNwElementVisualization : Grid, INotifyPropertyChanged
    {
        #region STATIC

        internal static System.IFormatProvider NR_FORMATTER = new System.Globalization.NumberFormatInfo();

        // EMPTY ELEMENTS
        internal static Color NODE_COLOR_FILL_INACTIVE_1 = (Color)ColorConverter.ConvertFromString("#ff828282");
        internal static Color NODE_COLOR_FILL_INACTIVE_2 = (Color)ColorConverter.ConvertFromString("#ffc9c9c9");
        internal static Color NODE_COLOR_STROKE_INACTIVE = (Color)ColorConverter.ConvertFromString("#ff555555");
        internal static double NODE_STROKE_THICKNESS_INACTIVE = 1;

        // FULL ELEMENTS
        internal static Color NODE_COLOR_FILL_ACTIVE_1 = (Color)ColorConverter.ConvertFromString("#ff828282");
        internal static Color NODE_COLOR_FILL_ACTIVE_2 = (Color)ColorConverter.ConvertFromString("#ffffcd7f");
        internal static Color NODE_COLOR_FILL_ACTIVE_3 = (Color)ColorConverter.ConvertFromString("#ffffb135");
        internal static Color NODE_COLOR_FILL_ACTIVE_4 = (Color)ColorConverter.ConvertFromString("#ffff7e00");
        internal static Color NODE_COLOR_FILL_ACTIVE_4tr = (Color)ColorConverter.ConvertFromString("#33ff7e00");
        internal static Color NODE_COLOR_STROKE_ACTIVE = (Color)ColorConverter.ConvertFromString("#ff000000");
        internal static double NODE_STROKE_THICKNESS_ACTIVE = 2;

        // MANIPULATED ELEMENTS
        internal static double NODE_STROKE_THICKNESS_MANIPULATED = 3;
        internal static Color NODE_COLOR_STROKE_MANIPULATED = (Color)ColorConverter.ConvertFromString("#ffffffff");

        // PICKED ELEMENTS
        internal static Color NODE_COLOR_FILL_PICKED_1 = (Color)ColorConverter.ConvertFromString("#ff555555");
        internal static Color NODE_COLOR_STROKE_PICKED = (Color)ColorConverter.ConvertFromString("#ff000000");
        internal static double NODE_STROKE_THICKNESS_PICKED = 3;

        // general colors
        internal static Color NODE_FOREGROUND = (Color)ColorConverter.ConvertFromString("#ff333333");
        internal static Color NODE_FOREGROUND_1 = (Color)ColorConverter.ConvertFromString("#55333333");
        internal static Color NODE_BACKGROUND = (Color)ColorConverter.ConvertFromString("#22ffcd7f");
        internal static Color NODE_COLOR_ERR = (Color)ColorConverter.ConvertFromString("#ffff4500");
        internal static Color NODE_WHITE_TEXT = (Color)ColorConverter.ConvertFromString("#ffffffff");
        internal static Color NODE_HIGHLIGHT_TEXT = (Color)ColorConverter.ConvertFromString("#ff0000ff");

        // SIZES
        internal static double NODE_HEIGHT_DEFAULT = 40;
        internal static double NODE_HEIGHT_MAX = 60;
        internal static double NODE_WIDTH_DEFAULT = 140;
        internal static double NODE_BUFFER = 20;
        internal static double NODE_IMG_SIZE = 24;

        #endregion

        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region CLASS MEMBERS, PROPERTIES: General

        protected FlowNwGraph parent_canvas;

        protected double element_width;
        protected double element_height;
        protected double element_offset_hrzt;
        protected double element_offset_vert;
        protected Shape element_main;
        
        public Point Anchor { get; set; }
        
        protected Point position;
        public Point Position 
        { 
            get { return this.position; }
            protected set
            {
                this.position = value;
                this.RegisterPropertyChanged("Position");
            }
        }

        #endregion
        
        #region PROPERTIES: Display

        protected Color element_fill_color_1;
        protected Color element_fill_color_2;
        protected Color element_contour;
        protected double element_contour_thickness;

        private ElementVisHighlight vis_state;
        public ElementVisHighlight VisState
        {
            get { return this.vis_state; }
            set 
            { 
                this.vis_state = value;
                this.AdaptColorsToVisState();
            }
        }

        public virtual bool ElementLocked { get { return false; } }

        #endregion

        #region .CTOR

        public FlNwElementVisualization(FlowNwGraph _parent, double _width, double _height, double _offset_hrzt, double _offset_vert)
        {
            this.parent_canvas = _parent;
            this.element_offset_hrzt = _offset_hrzt;
            this.element_offset_vert = _offset_vert;

            this.element_width = _width;
            this.element_height = _height;
            this.Width = _width;
            this.Height = _height;

            this.element_fill_color_1 = FlNwElementVisualization.NODE_COLOR_FILL_ACTIVE_1;
            this.element_fill_color_2 = FlNwElementVisualization.NODE_COLOR_FILL_ACTIVE_2;
            this.element_contour = FlNwElementVisualization.NODE_COLOR_STROKE_ACTIVE;
            this.element_contour_thickness = FlNwElementVisualization.NODE_STROKE_THICKNESS_ACTIVE;

            this.Position = new Point(this.element_offset_hrzt, this.element_offset_vert);
            TranslateTransform transf = new TranslateTransform(this.element_offset_hrzt, this.element_offset_vert);
            this.RenderTransform = transf;

            this.Loaded += element_Loaded;

            // Add to Parent!
            if (this.parent_canvas != null)
                this.parent_canvas.AddChild(this, this.position, this.element_width, this.element_height);
        }

        #endregion

        #region METHODS: Display Update

        public virtual void UpdateContent()
        {
            this.RedefineGrid();
            this.PopulateGrid();
        }

        protected virtual void RedefineGrid()
        { }

        protected virtual void PopulateGrid()
        { }

        protected virtual string GetFlowInfo(ParameterStructure.Component.Component _c)
        {
            if (_c == null)
                return "[...]";

            string flow = "[";
            ParameterStructure.Parameter.Parameter p_V = _c.GetFirstParamByName(Parameter.RP_FLOW);
            if (p_V != null)
                flow += Parameter.ValueToString(p_V.ValueCurrent, "F3") + " " + p_V.Unit;

            Parameter p_dP = _c.GetFirstParamByName(Parameter.RP_PRESS_IN);
            Parameter p_dP2 = _c.GetFirstParamByName(Parameter.RP_PRESS_IN_MAIN);
            Parameter p_dP3 = _c.GetFirstParamByName(Parameter.RP_PRESS_IN_BRANCH);
            if (p_dP != null)
                flow += " " + Parameter.RP_PRESS_IN + ": " + Parameter.ValueToString(p_dP.ValueCurrent, "F2") + " " + p_dP.Unit;
            if (p_dP2 != null)
                flow += " " + Parameter.RP_PRESS_IN_MAIN + ": " + Parameter.ValueToString(p_dP2.ValueCurrent, "F2");
            if (p_dP3 != null)
                flow += " " + Parameter.RP_PRESS_IN_BRANCH + ": " + Parameter.ValueToString(p_dP3.ValueCurrent, "F2");

            flow += "]";

            return flow;
        }

        protected virtual string GetCrossSectionInfo(ParameterStructure.Component.Component _c)
        {
            if (_c == null)
                return "[]";

            string cs = "[";
            ParameterStructure.Parameter.Parameter p_B = _c.GetFirstParamByName(Parameter.RP_WIDTH);
            ParameterStructure.Parameter.Parameter p_H = _c.GetFirstParamByName(Parameter.RP_HEIGHT);
            ParameterStructure.Parameter.Parameter p_D = _c.GetFirstParamByName(Parameter.RP_DIAMETER);

            if (p_B != null && p_H != null && p_B.ValueCurrent > 0 && p_H.ValueCurrent > 0)
                cs += p_B.ValueCurrent.ToString("F0", FlNwElementVisualization.NR_FORMATTER) + " x "
                    + p_H.ValueCurrent.ToString("F0", FlNwElementVisualization.NR_FORMATTER);

            if (p_D != null && p_D.ValueCurrent > 0)
                cs += "Ø " + p_D.ValueCurrent.ToString("F0", FlNwElementVisualization.NR_FORMATTER);

            cs += "]";

            return cs;
        }

        #endregion

        #region METHODS: Transform

        public virtual void Translate(Vector _offset)
        {
            TranslateTransform transf = (TranslateTransform)this.RenderTransform;
            transf.X += _offset.X;
            transf.Y += _offset.Y;
            this.element_offset_hrzt += _offset.X;
            this.element_offset_vert += _offset.Y;
            this.RenderTransform = transf;
            this.Anchor += _offset;

            this.Position += _offset;
        }

        #endregion

        #region METHODS: Resizing

        public virtual void TransferSize(double _min_h, double _min_b, double _min_L, double _max_h, double _max_b, double _max_L)
        { }

        public virtual void TransferSize(List<double> _size, List<ParameterStructure.Geometry.GeomSizeTransferDef> _size_transfer_settings)
        { }

        public virtual double RetrieveSingleSizeValue(int _at_index, double _size, ParameterStructure.Geometry.GeomSizeTransferDef _transfer_setting)
        {
            return 0.0;
        }

        public virtual List<double> RetrieveSize()
        {
            return new List<double>();
        }

        public virtual List<ParameterStructure.Geometry.GeomSizeTransferDef> RetrieveSizeTransferSettings()
        {
            return new List<ParameterStructure.Geometry.GeomSizeTransferDef>();
        }

        public virtual CompositeCollection RetrieveParamToSelectForSizeTransfer()
        {
            return new CompositeCollection();
        }

        public virtual bool ContainsPath()
        {
            return false;
        }

        public virtual bool ContainsValidPath()
        {
            return false;
        }

        #endregion

        #region METHODS: Adapt to Highlighing Status

        protected virtual void AdaptColorsToVisState()
        {

            if (this.vis_state.HasFlag(ElementVisHighlight.Full))
            {
                this.element_fill_color_1 = FlNwElementVisualization.NODE_COLOR_FILL_ACTIVE_1;
                this.element_fill_color_2 = FlNwElementVisualization.NODE_COLOR_FILL_ACTIVE_2;
                this.element_contour = FlNwElementVisualization.NODE_COLOR_STROKE_ACTIVE;
                this.element_contour_thickness = FlNwElementVisualization.NODE_STROKE_THICKNESS_ACTIVE;
                if (this.vis_state.HasFlag(ElementVisHighlight.Highlighted))
                {
                    this.element_fill_color_1 = FlNwElementVisualization.NODE_COLOR_FILL_ACTIVE_3;
                    this.element_fill_color_2 = FlNwElementVisualization.NODE_COLOR_FILL_ACTIVE_4;
                }
            }
            else
            {
                this.element_fill_color_1 = FlNwElementVisualization.NODE_COLOR_FILL_INACTIVE_2;
                this.element_fill_color_2 = FlNwElementVisualization.NODE_COLOR_FILL_INACTIVE_1;
                this.element_contour = FlNwElementVisualization.NODE_COLOR_STROKE_INACTIVE;
                this.element_contour_thickness = FlNwElementVisualization.NODE_STROKE_THICKNESS_INACTIVE;
            }

            if (this.vis_state.HasFlag(ElementVisHighlight.Manipulated))
            {
                Color tmp = this.element_fill_color_1;
                this.element_fill_color_1 = this.element_fill_color_2;
                this.element_fill_color_2 = tmp;
                this.element_contour = FlNwElementVisualization.NODE_COLOR_STROKE_MANIPULATED;
                this.element_contour_thickness = FlNwElementVisualization.NODE_STROKE_THICKNESS_MANIPULATED;
            }

            if (this.vis_state.HasFlag(ElementVisHighlight.Picked))
            {
                this.element_fill_color_1 = FlNwElementVisualization.NODE_COLOR_FILL_PICKED_1;
                this.element_contour = (this.ElementLocked) ? FlNwElementVisualization.NODE_COLOR_ERR : FlNwElementVisualization.NODE_COLOR_STROKE_PICKED;
                this.element_contour_thickness = FlNwElementVisualization.NODE_STROKE_THICKNESS_PICKED;
            }

            // apply to node
            this.ApplyColorChangeToNode();
        }

        protected void ApplyColorChangeToNode()
        {
            this.element_main.Fill = new LinearGradientBrush(this.element_fill_color_1, this.element_fill_color_2, new Point(0.5, 0), new Point(0.5, 1));
            this.element_main.Stroke = new SolidColorBrush(this.element_contour);
            this.element_main.StrokeThickness = this.element_contour_thickness;

        }

        #endregion

        #region EVENT HANDLERS

        private void element_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateContent();
        }

        #endregion
    }
}
