using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;

using ParameterStructure.Component;
using ParameterStructure.Parameter;

using ComponentBuilder.WinUtils;

namespace ComponentBuilder.GraphUIE
{
    class ComponentVisualization : NodeVisualization
    {
        #region PROPERTIES: Display

        public override bool IsExpanded
        {
            get { return this.is_expanded; }
            set
            {
                this.is_expanded = value;
                if (this.is_expanded)
                {
                    this.PopulateChildren();
                    this.DrawConnectionsParamsCalc();
                    if (this.node_switch_SUB != null)
                        this.node_switch_SUB.Fill = new SolidColorBrush(NodeVisualization.NODE_COLOR_YES);
                    
                    this.to_be_expanded = false;
                    this.to_be_expanded_chain = null;
                }
                else
                {
                    this.UpdateContent();
                    if (this.node_switch_SUB != null)
                        this.node_switch_SUB.Fill = new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
                }
                this.RegisterPropertyChanged("IsExpanded");
            }
        }

        public override bool IsShowingRefs
        {
            get { return this.is_showing_refs; }
            set
            {
                this.is_showing_refs = value;
                if (this.is_showing_refs)
                {
                    this.DrawConnectionsToRefs();
                    if (this.node_switch_REF != null)
                        this.node_switch_REF.Fill = new SolidColorBrush(NodeVisualization.NODE_COLOR_YES);
                }
                else
                {
                    this.RemoveReferencePolylines();
                    if (this.node_switch_REF != null)
                        this.node_switch_REF.Fill = new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
                }
            }
        }

        public override bool IsSimple
        {
            get { return this.is_simple; }
            set
            {
                this.is_simple = value;
                if (this.is_simple)
                {
                    this.node_width = NodeVisualization.NODE_HEIGHT_SMALL;
                    this.node_height = NodeVisualization.NODE_HEIGHT_SMALL;
                    this.Width = this.node_width;
                }
                else
                {
                    this.node_width = NodeVisualization.NODE_WIDTH_DEFAULT;
                    this.node_height = NodeVisualization.NODE_HEIGHT_DEFAULT;
                    this.Width = this.node_width + NodeVisualization.NODE_WIDTH_SWITCHES;                   
                }
                this.UpdateContent();
            }
        }

        #endregion

        #region PROPERTIES: Info

        public override Category VisCategory
        {
            get
            {
                if (this.node_component == null)
                    return Category.NoNe;
                else
                    return this.node_component.Category;
            }
        }

        public override long VisID
        {
            get
            {
                if (this.node_component == null)
                    return -1;
                else
                    return this.node_component.ID;
            }
        }

        #endregion

        #region CLASS MEMBERS

        // TODO: revert to protected
        internal ParameterStructure.Component.Component node_component;

        #endregion

        #region .CTOR

        public ComponentVisualization(CompGraph _parent_canvas, ParameterStructure.Component.Component _data, double _offset_hrzt, double _offset_vert)
            :base(_parent_canvas, _data, _offset_hrzt, _offset_vert)
        {
            this.Width = this.node_width + NodeVisualization.NODE_WIDTH_SWITCHES;
            this.Height = this.node_height + NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP + NodeVisualization.NODE_WIDTH_MARKERS;
            this.Extents = new BoundingBox()
            {
                UpperLeft = this.position,
                LowerRight = new Point(this.position.X + this.Width, this.position.Y + this.Height)
            };

            if (this.node_data != null)
            {
                this.node_component = this.node_data as ParameterStructure.Component.Component;
                this.node_component.PropertyChanged += node_comp_PropertyChanged;
            }
        }

        #endregion

        #region METHODS: Display Update

        protected override void RedefineGrid()
        {
            // reset grid
            this.Children.Clear();
            this.RowDefinitions.Clear();
            this.ColumnDefinitions.Clear();
            this.ClearChildrenTree();

            // reset extents
            this.Extents.UpperLeft = this.position;
            this.Extents.LowerRight = new Point(this.position.X + this.Width, this.position.Y + this.Height);
            this.Extents.ParentTranslation = new Vector(0, 0);

            if (this.IsSimple)
                this.RedefineGridSimple();
            else
                this.RedefineGridComplex();

            //// debug
            //this.ShowGridLines = true;
        }

        protected override void PopulateGrid()
        {
            if (this.IsSimple)
                this.PopulateGridSimple();
            else
                this.PopulateGridComplex();            
        }

        #endregion

        #region METHODS: Redefine Grid

        protected void RedefineGridComplex()
        {
            // re-define the columns
            ColumnDefinition cd_MAIN = new ColumnDefinition();
            cd_MAIN.Width = new GridLength(NodeVisualization.NODE_WIDTH_DEFAULT);
            this.ColumnDefinitions.Add(cd_MAIN);

            ColumnDefinition cd_SWITCH = new ColumnDefinition();
            cd_SWITCH.Width = new GridLength(NodeVisualization.NODE_WIDTH_SWITCHES);
            this.ColumnDefinitions.Add(cd_SWITCH);

            // re-define the rows
            RowDefinition rd_MANAGER = new RowDefinition();
            rd_MANAGER.Height = new GridLength(NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP);
            this.RowDefinitions.Add(rd_MANAGER);

            RowDefinition rd_MAIN = new RowDefinition();
            rd_MAIN.Height = new GridLength(NodeVisualization.NODE_HEIGHT_DEFAULT);
            this.RowDefinitions.Add(rd_MAIN);

            RowDefinition rd_CATEGORY = new RowDefinition();
            rd_CATEGORY.Height = new GridLength(NodeVisualization.NODE_WIDTH_MARKERS);
            this.RowDefinitions.Add(rd_CATEGORY);
        }

