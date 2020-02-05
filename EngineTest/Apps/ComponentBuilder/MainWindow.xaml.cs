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
using System.Windows.Media.Effects;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ComponentBuilder.UIElements;
using ParameterStructure.Values;
using ParameterStructure.Parameter;
using ParameterStructure.Component;

namespace ComponentBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool close_really = false;

#if DEBUG
            close_really = true;
#else
            MessageBoxResult result = MessageBox.Show("Möchten Sie die Anwendung wirklich verlassen?", "Anwendung Schliessen", MessageBoxButton.OKCancel);
            close_really = (result == MessageBoxResult.OK);
#endif

            if (close_really)
            {
                AppViewModel data_context = this.DataContext as AppViewModel;
                if (data_context == null) return;

                // shut-down the comm unit
                data_context.ShutDownCommUnit(false);

                // close an open project
                if (data_context.CloseComponentProjectCmd.CanExecute(null))
                    data_context.CloseComponentProjectCmd.Execute(null);

                // close any open sub-windows
                if (this.resPNames_win != null && this.resPNames_win.IsLoaded)
                    this.resPNames_win.Close();
                if (this.comp_Gr_win != null && this.comp_Gr_win.IsLoaded)
                    this.comp_Gr_win.Close();
                if (this.flow_nw_Gr_win != null && this.flow_nw_Gr_win.IsLoaded)
                    this.flow_nw_Gr_win.Close();
                if (this.compComp_win != null && this.compComp_win.IsLoaded)
                    this.compComp_win.Close();
                if (this.wsMap_win != null && this.wsMap_win.IsLoaded)
                    this.wsMap_win.Close();
                if (this.c2dMap_win != null && this.c2dMap_win.IsLoaded)
                    this.c2dMap_win.Close();
            }
            else
            {
                e.Cancel = true;
            }
        }

        #region USER PROFILE

        private UserRolePickerWindow user_RP_window;

        public ComponentManagerType OpenUserRolePickerWin(string _git_config_msg_short, string _git_config_msg_long, bool _git_ok, out bool _user_cancelled)
        {
            this.user_RP_window = new UserRolePickerWindow();
            this.user_RP_window.GitMsgShort = _git_config_msg_short;
            this.user_RP_window.GitMsgLong = _git_config_msg_long;
            this.user_RP_window.GitOK = _git_ok;
            this.user_RP_window.ShowDialog();

            _user_cancelled = !this.user_RP_window.UserProfileSelected;
            return this.user_RP_window.UserProfile;
        }

        #endregion

        #region Value TABLE Creation / Editing Window

        private CreateMVTableWindow create_MVTable_win;
        private MultiValueTable value_table_in_edit_mode;
        public void OpenCreateValueWindow(ref MultiValueFactory _factory)
        {
            this.create_MVTable_win = new CreateMVTableWindow();
            this.create_MVTable_win.MVFactory = _factory;
            this.create_MVTable_win.ShowDialog();
        }

        public void OpenEditValueTableWindow(ref MultiValueFactory _factory, MultiValueTable _to_edit)
        {
            this.create_MVTable_win = new CreateMVTableWindow();
            this.create_MVTable_win.Loaded += create_MVTable_win_Loaded;
            this.create_MVTable_win.MVFactory = _factory;
            this.value_table_in_edit_mode = _to_edit;
            this.create_MVTable_win.ShowDialog();
        }

        private void create_MVTable_win_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.create_MVTable_win == null || this.value_table_in_edit_mode == null) return;
            this.create_MVTable_win.FillInput(this.value_table_in_edit_mode);
        }

        #endregion

        #region Value TABLE Info Window

        private ShowMVTableWindow show_MVTable_win;

        public void OpenShowValueWindow(MultiValueTable _to_show)
        {
            if (_to_show == null) return;

            this.show_MVTable_win = new ShowMVTableWindow();
            this.show_MVTable_win.Data = _to_show;
            this.show_MVTable_win.ShowDialog();
        }

        #endregion

        #region Value FUNCTION Creation / Editing Window

        private CreateMVFunctionWindow create_MVFunct_win;
        private MultiValueFunction function_in_edit_mode;

        public void OpenCreateValueFunctionWindow(ref MultiValueFactory _factory)
        {
            this.create_MVFunct_win = new CreateMVFunctionWindow();
            this.create_MVFunct_win.MVFactory = _factory;
            this.create_MVFunct_win.ShowDialog();
        }

        public void OpenEditValueFunctionWindow(ref MultiValueFactory _factory, MultiValueFunction _to_edit)
        {
            this.create_MVFunct_win = new CreateMVFunctionWindow();
            this.create_MVFunct_win.Loaded += create_MVFunct_win_Loaded;
            this.create_MVFunct_win.MVFactory = _factory;
            this.function_in_edit_mode = _to_edit;
            this.create_MVFunct_win.ShowDialog();
        }

        private void create_MVFunct_win_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.create_MVFunct_win == null || this.function_in_edit_mode == null) return;
            this.create_MVFunct_win.FillInput(this.function_in_edit_mode);
        }


        #endregion

        #region Function FIELD Info Window

        private ShowMVFunctionWindow show_MVFunction_win;

        public void OpenShowFunctionWindow(MultiValueFunction _to_show)
        {
            if (_to_show == null) return;

            this.show_MVFunction_win = new ShowMVFunctionWindow();
            this.show_MVFunction_win.Data = _to_show;
            this.show_MVFunction_win.ShowDialog();
        }

        #endregion

        #region EXCEL Big Table Info Window

        private ShowMVBigTableWindow show_BT_win;

        public void OpenShowBigTableWindow(MultiValueBigTable _to_show)
        {
            if (_to_show == null) return;

            this.show_BT_win = new ShowMVBigTableWindow();
            this.show_BT_win.Data = _to_show;
            this.show_BT_win.ShowDialog();
        }

        #endregion

        #region PARAMETER Creation / Editing EXPANDER

        #region PROPERTY: Picked Value

        private MultiValue picked_mvalue;
        public MultiValue PickedMValue
        {
            get { return this.picked_mvalue; }
            set
            {
                this.picked_mvalue = value;

                // populate the display with the value field data
                if (this.sp_param_MV != null && this.chb_interp != null)
                {
                    int debug1 = this.sp_param_MV.Children.Count;
                    this.sp_param_MV.Children.Clear();
                    this.tb_MVName.Text = "";
                    if (this.picked_mvalue == null) return;

                    this.tb_MVName.Text = this.picked_mvalue.MVName;

                    MultiValueTable mvt = this.picked_mvalue as MultiValueTable;
                    MultiValueFunction mvf = this.picked_mvalue as MultiValueFunction;
                    MultiValueBigTable mvBT = this.picked_mvalue as MultiValueBigTable;
                    if (mvt != null)
                    {
                        MValueField3DInfo vis = new MValueField3DInfo();
                        vis.Height = 230;
                        vis.Width = 420;
                        vis.UseLayoutRounding = true;
                        vis.SnapsToDevicePixels = true;
                        vis.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        vis.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                        vis.Background = new SolidColorBrush(Colors.Gainsboro);
                        vis.BorderBrush = new SolidColorBrush(Colors.DimGray);
                        vis.TabStripPlacement = Dock.Bottom;
                        vis.DataField = mvt;
                        if (mvt.MVDisplayVector.NrDim > 2)
                            vis.SelectedIndex = mvt.MVDisplayVector.CellIndices[2];
                        this.sp_param_MV.Children.Add(vis);

                        this.chb_interp.IsChecked = mvt.MVCanInterpolate;
                    }
                    else if (mvf != null)
                    {
                        MValueFunct3DInfo vis = new MValueFunct3DInfo();
                        vis.Height = 280;
                        vis.Width = 420;
                        vis.UseLayoutRounding = true;
                        vis.SnapsToDevicePixels = true;
                        vis.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        vis.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                        vis.Background = new SolidColorBrush(Colors.Gainsboro);
                        vis.BorderBrush = new SolidColorBrush(Colors.DimGray);
                        vis.TabStripPlacement = Dock.Bottom;
                        vis.DataField = mvf;
                        if (mvf.MVDisplayVector.NrDim > 2)
                            vis.SelectedIndex = mvf.MVDisplayVector.CellIndices[2];
                        this.sp_param_MV.Children.Add(vis);

                        this.chb_interp.IsChecked = true;
                    }
                    else if (mvBT != null)
                    {
                        MValueBigTableDataGrid vis = new MValueBigTableDataGrid();
                        vis.Height = 280;
                        vis.Margin = new Thickness(5, 2, 5, 2);
                        vis.Padding = new Thickness(0, 0, 0, 5);
                        vis.Background = new SolidColorBrush(Colors.Gainsboro);
                        vis.BorderBrush = new SolidColorBrush(Colors.DimGray);
                        vis.HorizontalGridLinesBrush = new SolidColorBrush(Colors.DimGray);
                        vis.VerticalGridLinesBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB0B0B0"));
                        vis.RowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFBBBBBB"));
                        vis.AlternatingRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD8D8D8"));
                        vis.AlternationCount = 2;
                        vis.AutoGenerateColumns = false;
                        vis.SelectionMode = DataGridSelectionMode.Single;
                        vis.SelectionUnit = DataGridSelectionUnit.Cell;
                        vis.FrozenColumnCount = 1;
                        vis.CanUserSortColumns = false;
                        vis.CanUserReorderColumns = false;
                        vis.CanUserDeleteRows = false;
                        vis.CanUserAddRows = false;
                        vis.IsSynchronizedWithCurrentItem = true;
                        vis.CellStyle = (Style)this.TryFindResource("BigTable_DataGridCell_Normal");
                        vis.DataField = mvBT;
                        this.sp_param_MV.Children.Add(vis);

                        this.chb_interp.IsChecked = false;
                    }
                }
            }
        }

        #endregion

        #region EVENT HANDLERS: Category, InfoFlow
        private void TextBlock_category_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            if (tb == null) return;

            if (ComponentUtils.LOWER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.Text = tb.Text.ToUpper();
                tb.ToolTip = "IST " + ComponentUtils.CategoryStringToDescription(tb.Text);
                tb.Effect = new DropShadowEffect()
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FF0000ff"),
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 1,
                    Opacity = 1
                };
            }
            else if (ComponentUtils.UPPER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.ToolTip = "IST NICHT " + ComponentUtils.CategoryStringToDescription(tb.Text);
                tb.Text = tb.Text.ToLower();
                tb.Effect = null;
            }
        }

        private void TextBlock_category_AppearanceAccToContent(object sender)
        {
            TextBlock tb = sender as TextBlock;
            if (tb == null) return;

            if (ComponentUtils.LOWER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.ToolTip = "IST NICHT " + ComponentUtils.CategoryStringToDescription(tb.Text);
                tb.Effect = null;   
            }
            else if (ComponentUtils.UPPER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.ToolTip = "IST " + ComponentUtils.CategoryStringToDescription(tb.Text);
                tb.Effect = new DropShadowEffect()
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FF0000ff"),
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 1,
                    Opacity = 1
                };
            }
        }

        private void TextBlock_info_flow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            if (tb == null) return;
            if (string.IsNullOrEmpty(tb.Text)) return;

            InfoFlow infoF = ComponentUtils.StringToInfoFlow(tb.Text);
            int nrTypes = Enum.GetNames(typeof(InfoFlow)).Length;
            int tmp = (int)(infoF + 1) % nrTypes;
            infoF = (InfoFlow)((int)(infoF + 1) % nrTypes);
            tb.Text = ComponentUtils.InfoFlowToString(infoF);
            tb.ToolTip = ComponentUtils.InfoFlowStringToDescription(tb.Text);
        }
        #endregion

        #region EVENT HANLDERS: Stack Panel, OK Button
        private void sp_param_MV_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.picked_mvalue == null) return;
            
            if (this.sp_param_MV == null) return;
            if (this.sp_param_MV.Tag == null) return;
            if (!(this.sp_param_MV.Tag is ParameterDummy)) return;

            ParameterDummy pd = this.sp_param_MV.Tag as ParameterDummy;
            pd.ValueCurrent = this.picked_mvalue.MVDisplayVector.Value;
            pd.MValPointer = new MultiValPointer(this.picked_mvalue.MVDisplayVector);
        }

        // called before the command associated with this button
        private void Parameter_New_OK(object sender, RoutedEventArgs e)
        {
            
        }
        #endregion

        #endregion

        #region PARAMETER: List of Reserved Names

        private ReservedPNamesWindow resPNames_win;

        public ReservedPNamesWindow OpenReservedParamNamesWindow(List<ParameterReservedNameRecord> _record)
        {
            this.resPNames_win = new ReservedPNamesWindow();
            this.resPNames_win.Record = _record;
            this.resPNames_win.Show();

            return this.resPNames_win;
        }

        #endregion

        #region COMPONENT GRAPH

        private ComponentGraphWindow comp_Gr_win;

        public ComponentGraphWindow OpenComponentGraphWindow(ComponentFactory _factory)
        {
            this.comp_Gr_win = new ComponentGraphWindow();
            this.comp_Gr_win.CompManager = _factory;
            this.comp_Gr_win.Show();

            return this.comp_Gr_win;
        }

        private FlowNetworkGraphWindow flow_nw_Gr_win;

        public FlowNetworkGraphWindow OpenNetworkGraphWindow(FlowNetwork _to_display, ComponentFactory _factory)
        {
            this.flow_nw_Gr_win = new FlowNetworkGraphWindow();
            this.flow_nw_Gr_win.Network = _to_display;
            this.flow_nw_Gr_win.CompManager = _factory;
            this.flow_nw_Gr_win.Show();

            return this.flow_nw_Gr_win;
        }

        #endregion

        #region COMPONENT IMPORT

        private ImportDxfCompsWindow imp_dxfComp_win;

        public List<Component> OpenImportDxfCompsWindow()
        {
            this.imp_dxfComp_win = new ImportDxfCompsWindow();
            this.imp_dxfComp_win.ShowDialog();

            return this.imp_dxfComp_win.MarkedForImport;
        }

        #endregion

        #region COMPONENT COMPARISON, MAPPING

        private CompCompareWindow compComp_win;

        public CompCompareWindow OpenCompareComponentsWindow(ComponentManagerType _user)
        {
            this.compComp_win = new CompCompareWindow(_user);           
            this.compComp_win.Show();

            return this.compComp_win;
        }

        private WebServiceMapWindow wsMap_win;
        public WebServiceMapWindow OpenWebServiceMapWindow(List<WebServiceConnections.TypeNode> _ws_entry_points)
        {
            if (_ws_entry_points == null || _ws_entry_points.Count == 0) return null;

            this.wsMap_win = new WebServiceMapWindow();
            this.wsMap_win.WSRoots = _ws_entry_points;
            this.wsMap_win.Show();

            return this.wsMap_win;
        }

        private Comp2CompMappingWindow c2dMap_win;
        public Comp2CompMappingWindow OpenComp2CompMappingWindow()
        {
            this.c2dMap_win = new Comp2CompMappingWindow();
            this.c2dMap_win.Show();
            return this.c2dMap_win;
        }

        #endregion

        #region CONTROLS

        // enable horizontal scrolling with the mouse wheel
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollviewer = sender as ScrollViewer;
            if (scrollviewer == null || e == null) return;

            // activate only over the scrollbar (in this case at the bottom)
            Point pos = e.GetPosition(this);
            if (pos.Y < this.Height - 100) return;

            if (e.Delta > 0)
                scrollviewer.LineLeft();
            else
                scrollviewer.LineRight();
            e.Handled = true;
        }

        #endregion

    }
}
