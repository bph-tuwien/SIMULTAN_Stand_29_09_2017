using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Effects;
using System.Globalization;
using System.ComponentModel;

using ParameterStructure.Component;

namespace ComponentBuilder.GraphUIE
{
    #region ENUMS

    [Flags]
    public enum NodeVisHighlight
    {
        Inactive = 0,           // when a group is isolated for viewing, but this node does not belong to it
        Active = 1,             // neutral state (node can be normally manipulated)
        Selected = 2,           // selected 
        Manipulated = 4,        // during translation by the user
        Highlighted = 8         // when found by any search function
    }

    public enum NodeVisExpanded
    {
        Collapsed,
        ExpandedOneLevel,
        ExpandedAllLevels
    }

    #endregion

    #region HELPER CLASSES

    public class BoundingBox : INotifyPropertyChanged
    {
        protected static readonly IFormatProvider FORMATTER = new NumberFormatInfo();

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

        protected Point upper_left;
        public Point UpperLeft 
        {
            get { return this.upper_left; }
            set
            {
                this.upper_left = value;
                this.RegisterPropertyChanged("UpperLeft");
            }
        }

        protected Point lower_right;
        public Point LowerRight 
        {
            get { return this.lower_right; }
            set
            {
                this.lower_right = value;
                this.RegisterPropertyChanged("LowerRight");
            }
        }

        public Vector Size
        {
            get { return this.LowerRight - this.UpperLeft; }
        }

        private Vector parent_translation;
        public Vector ParentTranslation
        {
            get { return this.parent_translation; }
            set 
            {
                this.parent_translation = value;
                this.RegisterPropertyChanged("ParentTranslation");
            }
        }

        public void SilentTranslation(Vector _offset, bool _add)
        {
            if (_add)
                this.parent_translation += _offset;
            else
                this.parent_translation = _offset;
        }

        public override string ToString()
        {
            return "UL (" + this.UpperLeft.X.ToString("F2", FORMATTER)  + " " + this.UpperLeft.Y.ToString("F2", FORMATTER)  + ")\t" +
                   "LR (" + this.LowerRight.X.ToString("F2", FORMATTER) + " " + this.LowerRight.Y.ToString("F2", FORMATTER) + ")\t" +
                   "tr [" + this.ParentTranslation.X.ToString("F2", FORMATTER) + " " + this.ParentTranslation.Y.ToString("F2", FORMATTER) + "]";
        }

    }

    #endregion

    class NodeVisualization : Grid, INotifyPropertyChanged
    {
        #region STATIC NODE COLORS

        // INACTIVE
        protected static Color NODE_COLOR_FILL_INACTIVE_1 = (Color)ColorConverter.ConvertFromString("#ff828282");
        protected static Color NODE_COLOR_FILL_INACTIVE_2 = (Color)ColorConverter.ConvertFromString("#ffc9c9c9");
        protected static Color NODE_COLOR_STROKE_INACTIVE = (Color)ColorConverter.ConvertFromString("#ff555555");
        protected static double NODE_STROKE_THICKNESS_INACTIVE = 0;

        // ACTIVE only
        protected static Color NODE_COLOR_FILL_ACTIVE_1 = (Color)ColorConverter.ConvertFromString("#ff828282");
        protected static Color NODE_COLOR_FILL_ACTIVE_2 = (Color)ColorConverter.ConvertFromString("#ffffcd7f");
        protected static Color NODE_COLOR_FILL_ACTIVE_3 = (Color)ColorConverter.ConvertFromString("#ffffb135");
        protected static Color NODE_COLOR_FILL_ACTIVE_4 = (Color)ColorConverter.ConvertFromString("#ffff7e00"); // #ffff9c00, #ffb17500
        protected static Color NODE_COLOR_STROKE_ACTIVE = (Color)ColorConverter.ConvertFromString("#ff000000");
        protected static double NODE_STROKE_THICKNESS_ACTIVE = 1;

        // MANIPULATED only
        protected static Color NODE_COLOR_STROKE_MANIPULATED = (Color)ColorConverter.ConvertFromString("#ffffaa00");
        protected static double NODE_STROKE_THICKNESS_MANIPULATED = 2;

        // HIGHLIGHTED only
        protected static Color NODE_COLOR_FILL_HIGHLIGHT_1 = (Color)ColorConverter.ConvertFromString("#ff00ffff");
        protected static Color NODE_COLOR_FILL_HIGHLIGHT_2 = (Color)ColorConverter.ConvertFromString("#ff0055ff");
        
        // SELECTED any
        protected static Color NODE_COLOR_SPECULAR = (Color)ColorConverter.ConvertFromString("#ffffffff");
        
        // functions
        protected static Color NODE_COLOR_YES = (Color)ColorConverter.ConvertFromString("#ffffaa00");
        protected static Color NODE_COLOR_NO = (Color)ColorConverter.ConvertFromString("#ff555555");
        protected static Color NODE_COLOR_MAYBE = (Color)ColorConverter.ConvertFromString("#ff00aaff");

        // text
        protected static Color NODE_FOREGROUND = (Color)ColorConverter.ConvertFromString("#ff333333");

        // misc
        protected static Color NODE_COLOR_BB = (Color)ColorConverter.ConvertFromString("#55ffffff");
        protected static Color NODE_COLOR_CALC_IN = (Color)ColorConverter.ConvertFromString("#ff0000ff");
        protected static Color NODE_COLOR_CALC_RET = (Color)ColorConverter.ConvertFromString("#ffb17500");

