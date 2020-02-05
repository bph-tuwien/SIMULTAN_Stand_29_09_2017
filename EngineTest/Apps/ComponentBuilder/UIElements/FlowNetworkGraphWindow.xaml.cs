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
using System.ComponentModel;

using ComponentBuilder.WinUtils;

using ParameterStructure.Component;

namespace ComponentBuilder.UIElements
{

    public enum ManagementState
    {
        NEUTRAL,
        ADD_NODE,
        ADD_EDGE,
        ADD_NETWORK,
        REROUTE_EDGE,
        REMOVE,
        ASSIGN_COMP,
        DISASSOC_COMP,
        DEFINE_SOURCE,
        DEFINE_SINK,
        DEFINE_SIZE_OF_COMP,
        DEFINE_OPERATIONS_IN_NODE,
        COPY_OPERATION_TO_ALL_INST
    }

    /// <summary>
    /// Interaction logic for FlowNetworkGraphWindow.xaml
    /// </summary>
    public partial class FlowNetworkGraphWindow : Window, INotifyPropertyChanged
    {
        #region STATIC

        protected static double CANVAS_EXPANSION_STEP = 100.0;

        #endregion

        public FlowNetworkGraphWindow()
        {
            InitializeComponent();
            this.InitControls();
            this.Loaded += FlowNetworkGraphWindow_Loaded;
        }

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

        #region PROPETIES
        
        private FlowNetwork network;
        public FlowNetwork Network
        {
            get { return this.network; }
            set 
            { 
                this.network = value;
                if (this.network != null)
                    this.Title = "Flow Network Graph: " + this.network.Name;
                if (this.canv != null)
                    this.canv.NetworkToDisplay = this.network;
                this.UpdateEditingControls();
            }
        }

        private ComponentFactory comp_manager;
        public ComponentFactory CompManager
        {
            get { return this.comp_manager; }
            set
            {
                this.comp_manager = value;
                if (this.canv != null)
                    this.canv.CompFactory = this.comp_manager;
            }
        }

        private bool is_picking_comp;
        public bool IsPickingComp
        {
            get { return this.is_picking_comp; }
            private set 
            { 
                this.is_picking_comp = value;
                this.RegisterPropertyChanged("IsPickingComp");
            }
        }

        private ParameterStructure.Component.Component picked_comp;
        public ParameterStructure.Component.Component PickedComp 
        {
            get { return this.picked_comp; }
            set
            {
                this.picked_comp = value;
                if (this.canv != null)
                    this.canv.CompToAssign = this.picked_comp;
            }
        }

        #endregion

        #region CLASS MEMBERS

        private Binding binding_name;
        private Binding binding_descr;
        private Binding binding_suffix_to_show;

        #endregion