        protected void RedefineGridSimple()
        {
            // do nothing

            //// debug
            //ColumnDefinition cd_1 = new ColumnDefinition();
            //cd_1.Width = new GridLength(this.Width * 0.5);
            //this.ColumnDefinitions.Add(cd_1);

            //ColumnDefinition cd_2 = new ColumnDefinition();
            //cd_2.Width = new GridLength(this.Width * 0.5);
            //this.ColumnDefinitions.Add(cd_2);

            //RowDefinition rd_1 = new RowDefinition();
            //rd_1.Height = new GridLength(this.Height * 0.5);
            //this.RowDefinitions.Add(rd_1);

            //RowDefinition rd_2= new RowDefinition();
            //rd_2.Height = new GridLength(this.Height * 0.5);
            //this.RowDefinitions.Add(rd_2);
        }

        #endregion

        #region METHODS: Populate Grid

        protected void PopulateGridComplex()
        {
            if (this.node_component == null) return;
            this.node_graphics_for_contour_update = new List<Shape>();

            // MAIN NODE
            Rectangle rect = new Rectangle();
            rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            rect.Height = this.node_height;
            rect.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            rect.Stroke = new SolidColorBrush(this.node_contour);
            rect.StrokeThickness = this.node_contour_thickness;
            rect.Fill = new LinearGradientBrush(this.node_fill_color_1, this.node_fill_color_2, new Point(0.5, 0), new Point(0.5, 1));
            rect.RadiusX = NodeVisualization.NODE_RADIUS;
            rect.RadiusY = NodeVisualization.NODE_RADIUS;
            rect.ContextMenu = this.BuildContextMenu();

            this.node_main = rect;
            Grid.SetColumn(rect, 0);
            Grid.SetRow(rect, 1);
            this.Children.Add(rect);

            // state switches
            Rectangle rect_SEL = new Rectangle();
            rect_SEL.Height = NodeVisualization.NODE_WIDTH_SWITCHES;
            rect_SEL.Width = NodeVisualization.NODE_WIDTH_SWITCHES;
            rect_SEL.RadiusX = Math.Floor(rect_SEL.Width * 0.5);
            rect_SEL.RadiusY = Math.Floor(rect_SEL.Height * 0.5);
            rect_SEL.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            rect_SEL.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            rect_SEL.Margin = new Thickness(0, NodeVisualization.NODE_WIDTH_SWITCHES * 0.75, NodeVisualization.NODE_WIDTH_SWITCHES, 0);
            rect_SEL.Stroke = new SolidColorBrush(this.node_contour);
            rect_SEL.Fill = (this.VisState.HasFlag(NodeVisHighlight.Selected)) ? new SolidColorBrush(NodeVisualization.NODE_COLOR_YES) : new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
            rect_SEL.ToolTip = "Auswahl";
            rect_SEL.Cursor = System.Windows.Input.Cursors.Hand;
            rect_SEL.MouseUp += rect_SELECT_MouseUp;

            this.node_switch_SELECT = rect_SEL;
            Grid.SetColumn(rect_SEL, 0);
            Grid.SetRow(rect_SEL, 1);
            this.Children.Add(rect_SEL);


            Rectangle rect_ACT = new Rectangle();
            rect_ACT.Height = NodeVisualization.NODE_WIDTH_SWITCHES;
            rect_ACT.Width = NodeVisualization.NODE_WIDTH_SWITCHES;
            rect_ACT.RadiusX = Math.Floor(rect_SEL.Width * 0.5);
            rect_ACT.RadiusY = Math.Floor(rect_SEL.Height * 0.5);
            rect_ACT.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            rect_ACT.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            rect_ACT.Margin = new Thickness(0, 0, NodeVisualization.NODE_WIDTH_SWITCHES, NodeVisualization.NODE_WIDTH_SWITCHES * 0.75);
            rect_ACT.Stroke = new SolidColorBrush(this.node_contour);
            rect_ACT.Fill = (this.VisState == NodeVisHighlight.Inactive) ? new SolidColorBrush(NodeVisualization.NODE_COLOR_NO) : new SolidColorBrush(NodeVisualization.NODE_COLOR_YES);
            rect_ACT.ToolTip = "Aktivierung";
            rect_ACT.Cursor = System.Windows.Input.Cursors.Hand;
            rect_ACT.MouseUp += rect_ACTIVATE_MouseUp;

            this.node_switch_ACTIVATE = rect_ACT;
            Grid.SetColumn(rect_ACT, 0);
            Grid.SetRow(rect_ACT, 1);
            this.Children.Add(rect_ACT);

            // MAIN NODE TEXT
            TextBlock tb_slot = new TextBlock();
            tb_slot.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            tb_slot.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            tb_slot.Padding = new Thickness(5, 5, 5, 0);
            tb_slot.Text = this.node_component.CurrentSlot + ":";
            tb_slot.FontSize = 10;
            tb_slot.FontWeight = FontWeights.Bold;
            tb_slot.Foreground = new SolidColorBrush(NodeVisualization.NODE_FOREGROUND);
            tb_slot.IsHitTestVisible = false;

            Grid.SetColumn(tb_slot, 0);
            Grid.SetRow(tb_slot, 1);
            this.Children.Add(tb_slot);

            TextBlock tb = new TextBlock();
            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            tb.Padding = new Thickness(5);
            tb.Text = this.node_component.Name + " " + this.node_component.Description;
            tb.FontSize = 10;
            tb.Foreground = new SolidColorBrush(NodeVisualization.NODE_FOREGROUND);
            tb.IsHitTestVisible = false;

            this.text_main = tb;
            Grid.SetColumn(tb, 0);
            Grid.SetRow(tb, 1);
            this.Children.Add(tb);

            // MAIN NODE SUBCOMPONENT AND REFERENCE SWITCHES -> OUT
            Rectangle rect_SUB = new Rectangle();
            rect_SUB.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            rect_SUB.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            rect_SUB.Width = NodeVisualization.NODE_WIDTH_SWITCHES;
            rect_SUB.Height = NodeVisualization.NODE_WIDTH_SWITCHES;
            rect_SUB.Margin = new Thickness(-1, 0, 0, NodeVisualization.NODE_WIDTH_SWITCHES * 1.5);
            rect_SUB.Stroke = new SolidColorBrush(this.node_contour);
            rect_SUB.StrokeThickness = 1;
            rect_SUB.Fill = (this.IsExpanded) ? new SolidColorBrush(NodeVisualization.NODE_COLOR_YES) : new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
            rect_SUB.ToolTip = "Subkomponenten";
            rect_SUB.Cursor = System.Windows.Input.Cursors.Hand;
            rect_SUB.MouseUp += rect_SUB_MouseUp;

            this.node_switch_SUB = rect_SUB;
            Grid.SetColumn(rect_SUB, 1);
            Grid.SetRow(rect_SUB, 1);
            this.Children.Add(rect_SUB);

            Rectangle rect_REF = new Rectangle();
            rect_REF.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            rect_REF.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            rect_REF.Width = NodeVisualization.NODE_WIDTH_SWITCHES;
            rect_REF.Height = NodeVisualization.NODE_WIDTH_SWITCHES;
            rect_REF.Margin = new Thickness(-1, NodeVisualization.NODE_WIDTH_SWITCHES * 1.5, 0, 0);
            rect_REF.Stroke = new SolidColorBrush(this.node_contour);
            rect_REF.StrokeThickness = 1;
            rect_REF.Fill = (this.IsShowingRefs) ? new SolidColorBrush(NodeVisualization.NODE_COLOR_YES) : new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
            rect_REF.ToolTip = "Referenzkomponenten";
            rect_REF.Cursor = System.Windows.Input.Cursors.Hand;
            rect_REF.MouseUp += rect_REF_MouseUp;

            this.node_switch_REF = rect_REF;
            Grid.SetColumn(rect_REF, 1);
            Grid.SetRow(rect_REF, 1);
            this.Children.Add(rect_REF);

            // MANGER MARKERS
            Rectangle rect_M_BACK = new Rectangle();
            rect_M_BACK.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            rect_M_BACK.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            rect_M_BACK.Fill = new SolidColorBrush(Colors.Transparent);
            rect_M_BACK.MouseMove += marker_manager_bgr_MouseMove;

            Grid.SetColumn(rect_M_BACK, 0);
            Grid.SetRow(rect_M_BACK, 0);
            this.Children.Add(rect_M_BACK);

            int nr_roles = Enum.GetNames(typeof(ComponentManagerType)).Length;
            for (int i = 0; i < nr_roles; i++)
            {
                Rectangle el = new Rectangle();
                el.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                el.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                el.Width = NodeVisualization.NODE_WIDTH_MARKERS + 1;
                el.Height = NodeVisualization.NODE_WIDTH_MARKERS;
                el.Margin = new Thickness(NodeVisualization.NODE_RADIUS + i * NodeVisualization.NODE_WIDTH_MARKERS, 0, 0, -1);
                el.Stroke = new SolidColorBrush(this.node_contour);
                el.StrokeThickness = 1;
                bool has_writing_access = this.node_component != null && this.node_component.AccessLocal[(ComponentManagerType)i].AccessTypeFlags.HasFlag(ComponentAccessType.WRITE);
                el.Fill = has_writing_access ? new SolidColorBrush(NodeVisualization.NODE_COLOR_YES) : new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
                el.Tag = i;
                el.MouseMove += marker_manager_MouseMove;
                this.node_graphics_for_contour_update.Add(el);

                Grid.SetColumn(el, 0);
                Grid.SetRow(el, 0);
                this.Children.Add(el);

                // manager type symbols
                TextBlock tbM = new TextBlock();
                tbM.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                tbM.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                tbM.Width = NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP;
                tbM.Height = NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP;
                tbM.Margin = new Thickness(NodeVisualization.NODE_RADIUS + i * NodeVisualization.NODE_WIDTH_MARKERS, 0, 0, 0);
                tbM.Text = ComponentUtils.ComponentManagerTypeToLetter((ComponentManagerType)i);
                tbM.ToolTip = ComponentUtils.ComponentManagerTypeToDescrDE((ComponentManagerType)i);
                tbM.Padding = new Thickness(2, 2, 1, 1);
                tbM.FontSize = 22;
                tbM.FontFamily = new FontFamily(new Uri("pack://application:,,,/ComponentBuilder;component/Data/fonts/"), "./#managers");
                tbM.Foreground = has_writing_access ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
                tbM.Visibility = System.Windows.Visibility.Collapsed;
                tbM.Tag = i;

                Grid.SetColumn(tbM, 0);
                Grid.SetColumnSpan(tbM, 2);
                Grid.SetRow(tbM, 0);
                Grid.SetZIndex(tbM, 1);
                this.Children.Add(tbM);
            }

            // SYMBOL
            if (this.node_component.SymbolImage != null)
            {
                Image symb = new Image();
                symb.Source = this.node_component.SymbolImage;
                symb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                symb.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                symb.Width = NodeVisualization.NODE_IMG_SIZE;
                symb.Height = NodeVisualization.NODE_IMG_SIZE;
                symb.Margin = new Thickness(NodeVisualization.NODE_RADIUS + nr_roles * NodeVisualization.NODE_WIDTH_MARKERS, 0, 0, -1);
                symb.IsHitTestVisible = false;

                Grid.SetColumn(symb, 0);
                Grid.SetRow(symb, 0);
                this.Children.Add(symb);
            }

            // CATEGORY MARKERS
            Rectangle rect_C_BACK = new Rectangle();
            rect_C_BACK.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            rect_C_BACK.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            rect_C_BACK.Fill = new SolidColorBrush(Colors.Transparent);
            rect_C_BACK.MouseMove += marker_manager_bgr_MouseMove;

            Grid.SetColumn(rect_C_BACK, 0);
            Grid.SetRow(rect_C_BACK, 2);
            this.Children.Add(rect_C_BACK);

            int nr_categories = Enum.GetNames(typeof(Category)).Length - 2;
            for (int i = 0; i < nr_categories; i++)
            {
                Rectangle el = new Rectangle();
                el.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                el.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                el.Width = NodeVisualization.NODE_WIDTH_MARKERS + 1;
                el.Height = NodeVisualization.NODE_WIDTH_MARKERS;
                el.Margin = new Thickness(NodeVisualization.NODE_RADIUS + i * NodeVisualization.NODE_WIDTH_MARKERS, -1, 0, 0);
                el.Stroke = new SolidColorBrush(this.node_contour);
                el.StrokeThickness = 1;
                Category cat_debug = (Category)(2 << i);
                bool has_category = this.node_component != null && this.node_component.Category.HasFlag((Category)(2 << i));
                el.Fill = has_category ? new SolidColorBrush(NodeVisualization.NODE_COLOR_YES) : new SolidColorBrush(NodeVisualization.NODE_COLOR_NO);
                el.Tag = 100 + i;
                el.MouseMove += marker_manager_MouseMove;
                this.node_graphics_for_contour_update.Add(el);

                Grid.SetColumn(el, 0);
                Grid.SetRow(el, 2);
                this.Children.Add(el);

                // category type symbols
                TextBlock tbM = new TextBlock();
                tbM.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                tbM.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                tbM.Width = NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP;
                tbM.Height = NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP;
                tbM.Margin = new Thickness(NodeVisualization.NODE_RADIUS + i * NodeVisualization.NODE_WIDTH_MARKERS, 0, 0, 0);
                tbM.Text = (this.node_component != null) ? ComponentUtils.CategoryToString(this.node_component.Category)[i].ToString() : "";
                el.ToolTip = ComponentUtils.CategoryStringToDescription(tbM.Text.ToUpper());
                tbM.Padding = new Thickness(2, 2, 1, 1);
                tbM.FontSize = 22;
                tbM.FontFamily = new FontFamily(new Uri("pack://application:,,,/ComponentBuilder;component/Data/fonts/"), "./#categories");
                tbM.Foreground = new SolidColorBrush(Colors.Black);
                tbM.Visibility = System.Windows.Visibility.Collapsed;
                tbM.Tag = 100 + i;
                tbM.IsHitTestVisible = false;

                Grid.SetColumn(tbM, 0);
                Grid.SetColumnSpan(tbM, 2);
                Grid.SetRow(tbM, 1);
                Grid.SetZIndex(tbM, 1);
                this.Children.Add(tbM);
            }
        }

