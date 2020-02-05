using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.IO;

using EXCELImportExport;

using DataStructVisualizer.Nodes;
using DataStructVisualizer.WinUtils;

namespace DataStructVisualizer
{
    public class AppViewModel : DependencyObject, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion   

        #region PROPERTIES: INFO RESOURCES

        public List<Node> NodeTree
        {
            get { return (List<Node>)GetValue(NodeTreeProperty); }
            set { SetValue(NodeTreeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NodeTree.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeTreeProperty =
            DependencyProperty.Register("NodeTree", typeof(List<Node>), typeof(AppViewModel),
            new UIPropertyMetadata(new List<Node>(), new PropertyChangedCallback(MyNodeTreePropertyChangedCallback)));

        private static void MyNodeTreePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel avm = d as AppViewModel;
            if (avm != null)
            {
                avm.NodeTreeCounter = 0;
            }
        }

        public int NodeTreeCounter
        {
            get { return (int)GetValue(NodeTreeCounterProperty); }
            set { SetValue(NodeTreeCounterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NodeTreeCounter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeTreeCounterProperty =
            DependencyProperty.Register("NodeTreeCounter", typeof(int), typeof(AppViewModel), new UIPropertyMetadata(0));


        public List<Node> NodeList
        {
            get { return (List<Node>)GetValue(NodeListProperty); }
            set { SetValue(NodeListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NodeList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeListProperty =
            DependencyProperty.Register("NodeList", typeof(List<Node>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<Node>()));

        public List<string> NodeNameList
        {
            get { return (List<string>)GetValue(NodeNameListProperty); }
            set { SetValue(NodeNameListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NodeNameList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodeNameListProperty =
            DependencyProperty.Register("NodeNameList", typeof(List<string>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<string>()));


        public List<NodeManagerType> NMTypes
        {
            get { return (List<NodeManagerType>)GetValue(NMTypesProperty); }
            set { SetValue(NMTypesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NMTypes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NMTypesProperty =
            DependencyProperty.Register("NMTypes", typeof(List<NodeManagerType>), typeof(AppViewModel), 
            new UIPropertyMetadata(NodePropertyValues.GetListOfAllNodeManagerTypes()));
        

        #endregion

        #region PROPERTIES: SELECTION

        public Node SelectedNode
        {
            get { return (Node)GetValue(SelectedNodeProperty); }
            set { SetValue(SelectedNodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedNode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register("SelectedNode", typeof(Node), typeof(AppViewModel),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MySelectedEntityPropertyChangedCallback),
                new CoerceValueCallback(MySelectedEntityCoerceValueCallback)));

        private static void MySelectedEntityPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel avm = d as AppViewModel;
            if (avm != null)
                avm.HandleStandardSelection();
        }

        private static object MySelectedEntityCoerceValueCallback(DependencyObject d, object baseValue)
        {
            AppViewModel avm = d as AppViewModel;
            if (avm != null)
            {
                return avm.SelectionPreview(baseValue);
            }
 	        return baseValue;
        }

        public bool SomethingISSelected
        {
            get { return (bool)GetValue(SomethingISSelectedProperty); }
            set { SetValue(SomethingISSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SomethingISSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SomethingISSelectedProperty =
            DependencyProperty.Register("SomethingISSelected", typeof(bool), typeof(AppViewModel), 
            new UIPropertyMetadata(false));



        public NodeManagerType NMTofSelectedNode
        {
            get { return (NodeManagerType)GetValue(NMTofSelectedNodeProperty); }
            set { SetValue(NMTofSelectedNodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NMTofSelectedNode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NMTofSelectedNodeProperty =
            DependencyProperty.Register("NMTofSelectedNode", typeof(NodeManagerType), typeof(AppViewModel),
            new UIPropertyMetadata(NodeManagerType.NONE, new PropertyChangedCallback(MyNMTofSelectedNodePropertyChangedCallback)));

        private static void MyNMTofSelectedNodePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel avm = d as AppViewModel;
            if (avm != null)
            {
                if (avm.SomethingISSelected)
                    avm.SelectedNode.NodeManager = avm.NMTofSelectedNode;
            }
        }


        public Node ParentOfSelectedNode
        {
            get { return (Node)GetValue(ParentOfSelectedNodeProperty); }
            set { SetValue(ParentOfSelectedNodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ParentOfSelectedNode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentOfSelectedNodeProperty =
            DependencyProperty.Register("ParentOfSelectedNode", typeof(Node), typeof(AppViewModel),
            new UIPropertyMetadata(null));


        public List<Node> ParentChainOfSelectedNode
        {
            get { return (List<Node>)GetValue(ParentChainOfSelectedNodeProperty); }
            set { SetValue(ParentChainOfSelectedNodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ParentChainOfSelectedNode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentChainOfSelectedNodeProperty =
            DependencyProperty.Register("ParentChainOfSelectedNode", typeof(List<Node>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<Node>()));


        public List<Node> ConnectionsOfSelectedNode
        {
            get { return (List<Node>)GetValue(ConnectionsOfSelectedNodeProperty); }
            set { SetValue(ConnectionsOfSelectedNodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConnectionsOfSelectedNode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConnectionsOfSelectedNodeProperty =
            DependencyProperty.Register("ConnectionsOfSelectedNode", typeof(List<Node>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<Node>()));


        #endregion

        #region PROPERTIES : SELECTION OF CONNECTED

        private Node selected_connected_node_prev;
        public Node SelectedConnectedNode
        {
            get { return (Node)GetValue(SelectedConnectedNodeProperty); }
            set { SetValue(SelectedConnectedNodeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedConnectedNode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedConnectedNodeProperty =
            DependencyProperty.Register("SelectedConnectedNode", typeof(Node), typeof(AppViewModel),
            new UIPropertyMetadata(null, new PropertyChangedCallback(MySelectedConnectedNodePropertyChangedCallback),
                new CoerceValueCallback(MySelectedConnectedNodeCoerceValueCallback)));

        private static void MySelectedConnectedNodePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel avm = d as AppViewModel;
            if (avm != null)
            {
                avm.HandleSelectionOfConnectedNode();
            }
        }

        private static object MySelectedConnectedNodeCoerceValueCallback(DependencyObject d, object baseValue)
        {
            AppViewModel avm = d as AppViewModel;
            if (d != null)
            {
                avm.selected_connected_node_prev = avm.SelectedConnectedNode;
            }
            return baseValue;
        }


        #endregion

        #region PROPERTIES: Edit Mode

        private EditMode node_edit_mode_prev;
        public EditMode NodesEditMode
        {
            get { return (EditMode)GetValue(NodesEditModeProperty); }
            set { SetValue(NodesEditModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NodesEditMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NodesEditModeProperty =
            DependencyProperty.Register("NodesEditMode", typeof(EditMode), typeof(AppViewModel), 
            new UIPropertyMetadata(EditMode.NONE));

        #endregion

        #region PROPERTIES: SORTING

        public int SortingMode
        {
            get { return (int)GetValue(SortingModeProperty); }
            set { SetValue(SortingModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortingMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortingModeProperty =
            DependencyProperty.Register("SortingMode", typeof(int), typeof(AppViewModel), new UIPropertyMetadata(0));

        public int SortingMode_Name
        {
            get { return (int)GetValue(SortingMode_NameProperty); }
            set { SetValue(SortingMode_NameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortingMode_Name.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortingMode_NameProperty =
            DependencyProperty.Register("SortingMode_Name", typeof(int), typeof(AppViewModel), new UIPropertyMetadata(-1));

        public int SortingMode_Manager
        {
            get { return (int)GetValue(SortingMode_ManagerProperty); }
            set { SetValue(SortingMode_ManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortingMode_Manager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortingMode_ManagerProperty =
            DependencyProperty.Register("SortingMode_Manager", typeof(int), typeof(AppViewModel), new UIPropertyMetadata(-1));

        public string SortingPropertyName
        {
            get { return (string)GetValue(SortingPropertyNameProperty); }
            set { SetValue(SortingPropertyNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SortingPropertyName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SortingPropertyNameProperty =
            DependencyProperty.Register("SortingPropertyName", typeof(string), typeof(AppViewModel), 
            new UIPropertyMetadata("NodeName"));

        #endregion

        #region PROPERTIES: SAVING

        public string CurrentFilePath
        {
            get { return (string)GetValue(CurrentFilePathProperty); }
            set { SetValue(CurrentFilePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentFilePath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentFilePathProperty =
            DependencyProperty.Register("CurrentFilePath", typeof(string), typeof(AppViewModel), 
            new UIPropertyMetadata(string.Empty));

        public string SavingXMLState
        {
            get { return (string)GetValue(SavingXMLStateProperty); }
            set { SetValue(SavingXMLStateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SavingXMLState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SavingXMLStateProperty =
            DependencyProperty.Register("SavingXMLState", typeof(string), typeof(AppViewModel), 
            new UIPropertyMetadata("NO"));

        #endregion

        #region PROPERTIES: LAST EDIT TOUR

        public bool LastEditTourRunning
        {
            get { return (bool)GetValue(LastEditTourRunningProperty); }
            set { SetValue(LastEditTourRunningProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LastEditTourRunning.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastEditTourRunningProperty =
            DependencyProperty.Register("LastEditTourRunning", typeof(bool), typeof(AppViewModel),
            new UIPropertyMetadata(false));

        #endregion

        #region CLASS MEMBERS

        private NodeManager NManager;
        public ICommand RefreshTreeCmd { get; private set; }
        public ICommand CollapseTreeCmd { get; private set; }
        public ICommand ExpandTreeCmd { get; private set; }
        public ICommand MoveLevel0NodeInTreeCmd { get; private set; }
        public ICommand SycleThroughSortingModesCmd { get; private set; }

        public ICommand DragNDropCmd { get; private set; }
        public ICommand MoveNodeToRootCmd { get; private set; }
        public ICommand MoveNodeToGrandParentCmd { get; private set; }
        public ICommand DeleteSelectedNodeCmd { get; private set; }
        public ICommand DeleteAllCmd { get; private set; }
        public ICommand SearchByNameCmd { get; private set; }
        public ICommand CreateNewNodeCmd { get; private set; }
        public ICommand CopySelectedNodeCmd { get; private set; }
        public ICommand TurnOffSyncCmd { get; private set; }
        public ICommand ToggleParameterTypeCmd { get; private set; }
        public ICommand ToggleParameterTypeBackCmd { get; private set; }

        public ICommand DisConnectSelectedNodeFromAllCmd { get; private set; }
        public ICommand SwitchNodeEditModeCmd { get; private set; }


        // import / export
        public ICommand ImportFromExcelCmd { get; private set; }
        private List<Node> imported_node_list;
        public ICommand ExportToExcelCmd { get; private set; }

        public ICommand ImportFromXMLCmd { get; private set; }
        public ICommand ExportToXMLCmd { get; private set; }
        public ICommand SaveCmd { get; private set; }

        public ICommand ExportToCSharpCmd { get; private set; }
        public ICommand CopyCSharpNameToClipBoardCmd { get; private set; }

        // last update functions
        public ICommand ResetLastUpdateCmd { get; private set; }

        private CancellationTokenSource cts;
        public ICommand TourLastEditsCmd { get; private set; }
        public ICommand TourLastEditsStopCmd { get; private set; }

        #endregion

        #region .CTOR

        public AppViewModel()
        {
            // commands GUI
            this.RefreshTreeCmd = new RelayCommand((x) => OnRefreshTree());
            this.CollapseTreeCmd = new RelayCommand((x) => OnCollapseTree());
            this.ExpandTreeCmd = new RelayCommand((x) => OnExpandTree());
            this.MoveLevel0NodeInTreeCmd = new RelayCommand((x) => OnMoveLevel0NodeInTree(x), 
                                                 (x) => CanExecute_OnMoveLevel0NodeInTree());
            this.SycleThroughSortingModesCmd = new RelayCommand((x) => OnSycleThroughSortingModes(x));
            this.SortingMode = 0; this.SortingMode_Name = 0; this.SortingMode_Manager = -1;

            // commands on Nodes
            this.DragNDropCmd = new RelayCommandTwoIn((x, y) => OnDragNDrop(x, y));
            this.MoveNodeToRootCmd = new RelayCommand((x) => OnMoveNodeToRoot(), (x) => CanExecute_OnMoveNodeToRoot());
            this.MoveNodeToGrandParentCmd = new RelayCommand((x) => OnMoveToGrandParent(), (x) => CanExecute_OnMoveToGrandParent());
            this.DeleteSelectedNodeCmd = new RelayCommand((x) => OnDeleteSelectedNode(), (x) => CanExecute_OnDeleteSelectedNode());
            this.DeleteAllCmd = new RelayCommand((x) => OnDeleteAll());
            this.SearchByNameCmd = new RelayCommand((x) => OnSearchByName(x));
            this.CreateNewNodeCmd = new RelayCommand((x) => OnCreateNewNode());
            this.CopySelectedNodeCmd = new RelayCommand((x) => OnCopySelectedNode(x), (x) => CanExecute_OnCopySelectedNode());
            this.TurnOffSyncCmd = new RelayCommand((x) => OnTurnSyncOff(x));
            this.ToggleParameterTypeCmd = new RelayCommand((x) => OnToggleParameterType(x));
            this.ToggleParameterTypeBackCmd = new RelayCommand((x) => OnToggleParameterTypeBack(x));
            
            this.DisConnectSelectedNodeFromAllCmd = new RelayCommand((x) => OnDisConnectSelectedNodeFromAll(),
                                                          (x) => CanExecute_OnDisConnectSelectedNodeFromAll());

            this.SwitchNodeEditModeCmd = new RelayCommand((x) => OnSwitchEditMode(x));
 
            // commands Import / Export
            this.ImportFromExcelCmd = new RelayCommand((x) => OnImportEXCEL(x));
            this.ExportToExcelCmd = new RelayCommand((x) => OnExportEXCEL(x));

            this.ImportFromXMLCmd = new RelayCommand((x) => OnImportXML());
            this.ExportToXMLCmd = new RelayCommand((x) => OnExportXML());
            this.SaveCmd = new RelayCommand((x) => OnSave(x));

            this.ExportToCSharpCmd = new RelayCommand((x) => OnExportToCSharp(), (x) => CanExecute_OnExportToCSharp());
            this.CopyCSharpNameToClipBoardCmd = new RelayCommand((x) => OnCopyCSharpNameToClipBoard(),
                                                                 (x) => CanExecute_OnExportToCSharp());

            // commands Last Update
            this.ResetLastUpdateCmd = new RelayCommand((x) => OnResetLastUpdate(), (x) => CanExecute_OnResetLastUpdate());

            this.TourLastEditsCmd = new RelayCommand((x) => OnTourLastEdits(), (x) => CanExecute_OnResetLastUpdate());
            this.TourLastEditsStopCmd = new RelayCommand((x) => OnTourLastEditsStop(), (x) => CanExecute_OnResetLastUpdate());

            // data management
            this.NManager = new NodeManager();
            this.NManager.PropertyChanged += NManager_PropertyChanged;
            
            // publish data
            this.NodeTree = new List<Node>(this.NManager.Nodes);
            this.NodeList = new List<Node>(this.NManager.GetFlatNodeList());
            this.NodeNameList = new List<string>(this.NManager.Node_Names);
        }

        #endregion

        #region COMMANDS: Import / Export EXCEL
        private void OnImportEXCEL(object _type)
        {
            if (_type == null) return;
            NodesToExcelExportType type = NodeAssembler.StringToType(_type.ToString());

            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "excel files|*.xls;*.xlsx"
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        // get the name of the table
                        string tablename = "Tabelle1";
                        Window window = Application.Current.MainWindow;
                        if (window != null)
                        {
                            MainWindow mw = window as MainWindow;
                            if (mw != null)
                            {
                               tablename = mw.OpenTableNameWindow();
                            }
                        }

                        //imports the EXCEL file
                        ExcelImporter excelImp = new ExcelImporter();
                        List<List<string>> raw_data = excelImp.ImportFromFile(dlg.FileName, tablename, 1000);
                        
                        // extract the data
                        switch (type)
                        {
                            case NodesToExcelExportType.COMPLETE:
                                this.imported_node_list = NodeAssembler.GetNodeListComplete(raw_data);
                                break;
                            case NodesToExcelExportType.EXPLICIT:
                                this.imported_node_list = NodeAssembler.GetNodeListExplicit(raw_data);
                                break;
                            default:
                                this.imported_node_list = NodeAssembler.GetNodeList(raw_data);
                                break;
                        }

                        // attach to the nodemanager
                        NodeAssembler.AttachNodesToNodeManager(this.imported_node_list, ref this.NManager);

                        // publish to GUI
                        this.OnRefreshTree();
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "EXCEL File Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "EXCEL File Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void OnExportEXCEL(object _type)
        {
            if (_type == null) return;
            NodesToExcelExportType type = NodeAssembler.StringToType(_type.ToString());

            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    FileName = "Export", // Default file name
                    DefaultExt = ".xlsx", // Default file extension
                    Filter = "excel files|*.xls;*.xlsx" // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // transfer the nodes into strings
                    List<string> data_header;
                    List<List<string>> data_as_strings;
                    switch(type)
                    {
                        case NodesToExcelExportType.COMPLETE:
                            data_header = NodeAssembler.GetNodeListHeaderComplete();
                            data_as_strings = NodeAssembler.NodeListToStringsComplete(this.NManager.Nodes, false);
                            break;
                        case NodesToExcelExportType.EXPLICIT:
                            data_header = NodeAssembler.GetNodeListHeaderComplete();
                            data_as_strings = NodeAssembler.NodeListToStringsComplete(this.NManager.Nodes, true);
                            break;
                        default:
                            data_header = NodeAssembler.GetNodeListHeader();
                            data_as_strings = NodeAssembler.NodeListToStrings(this.NManager.Nodes);
                            break;
                    }

                    // prepare an empty document
                    string filename = dlg.FileName;
                    File.Delete(filename);
                    File.Copy(".\\Data\\blank.xlsx", filename);

                    // write data
                    ExcelExporter excelExp = new ExcelExporter();
                    excelExp.ExportToFile(filename, "Export", data_header, data_as_strings);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EXCEL File Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "EXCEL File Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        #endregion

        #region COMMANDS: Import / Export XML

        private void OnImportXML()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "xml files|*.xml" // Filter files by extension
                };

                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        // retrieve data
                        this.CurrentFilePath = dlg.FileName;
                        List<Node> nodes = NodeSerializer.RetrieveNodeList(this.CurrentFilePath);

                        // attach data to the node manager
                        foreach (Node n in nodes)
                        {
                            bool success = this.NManager.AddNode(n);
                        }

                        // publish to GUI
                        this.OnRefreshTree();
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "XML File Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "XML File Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void OnExportXML()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    FileName = "Export", // Default file name
                    DefaultExt = ".xml", // Default file extension
                    Filter = "xml files|*.xml" // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // write data
                    this.CurrentFilePath = dlg.FileName;
                    NodeSerializer.SaveNodeList(this.NManager.Nodes, this.CurrentFilePath);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "XML File Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "EXCEL File Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void OnSave(object _mode)
        {
            if (this.NManager == null) return;
            if (this.NManager.Nodes == null) return;
            if (this.NManager.Nodes.Count == 0) return;
            if (_mode == null) return;

            string mode = _mode.ToString();
            if (string.IsNullOrEmpty(mode)) return;

            if (string.Equals(mode, "SAVE_AS") || string.IsNullOrEmpty(this.CurrentFilePath))
            {
                this.OnExportXML();
            }
            else if (string.Equals(mode, "SAVE"))
            {
                this.SavingXMLState = "YES";
                NodeSerializer.SaveNodeList(this.NManager.Nodes, this.CurrentFilePath);
                this.SavingXMLState = "NO";
            }
        }

        #endregion

        #region COMMANDS: Class Generation

        private void OnExportToCSharp()
        {
            if (this.SelectedNode == null) return;

            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    mw.OpenClassPreviewWindow(this.SelectedNode);
                }
            }
        }

        private bool CanExecute_OnExportToCSharp()
        {
            return (this.SelectedNode != null);
        }

        private void OnCopyCSharpNameToClipBoard()
        {
            if (this.SelectedNode == null) return;
            string name = ClassGenerator.ClassGenerator.ExtractClassName(this.SelectedNode);
            Clipboard.SetText(name);
        }

        #endregion

        #region COMMANDS: Tree Management - GUI

        private void OnRefreshTree()
        {
            // publish to GUI
            this.NodeTree = new List<Node>(this.NManager.Nodes);
            this.NodeList = new List<Node>(this.NManager.GetFlatNodeList());
            this.NodeNameList = new List<string>(this.NManager.Node_Names);
        }

        private void OnCollapseTree()
        {
            this.NManager.CollapseAll();
            // publish to GUI
            this.NodeTree = new List<Node>(this.NManager.Nodes);
            this.NodeList = new List<Node>(this.NManager.GetFlatNodeList());
        }

        private void OnExpandTree()
        {
            this.NManager.ExpandAll();
            // publish to GUI
            this.NodeTree = new List<Node>(this.NManager.Nodes);
            this.NodeList = new List<Node>(this.NManager.GetFlatNodeList());
        }

        private void OnMoveLevel0NodeInTree(object _up)
        {
            if (_up is bool)
            {
                bool up = (bool)_up;
                Node sel = this.SelectedNode;
                this.NManager.MoveNodeInList(this.SelectedNode, this.ParentOfSelectedNode, up);
                // publish to GUI
                this.NodeTree = new List<Node>(this.NManager.Nodes);
                this.NodeList = new List<Node>(this.NManager.GetFlatNodeList());
                this.SelectedNode = sel;
            }
        }

        private bool CanExecute_OnMoveLevel0NodeInTree()
        {
            return (this.SomethingISSelected);
        }

        private void OnSycleThroughSortingModes(object _propName)
        {
            if (_propName == null)
                return;

            string propName = _propName.ToString();
            if (propName == "NodeName")
            {
                this.SortingPropertyName = propName;
                this.SortingMode_Name = (this.SortingMode_Name + 1) % 3;
                this.SortingMode_Manager = -1;
                this.SortingMode = this.SortingMode_Name;
                this.NodeTreeCounter = 0;
            }
            else if (propName == "NodeManager")
            {
                this.SortingPropertyName = propName;
                this.SortingMode_Name = -1;
                this.SortingMode_Manager = (this.SortingMode_Manager + 1) % 3;
                this.SortingMode = this.SortingMode_Manager;
                this.NodeTreeCounter = 0;
            }
        }

        #endregion

        #region COMMANDS: Tree Management - Node MOVE

        private void OnDragNDrop(object _o, object _oDest)
        {
            Node n = _o as Node;
            Node nDest = _oDest as Node;
            if (n != null && nDest != null)
            {
                this.NManager.MoveNode(n, nDest);
                this.OnRefreshTree();
                this.SelectedNode = n;
            }
        }

        private void OnMoveNodeToRoot()
        {
            Node n = this.SelectedNode;
            this.NManager.MoveNode(n, NodeManager.ROOT);
            this.OnRefreshTree();
            //this.SelectedNode = null;
            this.SelectedNode = n;
        }

        private bool CanExecute_OnMoveNodeToRoot()
        {
            return (this.SelectedNode != null);
        }

        private void OnMoveToGrandParent()
        {
            Node n = this.SelectedNode;
            int nrParents = this.ParentChainOfSelectedNode.Count;
            Node nDest = this.ParentChainOfSelectedNode[nrParents - 2];
            this.NManager.MoveNode(n, nDest);
            this.OnRefreshTree();
            //this.SelectedNode = null;
            this.SelectedNode = n;
        }

        private bool CanExecute_OnMoveToGrandParent()
        {
            return (this.SelectedNode != null && this.ParentChainOfSelectedNode != null && 
                                                 this.ParentChainOfSelectedNode.Count > 1);
        }

        #endregion

        #region COMMANDS: Tree Management - Node DELETE, NEW, COPY

        private void OnDeleteSelectedNode()
        {
            string message = "Do you really want to delete " + this.SelectedNode.NodeName + " ?";
            string caption = "Deleting Object: " + this.SelectedNode.NodeName;
            MessageBoxResult answer = MessageBox.Show(message, caption,
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer == MessageBoxResult.Yes)
            {
                Node parent = this.ParentOfSelectedNode;
                bool success = this.NManager.RemoveNode(this.SelectedNode);
                if (success)
                {
                    this.OnRefreshTree();
                    this.SelectedNode = parent;              
                }
            }
        }

        private bool CanExecute_OnDeleteSelectedNode()
        {
            return (this.SelectedNode != null);
        }

        private void OnDeleteAll()
        {
            string message = "Do you really want to delete all data ?";
            string caption = "Deleting ALL";
            MessageBoxResult answer = MessageBox.Show(message, caption,
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer == MessageBoxResult.Yes)
            {
                this.NManager.Nodes.Clear();
                this.SelectedNode = null;
                this.OnRefreshTree();
            }
        }

        private void OnCreateNewNode()
        {
            // creates a node without event handlers
            Node newN = new Node();
            if (this.SelectedNode != null)
                this.NManager.AddNodeToNode(newN, this.SelectedNode, true);
            else
                this.NManager.AddNode(newN);

            // communicate tp GUI
            this.OnRefreshTree();
            this.SelectedNode = newN;
        }

        private void OnCopySelectedNode(object _deep_copy)
        {
            bool deep_copy = false;
            if (_deep_copy != null)
            {
                if (_deep_copy.ToString() == "DEEP")
                    deep_copy = true;
            }

            // make the copy (including event handlers)
            Node copyN;
            if (deep_copy)
                copyN = new Node(this.SelectedNode, true);
            else
                copyN = new Node(this.SelectedNode, false);

            // attach to the same parent node
            if (this.ParentOfSelectedNode != null)
                this.NManager.AddNodeToNode(copyN, this.ParentOfSelectedNode, false);
            else
                this.NManager.AddNode(copyN);

            // communicate to GUI
            this.OnRefreshTree();
            this.SelectedNode = copyN;
        }

        private bool CanExecute_OnCopySelectedNode()
        {
            return (this.SelectedNode != null);
        }

        #endregion

        #region COMMANDS: Tree Management - Node CONNECT, Turn Sync Off, Cycle through Parameter Type

        private void OnDisConnectSelectedNodeFromAll()
        {
            this.SelectedNode.RemoveAllConnections();

            Node selected = this.SelectedNode;
            this.SelectedNode = null;
            this.SelectedNode = selected;
        }

        private bool CanExecute_OnDisConnectSelectedNodeFromAll()
        {
            return (this.SelectedNode != null && this.SelectedNode.ConnectionTo.Count > 0);
        }

        private void OnTurnSyncOff(object _o)
        {
            Node n = _o as Node;
            if (n != null)
            {
                string message = "Do you really want to turn synchronization off for " + n.NodeName + " ?\nThis action cannot be reversed.";
                string caption = "Turning synchronization off: " + n.NodeName;
                MessageBoxResult answer = MessageBox.Show(message, caption,
                            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (answer == MessageBoxResult.Yes)
                {
                    n.SyncByName = false;
                }
            }
        }

        private void OnToggleParameterType(object _o)
        {
            Node n = _o as Node;
            if (n != null)
            {
                int nrTypes = Enum.GetNames(typeof(ParameterType)).Length;
                n.NodeParamType = (ParameterType)((int)(n.NodeParamType + 1) % nrTypes);
            }
        }

        private void OnToggleParameterTypeBack(object _o)
        {
            Node n = _o as Node;
            if (n != null)
            {
                int nrTypes = Enum.GetNames(typeof(ParameterType)).Length;
                n.NodeParamType = (ParameterType)((int)(nrTypes + n.NodeParamType - 1) % nrTypes);
            }
        }

        #endregion

        #region COMMANDS: Tree Management - Node SEARCH

        private void OnSearchByName(object _name)
        {
            if (_name == null)
                return;

            string strName = _name.ToString();
            if (string.IsNullOrEmpty(strName))
                return;

            this.NManager.ProcessSearch(strName);
        }

        #endregion

        #region COMMANDS: Tree Management - Node Last Edit

        private void OnResetLastUpdate()
        {
            this.NManager.SetAllLastEditsToNow();
        }

        private bool CanExecute_OnResetLastUpdate()
        {
            return (this.NManager != null && this.NManager.Nodes != null && this.NManager.Nodes.Count > 1);
        }


        private async void OnTourLastEdits()
        {           
            this.cts = new CancellationTokenSource();
            try
            {
                this.LastEditTourRunning = true;
                this.NodesEditMode = EditMode.TOUR_RUNNING;
                await this.NManager.TourNodesAccordingToLastEdit(this.SelectedNode, this.cts.Token);
            }
            catch(OperationCanceledException)
            {
                this.LastEditTourRunning = false;
                this.NodesEditMode = EditMode.NONE;
            }
            this.cts = null;
            this.LastEditTourRunning = false;
            this.NodesEditMode = EditMode.NONE;
        }

        private void OnTourLastEditsStop()
        {
            if (this.cts != null)
                cts.Cancel();
        }

        #endregion

        #region SELECTION HANDLING, EDIT MODE

        private object SelectionPreview(object _preview_object)
        {
            Node n = _preview_object as Node;
            switch (this.NodesEditMode)
            {
                case EditMode.CONNECT:
                    if (n != null)
                    {
                        this.SelectedNode.AddConnectionToNode(n);
                        this.ConnectionsOfSelectedNode = new List<Node>(this.SelectedNode.ConnectionTo);
                    }
                    return this.SelectedNode;
                case EditMode.DISCONNECT:
                    return this.SelectedNode;
                case EditMode.SHOW_CONN:
                    return this.SelectedNode;
                case EditMode.TOUR_RUNNING:
                    return this.SelectedNode;
                default:                    
                    return _preview_object;
            }
        }

        private void HandleStandardSelection()
        {            
            this.SomethingISSelected = (this.SelectedNode != null);
            foreach (Node pN in this.ParentChainOfSelectedNode)
            {
                pN.IsParentOfSelected = false;
            }
            if (this.SomethingISSelected)
            {
                this.NMTofSelectedNode = this.SelectedNode.NodeManager;
                List<Node> parent_chain = this.NManager.GetParentNodeChain(this.SelectedNode);                
                if (parent_chain != null)
                {
                    int len = parent_chain.Count;
                    this.ParentChainOfSelectedNode = parent_chain.Take(len - 1).ToList();
                    foreach (Node pN in this.ParentChainOfSelectedNode)
                    {
                        pN.IsParentOfSelected = true;
                    }
                    if (len <= 1)
                        this.ParentOfSelectedNode = null;
                    else if (len > 1)
                        this.ParentOfSelectedNode = parent_chain[len - 2];
                }
                this.ConnectionsOfSelectedNode = new List<Node>(this.SelectedNode.ConnectionTo);
            }
            else
            {
                this.NMTofSelectedNode = NodeManagerType.NONE;
                this.ParentChainOfSelectedNode = new List<Node>();
                this.ConnectionsOfSelectedNode = new List<Node>();
            }      
            // communicate to the node manager
            this.NManager.SelectNode(this.SelectedNode);
        }

        private void HandleSelectionOfConnectedNode()
        {
            switch (this.NodesEditMode)
            {
                case EditMode.CONNECT:
                    break;
                case EditMode.DISCONNECT:
                    if (this.SelectedConnectedNode != null)
                    {
                        this.SelectedNode.RemoveConnectionToNode(this.SelectedConnectedNode);
                        this.ConnectionsOfSelectedNode = new List<Node>(this.SelectedNode.ConnectionTo);
                    }
                    break;
                case EditMode.SHOW_CONN:
                    break;
                case EditMode.TOUR_RUNNING:
                    break;
                default:
                    // show in tree
                    if (this.selected_connected_node_prev != null)
                    {
                        this.selected_connected_node_prev.UnHighlight();
                    }
                    if (this.SelectedConnectedNode != null)
                    {
                        this.SelectedConnectedNode.Highlight();
                    }
                    break;
            }
        }

        private void OnSwitchEditMode(object _mode)
        {
            if (_mode == null)
                return;

            // here we are RETURNING from the respective mode
            switch(this.NodesEditMode)
            {
                case EditMode.CONNECT:
                    break;
                case EditMode.DISCONNECT:
                    break;
                case EditMode.SHOW_CONN:
                    this.NManager.UnHighlightConnectedNodes(this.SelectedNode);
                    break;
                case EditMode.TOUR_RUNNING:
                    break;
                default:
                    break;
            }

            // perform SWITCH
            this.node_edit_mode_prev = this.NodesEditMode;
            this.NodesEditMode = NodePropertyValues.StringToEditMode(_mode.ToString());
            if (this.node_edit_mode_prev == this.NodesEditMode)
            {
                // twice the same -> go to NONE
                this.NodesEditMode = EditMode.NONE;
            }

            // here we are ENTERING into the respective mode
            switch (this.NodesEditMode)
            {
                case EditMode.CONNECT:
                    break;
                case EditMode.DISCONNECT:
                    break;
                case EditMode.SHOW_CONN:
                    this.NManager.HighlightConnectedNodes(this.SelectedNode);
                    break;
                case EditMode.TOUR_RUNNING:
                    break;
                default:
                    Node sel = this.SelectedNode;
                    this.SelectedNode = sel;
                    break;
            }

        }

        #endregion

        #region EVENT HANDLERS

        void NManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NodeManager nm = sender as NodeManager;
            if (nm != null && e != null && e.PropertyName != null)
            {
                if (e.PropertyName == "Node_Names")
                {
                    this.NodeNameList = new List<string>(this.NManager.Node_Names);
                }
            }
        }

        #endregion

    }
}
