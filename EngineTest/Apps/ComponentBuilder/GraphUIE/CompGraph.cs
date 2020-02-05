using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;

using ParameterStructure.Component;

namespace ComponentBuilder.GraphUIE
{
    class CompGraph : Canvas, INotifyPropertyChanged
    {
        #region STATIC

        protected static int NODE_HEIGHT_MIN = 10;
        protected static int NODE_HEIGHT_STD = 25;
        protected static int NODE_HEIGHT_MAX = 40;

        protected static int NODE_DIST_VERT_MIN = 2;

        protected static int NODE_WIDTH_STD = 60;

        protected static Color NODE_FILL_1 = (Color)ColorConverter.ConvertFromString("#ff8787ff");
        protected static Color NODE_FILL_2 = (Color)ColorConverter.ConvertFromString("#ffffe3b6");
        protected static Color TICK_COLOR = (Color)ColorConverter.ConvertFromString("#ff000033");

        protected static double TICK_SPACING = 50.0;
        protected static double TICK_HEIGHT = 10.0;

        protected static readonly IFormatProvider FORMATTER = new NumberFormatInfo();
        protected static readonly DateTimeFormatInfo DT_FORMATTER = new DateTimeFormatInfo();

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

        #region PROERTIES: Component Factory

        public ComponentFactory CompFactory
        {
            get { return (ComponentFactory)GetValue(CompFactoryProperty); }
            set { SetValue(CompFactoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CompFactory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CompFactoryProperty =
            DependencyProperty.Register("CompFactory", typeof(ComponentFactory), typeof(CompGraph),
            new UIPropertyMetadata(null, new PropertyChangedCallback(CompFactoryPropertyChangedCallback),
                                         new CoerceValueCallback(CompFactoryPropertyCoerceValueCallback)));

        private static void CompFactoryPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CompGraph instance = d as CompGraph;
            if (instance == null) return;

            if (instance.CompFactory != null)
            {
                instance.CompFactory.PropertyChanged += instance.compFactory_PropertyChanged;
                instance.PopulateCanvas();
            }
        }

        private static object CompFactoryPropertyCoerceValueCallback(DependencyObject d, object baseValue)
        {
            CompGraph instance = d as CompGraph;            
            if (instance != null && instance.CompFactory != null)
            {
                instance.CompFactory.PropertyChanged -= instance.compFactory_PropertyChanged;
            }

            return baseValue;
        }

        #endregion

        #region PROPERTIES: Node Ordering

        private bool node_order_list_full = false;
        public bool NodeOrderListFull
        {
            get { return this.node_order_list_full; }
            set 
            { 
                this.node_order_list_full = value;
                if (this.node_order_list_full)
                {
                    this.ReorderNodes();
                }
            }
        }

        #endregion

        #region CLASS MEMBERS

        private int nr_nodes = 0;

        private double node_height = NodeVisualization.NODE_COMP_HEIGHT;
        private double node_width = NodeVisualization.NODE_WIDTH_DEFAULT;

        private int node_dist_vert = CompGraph.NODE_DIST_VERT_MIN;
        //private int node_dist_hrzt = 0;

        private Vector canvas_expand = new Vector(0, 0);
        private Vector content_offset = new Vector(0, 0);

        private TextBlock canvas_size_label;
        private List<Line> canvas_size_ticks;

        // for ordering expanded nodes to avoid overlaps
        internal int nodes_to_order_NR = 0;
        internal List<NodeVisualization> nodes_to_order;

        #endregion

        #region .CTOR

        public CompGraph()
            :base()
        {
            this.UseLayoutRounding = true;
            this.MouseUp += GraphCanvas_MouseUp;
            this.Loaded += CompGraph_Loaded;
        }

        #endregion

        #region METHODS: Labels & Rulers

        private void CompGraph_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.canvas_size_label == null)
            {
                // label the lower right corner
                TextBlock tb_LR = new TextBlock();
                tb_LR.Width = 100;
                tb_LR.Height = 25;
                TranslateTransform transf = new TranslateTransform(this.Width - tb_LR.Width, this.Height - tb_LR.Height);
                tb_LR.RenderTransform = transf;

                tb_LR.Text = "(" + this.Width + " " + this.Height + ")";
                tb_LR.Foreground = new SolidColorBrush(CompGraph.TICK_COLOR);
                tb_LR.FontSize = 12;
                tb_LR.Padding = new Thickness(2);
                tb_LR.IsHitTestVisible = false;

                this.canvas_size_label = tb_LR;
                this.Children.Add(tb_LR);
            }

            this.UpdateTicks();
        }

