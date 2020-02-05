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
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Timers;

using TalkGit;
using MyDataTypes;

using InterProcCommunication;
using InterProcCommunication.Specific;

using ParameterStructure.Component;
using ParameterStructure.Parameter;
using ParameterStructure.Values;
using ParameterStructure.Mapping;
using ParameterStructure.DXF;
using ParameterStructure.EXCEL;
using ParameterStructure.Utils;

using ComponentBuilder.WinUtils;
using ComponentBuilder.WpfUtils;
using ComponentBuilder.GitUtils;
using ComponentBuilder.Communication;
using ComponentBuilder.WebServiceConnections;

namespace ComponentBuilder
{
    class AppViewModel : DependencyObject, INotifyPropertyChanged
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

        public List<Parameter> ExistingParameters
        {
            get { return (List<Parameter>)GetValue(ExistingParametersProperty); }
            set { SetValue(ExistingParametersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingParameters.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingParametersProperty =
            DependencyProperty.Register("ExistingParameters", typeof(List<Parameter>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<Parameter>()));

        public List<MultiValue> ExistingValues
        {
            get { return (List<MultiValue>)GetValue(ExistingValuesProperty); }
            set { SetValue(ExistingValuesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingValues.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingValuesProperty =
            DependencyProperty.Register("ExistingValues", typeof(List<MultiValue>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<MultiValue>()));

        public List<Calculation> ExistingCalcs
        {
            get { return (List<Calculation>)GetValue(ExistingCalcsProperty); }
            set { SetValue(ExistingCalcsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingCalcs.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingCalcsProperty =
            DependencyProperty.Register("ExistingCalcs", typeof(List<Calculation>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<Calculation>()));

        public List<ParameterStructure.Component.Component> ExistingComponents
        {
            get { return (List<ParameterStructure.Component.Component>)GetValue(ExistingComponentsProperty); }
            set { SetValue(ExistingComponentsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingComponents.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingComponentsProperty =
            DependencyProperty.Register("ExistingComponents", typeof(List<ParameterStructure.Component.Component>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<ParameterStructure.Component.Component>()));


        public List<FlowNetwork> ExistingNetworks
        {
            get { return (List<FlowNetwork>)GetValue(ExistingNetworksProperty); }
            set { SetValue(ExistingNetworksProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingNetworks.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingNetworksProperty =
            DependencyProperty.Register("ExistingNetworks", typeof(List<FlowNetwork>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<FlowNetwork>()));

        public List<SelectableString> ExistingFunctionSlots
        {
            get { return (List<SelectableString>)GetValue(ExistingFunctionSlotsProperty); }
            set { SetValue(ExistingFunctionSlotsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingFunctionSlots.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingFunctionSlotsProperty =
            DependencyProperty.Register("ExistingFunctionSlots", typeof(List<SelectableString>), typeof(AppViewModel),
            new PropertyMetadata(new List<SelectableString>()));

        public List<ImageRecord> ExistingSymbols
        {
            get { return (List<ImageRecord>)GetValue(ExistingSymbolsProperty); }
            set { SetValue(ExistingSymbolsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingSymbols.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingSymbolsProperty =
            DependencyProperty.Register("ExistingSymbols", typeof(List<ImageRecord>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<ImageRecord>()));


        public List<LoggerEntry> ExistingLoggerEntries
        {
            get { return (List<LoggerEntry>)GetValue(ExistingLoggerEntriesProperty); }
            set { SetValue(ExistingLoggerEntriesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingLoggerEntries.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingLoggerEntriesProperty =
            DependencyProperty.Register("ExistingLoggerEntries", typeof(List<LoggerEntry>), typeof(AppViewModel), 
            new UIPropertyMetadata(new List<LoggerEntry>()));


        public List<string> ExistingManagersTypes
        {
            get { return (List<string>)GetValue(ExistingManagersTypesProperty); }
            set { SetValue(ExistingManagersTypesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExistingManagersTypes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExistingManagersTypesProperty =
            DependencyProperty.Register("ExistingManagersTypes", typeof(List<string>), typeof(AppViewModel), 
            new PropertyMetadata(new List<string>()));


        #endregion

        #region PROPERTIES: INFO COMMUNICATION

        public bool CommUnitRunning
        {
            get { return (bool)GetValue(CommUnitRunningProperty); }
            set { SetValue(CommUnitRunningProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommUnitRunning.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommUnitRunningProperty =
            DependencyProperty.Register("CommUnitRunning", typeof(bool), typeof(AppViewModel),
            new UIPropertyMetadata(false, new PropertyChangedCallback(CommUnitRunningPropertyChangedCallback)));

        private static void CommUnitRunningPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //AppViewModel instance = d as AppViewModel;
            //if (instance == null) return;

            //var debug = instance.CommUnitRunning;
        }

        public bool CommFinishedSendingMessages
        {
            get { return (bool)GetValue(CommFinishedSendingMessagesProperty); }
            set { SetValue(CommFinishedSendingMessagesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommFinishedSendingMessages.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommFinishedSendingMessagesProperty =
            DependencyProperty.Register("CommFinishedSendingMessages", typeof(bool), typeof(AppViewModel), 
            new UIPropertyMetadata(true));

        #endregion

        #region PROPETIES: INFO RESOURCES - Component Factory

        public ComponentFactory COMPFactoryProp
        {
            get { return this.COMPFactory; }
            private set
            {
                this.COMPFactory = value;
                this.RegisterPropertyChanged("COMPFactoryProp");
            }
        }

        #endregion

        #region PROPERTIES: Mode, UserProfile

        public GuiModus Mode
        {
            get { return (GuiModus)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Mode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(GuiModus), typeof(AppViewModel), 
            new UIPropertyMetadata(GuiModus.NEUTRAL));

        public ComponentManagerType UserRole
        {
            get { return (ComponentManagerType)GetValue(UserRoleProperty); }
            set { SetValue(UserRoleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserRole.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserRoleProperty =
            DependencyProperty.Register("UserRole", typeof(ComponentManagerType), typeof(AppViewModel), 
            new UIPropertyMetadata(ComponentManagerType.GUEST));


        public bool UserHasWritingAccessForSelectedComp
        {
            get { return (bool)GetValue(UserHasWritingAccessForSelectedCompProperty); }
            set { SetValue(UserHasWritingAccessForSelectedCompProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserHasWritingAccessForSelectedComp.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserHasWritingAccessForSelectedCompProperty =
            DependencyProperty.Register("UserHasWritingAccessForSelectedComp", typeof(bool), typeof(AppViewModel), 
            new UIPropertyMetadata(false));


        #endregion

        #region PROPERTIES: Image (Symbol) Selection

        public ImageRecord SelectedSymbol
        {
            get { return (ImageRecord)GetValue(SelectedSymbolProperty); }
            set { SetValue(SelectedSymbolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedSymbol.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedSymbolProperty =
            DependencyProperty.Register("SelectedSymbol", typeof(ImageRecord), typeof(AppViewModel),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SelectedSymbolPropertyChangedCallback)));

        private static void SelectedSymbolPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;

            instance.HandleSymbolSelection();
        }

        #endregion

        #region PROPERTIES: Value Selection

        public MultiValue SelectedValue
        {
            get { return (MultiValue)GetValue(SelectedValueProperty); }
            set { SetValue(SelectedValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue", typeof(MultiValue), typeof(AppViewModel),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SelectedValuePropertyChangedCallback)));

        private static void SelectedValuePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;

            instance.HandleValueSelection();
        }


        #endregion

        #region PROPERTIES: Parameter Selection

        public Parameter SelectedParameter
        {
            get { return (Parameter)GetValue(SelectedParameterProperty); }
            set { SetValue(SelectedParameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedParameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedParameterProperty =
            DependencyProperty.Register("SelectedParameter", typeof(Parameter), typeof(AppViewModel),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SelectedParameterPropertyChangedCallback),
                                        new CoerceValueCallback(SelectedParameterPropertyCoerceValueCallback)));

        private static object SelectedParameterPropertyCoerceValueCallback(DependencyObject d, object baseValue)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return baseValue;

            Parameter p = baseValue as Parameter;
            instance.HandleParameterValueCheck(p);

            return baseValue;
        }

        private static void SelectedParameterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;

            instance.HandleParameterSelection();           
        }

        public ParameterDummy SelectedParameterDummy
        {
            get { return (ParameterDummy)GetValue(SelectedParameterDummyProperty); }
            set { SetValue(SelectedParameterDummyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedParameterDummy.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedParameterDummyProperty =
            DependencyProperty.Register("SelectedParameterDummy", typeof(ParameterDummy), typeof(AppViewModel), 
            new UIPropertyMetadata(null));


        #endregion

        #region PROPERTIES: Calculation Selection

        public Calculation SelectedCalculation
        {
            get { return (Calculation)GetValue(SelectedCalculationProperty); }
            set { SetValue(SelectedCalculationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedCalculation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedCalculationProperty =
            DependencyProperty.Register("SelectedCalculation", typeof(Calculation), typeof(AppViewModel),
            new PropertyMetadata(null, new PropertyChangedCallback(SelectedCalculationPropertyChangedCallback)));

        private static void SelectedCalculationPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;

            instance.HandleCalcSelection();
        }

        public Calculation SelectedCompCalculation
        {
            get { return (Calculation)GetValue(SelectedCompCalculationProperty); }
            set { SetValue(SelectedCompCalculationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedCompCalculation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedCompCalculationProperty =
            DependencyProperty.Register("SelectedCompCalculation", typeof(Calculation), typeof(AppViewModel), 
            new UIPropertyMetadata(null));

        #endregion

        #region PROPERTIES: Component Selection

        // the list of components contains parameters, claculations and empty slots for subcomponents and referenced components
        // to make sure that ONLY components are selectable (i.e. SelectedComponent is always a Component) 
        // we use an ITEM CONTAINER STYLE that makes only components FOCUSABLE
        public ParameterStructure.Component.Component SelectedComponent
        {
            get { return (ParameterStructure.Component.Component)GetValue(SelectedComponentProperty); }
            set { SetValue(SelectedComponentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedComponent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedComponentProperty =
            DependencyProperty.Register("SelectedComponent", typeof(ParameterStructure.Component.Component), typeof(AppViewModel),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SelectedComponentPropertyChangedCallback),
                                         new CoerceValueCallback(SelectedComponentCoerceValueCallback)));

        // if the application does not reach this method -> the selection was prevented by the xaml style (see App.xaml)
        private static object SelectedComponentCoerceValueCallback(DependencyObject d, object baseValue)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return baseValue;
            return instance.HandleComponentSelectionPreview(baseValue);
        }

        private static void SelectedComponentPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;

            instance.HandleComponentSelection();
        }


        public bool SynchronizeSelectionWithGV
        {
            get { return (bool)GetValue(SynchronizeSelectionWithGVProperty); }
            set { SetValue(SynchronizeSelectionWithGVProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SynchronizeSelectionWithGV.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SynchronizeSelectionWithGVProperty =
            DependencyProperty.Register("SynchronizeSelectionWithGV", typeof(bool), typeof(AppViewModel), 
            new UIPropertyMetadata(false));


        #endregion

        #region PROPERTIES: Component Network Selection

        public FlowNetwork SelectedNetwork
        {
            get { return (FlowNetwork)GetValue(SelectedNetworkProperty); }
            set { SetValue(SelectedNetworkProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedNetwork.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedNetworkProperty =
            DependencyProperty.Register("SelectedNetwork", typeof(FlowNetwork), typeof(AppViewModel),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SelectedNetworkPropertyChangedCallback)));

        private static void SelectedNetworkPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;
            if (instance.SelectedNetwork == null) return;

            instance.SelectedNWOwnerLetter = ComponentUtils.ComponentManagerTypeToLetter(instance.SelectedNetwork.Manager);

            if (instance.win_NW_Graph != null && instance.win_NW_Graph.IsActive)
            {
                instance.win_NW_Graph.Network = instance.SelectedNetwork;
            }
        }



        public string SelectedNWOwnerLetter
        {
            get { return (string)GetValue(SelectedNWOwnerLetterProperty); }
            set { SetValue(SelectedNWOwnerLetterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedNWOwnerLetter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedNWOwnerLetterProperty =
            DependencyProperty.Register("SelectedNWOwnerLetter", typeof(string), typeof(AppViewModel),
            new UIPropertyMetadata("@", new PropertyChangedCallback(SelectedNWOwnerLetterPropertyChangedCallback)));

        private static void SelectedNWOwnerLetterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;
            if (instance.SelectedNetwork == null) return;
            if (instance.COMPFactory == null) return;

            ComponentManagerType manager = ComponentUtils.StringToComponentManagerType(instance.SelectedNWOwnerLetter);
            if (manager != instance.SelectedNetwork.Manager)
                instance.COMPFactory.SwitchNetworkManager(instance.SelectedNetwork, manager);
                
        }

        

        #endregion

        #region PROPERTIES: Component Project Management, GIT

        public DXFDistributedDecoder ProjectImporter
        {
            get { return (DXFDistributedDecoder)GetValue(ProjectImporterProperty); }
            set { SetValue(ProjectImporterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ProjectImporter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProjectImporterProperty =
            DependencyProperty.Register("ProjectImporter", typeof(DXFDistributedDecoder), typeof(AppViewModel),
            new UIPropertyMetadata(null, new PropertyChangedCallback(ProjectImporterPropertyChangedCallback)));

        private static void ProjectImporterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;

            instance.ProjectIsOpen = (instance.ProjectImporter != null);
        }

        public bool ProjectIsOpen
        {
            get { return (bool)GetValue(ProjectIsOpenProperty); }
            set { SetValue(ProjectIsOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ProjectIsOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProjectIsOpenProperty =
            DependencyProperty.Register("ProjectIsOpen", typeof(bool), typeof(AppViewModel), 
            new UIPropertyMetadata(false));

        public int GITNrStepsBehindNewest
        {
            get { return (int)GetValue(GITNrStepsBehindNewestProperty); }
            set { SetValue(GITNrStepsBehindNewestProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GITNrStepsBehindNewest.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GITNrStepsBehindNewestProperty =
            DependencyProperty.Register("GITNrStepsBehindNewest", typeof(int), typeof(AppViewModel),
            new UIPropertyMetadata(0, new PropertyChangedCallback(GITNrStepsBehindNewestPropertyChangedCallback)));

        private static void GITNrStepsBehindNewestPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;

            instance.GITUpdateNecessary = (instance.GITNrStepsBehindNewest > 0);
        }

        public bool GITUpdateNecessary
        {
            get { return (bool)GetValue(GITUpdateNecessaryProperty); }
            set { SetValue(GITUpdateNecessaryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GITUpdateNecessary.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GITUpdateNecessaryProperty =
            DependencyProperty.Register("GITUpdateNecessary", typeof(bool), typeof(AppViewModel), 
            new UIPropertyMetadata(false));

        

        #endregion

        #region PROPERTIES: User-defined component visibility, Parameter visibility

        public bool UserDefinedVisibilityON
        {
            get { return (bool)GetValue(UserDefinedVisibilityONProperty); }
            set { SetValue(UserDefinedVisibilityONProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserDefinedVisibilityON.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserDefinedVisibilityONProperty =
            DependencyProperty.Register("UserDefinedVisibilityON", typeof(bool), typeof(AppViewModel),
            new UIPropertyMetadata(false, new PropertyChangedCallback(UserDefinedVisibilityONPropertyChangedCallback)));

        private static void UserDefinedVisibilityONPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;
            if (instance.COMPFactory == null) return;

            instance.ExistingComponents = instance.COMPFactory.ExtractVisibleComponents(instance.UserDefinedVisibilityON);
        }

        public bool ParamVisibilityInputOnlyON
        {
            get { return (bool)GetValue(ParamVisibilityInputOnlyONProperty); }
            set { SetValue(ParamVisibilityInputOnlyONProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ParamVisibilityInputOnlyON.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParamVisibilityInputOnlyONProperty =
            DependencyProperty.Register("ParamVisibilityInputOnlyON", typeof(bool), typeof(AppViewModel),
            new UIPropertyMetadata(false, new PropertyChangedCallback(ParamVisibilityacc2PropagationChangedCallback)));

        public bool ParamVisibilityNWOnlyON
        {
            get { return (bool)GetValue(ParamVisibilityNWOnlyONProperty); }
            set { SetValue(ParamVisibilityNWOnlyONProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ParamVisibilityNWOnlyON.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParamVisibilityNWOnlyONProperty =
            DependencyProperty.Register("ParamVisibilityNWOnlyON", typeof(bool), typeof(AppViewModel),
            new UIPropertyMetadata(false, new PropertyChangedCallback(ParamVisibilityacc2PropagationChangedCallback)));

        private static void ParamVisibilityacc2PropagationChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;
            if (instance.COMPFactory == null) return;

            if (instance.ParamVisibilityInputOnlyON || instance.ParamVisibilityNWOnlyON)
            {
                instance.COMPFactory.PVis = new Dictionary<InfoFlow, bool>
                {
                    {InfoFlow.INPUT, instance.ParamVisibilityInputOnlyON},
                    {InfoFlow.OUPUT, false},
                    {InfoFlow.MIXED, false},
                    {InfoFlow.REF_IN, false},
                    {InfoFlow.CALC_IN, instance.ParamVisibilityNWOnlyON}
                };
            }
            else
            {
                // if both are off -> show ALL
                instance.COMPFactory.PVis = new Dictionary<InfoFlow, bool>
                {
                    {InfoFlow.INPUT, true},
                    {InfoFlow.OUPUT, true},
                    {InfoFlow.MIXED, true},
                    {InfoFlow.REF_IN, true},
                    {InfoFlow.CALC_IN, true}
                };
            }
            instance.ExistingComponents = instance.COMPFactory.ExtractVisibleComponents(instance.UserDefinedVisibilityON);
        }



        public string CompParamFilterString
        {
            get { return (string)GetValue(CompParamFilterStringProperty); }
            set { SetValue(CompParamFilterStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CompParamFilterString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CompParamFilterStringProperty =
            DependencyProperty.Register("CompParamFilterString", typeof(string), typeof(AppViewModel),
            new UIPropertyMetadata(string.Empty, new PropertyChangedCallback(CompParamFilterStringPropertyChangedCallback)));

        private static void CompParamFilterStringPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AppViewModel instance = d as AppViewModel;
            if (instance == null) return;
            if (instance.COMPFactory == null) return;

            instance.COMPFactory.ParamFilterString = instance.CompParamFilterString;
            if (instance.SelectedComponent != null)
                instance.SelectedComponent.UpdateParamChildrenContainer();
        }



        #endregion

        #region CLASS MEMBERS

        private Task GeomViewerTask;

        private LocalLogger LoggingService;

        private CommDuplex CommUnit;
        private LocalLogger CU_Logger;
        private string CU_server_name = "CompBild_Server";
        private string CU_client_name = "GeomView_Server";
        private string CU_authentication = "SIMULTAN_CB_GV";

        private ComponentFactory COMPFactory;
        private CalculationFactory CALCFactory;
        private ParameterFactory PFactory;
        private MultiValueFactory MVFactory;
        private ImageRecordManager IRManager;

        private GuiModus mode_prev;
        private Stack<GuiModus> modes_prev;
        public ICommand SwitchGuiModeCmd { get; private set; }

        // windows containing the component garphs
        private UIElements.ComponentGraphWindow win_Graph;
        private UIElements.FlowNetworkGraphWindow win_NW_Graph;
        // window containing the componentsto be compared
        private UIElements.CompCompareWindow win_CCompare;
        // window containing the reserved parameter names
        private UIElements.ReservedPNamesWindow win_ResPNames;
        public ICommand ShowReservedParamNamesCmd { get; private set; }
        // window for mapping components to webservices
        private UIElements.WebServiceMapWindow win_WS;
        // window for mapping one component to other components
        private UIElements.Comp2CompMappingWindow win_comp2compM;

        // log entries
        public ICommand SaveLogEntriesCmd { get; private set; }

        // symbols
        public ICommand ImportImageFromPNGCmd { get; private set; }
        public ICommand ImportImagesFromBinaryFileCmd { get; private set; }
        public ICommand SaveImagesToBinaryFileCmd { get; private set; }
        public ICommand DeleteSelectedImageCmd { get; private set; }
        public ICommand ApplyImagesToComponentsCmd { get; private set; }

        // values
        public ICommand DefineNewValueCmd { get; private set; }
        public ICommand DefineNewFunctCmd { get; private set; }
        public ICommand DeleteSelectedValueCmd { get; private set; }
        public ICommand DeleteAllValuesCmd { get; private set; }
        public ICommand CopySelectedValueCmd { get; private set; }
        public ICommand EditSelectedValueCmd { get; private set; }
        public ICommand ShowSelectedValueCmd { get; private set; }

        public ICommand SaveAllValuesCmd { get; private set; }
        public ICommand ImportValuesFromDXFCmd { get; private set; }
        public ICommand ImportEXCELValueTableCmd { get; private set; }
        public ICommand ImportEXCELValueTableNamedRowsCmd { get; private set; }

        // parameter
        public ICommand DecoupleFromFalueFieldCmd { get; private set; }
        
        private MultiValPointer picked_value_own_pointer;
        public ICommand DeleteSelectedParameterCmd { get; private set; }
        public ICommand DeleteAllParametersCmd { get; private set; }
        public ICommand CopySelectedParameterCmd { get; private set; }
        public ICommand SaveAllParametersCmd { get; private set; }
        public ICommand ImportParametersFromDXFCmd { get; private set; }

        // calculations
        string calc_param_symb_CURRENT;
        public ICommand InputParamSelectionCmd { get; private set; }
        public ICommand OutputParamSelectionCmd { get; private set; }
        public ICommand PerformSelectedCalculationCmd { get; private set; }
        public ICommand InputParamSelection_CalcInComp_Cmd { get; private set; }
        public ICommand OutputParamSelection_CalcInComp_Cmd { get; private set; }
        public ICommand PerformSelectedCalculation_CalcInComp_Cmd { get; private set; }

        public ICommand DeleteSelectedCalculationCmd { get; private set; }
        public ICommand CopySelectedCalculationCmd { get; private set; }

        // COMPONENTS
        public ICommand CompParameterSelectionCmd { get; private set; }
        public ICommand CompAssignSlotsCmd { get; private set; }
        public ICommand CompSubcompSelectionCmd { get; private set; }
        public ICommand CompRefCompSelectionCmd { get; private set; }
        public ICommand CompRefCompHighlightCmd { get; private set; }
        public ICommand CompPerformCalculationChainCmd { get; private set; }
        public ICommand CompPerformAllCalculationChainsCmd { get; private set; }
        
        private bool comp_selecting_subcomponent;
        private string comp_selecting_slot_name;

        public ICommand SaveAllComponentsCmd { get; private set; } // to single file
        public ICommand ImportAllComponentsFromDXFCmd { get; private set; } // from single file, replacing the current content
        public ICommand ImportComponentFromDXFCmd { get; private set; } // import to project
        public ICommand ImportComponentsFromDXFAddingCmd { get; private set; } // import the full content of a file (including NWs) to project
        public ICommand SaveComponentProjectToDXFCmd { get; private set; } // to summay file referencing multiple dxf files
        public ICommand SaveComponentProjectAsToDXFCmd { get; private set; }
        public ICommand OpenComponentProjectFromDXFCmd { get; private set; } // from summary file referencing multiple dxf files
        public ICommand CloseComponentProjectCmd { get; private set; }
        public ICommand RepareComponentPojectCmd { get; private set; }

        public ICommand GotoProjectVersionCmd { get; private set; }
        public ICommand SaveProjectToServerCmd { get; private set; }

        public ICommand CopySelectedComponentCmd { get; private set; }
        public ICommand DeleteSelectedComponentCmd { get; private set; }
        public ICommand DeleteMarkedComponentsCmd { get; private set; }

        // COMPONENTS: parameters and calculations
        public ICommand GeneratePointerParamsForParamCmd { get; private set; }
        public ICommand DeleteSelectedCompParameterCmd { get; private set; }
        public ICommand CopySelectedCompParameterCmd { get; private set; }
        public ICommand SaveCurrentValuesAsDefaultCmd { get; private set; }
        public ICommand DeleteSelectedCompCalculationCmd { get; private set; }
        public ICommand PropagateOneAddedParamToCopiesCmd { get; private set; }

        // COMPONENTS: display
        public ICommand ExpandAllComponentsCmd { get; private set; }
        public ICommand ExpandSelectedCompCmd { get; private set; }
        public ICommand CollapseAllComponentsCmd { get; private set; }
        public ICommand DisplayComponentsAsGraphCmd { get; private set; }
        public ICommand SelectComponentInGraphCmd { get; private set; }
        public ICommand MarkComponentsAll { get; private set; }
        public ICommand UnmarkComponentsAll { get; private set; }
        public ICommand MarkSelectedCompWRefsCmd { get; private set; }
        public ICommand TextSearchComponentFindCmd { get; private set; }

        public ICommand UserSwitchComponentsAll { get; private set; }
        public ICommand SaveAllComponentVisSettingsCmd { get; private set; }
        public ICommand ImportComponentVisSettingsCmd { get; private set; }

        // COMPONENT: Comparison
        public ICommand CompareComponentsCmd { get; private set; }
        // COMPONENT: Mapping btw Components
        public ICommand MapComp2CompCmd { get; private set; }
        public ICommand CalculateMappingForSelectedCmd { get; private set; }

        // COMPONENT NETWORKS
        public ICommand CreateNewFlNetworkCmd { get; private set; }
        public ICommand DisplayNetworksAsGraphCmd { get; private set; }
        public ICommand DeleteMarkedNetworksCmd { get; private set; }
        public ICommand CopySelectedNetworkCmd { get; private set; }

        public ICommand MarkNetworksAllCmd { get; private set; }
        public ICommand UnmarkNetworksAllCmd { get; private set; }

        // COMPONENT MANAGEMENT: Supervize, Publish
        public ICommand CompSupervizeOKCmd { get; private set; }
        public ICommand CompPublishOKCmd { get; private set; }

        // OTHER APPLICATIONS
        public ICommand StartGeometryViewerCmd { get; private set; }
        public ICommand StartGeometryViewerWOUpdatesCmd { get; private set; }
        public ICommand SendComponentToGeomViewerEditCmd { get; private set; }
        public ICommand SendComponentToGeomViewerUpdateCmd { get; private set; }

        private NotifyingBool comm_can_send_next_msg;
        private Queue<string> messages_to_send;
        private string message_last_sent;

        private List<ComponentMessage> unprocessed_incoming_messages;
        private CommMessageType CU_alternative_action_to_take;
        
        // communication with Git Repository
        public bool GitRepoConfigOK { get; private set; }
        private string git_path_to_config_file = @"_config_files\git_repo_config.txt";
        private int git_nr_required_entries = 3;

        private string git_config_msg_short;
        private string git_config_msg_long;

        // repository at:
        // http://128.130.183.105:7990
        // login data:
        // 01. berghamster / berghamster
        // 02. galina.paskaleva / berghamster

        // internal repo data
        private string repo = @"C:\Users\friedeggs\Documents\galrep2";
        private string account_user_name = @"galina.paskaleva";
        private string account_password = @"berghamster";

        private int git_max_nr_versions = 100;
        private string git_file_marker = @"[F]";
        private string git_comp_marker = @"[C]";
        private string git_user_marker = @"[U]";
        private string git_nw_marker = @"[N]";
        private string git_comp_changes = string.Empty;
        private string git_nw_changes = string.Empty;

        private string git_last_comp_file_openend;
        private string git_last_mv_file_openend;
        private string git_last_commit_key_loaded;

        private UIElements.GitVersionsWindow git_version_win;

        public ICommand GetGitRepoStatusCmd { get; private set; }
        private System.Timers.Timer git_repo_status_check_timer;

        // web-service connection
        public ICommand MapToWebServiceCmd { get; private set; }

        #endregion

        #region .CTOR

        public AppViewModel()
        {
            // connection to git repository
            this.ReadGitConfigFile();

            // call the user profile window
            this.SetUserProfile();

            // logger
            this.LoggingService = new LocalLogger(ComponentUtils.ComponentManagerTypeToDescrDE(this.UserRole));
            this.LoggingService.PropertyChanged += LoggerSerivce_PropertyChanged;
            this.LoggingService.LogInfo("...");

            // inter-process communications
            this.CU_Logger = new LocalLogger(ComponentUtils.ComponentManagerTypeToDescrDE(this.UserRole));
            this.CU_Logger.PropertyChanged += CU_Logger_PropertyChanged;
            this.CommUnit = new CommDuplex("CB", this.CU_client_name, this.CU_server_name, this.CU_authentication, this.CU_Logger);
            this.CommUnit.AnswerRequestHandler = this.AnswerRequestFromGeomView;
            // the CommUnit receives requests over the Property CurrentInput
            this.CommUnitRunning = false;

            // data managers
            this.COMPFactoryProp = new ComponentFactory(this.UserRole);
            this.COMPFactory.PropertyChanged += COMPFactory_PropertyChanged;
            this.CALCFactory = new CalculationFactory();
            this.PFactory = new ParameterFactory();
            this.MVFactory = new MultiValueFactory();
            this.IRManager = new ImageRecordManager();

            // COMMANDS: general
            this.mode_prev = GuiModus.NEUTRAL;
            this.modes_prev = new Stack<GuiModus>();
            this.modes_prev.Push(GuiModus.NEUTRAL);
            this.SwitchGuiModeCmd = new RelayCommand((x) => OnSwitchMode(x));
            this.ShowReservedParamNamesCmd = new RelayCommand((x) => OnShowReservedParamNames());

            // COMMANDS: log entries
            this.SaveLogEntriesCmd = new RelayCommand((x) => this.LoggingService.SaveLogToFile("Komponenten-Baukasten"));

            // COMMANDS: symbols      
            this.ImportImageFromPNGCmd = new RelayCommand((x) => OnImportImageFromPNG(),
                                               (x) => CanExecute_OnImportImageFromPNG());
            this.ImportImagesFromBinaryFileCmd = new RelayCommand((x) => OnImportImagesFromBinaryFile(),
                                                       (x) => CanExecute_OnImportImagesFromBinaryFile());
            this.SaveImagesToBinaryFileCmd = new RelayCommand((x) => OnSaveImagesToBinaryFile(),
                                                   (x) => CanExecute_OnSaveImagesToBinaryFile());
            this.DeleteSelectedImageCmd = new RelayCommand((x) => 
            { 
                this.IRManager.RemoveRecord(this.SelectedSymbol); 
                this.ExistingSymbols = new List<ImageRecord>(this.IRManager.ImagesForDisplay); 
            },
            (x) => this.IRManager != null && this.SelectedSymbol != null);
            this.ApplyImagesToComponentsCmd = new RelayCommand((x) => this.SetAllComponentSymbols(),
                                                               (x) => this.IRManager != null && this.IRManager.ImagesForDisplay.Count > 1 && 
                                                                      this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0);

            // COMMANDS: values
            this.DefineNewValueCmd = new RelayCommand((x) => OnDefineNewValue());
            this.DefineNewFunctCmd = new RelayCommand((x) => OnDefineNewFunction());
            this.DeleteSelectedValueCmd = new RelayCommand((x) => OnDeleteSelectedValue(), 
                                                (x) => CanExecute_OnDeleteSelectedValue());
            this.DeleteAllValuesCmd = new RelayCommand((x) => OnDeleteAllValues(),
                                                       (x) => this.MVFactory != null && this.MVFactory.ValueRecord.Count > 0);
            this.CopySelectedValueCmd = new RelayCommand((x) => OnCopySelectedValue(),
                                              (x) => CanExecute_OnCopySelectedValue());
            this.EditSelectedValueCmd = new RelayCommand((x) => OnEditSelectedValue(),
                                    (x) => CanExecute_ManipulateSelectedValue());
            this.ShowSelectedValueCmd = new RelayCommand((x) => OnShowSelectedValue(),
                                                (x) => CanExecute_ShowSelectedValue());

            this.SaveAllValuesCmd = new RelayCommand((x) => SaveAllValues(), (x) => CanExecute_SaveAllValues());
            this.ImportValuesFromDXFCmd = new RelayCommand((x) => ImportValuesFromDXF(),
                                                (x) => CanExecute_ImportValuesFromDXF());

            this.ImportEXCELValueTableCmd = new RelayCommand((x) => OnImportEXCELValueTable());
            this.ImportEXCELValueTableNamedRowsCmd = new RelayCommand((x) => OnImportEXCELValueTable(true));

            // COMMANDS: parameters
            this.DecoupleFromFalueFieldCmd = new RelayCommand((x) => OnDecoupleFromFalueField(),
                                                   (x) => CanExecute_OnDecoupleFromFalueField());
            this.DeleteSelectedParameterCmd = new RelayCommand((x) => OnDeleteSelectedParameter(),
                                                    (x) => CanExecute_OnDeleteSelectedParameter());
            this.DeleteAllParametersCmd = new RelayCommand((x) => OnDeleteAllParameters(),
                                                           (x) => this.PFactory != null && this.PFactory.ParameterRecord.Count > 0);
            this.CopySelectedParameterCmd = new RelayCommand((x) => OnCopySelectedParameter(),
                                                  (x) => CanExecute_OnCopySelectedParameter());
            this.SaveAllParametersCmd = new RelayCommand((x) => SaveAllParameters(),
                                              (x) => CanExecute_SaveAllParameters());
            this.ImportParametersFromDXFCmd = new RelayCommand((x) => ImportParametersFromDXF(),
                                                    (x) => CanExecute_ImportParametersFromDXF());

            // COMMANDS: calculations
            this.calc_param_symb_CURRENT = string.Empty;

            this.InputParamSelectionCmd = new RelayCommand((x) => OnInputParamSelection(x),
                                                (x) => CanExecute_OnInputParamSelection());
            this.OutputParamSelectionCmd = new RelayCommand((x) => OnOutputParamSelection(x),
                                                  (x) => CanExecute_OnInputParamSelection());
            this.PerformSelectedCalculationCmd = new RelayCommand((x) => OnPerformSelectedCalculation(),
                                                       (x) => CanExecute_OnPerformSelectedCalculation());
            
            this.InputParamSelection_CalcInComp_Cmd = new RelayCommand((x) => OnInputParamSelection_CalcInComp(x),
                                                            (x) => CanExecute_OnInputParamSelection_CalcInComp());
            this.OutputParamSelection_CalcInComp_Cmd = new RelayCommand((x) => OnOutputParamSelection_CalcInComp(x),
                                                             (x) => CanExecute_OnInputParamSelection_CalcInComp());
            this.PerformSelectedCalculation_CalcInComp_Cmd = new RelayCommand((x) => OnPerformSelectedCalculation_CalcInComp(),
                                                                   (x) => CanExecute_OnPerformSelectedCalculation_CalcInComp());

            this.DeleteSelectedCalculationCmd = new RelayCommand((x) => OnDeleteSelectedCalulation(),
                                                      (x) => CanExecute_OnDeleteSelectedCalulation());
            this.CopySelectedCalculationCmd = new RelayCommand((x) => OnCopySelectedCalculation(),
                                                   (x) => CanExecute_OnDeleteSelectedCalulation());

            // COMMANDS: components
            this.ExistingFunctionSlots = new List<SelectableString>(ComponentUtils.COMP_SLOTS_ALL_SELECTABLE);
            this.CompParameterSelectionCmd = new RelayCommand((x) => OnCompParameterSelection(),
                                                   (x) => CanExecute_OnCompParameterSelection());
            this.CompAssignSlotsCmd = new RelayCommand((x) => OnCompAssignSlots(),
                                            (x) => CanExecute_OnCompAssignSlots());
            
            this.comp_selecting_subcomponent = true;
            this.comp_selecting_slot_name = string.Empty;
            this.CompSubcompSelectionCmd = new RelayCommand((x) => OnCompSubcompSelection(x),
                                                    (x) => CanExecute_OnCompCompSelection());
            this.CompRefCompSelectionCmd = new RelayCommand((x) => OnCompRefCompSelection(x),
                                                    (x) => CanExecute_OnCompCompSelection());
            this.CompRefCompHighlightCmd = new RelayCommand((x) => OnCompSelectionExternal(x),
                                                     (x) => CanExecute_OnCompCompSelection());
            this.CompPerformCalculationChainCmd = new RelayCommand((x) => OnCompPerformCalculationChain(),
                                                        (x) => CanExecute_OnCompPerformCalculationChain());
            this.CompPerformAllCalculationChainsCmd = new RelayCommand((x) => { this.SelectedComponent.EvaluateAllMappings(); this.SelectedComponent.ExecuteAllCalculationChains(); },
                                                                       (x) => this.SelectedComponent != null && !this.SelectedComponent.IsLocked);

            this.SaveAllComponentsCmd = new RelayCommand((x) => SaveAllComponents(),
                                              (x) => CanExecute_SaveAllComponents());
            this.ImportAllComponentsFromDXFCmd = new RelayCommand((x) => ImportComponentsFromDXF(),
                                                       (x) => CanExecute_ImportComponentsFromDXF());
            this.ImportComponentFromDXFCmd = new RelayCommand((x) => OnImportComponentFromDXF(),
                                                   (x) => CanExecute_OnImportComponentFromDXF());
            this.ImportComponentsFromDXFAddingCmd = new RelayCommand((x) => this.OnImportComponentFromDXFAdding(),
                                                               (x) => CanExecute_OnImportComponentFromDXFAdding());

            this.SaveComponentProjectToDXFCmd = new RelayCommand((x) => OnSaveComponentProjectToDXF(),
                                                      (x) => CanExecute_OnSaveComponentProjectToDXF());
            this.SaveComponentProjectAsToDXFCmd = new RelayCommand((x) => OnSaveComponentProjectAsToDXF(),
                                                        (x) => CanExecute_OnSaveComponentProjectAsToDXF());
            this.OpenComponentProjectFromDXFCmd = new RelayCommand((x) => OnOpenComponentProjectFromDXF(),
                                                        (x) => CanExecute_OnOpenComponentProjectFromDXF());
            this.CloseComponentProjectCmd = new RelayCommand((x) => OnCloseComponentProject(),
                                                  (x) => CanExecute_OnCloseComponentProject());
            this.RepareComponentPojectCmd = new RelayCommand((x) => OnRepareComponentPoject(),
                                                             (x) => this.UserRole == ComponentManagerType.ADMINISTRATOR);

            this.GotoProjectVersionCmd = new RelayCommand((x) => OnGotoProjectVersion(),
                                               (x) => CanExecute_OnGotoProjectVersion());
            this.SaveProjectToServerCmd = new RelayCommand((x) => OnSaveProjectToServer(),
                                                (x) => CanExecute_OnSaveProjectToServer());

            this.CopySelectedComponentCmd = new RelayCommand((x) => OnCompCopy(),
                                                  (x) => CanExecute_OnCompCopy());
            this.DeleteSelectedComponentCmd = new RelayCommand((x) => OnCompDelete(),
                                                      (x) => CanExecute_OnCompCopy());
            this.DeleteMarkedComponentsCmd = new RelayCommand((x) => OnDeleteCompMarked(),
                                                   (x) => CanExecute_OnDeleteCompMarked());

            // COMMANDS: components (parameters and calculations)
            this.GeneratePointerParamsForParamCmd = new RelayCommand((x) => OnGeneratePointerParamsForParam(),
                                                          (x) => CanExecute_OnGeneratePointerParamsForParam());
            this.DeleteSelectedCompParameterCmd = new RelayCommand((x) => OnDeleteCompParameter(),
                                                        (x) => CanExecute_OnDeleteCompParameter());
            this.CopySelectedCompParameterCmd = new RelayCommand((x) => OnCopyCompParameter(),
                                                    (x) => CanExecute_OnDeleteCompParameter());
            this.SaveCurrentValuesAsDefaultCmd = new RelayCommand((x) => this.SelectedComponent.SaveNewDefaultValues(),
                                                                  (x) => this.SelectedComponent != null);

            this.DeleteSelectedCompCalculationCmd = new RelayCommand((x) => OnDelecteCompCalculation(),
                                                          (x) => CanExecute_OnDelecteCompCalculation());
            this.PropagateOneAddedParamToCopiesCmd = new RelayCommand((x) => OnPropagateOneAddedParamToCopies(),
                                                                      (x) => CanExecute_OnPropagateOneAddedParamToCopies());

            // COMMANDS: Display
            this.ExpandAllComponentsCmd = new RelayCommand((x) => OnExpandAllComponents());
            this.ExpandSelectedCompCmd = new RelayCommand((x) => OnExpandSelectedComp(),
                                               (x) => CanExecute_OnExpandSelectedComp()); 
            this.CollapseAllComponentsCmd = new RelayCommand((x) => OnCollapseAllComponents());
            this.DisplayComponentsAsGraphCmd = new RelayCommand((x) => OnDisplayComponentsAsGraph(),
                                                     (x) => CanExecute_OnDisplayComponentsAsGraph());

            this.SelectComponentInGraphCmd = new RelayCommand((x) => OnSelectComponentInGraph(),
                                                  (x) => CanExecute_OnSelectComponentInGraph());

            this.MarkComponentsAll = new RelayCommand((x) => this.COMPFactory.MarkAll(true),
                                                      (x) => this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0);
            this.UnmarkComponentsAll = new RelayCommand((x) => this.COMPFactory.MarkAll(false),
                                                        (x) => this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0);
            this.MarkSelectedCompWRefsCmd = new RelayCommand((x) => this.COMPFactory.MarkWReferences(this.SelectedComponent),
                                                             (x) => this.COMPFactory != null && this.SelectedComponent != null);
            this.TextSearchComponentFindCmd = new RelayCommand((x) => OnTextSearchComponentFind(x),
                (x) => this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 1);

            this.UserSwitchComponentsAll = new RelayCommand((x) => this.COMPFactory.UserSwitchAll((bool)x),
                                                            (x) => this.COMPFactory != null && (x is bool));
            this.SaveAllComponentVisSettingsCmd = new RelayCommand((x) => SaveAllComponentVisSettings(),
                                                        (x) => CanExecute_SaveAllComponentVisSettings());
            this.ImportComponentVisSettingsCmd = new RelayCommand((x) => ImportComponentVisSettings(),
                                                       (x) => CanExecute_ImportComponentVisSettings());

            // COMMANDS: Compare Components
            this.CompareComponentsCmd = new RelayCommand((x) => OnCompareComponents(),
                                              (x) => CanExecute_OnCompareComponents());
            // COMANDS: Map Component to another Component
            this.MapComp2CompCmd = new RelayCommand((x) => this.OnMapComp2Comp(),
                                         (x) => this.CanExecute_OnMapComp2Comp());
            this.CalculateMappingForSelectedCmd = new RelayCommand((x) => this.OnCalculateMappingForSelected(),
                                                             (x) => CanExecute_OnCalculateMappingForSelected());

            // COMMANDS: Component Networks
            this.ExistingManagersTypes = new List<string>(ComponentUtils.MANAGER_TYPES_STRING);
            this.CreateNewFlNetworkCmd = new RelayCommand((x) => OnCreateNewFlNetwork(), (x) => this.COMPFactory != null);
            this.DisplayNetworksAsGraphCmd = new RelayCommand((x) => OnDisplayNetworksAsGraph(), 
                                                              (x) => this.COMPFactory != null && this.SelectedNetwork != null && !this.SelectedNetwork.IsLocked);
            this.DeleteMarkedNetworksCmd = new RelayCommand((x) => OnDeleteNetwMarked(),
                                                 (x) => CanExecute_OnDeleteNetwMarked());
            this.CopySelectedNetworkCmd = new RelayCommand((x) => OnNetworkCopy(x), (x) => CanExecute_OnNetworkCopy());

            this.MarkNetworksAllCmd = new RelayCommand((x) => this.COMPFactory.MarkAllNW(true),
                                                       (x) => this.COMPFactory != null && this.COMPFactory.NetworkRecord.Count > 0);
            this.UnmarkNetworksAllCmd = new RelayCommand((x) => this.COMPFactory.MarkAllNW(false),
                                                       (x) => this.COMPFactory != null && this.COMPFactory.NetworkRecord.Count > 0);

            // COMMANDS: Component Management - Supervize, Publish
            this.CompSupervizeOKCmd = new RelayCommand((x) => OnCompSupervizeOK(),
                                            (x) => CanExecute_OnCompSupervizeOK());
            this.CompPublishOKCmd = new RelayCommand((x) => OnCompPublishOK(),
                                          (x) => CanExecute_OnCompPublishOK());

            // COMMANDS: other Applications
            this.StartGeometryViewerCmd = new RelayCommand((x) => OnStartGeometryViewer(true), (x) => CanExecute_OnStartGeometryViewer());
            this.StartGeometryViewerWOUpdatesCmd = new RelayCommand((x) => OnStartGeometryViewer(false), (x) => CanExecute_OnStartGeometryViewer());
            this.SendComponentToGeomViewerEditCmd = new RelayCommand((x) => OnSendComponentToGeometryViewer(true), 
                                                          (x) => CanExecute_OnSendComponentToGeometryViewer());
            this.SendComponentToGeomViewerUpdateCmd = new RelayCommand((x) => OnSendComponentToGeometryViewer(false),
                                                          (x) => CanExecute_OnSendComponentToGeometryViewer());
            
            this.comm_can_send_next_msg = new NotifyingBool(false);
            this.comm_can_send_next_msg.ReturnToTrueAfterTimeOut = true;
            this.comm_can_send_next_msg.PropertyChanged += comm_can_send_next_msg_PropertyChanged;
            this.messages_to_send = new Queue<string>();

            // COMMANDS: GIT
            this.GetGitRepoStatusCmd = new RelayCommand((x) => OnGetGitRepoStatus(), (x) => this.GitRepoConfigOK && !string.IsNullOrEmpty(this.repo));
            this.git_repo_status_check_timer = new System.Timers.Timer(60000); // 1m = 60 s = 60000 ms
            this.git_repo_status_check_timer.AutoReset = true;
            this.git_repo_status_check_timer.Elapsed += git_repo_status_check_timer_Elapsed;
            this.git_repo_status_check_timer.Start();


            // COMMANDS: Web-Services
            this.MapToWebServiceCmd = new RelayCommand((x) => this.MapToWebService(),
                                                   (x) => CanExecute_MapToWebService());

            // lists
            this.ExistingParameters = new List<Parameter>(this.PFactory.ParameterRecord);
            this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
        }

        #endregion

        #region COMMANDS: Create, Delete, Copy, Edit, Show Values

        private void OnDefineNewValue()
        {
            // open mask for creating value
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    mw.OpenCreateValueWindow(ref this.MVFactory);
                    this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_VALUE_NEW_T);
                }
            }
        }

        private void OnDefineNewFunction()
        {
            // open mask for creating value
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    mw.OpenCreateValueFunctionWindow(ref this.MVFactory);
                    this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_VALUE_NEW_F);
                }
            }
        }

        private void OnEditSelectedValue()
        {
            // open mask for creating value
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    MultiValueTable mvt = this.SelectedValue as MultiValueTable;
                    MultiValueFunction mvf = this.SelectedValue as MultiValueFunction;
                    if (mvt != null)
                    {
                        mw.OpenEditValueTableWindow(ref this.MVFactory, mvt);
                        LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_VALUE_EDIT_T + mvt.ToString());
                    }
                    else if (mvf != null)
                    {
                        mw.OpenEditValueFunctionWindow(ref this.MVFactory, mvf);
                        LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_VALUE_EDIT_F + mvf.ToString());
                    }

                    this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
                }
            }
        }

        private void OnShowSelectedValue()
        {
            // open mask for creating value
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    MultiValueTable mvt = this.SelectedValue as MultiValueTable;
                    MultiValueFunction mvf = this.SelectedValue as MultiValueFunction;
                    MultiValueBigTable mvBT = this.SelectedValue as MultiValueBigTable;
                    if (mvt != null)
                        mw.OpenShowValueWindow(mvt);
                    else if (mvf != null)
                        mw.OpenShowFunctionWindow(mvf);
                    else if (mvBT != null)
                        mw.OpenShowBigTableWindow(mvBT);

                    this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
                }
            }
        }

        private bool CanExecute_ManipulateSelectedValue()
        {
            if (this.SelectedValue == null) return false;

            MultiValueTable mvt = this.SelectedValue as MultiValueTable;
            MultiValueFunction mvf = this.SelectedValue as MultiValueFunction;
            return (mvt != null || mvf != null);
        }

        private bool CanExecute_ShowSelectedValue()
        {
            if (this.SelectedValue == null) return false;

            MultiValueTable mvt = this.SelectedValue as MultiValueTable;
            MultiValueFunction mvf = this.SelectedValue as MultiValueFunction;
            MultiValueBigTable mvBT = this.SelectedValue as MultiValueBigTable;
            return (mvt != null || mvf != null || mvBT != null);
        }


        private void OnDeleteSelectedValue()
        {
            bool success = this.MVFactory.DeleteRecord(this.SelectedValue.MVID);
            if (success)
            {
                this.SelectedValue = null;
                this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_VALUE_DEL_OK);
            }
            else
            {
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_VALUE_DEL_NE);
            }
        }

        private bool CanExecute_OnDeleteSelectedValue()
        {
            return (this.MVFactory != null && this.SelectedValue != null);
        }

        private void OnCopySelectedValue()
        {
            MultiValue copy = this.MVFactory.CopyRecord(this.SelectedValue);
            if (copy != null)
            {
                this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
                this.SelectedValue = copy;
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_VALUE_COPY_OK);
            }
            else
            {
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_VALUE_COPY_NE);
            }
        }

        private bool CanExecute_OnCopySelectedValue()
        {
            if (this.SelectedValue == null) return false;

            // we do not copy big tables with 8760 rows imported from EXCEL !
            MultiValueBigTable mvBT = this.SelectedValue as MultiValueBigTable;
            if (mvBT != null) return false;

            return true;
        }

        private void OnDeleteAllValues()
        {
            this.MVFactory.ClearRecord();
            this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_VALUE_DEL_ALL);
        }

        #endregion

        #region COMMANDS: Parameters

        private void OnAssociatePwValueField_wP()
        {
            this.OnSwitchMode(GuiModus.PARAMETER_PICK_VALUE_FIELD);
        }

        private bool CanExecute_OnAssociatePwValueField_wP()
        {
            return (this.SelectedParameter != null && this.SelectedComponent != null && this.COMPFactory != null);
        }


        private void OnDecoupleFromFalueField()
        {
            this.SelectedParameterDummy.ValueField = null;
            this.SelectedParameterDummy.MValPointer = MultiValPointer.INVALID;
            // communicate the parameter field value to the GUI
            Window window = Application.Current.MainWindow;
            MainWindow mw = window as MainWindow;
            if (mw != null)
                mw.PickedMValue = null;
            // log: 13.09.2016
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_PARAM_DECOUPLE_OK);
        }

        private bool CanExecute_OnDecoupleFromFalueField()
        {
            if ((this.Mode == GuiModus.PARAMETER_NEW || this.Mode == GuiModus.PARAMETER_EDIT ||
                this.Mode == GuiModus.COMPONENT_EDIT_PARAMETER) &&
                 this.SelectedParameter != null && 
                 this.SelectedParameterDummy != null && 
                 this.SelectedParameterDummy.ValueField != null)
                return true;
            else
                return false;
        }

        private void OnDeleteSelectedParameter()
        {
            string param_name = this.SelectedParameter.ToInfoString();
            bool success = this.PFactory.DeleteRecord(this.SelectedParameter.ID);
            if (success)
            {
                this.SelectedParameter = null;
                this.ExistingParameters = new List<Parameter>(this.PFactory.ParameterRecord);
                // log: 13.09.2016
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_PARAM_DEL_OK + param_name);
            }
            else
            {
                // log: 13.09.2016
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_PARAM_DEL_NE + param_name);
            }
        }

        private bool CanExecute_OnDeleteSelectedParameter()
        {
            return (this.PFactory != null && this.SelectedParameter != null);
        }

        private void OnDeleteAllParameters()
        {
            this.PFactory.ClearRecord();
            this.ExistingParameters = new List<Parameter>(this.PFactory.ParameterRecord);
            // log: 13.09.2016
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_PARAM_DEL_ALL_OK);
        }

        private void OnCopySelectedParameter()
        {
            string param_name_orig = this.SelectedParameter.ToInfoString();
            
            Parameter copy = this.PFactory.CopyRecord(this.SelectedParameter);
            if (copy != null)
            {
                this.ExistingParameters = new List<Parameter>(this.PFactory.ParameterRecord);
                this.SelectedParameter = copy;
                // log: 13.09.2016
                string param_name_copy = this.SelectedParameter.ToInfoString();
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_PARAM_COPY_OK + param_name_orig + " > " + param_name_copy);
            }
            else
            {
                // log: 13.09.2016
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_PARAM_COPY_NE + param_name_orig);
            }
        }

        private bool CanExecute_OnCopySelectedParameter()
        {
            return (this.PFactory != null && this.SelectedParameter != null);
        }

        private void OnShowReservedParamNames()
        {
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    this.win_ResPNames = mw.OpenReservedParamNamesWindow(Parameter.RESERVED_NAMES);
                }
            }
        }

        #endregion

        #region COMMANDS: Calculations

        private void OnInputParamSelection(object _symbol)
        {
            if (_symbol == null) return;
            string symb_str = _symbol.ToString();
            if (string.IsNullOrEmpty(symb_str)) return;

            this.calc_param_symb_CURRENT = symb_str;
            this.OnSwitchMode(GuiModus.CALC_PICK_PARAMETER_IN);
        }

        private bool CanExecute_OnInputParamSelection()
        {
            return (this.PFactory.ParameterRecord.Count > 0);
        }

        private void OnOutputParamSelection(object _symbol)
        {
            if (_symbol == null) return;
            string symb_str = _symbol.ToString();
            if (string.IsNullOrEmpty(symb_str)) return;

            this.calc_param_symb_CURRENT = symb_str;
            this.OnSwitchMode(GuiModus.CALC_PICK_PARAMETER_OUT);
        }

        private void OnPerformSelectedCalculation()
        {
            this.SelectedCalculation.PerformCalculation();
            this.ExistingParameters = new List<Parameter>(this.PFactory.ParameterRecord);
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_CALC_CALC + this.SelectedCalculation.ToInfoString());
        }

        private bool CanExecute_OnPerformSelectedCalculation()
        {
            return (this.SelectedCalculation != null);
        }

        private void OnDeleteSelectedCalulation()
        {
            string calc_name = this.SelectedCalculation.ToInfoString();
            bool success = this.CALCFactory.RemoveCalculation(this.SelectedCalculation.ID);
            if (success)
            {
                this.ExistingCalcs = new List<Calculation>(this.CALCFactory.CalcRecord);
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_CALC_DEL_OK + calc_name);
            }
            else
            {
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_CALC_DEL_NE + calc_name);
            }
        }

        private bool CanExecute_OnDeleteSelectedCalulation()
        {
            return (this.CALCFactory != null && this.SelectedCalculation != null);
        }

        private void OnCopySelectedCalculation()
        {
            Calculation calc = this.CALCFactory.CopyCalculation(this.SelectedCalculation.ID);
            string calc_name_orig = this.SelectedCalculation.ToInfoString();            
            if (calc != null)
            {
                this.ExistingCalcs = new List<Calculation>(this.CALCFactory.CalcRecord);
                this.SelectedCalculation = calc;
                string calc_name_copy = calc.ToInfoString();
                LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_CALC_COPY_OK + calc_name_orig + " > " + calc_name_copy);
            }
            else
            {
                LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_CALC_COPY_NE + calc_name_orig);
            }
        }

        // only for calculations CONTAINED IN A COMPONENT !!!
        private void OnInputParamSelection_CalcInComp(object _symbol)
        {
            if (_symbol == null) return;
            string symb_str = _symbol.ToString();
            if (string.IsNullOrEmpty(symb_str)) return;

            this.calc_param_symb_CURRENT = symb_str;
            this.OnSwitchMode(GuiModus.COMPONENT_CALC_PICK_PARAMETER_IN);
        }

        private bool CanExecute_OnInputParamSelection_CalcInComp()
        {
            return (this.SelectedComponent != null && this.SelectedComponent.ParamChildren != null && 
                this.SelectedComponent.ParamChildren.Count > 0);
        }

        private void OnOutputParamSelection_CalcInComp(object _symbol)
        {
            if (_symbol == null) return;
            string symb_str = _symbol.ToString();
            if (string.IsNullOrEmpty(symb_str)) return;

            this.calc_param_symb_CURRENT = symb_str;
            this.OnSwitchMode(GuiModus.COMPONENT_CALC_PICK_PARAMETER_OUT);
        }

        private void OnPerformSelectedCalculation_CalcInComp()
        {
            this.SelectedCompCalculation.PerformCalculation();
            this.SelectedComponent.UpdateParamChildrenContainer();
        }

        private bool CanExecute_OnPerformSelectedCalculation_CalcInComp()
        {
            return (this.SelectedComponent != null && this.SelectedCompCalculation != null);
        }

        private void OnCompPerformCalculationChain()
        {
            this.SelectedComponent.ExecuteCalculationChain();
        }

        private bool CanExecute_OnCompPerformCalculationChain()
        {
            return (this.SelectedComponent != null && this.SelectedComponent.ContainedCalculations.Count > 0);
        }

        #endregion

        #region COMMANDS: Components

        private void OnCompParameterSelection()
        {
            this.OnSwitchMode(GuiModus.COMPONENT_PICK_PARAMETER);
        }

        private bool CanExecute_OnCompParameterSelection()
        {
            return (this.PFactory != null && this.PFactory.ParameterRecord.Count > 0 &&
                    this.COMPFactory != null);
        }

        private void OnCompAssignSlots()
        {
            this.SelectedComponent.FitsInSlots = new List<string>(this.ExistingFunctionSlots.Where(x => x.IsSelected == true).Select(x => x.ObjectData));
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_ASSIGN_SLOT + this.SelectedComponent.ToInfoString());
            if (this.SelectedComponent.FitsInSlots.Count > 0)
            {
                string all_asigned_slots = this.SelectedComponent.FitsInSlots.Aggregate((x, y) => y + ", " + x);
                this.RecordChangeForGit(this.SelectedComponent, "was assigned slots: " + all_asigned_slots);
            }
            else
            {
                this.RecordChangeForGit(this.SelectedComponent, "was assigned no slots.");
            }
        }

        private bool CanExecute_OnCompAssignSlots()
        {
            return (this.SelectedComponent != null);
        }

        private void OnCompSubcompSelection(object _slot)
        {
            if (_slot == null) return;
            this.comp_selecting_slot_name = _slot.ToString();
            if (string.IsNullOrEmpty(this.comp_selecting_slot_name)) return;

            this.OnSwitchMode(GuiModus.COMPONENT_PICK_COMPONENT);
            this.comp_selecting_subcomponent = true;
        }

        private void OnCompRefCompSelection(object _slot)
        {
            if (_slot == null) return;
            this.comp_selecting_slot_name = _slot.ToString();
            if (string.IsNullOrEmpty(this.comp_selecting_slot_name)) return;

            this.OnSwitchMode(GuiModus.COMPONENT_PICK_COMPONENT);
            this.comp_selecting_subcomponent = false;
        }

        private bool CanExecute_OnCompCompSelection()
        {
            return (this.COMPFactory.ComponentRecord.Count > 1);
        }

        // used to highlight referenced components
        private void OnCompSelectionExternal(object _id_as_obj)
        {
            if (_id_as_obj == null) return;
            if (!(_id_as_obj is long)) return;

            long id = (long)_id_as_obj;
            this.SelectedComponent = this.COMPFactory.SelectComponent(id);

            // exit component edit mode: added 18.10.2016
            this.OnSwitchMode(GuiModus.COMPONENT_EDIT);
        }

        // works only on unattached components (cannot copy a subcomponent)
        private void OnCompCopy()
        {
            string comp_name_orig = this.SelectedComponent.ToInfoString();
            ParameterStructure.Component.Component copy = this.COMPFactory.CopyUnassignedComponent(this.SelectedComponent);
            this.SelectedComponent = null;
            //this.ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
            this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
            if (copy != null)
            {
                this.SelectedComponent = this.COMPFactory.SelectComponent(copy.ID);
                this.SetComponentSymbol(this.SelectedComponent);

                string comp_name_copy = copy.ToInfoString();
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_COPY_OK + comp_name_orig + " > " + comp_name_copy);
                this.RecordChangeForGit(copy, "copied from " + comp_name_orig);
            }
            else
            {
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_COMP_COPY_NE + comp_name_orig);
            }
        }

        private bool CanExecute_OnCompCopy()
        {
            return (this.COMPFactory != null && this.SelectedComponent != null && !this.SelectedComponent.IsLocked);
            // condition
            // this.COMPFactory.GetParentComponent(this.SelectedComponent) == null
            // removed 27.09.2017 - no reason to restric copying to top-level components
        }

        // deletes ONLY the SELECTED component
        private void OnCompDelete()
        {
            MessageBoxResult result = MessageBox.Show("Möchten Sie die Komponente wirklich entfernen?", "Komponente entfernen", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) return;

            this.RecordChangeForGit(this.SelectedComponent, "was deleted");
            string comp_name = (this.SelectedComponent == null) ? "-" : this.SelectedComponent.ToInfoString();
            
            this.COMPFactory.RemoveComponent(this.SelectedComponent, true);
            this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);

            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_DEL_OK + comp_name);
        }

        // deletes ONLY marked components on the highest hierarchy level
        private void OnDeleteCompMarked()
        {
            MessageBoxResult result = MessageBox.Show("Möchten Sie die markierten Komponenten wirklich entfernen?", "Komponenten entfernen", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) return;

            bool success = this.COMPFactory.RemoveMarkedComponents(); // excludes locked ones
            this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_DEL_M);
        }

        private bool CanExecute_OnDeleteCompMarked()
        {
            return (this.COMPFactory != null && this.COMPFactory.GetNrMarkedUnlockedRecords() > 0);
        }

        #endregion

        #region COMMANDS: Component Search

        private void OnTextSearchComponentFind(object _obj)
        {
            if (_obj == null)
                return;

            string searchText = _obj.ToString();
            if (string.IsNullOrEmpty(searchText))
                return;

            this.COMPFactory.ProcessSearch(searchText);
        }

        #endregion

        #region COMMANDS: Component Parameters and Calculations

        private void OnGeneratePointerParamsForParam()
        {
            Parameter pX, pY, pZ, pS;
            this.PFactory.CreatePointerParameters(this.SelectedParameter, out pX, out pY, out pZ, out pS);
            if (pX != null || pY != null || pZ != null || pS != null)
            {
                string p_names = (pX != null) ? pX.Name + " " : string.Empty;
                p_names += (pY != null) ? pY.ToInfoString() + " " : string.Empty;
                p_names += (pZ != null) ? pZ.ToInfoString() + " " : string.Empty;
                p_names += (pS != null) ? pS.ToInfoString() + " " : string.Empty;
                this.RecordChangeForGit(this.SelectedComponent, "added parameters " + p_names);
            }
            // add to the selected component
            if (pX != null)
            {
                this.SelectedComponent.AddParameter(pX, this.UserRole);
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_OK +
                    this.SelectedParameter.ToInfoString() + " > " + pX.ToInfoString());
            }
            else
            {
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_NE + this.SelectedParameter.ToInfoString());
            }
            if (pY != null)
            {
                this.SelectedComponent.AddParameter(pY, this.UserRole);
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_OK +
                    this.SelectedParameter.ToInfoString() + " > " + pY.ToInfoString());
            }
            else
            {
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_NE + this.SelectedParameter.ToInfoString());
            }
            if (pZ != null)
            {
                this.SelectedComponent.AddParameter(pZ, this.UserRole);
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_OK +
                    this.SelectedParameter.ToInfoString() + " > " + pZ.ToInfoString());
            }
            else
            {
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_NE + this.SelectedParameter.ToInfoString());
            }
            if (pS != null)
            {
                this.SelectedComponent.AddParameter(pS, this.UserRole);
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_OK +
                    this.SelectedParameter.ToInfoString() + " > " + pS.ToInfoString());
            }
            else
            {
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_NE + this.SelectedParameter.ToInfoString());
            }
        }

        private bool CanExecute_OnGeneratePointerParamsForParam()
        {
            return (this.COMPFactory != null && this.SelectedComponent != null && 
                    this.PFactory != null && this.SelectedParameter != null && this.SelectedParameter.ValueField != null &&
                    this.Mode == GuiModus.COMPONENT_EDIT_PARAMETER);
        }

        private void OnDeleteCompParameter()
        {
            ParameterDeleteResult result = this.COMPFactory.RemoveParameterFromComponent(this.SelectedParameter, this.SelectedComponent);
            string p_name = (this.SelectedParameter != null) ? this.SelectedParameter.ToInfoString() : "-";
            if (result == ParameterDeleteResult.ERR_BOUND_IN_CALC)
            {
                MessageBox.Show("Der Parameter ist gebunden in einer Gleichung!\nEntfernen oder modifizieren Sie die Gleichung zuesrt.", "Parameter Entfernen", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoggingService.LogWarning("Der Komponenten-Parameter ist gebunden in einer Gleichung!");
            }
            else if (result == ParameterDeleteResult.ERR_NOT_FOUND)
            {
                MessageBox.Show("Der Parameter konnte nicht gefunden werden...", "Parameter Entfernen", MessageBoxButton.OK, MessageBoxImage.Error);
                LoggingService.LogError("Der Komponenten-Parameter konnte nicht gefunden werden...");
            }
            else
            {
                this.RecordChangeForGit(this.SelectedComponent, "removed parameter " + p_name);
            }
        }

        private bool CanExecute_OnDeleteCompParameter()
        {
            return (this.COMPFactory != null && this.SelectedComponent != null && this.SelectedParameter != null &&
                    this.Mode == GuiModus.COMPONENT_EDIT_PARAMETER);
        }

        private void OnCopyCompParameter()
        {
            string param_name = (this.SelectedParameter == null) ? "-" : this.SelectedParameter.ToInfoString();
            this.SelectedComponent.CopyParameterWithinComponent(this.SelectedParameter, this.UserRole);
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_COPY + param_name);
            this.RecordChangeForGit(this.SelectedComponent, "copied parameter " + param_name);
        }

        private void OnDelecteCompCalculation()
        {
            string calc_name = (this.SelectedCompCalculation == null) ? "-" : this.SelectedCompCalculation.ToInfoString();
            bool removed = this.SelectedComponent.RemoveCalculation(this.SelectedCompCalculation, this.UserRole);
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_CALC_DEL + calc_name);
            this.RecordChangeForGit(this.SelectedComponent, "removed calculation " + calc_name);
        }

        private bool CanExecute_OnDelecteCompCalculation()
        {
            return (this.COMPFactory != null && this.SelectedComponent != null && this.SelectedCompCalculation != null &&
                    this.Mode == GuiModus.COMPONENT_EDIT_CALC);
        }

        private void OnPropagateOneAddedParamToCopies()
        {
            int nr_changed = this.COMPFactory.PropagateOneAddedParameter(this.SelectedComponent);
            MessageBox.Show(nr_changed + " Komponenten wurden angepasst.", "Hinzugefügten Komponentenparameter fortpflanzen", MessageBoxButton.OK, MessageBoxImage.Information);
            this.LoggingService.LogInfo("Hinzugefügten Komponentenparameter fortpflanzen... " + nr_changed + " angepasst.");
            this.RecordChangeForGit(this.SelectedComponent, "propagated param to " + nr_changed + " other components");
        }

        private bool CanExecute_OnPropagateOneAddedParamToCopies()
        {
            return (this.COMPFactory != null && this.SelectedComponent != null && !this.SelectedComponent.IsLocked &&
                this.ExistingComponents.Count > 1);
        }

        #endregion

        #region COMMANDS: Component Comparison

        private void OnCompareComponents()
        {
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    this.win_CCompare = mw.OpenCompareComponentsWindow(this.UserRole);
                    this.win_CCompare.PropertyChanged += win_CCompare_PropertyChanged;
                }
            }
        }

        private void win_CCompare_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.win_CCompare == null || e == null) return;

            if (((e.PropertyName == "IsPickingC1" && this.win_CCompare.IsPickingC1) ||
                 (e.PropertyName == "IsPickingC2" && this.win_CCompare.IsPickingC2)) &&
                this.COMPFactory != null && this.ExistingComponents.Count > 0)
            {
                // turn mode on
                this.OnSwitchMode(GuiModus.COMP_COMPARER_PICK_COMP);
            }
        }

        private bool CanExecute_OnCompareComponents()
        {
            return (this.COMPFactory != null && this.ExistingComponents.Count > 1);
        }

        #endregion

        #region COMMANDS: Component -> Component Mapping

        private void OnMapComp2Comp()
        {
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    this.win_comp2compM = mw.OpenComp2CompMappingWindow();
                    this.win_comp2compM.PropertyChanged += win_comp2compM_PropertyChanged;
                }
            }
        }

        private void win_comp2compM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.win_comp2compM == null || e == null) return;

            if (e.PropertyName == "IsPickingCompData" || e.PropertyName == "IsPickingCompCalculator")
            {
                if ((this.win_comp2compM.IsPickingCompData || this.win_comp2compM.IsPickingCompCalculator) && this.COMPFactory != null && this.ExistingComponents.Count > 1)
                    this.OnSwitchMode(GuiModus.COMP_TO_COMP_MAPPER_PICK_COMP);
            }
        }

        private bool CanExecute_OnMapComp2Comp()
        {
            return (this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 1);
        }

        private void OnCalculateMappingForSelected()
        {
            foreach(Mapping2Component map in this.SelectedComponent.Mappings2Comps)
            {
                this.SelectedComponent.EvaluateMapping(map);
            }
        }

        private bool CanExecute_OnCalculateMappingForSelected()
        {
            if (this.SelectedComponent == null) return false;
            if (this.SelectedComponent.Mappings2Comps.Count == 0) return false;

            return true;
        }

        #endregion

        #region COMMANDS: Component Networks

        private void OnCreateNewFlNetwork()
        {
            FlowNetwork created = this.COMPFactory.CreateEmptyNetwork(this.UserRole);

            this.ExistingNetworks = new List<FlowNetwork>(this.COMPFactory.NetworkRecord);
            this.SelectedNetwork = created;
            this.COMPFactory.SelectNetwork(created);

            if (created != null)
            {
                LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_NW_NEW + "{" + created.ID + "}" + created.Name);
                this.RecordChangeForGit(created, "new");
            }
        }

        // deletes ONLY marked components on the highest hierarchy level
        private void OnDeleteNetwMarked()
        {
            MessageBoxResult result = MessageBox.Show("Möchten Sie die markierten Netzwerke wirklich entfernen?", "Netzwerke entfernen", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel) return;

            if (this.win_NW_Graph != null)
                this.win_NW_Graph.Close();
            bool success = this.COMPFactory.RemoveMarkedNetworks(); // excludes locked ones           
            this.ExistingNetworks = new List<FlowNetwork>(this.COMPFactory.NetworkRecord);
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_NW_DEL_M);
        }

        private bool CanExecute_OnDeleteNetwMarked()
        {
            return (this.COMPFactory != null && this.COMPFactory.GerNrMarkedUnlockedNetworks() > 0);
        }

        private void OnNetworkCopy(object _in_place)
        {
            bool copy_in_place = true;
            if (_in_place != null && _in_place is bool)
                copy_in_place = (bool)_in_place;

            string nw_name = (this.SelectedNetwork == null) ? "-" : "{" + this.SelectedNetwork.ID + "}" + this.SelectedNetwork.Name;
            long id_copy = this.COMPFactory.CopyNetwork(this.SelectedNetwork, copy_in_place);
            this.SelectedNetwork = null;
            if (id_copy >= 0)
            {
                this.ExistingNetworks = new List<FlowNetwork>(this.COMPFactory.NetworkRecord);
                this.SelectedNetwork = this.COMPFactory.SelectNetwork(id_copy);
                LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_NW_COPY_OK + nw_name + " > {" + id_copy.ToString() + "}");
                this.RecordChangeForGit(this.SelectedNetwork, "new");
            }
            else
            {
                LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_NW_COPY_NE + nw_name);
            }

        }

        private bool CanExecute_OnNetworkCopy()
        {
            return (this.COMPFactory != null && this.SelectedNetwork != null && !this.SelectedNetwork.IsLocked);
        }

        #endregion

        #region COMMANDS: Component Management (Supervize, Publish)

        private void OnCompSupervizeOK()
        {
            MessageBoxResult dlg = MessageBox.Show("Soll die Komponente wirklich freigegeben werden?", "Komponente freigeben", MessageBoxButton.YesNo);
            if (dlg == MessageBoxResult.Yes)
            {
                ComponentAccessTracker tracker = this.SelectedComponent.AccessLocal[this.UserRole];
                tracker.LastAccess_Supervize = DateTime.Now;
                this.SelectedComponent.AccessLocal[this.UserRole] = tracker;
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_SUPERV + this.SelectedComponent.ToInfoString());
                this.RecordChangeForGit(this.SelectedComponent, "supervize OK");
            }
        }

        private bool CanExecute_OnCompSupervizeOK()
        {
            if (this.SelectedComponent == null) return false;

            ComponentAccessTracker tracker = this.SelectedComponent.AccessLocal[this.UserRole];
            if (!(tracker.AccessTypeFlags.HasFlag(ComponentAccessType.SUPERVIZE))) return false;

            return true;
        }

        private void OnCompPublishOK()
        {
            MessageBoxResult dlg = MessageBox.Show("Soll die Komponente wirklich publiziert werden?", "Komponente publizieren", MessageBoxButton.YesNo);
            if (dlg == MessageBoxResult.Yes)
            {
                ComponentAccessTracker tracker = this.SelectedComponent.AccessLocal[this.UserRole];
                tracker.LastAccess_Release = DateTime.Now;
                this.SelectedComponent.AccessLocal[this.UserRole] = tracker;
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_RELEASE + this.SelectedComponent.ToInfoString());
                this.RecordChangeForGit(this.SelectedComponent, "publish OK");
            }
        }

        private bool CanExecute_OnCompPublishOK()
        {
            if (this.SelectedComponent == null) return false;

            ComponentAccessTracker tracker = this.SelectedComponent.AccessLocal[this.UserRole];
            if (!(tracker.AccessTypeFlags.HasFlag(ComponentAccessType.RELEASE))) return false;

            return true;
        }

        #endregion

        #region COMMANDS: PNG Import / Export Images

        private void OnImportImageFromPNG()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "png files|*.png"
                };
                
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        // build a new image record
                        this.IRManager.AddRecord(dlg.FileName);
                        this.ExistingSymbols = new List<ImageRecord>(this.IRManager.ImagesForDisplay);
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_IMG_OK + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PNG Image Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "PNG Image Value Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_IMG_NE + ex.Message);
            }
        }

        private bool CanExecute_OnImportImageFromPNG()
        {
            return (this.IRManager != null);
        }

        private void OnImportImagesFromBinaryFile()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "binary files|*.bin"
                };

                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        // build a new image record
                        this.IRManager.LoadRecordsFromFile(dlg.FileName);
                        this.ExistingSymbols = new List<ImageRecord>(this.IRManager.ImagesForDisplay);
                        this.SetAllComponentSymbols();
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_IMGS_OK + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Binary Image Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Binary Image Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_IMGS_NE + ex.Message);
            }

        }

        private bool CanExecute_OnImportImagesFromBinaryFile()
        {
            return (this.IRManager != null && this.IRManager.ImagesForDisplay.Count == 1);
        }

        private void OnSaveImagesToBinaryFile()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = "Symbols", // Default file name
                    DefaultExt = ".bin", // Default file extension
                    Filter = "binary files|*.bin" // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // create the export string
                    this.IRManager.SaveRecordToFile(dlg.FileName);
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_IMGS_OK + dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Symbols", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving Symbols", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_OUT_IMGS_NE + ex.Message);
            }
        }

        private bool CanExecute_OnSaveImagesToBinaryFile()
        {
            return (this.IRManager != null && this.IRManager.ImagesForDisplay.Count > 1);
        }

        #endregion

        #region COMMANDS: DXF Import / Export Values

        private void SaveAllValues()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = MultiValueFactory.VALUE_RECORD_FILE_NAME, // Default file name
                    DefaultExt = "." + ParamStructFileExtensions.FILE_EXT_MULTIVALUES, // Default file extension
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_MULTIVALUES // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // create the export string
                    StringBuilder export = this.MVFactory.ExportRecord(true);
                    string content = export.ToString();
                    using (FileStream fs = File.Create(dlg.FileName))
                    {
                        byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                        fs.Write(content_B, 0, content_B.Length);
                    }
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_MV_OK + dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Values", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving Values", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_OUT_MV_NE + ex.Message);
            }
        }

        private bool CanExecute_SaveAllValues()
        {
            return (this.MVFactory != null) && (this.MVFactory.ValueRecord.Count > 0);
        }

        private void ImportValuesFromDXF()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_MULTIVALUES
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        //imports the DXF file
                        DXFDecoder dxf_decoder = new DXFDecoder(this.MVFactory);
                        dxf_decoder.LoadFromFile(dlg.FileName);
                        this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_MV_OK + dlg.FileName);
                        this.git_last_mv_file_openend = dlg.FileName;
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "DXF Value Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "DXF Value Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_MV_NE + ex.Message);
            }

        }

        private bool CanExecute_ImportValuesFromDXF()
        {
            return (this.MVFactory.ValueRecord.Count == 0);
        }

        #endregion

        #region COMMANDS: DXF Import / Export Parameters

        private void SaveAllParameters()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = ParameterFactory.PARAMETER_RECORD_FILE_NAME, // Default file name
                    DefaultExt = "." + ParamStructFileExtensions.FILE_EXT_PARAMETERS, // Default file extension
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_PARAMETERS // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // create the export string
                    StringBuilder export = this.PFactory.ExportRecord(true);
                    string content = export.ToString();
                    using (FileStream fs = File.Create(dlg.FileName))
                    {
                        byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                        fs.Write(content_B, 0, content_B.Length);
                    }
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_P_OK + dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Parameters", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving Parameters", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_OUT_P_NE + ex.Message);
            }
        }

        private bool CanExecute_SaveAllParameters()
        {
            return (this.MVFactory != null && this.PFactory != null) && (this.PFactory.ParameterRecord.Count > 0);
        }

        private void ImportParametersFromDXF()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_PARAMETERS
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        //imports the DXF file
                        DXFDecoder dxf_decoder = new DXFDecoder(this.MVFactory, this.PFactory);
                        dxf_decoder.LoadFromFile(dlg.FileName);
                        this.ExistingParameters = new List<Parameter>(this.PFactory.ParameterRecord);
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_P_OK + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "DXF Parameter Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "DXF Parameter Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_P_NE + ex.Message);
            }

        }

        private bool CanExecute_ImportParametersFromDXF()
        {
            return (this.MVFactory.ValueRecord.Count > 0 && this.PFactory.ParameterRecord.Count == 0);
        }

        #endregion

        #region COMMANDS: DXF Import / Export Components

        // saves all marked components
        private void SaveAllComponents()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = ComponentFactory.COMPONENT_RECORD_FILE_NAME, // Default file name
                    DefaultExt = "." + ParamStructFileExtensions.FILE_EXT_COMPONENTS, // Default file extension
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_COMPONENTS // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // create the export string
                    StringBuilder export = this.COMPFactory.ExportRecord(true, true);
                    string content = export.ToString();
                    using (FileStream fs = File.Create(dlg.FileName))
                    {
                        byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                        fs.Write(content_B, 0, content_B.Length);
                    }
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_COMP_OK + dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Components", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving Components", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_OUT_COMP_NE + ex.Message);
            }
        }

        private bool CanExecute_SaveAllComponents()
        {
            return (this.MVFactory != null && this.COMPFactory != null && this.COMPFactory.GetNrMarkedRecords() > 0);
        }

        private void ImportComponentsFromDXF()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_COMPONENTS
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        //imports the DXF file
                        DXFDecoder dxf_decoder = new DXFDecoder(this.MVFactory, this.PFactory, this.COMPFactory);
                        dxf_decoder.LoadFromFile(dlg.FileName);
                        this.COMPFactory.RestoreReferencesWithinRecord();
                        this.COMPFactory.MakeParameterOutsideBoundsVisible();
                        //this.ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
                        this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
                        this.ExistingNetworks = new List<FlowNetwork>(this.COMPFactory.NetworkRecord);
                        this.SetAllComponentSymbols();
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_COMPS_OK + dlg.FileName);
                        // this.git_last_comp_file_openend = dlg.FileName; // commented 08.09.2017
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DXF Component Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "DXF Component Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_COMPS_NE + ex.Message);
            }

        }

        private bool CanExecute_ImportComponentsFromDXF()
        {
            return (this.MVFactory.ValueRecord.Count > 0 && this.COMPFactory.ComponentRecord.Count == 0);
        }

        private void OnImportComponentFromDXF()
        {
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    List<ParameterStructure.Component.Component> to_import = mw.OpenImportDxfCompsWindow();
                    this.COMPFactory.AddToRecord(to_import);
                    this.SetComponentSymbols(to_import);
                    this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_COMP_OK);
                }
            }
        }

        private bool CanExecute_OnImportComponentFromDXF()
        {
            return (this.MVFactory != null && this.MVFactory.ValueRecord.Count > 0);
        }

        private void OnImportComponentFromDXFAdding()
        {
            // 1. Step: tell the user which MV-Record is currently loaded
            string mv_record_info = "Derzeit geladene MultiValues:\n";
            foreach(MultiValue mv in this.ExistingValues)
            {
                mv_record_info += mv.MVID + ": " + mv.MVName + " of type " + mv.MVType + "\n";
            }
            mv_record_info += "Verknüpfungen der Komponentenparameter mit MultiValues werden in der o.g. Liste gesucht.\n";
            mv_record_info += "Soll das Komponentenfile importiert werden?";
            MessageBoxResult result = MessageBox.Show(mv_record_info, "Derzeit geladene MultiValues", MessageBoxButton.OKCancel);
            
            if (result != MessageBoxResult.OK) return;

            // 2. Step: load the components to a separate Component Factory
            List<ParameterStructure.Component.Component> comps_to_merge = new List<ParameterStructure.Component.Component>();
            List<FlowNetwork> nws_to_merge = new List<FlowNetwork>();
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_COMPONENTS,
                    Title = "Importing all components and networks from file..."
                };

                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        // import the DXF file to the TEMPORARY Component Factory
                        ComponentFactory COMPFactory_tmp = new ComponentFactory(ComponentManagerType.ADMINISTRATOR);
                        ParameterFactory PFactory_tmp = new ParameterFactory();
                        DXFDecoder dxf_decoder = new DXFDecoder(this.MVFactory, PFactory_tmp, COMPFactory_tmp);
                        dxf_decoder.LoadFromFile(dlg.FileName);
                        COMPFactory_tmp.RestoreReferencesWithinRecord();

                        // NO COPYING - we take the originals for merging!
                        comps_to_merge = new List<ParameterStructure.Component.Component>(COMPFactory_tmp.ComponentRecord);
                        nws_to_merge = new List<FlowNetwork>(COMPFactory_tmp.NetworkRecord);
                        // git is blind to file imports
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DXF Component File Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "DXF Component File Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_COMPS_NWS_NE + ex.Message);
            }

            // 3. Step: merge records
            this.COMPFactory.AddToRecord(comps_to_merge);
            this.SetComponentSymbols(comps_to_merge);
            this.COMPFactory.AddToRecord(nws_to_merge);
            this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
            this.ExistingNetworks = new List<FlowNetwork>(this.COMPFactory.NetworkRecord);
            
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_COMPS_NWS_OK);
        }

        private bool CanExecute_OnImportComponentFromDXFAdding()
        {
            return (this.MVFactory.ValueRecord.Count > 0);
        }

        #endregion

        #region COMMANDS: DXF Import / Export Components : Distributed

        private void OnOpenComponentProjectFromDXF()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "simultan files|*." + ParamStructFileExtensions.FILE_EXT_PROJECT
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {

                    this.ProjectImporter = new DXFDistributedDecoder(this.MVFactory, this.PFactory, this.COMPFactory,
                                                                                    dlg.FileName, this.UserRole);
                    this.ProjectImporter.LoadFiles();
                    //this.ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
                    this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
                    this.ExistingNetworks = new List<FlowNetwork>(this.COMPFactory.NetworkRecord);
                    this.SetAllComponentSymbols();
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_PROJ_OK + dlg.FileName);
                    this.git_last_comp_file_openend = dlg.FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Opening Component Project", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Opening Component Project", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_PROJ_NE + ex.Message);
            }
        }

        private bool CanExecute_OnOpenComponentProjectFromDXF()
        {
            return (this.MVFactory != null && this.PFactory != null && this.COMPFactory != null &&
                    this.MVFactory.ValueRecord.Count > 0 &&
                    this.COMPFactory.ComponentRecord.Count == 0);
        }


        private void OnSaveComponentProjectToDXF()
        {
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_PROJ);
            this.ProjectImporter.SaveFiles();
        }

        private bool CanExecute_OnSaveComponentProjectToDXF()
        {
            return (this.ProjectImporter != null &&
                    this.MVFactory != null && this.PFactory != null && this.COMPFactory != null &&
                    this.COMPFactory.ComponentRecord.Count > 0);
        }

        private void OnSaveComponentProjectAsToDXF()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = ComponentFactory.COMPONENT_RECORD_FILE_NAME, // Default file name
                    DefaultExt = "." + ParamStructFileExtensions.FILE_EXT_PROJECT, // Default file extension
                    Filter = "simultan files|*." + ParamStructFileExtensions.FILE_EXT_PROJECT // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_PROJ_AS + dlg.FileName);
                    if (this.ProjectImporter != null)
                        this.ProjectImporter.SaveFilesAs(dlg.FileName);
                    else
                    {
                        DXFDistributedDecoder tmp = new DXFDistributedDecoder(this.MVFactory, this.PFactory, this.COMPFactory, 
                                                                                null, this.UserRole);
                        tmp.SaveFilesAs(dlg.FileName);
                    }
                }    
            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.Message, "Error Saving Component Project", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageBox.Show(ex.StackTrace, "Error Saving Component Project", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError("Error Saving Component Project:\n" + ex.StackTrace);
            }
        }

        private bool CanExecute_OnSaveComponentProjectAsToDXF()
        {
            return (this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0);
        }

        private void OnCloseComponentProject()
        {
            try
            {
                // ask user about saving file
                MessageBoxResult dlg = MessageBox.Show("Soll das Projekt gespeichert werden?", "Projekt Schliessen", MessageBoxButton.YesNo);
                string comment = (dlg == MessageBoxResult.Yes) ? "und gespeichert." : "ohne Speichern.";
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_PROJ_CL + comment);
                
                if (dlg == MessageBoxResult.Yes)
                    this.ProjectImporter.ReleaseFiles(true);     
                else
                    this.ProjectImporter.ReleaseFiles(false);               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Closing Component Project", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Closing Component Project", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //this.ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
            this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
            this.ExistingNetworks = new List<FlowNetwork>(this.COMPFactory.NetworkRecord);
            this.ProjectImporter = null;
        }

        private bool CanExecute_OnCloseComponentProject()
        {
            return (this.ProjectImporter != null);
        }

        private void OnRepareComponentPoject()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "simultan files|*." + ParamStructFileExtensions.FILE_EXT_PROJECT
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_PROJ_UNLOCK);
                    DXFDistributedDecoder tmp_importer = new DXFDistributedDecoder(this.MVFactory, this.PFactory, this.COMPFactory, 
                                                                                    dlg.FileName, this.UserRole);

                    tmp_importer.RepareSummaryFile();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Reparing Component Project", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Reparing Component Project", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        #endregion

        #region COMMANDS: DXF Import / Export via GIT to Server

        #region GENERAL HELPER METHODS -OK-
        private void ReadGitConfigFile()
        {
            bool tmp_git_repo_config_OK = false;
            GitCommunicationUtils.ReadGitConfigFile(this.git_path_to_config_file, this.git_nr_required_entries,
                                                    out this.repo, out this.account_user_name, out this.account_password,
                                                    out tmp_git_repo_config_OK, out this.git_config_msg_long, out this.git_config_msg_short);
            this.GitRepoConfigOK = tmp_git_repo_config_OK;
        }

        private void RecordChangeForGit(ParameterStructure.Component.Component _comp, string _action)
        {
            if (!this.GitRepoConfigOK) return;
            if (_comp == null) return;

            this.git_comp_changes += this.git_comp_marker + 
                                    _comp.ToInfoString() + ": " + _action + 
                                     this.git_comp_marker;
        }

        private void RecordChangeForGit(FlowNetwork _nw, string _action)
        {
            if (!this.GitRepoConfigOK) return;
            if (_nw == null) return;

            this.git_nw_changes += this.git_nw_marker +
                                   _nw.ID + " " + _nw.Name + ": " + _action +
                                   this.git_nw_marker;
        }


        private void OnGetGitRepoStatus()
        {
            try
            {
                MyGit gitTalker = new MyGit();
                string repo_remote_status = gitTalker.GetStatusOfRemoteRepo(this.repo, this.account_user_name, this.account_password);
                string repo_status = gitTalker.GetSatusOfRepo(this.repo);
                //MessageBox.Show(repo_status, "GIT REPO Staus", MessageBoxButton.OK, MessageBoxImage.Information);
                this.GITNrStepsBehindNewest = gitTalker.GetNrStepsBehindRemoteBranch(this.repo);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error retrieving GIT REPO staus", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        private void git_repo_status_check_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.GitRepoConfigOK && !string.IsNullOrEmpty(this.git_last_comp_file_openend))
                if (this.GetGitRepoStatusCmd.CanExecute(null))
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.GetGitRepoStatusCmd.Execute(null);
                    }));
                }
        }

        #endregion

        #region VERSION SWITCH HELPER METHODS

        private bool FileOverrideFromRepoDialog(out List<string> file_paths)
        {
            file_paths = new List<string>();

            if (!string.IsNullOrEmpty(this.git_last_comp_file_openend))
            {
                // see what other files are in the current directory
                string current_dir;
                string current_file_name;
                StringHandling.GetUnQualifiedFileName(this.git_last_comp_file_openend, out current_dir, out current_file_name);
                if (Directory.Exists(current_dir))
                {
                    string[] files_in_current_dir = Directory.GetFiles(current_dir, "*", SearchOption.AllDirectories);
                    string override_msg = "Files that may be overridden are:\n";
                    if (files_in_current_dir != null && files_in_current_dir.Length > 0)
                    {
                        string file_list = files_in_current_dir.Aggregate((x, y) => x + "\n" + y);
                        override_msg += file_list;
                        file_paths = files_in_current_dir.ToList();

                        MessageBoxResult result_1 = MessageBox.Show(override_msg, "Files to override", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result_1 == MessageBoxResult.No)
                            return false;
                        else
                            return true;
                    }
                }
            }

            return false;
        }

        private void ReloadAllFromFiles(string _path_to_mv, string _path_to_comps)
        {
            try
            {
                if (!string.IsNullOrEmpty(_path_to_mv))
                {
                    this.MVFactory.ClearRecord();

                    // import the value file
                    DXFDecoder dxf_decoder_mv = new DXFDecoder(this.MVFactory);
                    dxf_decoder_mv.LoadFromFile(_path_to_mv);
                    this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_MV_OK + _path_to_mv);
                    this.git_last_mv_file_openend = _path_to_mv;
                }

                if (!string.IsNullOrEmpty(_path_to_comps))
                {
                    this.COMPFactory.ClearRecord();

                    //open the PROJECT files
                    this.ProjectImporter = new DXFDistributedDecoder(this.MVFactory, this.PFactory, this.COMPFactory,
                                                                                    _path_to_comps, this.UserRole);
                    this.ProjectImporter.LoadFiles(false);

                    this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
                    this.ExistingNetworks = new List<FlowNetwork>(this.COMPFactory.NetworkRecord);
                    this.SetAllComponentSymbols();
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_PROJ_OK + _path_to_comps);
                    this.git_last_comp_file_openend = _path_to_comps;
                }
                
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error reading files from current directory.", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError("Error reading files from current directory: " + ex.Message);
            }
        }


        #endregion

        private void OnGotoProjectVersion()
        {
            // 1. check, if the user means to overwrite the files in the current directory
            List<string> file_paths = new List<string>();
            bool override_files = this.FileOverrideFromRepoDialog(out file_paths);
            if (!override_files)
                return;

            // start communication with the repository
            string err_advice = "The repository configuration information in \"_config_files\\git_repo_config.txt\" may be incorrect,\n or there may be a problem with the bitbucket server.";
            err_advice += "\nContact your system administrator.";

            List<TalkGit.CommitRecord> all_records = new List<CommitRecord>();
            try
            {
                // 2. perform a pull to get the newest version from the remote repository to the local one
                MyGit gitTalker = new MyGit();
                gitTalker.Pull(this.repo, this.account_user_name, this.account_password);

                // 3. get all commits info from the local repository
                all_records = gitTalker.RetrieveVersionList(this.repo, this.git_max_nr_versions);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + err_advice, "Error retrieving commits from Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError("Error retrieving commits from Repository: " + ex.Message);
            }             

            // 4. translate the commits for display
            List<UIElements.CommitInfo> all_infos = new List<UIElements.CommitInfo>();
            foreach(TalkGit.CommitRecord record in all_records)
            {
                string file_name = StringHandling.ExtractStringBtw(record.MsgLoong, this.git_file_marker);

                string change_log = string.Empty;
                string[] changes = record.MsgLoong.Split(new string[] { this.git_comp_marker, this.git_nw_marker, this.git_user_marker }, StringSplitOptions.RemoveEmptyEntries);
                if (changes != null && changes.Length > 1)
                {
                    for(int i = 1; i < changes.Length; i++)
                    {
                        change_log += changes[i];
                        if (i < changes.Length - 1)
                            change_log += "\n";
                    }
                }
                bool is_current_version = (!string.IsNullOrEmpty(this.git_last_commit_key_loaded) && this.git_last_commit_key_loaded == record.Key);

                all_infos.Add(new UIElements.CommitInfo(record.Key, file_name, change_log, record.TimeStamp, record.AuthorName, is_current_version));
            }
            
            if (all_infos.Count > 0)
            {
                // 5. display the commits info and choose a version to return to
                this.git_version_win = new UIElements.GitVersionsWindow();
                this.git_version_win.RecordsToShow = all_infos;
                this.git_version_win.ShowDialog();

                if (string.IsNullOrEmpty(this.git_version_win.KeyToReturnTo))
                    return;

                this.git_last_commit_key_loaded = this.git_version_win.KeyToReturnTo;               

                // 6. attempt restoring the version to the local repo
                try
                {
                    MyGit gitTalker = new MyGit();
                    gitTalker.GoToVersion(this.git_last_commit_key_loaded, this.repo);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + err_advice, "Error recovering version from Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.LoggingService.LogError("Error recovering version from Repository: " + ex.Message);
                }

                // 7. discover which files to copy and which to delete
                string err_msg = string.Empty;
                string project_file_counterpart_path = GitCommunicationUtils.FindBestMatchForFileAtSource(this.repo, this.git_last_comp_file_openend, out err_msg);
                if (string.IsNullOrEmpty(project_file_counterpart_path))
                    return;

                DXFDistributedDecoder tmp_proj_loader = new DXFDistributedDecoder(this.MVFactory, this.PFactory, this.COMPFactory,
                                                                project_file_counterpart_path, ComponentManagerType.ADMINISTRATOR);
                List<string> single_file_paths = tmp_proj_loader.GetSingleFilePaths();
                single_file_paths.Add(project_file_counterpart_path);

                // 8. overwrite files in current folder with the files in the local repo
                List<string> file_ext_to_copy = new List<string>
                {
                    "*." + ParamStructFileExtensions.FILE_EXT_MULTIVALUES,
                    "*." + ParamStructFileExtensions.FILE_EXT_PARAMETERS,
                    "*.geodxf"
                };
                GitCommunicationUtils.ReplaceFilesAtTargetFromSource(this.repo, single_file_paths, this.git_last_comp_file_openend,
                                                                            "*." + ParamStructFileExtensions.FILE_EXT_COMPONENTS, file_ext_to_copy, out err_msg);

                // 9. reload MV and component files in the GUI
                this.ReloadAllFromFiles(this.git_last_mv_file_openend, this.git_last_comp_file_openend);

                // 10. update user display of repo status
                this.OnGetGitRepoStatus();
            }
        }        


        private bool CanExecute_OnGotoProjectVersion()
        {
            return (this.GitRepoConfigOK && !string.IsNullOrEmpty(this.git_last_comp_file_openend) && 
                this.COMPFactory != null && this.PFactory != null && this.MVFactory != null);
        }

        private void OnSaveProjectToServer()
        {
            string filename_for_error_handling = string.Empty;
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = ComponentFactory.COMPONENT_RECORD_FILE_NAME, // Default file name
                    DefaultExt = "." + ParamStructFileExtensions.FILE_EXT_PROJECT, // Default file extension
                    Filter = "simultan files|*." + ParamStructFileExtensions.FILE_EXT_PROJECT, // Filter files by extension                    
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    filename_for_error_handling = dlg.FileName;
                    // ------------------------------------------------------------------ //
                    // 1. save the PROJECT files
                    this.ProjectImporter.ReleaseWatcher(); // not necessary when using GIT version control
                    this.ProjectImporter.SaveFiles(true);

                    // 2. copy them to the local repository folder
                    List<string> file_paths = new List<string>(this.ProjectImporter.SingleFileNames);
                    file_paths.Add(this.ProjectImporter.SummaryFileName);
                    GitCommunicationUtils.CopyFilesToLocalLocation(this.repo, file_paths);

                    // extract the project file name and dir
                    string dir = "";
                    string dir_last_comp = "";
                    string filename = "";
                    StringHandling.GetUnQualifiedFileName(this.ProjectImporter.SummaryFileName, out dir, out filename, out dir_last_comp);

                    // ------------------------------------------------------------------ //
                    // 3. save the MULTIVALUE file in the same folder as well, if possible
                    if (!string.IsNullOrEmpty(this.git_last_mv_file_openend))
                    {
                        string mv_dir = "";
                        string mv_dir_last_comp = "";
                        string mv_filename = "";
                        StringHandling.GetUnQualifiedFileName(this.git_last_mv_file_openend, out mv_dir, out mv_filename, out mv_dir_last_comp);

                        StringBuilder export_MV = this.MVFactory.ExportRecord(true);
                        string content_MV = export_MV.ToString();
                        GitCommunicationUtils.SaveAndCopyFileToLocalLocation(this.repo, Path.Combine(dir, dir_last_comp, mv_filename), content_MV);
                    }

                    // ------------------------------------------------------------------ //
                    // 4. perform a PULL (in case someone else has made changes in the meantime -> 
                    // gets the newest versions and overwrites the old ones)
                    // since we are working with a project file and there is only ONE representative of each role (e.g. ONE ARCHITECT) ->
                    // only the summary files of the project need to be merged
                    MyGit gitTalker = new MyGit();
                    gitTalker.Pull(this.repo, this.account_user_name, this.account_password);
                    // get the pulled project
                    DXFDistributedDecoder tmp_pulled_project_importer = new DXFDistributedDecoder(this.MVFactory, this.PFactory, this.COMPFactory, 
                                                                        Path.Combine(this.repo, dir_last_comp, filename), ComponentManagerType.ADMINISTRATOR);
                    // 5. merge with the current project
                    ProjectMergeResult merge_result = DXFDistributedDecoder.MergeSummaryFiles(this.ProjectImporter, tmp_pulled_project_importer);
                    // copy the project files to the local repo again
                    file_paths = new List<string>(this.ProjectImporter.SingleFileNames);
                    file_paths.Add(this.ProjectImporter.SummaryFileName);
                    GitCommunicationUtils.CopyFilesToLocalLocation(this.repo, file_paths);
                    
                    // WE ARE READY FOR COMMIT AND PUSH

                    // ------------------------------------------------------------------ //
                    // 6. get the user-generated commit message
                    UIElements.CommitMsgWindow msg_window = new UIElements.CommitMsgWindow();
                    msg_window.AuthorOfMsg = ComponentUtils.ComponentManagerTypeToDescrDE(this.UserRole);
                    msg_window.ShowDialog();
                    string user_comment = this.git_user_marker + msg_window.Message + this.git_user_marker;

                    // gather commit info
                    string commit_msg = "Changed file: " + this.git_file_marker + filename + this.git_file_marker;
                    commit_msg += user_comment;
                    commit_msg += "\nLog: " + this.git_comp_changes + this.git_nw_changes;
                    string author = ComponentUtils.ComponentManagerTypeToDescrDE(this.UserRole);
                    string author_eMail = ComponentUtils.ComponentManagerTypeToAbbrevEN(this.UserRole) + @"@simultan.ac.at";

                    // ------------------------------------------------------------------ //
                    // 7. attempt to save to server
                    
                    // the parameters userName and passWord refer to the BITBUCKET user account!!!
                    // a save works if there was a change in at least one file
                    string filename_for_repo = string.IsNullOrEmpty(dir_last_comp) ? filename : Path.Combine(dir_last_comp, filename);
                    string new_commit_key = string.Empty;
                    string save_result = gitTalker.Save(this.repo, filename_for_repo, author, author_eMail, DateTime.Now, commit_msg, this.account_user_name, this.account_password, false,
                        out new_commit_key);
                    if (!string.IsNullOrEmpty(new_commit_key))
                        this.git_last_commit_key_loaded = new_commit_key;

                    if (save_result == "ok")
                    {
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_TO_SERVER_COMP_OK + dlg.FileName + "->" + save_result);
                        this.git_comp_changes = string.Empty; // reset change record
                        this.git_nw_changes = string.Empty;

                        // 8. reload the project to see all new changes (added 11.09.2017)
                        this.ReloadAllFromFiles(this.git_last_mv_file_openend, this.git_last_comp_file_openend);
                    }
                    else
                    {
                        this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_OUT_TO_SERVER_COMP_NE + save_result);
                        MessageBox.Show(save_result, "Error Saving Components to Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // List<Process> locking_processes = LockedFileChecker.WhoIsLocking(filename_for_error_handling);
                MessageBox.Show(ex.Message, "Error Saving Components to Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_OUT_TO_SERVER_COMP_NE + ex.Message);
            }
        }

        private bool CanExecute_OnSaveProjectToServer()
        {
            return (this.GitRepoConfigOK && this.ProjectImporter != null && 
                    this.MVFactory != null && this.COMPFactory != null && this.COMPFactory.GetNrMarkedRecords() > 0);
        }

        #endregion

        #region COMMANDS: EXCEL Import Values

        // nr of processed table rows is capped at 250!!! (24.01.2017: switched from 100 to 250)
        private void OnImportEXCELValueTable(bool _named_rows = false)
        {
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
                        //imports the EXCEL file
                        ExcelStandardImporter excelImp = new ExcelStandardImporter();
                        if (_named_rows)
                            excelImp.ImportBigTableWNamesFromFile(dlg.FileName, ref this.MVFactory, 250);
                        else
                            excelImp.ImportBigTableFromFile(dlg.FileName, ref this.MVFactory, 250);
                        this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);

                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_MV_OK + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "EXCEL Value Table Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "EXCEL Value Table Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_MV_NE + ex.Message);
            }

        }


        #endregion

        #region COMMANDS: User Profile

        private void SetUserProfile()
        {
             Window window = Application.Current.MainWindow;
             if (window != null)
             {
                 MainWindow mw = window as MainWindow;
                 if (mw != null)
                 {
                     bool user_cancelled = false;
                     this.UserRole = mw.OpenUserRolePickerWin(this.git_config_msg_short, this.git_config_msg_long, this.GitRepoConfigOK, out user_cancelled);
                     if (user_cancelled)
                         mw.Close();
                 }
             }
        }


        #endregion

        #region COMMANDS: Display

        private void OnExpandAllComponents()
        {
            if (this.COMPFactory != null)
            {
                this.SelectedComponent = null;
                this.COMPFactory.ExpandAll();
                //this.ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
                this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
            }
        }

        private void OnExpandSelectedComp()
        {
            this.COMPFactory.ExpandComp(this.SelectedComponent);
            //this.ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
            this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
        }

        private bool CanExecute_OnExpandSelectedComp()
        {
            return (this.COMPFactory != null && this.SelectedComponent != null);
        }

        private void OnCollapseAllComponents()
        {
            if (this.COMPFactory != null)
            {
                this.SelectedComponent = null;
                this.COMPFactory.CollapseAll();
                //this.ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
                this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
            }
        }

        #endregion

        #region COMANDS: Display Graph, Select in Graph

        private void OnDisplayComponentsAsGraph()
        {
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    this.win_Graph = mw.OpenComponentGraphWindow(this.COMPFactory);
                }
            }
        }

        private bool CanExecute_OnDisplayComponentsAsGraph()
        {
            return (this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0);
        }

        private void OnSelectComponentInGraph()
        {
            this.win_Graph.SelectComponentInGraph(this.SelectedComponent);
            this.win_Graph.Activate();
        }

        private bool CanExecute_OnSelectComponentInGraph()
        {
            return (this.win_Graph != null && this.SelectedComponent != null && this.COMPFactory != null);
        }


        private void OnDisplayNetworksAsGraph()
        {
            Window window = Application.Current.MainWindow;
            if (window != null)
            {
                MainWindow mw = window as MainWindow;
                if (mw != null)
                {
                    this.win_NW_Graph = mw.OpenNetworkGraphWindow(this.SelectedNetwork, this.COMPFactory);
                    this.win_NW_Graph.PropertyChanged += win_NW_Graph_PropertyChanged;
                    this.win_NW_Graph.Closing += win_NW_Graph_Closing;
                }
            }
        }

        private void win_NW_Graph_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.win_NW_Graph == null || e == null) return;

            if (e.PropertyName == "IsPickingComp" && this.win_NW_Graph.IsPickingComp &&
                this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0)
            {
                this.OnSwitchMode(GuiModus.NETWORK_ELEM_PICK_COMP);
            }
        }

        private void win_NW_Graph_Closing(object sender, CancelEventArgs e)
        {
            // safeguard - added 28.04.2017
            if (this.Mode == GuiModus.NETWORK_ELEM_PICK_COMP)
            {
                this.OnSwitchMode(GuiModus.NETWORK_ELEM_PICK_COMP);
            }
        }

        #endregion

        #region COMMANDS: Display Import / Export Settings

        private void SaveAllComponentVisSettings()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = ComponentFactory.COMPONENT_RECORD_VIS_FILE_NAME, // Default file name
                    DefaultExt = ".txt", // Default file extension
                    Filter = "text files|*.txt" // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // create the export string
                    StringBuilder export = this.COMPFactory.ExportLightbulbSettings();
                    string content = export.ToString();
                    using (FileStream fs = File.Create(dlg.FileName))
                    {
                        byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                        fs.Write(content_B, 0, content_B.Length);
                    }
                    this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_OUT_COMP_VIS_OK + dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Component Visiblility", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving Component Visiblility", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_OUT_COMP_VIS_NE + ex.Message);
            }
        }

        private bool CanExecute_SaveAllComponentVisSettings()
        {
            return (this.COMPFactory != null && this.ExistingComponents.Count > 0);
        }

        private void ImportComponentVisSettings()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "text files|*.txt"
                };
                // this causes a Securiy Exception (SQLite) if the propgram is not run as Administrator,
                // but it has no effect on the rest
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        //imports the text file
                        this.COMPFactory.ImportLightbulbSettings(dlg.FileName);
                        // this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IN_COMP_VIS_OK + dlg.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Component Visibility Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Component Visibility Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_IN_COMP_VIS_NE + ex.Message);
            }

        }

        private bool CanExecute_ImportComponentVisSettings()
        {
            return (this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0);
        }

        #endregion

        #region COMMANDS: Mode

        private void OnSwitchMode(object _to_mode)
        {
            if (_to_mode == null) return;
            string to_mode_str = _to_mode.ToString();
            if (string.IsNullOrEmpty(to_mode_str)) return;

            GuiModus to_gui_mode = GuiModusUtils.StringToGuiModus(to_mode_str);

            // returning from the old mode
            bool returning_from_submode = GuiModusUtils.IsSubMode(this.Mode);
            bool returning_from_submode_of_submode = GuiModusUtils.IsSubModeOfSubMode(this.Mode);
            // switch (this.Mode) ... if necessary

            this.mode_prev = this.Mode;
            this.Mode = GuiModusUtils.SwitchMode(ref this.modes_prev, to_gui_mode);
            this.LoggingService.LogInfo("Modus-Änderung: " + GuiModusUtils.GuiModusToString(this.Mode));

            // entering the new mode
            switch (this.Mode)
            {
                case GuiModus.NEUTRAL:
                    if (this.mode_prev == GuiModus.PARAMETER_NEW || this.mode_prev == GuiModus.PARAMETER_EDIT)
                        this.CopyParameterDataFromDummy();
                    if (this.mode_prev == GuiModus.CALC_NEW || this.mode_prev == GuiModus.CALC_EDIT)
                        this.TestAndSaveCalculation();
                    this.ResetAllSelections();
                    break;
                case GuiModus.PARAMETER_NEW:
                    if (!returning_from_submode)
                    {
                        this.SelectedValue = null;
                        // create an 'empty' parameter
                        Parameter p_new = this.PFactory.CreateParameter();
                        this.ExistingParameters = new List<Parameter>(this.PFactory.ParameterRecord);
                        this.SelectedParameter = p_new;
                    }
                    break;
                case GuiModus.PARAMETER_EDIT:
                    if (!returning_from_submode)
                    {
                        this.SelectedValue = null;
                    }
                    break;
                case GuiModus.PARAMETER_INFO:
                    this.SelectedValue = null;
                    break;
                case GuiModus.PARAMETER_PICK_VALUE_FIELD:
                    break;
                case GuiModus.CALC_NEW:
                    if (!returning_from_submode)
                    {
                        this.SelectedParameter = null;
                        Calculation c_new = this.CALCFactory.CreateEmptyCalculation();
                        ExistingCalcs = new List<Calculation>(this.CALCFactory.CalcRecord);
                        this.SelectedCalculation = c_new;
                    }
                    break;
                case GuiModus.CALC_EDIT:
                    if (!returning_from_submode)
                    {
                        this.SelectedParameter = null;
                    }
                    break;
                case GuiModus.COMPONENT_NEW:
                    if (!returning_from_submode)
                    {
                        this.SelectedComponent = null;
                        ParameterStructure.Component.Component comp_new = this.COMPFactory.CreateEmptyComponent(true);
                        ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
                        this.SelectedComponent = comp_new;
                        this.COMPFactory.SelectComponent(comp_new);
                        this.RecordChangeForGit(comp_new, "new");
                    }
                    else
                    {
                        if (this.mode_prev == GuiModus.COMPONENT_EDIT_PARAMETER)
                        {
                            this.CopyParameterDataFromDummy();
                            string p_name = (this.SelectedParameter == null) ? "-" : this.SelectedParameter.ToInfoString();
                            this.RecordChangeForGit(this.SelectedComponent, "edited param " + p_name);
                        }                            
                        if (this.mode_prev == GuiModus.COMPONENT_PICK_CALC || this.mode_prev == GuiModus.COMPONENT_EDIT_CALC)
                            this.TestAndAddCalculationToComponent();
                    }
                    break;
                case GuiModus.COMPONENT_EDIT:
                    if (!returning_from_submode)
                    {
                        this.SelectedParameter = null;
                        this.SelectedCalculation = null;
                    }
                    else
                    {
                        if (this.mode_prev == GuiModus.COMPONENT_EDIT_PARAMETER)
                        {
                            this.CopyParameterDataFromDummy();
                            string p_name = (this.SelectedParameter == null) ? "-" : this.SelectedParameter.ToInfoString();
                            this.RecordChangeForGit(this.SelectedComponent, "edited param " + p_name);
                        }
                        if (this.mode_prev == GuiModus.COMPONENT_PICK_CALC || this.mode_prev == GuiModus.COMPONENT_EDIT_CALC)
                            this.TestAndAddCalculationToComponent();
                    }
                    break;
                case GuiModus.COMPONENT_INFO:
                    break;
                case GuiModus.COMPONENT_PICK_PARAMETER:
                    break;
                case GuiModus.COMPONENT_EDIT_PARAMETER:
                    if (this.SelectedComponent != null && this.SelectedComponent.ParamChildren != null && this.SelectedComponent.ParamChildren.Count > 0)
                    {
                        if (this.mode_prev == GuiModus.PARAMETER_PICK_VALUE_FIELD)
                        {
                            // added 26.10.2016
                            this.CopyParameterDataFromDummy();
                        }
                        else
                        {
                            // added 19.08.2016
                            this.SelectedParameter = this.SelectedComponent.SelectFirstChildParameter();
                            this.CopyParamValueFieldInfoToGUI();
                        }
                    } 
                    break;
                case GuiModus.COMPONENT_PICK_CALC:
                    // creates a new calculation within a component
                    // and passes it to the GUI for editing
                    if (!returning_from_submode_of_submode)
                    {
                        this.SelectedParameter = null;
                        this.SelectedCalculation = null;
                        if (this.SelectedComponent != null)
                            this.SelectedCompCalculation = this.SelectedComponent.CreateEmptyCalculation();
                    }
                    break;
                case GuiModus.COMPONENT_EDIT_CALC:
                    // edits a calculation within a component
                    if (!returning_from_submode_of_submode)
                    {
                        this.SelectedParameter = null;
                        this.SelectedCalculation = null;
                    }
                    break;
                case GuiModus.COMPONENT_PICK_COMPONENT:
                    break;    
                case GuiModus.NETWORK_ELEM_PICK_COMP:
                    break;
                case GuiModus.COMP_COMPARER_PICK_COMP:
                    break;
                case GuiModus.COMP_TO_WS_MAPPER_PICK_COMP:
                    break;
                case GuiModus.COMP_TO_COMP_MAPPER_PICK_COMP:
                    break;
                default:
                    break;
            }

            
        }

        #endregion

        #region COMMANDS: Start Geometry Viewer, Manage Communication with it

        private void OnStartGeometryViewer(bool _send_me_updates_back)
        {
            // /ComponentBuilder;component/MainWindow.xaml
            // string path_1 = Application.Current.StartupUri.LocalPath;

            // C:\_TU\Code-Test\c#\EngineTest\Apps\ComponentBuilder\bin\Debug
            // string path_2 = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // C:\_TU\Code-Test\c#\EngineTest\Apps\ComponentBuilder\bin\Debug\
            // string path_3 = AppDomain.CurrentDomain.BaseDirectory;

            // C:\_TU\Code-Test\c#\EngineTest\Apps\ComponentBuilder\bin\Debug
            // string path_4 = System.IO.Directory.GetCurrentDirectory();

            // C:\_TU\Code-Test\c#\EngineTest\Apps\ComponentBuilder\bin\Debug
            // string path_5 = Environment.CurrentDirectory;

            // file:\C:\_TU\Code-Test\c#\EngineTest\Apps\ComponentBuilder\bin\Debug
            // string path_6 = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            // get current application path
            string path = AppDomain.CurrentDomain.BaseDirectory;
            // construct path of geometry viewer
            string[] path_comps = path.Split(new string[] { "\\", "/" }, StringSplitOptions.RemoveEmptyEntries);
            int nr_comps = path_comps.Length;
            if (nr_comps <= 3) return;

            string path_GV = string.Empty;
            for (int i = 0; i < nr_comps - 3; i++)
            {
                path_GV += path_comps[i] + "\\";
            }

            string path_CV_WD = path_GV;
#if DEBUG
            path_CV_WD += @"GeometryViewer\bin\Debug\";
            path_GV += @"GeometryViewer\bin\Debug\GeometryViewer.exe";
#else
            path_CV_WD += @"GeometryViewer\bin\Release\";
            path_GV += @"GeometryViewer\bin\Release\GeometryViewer.exe";
#endif

            // assemble the start arguments for the Geometry Viewer
            string args = ComponentUtils.ComponentManagerTypeToLetter(this.UserRole) + CommMessageUtils.STR_ARG_SEPARATOR;
            args += this.CU_client_name + CommMessageUtils.STR_ARG_SEPARATOR;
            args += this.CU_server_name + CommMessageUtils.STR_ARG_SEPARATOR;
            args += this.CU_authentication + CommMessageUtils.STR_ARG_SEPARATOR;
            args += _send_me_updates_back.ToString() + CommMessageUtils.STR_ARG_SEPARATOR;

            // start the application
            if (File.Exists(path_GV))
            {
                ProcessStartInfo psi_GV = new ProcessStartInfo();
                psi_GV.FileName = path_GV;
                psi_GV.Arguments = args;
                psi_GV.UseShellExecute = false;
                psi_GV.WorkingDirectory = path_CV_WD;
                psi_GV.WindowStyle = ProcessWindowStyle.Normal;
                psi_GV.CreateNoWindow = false;                

                //// OLD: run and wait for process to finish
                //int exit_code = -1;
                //using (Process p_GV = Process.Start(psi_GV))
                //{
                //    this.LoggingService.LogInfo("Geometry Viewer wird gestartet.");
                //    p_GV.WaitForExit();
                //    exit_code = p_GV.ExitCode;
                //}
                //if (exit_code == 0)
                //    this.LoggingService.LogInfo("Geometry Viewer wurde geschlossen.");
                //else
                //    this.LoggingService.LogWarning("Geometry Viewer hat einen Fehler verursacht!");

                // NEW: start process asynchronously
                this.LoggingService.LogInfo("Geometry Viewer wird gestartet.");
                try
                {
                    // start process
                    this.GeomViewerTask = AppViewModel.RunProcessAsynch(psi_GV);
                    this.GeomViewerTask.ContinueWith(this.CleanUpAfterGeomViewerClosed, CancellationToken.None);
                    // check if the inter-preocess communication unit exists
                    // and initialize, if it does not
                    if (this.CommUnit == null)
                    {
                        this.CommUnit = new CommDuplex("CB", this.CU_client_name, this.CU_server_name, this.CU_authentication, this.CU_Logger);
                        this.CommUnit.AnswerRequestHandler = this.AnswerRequestFromGeomView;
                        // the CommUnit receives requests over the Property CurrentInput
                    }
                    // start the inter-process communication unit
                    //Thread t_cu = new Thread(x => this.CommUnit.StartDuplex());
                    //t_cu.Start();
                    Task task_cu = new Task(() => this.CommUnit.StartDuplex());
                    task_cu.Start();
                    task_cu.ContinueWith(this.Debug, CancellationToken.None);
                    this.CommUnitRunning = true;
                }
                catch(Exception ex)
                {
                    this.LoggingService.LogError("Geometry Viewer konnte nicht gestartet werden: " + ex.Message);
                }
                          
            }

        }

        private bool CanExecute_OnStartGeometryViewer()
        {
            return (this.ExistingComponents.Count > 0 && !this.CommUnitRunning);
        }

        private static Task RunProcessAsynch(ProcessStartInfo _psi)
        {
            var tcs = new TaskCompletionSource<bool>();
            Process p = new Process
            {
                StartInfo = _psi,
                EnableRaisingEvents = true
            };

            p.Exited += (sender, args) =>
            {
                tcs.SetResult(true);
                p.Dispose();
            };

            p.Start();

            return tcs.Task;
        }

        private void CleanUpAfterGeomViewerClosed(Task _t, object _o)
        {
            // close down communication
            this.ShutDownCommUnit(true);
        }

        internal void ShutDownCommUnit(bool _closing_initiated_by_other)
        {
            // close down communication
            this.Dispatcher.Invoke(() =>
            {
                this.CommUnitRunning = false;
                this.message_last_sent = null;
                this.messages_to_send = new Queue<string>();
                this.CommFinishedSendingMessages = true;
            });
            if (this.CommUnit == null) return;

            this.LoggingService.LogAsProcess("communit {0} closing ...", this.CommUnit.Name);
            this.CommUnit.StopDuplex(_closing_initiated_by_other);
            this.CommUnit.AnswerRequestHandler -= this.AnswerRequestFromGeomView;
            this.CommUnit = null;           
        }

        private void Debug(Task _t, object _o)
        {
            
        }

        #endregion

        #region COMMANDS: Communication w GeometryViewer

        // IF _for_edit = true -> the component is shown in the GeometryViewer and can be edited
        // ELSE the GeometryViewer checks if the id of the referenced gometry is valid and updates 
        // the component without displaying it
        private void OnSendComponentToGeometryViewer(bool _for_edit)
        {
            //// OLD
            //ComponentMessage message_body = ComponentMessageTranslator.AssembleComponentMessage(this.SelectedComponent, MessagePositionInSeq.SINGLE_MESSAGE, -1L);
            //CommMessageType message_type = (_for_edit) ? CommMessageType.EDIT : CommMessageType.UPDATE;

            //string msg = CommMessageUtils.ComposeMessage(message_type, message_body.ToString());

            //this.CommUnit.CurrentUserInput = msg;


            //// NEW (03.05.2017)
            //// 1. prepare the message strings for sending
            //List<ComponentMessage> messages = ComponentMessageTranslator.AssembleMultipleComponentMessages(this.SelectedComponent);
            //CommMessageType messages_type = (_for_edit) ? CommMessageType.EDIT : CommMessageType.UPDATE;

            //this.messages_to_send = new Queue<string>();
            //foreach(ComponentMessage cmsg in messages)
            //{
            //    string msg_i = CommMessageUtils.ComposeMessage(messages_type, cmsg.ToString());
            //    messages_to_send.Enqueue(msg_i);
            //}

            //// 2. send the first string
            //// the rest will be sent in the event handler of 'comm_can_send_next_msg'
            //if (this.messages_to_send.Count > 0)
            //{
            //    this.comm_can_send_next_msg.Value_NotifyOnTrue = false;
            //    System.Diagnostics.Debug.WriteLine("CB>> sending 1st message...");                
            //    this.CommUnit.CurrentUserInput = this.messages_to_send.Dequeue();
            //}

            // NEW (04.08.2017)
            this.SendAnyComponentToGeometryViewer(new List<ParameterStructure.Component.Component> { this.SelectedComponent }, _for_edit, true);
        }

        private bool CanExecute_OnSendComponentToGeometryViewer()
        {
            return (this.CommUnit != null && !this.CommUnit.IsStopped && this.SelectedComponent != null);
        }

        // NEW 04.08.2017
        private void SendAnyComponentToGeometryViewer(List<ParameterStructure.Component.Component> _to_send, bool _for_edit, bool _discover_hierarchy)
        {
            if (_to_send == null) return;
            if (_to_send.Count == 0) return;

            // NEW (03.05.2017)
            // 2. prepare the message strings for sending
            List<ComponentMessage> all_messages = new List<ComponentMessage>();
            if (_discover_hierarchy)
            {
                // sends a COMPONENT TREE for each entry in _to_send
                // depicts the component structure, may result in multimple sequence starts and ends
                foreach (var entry in _to_send)
                {
                    List<ComponentMessage> messages = ComponentMessageTranslator.AssembleMultipleComponentMessages(entry);
                    all_messages.AddRange(messages);
                }
            }
            else
            {
                // added 08.08.2017
                // sends ONLY THE COMPONENTS in _to_send and no others (derives the parent ids from the factory)
                // to be used only for quick updates, not useful for depicting structure
                List<ComponentMessage> messages = ComponentMessageTranslator.AssembleUnrelatedComponentMessages(_to_send, this.COMPFactory);
                if (messages.Count > 0)
                    all_messages.AddRange(messages);
            }            
            
            CommMessageType messages_type = (_for_edit) ? CommMessageType.EDIT : CommMessageType.UPDATE;
            this.messages_to_send = new Queue<string>();
            foreach (ComponentMessage cmsg in all_messages)
            {
                string msg_i = CommMessageUtils.ComposeMessage(messages_type, cmsg.ToString());
                messages_to_send.Enqueue(msg_i);
            }

            // 3. send the first string
            // the rest will be sent in the event handler of 'comm_can_send_next_msg'
            if (this.messages_to_send.Count > 0)
            {
                this.CommFinishedSendingMessages = false;
                this.comm_can_send_next_msg.Value_NotifyOnTrue = false;
                System.Diagnostics.Debug.WriteLine("CB>> sending 1st message...");
                this.message_last_sent = this.messages_to_send.Dequeue();
                this.CommUnit.CurrentUserInput = this.message_last_sent;
                this.CommFinishedSendingMessages = (this.messages_to_send.Count == 0);
            }
        }

        private void OnSendComponentToGVForSelection()
        {
            if (this.CommUnit == null || this.CommUnit.IsStopped || this.SelectedComponent == null)
                return;

            ComponentMessage msg = ComponentMessage.MessageForSelection(this.SelectedComponent.ID);
            string msg_to_send = CommMessageUtils.ComposeMessage(CommMessageType.SYNCH, msg.ToString());

            this.messages_to_send = new Queue<string>();
            this.comm_can_send_next_msg.Value_NotifyOnTrue = false;
            this.CommUnit.CurrentUserInput = msg_to_send;
        }

        #endregion

        #region COMMANDS: Communication w WebServices

        private void MapToWebService()
        {
            string info = WebServiceReflector.GetAssemblyInfo(typeof(WebServiceConnector.ShadowService.Surface));
            System.Diagnostics.Debug.WriteLine("WEB SERVICE: " + info);

            List<string> types_info = WebServiceReflector.GetAsseblyTypeNames(typeof(WebServiceConnector.ShadowService.Surface));
            foreach(string ti in types_info)
            {
                System.Diagnostics.Debug.WriteLine("has Type " + ti);
            }

            List<Type> possible_webservice_entry_points = WebServiceReflector.GetServiceTypesInAssemblyOf(typeof(WebServiceConnector.ShadowService.Surface), "Serv");
            if (possible_webservice_entry_points != null && possible_webservice_entry_points.Count > 0)
            {
                List<TypeNode> rnodes = possible_webservice_entry_points.Select( x => TypeNode.CreateFor("ENTRY", x, false, "execute")).ToList();
                
                // display the caller methods for the web-services
                List<TypeNode> rnodes_return = new List<TypeNode>();
                foreach(TypeNode rtn in rnodes)
                {
                    MethodInfo mi = WebServiceReflector.GetMethodByName(rtn.ContainedType, "execute");
                    if (mi != null)
                    {
                        System.Diagnostics.Debug.WriteLine(rtn.TypeName + ": " + mi.ToString());                                          
                    }  
                }

                // display mapping window
                Window window = Application.Current.MainWindow;
                if (window != null)
                {
                    MainWindow mw = window as MainWindow;
                    if (mw != null)
                    {
                        this.win_WS = mw.OpenWebServiceMapWindow(rnodes);
                        this.win_WS.PropertyChanged += win_WS_PropertyChanged;
                    }
                }
            }   
        }

        private void win_WS_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.win_WS == null || e == null) return;

            if ((e.PropertyName == "IsPickingCompMain" && this.win_WS.IsPickingCompMain) &&
                this.COMPFactory != null && this.ExistingComponents.Count > 0)
            {
                // turn mode on
                this.OnSwitchMode(GuiModus.COMP_TO_WS_MAPPER_PICK_COMP);
            }
        }

        private bool CanExecute_MapToWebService()
        {
            //TODO...
            return true;
        }

        #endregion

        #region METHODS: Selection Handling

        #region CALCULATIONS
        private void HandleCalcSelection()
        {
            if (this.SelectedCalculation == null) return;

            switch(this.Mode)
            {
                case GuiModus.NEUTRAL:
                case GuiModus.CALC_EDIT:
                    break;
            }
        }
        #endregion

        #region SYMBOLS

        private void HandleSymbolSelection()
        {
            if (this.SelectedSymbol == null) return;

            switch(this.Mode)
            {
                case GuiModus.COMPONENT_PICK_SYMBOL:
                    if (this.SelectedComponent != null)
                    {
                        this.SelectedComponent.SymbolId = this.SelectedSymbol.ID;
                        this.SetComponentSymbol(this.SelectedComponent);
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_SYMB + this.SelectedSymbol.ID);
                        this.RecordChangeForGit(this.SelectedComponent, "received a symbol");
                    }
                    this.OnSwitchMode(GuiModus.COMPONENT_PICK_SYMBOL);
                    break;
            }
        }
        #endregion

        #region VALUE FIELDS
        private void HandleValueSelection()
        {
            if (this.SelectedValue == null) return;

            switch(this.Mode)
            {
                case GuiModus.PARAMETER_PICK_VALUE_FIELD:
                    // communicate the parameter field value to the GUI
                    Window window = Application.Current.MainWindow;
                    MainWindow mw = window as MainWindow;
                    if (mw != null)
                    {
                        mw.PickedMValue = this.SelectedValue;
                        this.picked_value_own_pointer = new MultiValPointer(this.SelectedValue.MVDisplayVector);
                        
                        string val_name = (this.SelectedValue == null) ? "-" : this.SelectedValue.ToString();
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_PARAM_COUPLE + val_name);
                    }
                    break;
                default:
                    this.picked_value_own_pointer = new MultiValPointer(this.SelectedValue.MVDisplayVector);
                    break;
            }
        }
        #endregion

        #region PARAMETER

        private void HandleParameterValueCheck(Parameter _p)
        {
            switch (this.Mode)
            {
                case GuiModus.COMPONENT_EDIT_PARAMETER:
                    if (this.SelectedParameter != null)
                        this.CopyParameterDataFromDummy();
                    break;
            }
        }

        private void HandleParameterSelection()
        {
            this.SelectedValue = null;

            switch(this.Mode)
            {
                case GuiModus.NEUTRAL:
                case GuiModus.PARAMETER_NEW:
                case GuiModus.PARAMETER_EDIT:
                case GuiModus.COMPONENT_EDIT_PARAMETER:
                    if (this.SelectedParameter != null)
                        this.SelectedParameterDummy = Parameter.CopyDataToDummy(this.SelectedParameter);
                    else
                    {
                        if (this.SelectedParameterDummy != null) // changed 30.08.2016
                            this.SelectedParameterDummy.RevertToDefault();
                    }
                
                    // communicate the parameter field value to the GUI
                    this.CopyParamValueFieldInfoToGUI(); // transferred to method: 19.08.2016
                    break;
                case GuiModus.CALC_PICK_PARAMETER_IN:
                    if (this.SelectedCalculation != null && !string.IsNullOrEmpty(this.calc_param_symb_CURRENT) &&
                        this.SelectedParameter != null)
                    {
                        if (this.SelectedParameter.Propagation != ParameterStructure.Component.InfoFlow.OUPUT)
                        {
                            if (!this.SelectedCalculation.InputParams.ContainsKey(this.calc_param_symb_CURRENT))
                                this.SelectedCalculation.InputParams.Add(this.calc_param_symb_CURRENT, this.SelectedParameter);
                            else
                                this.SelectedCalculation.InputParams[this.calc_param_symb_CURRENT] = this.SelectedParameter;
                            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_CALC_INP_OK + this.SelectedParameter.ToInfoString());
                        }
                        else
                        {
                            MessageBox.Show("Parameter ist als 'Reiner Output' definitert!", "Parameter Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                            this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_CALC_INP_NE + this.SelectedParameter.ToInfoString());
                            this.LoggingService.LogWarning("Parameter ist als 'Reiner Output' definitert!");
                        }
                    }
                    this.OnSwitchMode(GuiModus.CALC_PICK_PARAMETER_IN);
                    break;
                case GuiModus.CALC_PICK_PARAMETER_OUT:
                    if (this.SelectedCalculation != null && !string.IsNullOrEmpty(this.calc_param_symb_CURRENT) &&
                        this.SelectedParameter != null)
                    {
                        if (this.SelectedParameter.Propagation != ParameterStructure.Component.InfoFlow.INPUT &&
                            this.SelectedParameter.Propagation != ParameterStructure.Component.InfoFlow.REF_IN &&
                            this.SelectedParameter.ValueField == null)
                        {
                            if (!this.SelectedCalculation.ReturnParams.ContainsKey(this.calc_param_symb_CURRENT))
                                this.SelectedCalculation.ReturnParams.Add(this.calc_param_symb_CURRENT, this.SelectedParameter);
                            else
                                this.SelectedCalculation.ReturnParams[this.calc_param_symb_CURRENT] = this.SelectedParameter;
                            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_CALC_OUTP_OK + this.SelectedParameter.ToInfoString());
                        }
                        else
                        {
                            MessageBox.Show("Parameter muss als 'Reiner Output' oder 'In- und Output' definitert sein und kein Kennfeld referenzieren!", "Parameter Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                            this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_CALC_OUTP_NE + this.SelectedParameter.ToInfoString());
                            this.LoggingService.LogWarning("Parameter muss als 'Reiner Output' oder 'In- und Output' definitert sein und kein Kennfeld referenzieren!");
                        }
                    }
                    this.OnSwitchMode(GuiModus.CALC_PICK_PARAMETER_OUT);
                    break;
                case GuiModus.COMPONENT_PICK_PARAMETER:
                    if (this.SelectedComponent != null && this.SelectedParameter != null)
                    {
                        // copy the selected parameter (DEEP COPY)
                        // w/o adding it to the Parameter Factory Record
                        Parameter copy = this.PFactory.CopyWithoutRecord(this.SelectedParameter);
                        // add to the selected component
                        if (copy != null)
                        {
                            this.SelectedComponent.AddParameter(copy, this.UserRole);
                            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_OK + 
                                this.SelectedParameter.ToInfoString() + " > " + copy.ToInfoString());
                            this.RecordChangeForGit(this.SelectedComponent, "added parameter " + copy.ToInfoString());
                        }
                        else
                        {
                            this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_COMP_P_GET_NE + this.SelectedParameter.ToInfoString());
                        }
                    }
                    this.OnSwitchMode(GuiModus.COMPONENT_PICK_PARAMETER);
                    this.SelectedParameter = null; // added 18.08.2016
                    break;
                case GuiModus.COMPONENT_CALC_PICK_PARAMETER_IN:
                    if (this.SelectedCompCalculation != null && !string.IsNullOrEmpty(this.calc_param_symb_CURRENT) &&
                        this.SelectedParameter != null)
                    {
                        if (this.SelectedParameter.Propagation != ParameterStructure.Component.InfoFlow.OUPUT)
                        {
                            if (!this.SelectedCompCalculation.InputParams.ContainsKey(this.calc_param_symb_CURRENT))
                                this.SelectedCompCalculation.InputParams.Add(this.calc_param_symb_CURRENT, this.SelectedParameter);
                            else
                                this.SelectedCompCalculation.InputParams[this.calc_param_symb_CURRENT] = this.SelectedParameter;
                            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_CALC_PI_GET_OK + this.SelectedParameter.ToInfoString());
                        }
                        else
                        {
                            MessageBox.Show("Parameter ist als 'Reiner Output' definitert!", "Parameter Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                            this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_COMP_CALC_PI_GET_NE + this.SelectedParameter.ToInfoString());
                            this.LoggingService.LogWarning("Parameter ist als 'Reiner Output' definitert!");
                        }
                    }
                    this.OnSwitchMode(GuiModus.COMPONENT_CALC_PICK_PARAMETER_IN);
                    break;
                case GuiModus.COMPONENT_CALC_PICK_PARAMETER_OUT:
                    if (this.SelectedCompCalculation != null && !string.IsNullOrEmpty(this.calc_param_symb_CURRENT) &&
                        this.SelectedParameter != null)
                    {
                        if (this.SelectedParameter.Propagation != ParameterStructure.Component.InfoFlow.INPUT &&
                            this.SelectedParameter.Propagation != ParameterStructure.Component.InfoFlow.REF_IN &&
                            this.SelectedParameter.ValueField == null)
                        {
                            if (!this.SelectedCompCalculation.ReturnParams.ContainsKey(this.calc_param_symb_CURRENT))
                                this.SelectedCompCalculation.ReturnParams.Add(this.calc_param_symb_CURRENT, this.SelectedParameter);
                            else
                                this.SelectedCompCalculation.ReturnParams[this.calc_param_symb_CURRENT] = this.SelectedParameter;
                            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_CALC_PO_GET_OK + this.SelectedParameter.ToInfoString());
                        }
                        else
                        {
                            MessageBox.Show("Parameter muss als 'Reiner Output' oder 'In- und Output' definitert sein und kein Kennfeld referenzieren!", "Parameter Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                            this.LoggingService.LogError(ComponentBuilder.Properties.Resources.LOGGER_COMP_CALC_PO_GET_NE + this.SelectedParameter.ToInfoString());
                            this.LoggingService.LogWarning("Parameter muss als 'Reiner Output' oder 'In- und Output' definitert sein und kein Kennfeld referenzieren!");
                        }
                    }
                    this.OnSwitchMode(GuiModus.COMPONENT_CALC_PICK_PARAMETER_OUT);
                    break;
            }
        }
        #endregion

        #region COMPONENT
        private void HandleComponentSelection()
        {
            this.SelectedValue = null;
            this.SelectedParameter = null;
            this.SelectedCalculation = null;

            // adjust the function slots
            this.ExistingFunctionSlots = new List<SelectableString>(ComponentUtils.COMP_SLOTS_ALL_SELECTABLE);           
            if (this.SelectedComponent != null)
            {
                foreach(var entry in this.ExistingFunctionSlots)
                {
                    if (this.SelectedComponent.FitsInSlots.Contains(entry.ObjectData))
                        entry.IsSelected = true;
                    else
                        entry.IsSelected = false;
                }
                this.UserHasWritingAccessForSelectedComp = this.SelectedComponent.HasReadWriteAccess(this.UserRole);
            }
            else
            {
                this.UserHasWritingAccessForSelectedComp = false;
            }

            switch(this.Mode)
            {
                case GuiModus.NEUTRAL:
                    if (this.SynchronizeSelectionWithGV)
                    {
                        // send a message to the GeometryViewer - added 20.05.2017
                        this.OnSendComponentToGVForSelection();
                    }
                    break;
                case GuiModus.NETWORK_ELEM_PICK_COMP:
                    if (this.SelectedComponent != null && this.win_NW_Graph != null)
                    {
                        this.win_NW_Graph.PickedComp = this.SelectedComponent;
                        string comp_info= (this.SelectedComponent == null) ? "..." : this.SelectedComponent.ToInfoString();
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_NW_PICK_COMP + comp_info);
                        string nw_name = (this.win_NW_Graph.Network == null) ? "-" : this.win_NW_Graph.Network.Name;
                        this.RecordChangeForGit(this.SelectedComponent, "was placed in network " + nw_name);
                    }
                    this.OnSwitchMode(GuiModus.NETWORK_ELEM_PICK_COMP);
                    break;
                case GuiModus.COMP_COMPARER_PICK_COMP:
                    if (this.SelectedComponent != null && this.win_CCompare != null)
                    {
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMPCOMP_PICK_COMP + this.SelectedComponent.ToInfoString());
                        if (this.win_CCompare.IsPickingC1)
                            this.win_CCompare.C1 = this.SelectedComponent;
                        else if (this.win_CCompare.IsPickingC2)
                            this.win_CCompare.C2 = this.SelectedComponent;                 
                    }
                    this.OnSwitchMode(GuiModus.COMP_COMPARER_PICK_COMP);
                    break;
                case GuiModus.COMP_TO_WS_MAPPER_PICK_COMP:
                    if (this.SelectedComponent != null && this.win_WS != null)
                    {
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP2WS_PICK_COMP + this.SelectedComponent.ToInfoString());
                        if (this.win_WS.IsPickingCompMain)
                            this.win_WS.CompMain = this.SelectedComponent;
                    }
                    this.OnSwitchMode(GuiModus.COMP_TO_WS_MAPPER_PICK_COMP);
                    break;
                case GuiModus.COMP_TO_COMP_MAPPER_PICK_COMP:
                    if(this.SelectedComponent != null && this.win_comp2compM != null)
                    {
                        this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP2COMP_M_PICK_COMP + this.SelectedComponent.ToInfoString());
                        if (this.win_comp2compM.IsPickingCompData)
                            this.win_comp2compM.CompData = this.SelectedComponent;
                        else if (this.win_comp2compM.IsPickingCompCalculator)
                        {
                            if (this.win_comp2compM.CompData != null && this.win_comp2compM.CompData.ID == this.SelectedComponent.ID)
                                MessageBox.Show("Mapping Error: Cannot select self as the calculator!", "Mapping Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            else if (this.win_comp2compM.CompData != null && this.win_comp2compM.CompData.MappingToCompExists(this.SelectedComponent))
                                MessageBox.Show("Mapping Error: Cannot use the same calculator twice!", "Mapping Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            else
                                this.win_comp2compM.CompCalculator = this.SelectedComponent;
                        }                           
                    }
                    OnSwitchMode(GuiModus.COMP_TO_COMP_MAPPER_PICK_COMP);
                    break;
            }
        }

        private object HandleComponentSelectionPreview(object _preview_object)
        {
            if (_preview_object == null) return null;

            ParameterStructure.Component.Component comp = _preview_object as ParameterStructure.Component.Component;
            switch (this.Mode)
            {
                case GuiModus.COMPONENT_PICK_COMPONENT:
                    if (comp != null && this.SelectedComponent != null)
                    {
                        string comp_name = this.SelectedComponent.ToInfoString();
                        if (this.comp_selecting_subcomponent)
                        {
                            // create a deep copy of the selected component
                            // add the copy to 'SelectedComponent'                     
                            bool success = this.COMPFactory.CopyComponent(comp, this.SelectedComponent,
                                                                          this.comp_selecting_slot_name);
                            if (success)
                            {
                                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_SUB_OK + comp_name);
                                string comp_to_copy_name = (comp == null) ? "-" : comp.ToInfoString();
                                this.RecordChangeForGit(this.SelectedComponent, "received a subcomponent as a copy of" + comp_to_copy_name);
                            }                               
                            else
                                this.LoggingService.LogWarning(ComponentBuilder.Properties.Resources.LOGGER_COMP_SUB_NE + comp_name);
                        }
                        else
                        {
                            // check that the 'SelectedComponentInSubMode' is not a sub-component or a parent of
                            // 'SelectedComponent' or has been referenced already
                            // pass a REFERENCE of the this component to the 'SelectedComponent'
                            bool success = ComponentFactory.AddReferenceComponent(comp, this.SelectedComponent, this.comp_selecting_slot_name, this.UserRole);
                            if (success)
                            {
                                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_REF_OK + comp_name);
                                string comp_to_ref_name = (comp == null) ? "-" : comp.ToInfoString();
                                this.RecordChangeForGit(this.SelectedComponent, "received " + comp_to_ref_name + " as a referenced component");
                            }                                
                            else
                                this.LoggingService.LogWarning(ComponentBuilder.Properties.Resources.LOGGER_COMP_REF_NE + comp_name);
                        }
                    }
                    this.OnSwitchMode(GuiModus.COMPONENT_PICK_COMPONENT);
                    return this.SelectedComponent;
                default:
                    return _preview_object;

            }
        }
        #endregion

        #region GENERAL
        private void ResetAllSelections()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.SelectedComponent = null;
                this.SelectedCalculation = null;
                this.SelectedParameter = null;
                this.SelectedValue = null;

                // to avoid System.InvalidOperationException in TreeViewExt
                if (this.mode_prev == GuiModus.COMP_TO_COMP_MAPPER_PICK_COMP || this.mode_prev == GuiModus.COMP_TO_WS_MAPPER_PICK_COMP)
                    return;

                //this.ExistingComponents = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
                this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
                this.ExistingCalcs = new List<Calculation>(this.CALCFactory.CalcRecord);
                this.ExistingParameters = new List<Parameter>(this.PFactory.ParameterRecord);
                this.ExistingValues = new List<MultiValue>(this.MVFactory.ValueRecord);
            }));
        }

        #endregion

        #endregion

        #region METHODS: Parameter Creation / Editing

        private void CopyParameterDataFromDummy()
        {
            Parameter reference = this.SelectedParameter;
            string param_name = (reference == null) ? "-" : reference.ToInfoString();
            if (this.SelectedParameterDummy != null && !string.IsNullOrEmpty(this.SelectedParameterDummy.Name))
            {
                Parameter.CopyDataFromDummy(ref reference, this.SelectedParameterDummy);
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_PARAM_EDIT + param_name);
            }
            if (this.SelectedParameterDummy != null) // changed 30.08.2016
                this.SelectedParameterDummy.RevertToDefault();
        }

        private void CopyParamValueFieldInfoToGUI()
        {
            Window window = Application.Current.MainWindow;
            MainWindow mw = window as MainWindow;
            if (mw != null)
            {
                if (this.SelectedParameter != null)
                {
                    if (this.SelectedParameter.MValPointer != MultiValPointer.INVALID && this.SelectedParameter.ValueField != null)
                        this.SelectedParameter.ValueField.MVDisplayVector = new MultiValPointer(this.SelectedParameter.MValPointer);
                    mw.PickedMValue = this.SelectedParameter.ValueField;
                }
                else
                {
                    if (mw.PickedMValue != null && this.picked_value_own_pointer != null)
                    {
                        mw.PickedMValue.MVDisplayVector = this.picked_value_own_pointer;
                        this.picked_value_own_pointer = null;
                    }
                    mw.PickedMValue = null;
                }
            }
        }

        #endregion

        #region METHODS: Calculation Creation / Editing

        private void TestAndSaveCalculation()
        {
            this.CALCFactory.TestAndSaveCalculation();
            if (this.SelectedCalculation != null)
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_CALC_EDIT + this.SelectedCalculation.ToInfoString());
            this.SelectedCalculation = null;   
        }

        // changed 02.09.2016 (call to the factory)
        private void TestAndAddCalculationToComponent()
        {
            if (this.SelectedComponent == null) return;
            if (this.SelectedCompCalculation != null) // added 13.09.2016
                this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_COMP_CALC_EDIT + this.SelectedCompCalculation.ToInfoString());
            
            CalculationState calc_state = CalculationState.VALID;
            this.COMPFactory.TestAndSaveCalculationInEditMode(this.SelectedComponent, this.SelectedCompCalculation, this.UserRole, ref calc_state);            
            this.SelectedCompCalculation = null;

            if (calc_state != CalculationState.VALID) // added 29.08.2016
            {
                MessageBox.Show(CalculationFactory.CalcStateToStringDE(calc_state),
                    "Eingabefehler Gleichung", MessageBoxButton.OK, MessageBoxImage.Error);
                this.LoggingService.LogError(CalculationFactory.CalcStateToStringDE(calc_state)); // added 13.09.2016
            }
            else
            {
                this.LoggingService.LogInfo("Ergebnis " + CalculationFactory.CalcStateToStringDE(calc_state)); // added 13.09.2016
                string calc_name = (this.SelectedCompCalculation == null) ? "-" : this.SelectedCompCalculation.ExpressionExtended;
                this.RecordChangeForGit(this.SelectedComponent, "added calculation " + calc_name);
            }
        }

        #endregion

        #region METHODS: Assigning Symbol to Component

        private void SetComponentSymbol(ParameterStructure.Component.Component _comp)
        {
            if (_comp == null) return;
            if (_comp.SymbolId < 0)
            {
                _comp.SymbolImage = null;
                return; 
            }

            ImageRecord ir = this.ExistingSymbols.Find(x => x.ID == _comp.SymbolId);
            if (ir != null)
                _comp.SymbolImage = ir.Symbol;
            else
                _comp.SymbolImage = null;
        }

        private void SetComponentSymbols(List<ParameterStructure.Component.Component> _comps)
        {
            if (this.IRManager == null || this.IRManager.ImagesForDisplay.Count == 1 || 
                _comps == null || _comps.Count < 1) return;

            foreach (ParameterStructure.Component.Component c in _comps)
            {
                this.SetComponentSymbol(c);
                // added 12.09.2016
                List<ParameterStructure.Component.Component> sCs = c.ContainedComponents.Values.ToList();
                this.SetComponentSymbols(sCs);
            }
            this.LoggingService.LogInfo(ComponentBuilder.Properties.Resources.LOGGER_IMG_SET);
        }

        private void SetAllComponentSymbols()
        {
            this.SetComponentSymbols(this.ExistingComponents);
        }

        #endregion

        #region METHODS: Request Handler of the CommUnit

        private string AnswerRequestFromGeomView(string _request)
        {
            System.Diagnostics.Debug.WriteLine("CB: 'AnswerRequestFromGeomView' for " + _request.Substring(0, 2));
            // analyze message
            CommMessageType msg_type;
            string msg_body;
            CommMessageUtils.DecomposeMessage(_request, out msg_type, out msg_body);

            string return_msg = string.Empty;
            this.CU_alternative_action_to_take = msg_type;
            switch (msg_type)
            {
                case CommMessageType.UPDATE:
                case CommMessageType.EDIT:
                case CommMessageType.REF_UPDATE:
                    // update the component from the message                    
                    try
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.CommUnit.CurrentUserInput = CommMessageUtils.CMT_OK;
                            ComponentMessage current_message = InterProcCommunication.Specific.ComponentMessage.FromString(msg_body);
                            this.IncomingMessageHandler(current_message);
                        }));
                    }
                    catch
                    { }                    
                    return_msg = CommMessageUtils.CMT_OK;
                    break;
                case CommMessageType.OK:                    
                default:
                    return_msg = CommMessageUtils.CMT_OK;
                    break;
            }

            this.comm_can_send_next_msg.Value_NotifyOnTrue = true;
            return return_msg;
         }

        private void IncomingMessageHandler(ComponentMessage _msg)
        {
            if (this.COMPFactory == null) return;
            if (_msg == null) return;
            System.Diagnostics.Debug.WriteLine("CB: 'Incoming Message Handler' at {0} for {1}", _msg.MsgPos, _msg.CompDescr);
            
            // look at the position of the message in a sequence
            List<ParameterStructure.Component.Component> new_comps_generated_in_GV = new List<ParameterStructure.Component.Component>();
            switch(_msg.MsgPos)
            {
                case MessagePositionInSeq.SEQUENCE_START_MESSAGE:
                    this.unprocessed_incoming_messages = new List<ComponentMessage>();
                    this.unprocessed_incoming_messages.Add(_msg);
                    break;
                case MessagePositionInSeq.MESSAGE_INSIDE_SEQUENCE:
                    if (this.unprocessed_incoming_messages != null)
                        this.unprocessed_incoming_messages.Add(_msg);
                    break;
                case MessagePositionInSeq.SEQUENCE_END_MESSAGE:
                    if (this.unprocessed_incoming_messages != null)
                        this.unprocessed_incoming_messages.Add(_msg);
                    // send messages for processing by the ComponentMessageTranslator and the ComponentFactory
                    if (this.CU_alternative_action_to_take == CommMessageType.REF_UPDATE)
                        ComponentMessageTranslator.UpdateComponentReferences(this.unprocessed_incoming_messages, this.COMPFactory);
                    else
                        ComponentMessageTranslator.TranslateIntoComponents(this.unprocessed_incoming_messages, this.COMPFactory, out new_comps_generated_in_GV);
                    this.unprocessed_incoming_messages = new List<InterProcCommunication.Specific.ComponentMessage>();
                    this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
                    break;
                case MessagePositionInSeq.SINGLE_MESSAGE:
                    this.unprocessed_incoming_messages = new List<InterProcCommunication.Specific.ComponentMessage>();
                    // send message directly to the ComponentMessageTranslator for processing
                    if (this.CU_alternative_action_to_take == CommMessageType.REF_UPDATE)
                        ComponentMessageTranslator.UpdateComponentReferences(new List<ComponentMessage> { _msg }, this.COMPFactory);
                    else
                        ComponentMessageTranslator.TranslateIntoComponents(new List<ComponentMessage> { _msg }, this.COMPFactory, out new_comps_generated_in_GV);
                    this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
                    break;
                default:
                    break;
            }

            // if there were new components generated -> send their proper ids back to the geometry viewer
            if ((_msg.MsgPos == MessagePositionInSeq.SINGLE_MESSAGE || _msg.MsgPos == MessagePositionInSeq.SEQUENCE_END_MESSAGE) &&
                (new_comps_generated_in_GV.Count > 0))
            {
                // HACK: duplicate first message, so that it actually arrives (problems w deferred execution)
                List<ParameterStructure.Component.Component> processed_comps_outgoing_for_immediate_update =
                    new List<ParameterStructure.Component.Component>() { new_comps_generated_in_GV[0] };
                processed_comps_outgoing_for_immediate_update.AddRange(new_comps_generated_in_GV);
                this.SendAnyComponentToGeometryViewer(processed_comps_outgoing_for_immediate_update, false, false);                
            }
        }

        private void comm_can_send_next_msg_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.comm_can_send_next_msg == null || e == null) return;

            if (e.PropertyName == "Value_NotifyOnTrue")
            {
                // can send the next message, if there is one
                if (this.messages_to_send.Count > 0)
                {
                    this.comm_can_send_next_msg.Value_NotifyOnTrue = false;
                    System.Diagnostics.Debug.WriteLine("CB>> sending next message...");
                    this.message_last_sent = this.messages_to_send.Dequeue();
                    this.CommUnit.CurrentUserInput = this.message_last_sent;

                    this.Dispatcher.Invoke(
                        new Action(() =>
                        {
                            this.CommFinishedSendingMessages = (this.messages_to_send.Count == 0);
                        })
                    );
                }
            }
            else if (e.PropertyName == "TrueAfterTimeout")
            {
                if (this.messages_to_send.Count > 0)
                {
                    // message got lost on the way -> resend
                    if (!(string.IsNullOrEmpty(this.message_last_sent)))
                    {
                        this.comm_can_send_next_msg.Value_NotifyOnTrue = false;
                        System.Diagnostics.Debug.WriteLine("CB>> sending message AGAIN after loss ...");
                        this.CommUnit.CurrentUserInput = this.message_last_sent;
                        this.message_last_sent = string.Empty;

                        this.Dispatcher.Invoke(
                            new Action(() =>
                            {
                                this.CommFinishedSendingMessages = (this.messages_to_send.Count == 0);
                            })
                        );
                    }
                }
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void COMPFactory_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.COMPFactory == null || e == null) return;
            if (e.PropertyName == "RefreshView")
            {
                this.ExistingComponents = this.COMPFactory.ExtractVisibleComponents(this.UserDefinedVisibilityON);
            }
        }

        private void LoggerSerivce_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e != null)
            {
                if (e.PropertyName == "NewEntryToDisplay")
                {
                    try
                    {
                        this.Dispatcher.Invoke(() =>
                            {
                                this.ExistingLoggerEntries = new List<LoggerEntry>(this.LoggingService.Entries);
                            });
                    }
                    catch
                    { }
                }
            }
        }

        private void CU_Logger_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e != null)
            {
                if (e.PropertyName == "NewEntryToDisplay")
                        this.LoggingService.MergeLast(this.CU_Logger);
            }
        }

        #endregion
    }
}