        #region METHODS: Init
        private void InitControls()
        {
            this.canv.PropertyChanged += canv_PropertyChanged;

            this.tbtn_add_node.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_add_node.CommandParameter = "ADD_NODE";
            this.tbtn_add_node.IsChecked = (this.canv.GuiState == ManagementState.ADD_NODE);

            this.tbtn_add_edge.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_add_edge.CommandParameter = "ADD_EDGE";
            this.tbtn_add_edge.IsChecked = (this.canv.GuiState == ManagementState.ADD_EDGE);

            this.tbtn_remove.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_remove.CommandParameter = "REMOVE";
            this.tbtn_remove.IsChecked = (this.canv.GuiState == ManagementState.REMOVE);

            this.tbtn_reroute_edge.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_reroute_edge.CommandParameter = "REROUTE_EDGE";
            this.tbtn_reroute_edge.IsChecked = (this.canv.GuiState == ManagementState.REROUTE_EDGE);

            this.tbtn_add_nw.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_add_nw.CommandParameter = "ADD_NETWORK";
            this.tbtn_add_nw.IsChecked = (this.canv.GuiState == ManagementState.ADD_NETWORK);

            this.tbtn_assign_comp.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_assign_comp.CommandParameter = "ASSIGN_COMP";
            this.tbtn_assign_comp.IsChecked = (this.canv.GuiState == ManagementState.ASSIGN_COMP);

            this.tbtn_del_comp.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_del_comp.CommandParameter = "DISASSOC_COMP";
            this.tbtn_del_comp.IsChecked = (this.canv.GuiState == ManagementState.DISASSOC_COMP);

            this.tbtn_source.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_source.CommandParameter = "DEFINE_SOURCE";
            this.tbtn_source.IsChecked = (this.canv.GuiState == ManagementState.DEFINE_SOURCE);

            this.tbtn_sink.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_sink.CommandParameter = "DEFINE_SINK";
            this.tbtn_sink.IsChecked = (this.canv.GuiState == ManagementState.DEFINE_SINK);

            this.tbtn_size_inst.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_size_inst.CommandParameter = "DEFINE_SIZE_OF_COMP";
            this.tbtn_size_inst.IsChecked = (this.canv.GuiState == ManagementState.DEFINE_SIZE_OF_COMP);

            this.tbtn_operations_node.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_operations_node.CommandParameter = "DEFINE_OPERATIONS_IN_NODE";
            this.tbtn_operations_node.IsChecked = (this.canv.GuiState == ManagementState.DEFINE_OPERATIONS_IN_NODE);

            this.tbtn_operations_copy.Command = new RelayCommand((x) => OnSwitchState(x));
            this.tbtn_operations_copy.CommandParameter = "COPY_OPERATION_TO_ALL_INST";
            this.tbtn_operations_copy.IsChecked = (this.canv.GuiState == ManagementState.COPY_OPERATION_TO_ALL_INST);

            this.btn_goto_parent.Command = new RelayCommand((x) => this.canv.GoToParentNetwork(), (x) => this.canv != null);
            
            this.btn_resize_canv.Command = new RelayCommand((x) => this.canv.FitSize2Content(), (x) => this.canv != null);
            this.btn_snap.Command = new RelayCommand((x) => this.canv.AlignContentToGrid(), (x) => this.canv != null);
            this.btn_mark_parent.Command = new RelayCommand((x) => this.canv.ShowCommonNodeContentParent(), (x) => this.canv != null);
            this.btn_expX_canv.Command = new RelayCommand((x) => this.canv.ExpandCanvas(CANVAS_EXPANSION_STEP, 0), (x) => this.canv != null);
            this.btn_expY_canv.Command = new RelayCommand((x) => this.canv.ExpandCanvas(0, CANVAS_EXPANSION_STEP), (x) => this.canv != null);
            this.btn_expXmin_canv.Command = new RelayCommand((x) => this.canv.ExpandCanvasNeg(CANVAS_EXPANSION_STEP, 0), (x) => this.canv != null);
            this.btn_expYmin_canv.Command = new RelayCommand((x) => this.canv.ExpandCanvasNeg(0, CANVAS_EXPANSION_STEP), (x) => this.canv != null);

            this.cb_pnames.SelectionChanged += cb_pnames_SelectionChanged;

            this.btn_save_canv.Command = new RelayCommand(
            (x) => 
            {
                // resize to avoid bug in the canvas rendering
                this.WindowState = System.Windows.WindowState.Normal;
                this.Width = this.MinWidth;
                this.Height = this.MinHeight;
                this.scrl_main.ScrollToTop();
                this.scrl_main.ScrollToLeftEnd();
                this.canv.SaveCanvasAsImage();
            },
            (x) => this.canv != null);

            this.btn_all_comp_hL.Command = new RelayCommand((x) => this.canv.HighlighContainedComponents(true), (x) => this.canv != null);
            this.btn_all_comp_uNhL.Command = new RelayCommand((x) => this.canv.HighlighContainedComponents(false), (x) => this.canv != null);
            this.btn_all_comp_vis.Command = new RelayCommand((x) => this.canv.TurnAllContainedComponentsOn(), (x) => this.canv != null);
            this.btn_all_comp_inst_hL.Command = new RelayCommand((x) => this.canv.HighlightInstancesOfContainedComponent(true), (x) => this.canv != null && this.canv.CanExecute_HighlightInstancesOfContainedComponent());
            this.btn_all_comp_inst_UnhL.Command = new RelayCommand((x) => this.canv.HighlightInstancesOfContainedComponent(false), (x) => this.canv != null);

            this.btn_calc_flow_FORW.Command = new RelayCommand((x) => { this.canv.SynchFlows(true); this.UpdateParamListForDisplay(); }, (x) => this.canv != null);
            this.btn_calc_flow_BACKW.Command = new RelayCommand((x) => { this.canv.SynchFlows(false); this.UpdateParamListForDisplay(); }, (x) => this.canv != null);
            this.btn_calc_flow_RESET.Command = new RelayCommand((x) => { this.canv.ResetFlows(); this.UpdateParamListForDisplay(); }, (x) => this.canv != null);

            this.btn_calc_flow_FORW_1.Command = new RelayCommand((x) => { this.canv.SynchFlows_1Step(true, this.txb_suffix_to_display.Text); this.UpdateParamListForDisplay(); }, (x) => this.canv != null);
            this.btn_calc_flow_BACKW_1.Command = new RelayCommand((x) => { this.canv.SynchFlows_1Step(false, this.txb_suffix_to_display.Text); this.UpdateParamListForDisplay(); }, (x) => this.canv != null);
        }

        
        private void UpdateEditingControls()
        {
            this.binding_name = new Binding();
            this.binding_name.Path = new PropertyPath("Name");
            this.binding_name.Source = this.Network;
            this.binding_name.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(this.txb_name, TextBox.TextProperty, this.binding_name);

            this.binding_descr = new Binding();
            this.binding_descr.Path = new PropertyPath("Description");
            this.binding_descr.Source = this.Network;
            this.binding_descr.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(this.txb_descr, TextBox.TextProperty, this.binding_descr);

            this.binding_suffix_to_show = new Binding();
            this.binding_suffix_to_show.Path = new PropertyPath("Suffix_To_Display");
            this.binding_suffix_to_show.Source = this.Network;
            this.binding_suffix_to_show.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(this.txb_suffix_to_display, TextBox.TextProperty, this.binding_suffix_to_show);
        }