        #endregion

        #region STATIC

        protected static readonly IFormatProvider FORMATTER = new NumberFormatInfo();

        protected static double NODE_HEIGHT_DEFAULT = 40;
        internal static double NODE_WIDTH_DEFAULT = 140;

        protected static double NODE_HEIGHT_SMALL = 26;
        protected static double NODE_WIDTH_SWITCHES = 10;
        protected static double NODE_WIDTH_MARKERS = 8;
        protected static double NODE_WIDTH_MARKER_TOOLTIP = 26;
        protected static double NODE_RADIUS = 5;
        protected static double NODE_RADIUS_SMALL = 3;
        protected static double NODE_PADDING = 5;
        protected static double NODE_WIDTH_CONNECTION = 60;

        protected static double NODE_IMG_SIZE = 24;
        protected static double NODE_CONNECTION_ARROW_SIZE = 10; 

        internal static double NODE_COMP_HEIGHT = NODE_HEIGHT_DEFAULT + NODE_WIDTH_MARKER_TOOLTIP + NODE_WIDTH_MARKERS;

        protected static int NR_COMP_TO_PARAM_CONNECTION_CALLS = 0;
        protected static double NODE_MIN_LINE_LEN = 0.1;

        internal static void TranslateUIElement(UIElement _uie, Vector _offset)
        {
            if (_uie == null) return;

            if (_uie.RenderTransform is MatrixTransform)
            {
                MatrixTransform transf = (MatrixTransform)_uie.RenderTransform;
                Matrix matrix = transf.Matrix;
                matrix.OffsetX += _offset.X;
                matrix.OffsetY += _offset.Y;
                MatrixTransform transf_modified = new MatrixTransform(matrix);
                _uie.RenderTransform = transf_modified;
            }

            if (_uie.RenderTransform is TranslateTransform)
            {
                TranslateTransform transf = (TranslateTransform)_uie.RenderTransform;
                transf.X += _offset.X;
                transf.Y += _offset.Y;            
                _uie.RenderTransform = transf;
            }
        }

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

        #region PROPERTIES: Display

        public Point Anchor { get; set; }
        public bool IsUserManipulatable { get; protected set; }

        protected List<long> to_be_expanded_chain;
        protected bool to_be_expanded;
        protected bool is_expanded;
        public virtual bool IsExpanded
        {
            get { return this.is_expanded; }
            set { this.is_expanded = value; }
        }

        protected bool is_showing_refs;
        public virtual bool IsShowingRefs
        {
            get { return this.is_showing_refs; }
            set { this.is_showing_refs = value; }
        }

        protected bool is_simple;
        public virtual bool IsSimple
        {
            get { return this.is_simple; }
            set 
            { 
                this.is_simple = value;
                if (this.is_simple)
                {
                    this.node_width = NodeVisualization.NODE_HEIGHT_DEFAULT;
                    this.node_height = NodeVisualization.NODE_HEIGHT_DEFAULT;
                    this.UpdateContent();
                }
                else
                {
                    this.node_width = NodeVisualization.NODE_WIDTH_DEFAULT;
                    this.node_height = NodeVisualization.NODE_HEIGHT_DEFAULT;
                    this.UpdateContent();
                }
            }
        }

        private NodeVisHighlight vis_state;
        public virtual NodeVisHighlight VisState
        {
            get { return this.vis_state; }
            set
            { 
                this.vis_state = value;
                this.AdaptColorsToVisState();
            }
        }


        #endregion

        #region PROPERTIES: Info

        public virtual Category VisCategory
        {
            get { return Category.NoNe; }
        }

        public virtual long VisID
        {
            get { return -1; }
        }

        public Point Position { get { return this.position; } }

        public BoundingBox Extents { get; protected set; }
        

        #endregion

        #region PROPETIES: Connections

        protected List<NodeVisualization> node_children;
        public List<NodeVisualization> Node_Children
        {
            get { return this.node_children; }
            protected set 
            { 
                this.node_children = value; 
            }
        }

        #endregion

        #region CLASS MEMBERS

        protected CompGraph parent_canvas;
        protected object node_data;

        protected Rectangle node_main;
        protected TextBlock text_main;
        protected double node_width;
        protected double node_height;
        protected double node_offset_hrzt;
        protected double node_offset_vert;

        protected Rectangle node_switch_SUB;
        protected Rectangle node_switch_REF;
        protected List<Shape> node_graphics_for_contour_update;
        protected Rectangle node_switch_SELECT;
        protected Rectangle node_switch_ACTIVATE;

        protected Color node_contour;
        protected double node_contour_thickness;
        protected Color node_fill_color_1;
        protected Color node_fill_color_1_default;
        protected Color node_fill_color_2;
        protected Color node_fill_color_2_default;

        protected List<Polyline> node_connections_out;
        protected List<Polyline> node_connections_in;

        protected List<Polyline> node_references_out;
        protected List<Polyline> node_references_in;

        protected List<Polyline> node_param_calc_out;
        protected List<Polyline> node_param_calc_in;

        protected Polyline extents_vis;

        protected Point position;

        #endregion

        #region .CTOR