        protected void PopulateGridSimple()
        {
            if (this.node_component == null) return;

            // MAIN NODE
            Rectangle rect = new Rectangle();
            rect.Width = this.node_width;
            rect.Height = this.node_height;
            rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            double diff_sizes = NodeVisualization.NODE_HEIGHT_DEFAULT - NodeVisualization.NODE_HEIGHT_SMALL;
            rect.Margin = new Thickness(0, NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP + diff_sizes * 0.5, 0, 0);
            rect.Stroke = new SolidColorBrush(this.node_contour);
            rect.StrokeThickness = this.node_contour_thickness;
            rect.Fill = new LinearGradientBrush(this.node_fill_color_1, this.node_fill_color_2, new Point(0.5, 0), new Point(0.5, 1));
            rect.RadiusX = Math.Floor(this.node_width * 0.25); // 0.5
            rect.RadiusY = Math.Floor(this.node_height * 0.25); // 0.5
            rect.ToolTip = this.node_component.CurrentSlot + ":\n" + this.node_component.Name + " " + this.node_component.Description;
            rect.ContextMenu = this.BuildContextMenu();

            this.node_main = rect;
            //Grid.SetColumn(rect, 0);
            //Grid.SetColumnSpan(rect, 2);
            //Grid.SetRow(rect, 0);
            //Grid.SetRowSpan(rect, 2);
            this.Children.Add(rect);

            // SYMBOL
            if (this.node_component.SymbolImage != null)
            {
                Image symb = new Image();
                symb.Source = this.node_component.SymbolImage;
                symb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                symb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                diff_sizes = NodeVisualization.NODE_HEIGHT_DEFAULT - NodeVisualization.NODE_IMG_SIZE;
                symb.Margin = new Thickness(0, NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP + diff_sizes * 0.5, 0, 0);
                symb.Width = NodeVisualization.NODE_IMG_SIZE;
                symb.Height = NodeVisualization.NODE_IMG_SIZE;
                symb.IsHitTestVisible = false;

                this.Children.Add(symb);
            }
        }

