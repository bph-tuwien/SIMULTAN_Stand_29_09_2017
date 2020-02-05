using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ParameterStructure.Mapping;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for GenericTreeVisWindow.xaml
    /// </summary>
    public partial class GenericTreeVisWindow : Window
    {
        private static readonly double VIS_SIZE = 24.0;
        private static readonly Color COLOR_LABEL_1 = Colors.Black;
        private static readonly Color COLOR_LABEL_2 = Colors.Navy;
        private static readonly Color COLOR_LABEL_3 = Colors.DimGray;
        private static readonly Color COLOR_LABEL_4 = Colors.Blue;

        private static readonly Color COLOR_LINK_1 = Colors.DarkOrange;
        private static readonly Color COLOR_LINK_2 = Colors.Sienna;

        public GenericTreeVisWindow()
        {
            InitializeComponent();
            Loaded += GenericTreeVisWindow_Loaded;           
        }

        #region EVENT HANDLERS
        private void GenericTreeVisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.links = new List<System.Windows.Media.Media3D.Point4D>();
            
            this.UpdateCanvasComp();
            this.UpdateCanvasType();

            this.canv_links.SizeChanged += canv_links_SizeChanged;
            this.UpdateLinks();
        }

        private void canv_links_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateCanvasComp();
            this.UpdateCanvasType();
            this.UpdateLinks();
            e.Handled = true;
        }
        #endregion

        #region PROPERTIES: Forest Source

        private List<StructureNode> input_structure;
        public List<StructureNode> InputStructure
        {
            get { return this.input_structure; }
            set 
            { 
                this.input_structure = value;
                if (this.IsLoaded)
                    this.UpdateCanvasComp();
            }
        }

        private StructureNode type_structure;
        public StructureNode TypeStructure
        {
            get { return this.type_structure; }
            set 
            { 
                this.type_structure = value;
                if (this.IsLoaded)
                    this.UpdateCanvasType();
            }
        }

        private List<System.Windows.Media.Media3D.Point4D> links;
        private Dictionary<StructureNode, List<Point>> link_halves;

        #endregion

        #region METHODS: Display update

        private void UpdateCanvasComp()
        {
            if (this.canv_comp_forest == null) return;
            if (this.InputStructure == null) return;

            this.canv_comp_forest.Children.Clear();
            this.link_halves = new Dictionary<StructureNode, List<Point>>();

            // get the nodes per level
            Dictionary<int, List<StructureNode>> nodes_per_level = null;
            GenericTreeVisWindow.GetNodesPerLevel(this.InputStructure, 0, ref nodes_per_level);
            
            int nr_nodes_vert = nodes_per_level.Count;
            double offset_vert = this.canv_comp_forest.ActualHeight / (nr_nodes_vert + 1);

            for(int j = 0; j < nr_nodes_vert; j++)
            {
                int nr_nodes_on_this_level = nodes_per_level[j].Count;
                double offset_hrzt = this.canv_comp_forest.ActualWidth / (nr_nodes_on_this_level + 1);
                for(int n = 0; n < nr_nodes_on_this_level; n++)
                {
                    // line to parent
                    if (j > 0)
                    {
                        int index_in_upper_level = nodes_per_level[j - 1].IndexOf(nodes_per_level[j][n].ParentNode);
                        double offset_hzrt_upper_level = this.canv_comp_forest.ActualWidth / (nodes_per_level[j - 1].Count + 1);
                        Line link = new Line();
                        link.X1 = offset_hzrt_upper_level * (index_in_upper_level + 1) + VIS_SIZE / 2;
                        link.Y1 = offset_vert * j + VIS_SIZE;
                        link.X2 = offset_hrzt * (n + 1) + VIS_SIZE / 2;
                        link.Y2 = offset_vert * (j + 1);
                        link.Stroke = new SolidColorBrush(Colors.Black);
                        link.StrokeThickness = 1;

                        this.canv_comp_forest.Children.Add(link);
                    }

                    // node
                    Ellipse sn_vis = this.GetNodeVis(nodes_per_level[j][n]);
                    sn_vis.Margin = new Thickness(offset_hrzt * (n + 1), offset_vert * (j + 1), 0, 0);
                    TextBlock sn_label = this.GetNodeLabel(nodes_per_level[j][n]);
                    sn_label.Margin = new Thickness(offset_hrzt * (n + 1) - VIS_SIZE, offset_vert * (j + 1) - VIS_SIZE + (n % 2) * 2 * VIS_SIZE, 0, 0);
                    sn_label.Foreground = new SolidColorBrush(GetColorFor(n % 4));

                    this.canv_comp_forest.Children.Add(sn_vis);
                    this.canv_comp_forest.Children.Add(sn_label);

                    // links
                    if (nodes_per_level[j][n].LinkTargetNode != null)
                    {
                        if (this.link_halves.ContainsKey(nodes_per_level[j][n].LinkTargetNode))
                            this.link_halves[nodes_per_level[j][n].LinkTargetNode].Add(new Point(offset_hrzt * (n + 1) + VIS_SIZE / 2, offset_vert * (j + 1) + VIS_SIZE / 2));
                        else
                            this.link_halves.Add(nodes_per_level[j][n].LinkTargetNode,
                                                new List<Point> { new Point(offset_hrzt * (n + 1) + VIS_SIZE / 2, offset_vert * (j + 1) + VIS_SIZE / 2) });
                    }
                }
            }
        }

        private void UpdateCanvasType()
        {
            if (this.canv_type_tree == null) return;
            if (this.TypeStructure == null) return;

            this.canv_type_tree.Children.Clear();
            this.links = new List<System.Windows.Media.Media3D.Point4D>();

            // get the nodes per level
            Dictionary<int, List<StructureNode>> nodes_per_level = null;
            GenericTreeVisWindow.GetNodesPerLevel(this.TypeStructure, 0, ref nodes_per_level);

            int nr_nodes_vert = nodes_per_level.Count;
            double offset_vert = this.canv_type_tree.ActualHeight / (nr_nodes_vert + 1);

            for (int j = 0; j < nr_nodes_vert; j++)
            {
                int nr_nodes_on_this_level = nodes_per_level[j].Count;
                double offset_hrzt = this.canv_type_tree.ActualWidth / (nr_nodes_on_this_level + 1);
                for (int n = 0; n < nr_nodes_on_this_level; n++)
                {
                    // line to parent
                    if (j > 0)
                    {
                        int index_in_upper_level = nodes_per_level[j - 1].IndexOf(nodes_per_level[j][n].ParentNode);
                        double offset_hzrt_upper_level = this.canv_type_tree.ActualWidth / (nodes_per_level[j - 1].Count + 1);
                        Line link = new Line();
                        link.X1 = offset_hzrt_upper_level * (index_in_upper_level + 1) + VIS_SIZE / 2;
                        link.Y1 = offset_vert * j + VIS_SIZE;
                        link.X2 = offset_hrzt * (n + 1) + VIS_SIZE / 2;
                        link.Y2 = offset_vert * (j + 1);
                        link.Stroke = new SolidColorBrush(Colors.Black);
                        link.StrokeThickness = 1;

                        this.canv_type_tree.Children.Add(link);
                    }

                    // node
                    Ellipse sn_vis = this.GetNodeVis(nodes_per_level[j][n]);
                    sn_vis.Margin = new Thickness(offset_hrzt * (n + 1), offset_vert * (j + 1), 0, 0);
                    TextBlock sn_label = this.GetNodeLabel(nodes_per_level[j][n]);
                    sn_label.Margin = new Thickness(offset_hrzt * (n + 1) - VIS_SIZE, offset_vert * (j + 1) - VIS_SIZE + (n % 2) * 2 * VIS_SIZE, 0, 0);
                    sn_label.Foreground = new SolidColorBrush(GetColorFor(n % 4));

                    this.canv_type_tree.Children.Add(sn_vis);
                    this.canv_type_tree.Children.Add(sn_label);

                    // links
                    if (this.link_halves.ContainsKey(nodes_per_level[j][n]))
                    {
                        foreach(Point target in this.link_halves[nodes_per_level[j][n]])
                        {
                            System.Windows.Media.Media3D.Point4D link = new System.Windows.Media.Media3D.Point4D()
                            {
                                X = target.X,
                                Y = target.Y,
                                Z = offset_hrzt * (n + 1) + VIS_SIZE / 2,
                                W = offset_vert * (j + 1) + VIS_SIZE / 2
                            };
                            this.links.Add(link);
                        }                       
                    }
                }
            }
        }

        private void UpdateLinks()
        {
            if (this.canv_links == null) return;
            if (this.links == null) return;
            if (this.links.Count == 0) return;

            this.canv_links.Children.Clear();

            int counter = 0;
            Point last_target = new Point(-1, -1);
            foreach(System.Windows.Media.Media3D.Point4D link in this.links)
            {
                double connecing_line_Y = Math.Max(link.Y, link.W) + VIS_SIZE;

                Line line_1 = new Line();
                line_1.X1 = link.X;
                line_1.Y1 = link.Y;
                line_1.X2 = line_1.X1;
                line_1.Y2 = connecing_line_Y;

                Line link_line = new Line();
                link_line.X1 = link.X;
                link_line.Y1 = connecing_line_Y;
                link_line.X2 = this.canv_comp_forest.ActualWidth + this.canv_middle_measure.ActualWidth + link.Z;
                link_line.Y2 = connecing_line_Y;

                Line line_2 = new Line();
                line_2.X1 = link_line.X2;
                line_2.Y1 = connecing_line_Y;
                line_2.X2 = line_2.X1;
                line_2.Y2 = link.W;

                if (line_2.X2 != last_target.X || line_2.Y2 != last_target.Y)
                {
                    last_target = new Point(line_2.X2, line_2.Y2);
                    counter++;
                }  

                line_1.StrokeThickness = 1;
                line_1.Stroke = new SolidColorBrush(GenericTreeVisWindow.GetLinkColorFor(counter % 2));
                link_line.StrokeThickness = 1;
                link_line.Stroke = new SolidColorBrush(GenericTreeVisWindow.GetLinkColorFor(counter % 2));
                line_2.StrokeThickness = 1;
                line_2.Stroke = new SolidColorBrush(GenericTreeVisWindow.GetLinkColorFor(counter % 2));

                System.Windows.Media.Effects.DropShadowEffect line_effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = GenericTreeVisWindow.GetLinkColorFor(counter % 2),
                    BlurRadius = 3,
                    Direction = 315,
                    ShadowDepth = 2,
                    Opacity = 0.5
                };

                line_1.Effect = line_effect;
                link_line.Effect = line_effect;
                line_2.Effect = line_effect;

                this.canv_links.Children.Add(line_1);
                this.canv_links.Children.Add(link_line);
                this.canv_links.Children.Add(line_2);
                              
            }
        }

        private Ellipse GetNodeVis(StructureNode _sn)
        {
            Ellipse el = new Ellipse();
            el.Width = 24;
            el.Height = 24;
            el.Stroke = new SolidColorBrush(Colors.Black);

            if (_sn.ParentNode == null)
                el.StrokeThickness = 2;
            else
                el.StrokeThickness = 1;

            if (_sn.ChildrenNodes == null || _sn.ChildrenNodes.Count == 0)
                el.Fill = new SolidColorBrush(Colors.White);
            else
                el.Fill = new SolidColorBrush(Colors.Gray);

            el.Effect =
            new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 3,
                Direction = 315,
                ShadowDepth = 3,
                Opacity = 0.5
            };

            return el;
        }

        private TextBlock GetNodeLabel(StructureNode _sn)
        {
            TextBlock tb = new TextBlock();
            tb.Text = _sn.ToSimpleString();
            tb.FontSize = 10;
            tb.Foreground = new SolidColorBrush(Colors.Black);

            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Top;

            return tb;
        }

        #endregion

        #region UTILS

        private static void GetNodesPerLevel(StructureNode _node, int _level_index, ref Dictionary<int, List<StructureNode>> nodes_per_level)
        {
            if (nodes_per_level == null)
                nodes_per_level = new Dictionary<int, List<StructureNode>>();

            if (_node == null) return;

            if (!(nodes_per_level.ContainsKey(_level_index)))
                nodes_per_level.Add(_level_index, new List<StructureNode>());

            nodes_per_level[_level_index].Add( _node );
            
            // re-order the children nodes to put the ones w most children near the middle
            List<StructureNode> ascending = _node.ChildrenNodes.OrderBy(x => x.ChildrenNodes.Count).ToList();           
            List<StructureNode> children_ordered = new List<StructureNode>();
            for (int i = 0; i < ascending.Count; i++ )
            {
                if (i < 2)
                    children_ordered.Add(ascending[i]);
                else
                    children_ordered.Insert(i / 2, ascending[i]);
            }

            // recursion
            foreach (StructureNode sN in children_ordered)
            {
                GetNodesPerLevel(sN, _level_index + 1, ref nodes_per_level);
            }
        }

        private static void GetNodesPerLevel(List<StructureNode> _nodes, int _level_index, ref Dictionary<int, List<StructureNode>> nodes_per_level)
        {
            if (nodes_per_level == null)
                nodes_per_level = new Dictionary<int, List<StructureNode>>();

            foreach(StructureNode sN in _nodes)
            {
                Dictionary<int, List<StructureNode>> sN_levels = null;
                GenericTreeVisWindow.GetNodesPerLevel(sN, 0, ref sN_levels);
                foreach(var entry in sN_levels)
                {
                    if (nodes_per_level.ContainsKey(entry.Key))
                        nodes_per_level[entry.Key].AddRange(entry.Value);
                    else
                        nodes_per_level.Add(entry.Key, entry.Value);
                }
            }
        }

        private static Color GetColorFor(int _index)
        {
            switch(_index)
            {
                case 0:
                    return COLOR_LABEL_1;
                case 1:
                    return COLOR_LABEL_2;
                case 2:
                    return COLOR_LABEL_3;
                case 3:
                    return COLOR_LABEL_4;
                default:
                    return COLOR_LABEL_1;
            }
        }

        private static Color GetLinkColorFor(int _index)
        {
            switch(_index)
            {
                case 0:
                    return COLOR_LINK_1;
                default:
                    return COLOR_LINK_2;
            }
        }
        

        #endregion
    }
}
