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
using ComponentBuilder.WpfUtils;

using ParameterStructure.Component;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for Comp2CompMappingWindow.xaml
    /// </summary>
    public partial class Comp2CompMappingWindow : Window, INotifyPropertyChanged
    {
        #region STATIC

        private const int HALF_H_TV_ROW_COMP = 9;
        private const double CONN_OFFSET = 2;

        private static readonly Color COL_MAPPING_INPUT = (Color)ColorConverter.ConvertFromString("#ff0000ff");
        private static readonly Color COL_MAPPING_OUTPUT = (Color)ColorConverter.ConvertFromString("#ff967100");
        private static readonly Color COL_DEFAULT = (Color)ColorConverter.ConvertFromString("#ff000000");

        #endregion

        public Comp2CompMappingWindow()
        {
            InitializeComponent();
            this.Loaded += Comp2CompMappingWindow_Loaded;
            this.SizeChanged += Comp2CompMappingWindow_SizeChanged;
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

        #region PROPERTIES: Component Selection

        private ParameterStructure.Component.Component c_data;
        public ParameterStructure.Component.Component CompData
        {
            get { return this.c_data; }
            set
            {
                if (!(value.IsLocked))
                {
                    this.c_data = value;
                    this.RegisterPropertyChanged("CompData");
                    this.IsPickingCompData = false;
                    //this.IsInMapEditingMode = false;
                    this.UpdateDisplayOnCompDataLoad();
                }
            }
        }

        private bool is_picking_c_data;
        public bool IsPickingCompData
        {
            get { return this.is_picking_c_data; }
            set 
            { 
                this.is_picking_c_data = value;
                this.RegisterPropertyChanged("IsPickingCompData");
                this.UpdateButtons();
            }
        }


        private ParameterStructure.Component.Component c_calculator;
        public ParameterStructure.Component.Component CompCalculator
        {
            get { return this.c_calculator; }
            set 
            {
                if (value != null && !(value.IsLocked))
                {
                    this.c_calculator = value;
                    this.c_calculator.ResetToDefaultValuesBeforeCalculation();
                    this.RegisterPropertyChanged("CompCalculator");
                    this.IsPickingCompCalculator = false;
                    //this.IsInMapEditingMode = false;
                    this.UpdateDisplayOnCompCalculatorLoad();
                }
                else if (value == null)
                {
                    this.c_calculator = null;
                    this.RegisterPropertyChanged("CompCalculator");
                    this.IsPickingCompCalculator = false;
                    //this.IsInMapEditingMode = false;
                    this.UpdateDisplayOnCompCalculatorLoad();
                }
            }
        }

        private bool is_picking_c_calc;
        public bool IsPickingCompCalculator
        {
            get { return this.is_picking_c_calc; }
            set
            { 
                this.is_picking_c_calc = value;
                this.RegisterPropertyChanged("IsPickingCompCalculator");
                this.UpdateButtons();
            }
        }

        #endregion

        #region PROPERTIES: Mapping

        private Dictionary<ParameterStructure.Parameter.Parameter, ParameterStructure.Parameter.Parameter> current_input_mapping;
        private Dictionary<ParameterStructure.Parameter.Parameter, ParameterStructure.Parameter.Parameter> current_output_mapping;
        private ParameterStructure.Mapping.Mapping2Component current_mapping;

        private bool is_mapping;
        public bool IsMapping 
        {
            get { return this.is_mapping; }
            private set
            {
                this.is_mapping = value;
                this.UpdateButtons();
            }
        }

        private ParameterStructure.Parameter.Parameter mapped_source_parameter;
        public ParameterStructure.Parameter.Parameter MappedSourceParameter
        {
            get { return this.mapped_source_parameter; }
            set 
            { 
                if (this.IsMapping && value != null)
                {
                    this.mapped_source_parameter = value;
                    this.SelectedMappingSource = true;
                }
                else
                    this.mapped_source_parameter = null;
            }
        }


        // derived
        public bool SelectedMappingSource { get; private set; }

        private bool is_in_map_editing_mode;
        public bool IsInMapEditingMode
        {
            get { return this.is_in_map_editing_mode; }
            private set
            {
                this.is_in_map_editing_mode = value;
                if (this.tv_comp_calc != null && this.tv_comp_calc.IsLoaded)
                {
                    this.tv_comp_calc.BorderBrush = (this.is_in_map_editing_mode) ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.DimGray);
                    this.tv_comp_calc.BorderThickness = (this.is_in_map_editing_mode) ? new Thickness(1, 1, 1, 2) : new Thickness(1);
                }
            }
        }

        #endregion

        #region METHODS: Init, Update Controls

        private void InitControls()
        {
            // component picking
            this.IsPickingCompData = false;
            this.IsPickingCompCalculator = false;
            this.IsMapping = false;
            this.IsInMapEditingMode = false;

            this.tbtn_get_data.Command = new RelayCommand((x) => this.IsPickingCompData = !(this.IsPickingCompData),
                                                          (x) => !this.IsPickingCompCalculator && !this.IsMapping);
            this.tbtn_get_data.IsChecked = false;

            this.tbtn_get_calc.Command = new RelayCommand((x) => { this.cb_mappings.SelectedItem = null; this.IsPickingCompCalculator = !(this.IsPickingCompCalculator); },
                                                          (x) => !this.IsPickingCompData && !this.IsMapping && this.CompData != null);
            this.tbtn_get_calc.IsChecked = false;

            // mapping
            this.current_input_mapping = new Dictionary<ParameterStructure.Parameter.Parameter, ParameterStructure.Parameter.Parameter>();
            this.current_output_mapping = new Dictionary<ParameterStructure.Parameter.Parameter, ParameterStructure.Parameter.Parameter>();
            this.current_mapping = null;

            this.tbtn_map_manual.Command = new RelayCommand((x) => this.IsMapping = !(this.IsMapping),
                                                            (x) => !this.IsPickingCompData && !this.IsPickingCompCalculator);
            this.tbtn_map_manual.IsChecked = this.IsMapping;
            this.btn_P2Pmap_delete.Command = new RelayCommand((x) => RemoveP2PMapping());

            this.tv_comp_data.SelectedItemChanged += tv_comp_data_SelectedItemChanged;
            this.tv_comp_calc.SelectedItemChanged += tv_comp_calc_SelectedItemChanged;
            this.tv_comp_calc.MouseMove += tv_comp_calc_MouseMove;
            this.tv_comp_data.ActOnTreeViewItemExpandedChangedHandler = this.UpdateMappingDisplay;
            this.tv_comp_calc.ActOnTreeViewItemExpandedChangedHandler = this.UpdateMappingDisplay;            

            this.c_pointer_WS.MouseMove += c_pointer_WS_MouseMove;
            this.c_pointer_WS.PreviewMouseMove += c_pointer_WS_MouseMove;

            this.btn_OK.Command = new RelayCommand((x) => this.AddMappingToComponent(),
                                                   (x) => !this.IsMapping && !this.IsPickingCompData && !this.IsPickingCompCalculator);
            this.btn_Del.Command = new RelayCommand((x) => this.RemoveSelectedMapping(),
                                              (x) => CanExecute_RemoveSelectedMapping());

            this.btn_calculate.Command = new RelayCommand((x) => this.TestCalculateMapping(),
                                               (x) => this.CanExecute_TestCalculateMapping());
            this.btn_P_highlight.Command = new RelayCommand((x) => this.TogglePHighlight(),
                                                 (x) => this.CanExecute_TogglePHighlight());

            this.cb_mappings.SelectionChanged += cb_mappings_SelectionChanged;
        }


        private void UpdateButtons(ParameterStructure.Parameter.Parameter _source_param_selected = null)
        {
            this.tbtn_get_data.IsEnabled = (!this.IsPickingCompCalculator && !this.IsMapping);
            this.tbtn_get_data.IsChecked = this.IsPickingCompData;

            this.tbtn_get_calc.IsEnabled = (!this.IsPickingCompData && !this.IsMapping);
            this.tbtn_get_calc.IsChecked = this.IsPickingCompCalculator;

            this.tbtn_map_manual.IsEnabled = (!this.IsPickingCompData && !this.IsPickingCompCalculator);
            this.tbtn_map_manual.IsChecked = this.IsMapping;

            if (_source_param_selected != null)
                this.btn_P2Pmap_delete.IsEnabled = (this.current_input_mapping.ContainsKey(_source_param_selected) || this.current_output_mapping.ContainsKey(_source_param_selected));
            else
                this.btn_P2Pmap_delete.IsEnabled = false;
        }


        private void UpdateDisplayOnCompDataLoad()
        {
            if (this.c_data != null)
            {
                this.tb_comp_data.Text = this.c_data.ToInfoString();
                this.tv_comp_data.ItemsSource = new List<ParameterStructure.Component.Component> { this.c_data };
                this.cb_mappings.ItemsSource = new List<ParameterStructure.Mapping.Mapping2Component>(this.c_data.Mappings2Comps);
            }
            else
            {
                this.tb_comp_data.Text = "Komponent";
                this.tv_comp_data.ItemsSource = new List<ParameterStructure.Component.Component>();
                this.cb_mappings.ItemsSource = new List<ParameterStructure.Mapping.Mapping2Component>();
            }
        }

        private void UpdateDisplayOnCompCalculatorLoad()
        {
            if (this.c_calculator != null)
            {
                this.tb_comp_calculator.Text = this.c_calculator.ToInfoString();
                this.tv_comp_calc.ItemsSource = new List<ParameterStructure.Component.Component> { this.c_calculator };
            }
            else
            {
                this.tb_comp_calculator.Text = "Komponent";
                this.tv_comp_calc.ItemsSource = new List<ParameterStructure.Component.Component>();
            }
        }

        #endregion

        #region METHODS: Mapping

        private void CreateP2PMapping(ParameterStructure.Parameter.Parameter _target_parameter)
        {
            if (this.MappedSourceParameter == null) return;
            if (_target_parameter == null) return;

            if (_target_parameter.Propagation == InfoFlow.INPUT)
            {
                if (this.current_input_mapping.ContainsKey(this.MappedSourceParameter) ||
                    this.current_input_mapping.ContainsValue(_target_parameter))
                {
                    MessageBox.Show("Parameter Mapping Error: Cannot map the same parameter twice!", "Parameter Mapping Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (this.MappedSourceParameter.Propagation == InfoFlow.OUPUT || this.MappedSourceParameter.Propagation == InfoFlow.CALC_IN)
                {
                    MessageBox.Show("Parameter Mapping Error: Cannot map output to input!", "Parameter Mapping Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    this.current_input_mapping.Add(this.MappedSourceParameter, _target_parameter);
                }                
            }
            else if (_target_parameter.Propagation == InfoFlow.MIXED || _target_parameter.Propagation == InfoFlow.OUPUT)
            {
                if (this.current_output_mapping.ContainsKey(this.MappedSourceParameter) ||
                    this.current_output_mapping.ContainsValue(_target_parameter))
                {
                    MessageBox.Show("Parameter Mapping Error: Cannot map the same parameter twice!", "Parameter Mapping Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (this.MappedSourceParameter.Propagation != InfoFlow.MIXED && this.MappedSourceParameter.Propagation != InfoFlow.OUPUT)
                {
                     MessageBox.Show("Parameter Mapping Error: Cannot map input to output!", "Parameter Mapping Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);                
                }
                else
                {
                    this.current_output_mapping.Add(this.MappedSourceParameter, _target_parameter);
                }                
            }                

            this.MappedSourceParameter = null;
        }

        private void RemoveP2PMapping()
        {
            if (this.tv_comp_data == null) return;
            if (this.tv_comp_data.SelectedItem == null) return;

            ParameterStructure.Parameter.Parameter selP = this.tv_comp_data.SelectedItem as ParameterStructure.Parameter.Parameter;
            if (selP == null) return;

            if (this.current_input_mapping.ContainsKey(selP))
                this.current_input_mapping.Remove(selP);
            else if (this.current_output_mapping.ContainsKey(selP))
                this.current_output_mapping.Remove(selP);

            this.UpdateMappingTrackingDisplay();
        }

        private void AddMappingToComponent()
        {
            if (this.c_data == null || this.c_calculator == null) return;
            if (this.current_input_mapping.Count == 0) return;
            if (this.current_output_mapping.Count == 0) return;

            string name = (string.IsNullOrEmpty(this.tb_mapping_name.Text)) ? "Mapping to " + this.c_calculator.Name : this.tb_mapping_name.Text;
            Comp2CompMappingErr err = Comp2CompMappingErr.NONE;

            if (this.IsInMapEditingMode)
            {
                // save changes to mapping
                this.c_data.EditMapping(this.current_mapping, this.current_input_mapping, this.current_output_mapping, out err);
                this.IsInMapEditingMode = false;
            }
            else
            {
                // create new mapping                
                this.current_mapping = this.c_data.CreateMappingTo(name, this.c_calculator, this.current_input_mapping, this.current_output_mapping, out err); 
            }

            if (err != Comp2CompMappingErr.NONE)
            {
                MessageBox.Show("Mapping Error: " + err.ToString(), "Mapping Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (this.current_mapping == null)
            {
                MessageBox.Show("Mapping Error: UNKNOWN", "Mapping Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // reset internal containers
            this.current_input_mapping.Clear();
            this.current_output_mapping.Clear();

            // update GUI
            this.cb_mappings.ItemsSource = new List<ParameterStructure.Mapping.Mapping2Component>(this.c_data.Mappings2Comps);
            this.cb_mappings.SelectedItem = null;
        }

        private void TestCalculateMapping()
        {
            this.c_data.EvaluateMapping(this.current_mapping);  
        }

        private bool CanExecute_TestCalculateMapping()
        {
            if (this.IsMapping || this.IsPickingCompData || this.IsPickingCompCalculator)
                return false;
            if (this.c_data == null || this.c_calculator == null || this.current_mapping == null)
                return false;

            return true;
        }

        private void RemoveSelectedMapping()
        {
            MessageBoxResult result = MessageBox.Show("Wollen Sie das Mapping wirklich entfernen?", "Mapping Entfernen", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;
            
            this.CompData.RemoveMapping(this.current_mapping);

            this.CompCalculator = null;
            this.current_input_mapping = new Dictionary<ParameterStructure.Parameter.Parameter, ParameterStructure.Parameter.Parameter>();
            this.current_output_mapping = new Dictionary<ParameterStructure.Parameter.Parameter, ParameterStructure.Parameter.Parameter>();
            this.tb_mapping_name.Text = string.Empty;
            this.current_mapping = null;

            this.UpdateDisplayOnCompDataLoad();
            this.UpdateDisplayOnCompCalculatorLoad();
            this.UpdateMappingTrackingDisplay();
        }

        private bool CanExecute_RemoveSelectedMapping()
        {
            return (this.CompData != null && this.current_mapping != null);
        }

        #endregion

        #region METHODS: Parameter Highlighting

        private void TogglePHighlight()
        {
            ParameterStructure.Parameter.Parameter selected_p = this.tv_comp_calc.SelectedItem as ParameterStructure.Parameter.Parameter;
            if (selected_p == null) return;

            TreeViewItemForMapping container_selected = TreeViewQuery.ContainerFromItem(this.tv_comp_calc, selected_p) as TreeViewItemForMapping;
            if (container_selected != null && container_selected.IsVisible)
            {
                container_selected.IsMarkingInitiator = !container_selected.IsMarkingInitiator;

                if (container_selected.IsMarkingInitiator)
                {
                    // show all visible containers holding input parameters
                    List<ParameterStructure.Parameter.Parameter> input_involved = this.CompCalculator.GetInputParamsInvolvedInTheCalculationOf(selected_p);
                    foreach (ParameterStructure.Parameter.Parameter p in input_involved)
                    {
                        TreeViewItemForMapping container_p = TreeViewQuery.ContainerFromItem(this.tv_comp_calc, p) as TreeViewItemForMapping;
                        if (container_p != null && container_p.IsVisible)
                        {
                            container_p.IsMarked = true;
                        }
                    }
                }
                else
                {
                    // remove all markings
                    if (this.tv_comp_calc != null)
                        this.tv_comp_calc.PropagateMarkingToItems(false);
                }
                
            } 
        }

        private bool CanExecute_TogglePHighlight()
        {
            if (this.IsMapping || this.IsPickingCompData || this.IsPickingCompCalculator)
                return false;
            if (this.c_data == null || this.c_calculator == null)
                return false;

            if (this.tv_comp_calc.SelectedItem == null) return false;
            ParameterStructure.Parameter.Parameter selected_p = this.tv_comp_calc.SelectedItem as ParameterStructure.Parameter.Parameter;
            if (selected_p == null) return false;
            if (selected_p.Propagation != InfoFlow.MIXED && selected_p.Propagation != InfoFlow.OUPUT) return false;

            return true;
        }

        #endregion

        #region METHODS: Mapping visualisation

        private Point tracking_source;
        private Point tracking_target;
        private Point tracking_mouse_current;

        private void UpdateMappingTrackingDisplay()
        {
            this.c_pointer_WS.Children.Clear();

            // draw the existing mappings
            List<Line> mark_existing = new List<Line>();
            int counter = 0;
            if (this.current_input_mapping != null)
            {
                foreach (var entry in this.current_input_mapping)
                {
                    mark_existing.AddRange(this.GetMappingVis(entry.Key, entry.Value, true, counter * CONN_OFFSET));
                    counter++;
                }
            }
            if (this.current_output_mapping != null)
            {
                foreach (var entry in this.current_output_mapping)
                {
                    mark_existing.AddRange(this.GetMappingVis(entry.Key, entry.Value, false, counter * CONN_OFFSET));
                    counter++;
                }
            }
            foreach (Line L in mark_existing)
            {
                this.c_pointer_WS.Children.Add(L);
            }

            // draw the interactive marking
            if (this.IsMapping)
            {
                List<Line> mark_interactive = Utils.DrawConnection(HALF_H_TV_ROW_COMP, HALF_H_TV_ROW_COMP, this.c_pointer_midside_measure.ActualWidth, this.c_pointer_middle_measure.ActualWidth,
                                                                    this.tracking_source, this.tracking_mouse_current, this.tracking_target, false, COL_DEFAULT, 2.0);
                foreach (Line L in mark_interactive)
                {
                    this.c_pointer_WS.Children.Add(L);
                }
            }  
        }

        private void c_pointer_WS_MouseMove(object sender, MouseEventArgs e)
        {
            if (e == null) return;
            if (!IsMapping) return;

            this.tracking_mouse_current = e.GetPosition(this.c_pointer_WS);
            this.UpdateMappingTrackingDisplay();
        }

        private void tv_comp_calc_MouseMove(object sender, MouseEventArgs e)
        {
            if (e == null) return;

            if (this.IsMapping && this.SelectedMappingSource)
            {
                this.tracking_mouse_current = e.GetPosition(this.tv_comp_calc);
                this.tracking_target = e.GetPosition(this.tv_comp_calc);
                this.UpdateMappingTrackingDisplay();
            }
        }

        /// <summary>
        /// Event handler for the treeview items
        /// </summary>
        private void UpdateMappingDisplay()
        {
            if (this.IsMapping) return;
            this.UpdateMappingTrackingDisplay();
        }

        #endregion

        #region METHODS: Generate the visualisation

        private List<Line> GetMappingVis(ParameterStructure.Parameter.Parameter _source, ParameterStructure.Parameter.Parameter _target, bool _input, double _connection_offset = 0.0)
        {
            List<Line> lines = new List<Line>();
            if (_source == null || _target == null) return lines;

            TreeViewItem container_source = TreeViewQuery.ContainerFromItem(this.tv_comp_data, _source);
            TreeViewItem container_target = TreeViewQuery.ContainerFromItem(this.tv_comp_calc, _target);

            if (container_source != null && container_source.IsVisible &&
                container_target != null && container_target.IsVisible)
            {
                // draw the lines
                Point source = container_source.TransformToAncestor(this.tv_comp_data).Transform(new Point(0, 0));
                Point target = container_target.TransformToAncestor(this.tv_comp_calc).Transform(new Point(0, 0));

                Color col = (_input) ? COL_MAPPING_INPUT : COL_MAPPING_OUTPUT;
                double thickness = (_input) ? 1.0 : 2.0;

                lines = Utils.DrawConnection(HALF_H_TV_ROW_COMP, HALF_H_TV_ROW_COMP, this.c_pointer_midside_measure.ActualWidth, this.c_pointer_middle_measure.ActualWidth,
                                             source, target, target, (container_target == null || !container_target.IsVisible), col, thickness, _connection_offset);
            }

            return lines;
        }

        #endregion

        #region EVENT HANDLERS

        private void Comp2CompMappingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitControls();
        }

        private void Comp2CompMappingWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateMappingTrackingDisplay();
        }


        void tv_comp_data_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // adapt display
            DependencyObject selObj = TreeViewQuery.GetSelectedTreeViewItem(this.tv_comp_data);
            TreeViewItem tvi = selObj as TreeViewItem;
            if (tvi != null)
            {
                this.tracking_source = tvi.TransformToAncestor(this.tv_comp_data).Transform(new Point(0, 0));
                this.UpdateMappingTrackingDisplay();
            }

            // handle selection
            if (sender == null || e == null) return;
            TreeView tv = sender as TreeView;
            if (tv == null) return;

            object selection = tv.SelectedItem;
            if (selection == null)
            {
                this.UpdateButtons();
                return; 
            }

            ParameterStructure.Parameter.Parameter selected_p = selection as ParameterStructure.Parameter.Parameter;
            if (selected_p != null)
            {
                if (this.IsMapping)
                    this.MappedSourceParameter= selected_p;
                this.UpdateButtons(selected_p);
            }
            else
            {
                this.IsMapping = false;
                this.SelectedMappingSource = false;
                this.UpdateMappingTrackingDisplay();
                this.UpdateButtons();
            }
        }

        void tv_comp_calc_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // adapt display
            DependencyObject selObj = TreeViewQuery.GetSelectedTreeViewItem(this.tv_comp_calc);
            TreeViewItem tvi = selObj as TreeViewItem;
            if (tvi != null)
            {
                this.tracking_target = tvi.TransformToAncestor(this.tv_comp_calc).Transform(new Point(0, 0));
                if (this.IsMapping)
                    this.UpdateMappingTrackingDisplay();
            }

            // react to selection
            if (sender == null || e == null) return;
            TreeView tv = sender as TreeView;
            if (tv == null) return;

            object selection = tv.SelectedItem;
            if (selection == null) return;

            ParameterStructure.Parameter.Parameter selected_p = selection as ParameterStructure.Parameter.Parameter;
            if (selected_p == null) return;

            if (this.IsMapping && this.SelectedMappingSource)
            {
                this.CreateP2PMapping(selected_p);
                this.IsMapping = false;
                this.SelectedMappingSource = false;
                this.UpdateMappingTrackingDisplay();
                this.UpdateButtons();
            }
        }

        private void cb_mappings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cb_mappings.SelectedItem != null)
            {
                ParameterStructure.Mapping.Mapping2Component mapping = this.cb_mappings.SelectedItem as ParameterStructure.Mapping.Mapping2Component;
                if (mapping != null)
                {
                    this.CompCalculator = mapping.Calculator;
                    this.c_data.TranslateMapping(mapping, out this.current_input_mapping, out this.current_output_mapping);
                    this.tb_mapping_name.Text = mapping.Name;
                    this.current_mapping = mapping;
                    this.IsInMapEditingMode = true;
                }                
            }
            else
            {
                this.CompCalculator = null;
                this.current_input_mapping = new Dictionary<ParameterStructure.Parameter.Parameter, ParameterStructure.Parameter.Parameter>();
                this.current_output_mapping = new Dictionary<ParameterStructure.Parameter.Parameter, ParameterStructure.Parameter.Parameter>();
                this.tb_mapping_name.Text = string.Empty;                
                this.current_mapping = null;
                this.IsInMapEditingMode = false;
            }
            this.UpdateDisplayOnCompCalculatorLoad();
            this.UpdateMappingTrackingDisplay();
        }

        #endregion
    }
}