        protected ContextMenu BuildContextMenu()
        {
            ContextMenu cm = new ContextMenu();
            cm.UseLayoutRounding = true;

            MenuItem mi1 = new MenuItem();
            mi1.Header = "einfach";
            mi1.Command = new RelayCommand((x) => this.IsSimple = true, (x) => !this.IsSimple );
            mi1.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_simple.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi2 = new MenuItem();
            mi2.Header = "komplex";
            mi2.Command = new RelayCommand((x) => this.IsSimple = false, (x) => this.IsSimple );
            mi2.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_complex.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi3 = new MenuItem();
            mi3.Header = "entfalten";
            mi3.Command = new RelayCommand((x) => this.IsExpanded = true, (x) => this.IsUserManipulatable && !this.IsExpanded );
            mi3.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_SUB_on.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi3a = new MenuItem();
            mi3a.Header = "komplett entfalten";
            mi3a.Command = new RelayCommand((x) => this.ExpandAll(), (x) => this.IsUserManipulatable && !this.IsExpanded);
            mi3a.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_SUB_on_all.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi4 = new MenuItem();
            mi4.Header = "zusammenklappen";
            mi4.Command = new RelayCommand((x) => this.IsExpanded = false, (x) => this.IsUserManipulatable && this.IsExpanded);
            mi4.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_SUB_off.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi5 = new MenuItem();
            mi5.Header = "Referenzen zeigen";
            mi5.Command = new RelayCommand((x) => this.IsShowingRefs = true, (x) => this.IsUserManipulatable && !this.IsShowingRefs);
            mi5.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_REF_on.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi6 = new MenuItem();
            mi6.Header = "Referenzen verstecken";
            mi6.Command = new RelayCommand((x) => this.IsShowingRefs = false, (x) => this.IsUserManipulatable && this.IsShowingRefs);
            mi6.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_REF_off.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi7 = new MenuItem();
            mi7.Header = "Komponente in Liste auswählen";
            mi7.Command = new RelayCommand((x) => this.parent_canvas.SelectNode(this),
                                           (x) => this.IsUserManipulatable && this.parent_canvas != null && this.parent_canvas.CompFactory != null && !this.VisState.HasFlag(NodeVisHighlight.Selected));
            mi7.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_selected.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi8 = new MenuItem();
            mi8.Header = "Auswahl aufheben";
            mi8.Command = new RelayCommand((x) => this.parent_canvas.DeselectNode(this),
                                           (x) => this.IsUserManipulatable && this.parent_canvas != null && this.parent_canvas.CompFactory != null && this.VisState.HasFlag(NodeVisHighlight.Selected));
            mi8.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_deselected.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi9 = new MenuItem();
            mi9.Header = "Komponente aktivieren";
            mi9.Command = new RelayCommand((x) => { this.VisState |= NodeVisHighlight.Active; this.IsUserManipulatable = true; },
                                           (x) => !this.VisState.HasFlag(NodeVisHighlight.Active));
            mi9.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_active.png", UriKind.Relative)), Width = 16, Height = 16 };

            MenuItem mi10 = new MenuItem();
            mi10.Header = "Komponente deaktivieren";
            mi10.Command = new RelayCommand((x) => { this.parent_canvas.DeselectNode(this); this.VisState = NodeVisHighlight.Inactive; this.IsUserManipulatable = false; },
                                            (x) => this.parent_canvas != null && this.parent_canvas.CompFactory != null && this.VisState.HasFlag(NodeVisHighlight.Active));
            mi10.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_inactive.png", UriKind.Relative)), Width = 16, Height = 16 };

            cm.Items.Add(mi1);
            cm.Items.Add(mi2);
            cm.Items.Add(mi3);
            cm.Items.Add(mi3a);
            cm.Items.Add(mi4);
            cm.Items.Add(mi5);
            cm.Items.Add(mi6);
            cm.Items.Add(mi7);
            cm.Items.Add(mi8);
            cm.Items.Add(mi9);
            cm.Items.Add(mi10);

            return cm;
        }