        public NodeVisualization(CompGraph _parent_canvas, object _data, double _offset_hrzt, double _offset_vert)
            : this(_parent_canvas, _data,
                    NodeVisualization.NODE_WIDTH_DEFAULT, NodeVisualization.NODE_HEIGHT_DEFAULT, _offset_hrzt, _offset_vert,
                    NodeVisualization.NODE_COLOR_STROKE_ACTIVE, NodeVisualization.NODE_COLOR_FILL_ACTIVE_1, NodeVisualization.NODE_COLOR_FILL_ACTIVE_2)
        {  }

        public NodeVisualization(CompGraph _parent_canvas, object _data,
                                 double _width, double _height, double _offset_hrzt, double _offset_vert,
                                 Color _contour, Color _fill_1, Color _fill_2)
        {
            this.parent_canvas = _parent_canvas;
            this.node_data = _data;
            this.node_children = new List<NodeVisualization>();
            this.node_connections_out = new List<Polyline>();
            this.node_connections_in = new List<Polyline>();
            this.node_references_out = new List<Polyline>();
            this.node_references_in = new List<Polyline>();
            this.node_param_calc_out = new List<Polyline>();
            this.node_param_calc_in = new List<Polyline>();

            // node_main -> is built after loading
            // text_main -> is built after loading
            this.node_width = _width;
            this.node_height = _height;
            this.node_offset_hrzt = _offset_hrzt;
            this.node_offset_vert = _offset_vert;

            this.node_contour = _contour;
            this.node_contour_thickness = 1;
            this.node_fill_color_1 = _fill_1;
            this.node_fill_color_1_default = _fill_1;
            this.node_fill_color_2 = _fill_2;
            this.node_fill_color_2_default = _fill_2;

            // cutom display properties
            this.Anchor = new Point(_offset_hrzt, _offset_vert);
            this.IsUserManipulatable = true;
            this.is_expanded = false;
            this.to_be_expanded = false;
            this.to_be_expanded_chain = new List<long>();
            this.is_showing_refs = false;
            this.is_simple = false;
            this.vis_state = NodeVisHighlight.Active;

            // UIControl properties
            //this.Width = this.node_width + NodeVisualization.NODE_WIDTH_SWITCHES * 2;
            //this.Height = this.node_height + NodeVisualization.NODE_WIDTH_SWITCHES;
            this.position = new Point(this.node_offset_hrzt, this.node_offset_vert);
            TranslateTransform transf = new TranslateTransform(this.node_offset_hrzt, this.node_offset_vert);
            this.RenderTransform = transf;

            this.Loaded += NodeVisualization_Loaded;

            // Add to Parent!
            if (this.parent_canvas != null)
                this.parent_canvas.AddChild(this, this.position);
        }

        #endregion

        #region METHODS: Display Update

        public virtual void UpdateContent()
        {
            // remove connections to children nodes
            this.RemoveConnectionsToChildren();
            // remove connections from parameters to calculations
            this.RemoveConnectionsParamsCalc();
            // remove connections to referenced nodes
            this.RemoveReferencePolylines();

            // update the node itself
            this.RedefineGrid();
            this.PopulateGrid();
            if (this.IsExpanded)
            {
                this.PopulateChildren();
                this.DrawConnectionsParamsCalc();
            }
            if (this.IsShowingRefs)
                this.DrawConnectionsToRefs();

            string debug = this.parent_canvas.CanvasConnectionInfo();
        }

        protected virtual void RedefineGrid()
        {   }

        protected virtual void PopulateGrid()
        {   }

        protected void RemoveConnectionsToChildren()
        {
            if (this.parent_canvas == null || this.node_connections_out == null) return;

            foreach (Polyline pl in this.node_connections_out)
            {
                this.parent_canvas.Children.Remove(pl);
            }
            this.node_connections_out = new List<Polyline>();

            foreach(NodeVisualization n in this.node_children)
            {
                n.RemoveConnectionsToChildren();
            }
        }

        protected void RemoveConnectionsParamsCalc(bool _including_children = true)
        {
            if (this.parent_canvas == null || this.node_param_calc_out == null) return;
            //string debug_1 = this.parent_canvas.CanvasConnectionInfo();

            foreach (Polyline pl in this.node_param_calc_out)
            {
                this.parent_canvas.Children.Remove(pl);
            }
            this.node_param_calc_out = new List<Polyline>();
            
            if (_including_children)
            {
                foreach (NodeVisualization n in this.node_children)
                {
                    n.RemoveConnectionsParamsCalc();
                }
            }

            //string debug_2 = this.parent_canvas.CanvasConnectionInfo();
        }

        protected void RemoveReferencePolylines()
        {
            if (this.parent_canvas == null || this.node_references_out == null) return;

            foreach(Polyline pl in this.node_references_out)
            {
                this.parent_canvas.Children.Remove(pl);
            }
            this.node_references_out = new List<Polyline>();

            foreach (NodeVisualization n in this.node_children)
            {
                n.RemoveReferencePolylines();
            }
        }

        internal virtual void RemoveAllGraphics()
        {
            // remove connections to children nodes
            this.RemoveConnectionsToChildren();
            // remove connections from parameters to calculations
            this.RemoveConnectionsParamsCalc();
            // remove connections to referenced nodes
            this.RemoveReferencePolylines();

            // update the node itself
            this.RedefineGrid();
        }

        #endregion

        #region METHODS: Display Children & Connections from Params to Calc

        protected virtual void PopulateChildren()
        {   }