        private void UpdateParamListForDisplay()
        {
            if (this.canv != null)
                this.cb_pnames.ItemsSource = new List<string>(this.canv.GetUniqueParamNamesInNW());
        }
        
        #endregion

        #region METHODS: State Switch

        private void OnSwitchState(object _input)
        {
            if (this.canv == null) return;

            if (_input == null) return;
            string input_str = _input.ToString();
            if (string.IsNullOrEmpty(input_str)) return;
            ManagementState to_state = FlowNetworkGraphWindow.StringToManagementState(input_str);

            this.canv.OnSwitchState(to_state);
        }

        #endregion

        #region UTILS

        private static ManagementState StringToManagementState(string _state_str)
        {
            if (string.IsNullOrEmpty(_state_str)) return ManagementState.NEUTRAL;

            switch(_state_str)
            {
                case "ADD_NODE":
                    return ManagementState.ADD_NODE;
                case "ADD_EDGE":
                    return ManagementState.ADD_EDGE;
                case "ADD_NETWORK":
                    return ManagementState.ADD_NETWORK;
                case "REMOVE":
                    return ManagementState.REMOVE;
                case "REROUTE_EDGE":
                    return ManagementState.REROUTE_EDGE;
                case "ASSIGN_COMP":
                    return ManagementState.ASSIGN_COMP;
                case "DISASSOC_COMP":
                    return ManagementState.DISASSOC_COMP;
                case "DEFINE_SOURCE":
                    return ManagementState.DEFINE_SOURCE;
                case "DEFINE_SINK":
                    return ManagementState.DEFINE_SINK;
                case "DEFINE_SIZE_OF_COMP":
                    return ManagementState.DEFINE_SIZE_OF_COMP;
                case "DEFINE_OPERATIONS_IN_NODE":
                    return ManagementState.DEFINE_OPERATIONS_IN_NODE;
                case "COPY_OPERATION_TO_ALL_INST":
                    return ManagementState.COPY_OPERATION_TO_ALL_INST;
                default:
                    return ManagementState.NEUTRAL;
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void FlowNetworkGraphWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // editing setup
            this.UpdateEditingControls();  
        }

        private void canv_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e == null) return;
            if (e.PropertyName == "GuiState")
            {
                this.tbtn_add_node.IsChecked = (this.canv.GuiState == ManagementState.ADD_NODE);
                this.tbtn_add_node.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.ADD_NODE);

                this.tbtn_add_edge.IsChecked = (this.canv.GuiState == ManagementState.ADD_EDGE);
                this.tbtn_add_edge.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.ADD_EDGE);

                this.tbtn_remove.IsChecked = (this.canv.GuiState == ManagementState.REMOVE);
                this.tbtn_remove.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.REMOVE);

                this.tbtn_add_nw.IsChecked = (this.canv.GuiState == ManagementState.ADD_NETWORK);
                this.tbtn_add_nw.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.ADD_NETWORK);
                
                this.tbtn_assign_comp.IsChecked = (this.canv.GuiState == ManagementState.ASSIGN_COMP);
                this.tbtn_assign_comp.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.ASSIGN_COMP);

                this.tbtn_del_comp.IsChecked = (this.canv.GuiState == ManagementState.DISASSOC_COMP);
                this.tbtn_del_comp.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.DISASSOC_COMP);

                this.tbtn_reroute_edge.IsChecked = (this.canv.GuiState == ManagementState.REROUTE_EDGE);
                this.tbtn_reroute_edge.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.REROUTE_EDGE);

                this.tbtn_source.IsChecked = (this.canv.GuiState == ManagementState.DEFINE_SOURCE);
                this.tbtn_source.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.DEFINE_SOURCE);

                this.tbtn_sink.IsChecked = (this.canv.GuiState == ManagementState.DEFINE_SINK);
                this.tbtn_sink.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.DEFINE_SINK);

                this.tbtn_size_inst.IsChecked = (this.canv.GuiState == ManagementState.DEFINE_SIZE_OF_COMP);
                this.tbtn_size_inst.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.DEFINE_SIZE_OF_COMP);

                this.tbtn_operations_node.IsChecked = (this.canv.GuiState == ManagementState.DEFINE_OPERATIONS_IN_NODE);
                this.tbtn_operations_node.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.DEFINE_OPERATIONS_IN_NODE);

                this.tbtn_operations_copy.IsChecked = (this.canv.GuiState == ManagementState.COPY_OPERATION_TO_ALL_INST);
                this.tbtn_operations_copy.IsEnabled = (this.canv.GuiState == ManagementState.NEUTRAL || this.canv.GuiState == ManagementState.COPY_OPERATION_TO_ALL_INST);

            }
            else if (e.PropertyName == "IsPickingComp" && this.canv != null)
            {
                this.IsPickingComp = this.canv.IsPickingComp;
            }
        }

        private void cb_pnames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_pnames.SelectedItem == null) return;
            this.txb_suffix_to_display.Text = cb_pnames.SelectedItem.ToString();
        }


        #endregion

    }
}