        #endregion

        #region METHODS: Display Children

        protected override void PopulateChildren()
        {
            if (this.parent_canvas == null || this.node_component == null) return;

            if (this.IsSimple)
                this.PopulateChildrenSimple();
            else
                this.PopulateChildrenComplex();

            // adapt the size of the parent canvas
            this.parent_canvas.AdaptSize2Content();

        }

        protected void PopulateChildrenSimple()
        {
            // do not show parameters or calculations
            int nr_children = this.node_component.NrActiveChildren;
            int nr_params_calcs = this.node_component.ContainedParameters.Count() + this.node_component.ContainedCalculations.Count;
            int nr_subComps = this.node_component.NrActiveChildren - nr_params_calcs;

            double children_group_height = NodeVisualization.NODE_COMP_HEIGHT * nr_subComps +
                                           NodeVisualization.NODE_PADDING * (nr_subComps - 1);

            double diff_sizes = NodeVisualization.NODE_HEIGHT_DEFAULT - NodeVisualization.NODE_HEIGHT_SMALL;
            double offset_Y_child = NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP + diff_sizes * 0.5 + this.node_width * 0.5;
            Point posCENT = new Point(this.position.X + this.Width, this.position.Y + offset_Y_child);

            // initiate connection information
            this.node_connections_out = new List<Polyline>();

            // place children
            int counter = 0;
            foreach (var entry in this.node_component.ContainedComponents)
            {
                ParameterStructure.Component.Component sComp = entry.Value;
                if (sComp == null) continue;

                double offset_X = this.position.X + this.node_width + NodeVisualization.NODE_WIDTH_CONNECTION;
                double offset_Y = this.position.Y - children_group_height / 2 + NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP +
                                  counter * (NodeVisualization.NODE_COMP_HEIGHT + NodeVisualization.NODE_PADDING);
                ComponentVisualization sCompVis = new ComponentVisualization(this.parent_canvas, sComp, offset_X, offset_Y);
                sCompVis.IsSimple = true;
                sCompVis.Extents.PropertyChanged += sComp_Extents_PropertyChaged;
                this.Node_Children.Add(sCompVis);

                Polyline connection = this.CreateStepConnectingPolyline(posCENT, new Point(sCompVis.position.X, sCompVis.position.Y + offset_Y_child));
                this.node_connections_out.Add(connection);
                sCompVis.node_connections_in.Add(connection);

                counter++;
            }

            if (counter > 0)
            {
                this.Extents.UpperLeft = new Point(this.position.X,
                                                   this.position.Y - children_group_height / 2 + NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP * 2);
                this.Extents.LowerRight = new Point(this.position.X + NodeVisualization.NODE_HEIGHT_SMALL * 2 + NodeVisualization.NODE_WIDTH_CONNECTION,
                                                    this.position.Y + Math.Max(this.Height, children_group_height / 2 + NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP - NodeVisualization.NODE_WIDTH_MARKERS));
            }
            else
            {
                this.Extents.UpperLeft = this.position;
                this.Extents.LowerRight = new Point(this.position.X + this.Width,
                                                    this.position.Y + this.Height);
            }
            this.Extents.ParentTranslation = new Vector(0, 0);
            // DEBUG: update the bounding box
            this.extents_vis = this.CreateBoundingBoxPolyline(this.Extents);
            this.node_connections_out.Add(this.extents_vis);
        }