        private void UpdateSizeLabel()
        {
            if (this.canvas_size_label == null) return;

            this.canvas_size_label.Text = "(" + this.Width + " " + this.Height + ")";
            TranslateTransform transf = new TranslateTransform(this.Width - this.canvas_size_label.Width, this.Height - this.canvas_size_label.Height);
            this.canvas_size_label.RenderTransform = transf;
        }

        private void UpdateTicks()
        {
            if (this.canvas_size_ticks != null)
            {
                foreach(Line tick in this.canvas_size_ticks)
                {
                    this.Children.Remove(tick);
                }
            }
            this.canvas_size_ticks = new List<Line>();

            
            // the X ticks
            int nr_ticks_X = (int)Math.Floor(this.Width / CompGraph.TICK_SPACING);
            for (int i = 0; i < nr_ticks_X; i++)
            {
                Line tick = new Line();
                tick.X1 = i * CompGraph.TICK_SPACING;
                double y1_pos = (i % 2 == 0) ? this.Height - CompGraph.TICK_HEIGHT * 1.5 : this.Height - CompGraph.TICK_HEIGHT;
                tick.Y1 = Math.Floor(y1_pos);
                tick.X2 = tick.X1;
                tick.Y2 = this.Height;
                tick.Stroke = new SolidColorBrush(CompGraph.TICK_COLOR);
                tick.StrokeThickness = 1;
                tick.IsHitTestVisible = false;

                this.canvas_size_ticks.Add(tick);
                this.Children.Add(tick);
            } 
          
            // the Y ticks
            int nr_ticks_Y = (int)Math.Floor(this.Height / CompGraph.TICK_SPACING);
            for (int i = 0; i < nr_ticks_Y; i++)
            {
                Line tick = new Line();
                double x1_pos = (i % 2 == 0) ? this.Width - CompGraph.TICK_HEIGHT * 1.5 : this.Width - CompGraph.TICK_HEIGHT;
                tick.X1 = Math.Floor(x1_pos);
                tick.Y1 = i * CompGraph.TICK_SPACING;
                tick.X2 = this.Width;
                tick.Y2 = tick.Y1;
                tick.Stroke = new SolidColorBrush(CompGraph.TICK_COLOR);
                tick.StrokeThickness = 1;
                tick.IsHitTestVisible = false;

                this.canvas_size_ticks.Add(tick);
                this.Children.Add(tick);
            } 

            // the canvas UpperLeft corner
            Line tick_UL_x = new Line();
            tick_UL_x.X1 = 1;
            tick_UL_x.Y1 = 1;
            tick_UL_x.X2 = CompGraph.TICK_HEIGHT * 3 + 1;
            tick_UL_x.Y2 = 1;
            tick_UL_x.Stroke = new SolidColorBrush(CompGraph.TICK_COLOR);
            tick_UL_x.StrokeThickness = 1;
            tick_UL_x.IsHitTestVisible = false;

            this.canvas_size_ticks.Add(tick_UL_x);
            this.Children.Add(tick_UL_x);

            Line tick_UL_y = new Line();
            tick_UL_y.X1 = 1;
            tick_UL_y.Y1 = 1;
            tick_UL_y.X2 = 1;
            tick_UL_y.Y2 = CompGraph.TICK_HEIGHT * 3 + 1;
            tick_UL_y.Stroke = new SolidColorBrush(CompGraph.TICK_COLOR);
            tick_UL_y.StrokeThickness = 1;
            tick_UL_y.IsHitTestVisible = false;

            this.canvas_size_ticks.Add(tick_UL_y);
            this.Children.Add(tick_UL_y);

            // the canvas LowerRight corner
            Line tick_LR_x = new Line();
            tick_LR_x.X1 = this.Width - CompGraph.TICK_HEIGHT * 1.5 - 1;
            tick_LR_x.Y1 = this.Height - 1;
            tick_LR_x.X2 = this.Width - 1;
            tick_LR_x.Y2 = this.Height - 1;
            tick_LR_x.Stroke = new SolidColorBrush(CompGraph.TICK_COLOR);
            tick_LR_x.StrokeThickness = 1;
            tick_LR_x.IsHitTestVisible = false;

            this.canvas_size_ticks.Add(tick_LR_x);
            this.Children.Add(tick_LR_x);

            Line tick_LR_y = new Line();
            tick_LR_y.X1 = this.Width - 1;
            tick_LR_y.Y1 = this.Height - CompGraph.TICK_HEIGHT * 1.5 - 1;
            tick_LR_y.X2 = this.Width - 1;
            tick_LR_y.Y2 = this.Height - 1;
            tick_LR_y.Stroke = new SolidColorBrush(CompGraph.TICK_COLOR);
            tick_LR_y.StrokeThickness = 1;
            tick_LR_y.IsHitTestVisible = false;

            this.canvas_size_ticks.Add(tick_LR_y);
            this.Children.Add(tick_LR_y);
        }

