using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.IO;

using ComponentBuilder.WebServiceConnections;
using ComponentBuilder.WinUtils;
using ComponentBuilder.WpfUtils;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for WebServiceMapWindow.xaml
    /// </summary>
    public partial class WebServiceMapWindow : Window, INotifyPropertyChanged
    {
        #region STATIC

        private const int HALF_H_TV_ROW_TN = 7;
        private const int HALF_H_TV_ROW_COMP = 9;
        private const double CONN_OFFSET = 2;

        private static readonly Color COL_MAPPING_PARAM = (Color)ColorConverter.ConvertFromString("#660000ff");
        private static readonly Color COL_MAPPING_COMP = (Color)ColorConverter.ConvertFromString("#ff0000ff");
        private static readonly Color COL_MAPPING_P3D = (Color)ColorConverter.ConvertFromString("#66880088");

        private static readonly Color COL_DEFAULT = (Color)ColorConverter.ConvertFromString("#ff000000");

        #endregion

        public WebServiceMapWindow()
        {
            InitializeComponent();
            this.all_mappings = new List<MappingObject>();
            this.Loaded += WebServiceMapWindow_Loaded;
            this.SizeChanged += WebServiceMapWindow_SizeChanged;
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

        #region PROPERTIES: Web Service

        
        private List<TypeNode> ws_roots;
        public List<TypeNode> WSRoots
        {
            get { return this.ws_roots; }
            set 
            { 
                this.ws_roots = value;
                if (this.ws_roots != null)
                    this.chb_ws_entry_points.ItemsSource = new List<TypeNode>(this.ws_roots);
                else
                    this.chb_ws_entry_points.ItemsSource = new List<TypeNode>();
            }
        }


        // derived
        private TypeNode root_node;
        public TypeNode Root
        {
            get { return this.root_node; }
            private set
            {
                this.root_node = value;
                this.tb_caller_name.Text = (this.root_node == null) ? "-" : this.root_node.TypeName;
                this.tv_types.ItemsSource = new List<TypeNode> { this.root_node };
                if (this.root_node.ReturnTypeNode != null)
                    this.tv_return_type.ItemsSource = new List<TypeNode> { this.root_node.ReturnTypeNode };
                else
                    this.tv_return_type.ItemsSource = new List<TypeNode>();
            }
        }

        /// <summary>
        /// For the return values of the web-services.
        /// </summary>
        public object Results { get; private set; }

        /// <summary>
        /// For the 'out' parameters of the called method.
        /// </summary>
        public object[] OutPuts { get; private set; }

        #endregion

        #region CLASS MEMBERS

        private List<MappingObject> all_mappings;

        private Dictionary<string, string> all_web_service_urls;

        #endregion

        #region PROPERTIES: Loading a Component (later more...)

        private List<ParameterStructure.Component.Component> all_comps;

        private ParameterStructure.Component.Component c_main;
        public ParameterStructure.Component.Component CompMain
        {
            get { return this.c_main; }
            set 
            { 
                if (!(value.IsLocked))
                {
                    this.c_main = value;
                    this.RegisterPropertyChanged("CompMain");
                    this.IsPickingCompMain = false;
                    this.UpdateComponentContent();
                }               
            }
        }

        private bool is_picking_c_main;
        public bool IsPickingCompMain
        {
            get { return this.is_picking_c_main; }
            private set 
            { 
                this.is_picking_c_main = value;
                this.RegisterPropertyChanged("IsPickingCompMain");
                this.UpdateButtons();
            }
        }

        #endregion

        #region PROPERTIES: Mapping Actors - Component, TypeNode

        // general settings
        public bool MapAsTemplate { get; private set; }
        public bool UseMapGlobally { get; private set; }
        
        // specific: Mapping
        public bool IsMapping { get; private set; }

        private ParameterStructure.Parameter.Parameter mapped_parameter;
        public ParameterStructure.Parameter.Parameter MappedParameter
        {
            get { return this.mapped_parameter; }
            private set
            {
                if (this.IsMapping)
                {
                    this.mapped_parameter = value;
                    this.SelectedMappingSource = true;
                }
                else
                    this.mapped_parameter = null;
            }
        }

        private ParameterStructure.Component.Component mapped_component;
        public ParameterStructure.Component.Component MappedComponent
        {
            get { return this.mapped_component; }
            private set
            {
                if (this.IsMapping)
                {
                    this.mapped_component = value;
                    this.SelectedMappingSource = true;
                }
                else
                    this.mapped_component = null;
            }
        }

        private ParameterStructure.Geometry.Point3DContainer mapped_p3d_cont;
        public ParameterStructure.Geometry.Point3DContainer MappedPoint3DContainer
        {
            get { return this.mapped_p3d_cont; }
            private set 
            {
                if (this.IsMapping)
                {
                    this.mapped_p3d_cont = value;
                    this.SelectedMappingSource = true;
                }
                else
                    this.mapped_p3d_cont = null;
            }
        }

        // derived: Mapping
        public bool SelectedMappingSource { get; private set; }

        public ParameterStructure.Component.Component DirectParentOfMappingSource { get; private set; }

        // specific: Mapping Back
        public bool IsMappingBack { get; private set; }

        private TypeNode mapped_back_type;
        public TypeNode MappedBackType
        {
            get { return this.mapped_back_type; }
            private set
            {
                if (this.IsMappingBack)
                {
                    this.mapped_back_type = value;
                    this.SelectedMappingBackSource = true;
                }
                else
                    this.mapped_back_type = null;
            }
        }

        // derived: Mapping Back
        public bool SelectedMappingBackSource { get; private set; }

        #endregion

        #region METHODS: Helper

        private void GetWebServiceUrls(out string err_msg)
        {
            err_msg = string.Empty;
            this.all_web_service_urls = new Dictionary<string, string>();

            // --------------- look for the config file ------------------------  //
            string path_to_url_file = @"_config_files\web_service_urls.txt";

            // get current application path
            string path = AppDomain.CurrentDomain.BaseDirectory;
            // construct path of config file
            string[] path_comps = path.Split(new string[] { "\\", "/" }, StringSplitOptions.RemoveEmptyEntries);
            int nr_comps = path_comps.Length;
            if (nr_comps <= 3) return;

            string path_CONFIG = string.Empty;
            for (int i = 0; i < nr_comps - 3; i++)
            {
                path_CONFIG += path_comps[i] + "\\";
            }
            path_CONFIG += path_to_url_file;

            try
            {
                if (File.Exists(path_CONFIG))
                {
                    err_msg = "No URLs found!";
                    using (StreamReader fstream = new StreamReader(path_CONFIG))
                    {
                        while (fstream.Peek() >= 0)
                        {
                            string line = fstream.ReadLine();

                            if (string.IsNullOrEmpty(line))
                                continue;
                            string[] line_comps = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                            if (line_comps == null || line_comps.Length < 2)
                                continue;

                            string key = line_comps[0].Trim();
                            string possible_url = line_comps[1].Trim();
                            if (!this.all_web_service_urls.ContainsKey(key))
                            {
                                this.all_web_service_urls.Add(key, possible_url);
                                err_msg = string.Empty;
                            }                           
                        }
                    }
                }
                else
                {
                    err_msg = "File not found!";
                }
            }
            catch(Exception ex)
            {
                err_msg = ex.Message;
            }

        }

        #endregion

        #region METHODS: Init, Update

        private void InitControls()
        {
            // url dictionary
            string ws_url_err_msg = string.Empty;
            this.GetWebServiceUrls(out ws_url_err_msg);
            string displ_msg = "Web-Service urls ";
            if (!string.IsNullOrEmpty(ws_url_err_msg))
            {
                displ_msg += "nicht gefunden!";
                this.txt_url_msg.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }                
            else
            {
                displ_msg += "für " + this.all_web_service_urls.Keys.Aggregate((x, y) => x + ", " + y) + " gefunden.";
                this.txt_url_msg.Foreground = new SolidColorBrush(Colors.DimGray);
            }
            this.txt_url_msg.Text = displ_msg;

            // controls
            this.IsPickingCompMain = false;
            this.IsMapping = false;

            this.btn_copmps_expand.Command = new RelayCommand((x) => this.tv_comps.ChangeExpandedState(true), (x) => this.tv_comps != null);
            this.btn_copmps_collapse.Command = new RelayCommand((x) => this.tv_comps.ChangeExpandedState(false), (x) => this.tv_comps != null);
            this.btn_types_expand.Command = new RelayCommand((x) => this.tv_types.ChangeExpandedState(true), (x) => this.tv_types != null);
            this.btn_types_collapse.Command = new RelayCommand((x) => this.tv_types.ChangeExpandedState(false), (x) => this.tv_types != null);            
            
            this.btn_type_populate.Command = new RelayCommand((x) => this.OnExpandSelectedType(), 
                                                   (x) => this.CanExecute_OnExpandSelectedType());
            this.btn_type_depopulate.Command = new RelayCommand((x) => this.OnCollapseSelectedType(),
                                                     (x) => this.CanExecute_OnCollapseSelectedType());


            this.tbtn_get_CM.Command = new RelayCommand((x) => this.IsPickingCompMain = !(this.IsPickingCompMain),
                                                        (x) => !this.IsMapping && !this.IsMappingBack);
            this.tbtn_get_CM.IsChecked = (this.IsPickingCompMain && !this.IsMapping && !this.IsMappingBack);
            this.tbtn_get_CM.IsEnabled = !this.IsMapping && !this.IsMappingBack;

            this.tbtn_map.Command = new RelayCommand((x) => this.IsMapping = !(this.IsMapping), (x) => !this.IsPickingCompMain && !this.IsMappingBack);
            this.tbtn_map.IsChecked = (!this.IsPickingCompMain && this.IsMapping && !this.IsMappingBack);
            this.tbtn_map.IsEnabled = !this.IsPickingCompMain && !this.IsMappingBack;

            this.btn_unmap.IsEnabled = (!this.IsMapping && !this.IsPickingCompMain && !this.IsMappingBack);

            this.tbtn_map_back.Command = new RelayCommand((x) => this.IsMappingBack = !(this.IsMappingBack), (x) => !this.IsPickingCompMain && !this.IsMapping);
            this.tbtn_map_back.IsChecked = (!this.IsPickingCompMain && !this.IsMapping && this.IsMappingBack);
            this.tbtn_map_back.IsEnabled = (!this.IsPickingCompMain && !this.IsMapping);

            this.btn_instance_create.Command = new RelayCommand((x) => this.AttemptToInstantiate(), 
                                                                (x) => this.CanExecute_AttemptToInstantiate());

            this.tv_comps.SelectedItemChanged += tv_comps_SelectedItemChanged;
            this.tv_types.SelectedItemChanged += tv_types_SelectedItemChanged;
            this.tv_return_type.SelectedItemChanged += tv_return_type_SelectedItemChanged;
            this.tv_types.MouseMove += tv_types_MouseMove;
            this.tv_comps.ActOnTreeViewItemExpandedChangedHandler = this.UpdateMappingDisplay;
            this.tv_types.ActOnTreeViewItemExpandedChangedHandler = this.UpdateMappingDisplay;
            
            this.btn_OK.Click += btn_OK_Click;
            this.btn_OK.IsEnabled = !this.IsPickingCompMain && !this.IsMapping;
            this.btn_OK.Focus();

            this.c_pointer_WS.MouseMove += c_pointer_WS_MouseMove;
            this.c_pointer_WS.PreviewMouseMove += c_pointer_WS_MouseMove;

            this.chb_ws_entry_points.SelectionChanged += chb_ws_entry_points_SelectionChanged;
            

            this.cb_as_example.Checked += cb_as_example_Checked;
            this.cb_as_example.Unchecked += cb_as_example_Checked;
            this.cb_as_example_global.Checked += cb_as_example_global_Checked;
            this.cb_as_example_global.Unchecked += cb_as_example_global_Checked;
            this.cb_as_example.IsChecked = false;

            // debug
            this.btn_DEBUG_struct_tree_from_comp.Command = new RelayCommand((x) => this.ExtractStructureFromComponent(),
                                                                      (x) => CanExecute_ExtractStructureFromComponent());            
            // debug

            this.btn_pin_mapping.Command = new RelayCommand((x) => this.SaveMappingsOfTypeNode(),
                                                      (x) => CanExecute_SaveMappingsOfTypeNode());
            this.btn_clean_all.Command = new RelayCommand((x) => this.CleanAllMappings());
            this.btn_clean_all.IsEnabled = !this.IsPickingCompMain && !this.IsMapping;

            this.btn_Restore.Command = new RelayCommand((x) => this.RestoreMapping(), (x) => CanExecute_RestoreMapping());
            this.btn_SEND.Command = new RelayCommand((x) => this.SendToWebService(), (x) => this.CanExecute_SendToWebService());

            this.cb_results.SelectionChanged += cb_results_SelectionChanged;
        }


        private void cb_as_example_Checked(object sender, RoutedEventArgs e)
        {
            if (this.cb_as_example.IsChecked.HasValue && this.cb_as_example.IsChecked.Value)
            {
                this.MapAsTemplate = true;
                this.cb_as_example_global.IsChecked = false;
                this.cb_as_example_global.IsEnabled = true;
            }
            else
            {
                this.MapAsTemplate = false;
                this.cb_as_example_global.IsChecked = false;
                this.cb_as_example_global.IsEnabled = false;
            }
        }

        private void cb_as_example_global_Checked(object sender, RoutedEventArgs e)
        {
            if (this.cb_as_example_global.IsChecked.HasValue && this.cb_as_example.IsChecked.Value)
            {
                this.UseMapGlobally = true;
            }
            else
            {
                this.UseMapGlobally = false;
            }
        }
         

        private void UpdateButtons()
        {
            this.tbtn_get_CM.IsChecked = this.IsPickingCompMain && !this.IsMapping && !this.IsMappingBack;
            this.tbtn_get_CM.IsEnabled = !this.IsMapping && !this.IsMappingBack;
            this.tbtn_map.IsChecked = !this.IsPickingCompMain && this.IsMapping && !this.IsMappingBack;
            this.tbtn_map.IsEnabled = !this.IsPickingCompMain && !this.IsMappingBack;
            this.tbtn_map_back.IsChecked = !this.IsPickingCompMain && !this.IsMapping && this.IsMappingBack;
            this.tbtn_map_back.IsEnabled = !this.IsPickingCompMain && !this.IsMapping;
        }

        private void UpdateComponentContent()
        {
            if (this.all_comps == null)
                this.all_comps = new List<ParameterStructure.Component.Component>();
            
            if (this.CompMain != null)
            {
                this.all_comps.Add(this.CompMain);
                this.tv_comps.ItemsSource = new List<ParameterStructure.Component.Component>(this.all_comps);
            }           
        }


        private void OnExpandSelectedType()
        {
            (this.tv_types.SelectedItem as TypeNode).ExpandType(true);
            this.tv_types.ItemsSource = new List<TypeNode> { this.root_node };
            this.tv_types.ChangeExpandedState(true);
        }

        private bool CanExecute_OnExpandSelectedType()
        {
            return (this.tv_types != null && this.tv_types.SelectedItem != null && this.tv_types.SelectedItem is TypeNode &&
                   (this.tv_types.SelectedItem as TypeNode).TypeCanBeExpandedBySubtypes);
        }

        private void OnCollapseSelectedType()
        {
            (this.tv_types.SelectedItem as TypeNode).CollapseType();
            this.tv_types.ItemsSource = new List<TypeNode> { this.root_node };
            this.tv_types.ChangeExpandedState(true);
        }

        private bool CanExecute_OnCollapseSelectedType()
        {
            return (this.tv_types != null && this.tv_types.SelectedItem != null && this.tv_types.SelectedItem is TypeNode &&
                   (this.tv_types.SelectedItem as TypeNode).TypeCanBeSimplified);
        }

        #endregion

        #region METHODS: Mapping

        private void CreateMapping(TypeNode _target)
        {
            if (this.MappedParameter == null && this.MappedComponent == null && this.MappedPoint3DContainer == null) return;
            if (_target == null) return;

            MappingError err = MappingError.NONE;
            if (this.MappedParameter != null)
            {                
                MappingParameter mp = MappingParameter.Create(this.MappedParameter, this.DirectParentOfMappingSource, _target, this.MapAsTemplate, false, out err);
                if (mp != null)
                    this.all_mappings.Add(mp);
            }
            else if (this.MappedComponent != null)
            {
                // attempt a structural match
                List<KeyValuePair<string, TypeNode>> matches = new List<KeyValuePair<string, TypeNode>>();
                MappingComponent.MatchStructure(this.MappedComponent, _target, ref matches, out err);
                if (matches != null && matches.Count > 0)
                {
                    string test = matches.Select(x => x.Key + " -> " + x.Value.TypeName).Aggregate((x, y) => x + "\n" + y);
                    MessageBoxResult result = MessageBox.Show("Möchten Sie folgende Zuordnungen erstellen: \n" + test + "?", "Strukturelle Zuordung", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        // create the mappings
                        List<MappingObject> new_mappings = MappingComponent.CreateMultipleFrom(this.MappedComponent, _target, this.MapAsTemplate, matches, out err);
                        if (new_mappings.Count > 0)
                        {
                            this.all_mappings.AddRange(new_mappings);
                        }
                    }
                }                
            }
            else if (this.MappedPoint3DContainer != null)
            {
                MappingSinglePoint msp = MappingSinglePoint.Create(this.MappedPoint3DContainer, this.DirectParentOfMappingSource, _target, this.MapAsTemplate, out err);
                if (msp != null)
                    this.all_mappings.Add(msp);                            
            }

            if (err != MappingError.NONE)
            {
                MessageBox.Show("Error creating mapping " + err.ToString(), "Mapping Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.MappedParameter = null;
            this.MappedComponent = null;
            this.MappedPoint3DContainer = null;
        }

        #endregion

        #region METHODS: Save, Clean and Re-load Mapping

        private void SaveMappingsOfTypeNode()
        {
            TypeNode tn = this.tv_types.SelectedItem as TypeNode;
            if (tn == null) return;

            List<ParameterStructure.Mapping.StructureNode> mapped_input_roots = null;
            ParameterStructure.Mapping.StructureNode type_root = null;
            tn.SaveMappingAsRecord(this.txt_mapping_key.Text, out mapped_input_roots, out type_root);

            // update the mapping entries
            if (this.cb_mappings != null)
                this.cb_mappings.ItemsSource = new List<ParameterStructure.Mapping.StructureMap>(this.root_node.GetMappingRecordsOfTree());               

            // display the saved mapping
            GenericTreeVisWindow gtv_win = new GenericTreeVisWindow();
            gtv_win.InputStructure = mapped_input_roots;
            gtv_win.TypeStructure = type_root;
            gtv_win.ShowDialog();
        }

        private bool CanExecute_SaveMappingsOfTypeNode()
        {
            if (this.tv_comps == null || this.tv_types == null) return false;
            if (this.tv_types.SelectedItem == null) return false;
            if (!(this.tv_types.SelectedItem is TypeNode)) return false;

            TypeNode tn = this.tv_types.SelectedItem as TypeNode;
            if (tn == null) return false;
            if (!tn.MappingComplete) return false;

            return true;
        }

        private void CleanAllMappings()
        {
            if (this.Root != null)
            {
                this.Root.ResetMappings();
                this.all_mappings = new List<MappingObject>();
                this.IsMapping = false;
                this.SelectedMappingSource = false;
                this.UpdateMappingTrackingDisplay();
                this.UpdateButtons();
            }
        }

        private void RestoreMapping()
        {
            ParameterStructure.Mapping.StructureMap map = this.cb_mappings.SelectedItem as ParameterStructure.Mapping.StructureMap;
            if (map == null) return;

            TypeNode map_root = this.root_node.FindMatchTo(map.TargetTree);
            if (map_root == null) return;

            this.all_mappings = map_root.LoadMappingRecord(map.Key, this.all_comps);
            this.UpdateMappingTrackingDisplay();
        }

        private bool CanExecute_RestoreMapping()
        {
            if (this.root_node == null) return false;
            if (this.cb_mappings == null) return false;
            if (this.cb_mappings.SelectedItem == null) return false;

            ParameterStructure.Mapping.StructureMap map = this.cb_mappings.SelectedItem as ParameterStructure.Mapping.StructureMap;
            if (map == null) return false;

            return true;
        }

        #endregion

        #region METHODS: Prepare for and send to Web-Service

        private void SendToWebService()
        {
            List<ParameterStructure.Mapping.StructureMap> maps_to_restore = new List<ParameterStructure.Mapping.StructureMap>();
            foreach (object item in this.cb_mappings.Items)
            {
                ParameterStructure.Mapping.StructureMap map = item as ParameterStructure.Mapping.StructureMap;
                if (map == null) continue;

                if (map.Representation.IsSelected)
                    maps_to_restore.Add(map);
            }

            if (maps_to_restore.Count == 0)
            {
                MessageBox.Show("Mindestens ein Mapping muss ausgewählt sein!", "Warnung Web-Service", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                Dictionary<TypeNode, List<object>> instances = new Dictionary<TypeNode, List<object>>();
                foreach(ParameterStructure.Mapping.StructureMap map in maps_to_restore)
                {
                    TypeNode map_root = this.root_node.FindMatchTo(map.TargetTree);
                    if (map_root == null) continue;
                    map_root.LoadMappingRecord(map.Key, this.all_comps);

                    // instantiate
                    if (map_root.MappingComplete)
                    {
                        try
                        {
                            map_root.InstantiateNodeTree();
                            if (instances.ContainsKey(map_root))
                                instances[map_root].AddRange(map_root.AllInstances);
                            else
                                instances.Add(map_root, new List<object>(map_root.AllInstances));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Instanziierung nicht möglich: \n" + ex.Message, "Instanziierung", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

                // try to instantiate the root node
                List<object> to_send = this.root_node.InstantiateNodeTreeFromExternalInput(instances);
                if (to_send == null || to_send.Count == 0) return;
                object caller = to_send[0];

                // try to find the web-service destination url
                if (this.all_web_service_urls == null || this.all_web_service_urls.Count == 0) return;
                string url = string.Empty;
                foreach(var entry in this.all_web_service_urls)
                {
                    if (this.root_node.TypeName == entry.Key)
                    {
                        url = entry.Value;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(url)) return;

                // call the web-service 
                Task sevice_caller = new Task(() =>
                {
                    object[] out_params;
                    this.Results = this.root_node.CallMainServiceMethod(caller, url, out out_params);
                    this.OutPuts = out_params;
                });
                sevice_caller.Start();
                sevice_caller.ContinueWith(this.ServiceCallHandler, CancellationToken.None);
                
            }
        }

        private bool CanExecute_SendToWebService()
        {
            if (this.root_node == null) return false;
            if (this.cb_mappings == null) return false;
            if (this.cb_mappings.Items == null) return false;
            if (this.cb_mappings.Items.Count == 0) return false;

            return true;
        }

        private void ServiceCallHandler(Task _t, object _o)
        {
            if (this.OutPuts == null || this.OutPuts.Length == 0) return;

            string feedback = this.OutPuts.Aggregate((x, y) => x.ToString() + "\n" + y.ToString()).ToString();
            MessageBox.Show(feedback, "Web-Service Antwort", MessageBoxButton.OK, MessageBoxImage.Information);

            // display the results, if there are any
            if (this.cb_results != null && this.root_node.ReturnTypeNode != null && this.Results != null && this.Results is IEnumerable)
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.cb_results.ItemsSource = this.Results as IEnumerable;
                }));
            }
        }

        #endregion

        #region METHODS: Debug

        private void ExtractStructureFromComponent()
        {
            ParameterStructure.Component.Component c = this.tv_comps.SelectedItem as ParameterStructure.Component.Component;
            if (c == null) return;

            ParameterStructure.Mapping.StructureNode struct_root = ParameterStructure.Mapping.StructureNode.CreateFrom(c, null);
            if (struct_root == null)
                MessageBox.Show("Error creating structure.", "Extracting structure from component", MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                string result = struct_root.ToString(); // debug
                GenericTreeVisWindow gtv_win = new GenericTreeVisWindow();
                gtv_win.InputStructure = new List<ParameterStructure.Mapping.StructureNode>{ struct_root };
                gtv_win.ShowDialog();
            }                
        }

        private bool CanExecute_ExtractStructureFromComponent()
        {
            return (this.tv_comps != null && this.tv_comps.SelectedItem != null && this.tv_comps.SelectedItem is ParameterStructure.Component.Component);
        }

        private void ExtractStructureFromTypeNode()
        {
            TypeNode tn = this.tv_types.SelectedItem as TypeNode;
            if (tn == null) return;

            // ParameterStructure.Mapping.StructureNode struct_root = tn.TranslateToStructure(null);

            List<ParameterStructure.Mapping.StructureNode> mapped_input_roots;
            ParameterStructure.Mapping.StructureNode type_root;
            tn.TranslateMappingsToLinkedTrees(out mapped_input_roots, out type_root);
            foreach (ParameterStructure.Mapping.StructureNode in_link in mapped_input_roots)
            {
                // string debug_1 = in_link.ToString();
                in_link.PruneUnmarked();
                // string debug_2 = in_link.ToString();
            }

            if (type_root == null)
                MessageBox.Show("Error creating structure.", "Extracting structure from TYPE", MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                // string result = type_root.ToString();
                // MessageBox.Show(result, "Extracting structure from TYPE", MessageBoxButton.OK, MessageBoxImage.Information);
                GenericTreeVisWindow gtv_win = new GenericTreeVisWindow();
                gtv_win.InputStructure = mapped_input_roots;
                gtv_win.TypeStructure = type_root;
                gtv_win.ShowDialog();
            }               
        }

        private bool CanExecute_ExtractStructureFromTypeNode()
        {
            return (this.tv_types != null && this.tv_types.SelectedItem != null && this.tv_types.SelectedItem is TypeNode);
        }

        #endregion

        #region MAPPING VISUALISATION

        private Point tracking_source;
        private Point tracking_target;
        private Point tracking_mouse_current;

        private void UpdateMappingTrackingDisplay()
        {
            this.c_pointer_WS.Children.Clear();

            // draw the existing mappings
            List<Line> mark_existing = new List<Line>();
            int counter = 0;
            foreach(MappingObject entry in this.all_mappings)
            {
                mark_existing.AddRange(this.GetMappingVis(entry, counter * CONN_OFFSET));
                counter++;
            }
            foreach (Line L in mark_existing)
            {
                this.c_pointer_WS.Children.Add(L);
            }

            // draw the interactive marking
            if (this.IsMapping)
            {
                List<Line> mark_interactive = Utils.DrawConnection(HALF_H_TV_ROW_COMP, HALF_H_TV_ROW_TN, this.c_pointer_midside_measure.ActualWidth, this.c_pointer_middle_measure.ActualWidth,
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

        private void tv_types_MouseMove(object sender, MouseEventArgs e)
        {
            if (e == null) return;

            if (this.IsMapping && this.SelectedMappingSource)
            {
                this.tracking_mouse_current = e.GetPosition(this.tv_types);
                this.tracking_target = e.GetPosition(this.tv_types);
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

        #region MAPPING: generate vis

        private List<Line> GetMappingVis(MappingObject _mo, double _connection_offset = 0.0)
        {
            List<Line> lines = new List<Line>();
            if (_mo == null) return lines;

            MappingParameter mp = _mo as MappingParameter;
            MappingString mstr = _mo as MappingString;
            MappingSinglePoint msp = _mo as MappingSinglePoint;

            TreeViewItem container_source = null;
            TreeViewItem container_target = TreeViewQuery.ContainerFromItem(this.tv_types, _mo.MappedToType);

            if (mp != null)
                container_source = TreeViewQuery.ContainerFromItem(this.tv_comps, mp.MappedParameter);
            else if (mstr != null)
                container_source = TreeViewQuery.ContainerFromItem(this.tv_comps, mstr.DirectParent);
            else if (msp != null)
                container_source = TreeViewQuery.ContainerFromItem(this.tv_comps, msp.MappedPointC);               

            if (container_source != null && container_source.IsVisible &&
                container_target != null && container_target.IsVisible)
            {
                // draw the lines
                Point source = container_source.TransformToAncestor(this.tv_comps).Transform(new Point(0, 0));
                Point target = container_target.TransformToAncestor(this.tv_types).Transform(new Point(0, 0));

                Color col = COL_DEFAULT;
                double thickness = 1.0;
                if (mp != null)
                {
                    col = COL_MAPPING_PARAM;
                    thickness = 1.0;
                }
                else if (mstr != null)
                {
                    col = COL_MAPPING_COMP;
                    thickness = 2.0;
                }
                else if (msp != null)
                {
                    col = COL_MAPPING_P3D;
                    thickness = 1.0;
                }

                lines = Utils.DrawConnection(HALF_H_TV_ROW_COMP, HALF_H_TV_ROW_TN, this.c_pointer_midside_measure.ActualWidth, this.c_pointer_middle_measure.ActualWidth,
                                             source, target, target, (container_target == null || !container_target.IsVisible), col, thickness, _connection_offset);
            }

            return lines;
        }

        #region OLD
        private List<Line> DrawConnection(Point _source, Point _intermediate, Point _target, bool _only_source_part, Color _line_color, double _thickness, double _connection_offset = 0.0)
        {
            List<Line> lines = new List<Line>();

            // determine the offset sign
            int sign = (_source.Y + HALF_H_TV_ROW_COMP > _intermediate.Y + HALF_H_TV_ROW_TN) ? 1 : -1;

            // draw the SOURCE pointer
            Line s_L1 = new Line();
            s_L1.X1 = 0;
            s_L1.Y1 = _source.Y + HALF_H_TV_ROW_COMP;
            s_L1.X2 = this.c_pointer_midside_measure.ActualWidth + _connection_offset * sign;
            s_L1.Y2 = s_L1.Y1;
            s_L1.IsHitTestVisible = false;
            s_L1.Stroke = new SolidColorBrush(_line_color);
            s_L1.StrokeThickness = _thickness;

            Line s_L2 = new Line();
            s_L2.X1 = 0;
            s_L2.Y1 = _source.Y;
            s_L2.X2 = 0;
            s_L2.Y2 = _source.Y + HALF_H_TV_ROW_COMP * 2;
            s_L2.IsHitTestVisible = false;
            s_L2.Stroke = new SolidColorBrush(_line_color);
            s_L2.StrokeThickness = _thickness;

            Line s_L3 = new Line();
            s_L3.X1 = -10;
            s_L3.Y1 = _source.Y;
            s_L3.X2 = 0;
            s_L3.Y2 = s_L3.Y1;
            s_L3.IsHitTestVisible = false;
            s_L3.Stroke = new SolidColorBrush(_line_color);
            s_L3.StrokeThickness = _thickness;

            Line s_L4 = new Line();
            s_L4.X1 = -10;
            s_L4.Y1 = _source.Y + HALF_H_TV_ROW_COMP * 2;
            s_L4.X2 = 0;
            s_L4.Y2 = s_L4.Y1;
            s_L4.IsHitTestVisible = false;
            s_L4.Stroke = new SolidColorBrush(_line_color);
            s_L4.StrokeThickness = _thickness;

            lines.Add(s_L1);
            lines.Add(s_L2);
            lines.Add(s_L3);
            lines.Add(s_L4);

            if (!(_only_source_part))
            {
                // draw the current pointer
                Line c_L1 = new Line();
                c_L1.X1 = s_L1.X2;
                c_L1.Y1 = s_L1.Y2;
                c_L1.X2 = s_L1.X2;
                c_L1.Y2 = _intermediate.Y + HALF_H_TV_ROW_TN;
                c_L1.IsHitTestVisible = false;
                c_L1.Stroke = new SolidColorBrush(_line_color);
                c_L1.StrokeThickness = _thickness;

                Line c_L2 = new Line();
                c_L2.X1 = c_L1.X2;
                c_L2.Y1 = c_L1.Y2;
                c_L2.X2 = this.c_pointer_middle_measure.ActualWidth;
                c_L2.Y2 = _intermediate.Y + HALF_H_TV_ROW_TN;
                c_L2.IsHitTestVisible = false;
                c_L2.Stroke = new SolidColorBrush(_line_color);
                c_L2.StrokeThickness = _thickness;

                lines.Add(c_L1);
                lines.Add(c_L2);

                // draw the TARGET pointer
                Line pointer = new Line();
                pointer.X1 = this.c_pointer_middle_measure.ActualWidth;
                pointer.Y1 = _target.Y + HALF_H_TV_ROW_TN;
                pointer.X2 = this.c_pointer_middle_measure.ActualWidth + _target.X;
                pointer.Y2 = _target.Y + HALF_H_TV_ROW_TN;
                pointer.IsHitTestVisible = false;
                pointer.Stroke = new SolidColorBrush(_line_color);
                pointer.StrokeThickness = _thickness;

                Line pointer_V = new Line();
                pointer_V.X1 = this.c_pointer_middle_measure.ActualWidth + _target.X;
                pointer_V.Y1 = _target.Y;
                pointer_V.X2 = this.c_pointer_middle_measure.ActualWidth + _target.X;
                pointer_V.Y2 = _target.Y + HALF_H_TV_ROW_TN * 2;
                pointer_V.IsHitTestVisible = false;
                pointer_V.Stroke = new SolidColorBrush(_line_color);
                pointer_V.StrokeThickness = _thickness;

                Line pointer_H1 = new Line();
                pointer_H1.X1 = this.c_pointer_middle_measure.ActualWidth + _target.X;
                pointer_H1.Y1 = _target.Y;
                pointer_H1.X2 = this.c_pointer_middle_measure.ActualWidth + _target.X + HALF_H_TV_ROW_TN;
                pointer_H1.Y2 = _target.Y;
                pointer_H1.IsHitTestVisible = false;
                pointer_H1.Stroke = new SolidColorBrush(_line_color);
                pointer_H1.StrokeThickness = _thickness;

                Line pointer_H2 = new Line();
                pointer_H2.X1 = this.c_pointer_middle_measure.ActualWidth + _target.X;
                pointer_H2.Y1 = _target.Y + HALF_H_TV_ROW_TN * 2;
                pointer_H2.X2 = this.c_pointer_middle_measure.ActualWidth + _target.X + HALF_H_TV_ROW_TN;
                pointer_H2.Y2 = _target.Y + HALF_H_TV_ROW_TN * 2;
                pointer_H2.IsHitTestVisible = false;
                pointer_H2.Stroke = new SolidColorBrush(_line_color);
                pointer_H2.StrokeThickness = _thickness;

                lines.Add(pointer);
                lines.Add(pointer_V);
                lines.Add(pointer_H1);
                lines.Add(pointer_H2);
            }

            return lines;
        }
        #endregion

        #endregion

        #region INSTANTIATION

        private void AttemptToInstantiate()
        {
            TypeNode tn = this.tv_types.SelectedItem as TypeNode;
            if (tn == null) return;

            try
            {
                tn.InstantiateNodeTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Instanziierung nicht möglich: \n" + ex.Message, "Instanziierung", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private bool CanExecute_AttemptToInstantiate()
        {
            if (this.tv_types == null) return false;
            if (this.tv_types.SelectedItem == null) return false;

            TypeNode tn = this.tv_types.SelectedItem as TypeNode;
            if (tn == null) return false;

            return tn.MappingComplete;
        }

        #endregion

        #region EVENT HANDLERS
        private void WebServiceMapWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitControls();
        }

        private void WebServiceMapWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateMappingTrackingDisplay();
        }

        private void tv_comps_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // adapt display
            DependencyObject selObj = TreeViewQuery.GetSelectedTreeViewItem(this.tv_comps);
            TreeViewItem tvi = selObj as TreeViewItem;
            if (tvi != null)
            {
                this.tracking_source = tvi.TransformToAncestor(this.tv_comps).Transform(new Point(0, 0));
                this.UpdateMappingTrackingDisplay();
                this.DirectParentOfMappingSource = WebServiceMapWindow.GetDirectCompParentOf(tvi);
            }

            // handle selection
            if (sender == null || e == null) return;
            TreeView tv = sender as TreeView;
            if (tv == null) return;

            object selection = tv.SelectedItem;
            if (selection == null) return;

            ParameterStructure.Parameter.Parameter selected_p = selection as ParameterStructure.Parameter.Parameter;
            ParameterStructure.Component.Component selected_c = selection as ParameterStructure.Component.Component;
            ParameterStructure.Geometry.Point3DContainer selected_p3d = selection as ParameterStructure.Geometry.Point3DContainer;

            if (selected_p != null)
            {
                if (this.IsMapping)
                    this.MappedParameter = selected_p;
                else if (this.IsMappingBack && this.SelectedMappingBackSource)
                {
                    // try to transfer the value from the selected type
                    if (this.MappedBackType.InstancesExternal != null && this.MappedBackType.InstancesExternal.Count > 0)
                    {
                        selected_p.ValueCurrent = (double)this.MappedBackType.InstancesExternal[0];
                    }
                    this.IsMappingBack = false;
                    this.SelectedMappingBackSource = false;
                    this.UpdateButtons();
                }
            }
            else if (selected_c != null)
            {
                if (this.IsMapping)
                    this.MappedComponent = selected_c;
            }
            else if (selected_p3d != null)
            {
                if (this.IsMapping)
                    this.MappedPoint3DContainer = selected_p3d;
            }
        }

        private void tv_types_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // adapt display
            DependencyObject selObj = TreeViewQuery.GetSelectedTreeViewItem(this.tv_types);
            TreeViewItem tvi = selObj as TreeViewItem;
            if (tvi != null)
            {
                this.tracking_target = tvi.TransformToAncestor(this.tv_types).Transform(new Point(0, 0));
                if (this.IsMapping)
                    this.UpdateMappingTrackingDisplay();                
            }

            // react to selection
            if (sender == null || e == null) return;
            TreeView tv = sender as TreeView;
            if (tv == null) return;

            object selection = tv.SelectedItem;
            if (selection == null) return;

            TypeNode tn = selection as TypeNode;
            if (tn == null) return;

            if (this.IsMapping && this.SelectedMappingSource)
            {
                this.CreateMapping(tn);
                this.IsMapping = false;
                this.SelectedMappingSource = false;
                this.UpdateMappingTrackingDisplay();
                this.UpdateButtons();
            }
        }

        private void tv_return_type_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // react to selection
            if (sender == null || e == null) return;
            TreeView tv = sender as TreeView;
            if (tv == null) return;

            object selection = tv.SelectedItem;
            if (selection == null) return;

            TypeNode tn = selection as TypeNode;
            if (tn != null)
            {
                if (this.IsMappingBack)
                    this.MappedBackType = tn;
            }  
        }

        private void chb_ws_entry_points_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.chb_ws_entry_points.SelectedItem != null && this.chb_ws_entry_points.SelectedItem is TypeNode)
                this.Root = this.chb_ws_entry_points.SelectedItem as TypeNode;
            else
                this.Root = null;
        }

        private void cb_results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.root_node.ReturnTypeNode == null) return;

            if (this.cb_results.SelectedItem != null)
            {
                if ((this.root_node.ReturnTypeNode.IsEnumerable) &&
                    this.root_node.ReturnTypeNode.SubNodes != null && this.root_node.ReturnTypeNode.SubNodes.Count > 0)
                {
                    TypeNode first_sub = this.root_node.ReturnTypeNode.SubNodes[0];
                    if (first_sub.ContainedType == this.cb_results.SelectedItem.GetType())
                    {
                        first_sub.PlaceInstanceInTree(this.cb_results.SelectedItem);
                    }
                }
                
            }
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            // DO SOMETHING...
            // done
            // this.DialogResult = true;
            this.Close();
        }

        #endregion

        #region UTILS

        private static ParameterStructure.Component.Component GetDirectCompParentOf(TreeViewItem _tvi)
        {
            if (_tvi == null) return null;

            ParameterStructure.Component.Component direct_parent = null;
            TreeViewItem tvi_parent = TreeViewQuery.GetParentTreeViewItem(_tvi);

            while (direct_parent == null && tvi_parent != null)
            {
                object c_parent = TreeViewQuery.ItemFromTreeViewItem(tvi_parent);
                direct_parent = c_parent as ParameterStructure.Component.Component;
                
                if (direct_parent == null)
                    tvi_parent = TreeViewQuery.GetParentTreeViewItem(tvi_parent);
            }

            return direct_parent;
        }

        #endregion
    }
}
