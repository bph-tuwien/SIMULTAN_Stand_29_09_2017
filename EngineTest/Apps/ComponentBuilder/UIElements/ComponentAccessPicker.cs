using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Data;  
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;

using ComponentBuilder.WpfUtils;
using ParameterStructure.Component;

namespace ComponentBuilder.UIElements
{
    class ComponentAccessPicker : Grid, INotifyPropertyChanged
    {
        #region STATIC

        private const string USER_MARK = "user";

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

        #region PROPERTIES: User currently loggen in

        public ComponentManagerType LoggedUser
        {
            get { return (ComponentManagerType)GetValue(LoggedUserProperty); }
            set { SetValue(LoggedUserProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoggedUser.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoggedUserProperty =
            DependencyProperty.Register("LoggedUser", typeof(ComponentManagerType), typeof(ComponentAccessPicker),
            new PropertyMetadata(ComponentManagerType.GUEST, new PropertyChangedCallback(LoggedUserPropertyChangedCallback)));

        private static void LoggedUserPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentAccessPicker instance = d as ComponentAccessPicker;
            if (instance == null) return;
            
            if (instance.IsLoaded)
                instance.SetUserMark();

            instance.AccessEditMode = (instance.LoggedUser == ComponentManagerType.ADMINISTRATOR);
        }

        #endregion

        #region PROPERTIES: Bound Component

        public ParameterStructure.Component.Component ComponentToDisplay
        {
            get { return (ParameterStructure.Component.Component)GetValue(ComponentToDisplayProperty); }
            set { SetValue(ComponentToDisplayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ComponentToDisplay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ComponentToDisplayProperty =
            DependencyProperty.Register("ComponentToDisplay", typeof(ParameterStructure.Component.Component), typeof(ComponentAccessPicker),
            new UIPropertyMetadata(null, new PropertyChangedCallback(ComponentToDisplayPropertyChangedCallback),
                new CoerceValueCallback(ComponentToDisplayCoerceValueCallback)));

        private static object ComponentToDisplayCoerceValueCallback(DependencyObject d, object baseValue)
        {
            ComponentAccessPicker instance = d as ComponentAccessPicker;
            if (instance == null) return baseValue;
            if (instance.ComponentToDisplay != null)
                instance.ComponentToDisplay.PropertyChanged -= instance.ComponentToDisplay_PropertyChanged;

            return baseValue;
        }

        private static void ComponentToDisplayPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentAccessPicker instance = d as ComponentAccessPicker;
            if (instance == null) return;

            // event handler, if the profile changes
            if (instance.ComponentToDisplay != null)
            {
                instance.ComponentToDisplay.PropertyChanged += instance.ComponentToDisplay_PropertyChanged;
            }
            
            // display information
            instance.access_adapted_to_slot_once = false;
            instance.AccessEditMode = (instance.LoggedUser == ComponentManagerType.ADMINISTRATOR);
            instance.Children.Clear();
            instance.PopulteGrid();
            instance.SetUserMark();
        }

        #endregion

        #region PROPERTIES: Access Modification Mode

        public bool AccessEditMode
        {
            get { return (bool)GetValue(AccessEditModeProperty); }
            set { SetValue(AccessEditModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AccessEditMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AccessEditModeProperty =
            DependencyProperty.Register("AccessEditMode", typeof(bool), typeof(ComponentAccessPicker),
            new UIPropertyMetadata(true, new PropertyChangedCallback(AccessEditModePropertyChangedCallback)));

        private static void AccessEditModePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentAccessPicker instance = d as ComponentAccessPicker;
            if (instance == null) return;

            // re-display information
            instance.Children.Clear();
            instance.PopulteGrid();
            instance.SetUserMark();
        }

        #endregion

        #region CLASS MEMBERS

        private bool size_original_set = false;
        protected double width_original = 0;
        protected double height_original = 0;

        protected int nr_columns = Enum.GetNames(typeof(ComponentManagerType)).Length;
        protected int nr_rows = 1 + 4 + 1;
        protected int column_width = 26;
        protected int row_height = 26;

        protected bool access_adapted_to_slot_once = false;

        #endregion

        #region .CTOR

        public ComponentAccessPicker()
            :base()
        {
            this.Loaded += ComponentAccessPicker_Loaded;
        }

        #endregion

        #region METHODS: Grid Size

        protected void CalculateSize()
        {
            // set the original sizes, if not set already
            if (!this.size_original_set)
            {
                this.width_original = this.Width;
                this.height_original = this.Height;
                this.size_original_set = true;
            }
            
            // re-calculate the size of the grid
            this.Width = this.nr_columns * this.column_width;
            this.Height = this.nr_rows * this.row_height;
        }

        protected void DefineSymbolGrid()
        {
            for (int i = 0; i < this.nr_columns; i++ )
            {
                ColumnDefinition cd = new ColumnDefinition();
                cd.Width = new GridLength(this.column_width);
                this.ColumnDefinitions.Add(cd);
            }

            for (int i = 0; i < this.nr_rows; i++)
            {
                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(this.row_height);
                this.RowDefinitions.Add(rd);
            }
        }

        #endregion

        #region METHODS: Grid Realize

        private void PopulteGrid()
        {
            string access_basic = ComponentUtils.COMP_ACCESS_NONE;

            for (int i = 0; i < this.nr_columns; i++)
            {
                // backgrdound color
                Rectangle rect = new Rectangle();
                if (i % 2 == 0)
                    rect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC0C0C0"));
                
                Grid.SetRow(rect, 0);
                Grid.SetRowSpan(rect, this.nr_rows - 1);
                Grid.SetColumn(rect, i);
                this.Children.Add(rect);

                // background lines
                Border border = new Border();
                border.BorderThickness = new Thickness(0, 0, 0, 1);
                border.BorderBrush = new SolidColorBrush(Colors.DimGray);
                border.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                border.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

                Grid.SetRow(border, 0);
                Grid.SetColumn(border, i);
                this.Children.Add(border);

                // shadow
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Vertical;
                sp.Effect = new DropShadowEffect()
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FF551700"),
                    Direction = 315,
                    ShadowDepth = 3,
                    BlurRadius = 3,
                    Opacity = 0.5
                };
                sp.Tag = (ComponentManagerType)i;

                // manager type symbols
                TextBlock tbM = new TextBlock();
                tbM.Width = 26;
                tbM.Height = 26;
                tbM.Text = ComponentUtils.ComponentManagerTypeToLetter((ComponentManagerType)i);
                tbM.ToolTip = ComponentUtils.ComponentManagerTypeToDescrDE((ComponentManagerType)i);
                tbM.Padding = new Thickness(2, 2, 1, 1);
                tbM.FontSize = 22;
                tbM.FontFamily = new FontFamily(new Uri("pack://application:,,,/ComponentBuilder;component/Data/fonts/"), "./#managers");
                tbM.Foreground = new SolidColorBrush(Colors.Black);

                sp.Children.Add(tbM);

                // access type symbols
                string access_specific = access_basic;
                if (this.ComponentToDisplay != null)
                {
                    if (this.AccessEditMode)
                        access_specific = ComponentUtils.ComponentAccessTypeToString(this.ComponentToDisplay.AccessLocal[(ComponentManagerType)i].AccessTypeFlags);
                    else
                        access_specific = ComponentUtils.ComponentAccessTypeInTrackerToString(this.ComponentToDisplay.AccessLocal[(ComponentManagerType)i]);
                }

                for(int j = 1; j < this.nr_rows - 1; j++)
                {
                    TextBlockDoubleText tb = new TextBlockDoubleText();
                    tb.Width = 26;
                    tb.Height = 26;
                    tb.TextCopy = access_specific[j - 1].ToString();

                    ComponentManagerAndAccessFlagDateTimeTriple info = new ComponentManagerAndAccessFlagDateTimeTriple();
                    info.ManagerType = (ComponentManagerType)i;
                    info.AccessFlagIndex = j - 1;
                    info.AccessTimeStamp_Current = DateTime.MinValue;
                    info.AccessTimeStamp_Prev = DateTime.MinValue;
                    if (this.ComponentToDisplay != null)
                    {
                        info.AccessTimeStamp_Current = this.ComponentToDisplay.AccessLocal[info.ManagerType].GetTimeStamp(j - 1);
                        info.AccessTimeStamp_Prev = this.ComponentToDisplay.AccessLocal[info.ManagerType].GetPrevTimeStamp(j - 1);
                    }
                    tb.Tag = info;

                    if (this.AccessEditMode)
                        this.TextBlock_access_AppearanceAccToContent(tb);
                    else
                        this.TextBlock_accessRecord_AppearanceAccToContent(tb);

                    tb.Padding = new Thickness(2, 2, 1, 1);
                    tb.FontSize = 18;
                    tb.FontFamily = new FontFamily(new Uri("pack://application:,,,/ComponentBuilder;component/Data/fonts/"), "./#managers");
                    tb.Style = (Style)tb.TryFindResource("CategoryLabel");
                    
                    if (this.AccessEditMode)
                    {
                        tb.MouseUp += TextBlock_access_MouseUp;
                        tb.TextChangedEventHandler += TextBlock_access_AppearanceAccToContent;
                    }
                    else
                    {
                        tb.MouseUp += TextBlock_accessRecord_MouseUp;
                        tb.TextChangedEventHandler += TextBlock_accessRecord_AppearanceAccToContent;
                    }

                    //tb.IsEnabled = (this.AccessEditMode || (ComponentManagerType)i == this.LoggedUser);
                   
                    sp.Children.Add(tb);
                }

                Grid.SetRow(sp, 0);
                Grid.SetRowSpan(sp, this.nr_rows - 1);
                Grid.SetColumn(sp, i);
                this.Children.Add(sp);
            }

            // checkbox for the access editing mode
            CheckBox cb_AEM = new CheckBox();
            cb_AEM.Content = "EIN: zuweisen / AUS: nutzen ";
            cb_AEM.Margin = new Thickness(5, 4, 5, 2);
            cb_AEM.IsChecked = this.AccessEditMode;
            cb_AEM.Visibility = (this.LoggedUser == ComponentManagerType.ADMINISTRATOR) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            cb_AEM.Style = (Style)this.TryFindResource("check_Box_Blue_White");
            Binding ch_AEM_b = new Binding("IsChecked");
            ch_AEM_b.Source = cb_AEM;
            ch_AEM_b.Mode = BindingMode.TwoWay;
            this.SetBinding(ComponentAccessPicker.AccessEditModeProperty, ch_AEM_b);

            Grid.SetRow(cb_AEM, this.nr_rows - 1);
            Grid.SetColumn(cb_AEM, 0);
            Grid.SetColumnSpan(cb_AEM, this.nr_columns);
            this.Children.Add(cb_AEM);
        }



        #endregion

        #region METHODS: User Mark

        private void SetUserMark()
        {
            // delete old Mark
            Rectangle old_mark = (Rectangle)this.FindName(ComponentAccessPicker.USER_MARK);
            if (old_mark != null)
            {
                this.Children.Remove(old_mark);
                this.UnregisterName(ComponentAccessPicker.USER_MARK);
            }

            // set new mark
            Color mark_colF = (this.AccessEditMode) ? (Color)ColorConverter.ConvertFromString("#080000ff") : (Color)ColorConverter.ConvertFromString("#11ff7e00");
            Color mark_colE = (this.AccessEditMode) ? (Color)ColorConverter.ConvertFromString("#FF0000ff") : (Color)ColorConverter.ConvertFromString("#FFff7e00");
            
            Rectangle new_mark = new Rectangle();
            new_mark.Stroke = new SolidColorBrush(Colors.Black);
            new_mark.StrokeThickness = (this.AccessEditMode) ? 1 : 2;
            new_mark.Fill = new SolidColorBrush(mark_colF);
            new_mark.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            new_mark.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            new_mark.Effect = new DropShadowEffect()
            {
                Color = mark_colE,
                Direction = (this.AccessEditMode) ? 315 : 145,
                ShadowDepth = 1,
                BlurRadius = 2,
                Opacity = 1
            };
            new_mark.IsHitTestVisible = false;
            new_mark.Name = ComponentAccessPicker.USER_MARK;
            this.RegisterName(new_mark.Name, new_mark);

            Grid.SetColumn(new_mark, (int)this.LoggedUser);
            Grid.SetRow(new_mark, 0);
            Grid.SetRowSpan(new_mark, this.nr_rows - 1);
            this.Children.Add(new_mark);           
        }


        #endregion

        #region METHODS: Save Change in Access

        private void ChangeAccessFor(ComponentManagerType _type)
        {
            if (this.ComponentToDisplay == null) return;

            foreach(var child in this.Children)
            {
                StackPanel sp = child as StackPanel;
                if (sp == null) continue;
                if (sp.Tag == null) continue;
                if (!(sp.Tag is ComponentManagerType)) continue;

                ComponentManagerType sp_type = (ComponentManagerType)sp.Tag;
                if (sp_type != _type) continue;

                string access = ComponentUtils.COMP_ACCESS_NONE;
                char[] access_chars = access.ToCharArray();

                foreach(var sp_child in sp.Children)
                {
                    TextBlockDoubleText sp_tb = sp_child as TextBlockDoubleText;
                    if (sp_tb == null) continue;
                    if (sp_tb.Tag == null) continue;
                    if (!(sp_tb.Tag is ComponentManagerAndAccessFlagDateTimeTriple)) continue;

                    ComponentManagerAndAccessFlagDateTimeTriple MAtp = (ComponentManagerAndAccessFlagDateTimeTriple)sp_tb.Tag;
                    access_chars[MAtp.AccessFlagIndex] = sp_tb.Text.ToCharArray()[0];
                }
                access = new string(access_chars);
                ComponentAccessType access_type = ComponentUtils.StringToComponentAccessType(access);

                ComponentAccessTracker tracker = this.ComponentToDisplay.AccessLocal[sp_type];
                tracker.AccessTypeFlags = access_type;
                this.ComponentToDisplay.AccessLocal[sp_type] = tracker;

                break;
            }
        }

        private void ChangeAccessRecordFor(ComponentManagerType _type)
        {
            if (this.ComponentToDisplay == null) return;

            foreach (var child in this.Children)
            {
                StackPanel sp = child as StackPanel;
                if (sp == null) continue;
                if (sp.Tag == null) continue;
                if (!(sp.Tag is ComponentManagerType)) continue;

                ComponentManagerType sp_type = (ComponentManagerType)sp.Tag;
                if (sp_type != _type) continue;

                string access = ComponentUtils.COMP_ACCESS_NONE;
                char[] access_chars = access.ToCharArray();
                DateTime[] access_time_stamps = new DateTime[4];

                foreach (var sp_child in sp.Children)
                {
                    TextBlockDoubleText sp_tb = sp_child as TextBlockDoubleText;
                    if (sp_tb == null) continue;
                    if (sp_tb.Tag == null) continue;
                    if (!(sp_tb.Tag is ComponentManagerAndAccessFlagDateTimeTriple)) continue;

                    ComponentManagerAndAccessFlagDateTimeTriple info = (ComponentManagerAndAccessFlagDateTimeTriple)sp_tb.Tag;
                    access_chars[info.AccessFlagIndex] = sp_tb.Text.ToCharArray()[0];
                    access_time_stamps[info.AccessFlagIndex] = info.AccessTimeStamp_Current;                    
                }
                // set the ACCESS TYPE
                access = new string(access_chars);
                ComponentAccessType access_type = ComponentUtils.StringToComponentAccessType(access);

                ComponentAccessTracker tracker = this.ComponentToDisplay.AccessLocal[sp_type];
                tracker.AccessTypeFlags = access_type;               
                // set the TIME STAMPS
                for (int t = 0; t < 4; t++ )
                {
                    tracker.SetTimeStamp(t, access_time_stamps[t]);
                }
                this.ComponentToDisplay.AccessLocal[sp_type] = tracker;

                break;
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void ComponentToDisplay_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ParameterStructure.Component.Component ctd = sender as ParameterStructure.Component.Component;
            if (ctd == null || e == null || string.IsNullOrEmpty(e.PropertyName)) return;

            if (e.PropertyName == "FitsInSlots" && !this.access_adapted_to_slot_once)
            {
                // display information
                this.AccessEditMode = (this.LoggedUser == ComponentManagerType.ADMINISTRATOR);
                this.Children.Clear();
                this.PopulteGrid();
                this.SetUserMark();
                this.access_adapted_to_slot_once = true;
            }
            else if (e.PropertyName == "AccessLocal")
            {
                // update (added 01.09.2016)
                this.Children.Clear();
                this.PopulteGrid();
                this.SetUserMark();
            }
        }

        private void ComponentAccessPicker_Loaded(object sender, RoutedEventArgs e)
        {
            this.CalculateSize();
            this.DefineSymbolGrid();
            this.PopulteGrid();
            this.SetUserMark();
        }

        // DEFINITION of Access
        private void TextBlock_access_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            if (tb == null) return;
            if (tb.Tag == null) return;
            if (!(tb.Tag is ComponentManagerAndAccessFlagDateTimeTriple)) return;

            ComponentManagerAndAccessFlagDateTimeTriple info = (ComponentManagerAndAccessFlagDateTimeTriple)tb.Tag;

            if (ComponentUtils.LOWER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.Text = tb.Text.ToUpper();
                tb.ToolTip = ComponentUtils.ComponentAccessTypeStringToDescriptionDE(tb.Text);
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
                tb.Text = tb.Text.ToLower();
                tb.ToolTip = ComponentUtils.ComponentAccessTypeStringToDescriptionDE(tb.Text);
                tb.Effect = null;
            }

            // communicate to component
            this.ChangeAccessFor(info.ManagerType);
        }

        // USAGE of Access
        private void TextBlock_accessRecord_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TextBlockDoubleText tb = sender as TextBlockDoubleText;
            if (tb == null) return;
            if (tb.Tag == null) return;
            if (!(tb.Tag is ComponentManagerAndAccessFlagDateTimeTriple)) return;
                           
            ComponentManagerAndAccessFlagDateTimeTriple info = (ComponentManagerAndAccessFlagDateTimeTriple)tb.Tag;
            if (info.ManagerType != this.LoggedUser) return;

            string date = "";

            if (!ComponentUtils.ACCESS_ALLOWED_SINGLE.IsMatch(tb.Text) &&
                !ComponentUtils.ACCESS_RECORDED_SINGLE.IsMatch(tb.Text))
                return;
            if (ComponentUtils.ACCESS_ALLOWED_READWRITE.IsMatch(tb.Text))
                return;

            if (tb.NrClicks % 2 == 1)
            {
                info.AccessTimeStamp_Prev = info.AccessTimeStamp_Current;
                info.AccessTimeStamp_Current = DateTime.Now;
                date = info.AccessTimeStamp_Current.ToString(DateTimeFormatInfo.CurrentInfo);

                tb.Text = tb.Text.ToUpper();
                tb.ToolTip = ComponentUtils.ComponentAccessTypeStringToDescriptionDE(tb.Text, date);
                tb.Effect = new DropShadowEffect()
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FFff7e00"),
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 1,
                    Opacity = 1
                };
            }
            else
            {
                info.AccessTimeStamp_Current = info.AccessTimeStamp_Prev;
                
                if (info.AccessTimeStamp_Current > DateTime.MinValue)
                    date = info.AccessTimeStamp_Current.ToString(DateTimeFormatInfo.CurrentInfo);
                else
                    tb.Text = tb.Text.ToLower();

                tb.ToolTip = ComponentUtils.ComponentAccessTypeStringToDescriptionDE(tb.Text, date);
                tb.Effect = new DropShadowEffect()
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FF0000ff"),
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 1,
                    Opacity = 1
                };
            }

            // communicate to component
            tb.Tag = info;
            this.ChangeAccessRecordFor(info.ManagerType);
        }

        // DEFINITION of Access
        private void TextBlock_access_AppearanceAccToContent(object sender)
        {
            TextBlock tb = sender as TextBlock;
            if (tb == null) return;

            if (ComponentUtils.LOWER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.ToolTip = ComponentUtils.ComponentAccessTypeStringToDescriptionDE(tb.Text);
                tb.Effect = null;
            }
            else if (ComponentUtils.UPPER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.ToolTip = ComponentUtils.ComponentAccessTypeStringToDescriptionDE(tb.Text);
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

        // USAGE of Access
        private void TextBlock_accessRecord_AppearanceAccToContent(object sender)
        {
            TextBlock tb = sender as TextBlock;
            if (tb == null) return;

            string date = "";
            if (tb.Tag != null)
            {
                if (tb.Tag is ComponentManagerAndAccessFlagDateTimeTriple)
                {
                    ComponentManagerAndAccessFlagDateTimeTriple info = (ComponentManagerAndAccessFlagDateTimeTriple)tb.Tag;
                    DateTime recorded_time = info.AccessTimeStamp_Current;
                    if (recorded_time > DateTime.MinValue)
                        date = recorded_time.ToString(DateTimeFormatInfo.CurrentInfo);
                }
            }

            tb.ToolTip = ComponentUtils.ComponentAccessTypeStringToDescriptionDE(tb.Text, date);
            if (ComponentUtils.ACCESS_NOT_ALLOWED_SINGLE.IsMatch(tb.Text))
            {
                tb.Effect = null;
            }
            else if (ComponentUtils.ACCESS_ALLOWED_SINGLE.IsMatch(tb.Text))
            {
                tb.Effect = new DropShadowEffect()
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FF0000ff"),
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 1,
                    Opacity = 1
                };
            }
            else if (ComponentUtils.ACCESS_RECORDED_SINGLE.IsMatch(tb.Text))
            {
                tb.Effect = new DropShadowEffect()
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FFff7e00"),
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 1,
                    Opacity = 1
                };
            }
        }

        #endregion
    }
}
