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

using ComponentBuilder.UIElements;
using ParameterStructure.Component;

namespace ComponentBuilder.GraphUIE_FlNet
{
    public class FlowNwGraph : Canvas, INotifyPropertyChanged
    {
        #region STATIC

        private static Color TICK_COLOR = (Color)ColorConverter.ConvertFromString("#ff000033");
        private static double TICK_SPACING = 25.0;
        private static double TICK_HEIGHT = 5.0;
        private static double CONTENT_PADDING = 25.0;

        private static double SNAP_MAGNET = 20.0;
        private static double CLUSTERING_CHANGE_CUTOFF = 10.0;

        private static System.IFormatProvider NR_FORMATTER = new System.Globalization.NumberFormatInfo();

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

        #region PROPERTIES: Component Factory Containing the Network to Display

        public ComponentFactory CompFactory
        {
            get { return (ComponentFactory)GetValue(CompFactoryProperty); }
            set { SetValue(CompFactoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CompFactory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CompFactoryProperty =
            DependencyProperty.Register("CompFactory", typeof(ComponentFactory), typeof(FlowNwGraph), 
            new UIPropertyMetadata(null));

        #endregion

        #region PROPERTIES: Network to Display

        public FlowNetwork NetworkToDisplay
        {
            get { return (FlowNetwork)GetValue(NetworkToDisplayProperty); }
            set { SetValue(NetworkToDisplayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NetworkToDisplay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NetworkToDisplayProperty =
            DependencyProperty.Register("NetworkToDisplay", typeof(FlowNetwork), typeof(FlowNwGraph),
            new UIPropertyMetadata(null, new PropertyChangedCallback(NetworkToDisplayPropertyChangedCallback)));

        private static void NetworkToDisplayPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FlowNwGraph instance = d as FlowNwGraph;
            if (instance == null) return;

            if (instance.NetworkToDisplay != null)
            {                
                instance.PopulateCanvas();
                instance.SetSizeLabel();
                instance.UpdateTicks();
            }
        }

        #endregion

        #region CLASS MEMBERS

        // manipulation of the network
        public ManagementState GuiState_Prev { get; private set; }

        private ManagementState gui_state;
        public ManagementState GuiState 
        {
            get { return this.gui_state; }
            private set
            {
                this.gui_state = value;
                this.RegisterPropertyChanged("GuiState");
            }
        }

        // edge definition
        private int nr_nodes_for_edge_selected = 0;
        private FlNwNodeVisualization node_start;
        private FlNwNodeVisualization node_end;

        // edge rerouting
        private FlNwEdgeVisualization edge_to_rerout;
        private FlNwNodeVisualization edge_to_rerout_to_Node;
        bool edge_to_rerout_Start; // indicates whether the start or end of the edge is affected

        // component assignment
        private FlNwElementVisualization element_to_assoc_w_comp;

        private ParameterStructure.Component.Component comp_to_assign;
        internal ParameterStructure.Component.Component CompToAssign 
        {
            get { return this.comp_to_assign; }
            set
            {
                this.comp_to_assign = value;
                if (this.comp_to_assign != null && !this.comp_to_assign.IsLocked && this.element_to_assoc_w_comp != null)
                {
                    FlNwNodeVisualization node = this.element_to_assoc_w_comp as FlNwNodeVisualization;
                    FlNwEdgeVisualization edge = this.element_to_assoc_w_comp as FlNwEdgeVisualization;
                    if (node != null && node.FN_Node != null)
                        node.FN_Node.Content = this.comp_to_assign;
                    else if (edge != null && edge.FN_Edge != null)
                        edge.FN_Edge.Content = this.comp_to_assign;                    
                }
                this.OnSwitchState(ManagementState.ASSIGN_COMP);
            }
        }

        private bool is_picking_comp = false;
        public bool IsPickingComp
        {
            get { return this.is_picking_comp; }
            protected set 
            {
                this.is_picking_comp = value;
                this.RegisterPropertyChanged("IsPickingComp");
            }
        }

        // canvas decorations
        private TextBlock canvas_size_label;
        private List<Line> canvas_size_ticks;

        // canvas marking of common content
        private List<Rectangle> marking_boxes;

        // canvas resizng
        private Vector canvas_expand = new Vector(0, 0);
        private Vector content_offset = new Vector(0, 0);

        // component size and operation definition
        private CompGeomSizeWindow size_window;
        private FlNwElementVisualization element_vis_to_resize;
        private FlNwElementVisualization element_vis_to_set_operations;

        // network element highlighting
        private ParameterStructure.Component.Component last_selected_comp;

        // setting the operations in a node
        private FlowNetworkOperationWindow operation_window;

        // flow calculation step-by-step
        private bool flow_calc_step_by_step_ready;
        private List<FlNetNode> flow_calc_step_by_step_all_sorted_nodes;
        private int flow_calc_step_by_step_index;

        #endregion

        #region .CTOR

        public FlowNwGraph()
            :base()
        {
            this.UseLayoutRounding = true;
            this.MouseUp += FlowNwGraph_MouseUp;
            this.Loaded += FlowNwGraph_Loaded;
        }

        

        #endregion

        #region METHODS: Update Display According to Network to Display

        protected void PopulateCanvas()
        {
            // reset
            this.Children.Clear();

            // place the nodes
            Dictionary<long, FlNwNodeVisualization> node_vis_list = new Dictionary<long, FlNwNodeVisualization>();
            foreach (var entry in this.NetworkToDisplay.ContainedNodes)
            {
                FlNetNode n = entry.Value;
                if (n == null) continue;

                FlNwNodeVisualization n_vis = new FlNwNodeVisualization(this, n, this.GetNodeRoleInNetwork(n));
                node_vis_list.Add(n.ID, n_vis);
            }
            // place the sub-networks
            foreach (var entry in this.NetworkToDisplay.ContainedFlowNetworks)
            {
                FlowNetwork nw = entry.Value;
                if (nw == null) continue;

                FlNwNetworkVisualization nw_vis = new FlNwNetworkVisualization(this, nw, this.GetNodeRoleInNetwork(nw));
                node_vis_list.Add(nw.ID, nw_vis);
            }
            // place the edges
            foreach (var entry in this.NetworkToDisplay.ContainedEdges)
            {
                FlNetEdge e = entry.Value;
                if (e == null) continue;

                FlNwEdgeVisualization e_vis = new FlNwEdgeVisualization(this, this.NetworkToDisplay.ContainedEdges[e.ID],
                                                                        node_vis_list[e.Start.ID], node_vis_list[e.End.ID]);
            }

            if (this.NetworkToDisplay.ContainedNodes.Count() + this.NetworkToDisplay.ContainedFlowNetworks.Count() > 0)
                this.FitSize2Content();
        }

        #endregion

        #region METHODS: Add Children

        public void AddChild(UIElement _child, Point _anchor, double _width, double _height)
        {
            if (_child == null) return;
            _child.MouseDown += element_MouseDown;
            _child.MouseMove += element_MouseMove;
            _child.MouseUp += element_MouseUp;
            this.Children.Add(_child);

            // check position of child and mark this for resizing, if necessary
            this.TestCanvasAgainstPointForExpansion(_anchor, new Vector(_width, _height));
        }

        public void AddNode(Point _position)
        {
            if (this.NetworkToDisplay == null) return;

            long id_created = this.NetworkToDisplay.AddNode(_position);
            if (id_created >= 0)
            {
                // show it on the canvas...
                FlNwNodeVisualization n = new FlNwNodeVisualization(this, this.NetworkToDisplay.ContainedNodes[id_created], NodePosInFlow.INTERIOR);
            }
        }

        public void AddEdge(FlNwNodeVisualization _start, FlNwNodeVisualization _end)
        {
            if (_start == null || _end == null) return;
            if (_start.FN_Node == null || _end.FN_Node == null) return;

            long id_created = this.NetworkToDisplay.AddEdge(_start.FN_Node, _end.FN_Node);
            if (id_created >= 0)
            {
                // show it on the canvas...
                FlNwEdgeVisualization e = new FlNwEdgeVisualization(this, this.NetworkToDisplay.ContainedEdges[id_created],
                                                                        _start, _end);
            }

        }

        public void AddNetwork(Point _position)
        {
            if (this.NetworkToDisplay == null) return;
            if (this.CompFactory == null) return;

            long id_created = this.CompFactory.AddNetworkToNetwork(this.NetworkToDisplay, _position, "NW", "- - -");
            if (id_created >= 0)
            {
                // show it on the canvas...
                FlNwNetworkVisualization nw = new FlNwNetworkVisualization(this, this.NetworkToDisplay.ContainedFlowNetworks[id_created], NodePosInFlow.INTERIOR);
            }
        }

        #endregion

        #region METHODS: Remove Children

        public void RemoveNode(FlNwNodeVisualization _node)
        {
            if (_node == null) return;
            if (this.NetworkToDisplay == null) return;

            bool success = false;

            if (_node is FlNwNetworkVisualization && this.CompFactory != null)
            {
                FlNwNetworkVisualization nw_vis = _node as FlNwNetworkVisualization;
                if (nw_vis != null && nw_vis.FN_NW != null)
                    success = this.CompFactory.RemoveNetwork(nw_vis.FN_NW);
            }
            else
                success = this.NetworkToDisplay.RemoveNode(_node.FN_Node);

            if (success)
            {
                // remove all attached edge visualizations
                List<FlNwEdgeVisualization> to_remove = new List<FlNwEdgeVisualization>();
                foreach(object child in this.Children)
                {
                    FlNwEdgeVisualization edge_vis = child as FlNwEdgeVisualization;
                    if (edge_vis == null) continue;

                    if (edge_vis.StartVis != null && edge_vis.StartVis.FN_Node.ID == _node.FN_Node.ID)
                        to_remove.Add(edge_vis);
                    if (edge_vis.EndVis != null && edge_vis.EndVis.FN_Node.ID == _node.FN_Node.ID)
                        to_remove.Add(edge_vis);
                }
                foreach(FlNwEdgeVisualization item in to_remove)
                {
                    item.Detach();
                    this.Children.Remove(item);
                }
                // remove the node visualization
                this.Children.Remove(_node);
            }
        }


        public void RemoveEdge(FlNwEdgeVisualization _edge)
        {
            if (_edge == null) return;
            if (this.NetworkToDisplay == null) return;

            bool success = this.NetworkToDisplay.RemoveEdge(_edge.FN_Edge);
            if (success)
            {
                // remove the edge visualization
                _edge.Detach();
                this.Children.Remove(_edge);
            }
        }

        #endregion

        #region METHODS: Traverse Children, Replace Children

        public void GoToParentNetwork()
        {
            if (this.NetworkToDisplay == null || this.CompFactory == null) return;

            List<FlowNetwork> parent_chain = this.CompFactory.GetParentNetworkChain(this.NetworkToDisplay);
            
            int nr_parents = parent_chain.Count;
            if (nr_parents < 2) return;

            FlowNetwork immediate_parent = parent_chain[nr_parents - 2];
            this.NetworkToDisplay = immediate_parent;
            this.CompFactory.SelectNetwork(immediate_parent.ID);
        }

        internal void ConvertNodeToNetwork(FlNwNodeVisualization _node_vis)
        {
            if (this.CompFactory == null || this.NetworkToDisplay == null || _node_vis == null) return;
            if (_node_vis.FN_Node == null) return;

            // safety
            MessageBoxResult result = MessageBox.Show("Möchten Sie wirklich den Knoten in ein Netzwerk umwandeln?", 
                                                      "Knoten Bearbeiten", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) return;

            // extract affected edge visualizations
            List<FlNwEdgeVisualization> affected_start = new List<FlNwEdgeVisualization>();
            List<FlNwEdgeVisualization> affected_end = new List<FlNwEdgeVisualization>();
            foreach(object child in this.Children)
            {
                FlNwEdgeVisualization edge_vis = child as FlNwEdgeVisualization;
                if (edge_vis == null) continue;
                if (edge_vis.FN_Edge == null) continue;

                if (_node_vis.FN_Node.Edges_In.Contains(edge_vis.FN_Edge))
                    affected_end.Add(edge_vis);
                if (_node_vis.FN_Node.Edges_Out.Contains(edge_vis.FN_Edge))
                    affected_start.Add(edge_vis);
            }

            // perform conversion
            FlowNetwork network = this.CompFactory.ConvertNodeToNetwork(this.NetworkToDisplay, _node_vis.FN_Node);
            if (network == null) return;

            this.Children.Remove(_node_vis);
            FlNwNetworkVisualization nw_vis = new FlNwNetworkVisualization(this, network, this.GetNodeRoleInNetwork(network));

            // re-establish connection btw node and edges
            foreach(FlNwEdgeVisualization e_vis in affected_start)
            {
                e_vis.RedirectAfterConversion(nw_vis, true);
            }
            foreach (FlNwEdgeVisualization e_vis in affected_end)
            {
                e_vis.RedirectAfterConversion(nw_vis, false);
            }
        }

        internal void ConvertNetworkToNode(FlNwNetworkVisualization _nw_vis)
        {
            if (this.CompFactory == null || this.NetworkToDisplay == null || _nw_vis == null) return;
            if (_nw_vis.FN_NW == null) return;

            // safety
            MessageBoxResult result = MessageBox.Show("Möchten Sie wirklich dieses Netzwerk in ein Knoten umwandeln?",
                                                      "Netzwerk Bearbeiten", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) return;

            // extract affected edge visualizations
            List<FlNwEdgeVisualization> affected_start = new List<FlNwEdgeVisualization>();
            List<FlNwEdgeVisualization> affected_end = new List<FlNwEdgeVisualization>();
            foreach (object child in this.Children)
            {
                FlNwEdgeVisualization edge_vis = child as FlNwEdgeVisualization;
                if (edge_vis == null) continue;
                if (edge_vis.FN_Edge == null) continue;

                if (_nw_vis.FN_NW.Edges_In.Contains(edge_vis.FN_Edge))
                    affected_end.Add(edge_vis);
                if (_nw_vis.FN_NW.Edges_Out.Contains(edge_vis.FN_Edge))
                    affected_start.Add(edge_vis);
            }

            // perform conversion
            FlNetNode node = this.CompFactory.ConvertNetworkToNode(this.NetworkToDisplay, _nw_vis.FN_NW);
            if (node == null) return;

            this.Children.Remove(_nw_vis);
            FlNwNodeVisualization node_vis = new FlNwNodeVisualization(this, node, this.GetNodeRoleInNetwork(node));

            // re-establish connection btw node and edges
            foreach (FlNwEdgeVisualization e_vis in affected_start)
            {
                e_vis.RedirectAfterConversion(node_vis, true);
            }
            foreach (FlNwEdgeVisualization e_vis in affected_end)
            {
                e_vis.RedirectAfterConversion(node_vis, false);
            }
        }


        #endregion

        #region METHODS: Sources n Sinks

        protected void SwitchNetworkSource(FlNwNodeVisualization _node)
        {
            if (_node == null) return;
            if (_node.FN_Node == null) return;
            if (this.NetworkToDisplay == null) return;
            
            // find current SOURCE node visualization
            FlNwNodeVisualization source_old = null;
            if (this.NetworkToDisplay.NodeStart_ID >= 0)
            {
                foreach(object item in this.Children)
                {
                    FlNwNodeVisualization nv = item as FlNwNodeVisualization;
                    if (nv == null) continue;
                    if (nv.FN_Node == null) continue;

                    if (nv.FN_Node.ID == this.NetworkToDisplay.NodeStart_ID)
                    {
                        source_old = nv;
                        break;
                    }
                }
            }

            // switch
            this.NetworkToDisplay.NodeStart_ID = _node.FN_Node.ID;
            if (source_old != null)
                source_old.PosInFlow = NodePosInFlow.INTERIOR;
            _node.PosInFlow = NodePosInFlow.SOURCE;

            // update
            if (source_old != null)
                source_old.UpdateContent();
            _node.UpdateContent();
        }

        protected void SwitchNetworkSink(FlNwNodeVisualization _node)
        {
            if (_node == null) return;
            if (_node.FN_Node == null) return;
            if (this.NetworkToDisplay == null) return;

            // find current SINK node visualization
            FlNwNodeVisualization sink_old = null;
            if (this.NetworkToDisplay.NodeEnd_ID >= 0)
            {
                foreach (object item in this.Children)
                {
                    FlNwNodeVisualization nv = item as FlNwNodeVisualization;
                    if (nv == null) continue;
                    if (nv.FN_Node == null) continue;

                    if (nv.FN_Node.ID == this.NetworkToDisplay.NodeEnd_ID)
                    {
                        sink_old = nv;
                        break;
                    }
                }
            }

            // switch
            this.NetworkToDisplay.NodeEnd_ID = _node.FN_Node.ID;
            if (sink_old != null)
                sink_old.PosInFlow = NodePosInFlow.INTERIOR;
            _node.PosInFlow = NodePosInFlow.SINK;

            // update
            if (sink_old != null)
                sink_old.UpdateContent();
            _node.UpdateContent();
        }

        #endregion

        #region METHODS: Decorations

        private void SetSizeLabel()
        {
            if (this.canvas_size_label != null)
                this.Children.Remove(this.canvas_size_label);
            
            // label the lower right corner
            TextBlock tb_LR = new TextBlock();
            tb_LR.Width = 100;
            tb_LR.Height = 25;
            TranslateTransform transf = new TranslateTransform(this.Width - tb_LR.Width, this.Height - tb_LR.Height);
            tb_LR.RenderTransform = transf;

            tb_LR.Text = "(" + this.Width.ToString("F2", FlowNwGraph.NR_FORMATTER) + " " + this.Height.ToString("F2", FlowNwGraph.NR_FORMATTER) + ")";
            tb_LR.Foreground = new SolidColorBrush(FlowNwGraph.TICK_COLOR);
            tb_LR.FontSize = 12;
            tb_LR.Padding = new Thickness(2);
            tb_LR.IsHitTestVisible = false;

            this.canvas_size_label = tb_LR;
            this.Children.Add(tb_LR);           
        }

        private void UpdateTicks()
        {
            if (this.canvas_size_ticks != null)
            {
                foreach (Line tick in this.canvas_size_ticks)
                {
                    this.Children.Remove(tick);
                }
            }
            this.canvas_size_ticks = new List<Line>();


            // the X ticks
            int nr_ticks_X = (int)Math.Floor(this.Width / FlowNwGraph.TICK_SPACING);
            for (int i = 0; i < nr_ticks_X; i++)
            {
                Line tick = new Line();
                tick.X1 = i * FlowNwGraph.TICK_SPACING;
                double y1_pos = (i % 2 == 0) ? this.Height - FlowNwGraph.TICK_HEIGHT * 1.5 : this.Height - FlowNwGraph.TICK_HEIGHT;
                tick.Y1 = Math.Floor(y1_pos);
                tick.X2 = tick.X1;
                tick.Y2 = this.Height;
                tick.Stroke = new SolidColorBrush(FlowNwGraph.TICK_COLOR);
                tick.StrokeThickness = 1;
                tick.IsHitTestVisible = false;

                this.canvas_size_ticks.Add(tick);
                this.Children.Add(tick);
            }

            // the Y ticks
            int nr_ticks_Y = (int)Math.Floor(this.Height / FlowNwGraph.TICK_SPACING);
            for (int i = 0; i < nr_ticks_Y; i++)
            {
                Line tick = new Line();
                double x1_pos = (i % 2 == 0) ? this.Width - FlowNwGraph.TICK_HEIGHT * 1.5 : this.Width - FlowNwGraph.TICK_HEIGHT;
                tick.X1 = (int)Math.Floor(x1_pos);
                tick.Y1 = i * FlowNwGraph.TICK_SPACING;
                tick.X2 = this.Width;
                tick.Y2 = tick.Y1;
                tick.Stroke = new SolidColorBrush(FlowNwGraph.TICK_COLOR);
                tick.StrokeThickness = 1;
                tick.IsHitTestVisible = false;

                this.canvas_size_ticks.Add(tick);
                this.Children.Add(tick);
            }

            // the canvas UpperLeft corner
            Line tick_UL_x = new Line();
            tick_UL_x.X1 = 1;
            tick_UL_x.Y1 = 1;
            tick_UL_x.X2 = FlowNwGraph.TICK_HEIGHT * 3 + 1;
            tick_UL_x.Y2 = 1;
            tick_UL_x.Stroke = new SolidColorBrush(FlowNwGraph.TICK_COLOR);
            tick_UL_x.StrokeThickness = 1;
            tick_UL_x.IsHitTestVisible = false;

            this.canvas_size_ticks.Add(tick_UL_x);
            this.Children.Add(tick_UL_x);

            Line tick_UL_y = new Line();
            tick_UL_y.X1 = 1;
            tick_UL_y.Y1 = 1;
            tick_UL_y.X2 = 1;
            tick_UL_y.Y2 = FlowNwGraph.TICK_HEIGHT * 3 + 1;
            tick_UL_y.Stroke = new SolidColorBrush(FlowNwGraph.TICK_COLOR);
            tick_UL_y.StrokeThickness = 1;
            tick_UL_y.IsHitTestVisible = false;

            this.canvas_size_ticks.Add(tick_UL_y);
            this.Children.Add(tick_UL_y);

            // the canvas LowerRight corner
            Line tick_LR_x = new Line();
            tick_LR_x.X1 = this.Width - FlowNwGraph.TICK_HEIGHT * 1.5 - 1;
            tick_LR_x.Y1 = this.Height - 1;
            tick_LR_x.X2 = this.Width - 1;
            tick_LR_x.Y2 = this.Height - 1;
            tick_LR_x.Stroke = new SolidColorBrush(FlowNwGraph.TICK_COLOR);
            tick_LR_x.StrokeThickness = 1;
            tick_LR_x.IsHitTestVisible = false;

            this.canvas_size_ticks.Add(tick_LR_x);
            this.Children.Add(tick_LR_x);

            Line tick_LR_y = new Line();
            tick_LR_y.X1 = this.Width - 1;
            tick_LR_y.Y1 = this.Height - FlowNwGraph.TICK_HEIGHT * 1.5 - 1;
            tick_LR_y.X2 = this.Width - 1;
            tick_LR_y.Y2 = this.Height - 1;
            tick_LR_y.Stroke = new SolidColorBrush(FlowNwGraph.TICK_COLOR);
            tick_LR_y.StrokeThickness = 1;
            tick_LR_y.IsHitTestVisible = false;

            this.canvas_size_ticks.Add(tick_LR_y);
            this.Children.Add(tick_LR_y);
        }

        #endregion

        #region METHODS: Common Parent

        public void ShowCommonNodeContentParent()
        {
            if (this.CompFactory == null) return;

            // gather information
            Dictionary<FlNwNodeVisualization, long> id_of_parent = new Dictionary<FlNwNodeVisualization, long>();
            foreach(object child in this.Children)
            {
                FlNwNodeVisualization node_vis = child as FlNwNodeVisualization;
                if (node_vis == null) continue;
                if (node_vis.FN_Node == null) continue;
                if (node_vis.FN_Node.Content == null) continue;

                ParameterStructure.Component.Component closest_parent = null;
                List<ParameterStructure.Component.Component> parent_chain = this.CompFactory.GetParentComponentChain(node_vis.FN_Node.Content);
                if (parent_chain.Count > 1)
                    closest_parent = parent_chain[parent_chain.Count - 2];

                if (closest_parent != null)
                    id_of_parent.Add(node_vis, closest_parent.ID);
            }

            // check for common ancestor and set extents
            Dictionary<long, int> nr_nodes_affected = new Dictionary<long, int>();
            Dictionary<long, Point> upper_lefts = new Dictionary<long, Point>();
            Dictionary<long, Point> lower_rights = new Dictionary<long, Point>();
            foreach(var entry in id_of_parent)
            {
                long id = entry.Value;
                if (nr_nodes_affected.ContainsKey(id))
                {
                    nr_nodes_affected[id] += 1;
                    
                    Point ul_old = upper_lefts[id];
                    upper_lefts[id] = new Point(Math.Min(ul_old.X, entry.Key.Position.X), Math.Min(ul_old.Y, entry.Key.Position.Y));

                    Point lr_old = lower_rights[id];
                    Point lr_current = entry.Key.Position +
                        new Vector(FlNwElementVisualization.NODE_WIDTH_DEFAULT, FlNwElementVisualization.NODE_HEIGHT_DEFAULT);
                    lower_rights[id] = new Point(Math.Max(lr_old.X, lr_current.X), Math.Max(lr_old.Y, lr_current.Y));
                }
                else
                {
                    nr_nodes_affected.Add(id, 1);
                    upper_lefts.Add(id, entry.Key.Position);
                    lower_rights.Add(id, entry.Key.Position + 
                        new Vector(FlNwElementVisualization.NODE_WIDTH_DEFAULT, FlNwElementVisualization.NODE_HEIGHT_DEFAULT));
                }
            }

            // extract entries w more than 1 node affected
            this.marking_boxes = new List<Rectangle>();
            for(int i = 0; i < nr_nodes_affected.Count; i++)
            {
                if (nr_nodes_affected.ElementAt(i).Value < 2) continue;

                Rectangle frame = new Rectangle();
                TranslateTransform transf = new TranslateTransform(upper_lefts.ElementAt(i).Value.X - FlNwElementVisualization.NODE_STROKE_THICKNESS_MANIPULATED,
                                                                   upper_lefts.ElementAt(i).Value.Y - FlNwElementVisualization.NODE_STROKE_THICKNESS_MANIPULATED);
                frame.RenderTransform = transf;
                frame.Width = Math.Abs(lower_rights.ElementAt(i).Value.X - upper_lefts.ElementAt(i).Value.X) + FlNwElementVisualization.NODE_STROKE_THICKNESS_MANIPULATED * 2;
                frame.Height = Math.Abs(lower_rights.ElementAt(i).Value.Y - upper_lefts.ElementAt(i).Value.Y) + FlNwElementVisualization.NODE_STROKE_THICKNESS_MANIPULATED * 2;
                frame.Stroke = new SolidColorBrush(FlNwElementVisualization.NODE_COLOR_STROKE_ACTIVE);
                frame.StrokeThickness = 1;
                frame.Fill = new SolidColorBrush(FlNwElementVisualization.NODE_COLOR_FILL_ACTIVE_4tr);
                double radius = Math.Floor(FlNwElementVisualization.NODE_HEIGHT_DEFAULT * 0.25);
                frame.RadiusX = radius;
                frame.RadiusY = radius;
                frame.IsHitTestVisible = false;

                this.marking_boxes.Add(frame);
                this.Children.Add(frame);
                Canvas.SetZIndex(frame, -1);
            }
        }


        #endregion

        #region METHODS: Snap to Grid

        public void AlignContentToGrid()
        {
            // gather positions
            Dictionary<object, double> positions_x = new Dictionary<object, double>();
            Dictionary<object, double> positions_y = new Dictionary<object, double>();
            foreach (object child in this.Children)
            {
                FlNwElementVisualization element = child as FlNwElementVisualization;
                if (element == null) continue;
                if (element is FlNwEdgeVisualization) continue;

                positions_x.Add(element, element.Position.X);
                positions_y.Add(element, element.Position.Y);
            }

            //// test centroid extraction
            //List<double> test = new List<double> { 2.5, 10, 14.3, 15, 1, -3, -4.5, 11, 18, 2, 17.5 };
            //double test_tolerance = 1.5;
            //List<double> test_centroids = FlowNwGraph.InitClusterCentroids(test, test_tolerance);

            Dictionary<double, List<object>> clustered_x = FlowNwGraph.Cluster1D(positions_x, FlowNwGraph.SNAP_MAGNET);
            Dictionary<double, List<object>> clustered_y = FlowNwGraph.Cluster1D(positions_y, FlowNwGraph.SNAP_MAGNET);

            // perform alignment
            Dictionary<FlNwElementVisualization, Point> positions_snapped = new Dictionary<FlNwElementVisualization, Point>();
            foreach (var entry in clustered_x)
            {
                double x_val = entry.Key;
                foreach(object obj in entry.Value)
                {
                    FlNwElementVisualization element = obj as FlNwElementVisualization;
                    if (element == null) continue;

                    positions_snapped.Add(element, new Point(x_val, element.Position.Y));
                }   
            }
            foreach (var entry in clustered_y)
            {
                double y_val = entry.Key;
                foreach (object obj in entry.Value)
                {
                    FlNwElementVisualization element = obj as FlNwElementVisualization;
                    if (element == null) continue;
                    if (!(positions_snapped.ContainsKey(element)))
                        positions_snapped.Add(element, new Point(element.Position.X, y_val));
                    else
                        positions_snapped[element] = new Point(positions_snapped[element].X, y_val);
                }
            }

            foreach(var entry in positions_snapped)
            {
                Vector offset = entry.Value - entry.Key.Position;
                entry.Key.Translate(offset);
            }

            // adapt arrows
            foreach (object child in this.Children)
            {
                FlNwEdgeVisualization edge = child as FlNwEdgeVisualization;
                if (edge == null) continue;

                edge.UpdateVisual();
            }
        }


        #endregion

        #region METHODS: Resizing

        public void ExpandCanvas(double _width_exp, double _height_exp)
        {
            this.Width += _width_exp;
            this.Height += _height_exp;
            this.SetSizeLabel();
            this.UpdateTicks();
        }

        public void ExpandCanvasNeg(double _width_exp, double _height_exp)
        {
            this.canvas_expand = new Vector(Math.Abs(_width_exp), Math.Abs(_height_exp));
            this.content_offset = this.canvas_expand;
            this.AdaptSize2Content();
        }

        public void FitSize2Content()
        {
            // test for expansion
            foreach (object child in this.Children)
            {
                FlNwElementVisualization element = child as FlNwElementVisualization;
                if (element == null) continue;
                if (element is FlNwEdgeVisualization) continue;

                this.TestCanvasAgainstPointForExpansion(element.Position, new Vector(element.Width, element.Height));
            }

            // test for contraction
            if (this.canvas_expand.X == 0 && this.canvas_expand.Y == 0)
            {
                this.canvas_expand = new Vector(this.Width, this.Height);
                foreach (object child in this.Children)
                {
                    FlNwElementVisualization element = child as FlNwElementVisualization;
                    if (element == null) continue;
                    if (element is FlNwEdgeVisualization) continue;

                    this.TestCanvasAgainstPointForContraction(element.Position, new Vector(element.Width, element.Height));
                }
                this.canvas_expand = -this.canvas_expand;
            }

            this.AdaptSize2Content();
        }

        protected void AdaptSize2Content()
        {
            if (this.canvas_expand.X == 0 && this.canvas_expand.Y == 0) return;
            this.Width += this.canvas_expand.X;
            this.Height += this.canvas_expand.Y;
            this.canvas_expand = new Vector(0, 0);
            this.SetSizeLabel();
            this.UpdateTicks();

            if (this.content_offset.X == 0 && this.content_offset.Y == 0) return;
            if (this.NetworkToDisplay == null) return;

            foreach (object child in this.Children)
            {
                FlNwElementVisualization element = child as FlNwElementVisualization;
                if (element == null) continue;
                if (element is FlNwEdgeVisualization) continue;

                element.Translate(this.content_offset);
            }
            foreach(object child in this.Children)
            {
                FlNwEdgeVisualization edge = child as FlNwEdgeVisualization;
                if (edge == null) continue;

                edge.UpdateVisual();
            }

            this.content_offset = new Vector(0, 0);
        }

        protected void TestCanvasAgainstPointForExpansion(Point _p, Vector _size)
        {
            if (_p.X < 0)
            {
                this.canvas_expand.X = Math.Max(this.canvas_expand.X, -_p.X);
                this.content_offset.X = Math.Max(this.content_offset.X, -_p.X);
            }
            else if (_p.X + _size.X + FlowNwGraph.CONTENT_PADDING > this.Width)
            {
                this.canvas_expand.X = Math.Max(this.canvas_expand.X, _p.X + _size.X + FlowNwGraph.CONTENT_PADDING - this.Width);
            }

            if (_p.Y < 0)
            {
                this.canvas_expand.Y = Math.Max(this.canvas_expand.Y, -_p.Y);
                this.content_offset.Y = Math.Max(this.content_offset.Y, -_p.Y);
            }
            else if (_p.Y + _size.Y + FlowNwGraph.CONTENT_PADDING > this.Height)
            {
                this.canvas_expand.Y = Math.Max(this.canvas_expand.Y, _p.Y + _size.Y + FlowNwGraph.CONTENT_PADDING - this.Height);
            }
        }

        protected void TestCanvasAgainstPointForContraction(Point _p, Vector _size)
        {
            if (_p.X + _size.X + FlowNwGraph.CONTENT_PADDING < this.Width)
                this.canvas_expand.X = Math.Min(this.canvas_expand.X, Math.Abs(_p.X + _size.X + FlowNwGraph.CONTENT_PADDING - this.Width));
            else
                this.canvas_expand.X = 0;

            if (_p.Y + _size.Y + FlowNwGraph.CONTENT_PADDING < this.Height)
                this.canvas_expand.Y = Math.Min(this.canvas_expand.Y, Math.Abs(_p.Y + _size.Y + FlowNwGraph.CONTENT_PADDING - this.Height));
            else
                this.canvas_expand.Y = 0;
        }

        #endregion

        #region METHODS: Saving

        public void SaveCanvasAsImage()
        {
            // adapt canvas size to content
            this.FitSize2Content();
            // save
            Brush old_bgr = this.Background;
            this.Background = new SolidColorBrush(Colors.Transparent);

            WpfUtils.CanvasUtils.SaveCanvasAsImage(this, "Netzwerk-Graph", 192d);

            this.Background = old_bgr;
        }

        #endregion

        #region METHODS: Selection, Display

        internal void SelectNetwork(FlNwNetworkVisualization _to_be_selected)
        {
            if (_to_be_selected == null || this.CompFactory == null) return;

            this.CompFactory.SelectNetwork(_to_be_selected.FN_NW.ID);
        }

        internal void SelectContent(ParameterStructure.Component.Component _content)
        {
            if (_content == null || this.CompFactory == null) return;

            this.last_selected_comp = _content;
            this.CompFactory.SelectComponent(_content);
        }

        public void TurnAllContainedComponentsOn()
        {
            if (this.CompFactory == null) return;
            if (this.NetworkToDisplay == null) return;
            this.CompFactory.TurnAllContentOn(this.NetworkToDisplay);
        }

        public void HighlighContainedComponents(bool _on)
        {
            if (this.CompFactory == null) return;
            if (this.NetworkToDisplay == null) return;
            this.CompFactory.HighlightAllContent(this.NetworkToDisplay, _on);
        }

        public void HighlightInstancesOfContainedComponent(bool _on)
        {
            if (this.CompFactory == null) return;
            if (this.NetworkToDisplay == null) return;

            if (_on)
            {
                if (this.last_selected_comp != null)
                {
                    List < FlNetElement >  containers = this.CompFactory.GetFlNwElementsContainingInstancesOf(this.NetworkToDisplay, this.last_selected_comp);
                    // gather visualisations to be highlighted
                    List<FlNwElementVisualization> container_vis = new List<FlNwElementVisualization>();
                    foreach (object child in this.Children)
                    {
                        FlNwEdgeVisualization edge_vis = child as FlNwEdgeVisualization;
                        FlNwNodeVisualization node_vis = child as FlNwNodeVisualization;
                        FlNwNetworkVisualization nw_vis = child as FlNwNetworkVisualization;
                        if (edge_vis == null && node_vis == null) continue;
                        if (nw_vis != null) continue;

                        if (edge_vis != null)
                        {
                            if (edge_vis.FN_Edge != null && containers.Contains(edge_vis.FN_Edge))
                                container_vis.Add(edge_vis);
                        }
                        else if (node_vis != null)
                        {
                            if (node_vis.FN_Node != null && containers.Contains(node_vis.FN_Node))
                                container_vis.Add(node_vis);
                        }

                    }

                    // perform highlighting
                    foreach(FlNwElementVisualization vis in container_vis)
                    {
                        vis.VisState |= ElementVisHighlight.Highlighted;
                    }
                }
            }
            else
            {
                // turn all off
                foreach (object child in this.Children)
                {
                    FlNwElementVisualization vis = child as FlNwElementVisualization;
                    if (vis != null)
                        vis.VisState &= ~ElementVisHighlight.Highlighted;
                }
            }
        }

        public bool CanExecute_HighlightInstancesOfContainedComponent()
        {
            return this.last_selected_comp != null;
        }

        #endregion

        #region METHODS: Flow Management

        public void SynchFlows(bool _in_flow_dir)
        {
            if (this.NetworkToDisplay == null) return;
            // this.NetworkToDisplay.SynchAllFlows(_in_flow_dir); // OLD
            this.NetworkToDisplay.CalculateAllFlows(_in_flow_dir);
        }

        public void ResetFlows()
        {
            if (this.NetworkToDisplay == null) return;
            // this.NetworkToDisplay.ResetAllContent(); // OLD
            this.NetworkToDisplay.ResetAllContentInstances(new Point(0, 0));

            // added 27.09.2017 to enable flow calculation step by step
            this.flow_calc_step_by_step_all_sorted_nodes = new List<FlNetNode>();
            this.flow_calc_step_by_step_ready = false;
            this.flow_calc_step_by_step_index = -1;
        }

        public List<string> GetUniqueParamNamesInNW()
        {
            if (this.NetworkToDisplay == null) return new List<string>();
            return this.NetworkToDisplay.GetUniquePAramNamesInContent();
        }

        // --------------------------------------------- //

        public void SynchFlows_1Step(bool _in_flow_dir, string _suffix_to_display)
        {
            if (this.NetworkToDisplay == null) return;
            if (!this.flow_calc_step_by_step_ready)
            {
                this.flow_calc_step_by_step_all_sorted_nodes = this.NetworkToDisplay.PrepareToCalculateFlowStepByStep(_in_flow_dir);
                this.flow_calc_step_by_step_ready = (this.flow_calc_step_by_step_all_sorted_nodes != null && this.flow_calc_step_by_step_all_sorted_nodes.Count > 0);
            }

            if (this.flow_calc_step_by_step_ready)
            {
                // calculate flow
                this.flow_calc_step_by_step_index++;
                FlNetNode current_node = null;
                string feedback = this.NetworkToDisplay.CalculateFlowStep(this.flow_calc_step_by_step_all_sorted_nodes, _in_flow_dir, this.flow_calc_step_by_step_index, true, out current_node);
                
                // update the param value display
                this.NetworkToDisplay.Suffix_To_Display = _suffix_to_display;
                
                // highlighting display
                FlNwNodeVisualization current_node_vis = null;
                if (current_node != null)
                {
                    // highlight visualisation
                    foreach(object item in this.Children)
                    {
                        FlNwNodeVisualization node_vis = item as FlNwNodeVisualization;
                        if (node_vis == null) continue;

                        if (node_vis.FN_Node.ID == current_node.ID)
                        {
                            current_node_vis = node_vis;
                            break;
                        }
                    }
                    if (current_node_vis != null)
                        current_node_vis.VisState |= ElementVisHighlight.Highlighted;
                }
                MessageBox.Show(feedback, "Stromberechnung, Schritt " + this.flow_calc_step_by_step_index, MessageBoxButton.OK, MessageBoxImage.Information);

                // un-highlight visualisation
                if (current_node_vis != null)
                    current_node_vis.VisState &= ~ElementVisHighlight.Highlighted;
            }
        }

        #endregion

        #region UTILS : 2D Clustering

        protected static List<double> InitClusterCentroids(List<double> _values, double _tolerance)
        {
            if (_values == null) return new List<double>();
            if (_values.Count == 0) return new List<double>();

            List<double> centroids = new List<double>();

            int nr_values = _values.Count;
            _values.Sort();

            double centroid = _values[0];
            double centroid_start = _values[0];
            int counter = 1;
            for (int i = 1; i < nr_values; i++ )
            {
                double dist = Math.Abs(centroid_start - _values[i]);
                if (dist <= _tolerance * 2)
                {
                    centroid += _values[i];
                    counter++;
                }
                else
                {
                    centroid /= counter;
                    centroids.Add(centroid);
                    centroid = _values[i];
                    centroid_start = _values[i];
                    counter = 1;
                }
            }

            // finalize
            centroid /= counter;
            centroids.Add(centroid);           

            return centroids;
        }

        protected static Dictionary<double, List<object>> Cluster1D(Dictionary<object, double> _named_values, double _tolerance)
        {
            Dictionary<double, List<object>> clustered_values = new Dictionary<double, List<object>>();

            if (_named_values == null) return clustered_values;
            if (_named_values.Count == 0) return clustered_values;

            int nr_values = _named_values.Count;
            var sorted_named_values = _named_values.OrderBy(x => x.Value);

            double centroid = sorted_named_values.ElementAt(0).Value;
            double centroid_start = centroid;
            List<object> named_values_of_centroid = new List<object> { sorted_named_values.ElementAt(0).Key };
            int counter = 1;
            
            for (int i = 1; i < nr_values; i++)
            {
                double val = sorted_named_values.ElementAt(i).Value;
                double dist = Math.Abs(centroid_start - val);
                if (dist <= _tolerance * 2)
                {
                    centroid += val;
                    counter++;
                    named_values_of_centroid.Add(sorted_named_values.ElementAt(i).Key);
                }
                else
                {
                    centroid /= counter;
                    if (clustered_values.ContainsKey(centroid))
                        clustered_values[centroid].AddRange(named_values_of_centroid);
                    else
                        clustered_values.Add(centroid, named_values_of_centroid);
                    
                    centroid = val;
                    centroid_start = val;
                    named_values_of_centroid = new List<object> { sorted_named_values.ElementAt(i).Key };
                    counter = 1;
                }
            }

            // finalize           
            centroid /= counter;
            if (clustered_values.ContainsKey(centroid))
                clustered_values[centroid].AddRange(named_values_of_centroid);
            else
                clustered_values.Add(centroid, named_values_of_centroid);            

            return clustered_values;
        }

        
        // general algorithm
        protected static void ClusterKMean(Dictionary<object, double> _named_values, int _nr_clusters, int _max_nr_iterations,
                                           out Dictionary<int, List<object>> _clusters, out List<double> _cluster_centroids)
        {
            _clusters = new Dictionary<int, List<object>>();
            _cluster_centroids = new List<double>();
            if (_named_values == null) return;
            if (_named_values.Count == 0) return;
            if (_nr_clusters < 1) return;
            if (_nr_clusters == 1)
            {
                _clusters.Add(0, _named_values.Keys.ToList());
                return;
            }

            int nr_values = _named_values.Count;
            List<double> cluster_centroids_prev = new List<double>();
            
            // init the cluster centroids
            Random rnd = new Random();
            for (int i = 0; i < _nr_clusters; i++)
            {
                int index = rnd.Next(0, nr_values);
                cluster_centroids_prev.Add(double.MaxValue);
                _cluster_centroids.Add(_named_values.ElementAt(index).Value);
                _clusters.Add(i, new List<object>());
            }

            // iterate
            int iteration_counter = 0;
            double change_sum = double.MaxValue;
            while (iteration_counter < _max_nr_iterations && change_sum > FlowNwGraph.CLUSTERING_CHANGE_CUTOFF)
            {
                iteration_counter++;
                change_sum = 0;

                // reset clusters
                for (int i = 0; i < _nr_clusters; i++)
                {
                    _clusters[i].Clear();
                }

                // assign value to cluster
                foreach (var entry in _named_values)
                {
                    double v = entry.Value;
                    List<double> distances = new List<double>();
                    foreach (double c in _cluster_centroids)
                    {
                        distances.Add(Math.Abs(v - c));
                    }
                    int min_index = distances.FindIndex(x => x == distances.Min());
                    _clusters[min_index].Add(entry.Key);
                }

                // re-evaluate clusters
                cluster_centroids_prev = new List<double>(_cluster_centroids);
                for(int i = 0; i < _nr_clusters; i++)
                {
                    if (_clusters[i].Count == 0) continue;

                    double mean = 0;
                    foreach(object obj in _clusters[i])
                    {
                        mean += _named_values[obj];
                    }
                    mean /= _clusters[i].Count;

                    _cluster_centroids[i] = mean;
                    change_sum += Math.Abs(_cluster_centroids[i] - cluster_centroids_prev[i]);
                }
                
            }

            return;
        }



        #endregion

        #region UTILS: Switching State

        public void OnSwitchState(ManagementState _to_state)
        {
            // perform the switch
            // a repeat switches to NEUTRAL
            this.GuiState_Prev = this.GuiState;
            if (_to_state == this.GuiState)
                this.GuiState = ManagementState.NEUTRAL;
            else
                this.GuiState = _to_state;

            switch (this.GuiState)
            {
                case ManagementState.NEUTRAL:
                    this.ResetAllInteractiveVariables();
                    break;
                case ManagementState.ADD_NODE:
                    break;
                case ManagementState.ADD_EDGE:
                    break;
                case ManagementState.ADD_NETWORK:
                    break;
                case ManagementState.REMOVE:
                    break;
                case ManagementState.REROUTE_EDGE:
                    break;
                case ManagementState.ASSIGN_COMP:
                    break;
                case ManagementState.DEFINE_SIZE_OF_COMP:
                    break;
                case ManagementState.DEFINE_OPERATIONS_IN_NODE:
                    break;
                case ManagementState.COPY_OPERATION_TO_ALL_INST:
                    break;
            }
        }

        private void ResetAllInteractiveVariables()
        {
            // edge definition
            this.nr_nodes_for_edge_selected = 0;
            if (this.node_start != null)
                this.node_start.VisState &= ~ElementVisHighlight.Picked;
            this.node_start = null;
            if (this.node_end != null)
                this.node_end.VisState &= ~ElementVisHighlight.Picked;
            this.node_end = null;

            // edge rerouting
            if (this.edge_to_rerout != null)
                this.edge_to_rerout.VisState &= ~ElementVisHighlight.Picked;
             this.edge_to_rerout = null;
            if (edge_to_rerout_to_Node != null)
                this.edge_to_rerout_to_Node.VisState &= ~ElementVisHighlight.Picked;
             this.edge_to_rerout_to_Node = null;

            // component assignment
            if (this.element_to_assoc_w_comp != null)
                this.element_to_assoc_w_comp.VisState &= ~ElementVisHighlight.Picked;
            this.element_to_assoc_w_comp = null;
            this.comp_to_assign = null;
            this.IsPickingComp = false;
        }

        #endregion

        #region UTILS: Source & Sink visualization

        protected NodePosInFlow GetNodeRoleInNetwork(FlNetNode _node)
        {
            if (this.NetworkToDisplay == null || _node == null) return NodePosInFlow.INTERIOR;

            if (this.NetworkToDisplay.NodeStart_ID == _node.ID)
                return NodePosInFlow.SOURCE;
            else if (this.NetworkToDisplay.NodeEnd_ID == _node.ID)
                return NodePosInFlow.SINK;
            else
                return NodePosInFlow.INTERIOR;
        }

        #endregion

        #region EVENT HANDLERS : Graph

        protected void FlowNwGraph_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetSizeLabel();
            this.UpdateTicks();
        }

        protected void FlowNwGraph_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e == null) return;

            switch (this.GuiState)
            {
                case UIElements.ManagementState.NEUTRAL:
                    break;
                case UIElements.ManagementState.ADD_NODE:
                    Point pos = e.GetPosition(this);
                    this.AddNode(pos);
                    this.GuiState = UIElements.ManagementState.NEUTRAL;
                    break;
                case UIElements.ManagementState.ADD_EDGE:
                    // see element event handlers
                    this.ResetAllInteractiveVariables();
                    break;
                case UIElements.ManagementState.ADD_NETWORK:
                    Point pos_nw = e.GetPosition(this);
                    this.AddNetwork(pos_nw);
                    this.GuiState = UIElements.ManagementState.NEUTRAL;
                    break;
                case UIElements.ManagementState.REMOVE:
                    // see element event handlers
                    break;
                case ManagementState.REROUTE_EDGE:
                    // see element handlers
                    this.ResetAllInteractiveVariables();
                    break;
                case UIElements.ManagementState.ASSIGN_COMP:
                    break;
                case ManagementState.DEFINE_SIZE_OF_COMP:
                    break;
                case ManagementState.DEFINE_OPERATIONS_IN_NODE:
                    break;
                case ManagementState.COPY_OPERATION_TO_ALL_INST:
                    break;
            }
        }

        #endregion

        #region EVENT HANDLERS: Elements
        protected void element_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FlNwElementVisualization element = sender as FlNwElementVisualization;
            if (element == null || e == null) return;

            FlNwNodeVisualization node = sender as FlNwNodeVisualization;
            FlNwEdgeVisualization edge = sender as FlNwEdgeVisualization;

            switch(this.GuiState)
            {
                case ManagementState.NEUTRAL:
                    // start moving
                    if (node != null)
                    {
                        node.VisState |= ElementVisHighlight.Manipulated;
                        node.Anchor = e.GetPosition(this);
                    }
                    break;
                case ManagementState.ADD_EDGE:
                    // node selection under way:
                    if (node != null)
                    {
                        if (this.nr_nodes_for_edge_selected == 0)
                        {
                            this.node_start = node;
                            this.nr_nodes_for_edge_selected++;
                            node.VisState |= ElementVisHighlight.Picked;
                        }
                        else if (this.nr_nodes_for_edge_selected == 1)
                        {
                            if (this.node_start.FN_Node.ID != node.FN_Node.ID)
                            {
                                this.node_end = node;
                                this.nr_nodes_for_edge_selected++;
                                node.VisState |= ElementVisHighlight.Picked;
                            }
                        }
                    }
                    break;
                case ManagementState.REMOVE:
                    element.VisState |= ElementVisHighlight.Picked;
                    break;
                case ManagementState.REROUTE_EDGE:
                    if (edge != null && this.edge_to_rerout == null)
                    {
                        this.edge_to_rerout = edge;
                        this.edge_to_rerout_Start = edge.IsCloserToStart(e.GetPosition(edge));
                        edge.VisState |= ElementVisHighlight.Picked;
                    }
                    else if (node != null && this.edge_to_rerout != null && this.edge_to_rerout_to_Node == null)
                    {
                        this.edge_to_rerout_to_Node = node;
                        node.VisState |= ElementVisHighlight.Picked;
                    }
                    break;
                case ManagementState.ASSIGN_COMP:
                    if (this.element_to_assoc_w_comp == null && !(sender is FlNwNetworkVisualization))
                    {
                        this.element_to_assoc_w_comp = element;
                        element.VisState |= ElementVisHighlight.Picked;
                        this.IsPickingComp = true;
                    }
                    break;
                case ManagementState.DISASSOC_COMP:
                    if (!(element is FlNwNetworkVisualization))
                    {
                        element.VisState |= ElementVisHighlight.Picked;
                    }
                    break;
                case ManagementState.DEFINE_SOURCE:
                case ManagementState.DEFINE_SINK:
                    element.VisState |= ElementVisHighlight.Picked;
                    break;
                case ManagementState.DEFINE_SIZE_OF_COMP:
                    if (!(element is FlNwNetworkVisualization))
                    {
                        this.element_vis_to_resize = element;
                        element.VisState |= ElementVisHighlight.Picked;
                        // call the window to determine size
                        this.size_window = new CompGeomSizeWindow();
                        this.size_window.Loaded += size_window_Loaded;
                        this.size_window.Closed += size_window_Closed;
                        this.size_window.ShowDialog(); // blocking  
                    }
                    break;
                case ManagementState.DEFINE_OPERATIONS_IN_NODE:
                    if (node != null)
                    {
                        this.element_vis_to_set_operations = element;
                        element.VisState |= ElementVisHighlight.Picked;
                        // call the window
                        this.operation_window = new FlowNetworkOperationWindow();
                        this.operation_window.NodeCurrent = node.FN_Node;
                        this.operation_window.Closed += operation_window_Closed;
                        this.operation_window.ShowDialog(); // blocking
                    }
                    break;
                case ManagementState.COPY_OPERATION_TO_ALL_INST:
                    if (!(element is FlNwNetworkVisualization) && edge == null && node != null && node.FN_Node != null)
                    {
                        element.VisState |= ElementVisHighlight.Picked;
                        this.last_selected_comp = node.FN_Node.Content;
                        this.HighlightInstancesOfContainedComponent(true);
                        this.NetworkToDisplay.PropagateCalculationRulesToAllInstances(node.FN_Node);
                    }
                    break;
            }
            
        }

        protected void size_window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.size_window == null) return;
            if (this.element_vis_to_resize == null) return;