        #endregion

        #region METHODS: Populate the Canvas with the contents of the Component Factory

        protected void CalculatePositions()
        {
            this.nr_nodes = this.CompFactory.ComponentRecord.Count;

            this.Height = Math.Ceiling(this.nr_nodes * NodeVisualization.NODE_COMP_HEIGHT + (this.nr_nodes + 1) * this.node_dist_vert);
            this.Width = this.Height + NodeVisualization.NODE_WIDTH_DEFAULT * 2; // visual buffer
        }

        protected void PopulateCanvas()
        {
            CalculatePositions();

            int counter = 0;
            foreach (ParameterStructure.Component.Component c in this.CompFactory.ComponentRecord)
            {
                if (c == null) continue;               
                if (c.IsMarkable && !c.IsMarked) continue;
                counter++;

                ComponentVisualization cv = new ComponentVisualization(this, c, 3, counter * this.node_dist_vert + (counter - 1) * this.node_height);
            }
        }

        public void AddChild(UIElement _child, Point _anchor)
        {
            if (_child == null) return;
            _child.MouseDown += cv_MouseDown;
            _child.MouseMove += cv_MouseMove;
            _child.MouseUp += cv_MouseUp;
            this.Children.Add(_child);

            // check position of child and mark this for resizing, if necessary
            this.TestCanvasAgainstPointForExpansion(_anchor, new Vector(this.node_width, this.node_height));
        }

        // to use after adding new children to the canvas
        internal void AdaptSize2Content()
        {
            if (this.canvas_expand.X == 0 && this.canvas_expand.Y == 0) return;
            this.Width += this.canvas_expand.X;
            this.Height += this.canvas_expand.Y;
            this.canvas_expand = new Vector(0, 0);
            this.UpdateSizeLabel();
            this.UpdateTicks();

            if (this.content_offset.X == 0 && this.content_offset.Y == 0) return;
            foreach (ParameterStructure.Component.Component c in this.CompFactory.ComponentRecord)
            {
                foreach(object child in this.Children)
                {
                    ComponentVisualization cv = child as ComponentVisualization;
                    if (cv == null) continue;

                    if (cv.VisID == c.ID)
                    {
                        cv.Translate(this.content_offset);
                        cv.TranslateConnectionsIn(this.content_offset);
                        cv.TranslateRefConnections(this.content_offset);
                    }
                }
            }

            this.content_offset = new Vector(0, 0);            
        }