        protected virtual void ClearChildrenTree()
        {
            if (this.parent_canvas != null)
            {
                foreach (NodeVisualization vis in this.node_children)
                {
                    // remove incoming reference connections
                    foreach (Polyline pl in vis.node_references_in)
                    {
                        this.parent_canvas.Children.Remove(pl);
                    }
                    // remove children
                    vis.ClearChildrenTree();
                    // remove self
                    this.parent_canvas.Children.Remove(vis);
                }
            }
            this.node_children.Clear();
        }

        protected virtual void DrawConnectionsParamsCalc()
        {   }

        #endregion

        #region METHODS: Display Connections to Refs 

        protected virtual void DrawConnectionsToRefs()
        {   }

        #endregion

        #region METHODS: Transform

        public void Translate(Vector _offset)
        {
            TranslateTransform transf = (TranslateTransform)this.RenderTransform;
            transf.X += _offset.X;
            transf.Y += _offset.Y;
            this.node_offset_hrzt += _offset.X;
            this.node_offset_vert += _offset.Y;
            this.RenderTransform = transf;
            this.Anchor += _offset;

            //var posTransf = this.TransformToAncestor(this.parent_canvas);
            //this.position = posTransf.Transform(new Point(0, 0));
            this.position += _offset;
            this.Extents.SilentTranslation(_offset, true);

            foreach (NodeVisualization nvis in this.node_children)
            {
                nvis.Translate(_offset);
            }
            foreach (Polyline ncpl in this.node_connections_out)
            {
                NodeVisualization.TranslateUIElement(ncpl, _offset);
            }
        }

        public void TranslateConnectionsIn(Vector _offset)
        {
            foreach (Polyline ncpl_in in this.node_connections_in)
            {
                // rebuild the polyline
                Point startP_new = ncpl_in.Points[0];
                Point endP_old = ncpl_in.Points[ncpl_in.Points.Count - 1];
                Point endP_new = new Point(endP_old.X + _offset.X, endP_old.Y + _offset.Y);
                ncpl_in.Points = NodeVisualization.CreateStepPointCollection(startP_new, endP_new);
            }            
        }

        public void TranslateRefConnections(Vector _offset)
        {
            // REFERENCES
            foreach (Polyline ncpl_in in this.node_references_out)
            {
                // rebuild the polyline
                Point startP_old = ncpl_in.Points[0];
                Point startP_new = new Point(startP_old.X + _offset.X, startP_old.Y + _offset.Y);
                Point endP_new = ncpl_in.Points[ncpl_in.Points.Count - 1];
                ncpl_in.Points = NodeVisualization.Create2StepPointCollection(startP_new, endP_new, this.Height, this.Height, new Vector(1,-1));
            }
            foreach (Polyline ncpl_in in this.node_references_in)
            {
                // rebuild the polyline
                Point startP_new = ncpl_in.Points[0];
                Point endP_old = ncpl_in.Points[ncpl_in.Points.Count - 1];
                Point endP_new = new Point(endP_old.X + _offset.X, endP_old.Y + _offset.Y);
                ncpl_in.Points = NodeVisualization.Create2StepPointCollection(startP_new, endP_new, this.Height, this.Height, new Vector(1, -1));
            }
            // CONENCTIONS Calculation -> Parameter
            foreach(Polyline pl in this.node_param_calc_in)
            {
                // rebuild the polyline
                SolidColorBrush scb = pl.Stroke as SolidColorBrush;
                int arrow_dir = 0;
                if (scb != null)
                    arrow_dir = (scb.Color == NodeVisualization.NODE_COLOR_CALC_IN) ? 1 : -1;
                pl.Points = NodeVisualization.Adapt2StepPointCollectionWArrow(pl.Points, _offset, false, arrow_dir);
            }
            foreach (Polyline pl in this.node_param_calc_out)
            {
                // rebuild the polyline
                SolidColorBrush scb = pl.Stroke as SolidColorBrush;
                int arrow_dir = 0;
                if (scb != null)
                    arrow_dir = (scb.Color == NodeVisualization.NODE_COLOR_CALC_IN) ? 1 : -1;
                pl.Points = NodeVisualization.Adapt2StepPointCollectionWArrow(pl.Points, _offset, true, arrow_dir);
            }
            // recursion
            foreach (NodeVisualization nvis in this.node_children)
            {
                nvis.TranslateRefConnections(_offset);
            }
        }

        #endregion

        #region METHODS: Adapt to Highlighing Status

        protected virtual void AdaptColorsToVisState()
        {
            if (this.node_main == null) return;

            if (!this.vis_state.HasFlag(NodeVisHighlight.Active))
            {
                this.node_fill_color_1 = NodeVisualization.NODE_COLOR_FILL_INACTIVE_1;
                this.node_fill_color_2 = NodeVisualization.NODE_COLOR_FILL_INACTIVE_2;
                this.node_contour = NodeVisualization.NODE_COLOR_STROKE_INACTIVE;
                this.node_contour_thickness = NodeVisualization.NODE_STROKE_THICKNESS_INACTIVE;

                this.ApplyColorChangeToNode();
                return;
            }

            // ---------------------------------------------------------------------------------- //
            
            // set the colors and lines
            if (this.vis_state.HasFlag(NodeVisHighlight.Active))
            {
                this.node_fill_color_1 = this.node_fill_color_1_default;
                this.node_fill_color_2 = this.node_fill_color_2_default;
                this.node_contour = NodeVisualization.NODE_COLOR_STROKE_ACTIVE;
                this.node_contour_thickness = NodeVisualization.NODE_STROKE_THICKNESS_ACTIVE;
            }
            if (this.vis_state.HasFlag(NodeVisHighlight.Selected))
            {
                this.node_contour = NodeVisualization.NODE_COLOR_SPECULAR;
                this.node_contour_thickness = NodeVisualization.NODE_STROKE_THICKNESS_MANIPULATED;
            }
            if (this.vis_state.HasFlag(NodeVisHighlight.Highlighted))
            {
                this.node_fill_color_1 = NodeVisualization.NODE_COLOR_FILL_HIGHLIGHT_1;
                this.node_fill_color_2 = NodeVisualization.NODE_COLOR_FILL_HIGHLIGHT_2;
            }
            if (this.vis_state.HasFlag(NodeVisHighlight.Manipulated) && this.IsUserManipulatable)
            {
                Color tmp = this.node_fill_color_1;
                this.node_fill_color_1 = this.node_fill_color_2;
                this.node_fill_color_2 = tmp;
                //this.node_contour = NodeVisualization.NODE_COLOR_STROKE_MANIPULATED;
                this.node_contour_thickness = NodeVisualization.NODE_STROKE_THICKNESS_MANIPULATED;
            }

            // apply to node
            this.ApplyColorChangeToNode();
        }