            this.size_window.SetContent(this.element_vis_to_resize);
        }

        protected void size_window_Closed(object sender, EventArgs e)
        {
            if (this.size_window == null) return;

            // transfer the size to the content of the caller
            if (this.element_vis_to_resize == null) return;
            this.element_vis_to_resize.TransferSize(this.size_window.Sizes, this.size_window.Settings);
            this.OnSwitchState(ManagementState.NEUTRAL);
        }

        protected void operation_window_Closed(object sender, EventArgs e)
        {
            if (this.operation_window == null) return;
            this.element_vis_to_set_operations.UpdateContent();
            this.OnSwitchState(ManagementState.NEUTRAL);
        }

        protected void element_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            FlNwNodeVisualization node = sender as FlNwNodeVisualization;
            if (node == null || e == null) return;

            if (!node.VisState.HasFlag(ElementVisHighlight.Manipulated)) return;

            Point mPos = e.GetPosition(this);
            Vector offset = mPos - node.Anchor;
            node.Translate(offset);
        }

        protected void element_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FlNwElementVisualization element = sender as FlNwElementVisualization;
            if (element == null || e == null) return;

            // clear any boxes that mark common content parents
            if (this.marking_boxes != null)
            {
                foreach (Rectangle box in this.marking_boxes)
                {
                    this.Children.Remove(box);
                }
                this.marking_boxes.Clear();
            }

            // handle activites 
            FlNwNodeVisualization node = sender as FlNwNodeVisualization;
            FlNwEdgeVisualization edge = sender as FlNwEdgeVisualization;

            switch (this.GuiState)
            {
                case ManagementState.NEUTRAL:
                    // stop moving
                    if (node != null)
                        node.VisState &= ~ElementVisHighlight.Manipulated;
                    break;
                case ManagementState.ADD_EDGE:
                    // node selection under way:
                    if (this.node_start != null && this.node_end != null && this.nr_nodes_for_edge_selected == 2)
                    {                        
                        // build edge
                        this.AddEdge(this.node_start, this.node_end);
                        // reset
                        this.OnSwitchState(ManagementState.ADD_EDGE);
                    }
                    e.Handled = true;
                    break;
                case ManagementState.REMOVE:
                    if (!element.ElementLocked)
                    {
                        if (edge != null)
                            this.RemoveEdge(edge);
                        else if (node != null)
                            this.RemoveNode(node);
                    }
                    this.OnSwitchState(ManagementState.REMOVE);
                    break;
                case ManagementState.REROUTE_EDGE:
                    if (this.edge_to_rerout != null && this.edge_to_rerout_to_Node != null)
                    {
                        // redirect edge
                        this.edge_to_rerout.RedirectEdge(this.edge_to_rerout_to_Node, this.edge_to_rerout_Start, this.NetworkToDisplay);
                        // reset
                        this.OnSwitchState(ManagementState.REROUTE_EDGE);
                    }
                    e.Handled = true;                   
                    break;
                case ManagementState.DISASSOC_COMP:
                    if (!element.ElementLocked)
                    {
                        if (edge != null)
                            edge.FN_Edge.Content = null;
                        else if (node != null)
                            node.FN_Node.Content = null;
                    }
                    element.VisState &= ~ElementVisHighlight.Picked;
                    this.OnSwitchState(ManagementState.DISASSOC_COMP);
                    break;
                case ManagementState.DEFINE_SOURCE:
                    if (!element.ElementLocked && node != null)
                    {
                        this.SwitchNetworkSource(node);
                    }
                    element.VisState &= ~ElementVisHighlight.Picked;
                    this.OnSwitchState(ManagementState.DEFINE_SOURCE);
                    break;
                case ManagementState.DEFINE_SINK:
                    if (!element.ElementLocked && node != null)
                    {
                        this.SwitchNetworkSink(node);
                    }
                    element.VisState &= ~ElementVisHighlight.Picked;
                    this.OnSwitchState(ManagementState.DEFINE_SINK);
                    break;
                case ManagementState.DEFINE_SIZE_OF_COMP:
                case ManagementState.DEFINE_OPERATIONS_IN_NODE:
                    element.VisState &= ~ElementVisHighlight.Picked;
                    break;
                case ManagementState.COPY_OPERATION_TO_ALL_INST:
                    element.VisState &= ~ElementVisHighlight.Picked;
                    this.OnSwitchState(ManagementState.COPY_OPERATION_TO_ALL_INST);
                    break;
            }

        }


        #endregion
    }
}