        protected void PopulateChildrenComplex()
        {
            int nr_children = this.node_component.NrActiveChildren;
            int nr_params_calcs = this.node_component.ContainedParameters.Count() + this.node_component.ContainedCalculations.Count;
            int nr_subComps = this.node_component.NrActiveChildren - nr_params_calcs;

            double children_group_height = NodeVisualization.NODE_HEIGHT_SMALL * nr_params_calcs +
                                           NodeVisualization.NODE_COMP_HEIGHT * nr_subComps +
                                           NodeVisualization.NODE_PADDING * (nr_children - 1);
            this.Extents.UpperLeft = new Point(this.position.X,
                                               this.position.Y - children_group_height / 2);
            this.Extents.LowerRight = new Point(this.position.X + NodeVisualization.NODE_WIDTH_DEFAULT * 2 + NodeVisualization.NODE_WIDTH_CONNECTION + NodeVisualization.NODE_WIDTH_SWITCHES,
                                                this.position.Y + Math.Max(this.Height, children_group_height / 2));
            this.Extents.ParentTranslation = new Vector(0, 0);

            // doesn't work before the element has been actually rendered
            // var posSUBTransf = this.node_switch_SUB.TransformToAncestor(this.parent_canvas);
            //Point posSUB = posSUBTransf.Transform(new Point(NodeVisualization.NODE_WIDTH_SWITCHES, NodeVisualization.NODE_WIDTH_SWITCHES / 2));
            Point posSUB = new Point(this.position.X + this.Width,
                                     this.position.Y + this.RowDefinitions[0].Height.Value + this.RowDefinitions[1].Height.Value * 0.5 - NodeVisualization.NODE_WIDTH_SWITCHES * 0.75);

            // initiate connection information
            this.node_connections_out = new List<Polyline>();
            // place children
            int counter = 0;
            foreach (var entry in this.node_component.ContainedParameters)
            {
                ParameterStructure.Parameter.Parameter p = entry.Value;
                if (p == null) continue;

                double offset_X = this.position.X + NodeVisualization.NODE_WIDTH_DEFAULT + NodeVisualization.NODE_WIDTH_CONNECTION;
                double offset_Y = this.position.Y - children_group_height / 2 + counter * (NodeVisualization.NODE_HEIGHT_SMALL + NodeVisualization.NODE_PADDING);
                ParameterVisualization pVis = new ParameterVisualization(this.parent_canvas, p, offset_X, offset_Y);
                this.Node_Children.Add(pVis);

                this.node_connections_out.Add(this.CreateStepConnectingPolyline(posSUB, new Point(offset_X, offset_Y + NodeVisualization.NODE_HEIGHT_SMALL * 0.5)));

                counter++;
            }

            foreach (Calculation calc in this.node_component.ContainedCalculations)
            {
                double offset_X = this.position.X + NodeVisualization.NODE_WIDTH_DEFAULT + NodeVisualization.NODE_WIDTH_CONNECTION;
                double offset_Y = this.position.Y - children_group_height / 2 + counter * (NodeVisualization.NODE_HEIGHT_SMALL + NodeVisualization.NODE_PADDING);
                CalculationVisualization calcVis = new CalculationVisualization(this.parent_canvas, calc, offset_X, offset_Y);
                this.Node_Children.Add(calcVis);

                this.node_connections_out.Add(this.CreateStepConnectingPolyline(posSUB, new Point(offset_X, offset_Y + NodeVisualization.NODE_HEIGHT_SMALL * 0.5)));

                counter++;
            }

            foreach (var entry in this.node_component.ContainedComponents)
            {
                ParameterStructure.Component.Component sComp = entry.Value;
                if (sComp == null) continue;

                double offset_X = this.position.X + NodeVisualization.NODE_WIDTH_DEFAULT + NodeVisualization.NODE_WIDTH_CONNECTION;
                double offset_Y = this.position.Y - children_group_height / 2 +
                                  nr_params_calcs * (NodeVisualization.NODE_HEIGHT_SMALL + NodeVisualization.NODE_PADDING) +
                                  (counter - nr_params_calcs) * (NodeVisualization.NODE_COMP_HEIGHT + NodeVisualization.NODE_PADDING);
                ComponentVisualization sCompVis = new ComponentVisualization(this.parent_canvas, sComp, offset_X, offset_Y);
                sCompVis.PropertyChanged += sComp_PropertyChanged;
                sCompVis.Extents.PropertyChanged += sComp_Extents_PropertyChaged;
                this.Node_Children.Add(sCompVis);

                Polyline connection = this.CreateStepConnectingPolyline(posSUB, new Point(offset_X, offset_Y + NodeVisualization.NODE_WIDTH_MARKER_TOOLTIP + NodeVisualization.NODE_HEIGHT_DEFAULT * 0.5));
                this.node_connections_out.Add(connection);
                sCompVis.node_connections_in.Add(connection);

                counter++;
            }

            // DEBUG: draw the bounding box
            this.extents_vis = this.CreateBoundingBoxPolyline(this.Extents);
            this.node_connections_out.Add(this.extents_vis);
        }

        

        #endregion

        #region METHODS: Display Connections to Refs

