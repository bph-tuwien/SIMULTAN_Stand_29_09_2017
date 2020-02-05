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

using ParameterStructure.Parameter;
using ParameterStructure.Geometry;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for CompGeomSizeWindow.xaml
    /// </summary>
    public partial class CompGeomSizeWindow : Window
    {
        
        public CompGeomSizeWindow()
        {
            InitializeComponent();
            this.Loaded += CompGeomSizeWindow_Loaded;
        }

        private void InitContent()
        {
            this.MinH = 0.25;
            this.MinB = 0.50;
            this.MinL = 1.00;

            this.MaxH = 0.35;
            this.MaxB = 0.60;
            this.MaxL = 1.00;

            this.ShowSize();

            this.btn_OK.Click += btn_OK_Click;           
        }

        


        #region SIZE INITIALIZATION

        public void SetContent(GraphUIE_FlNet.FlNwElementVisualization _caller)
        {
            if (_caller == null) return;

            this.Caller = _caller;

            // retrieve information about the component instance
            List<double> sizes = _caller.RetrieveSize();
            CompositeCollection p_to_choose_from = _caller.RetrieveParamToSelectForSizeTransfer();
            List<ParameterStructure.Geometry.GeomSizeTransferDef> size_transfer_settings = _caller.RetrieveSizeTransferSettings();

            // fill the GUI containers
            this.SetSize(sizes);
            this.SetParamChoices(p_to_choose_from);
            this.SetSizeTransferSettings(size_transfer_settings);
        }

        private void SetSize(List<double> _input)
        {
            if (_input == null) return;
            if (_input.Count < 6) return;

            this.MinH = _input[0];
            this.MinB = _input[1];
            this.MinL = _input[2];

            this.MaxH = _input[3];
            this.MaxB = _input[4];
            this.MaxL = _input[5];

            this.ShowSize();
        }

        private void ShowSize()
        {
            this.min_h.Text = Parameter.ValueToString(this.MinH, "F2");
            this.min_b.Text = Parameter.ValueToString(this.MinB, "F2");
            this.min_L.Text = Parameter.ValueToString(this.MinL, "F2");

            this.max_h.Text = Parameter.ValueToString(this.MaxH, "F2");
            this.max_b.Text = Parameter.ValueToString(this.MaxB, "F2");
            this.max_L.Text = Parameter.ValueToString(this.MaxL, "F2");
        }

        private void SetParamChoices(CompositeCollection _p_collection)
        {
            this.parameters_for_size_transfer = _p_collection;

            this.lb_param_names_min_h.ItemsSource = _p_collection;
            this.lb_param_names_min_b.ItemsSource = _p_collection;
            this.lb_param_names_min_L.ItemsSource = _p_collection;

            this.lb_param_names_max_h.ItemsSource = _p_collection;
            this.lb_param_names_max_b.ItemsSource = _p_collection;
            this.lb_param_names_max_L.ItemsSource = _p_collection;
        }

        private void SetSizeTransferSettings(List<GeomSizeTransferDef> _settings)
        {
            if (_settings == null || _settings.Count < 6)
            {
                // initialize
                this.Settings = new List<GeomSizeTransferDef>();
                for(int i = 0; i < 6; i++)
                {
                    this.Settings.Add(new GeomSizeTransferDef
                    {
                        Source = GeomSizeTransferSource.USER,
                        ParameterName = string.Empty,
                        Correction = 0.0,
                        InitialValue = 0.0
                    });
                }
                this.MinHSettings = this.Settings[0];
                this.MinBSettings = this.Settings[1];
                this.MinLSettings = this.Settings[2];

                this.MaxHSettings = this.Settings[3];
                this.MaxBSettings = this.Settings[4];
                this.MaxLSettings = this.Settings[5];
                return;
            }

            // MinH
            this.MinHSettings = _settings[0];
            this.chb_min_h_from_p.IsChecked = _settings[0].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PARAMETER;
            this.min_h_correction.Text = Parameter.ValueToString(_settings[0].Correction, "F2");
            Parameter p0_selected = this.FindParameterToSelect(_settings[0].ParameterName);
            if (p0_selected != null)
                this.lb_param_names_min_h.SelectedItem = p0_selected;

            this.min_h.LostFocus += min_h_LostFocus;
            this.chb_min_h_from_p.Checked += chb_min_h_from_p_Checked;
            this.chb_min_h_from_p.Unchecked += chb_min_h_from_p_Checked;
            this.lb_param_names_min_h.SelectionChanged += chb_min_h_from_p_Checked;
            this.min_h_correction.LostFocus += chb_min_h_from_p_Checked;
            
            // MinB
            this.MinBSettings = _settings[1];
            this.chb_min_b_from_p.IsChecked = _settings[1].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PARAMETER;
            this.min_b_correction.Text = Parameter.ValueToString(_settings[1].Correction, "F2");
            Parameter p1_selected = this.FindParameterToSelect(_settings[1].ParameterName);
            if (p1_selected != null)
                this.lb_param_names_min_b.SelectedItem = p1_selected;

            this.min_b.LostFocus += min_b_LostFocus;
            this.chb_min_b_from_p.Checked += chb_min_b_from_p_Checked;
            this.chb_min_b_from_p.Unchecked += chb_min_b_from_p_Checked;
            this.lb_param_names_min_b.SelectionChanged += chb_min_b_from_p_Checked;
            this.min_b_correction.LostFocus += chb_min_b_from_p_Checked;
            
            // MinL
            this.MinLSettings = _settings[2];
            this.chb_min_L_from_p.IsChecked = _settings[2].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PARAMETER;
            this.chb_min_L_from_path.IsChecked = _settings[2].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PATH;
            
            if (_settings[2].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PARAMETER)
                this.min_L_correction.Text = Parameter.ValueToString(_settings[2].Correction, "F2");
            else if (_settings[2].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PATH)
                this.min_L_path_correction.Text = Parameter.ValueToString(_settings[2].Correction, "F2");
            
            Parameter p2_selected = this.FindParameterToSelect(_settings[2].ParameterName);
            if (p2_selected != null)
                this.lb_param_names_min_L.SelectedItem = p2_selected;

            this.min_L.LostFocus += min_L_LostFocus;
            this.chb_min_L_from_p.Checked += chb_min_L_from_p_Checked;
            this.chb_min_L_from_p.Unchecked += chb_min_L_from_p_Checked;
            this.chb_min_L_from_path.Checked += chb_min_L_from_p_Checked;
            this.chb_min_L_from_path.Unchecked += chb_min_L_from_p_Checked;
            this.lb_param_names_min_L.SelectionChanged += chb_min_L_from_p_Checked;
            this.min_L_correction.LostFocus += chb_min_L_from_p_Checked;
            this.min_L_path_correction.LostFocus += chb_min_L_from_p_Checked;

            // MaxH
            this.MaxHSettings = _settings[3];
            this.chb_max_h_from_p.IsChecked = _settings[3].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PARAMETER;
            this.max_h_correction.Text = Parameter.ValueToString(_settings[3].Correction, "F2");
            Parameter p3_selected = this.FindParameterToSelect(_settings[3].ParameterName);
            if (p3_selected != null)
                this.lb_param_names_max_h.SelectedItem = p3_selected;

            this.max_h.LostFocus += max_h_LostFocus;
            this.chb_max_h_from_p.Checked += chb_max_h_from_p_Checked;
            this.chb_max_h_from_p.Unchecked += chb_max_h_from_p_Checked;
            this.lb_param_names_max_h.SelectionChanged += chb_max_h_from_p_Checked;
            this.max_h_correction.LostFocus += chb_max_h_from_p_Checked;
            
            //MaxB
            this.MaxBSettings = _settings[4];
            this.chb_max_b_from_p.IsChecked = _settings[4].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PARAMETER;
            this.max_b_correction.Text = Parameter.ValueToString(_settings[4].Correction, "F2");
            Parameter p4_selected = this.FindParameterToSelect(_settings[4].ParameterName);
            if (p4_selected != null)
                this.lb_param_names_max_b.SelectedItem = p4_selected;

            this.max_b.LostFocus += max_b_LostFocus;
            this.chb_max_b_from_p.Checked += chb_max_b_from_p_Checked;
            this.chb_max_b_from_p.Unchecked += chb_max_b_from_p_Checked;
            this.lb_param_names_max_b.SelectionChanged += chb_max_b_from_p_Checked;
            this.max_b_correction.LostFocus += chb_max_b_from_p_Checked;
            
            // MaxL
            this.MaxLSettings = _settings[5];
            this.chb_max_L_from_p.IsChecked = _settings[5].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PARAMETER;
            this.chb_max_L_from_path.IsChecked = _settings[5].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PATH;
            
            if (_settings[5].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PARAMETER)
                this.max_L_correction.Text = Parameter.ValueToString(_settings[5].Correction, "F2");
            else if (_settings[5].Source == ParameterStructure.Geometry.GeomSizeTransferSource.PATH)
                this.max_L_path_correction.Text = Parameter.ValueToString(_settings[5].Correction, "F2");

            Parameter p5_selected = this.FindParameterToSelect(_settings[2].ParameterName);
            if (p5_selected != null)
                this.lb_param_names_max_L.SelectedItem = p5_selected;

            this.max_L.LostFocus += max_L_LostFocus;
            this.chb_max_L_from_p.Checked += chb_max_L_from_p_Checked;
            this.chb_max_L_from_p.Unchecked += chb_max_L_from_p_Checked;
            this.chb_max_L_from_path.Checked += chb_max_L_from_p_Checked;
            this.chb_max_L_from_path.Unchecked += chb_max_L_from_p_Checked;
            this.lb_param_names_max_L.SelectionChanged += chb_max_L_from_p_Checked;
            this.max_L_correction.LostFocus += chb_max_L_from_p_Checked;
            this.max_L_path_correction.LostFocus += chb_max_L_from_p_Checked;

            // path dependent info
            if (this.Caller.ContainsValidPath())
            {
                this.chb_min_L_from_path.Background = new SolidColorBrush(Colors.White);
                this.chb_max_L_from_path.Background = new SolidColorBrush(Colors.White);
                this.chb_min_L_from_path.ToolTip = null;
                this.chb_max_L_from_path.ToolTip = null;
            }               
            else if (this.Caller.ContainsPath())
            {
                this.chb_min_L_from_path.Background = new SolidColorBrush(Colors.Yellow);
                this.chb_max_L_from_path.Background = new SolidColorBrush(Colors.Yellow);
                this.chb_min_L_from_path.ToolTip = "nicht verortet";
                this.chb_max_L_from_path.ToolTip = "nicht verortet";
            }
            else
            {
                this.chb_min_L_from_path.Background = new SolidColorBrush(Colors.OrangeRed);
                this.chb_max_L_from_path.Background = new SolidColorBrush(Colors.OrangeRed);
                this.chb_min_L_from_path.ToolTip = "existiert nicht";
                this.chb_max_L_from_path.ToolTip = "existiert nicht";
            }
                
        }

        #endregion

        #region PROPERTIES

        public GraphUIE_FlNet.FlNwElementVisualization Caller { get; private set; }

        public double MinH { get; private set; }
        public double MinB { get; private set; }
        public double MinL { get; private set; }
        public double MaxH { get; private set; }
        public double MaxB { get; private set; }
        public double MaxL { get; private set; }
        public List<double> Sizes { get; private set; }

        public GeomSizeTransferDef MinHSettings { get; private set; }
        public GeomSizeTransferDef MinBSettings { get; private set; }
        public GeomSizeTransferDef MinLSettings { get; private set; }
        public GeomSizeTransferDef MaxHSettings { get; private set; }
        public GeomSizeTransferDef MaxBSettings { get; private set; }
        public GeomSizeTransferDef MaxLSettings { get; private set; }
        public List<GeomSizeTransferDef> Settings { get; private set; }

        // only for internal use
        private CompositeCollection parameters_for_size_transfer;

        #endregion


        #region EVENT HANDLER: Content

        #region Min_H

        private void min_h_LostFocus(object sender, RoutedEventArgs e)
        {
            this.MinH = Parameter.StringToDouble(this.min_h.Text);
        }

        private void chb_min_h_from_p_Checked(object sender, RoutedEventArgs e)
        {
            if (this.chb_min_h_from_p.IsChecked.HasValue && this.chb_min_h_from_p.IsChecked.Value)
            {
                this.MinHSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.PARAMETER,
                    ParameterName = (this.lb_param_names_min_h.SelectedItem is Parameter) ? (this.lb_param_names_min_h.SelectedItem as Parameter).Name : string.Empty,
                    Correction = Parameter.StringToDouble(this.min_h_correction.Text),
                    InitialValue = Parameter.StringToDouble(this.min_h.Text)
                };
                // perform calculation and transfer to GUI
                this.MinH = Parameter.StringToDouble(this.min_h.Text);
                double new_value = this.Caller.RetrieveSingleSizeValue(0, this.MinH, this.MinHSettings);
                this.MinH = new_value;
                this.min_h.Text = Parameter.ValueToString(this.MinH, "F2");
            }
            else
            {
                this.MinHSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.USER,
                    ParameterName = string.Empty,
                    Correction = 0.0,
                    InitialValue = Parameter.StringToDouble(this.min_h.Text)
                };
                this.min_h.Text = Parameter.ValueToString(this.MinH, "F2");
            }
        }

        #endregion

        #region Min_B

        private void min_b_LostFocus(object sender, RoutedEventArgs e)
        {
            this.MinB = Parameter.StringToDouble(this.min_b.Text);
        }

        private void chb_min_b_from_p_Checked(object sender, RoutedEventArgs e)
        {
            if (this.chb_min_b_from_p.IsChecked.HasValue && this.chb_min_b_from_p.IsChecked.Value)
            {
                this.MinBSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.PARAMETER,
                    ParameterName = (this.lb_param_names_min_b.SelectedItem is Parameter) ? (this.lb_param_names_min_b.SelectedItem as Parameter).Name : string.Empty,
                    Correction = Parameter.StringToDouble(this.min_b_correction.Text),
                    InitialValue = Parameter.StringToDouble(this.min_b.Text)
                };
                // perform calculation and transfer to GUI
                this.MinB = Parameter.StringToDouble(this.min_b.Text);
                double new_value = this.Caller.RetrieveSingleSizeValue(1, this.MinB, this.MinBSettings);
                this.MinB = new_value;
                this.min_b.Text = Parameter.ValueToString(this.MinB, "F2");
            }
            else
            {
                this.MinBSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.USER,
                    ParameterName = string.Empty,
                    Correction = 0.0,
                    InitialValue = Parameter.StringToDouble(this.min_b.Text)
                };
                this.min_b.Text = Parameter.ValueToString(this.MinB, "F2");
            }
        }

        #endregion

        #region Min_L

        private void min_L_LostFocus(object sender, RoutedEventArgs e)
        {
            this.MinL = Parameter.StringToDouble(this.min_L.Text);
        }

        private void chb_min_L_from_p_Checked(object sender, RoutedEventArgs e)
        {
            if (this.chb_min_L_from_p.IsChecked.HasValue && this.chb_min_L_from_p.IsChecked.Value)
            {
                this.MinLSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.PARAMETER,
                    ParameterName = (this.lb_param_names_min_L.SelectedItem is Parameter) ? (this.lb_param_names_min_L.SelectedItem as Parameter).Name : string.Empty,
                    Correction = Parameter.StringToDouble(this.min_L_correction.Text),
                    InitialValue = Parameter.StringToDouble(this.min_L.Text)
                };
                // perform calculation and transfer to GUI
                this.MinL = Parameter.StringToDouble(this.min_L.Text);
                double new_value = this.Caller.RetrieveSingleSizeValue(2, this.MinL, this.MinLSettings);
                this.MinL = new_value;
                this.min_L.Text = Parameter.ValueToString(this.MinL, "F2");
            }
            else if (this.chb_min_L_from_path.IsChecked.HasValue && this.chb_min_L_from_path.IsChecked.Value)
            {
                this.MinLSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.PATH,
                    ParameterName = string.Empty,
                    Correction = Parameter.StringToDouble(this.min_L_path_correction.Text),
                    InitialValue = Parameter.StringToDouble(this.min_L.Text)
                };
                // perform calculation and transfer to GUI
                this.MinL = Parameter.StringToDouble(this.min_L.Text);
                double new_value = this.Caller.RetrieveSingleSizeValue(2, this.MinL, this.MinLSettings);
                this.MinL = new_value;
                this.min_L.Text = Parameter.ValueToString(this.MinL, "F2");
            }
            else
            {
                this.MinLSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.USER,
                    ParameterName = string.Empty,
                    Correction = 0.0,
                    InitialValue = Parameter.StringToDouble(this.min_L.Text)
                };
                this.min_L.Text = Parameter.ValueToString(this.MinL, "F2");
            }
        }


        #endregion

        #region MAX_H

        private void max_h_LostFocus(object sender, RoutedEventArgs e)
        {
            this.MaxH = Parameter.StringToDouble(this.max_h.Text);
        }

        private void chb_max_h_from_p_Checked(object sender, RoutedEventArgs e)
        {
            if (this.chb_max_h_from_p.IsChecked.HasValue && this.chb_max_h_from_p.IsChecked.Value)
            {
                this.MaxHSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.PARAMETER,
                    ParameterName = (this.lb_param_names_max_h.SelectedItem is Parameter) ? (this.lb_param_names_max_h.SelectedItem as Parameter).Name : string.Empty,
                    Correction = Parameter.StringToDouble(this.max_h_correction.Text),
                    InitialValue = Parameter.StringToDouble(this.max_h.Text)
                };
                // perform calculation and transfer to GUI
                this.MaxH = Parameter.StringToDouble(this.max_h.Text);
                double new_value = this.Caller.RetrieveSingleSizeValue(3, this.MaxH, this.MaxHSettings);
                this.MaxH = new_value;
                this.max_h.Text = Parameter.ValueToString(this.MaxH, "F2");
            }
            else
            {
                this.MaxHSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.USER,
                    ParameterName = string.Empty,
                    Correction = 0.0,
                    InitialValue = Parameter.StringToDouble(this.max_h.Text)
                };
                this.max_h.Text = Parameter.ValueToString(this.MaxH, "F2");
            }
        }

        #endregion

        #region MAX_B

        private void max_b_LostFocus(object sender, RoutedEventArgs e)
        {
            this.MaxB = Parameter.StringToDouble(this.max_b.Text);
        }

        private void chb_max_b_from_p_Checked(object sender, RoutedEventArgs e)
        {
            if (this.chb_max_b_from_p.IsChecked.HasValue && this.chb_max_b_from_p.IsChecked.Value)
            {
                this.MaxBSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.PARAMETER,
                    ParameterName = (this.lb_param_names_max_b.SelectedItem is Parameter) ? (this.lb_param_names_max_b.SelectedItem as Parameter).Name : string.Empty,
                    Correction = Parameter.StringToDouble(this.max_b_correction.Text),
                    InitialValue = Parameter.StringToDouble(this.max_b.Text)
                };
                // perform calculation and transfer to GUI
                this.MaxB = Parameter.StringToDouble(this.max_b.Text);
                double new_value = this.Caller.RetrieveSingleSizeValue(4, this.MaxB, this.MaxBSettings);
                this.MaxB = new_value;
                this.max_b.Text = Parameter.ValueToString(this.MaxB, "F2");
            }
            else
            {
                this.MaxBSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.USER,
                    ParameterName = string.Empty,
                    Correction = 0.0,
                    InitialValue = Parameter.StringToDouble(this.max_b.Text)
                };
                this.max_b.Text = Parameter.ValueToString(this.MaxB, "F2");
            }
        }

        #endregion

        #region MAX_L

        private void max_L_LostFocus(object sender, RoutedEventArgs e)
        {
            this.MaxL = Parameter.StringToDouble(this.max_L.Text);
        }

        private void chb_max_L_from_p_Checked(object sender, RoutedEventArgs e)
        {
            if (this.chb_max_L_from_p.IsChecked.HasValue && this.chb_max_L_from_p.IsChecked.Value)
            {
                this.MaxLSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.PARAMETER,
                    ParameterName = (this.lb_param_names_max_L.SelectedItem is Parameter) ? (this.lb_param_names_max_L.SelectedItem as Parameter).Name : string.Empty,
                    Correction = Parameter.StringToDouble(this.max_L_correction.Text),
                    InitialValue = Parameter.StringToDouble(this.max_L.Text)
                };
                // perform calculation and transfer to GUI
                this.MaxL = Parameter.StringToDouble(this.max_L.Text);
                double new_value = this.Caller.RetrieveSingleSizeValue(5, this.MaxL, this.MaxLSettings);
                this.MaxL = new_value;
                this.max_L.Text = Parameter.ValueToString(this.MaxL, "F2");
            }
            else if (this.chb_max_L_from_path.IsChecked.HasValue && this.chb_max_L_from_path.IsChecked.Value)
            {
                this.MaxLSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.PATH,
                    ParameterName = string.Empty,
                    Correction = Parameter.StringToDouble(this.max_L_path_correction.Text),
                    InitialValue = Parameter.StringToDouble(this.max_L.Text)
                };
                // perform calculation and transfer to GUI
                this.MaxL = Parameter.StringToDouble(this.max_L.Text);
                double new_value = this.Caller.RetrieveSingleSizeValue(5, this.MaxL, this.MaxLSettings);
                this.MaxL = new_value;
                this.max_L.Text = Parameter.ValueToString(this.MaxL, "F2");
            }
            else
            {
                this.MaxLSettings = new GeomSizeTransferDef()
                {
                    Source = GeomSizeTransferSource.USER,
                    ParameterName = string.Empty,
                    Correction = 0.0,
                    InitialValue = Parameter.StringToDouble(this.max_L.Text)
                };
                this.max_L.Text = Parameter.ValueToString(this.MaxL, "F2");
            }
        }

        #endregion

        #endregion

        #region EVENT HANDLER: Main

        private void CompGeomSizeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitContent();
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            // transfer size
            this.MinH = Parameter.StringToDouble(this.min_h.Text);
            this.MinB = Parameter.StringToDouble(this.min_b.Text);
            this.MinL = Parameter.StringToDouble(this.min_L.Text);

            this.MaxH = Parameter.StringToDouble(this.max_h.Text);
            this.MaxB = Parameter.StringToDouble(this.max_b.Text);
            this.MaxL = Parameter.StringToDouble(this.max_L.Text);

            this.Sizes = new List<double> { this.MinH, this.MinB, this.MinL, this.MaxH, this.MaxB, this.MaxL };

            // transfer settings

            this.Settings = new List<GeomSizeTransferDef> { this.MinHSettings, this.MinBSettings, this.MinLSettings, 
                                                            this.MaxHSettings, this.MaxBSettings, this.MaxLSettings };

            // done
            this.DialogResult = true;
            this.Close();
        }


        #endregion

        #region UTILS

        private ParameterStructure.Parameter.Parameter FindParameterToSelect(string _pname)
        {
            if (string.IsNullOrEmpty(_pname)) return null;
            if (this.parameters_for_size_transfer == null) return null;

            foreach (object container in this.parameters_for_size_transfer)
            {
                CollectionContainer col_container = container as CollectionContainer;
                if (col_container != null && col_container.Collection != null)
                {
                    foreach (object item in col_container.Collection)
                    {
                        ParameterStructure.Parameter.Parameter p = item as ParameterStructure.Parameter.Parameter;
                        if (p == null) continue;

                        if (p.Name == _pname)
                            return p;
                    }
                }
            }

            return null;
        }

        #endregion
    }
}