        public void ExpandCanvas(double _width_exp, double _height_exp)
        {
            this.Width += _width_exp;
            this.Height += _height_exp;
            this.UpdateSizeLabel();
            this.UpdateTicks();
        }

        public void FitSize2Content()
        {
            // test for expansion
            foreach(object child in this.Children)
            {
                NodeVisualization nv = child as NodeVisualization;
                if (nv == null) continue;

                this.TestCanvasAgainstPointForExpansion(nv.Position, new Vector(nv.Width, nv.Height));
                //this.TestCanvasAgainstPointForExpansion(nv.Extents.UpperLeft + nv.Extents.ParentTranslation, nv.Extents.LowerRight - nv.Extents.UpperLeft);                
            }

            // test for contraction
            if (this.canvas_expand.X == 0 && this.canvas_expand.Y == 0)
            {
                this.canvas_expand = new Vector(this.Width, this.Height);
                foreach (object child in this.Children)
                {
                    NodeVisualization nv = child as NodeVisualization;
                    if (nv == null) continue;

                    this.TestCanvasAgainstPointForContraction(nv.Position, new Vector(nv.Width, nv.Height));
                    //this.TestCanvasAgainstPointForContraction(nv.Extents.UpperLeft + nv.Extents.ParentTranslation, nv.Extents.LowerRight - nv.Extents.UpperLeft);
                }
                this.canvas_expand = -this.canvas_expand;
            }           

            this.AdaptSize2Content();
        }

        protected void TestCanvasAgainstPointForExpansion(Point _p, Vector _size)
        {
            if (_p.X < 0)
            {
                this.canvas_expand.X = Math.Max(this.canvas_expand.X, -_p.X);
                this.content_offset.X = Math.Max(this.content_offset.X, - _p.X);
            }
            else if (_p.X + _size.X > this.Width)
            {
                this.canvas_expand.X = Math.Max(this.canvas_expand.X, _p.X + _size.X - this.Width);
            }

            if (_p.Y < 0)
            {
                this.canvas_expand.Y = Math.Max(this.canvas_expand.Y, -_p.Y);
                this.content_offset.Y = Math.Max(this.content_offset.Y, -_p.Y);
            }
            else if (_p.Y + _size.Y > this.Height)
            {
                this.canvas_expand.Y = Math.Max(this.canvas_expand.Y, _p.Y + _size.Y - this.Height);
            }
        }

        protected void TestCanvasAgainstPointForContraction(Point _p, Vector _size)
        {
            if (_p.X + _size.X < this.Width)
                this.canvas_expand.X = Math.Min(this.canvas_expand.X, Math.Abs(_p.X + _size.X - this.Width));
            else
                this.canvas_expand.X = 0;

            if (_p.Y + _size.Y < this.Height)
                this.canvas_expand.Y = Math.Min(this.canvas_expand.Y, Math.Abs(_p.Y + _size.Y - this.Height));
            else
                this.canvas_expand.Y = 0;
        }

        #endregion

        #region METHODS: Highlight acc to Category and Manager Type

        public void Highlight(List<Category> _cats, bool _or = true)
        {
            // reset
            this.UnHighlight();
            List<NodeVisualization> to_highlight = new List<NodeVisualization>();

            // gather information
            foreach (object child in this.Children)            
            {
                NodeVisualization nv = child as NodeVisualization;
                if (nv == null) continue;

                bool take = !_or;                
                foreach (Category cat in _cats)
                {
                    if (_or)
                        take |= nv.VisCategory.HasFlag(cat);
                    else
                        take &= nv.VisCategory.HasFlag(cat);                    
                }

                if (take)
                    to_highlight.Add(nv);
            }

            // perform highlighting
            foreach (NodeVisualization nv in to_highlight)
            {
                nv.VisState |= NodeVisHighlight.Highlighted;
            }
        }