        protected void ApplyColorChangeToNode()
        {
            if (this.vis_state.HasFlag(NodeVisHighlight.Selected))
            {
                List<GradientStop> gs = new List<GradientStop>();
                // XOR
                if (this.vis_state.HasFlag(NodeVisHighlight.Highlighted) != this.vis_state.HasFlag(NodeVisHighlight.Manipulated))
                {
                    gs.Add(new GradientStop(this.node_fill_color_1, 0.00));
                    gs.Add(new GradientStop(NodeVisualization.NODE_COLOR_SPECULAR, 0.25));
                    gs.Add(new GradientStop(this.node_fill_color_2, 0.75));
                    gs.Add(new GradientStop(this.node_fill_color_2, 1.00));
                }
                else
                {
                    gs.Add(new GradientStop(this.node_fill_color_1, 0.00));
                    gs.Add(new GradientStop(this.node_fill_color_1, 0.25));
                    gs.Add(new GradientStop(NodeVisualization.NODE_COLOR_SPECULAR, 0.75));
                    gs.Add(new GradientStop(this.node_fill_color_2, 1.00));
                }
                this.node_main.Fill = new LinearGradientBrush(new GradientStopCollection(gs), 90);
                //this.node_main.Effect = new DropShadowEffect()
                //{
                //    Color = this.node_fill_color_2,
                //    Direction = 315,
                //    ShadowDepth = 3,
                //    BlurRadius = 3,
                //    Opacity = 0.5
                //};
            }
            else
            {
                this.node_main.Fill = new LinearGradientBrush(this.node_fill_color_1, this.node_fill_color_2, new Point(0.5, 0), new Point(0.5, 1));
                //this.node_main.Effect = null;
            }
            // body of node
            this.node_main.Stroke = new SolidColorBrush(this.node_contour);
            this.node_main.StrokeThickness = this.node_contour_thickness;
            // subnode switches
            if (this.node_switch_SUB != null)
                this.node_switch_SUB.Stroke = new SolidColorBrush(this.node_contour);
            if (this.node_switch_REF != null)
                this.node_switch_REF.Stroke = new SolidColorBrush(this.node_contour);
            // selection and activation switches
            if (this.node_switch_SELECT != null)
            {
                this.node_switch_SELECT.Stroke = new SolidColorBrush(this.node_contour);
                this.node_switch_SELECT.Fill = (this.VisState.HasFlag(NodeVisHighlight.Selected)) ? new SolidColorBrush(NodeVisualization.NODE_COLOR_YES) : new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
            }
            if (this.node_switch_ACTIVATE != null)
            {
                this.node_switch_ACTIVATE.Stroke = new SolidColorBrush(this.node_contour);
                this.node_switch_ACTIVATE.Fill = (this.VisState == NodeVisHighlight.Inactive) ? new SolidColorBrush(NodeVisualization.NODE_COLOR_NO) : new SolidColorBrush(NodeVisualization.NODE_COLOR_YES);           
            }
            // other geometry
            if (this.node_graphics_for_contour_update != null)
            {
                foreach (Shape s in this.node_graphics_for_contour_update)
                {
                    s.Stroke = new SolidColorBrush(this.node_contour);
                    s.StrokeThickness = this.node_contour_thickness;
                }
            }

        }


        #endregion

        #region METHODS: Display All Children (w/o overlaps)

        protected void ExpandAll()
        {
            if (!this.IsUserManipulatable) return;

            if (!this.IsExpanded)
                this.IsExpanded = true;

            ParameterStructure.Component.Component comp = this.node_data as ParameterStructure.Component.Component;
            if (this.parent_canvas != null && comp != null)
            {
                // tell the parent canvas how many nodes need to be reordered
                if (this.parent_canvas.nodes_to_order_NR == 0)
                {
                    this.parent_canvas.nodes_to_order_NR = comp.GetFlatSubCompList().Count + 1;
                    // add to the nodes the parent canvas needs to reorder
                    if (this.parent_canvas.nodes_to_order == null)
                        this.parent_canvas.nodes_to_order = new List<NodeVisualization>();
                    this.parent_canvas.nodes_to_order.Add(this);
                }
            }

            foreach (NodeVisualization nv in this.node_children)
            {
                ComponentVisualization cv = nv as ComponentVisualization;
                if (cv == null) continue;

                cv.to_be_expanded = true;
                // the logic is completed and called recursively in the Loaded event handler
                // because expansion can only be completed after the respective node has been loaded
            }  
        }