        protected override void DrawConnectionsToRefs()
        {
            if (this.parent_canvas == null || this.node_component == null) return;

            // get position information
            Point posREF = new Point(0, 0);
            if (this.IsSimple)
                posREF = new Point(this.position.X + this.Width,
                                   this.position.Y + this.Height * 0.5);
            else
                posREF = new Point(this.position.X + this.Width,
                                   this.position.Y + this.RowDefinitions[0].Height.Value + this.RowDefinitions[1].Height.Value * 0.5 + NodeVisualization.NODE_WIDTH_SWITCHES * 0.75);

            foreach(var entry in this.node_component.ReferencedComponents)
            {
                Component rComp = entry.Value;
                if (rComp == null) continue;

                // if the referenced component is shown (i.e. parent is expanded)
                bool found = false;
                foreach(object child in this.parent_canvas.Children)
                {
                    ComponentVisualization cv = child as ComponentVisualization;
                    if (cv != null && cv.node_component != null && cv.node_component.ID == rComp.ID)
                    {
                        // get position information
                        Point pos_r = new Point(cv.position.X, cv.position.Y + cv.Height * 0.5);

                        Polyline ref_con = this.Create2StepReferencePolyline(posREF, pos_r, this.Height, cv.Height, true);
                        this.node_references_out.Add(ref_con);
                        cv.node_references_in.Add(ref_con);

                        found = true;
                        break;
                    }
                }
                // if the referenced component is contained in a non-expanded parent
                if (!found)
                {
                    List<Component> parent_chain = this.parent_canvas.CompFactory.GetParentComponentChain(rComp);
                    parent_chain.Remove(rComp);
                    parent_chain.Reverse();
                    foreach(Component pComp in parent_chain)
                    {
                        if (found) break;

                        foreach (object child in this.parent_canvas.Children)
                        {
                            ComponentVisualization cv = child as ComponentVisualization;
                            if (cv != null && cv.node_component != null && cv.node_component.ID == pComp.ID)
                            {
                                // get position information
                                Point pos_r = new Point(cv.position.X, cv.position.Y + cv.Height * 0.5);

                                Polyline ref_con = this.Create2StepReferencePolyline(posREF, pos_r, this.Height, cv.Height, false);
                                this.node_references_out.Add(ref_con);
                                cv.node_references_in.Add(ref_con);

                                found = true;
                                break;
                            }
                        }
                    }
                }
            }

        }

        #endregion

        #region METHODS: Connections from Params to Calc

        // call after all children have been defined
        protected override void DrawConnectionsParamsCalc()
        {
            if (this.parent_canvas == null) return;
            NodeVisualization.NR_COMP_TO_PARAM_CONNECTION_CALLS = 0;

            Dictionary<CalculationVisualization, List<long>> to_connect_in = new Dictionary<CalculationVisualization, List<long>>();
            Dictionary<CalculationVisualization, List<long>> to_connect_ret = new Dictionary<CalculationVisualization, List<long>>();
            foreach(NodeVisualization nv in this.node_children)
            {
                CalculationVisualization calv = nv as CalculationVisualization;
                if (calv == null) continue;
                to_connect_in.Add(calv, calv.ParamsInIDs);
                to_connect_ret.Add(calv, calv.ParamsOutIDs);                
            }

            this.ConnectCalcToParam(this, ref to_connect_in, true);
            this.ConnectCalcToParam(this, ref to_connect_ret, false);
        }

        protected void ConnectCalcToParam(ComponentVisualization _owner_out, ref Dictionary<CalculationVisualization, List<long>> _to_connect, bool _in)
        {
            if (_to_connect == null) return;

            foreach (var entry in _to_connect)
            {
                if (entry.Value.Count == 0) continue;

                CalculationVisualization calv = entry.Key;
                Point start = calv.Position + new Vector(calv.Width, calv.Height * 0.5);                            
                
                // look for input parameters on the same hierarchy level
                foreach (NodeVisualization nv in this.node_children)
                {
                    ParameterVisualization pv = nv as ParameterVisualization;
                    
                    if (pv != null)
                    {
                        if (entry.Value.Contains(pv.VisID))
                        {
                            // create a polyline
                            Point end = pv.Position + new Vector(pv.Width, pv.Height * 0.5);
                            Polyline connection = _owner_out.Create2StepCalcToParamPolyline(start, end, pv.Height, pv.Height, _in);
                            _owner_out.node_param_calc_out.Add(connection);
                            this.node_param_calc_in.Add(connection);
                            entry.Value.Remove(pv.VisID);
                        }
                    }
                }

                // look deeper (a calculation has parameters from the same component or a subcomponent)
                foreach (NodeVisualization nv in this.node_children)
                {
                    ComponentVisualization cv = nv as ComponentVisualization;
                    if (cv != null)
                    {
                        cv.ConnectCalcToParam(_owner_out, ref _to_connect, _in);
                    }
                }
            }
        }

        #endregion

        #region METHODS: Info

        public bool CompManagerHasWritingAccess(ComponentManagerType _m)
        {
            return this.node_component.AccessLocal[_m].AccessTypeFlags.HasFlag(ComponentAccessType.WRITE);           
        }

        public override string ToString()
        {
            if (this.node_component != null)
                return "CompViz: " + this.node_component.ID.ToString() + " " + this.node_component.Name;
            else
                return "CompViz: ";
        }

        #endregion

        #region METHODS: Utilities