        public void Highlight(List<ComponentManagerType> _mans, bool _or = true)
        {
            // reset
            this.UnHighlight();
            List<ComponentVisualization> to_highlight = new List<ComponentVisualization>();

            // gather information
            foreach (object child in this.Children)
            {
                ComponentVisualization cv = child as ComponentVisualization;
                if (cv == null) continue;

                bool take = !_or;
                foreach (ComponentManagerType man in _mans)
                {
                    if (_or)
                        take |= cv.CompManagerHasWritingAccess(man);
                    else
                        take &= cv.CompManagerHasWritingAccess(man);
                }

                if (take)
                    to_highlight.Add(cv);
            }

            // perform highlighting
            foreach (ComponentVisualization cv in to_highlight)
            {
                cv.VisState |= NodeVisHighlight.Highlighted;
            }
        }

        public void UnHighlight()
        {
            // gather information
            List<NodeVisualization> to_unhighlight = new List<NodeVisualization>();
            foreach (object child in this.Children)
            {
                NodeVisualization nv = child as NodeVisualization;
                if (nv == null) continue;

                to_unhighlight.Add(nv);
            }

            // perform unhighlighting
            foreach (NodeVisualization nv in to_unhighlight)
            {
                nv.VisState &= ~NodeVisHighlight.Highlighted;
            }
        }

        #endregion

        #region METHODS: Selection

        internal void SelectNode(ComponentVisualization _cv)
        {
            if (_cv == null || this.CompFactory == null) return;

            _cv.VisState |= NodeVisHighlight.Selected;
            this.CompFactory.SelectComponent(_cv.VisID);
        }

        internal void DeselectNode(ComponentVisualization _cv)
        {
            if (_cv == null || this.CompFactory == null) return;

            _cv.VisState &= ~NodeVisHighlight.Selected;
            this.CompFactory.SelectComponent(null);
        }

        public void SelectComponent(ParameterStructure.Component.Component _comp)
        {
            if (_comp == null) return;

            // deselect all
            this.DeselectAllNodes();

            // starts w top-level parent and ends with _comp
            List<ParameterStructure.Component.Component> parent_chain = this.CompFactory.GetParentComponentChain(_comp);
            List<long> parent_id_chain = parent_chain.Select(x => x.ID).ToList();
            if (parent_id_chain.Count < 1) return;

            foreach (object child in this.Children)
            {
                ComponentVisualization cv = child as ComponentVisualization;
                if (cv == null) continue;

                if (cv.VisID == parent_id_chain[0])
                {
                    cv.SelectChild(parent_id_chain);
                    break;
                }
            }
        }

        protected void DeselectAllNodes()
        {
            foreach(object child in this.Children)
            {
                ComponentVisualization cv = child as ComponentVisualization;
                if (cv == null) continue;

                cv.VisState &= ~NodeVisHighlight.Selected;
            }
        }

        #endregion

        #region METHODS: Repositioning of Nodes

        private void ReorderNodes()
        {
            if (this.nodes_to_order != null && this.nodes_to_order.Count == this.nodes_to_order_NR)
            {
                // debug
                string debug = string.Empty;                

                // perform the reordering
                this.nodes_to_order.Reverse();
                foreach (NodeVisualization nv in this.nodes_to_order)
                {
                    ComponentVisualization cv = nv as ComponentVisualization;
                    if (cv == null) continue;
                    if (cv.Node_Children.Where(x => x is ComponentVisualization).Count() == 0) continue;

                    //debug += cv.node_component.CurrentSlot + ": " + cv.node_component.Name + " " + cv.node_component.Description + "\n";

                    cv.RepositionChildren();
                }

                // reset
                this.nodes_to_order_NR = 0;
                this.nodes_to_order = null;
                this.node_order_list_full = false;
            }
        }


        #endregion

        #region METHODS: Saving