        #endregion

        #region METHODS: Expand a parent chain and select a child

        // the id_chain starts with the parent on a higher hierarchy level and ends with the child to be selected
        internal void SelectChild(List<long> _id_chain)
        {
            if (_id_chain == null) return;
            if (_id_chain.Count < 1) return;

            long current_id = _id_chain[0];
            if (this.VisID != current_id) return;

            long target_id = _id_chain[_id_chain.Count - 1];

            if (!this.IsUserManipulatable)
                this.IsUserManipulatable = true;

            // recursion end
            if (this.VisID == target_id)
            {
                this.to_be_expanded_chain = null;
                this.VisState |= NodeVisHighlight.Selected;
                this.BringIntoView();
                return;
            }

            // go deeper
            List<long> id_chain_rest = new List<long>(_id_chain);
            id_chain_rest.Remove(current_id);
            if (id_chain_rest.Count < 1) return;

            if (this.IsExpanded)
            {
                foreach (NodeVisualization nv in this.node_children)
                {
                    ComponentVisualization cv = nv as ComponentVisualization;
                    if (cv == null) continue;
                    cv.SelectChild(id_chain_rest);
                }
            }
            else
            {
                this.IsExpanded = true;
                foreach (NodeVisualization nv in this.node_children)
                {
                    ComponentVisualization cv = nv as ComponentVisualization;
                    if (cv == null) continue;

                    if (cv.VisID == id_chain_rest[0])
                        cv.to_be_expanded_chain = id_chain_rest;
                    // the logic is completed and called recursively in the Loaded event handler
                    // because expansion/selection can only be completed after the respective node has been loaded
                }
            }

        }

        #endregion

        #region UTILITY METHODS: Polylines

        protected static PointCollection CreateFreePointCollection(Point _startP, Point _endP, int _nr_subPoints = 0)
        {
            PointCollection pointCollection = new PointCollection();

            pointCollection.Add(_startP);
            if (_nr_subPoints > 0)
            {
                for (int i = 1; i <= _nr_subPoints; i++)
                {
                    double fact = (double)i / (_nr_subPoints + 2);
                    Point p = new Point(_startP.X * (1.0 - fact) + _endP.X * fact,
                                        _startP.Y * (1.0 - fact) + _endP.Y * fact);
                    pointCollection.Add(p);
                }
            }
            pointCollection.Add(_endP);

            return pointCollection;
        }

        protected static PointCollection CreateStepPointCollection(Point _startP, Point _endP)
        {
            PointCollection pointCollection = new PointCollection();
            pointCollection.Add(_startP);
            pointCollection.Add(new Point(_endP.X - NodeVisualization.NODE_WIDTH_SWITCHES * 2, _startP.Y));
            pointCollection.Add(new Point(_endP.X - NodeVisualization.NODE_WIDTH_SWITCHES * 2, _endP.Y));
            pointCollection.Add(_endP);
            return pointCollection;
        }

        protected static PointCollection Create2StepPointCollection(Point _startP, Point _endP, 
                                                    double _node_start_H, double _node_end_H, Vector _direction)
        {
            double direction_start = (_direction.X > 0) ? 1 : -1;
            double direction_end = (_direction.Y > 0) ? 1 : -1;

            Vector start_to_end = _endP - _startP;
            Vector start_to_end_Y = new Vector(0, start_to_end.Y);
            start_to_end_Y.Normalize();
            
            PointCollection pointCollection = new PointCollection();
            pointCollection.Add(_startP);

            Point startP2 = new Point(_startP.X + _node_end_H * 0.5 * direction_start, _startP.Y);
            Point startP3 = new Point(startP2.X, startP2.Y + start_to_end_Y.Y * _node_start_H * 0.5);

            pointCollection.Add(startP2);
            pointCollection.Add(startP3);

            Point endP2 = new Point(_endP.X + _node_end_H * 0.5 * direction_end, _endP.Y);
            Point endP3 = new Point(endP2.X, endP2.Y - start_to_end_Y.Y * _node_end_H * 0.5);

            pointCollection.Add(endP3);
            pointCollection.Add(endP2);
            
            pointCollection.Add(_endP);
            return pointCollection;
        }