        protected void ResetExtents()
        {
            this.Extents.UpperLeft = (this.extents_vis == null) ? this.position : this.extents_vis.Points[0];
            this.Extents.LowerRight = this.Extents.UpperLeft + new Vector(this.Width, this.Height);
            // in case too few children not reaching the bottom of the component node
            this.Extents.LowerRight = new Point(Math.Max(this.Extents.LowerRight.X, this.position.X + this.Width),
                                                Math.Max(this.Extents.LowerRight.Y, this.position.Y + this.Height));

            //string debug = "ResetExtents:\n";            
            foreach(NodeVisualization nv in this.node_children)
            {
                //debug += "parent: " + this.Extents.ToString() + "\n";
                //debug += "child : " + nv.Extents.ToString() + "\n\n";

                // determine change in the TranslateTransforms of the Extents of parent and child
                Vector diff = nv.Extents.ParentTranslation - this.Extents.ParentTranslation;

                this.Extents.UpperLeft = new Point(Math.Min(nv.Extents.UpperLeft.X + diff.X, this.Extents.UpperLeft.X),
                                                   Math.Min(nv.Extents.UpperLeft.Y + diff.Y, this.Extents.UpperLeft.Y));

                this.Extents.LowerRight = new Point(Math.Max(nv.Extents.LowerRight.X + diff.X, this.Extents.LowerRight.X),
                                                    Math.Max(nv.Extents.LowerRight.Y + diff.Y, this.Extents.LowerRight.Y));
                
            }
            //debug += "parent: " + this.Extents.ToString() + "\n";            
        }

        protected void AdaptExtentsToChild(BoundingBox bb)
        {
            // GROW the Extents of this
            this.ResetExtents();

            // trigger recursion up the parent chain
            Vector tmp = this.Extents.ParentTranslation;
            this.Extents.ParentTranslation = tmp;

            // update graph
            if (this.extents_vis != null)
                this.extents_vis.Points = NodeVisualization.CreateBoundingBoxPointCollection(this.Extents.UpperLeft, this.Extents.LowerRight);
        }

        // to be called after an IsExpanded = true in order to avoid overlaps
        internal void RepositionChildren()
        {
            // gather info
            Dictionary<ComponentVisualization, BoundingBox> info = new Dictionary<ComponentVisualization, BoundingBox>();
            Dictionary<ComponentVisualization, Vector> to_move = new Dictionary<ComponentVisualization, Vector>();                        
            foreach(NodeVisualization nv in this.node_children)
            {
                ComponentVisualization cv = nv as ComponentVisualization;
                if (cv == null) continue;

                info.Add(cv, cv.Extents);
                to_move.Add(cv, new Vector(0, 0));
            }

            // find vertical overlap and move lower child further down
            for(int i = 1; i < info.Count; i++)
            {
                Point prev_LR = info.ElementAt(i - 1).Value.LowerRight;
                Point current_UL = info.ElementAt(i).Value.UpperLeft;
                to_move[info.ElementAt(i).Key] = to_move[info.ElementAt(i - 1).Key] + new Vector(0, Math.Max(0, prev_LR.Y - current_UL.Y));
            }

            // perform translation
            foreach(var entry in to_move)
            {
                entry.Key.Translate(entry.Value);
                entry.Key.TranslateConnectionsIn(entry.Value);
                entry.Key.TranslateRefConnections(entry.Value);
            }

            // adapt to new children positions
            this.ResetExtents();

            // update graph
            if (this.extents_vis != null)
                this.extents_vis.Points = NodeVisualization.CreateBoundingBoxPointCollection(this.Extents.UpperLeft, this.Extents.LowerRight);

        }

        #endregion

        #region EVENT HANDLER

        protected void node_comp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ParameterStructure.Component.Component component = sender as ParameterStructure.Component.Component;
            if (component == null || e == null) return;

            if (e.PropertyName == "Children" || e.PropertyName == "SymbolImage")
            {
                this.UpdateContent();
            }
        }

        protected void marker_manager_bgr_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {           
            foreach(var child in this.Children)
            {
                TextBlock tb = child as TextBlock;
                if (tb == null || tb.Tag == null || !(tb.Tag is int)) continue;

                tb.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        protected void marker_manager_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Shape s = sender as Shape;
            if (s == null || s.Tag == null || !(s.Tag is int)) return;

            int index = (int)s.Tag;

            foreach (var child in this.Children)
            {
                TextBlock tb = child as TextBlock;
                if (tb == null || tb.Tag == null || !(tb.Tag is int)) continue;

                int tb_index = (int)tb.Tag;
                if (index == tb_index)
                    tb.Visibility = System.Windows.Visibility.Visible;
                else
                    tb.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        protected void rect_SELECT_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Rectangle rect = sender as Rectangle;
            if (rect == null || this.parent_canvas == null) return;
            if (!this.IsUserManipulatable) return;

            if (this.VisState.HasFlag(NodeVisHighlight.Selected))
                this.parent_canvas.DeselectNode(this);
            else
                this.parent_canvas.SelectNode(this);
        }

        protected void rect_ACTIVATE_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Rectangle rect = sender as Rectangle;
            if (rect == null) return;
            if (this.VisState.HasFlag(NodeVisHighlight.Active))
            {
                this.parent_canvas.DeselectNode(this);
                this.VisState = NodeVisHighlight.Inactive;
                this.IsUserManipulatable = false;
            }
            else
            {
                this.VisState |= NodeVisHighlight.Active;
                this.IsUserManipulatable = true;
            }
        }

        protected void sComp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ComponentVisualization cv = sender as ComponentVisualization;
            if (cv == null || e == null) return;

            if (e.PropertyName == "IsExpanded" && this.IsExpanded)
            {
                this.RemoveConnectionsParamsCalc(false);
                this.DrawConnectionsParamsCalc();
            }
        }

        #endregion

        #region EVENT HANDLER: Extents of Children

        protected void sComp_Extents_PropertyChaged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            BoundingBox bb = sender as BoundingBox;
            if (bb == null || e == null) return;

            if (e.PropertyName == "ParentTranslation")
            {
                this.AdaptExtentsToChild(bb);
            }
        }

        #endregion
    }
}