        public void SaveCanvasAsImage()
        {
            // adapt canvas size to content
            this.FitSize2Content();
            // save
            WpfUtils.CanvasUtils.SaveCanvasAsImage(this, "Graph", 192d);
        }

        #endregion

        #region METHODS: Info

        public string CanvasConnectionInfo()
        {
            string children_str = string.Empty;
            foreach (object child in this.Children)
            {
                if (child is Line || child is TextBlock) continue;

                Polyline pl = child as Polyline;
                if (pl != null)
                {
                    if (pl.Tag == null)
                        continue;
                    else
                    {
                        if (pl.Tag is Point4D)
                        {
                            Point4D p = (Point4D)pl.Tag;
                            children_str += "Polyline: start(" + p.X.ToString("F2", CompGraph.FORMATTER) + ", " +
                                                                 p.Y.ToString("F2", CompGraph.FORMATTER) + ") " +
                                                       "end(" +
                                                                 p.Z.ToString("F2", CompGraph.FORMATTER) + ", " +
                                                                 p.W.ToString("F2", CompGraph.FORMATTER) + ")\n";
                        }
                    }
                }
                else
                {
                    children_str += child.ToString() + "\n";
                }
            }

            return children_str;
        }

        #endregion

        #region EVENT HANDLERS: Mouse

        protected void cv_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ComponentVisualization cv = sender as ComponentVisualization;
            if (cv == null || e == null) return;

            cv.VisState |= NodeVisHighlight.Manipulated;
            cv.Anchor = e.GetPosition(this);
        }

        protected void cv_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ComponentVisualization cv = sender as ComponentVisualization;
            if (cv == null || e == null) return;

            if (!cv.VisState.HasFlag(NodeVisHighlight.Manipulated)) return;
            if (!cv.IsUserManipulatable) return;

            Point mPos = e.GetPosition(this);
            Vector offset = mPos - cv.Anchor;
            cv.Translate(offset);
            cv.TranslateConnectionsIn(offset);
            cv.TranslateRefConnections(offset);
        }

        protected void cv_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ComponentVisualization cv = sender as ComponentVisualization;
            if (cv == null || e == null) return;

            cv.VisState &= ~NodeVisHighlight.Manipulated;
            
            // announce change in child extents to parent
            Vector offset_total = cv.Extents.ParentTranslation;
            cv.Extents.ParentTranslation = offset_total;
        }

        protected void GraphCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            foreach (var child in this.Children)
            {
                ComponentVisualization cv = child as ComponentVisualization;
                if (cv == null) continue;

                cv.VisState &= ~NodeVisHighlight.Manipulated;
            }
        }


        #endregion

        #region EVENT HANDLER: Component Factory

        private void compFactory_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ComponentFactory cf = sender as ComponentFactory;
            if (cf == null || e == null) return;

            if (e.PropertyName == "MarkedId")
            {
                if (cf.MarkedId > 0)
                {
                    if (!cf.MarkedTrue)
                    {
                        // remove node from the graph
                        foreach(object child in this.Children)
                        {
                            ComponentVisualization cv = child as ComponentVisualization;
                            if (cv == null) continue;
                            if (cv.VisID == cf.MarkedId)
                            {
                                cv.RemoveAllGraphics();
                                this.Children.Remove(cv);
                                break;
                            }
                        }
                    }
                    else
                    {                        
                        ParameterStructure.Component.Component c_to_add = this.CompFactory.ComponentRecord.Find(x => x.ID == cf.MarkedId);
                        if (c_to_add != null)
                        {
                            // expand graph
                            this.canvas_expand = new Vector(NodeVisualization.NODE_WIDTH_DEFAULT, NodeVisualization.NODE_COMP_HEIGHT * 1.5);
                            this.AdaptSize2Content();
                            // add node to the graph
                            ComponentVisualization cv = new ComponentVisualization(this, c_to_add, 3, this.Height - NodeVisualization.NODE_COMP_HEIGHT);
                        }
                        
                    }
                }
            }
        }

        #endregion
    }
}