        protected static PointCollection Create2StepPointCollectionWArrow(Point _startP, Point _endP,
                                                    double _node_start_H, double _node_end_H, Vector _direction, int _arrow_direction)
        {
            double direction_start = (_direction.X > 0) ? 1 : -1;
            double direction_end = (_direction.Y > 0) ? 1 : -1;

            Vector start_to_end = _endP - _startP;
            double dist_vert = Math.Max(NodeVisualization.NODE_CONNECTION_ARROW_SIZE, Math.Min(Math.Abs(start_to_end.Y), _node_start_H * 0.5 + _node_end_H * 0.5));
            Vector start_to_end_Y = new Vector(0, start_to_end.Y);
            start_to_end_Y.Normalize();

            Point startP2 = new Point(_startP.X + _node_end_H * 0.5 * direction_start, _startP.Y);
            Point startP3 = new Point(startP2.X, startP2.Y + start_to_end_Y.Y * dist_vert * 0.5);

            Point endP2 = new Point(_endP.X + _node_end_H * 0.5 * direction_end, _endP.Y);
            Point endP3 = new Point(endP2.X, endP2.Y - start_to_end_Y.Y * dist_vert * 0.5);

            // arrow definition      
            double dir_arrow = (_arrow_direction > 0) ? 1 : -1;
            Vector dir = startP3 - endP3;
            if (Math.Abs(dir.X) < NodeVisualization.NODE_MIN_LINE_LEN && Math.Abs(dir.Y) < NodeVisualization.NODE_MIN_LINE_LEN)
            {
                // move the points further apart
                endP3 += (endP2 - endP3) * 0.25;
                startP3 += (startP2 - startP3) * 0.25;
                dir = startP3 - endP3;
            }
            dir.Normalize();
            dir *= dir_arrow;
            Vector dirN = new Vector(dir.Y, -dir.X);

            Point M1 = new Point((startP3.X + endP3.X) * 0.5, (startP3.Y + endP3.Y) * 0.5);
            Point M2 = M1 + dirN * NodeVisualization.NODE_CONNECTION_ARROW_SIZE * 0.5;
            Point M3 = M1 + dir * NodeVisualization.NODE_CONNECTION_ARROW_SIZE;
            Point M4 = M1 - dirN * NodeVisualization.NODE_CONNECTION_ARROW_SIZE * 0.5;
            Point M5 = M1;

            PointCollection pointCollection = new PointCollection();

            pointCollection.Add(_startP);
            pointCollection.Add(startP2);
            pointCollection.Add(startP3);

            pointCollection.Add(M1);
            pointCollection.Add(M2);
            pointCollection.Add(M3);
            pointCollection.Add(M4);
            pointCollection.Add(M5);

            pointCollection.Add(endP3);
            pointCollection.Add(endP2);
            pointCollection.Add(_endP);

            return pointCollection;
        }

        protected static PointCollection CreateLinePointCollectionWArrow(Point _startP, Point _endP, int _arrow_dir)
        {
            
            // arrow definition 
            double dir_arrow = (_arrow_dir > 0) ? 1 : -1;
            Vector dir = _startP - _endP;
            dir.Normalize();
            dir *= dir_arrow;
            Vector dirN = new Vector(dir.Y, -dir.X);

            Point M1 = new Point((_startP.X + _endP.X) * 0.5, (_startP.Y + _endP.Y) * 0.5);
            Point M2 = M1 + dirN * NodeVisualization.NODE_CONNECTION_ARROW_SIZE * 0.5;
            Point M3 = M1 + dir * NodeVisualization.NODE_CONNECTION_ARROW_SIZE;
            Point M4 = M1 - dirN * NodeVisualization.NODE_CONNECTION_ARROW_SIZE * 0.5;
            Point M5 = M1;

            PointCollection pointCollection = new PointCollection();

            pointCollection.Add(_startP);

            pointCollection.Add(M1);
            pointCollection.Add(M2);
            pointCollection.Add(M3);
            pointCollection.Add(M4);
            pointCollection.Add(M5);

            pointCollection.Add(_endP);

            return pointCollection;
        }

        protected static PointCollection Adapt2StepPointCollectionWArrow(PointCollection _points_old, Vector _offset, bool _affects_start, int _arrow_dir)
        {
            if (_points_old == null || _points_old.Count < 6) return _points_old;

            Point startP_new, startP1_new, startP2_new, endP_new, endP1_new, endP2_new;

            if (_affects_start)
            {
                startP_new = _points_old[0] + _offset;
                startP1_new = _points_old[1] + _offset;
                startP2_new = _points_old[2] + _offset;
                endP_new = _points_old[_points_old.Count - 1];
                endP1_new = _points_old[_points_old.Count - 2];
                endP2_new = _points_old[_points_old.Count - 3];
            }
            else
            {
                startP_new = _points_old[0];
                startP1_new = _points_old[1];
                startP2_new = _points_old[2];
                endP_new = _points_old[_points_old.Count - 1] + _offset;
                endP1_new = _points_old[_points_old.Count - 2] + _offset;
                endP2_new = _points_old[_points_old.Count - 3] + _offset;
            }

            PointCollection points_new = new PointCollection();
            points_new.Add(startP_new);
            points_new.Add(startP1_new);
            points_new.Add(startP2_new);
            // test if drawing an arrow is actually possible
            Vector test_dist = endP2_new - startP2_new;
            if (Math.Abs(test_dist.X) > NodeVisualization.NODE_MIN_LINE_LEN || Math.Abs(test_dist.Y) > NodeVisualization.NODE_MIN_LINE_LEN)
            {
                // draw the arrow
                PointCollection l_wArrow = NodeVisualization.CreateLinePointCollectionWArrow(startP2_new, endP2_new, _arrow_dir);
                foreach (Point p in l_wArrow)
                {
                    points_new.Add(p);
                }
            }            
            points_new.Add(endP2_new);
            points_new.Add(endP1_new);
            points_new.Add(endP_new);

            return points_new;
        }

        protected static PointCollection CreateBoundingBoxPointCollection(Point _upper_left, Point _lower_right)
        {
            PointCollection pointCollection = new PointCollection();
            pointCollection.Add(_upper_left);
            pointCollection.Add(new Point(_upper_left.X, _lower_right.Y));
            pointCollection.Add(_lower_right);
            pointCollection.Add(new Point(_lower_right.X, _upper_left.Y));
            pointCollection.Add(_upper_left);

            return pointCollection;
        }

        protected Polyline CreateFreeConnectingPolyline(Point _startP, Point _endP, int _nr_subPoints = 0)
        {
            if (this.parent_canvas == null) return null;

            Polyline pline = new Polyline();
            pline.Stroke = new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
            pline.StrokeThickness = 2;
            pline.FillRule = FillRule.EvenOdd;

            PointCollection pointCollection = NodeVisualization.CreateFreePointCollection(_startP, _endP, _nr_subPoints);
            pline.Points = pointCollection;

            this.parent_canvas.Children.Add(pline);
            return pline;
        }

        protected Polyline CreateStepConnectingPolyline(Point _startP, Point _endP)
        {
            if (this.parent_canvas == null) return null;

            Polyline pline = new Polyline();
            pline.Stroke = new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
            pline.StrokeThickness = 2;
            pline.FillRule = FillRule.EvenOdd;

            PointCollection pointCollection = NodeVisualization.CreateStepPointCollection(_startP, _endP);
            pline.Points = pointCollection;

            this.parent_canvas.Children.Add(pline);
            return pline;
        }

        protected Polyline Create2StepReferencePolyline(Point _startP, Point _endP, double _node_start_H, double _node_end_H, bool _direct = true)
        {
            if (this.parent_canvas == null) return null;

            Polyline pline = new Polyline();
            pline.Stroke = (_direct) ? new SolidColorBrush(NodeVisualization.NODE_COLOR_YES) : new SolidColorBrush(NodeVisualization.NODE_COLOR_MAYBE);
            pline.StrokeThickness = 2;
            pline.FillRule = FillRule.EvenOdd;

            PointCollection pointCollection = NodeVisualization.Create2StepPointCollection(_startP, _endP, _node_start_H, _node_end_H, new Vector(1, -1));
            pline.Points = pointCollection;

            this.parent_canvas.Children.Add(pline);
            return pline;
        }

        protected Polyline Create2StepCalcToParamPolyline(Point _startP, Point _endP, double _node_start_H, double _node_end_H, 
                                                          bool _in)
        {
            if (this.parent_canvas == null) return null;                                        

            Polyline pline = new Polyline();
            pline.Stroke = (_in) ? new SolidColorBrush(NodeVisualization.NODE_COLOR_CALC_IN) : new SolidColorBrush(NodeVisualization.NODE_COLOR_CALC_RET);
            pline.StrokeThickness = 1;
            pline.FillRule = FillRule.EvenOdd;

            // the offsets should allow subsequent polylines to avoid overlap
            double offset_size = 5 * NodeVisualization.NR_COMP_TO_PARAM_CONNECTION_CALLS;
            double offset_pos = 2 * NodeVisualization.NR_COMP_TO_PARAM_CONNECTION_CALLS;
            int arrow_dir = (_in) ? 1 : -1;
            PointCollection pointCollection = NodeVisualization.Create2StepPointCollectionWArrow(_startP + new Vector(0, offset_pos), _endP + new Vector(-0, offset_pos), 
                                                        _node_start_H + offset_size, _node_end_H + offset_size, new Vector(1, 1), arrow_dir);
            pline.Points = pointCollection;
            pline.Tag = new Point4D(_startP.X, _startP.Y, _endP.X, _endP.Y);

            string debug_1 = this.parent_canvas.CanvasConnectionInfo();

            this.parent_canvas.Children.Add(pline);

            string debug_2 = this.parent_canvas.CanvasConnectionInfo();

            // this can be reset, when needed
            NodeVisualization.NR_COMP_TO_PARAM_CONNECTION_CALLS++;
            return pline;
        }

        protected Polyline CreateBoundingBoxPolyline(BoundingBox _bb)
        {
            if (this.parent_canvas == null) return null;
            if (_bb == null) return null;

            Polyline pline = new Polyline();
            pline.Stroke = new SolidColorBrush(NodeVisualization.NODE_COLOR_BB);
            pline.StrokeThickness = 2;
            pline.FillRule = FillRule.EvenOdd;

            PointCollection pointCollection = NodeVisualization.CreateBoundingBoxPointCollection(_bb.UpperLeft, _bb.LowerRight);
            pline.Points = pointCollection;

            this.parent_canvas.Children.Add(pline);
            return pline;
        }

        #endregion

        #region EVENT HANDLER

        protected void NodeVisualization_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateContent();
            if (this.to_be_expanded)
            {
                ////long debug = this.VisID;
                this.ExpandAll();
                // add to the nodes the parent canvas needs to reorder
                if (this.parent_canvas.nodes_to_order_NR > this.parent_canvas.nodes_to_order.Count)
                    this.parent_canvas.nodes_to_order.Add(this);
                // check if all nodes were added
                if (this.parent_canvas.nodes_to_order_NR == this.parent_canvas.nodes_to_order.Count)
                    this.parent_canvas.NodeOrderListFull = true;
            }
            if (this.to_be_expanded_chain != null)
            {
                this.SelectChild(this.to_be_expanded_chain);
            }
        }

        protected void rect_SUB_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Rectangle rect = sender as Rectangle;
            if (rect == null) return;
            if (!this.IsUserManipulatable) return;
            this.IsExpanded = !(this.IsExpanded);
        }

        protected void rect_REF_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Rectangle rect = sender as Rectangle;
            if (rect == null) return;
            if (!this.IsUserManipulatable) return;
            this.IsShowingRefs = !(this.IsShowingRefs);
        }
        #endregion
    }
}
