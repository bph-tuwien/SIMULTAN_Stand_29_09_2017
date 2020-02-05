using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;

using ComponentBuilder.WinUtils;

using ParameterStructure.Parameter;
using ParameterStructure.Component;

namespace ComponentBuilder.UIElements
{
    #region HELPER CLASSES
    internal struct IntAndString
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
    }

    internal class PointAndString
    {
        public Point PointValue { get; set; }
        public string StringValue { get; set; }

        public bool Equals(PointAndString pAs)
        {
            if (pAs == null) return false;

            bool equal = true;
            equal &= this.PointValue.X == pAs.PointValue.X && this.PointValue.Y == pAs.PointValue.Y;
            equal &= this.StringValue == pAs.StringValue;
            return equal;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            PointAndString pAs = obj as PointAndString;
            if (pAs == null)
                return false;
            else
                return this.Equals(pAs);
        }

        public override int GetHashCode()
        {
            return this.PointValue.GetHashCode() ^ this.StringValue.GetHashCode();
        }

        public static bool operator==(PointAndString _pAs1, PointAndString _pAs2)
        {
            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(_pAs1, _pAs2)) return true;

            //Check whether any of the compared objects is null. 
            if (Object.ReferenceEquals(_pAs1, null) || Object.ReferenceEquals(_pAs2, null))
                return false;

            return _pAs1.Equals(_pAs2);
        }

        public static bool operator!=(PointAndString _pAs1, PointAndString _pAs2)
        {
            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(_pAs1, _pAs2)) return false;

            //Check whether any of the compared objects is null. 
            if (Object.ReferenceEquals(_pAs1, null) || Object.ReferenceEquals(_pAs2, null))
                return true;

            return !_pAs1.Equals(_pAs2);
        }

    }

    #endregion

    public class ComponentCompPicker : Grid, INotifyPropertyChanged
    {
        #region STATIC

        protected static readonly int MAX_NR_ROWS = 10;
        protected static readonly int IMG_COL_WIDTH = 28;
        protected static readonly int INFO_COL_WIDTH = 60;
        protected static readonly int BIG_INFO_COL_WIDTH = 150;
        protected static readonly int MID_INFO_COL_WIDTH = 90;
        
        protected static readonly int MIN_COL_WIDTH = 28;
        protected static readonly int MIN_ROW_HEIGHT = 30;

        protected static readonly int MAX_COL_WIDTH = 90;
        protected static readonly int MAX_ROW_HEIGHT = 40;

        protected const string NAME_TEXTBOX_NR_OUPUT_PARAMS = "txt_NR_OUT";
        protected const int MAX_NR_PARAMS = 20;

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

        #region PROPERTIES: UserRole

        public ComponentManagerType UserRole
        {
            get { return (ComponentManagerType)GetValue(UserRoleProperty); }
            set { SetValue(UserRoleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserRole.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserRoleProperty =
            DependencyProperty.Register("UserRole", typeof(ComponentManagerType), typeof(ComponentCompPicker), 
            new UIPropertyMetadata(ComponentManagerType.GUEST));

        #endregion

        #region PROPERTIES: Component Picking

        public ICommand PickCompCOPYCmd
        {
            get { return (ICommand)GetValue(PickCompCOPYCmdProperty); }
            set { SetValue(PickCompCOPYCmdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PickCompCOPYCmd.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PickCompCOPYCmdProperty =
            DependencyProperty.Register("PickCompCOPYCmd", typeof(ICommand), typeof(ComponentCompPicker),
            new UIPropertyMetadata(null, new PropertyChangedCallback(PickCompCOPYCmdPropertyChangedCallback)));

        private static void PickCompCOPYCmdPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentCompPicker instance = d as ComponentCompPicker;
            if (instance == null) return;

            foreach (object child in instance.Children)
            {
                ToggleButton btn = child as ToggleButton;
                if (btn == null) continue;
                if (btn.Name == null) continue;
                if (btn.Tag == null) continue;
                PointAndString pAs = btn.Tag as PointAndString;
                if (pAs == null) continue;

                if (btn.Name.Contains("COPY"))
                {
                    btn.Command = instance.PickCompCOPYCmd;
                    btn.CommandParameter = pAs.StringValue;
                    btn.IsChecked = instance.IsChecked && instance.tag_current == pAs;
                }
            }
        }


        public ICommand PickCompREFCmd
        {
            get { return (ICommand)GetValue(PickCompREFCmdProperty); }
            set { SetValue(PickCompREFCmdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PickCompREFCmd.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PickCompREFCmdProperty =
            DependencyProperty.Register("PickCompREFCmd", typeof(ICommand), typeof(ComponentCompPicker),
            new UIPropertyMetadata(null, new PropertyChangedCallback(PickCompREFCmdPropertyChangedCallback)));

        private static void PickCompREFCmdPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentCompPicker instance = d as ComponentCompPicker;
            if (instance == null) return;

            foreach (object child in instance.Children)
            {
                ToggleButton btn = child as ToggleButton;
                if (btn == null) continue;
                if (btn.Name == null) continue;
                if (btn.Tag == null) continue;
                PointAndString pAs = btn.Tag as PointAndString;
                if (pAs == null) continue;

                if (btn.Name.Contains("REF"))
                {
                    btn.Command = instance.PickCompREFCmd;
                    btn.CommandParameter = pAs.StringValue;
                    btn.IsChecked = instance.IsChecked && instance.tag_current == pAs;
                }
            }
        }

        #endregion

        #region PROPERTIES: Component Selection

        public ICommand SelectCompREFCmd
        {
            get { return (ICommand)GetValue(SelectCompREFCmdProperty); }
            set { SetValue(SelectCompREFCmdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectCompREFCmd.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectCompREFCmdProperty =
            DependencyProperty.Register("SelectCompREFCmd", typeof(ICommand), typeof(ComponentCompPicker),
            new UIPropertyMetadata(null, new PropertyChangedCallback(SelectCompREFCmdPropertyChangedCallback)));

        private static void SelectCompREFCmdPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentCompPicker instance = d as ComponentCompPicker;
            if (instance == null) return;

            foreach (object child in instance.Children)
            {
                Button btn = child as Button;
                if (btn == null) continue;
                if (btn.Name == null) continue;
                if (btn.Tag == null) continue;
                if (!(btn.Tag is long)) continue;

                long id = (long)btn.Tag;
                if (btn.Name.Contains("SELECT"))
                {
                    btn.Command = instance.SelectCompREFCmd;
                    btn.CommandParameter = id;
                }
            }
        }

        #endregion

        #region PROPERTIES: State

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsChecked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(ComponentCompPicker),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsCheckedPropertyChangedCallback)));

        private static void IsCheckedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentCompPicker instance = d as ComponentCompPicker;
            if (instance == null) return;

            foreach (object child in instance.Children)
            {
                ToggleButton btn = child as ToggleButton;
                if (btn == null) continue;
                btn.IsChecked = instance.IsChecked && instance.tag_current == (btn.Tag as PointAndString);
            }
        }

        #endregion

        #region PROPERTIES: Component

        public ParameterStructure.Component.Component ComponentUnderConstr
        {
            get { return (ParameterStructure.Component.Component)GetValue(ComponentUnderConstrProperty); }
            set { SetValue(ComponentUnderConstrProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ComponentUnderConstr.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ComponentUnderConstrProperty =
            DependencyProperty.Register("ComponentUnderConstr", typeof(ParameterStructure.Component.Component), typeof(ComponentCompPicker),
            new UIPropertyMetadata(null, new PropertyChangedCallback(ComponentUnderConstrPropertyChangedCallback),
                new CoerceValueCallback(ComponentUnderConstrCoerceValueCallback)));

        private static object ComponentUnderConstrCoerceValueCallback(DependencyObject d, object baseValue)
        {
            ComponentCompPicker instance = d as ComponentCompPicker;
            if (instance == null) return baseValue;

            if (instance.ComponentUnderConstr != null)
                instance.ComponentUnderConstr.PropertyChanged -= instance.compUC_PropertyChanged;

            return baseValue;
        }      

        private static void ComponentUnderConstrPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentCompPicker instance = d as ComponentCompPicker;
            if (instance == null) return;
            if (instance.ComponentUnderConstr == null) return;

            instance.ComponentUnderConstr.PropertyChanged += instance.compUC_PropertyChanged;

            instance.nr_rows_COPY = instance.ComponentUnderConstr.ContainedComponents.Count();
            instance.nr_rows_REF = instance.ComponentUnderConstr.ReferencedComponents.Count();

            instance.key_order_COPY = new SortedDictionary<int, string>();
            SortedDictionary<string, ParameterStructure.Component.Component> sorted_subComps = 
                new SortedDictionary<string, ParameterStructure.Component.Component>(instance.ComponentUnderConstr.ContainedComponents); // added 31.08.2017
            for (int i = 0; i < instance.nr_rows_COPY; i++ )
            {
                instance.key_order_COPY.Add(i, sorted_subComps.ElementAt(i).Key);
            }

            instance.key_order_REF = new SortedDictionary<int, string>();
            SortedDictionary<string, ParameterStructure.Component.Component> sorted_refComps =
                new SortedDictionary<string, ParameterStructure.Component.Component>(instance.ComponentUnderConstr.ReferencedComponents); // added 31.08.2017
            for (int i = 0; i < instance.nr_rows_REF; i++)
            {
                instance.key_order_REF.Add(i, sorted_refComps.ElementAt(i).Key);
            }

            instance.Update();
        }

        #endregion

        #region CLASS MEMBERS

        private List<string> slots_generic = new List<string>(ComponentUtils.COMP_SLOTS_ALL);
        private ICommand AddCOPYSlotCmd;
        private ICommand AddREFSlotCmd;
        private ICommand RemoveSlotCmd;
        private ICommand RemoveCompCmd;

        private bool size_original_set = false;
        protected double width_original = 0;
        protected double height_original = 0;

        protected int nr_columns = 5;
        protected int nr_rows_COPY = 0;
        protected int nr_rows_REF = 0;
        protected int column_width = 0;
        protected int row_height = 0;

        private SortedDictionary<int, string> key_order_COPY;
        private SortedDictionary<int, string> key_order_REF;
        private PointAndString tag_current;

        #endregion

        public ComponentCompPicker()
            :base()
        {
            this.AddCOPYSlotCmd = new RelayCommand((x) => OnAddCOPYSlot());
            this.AddREFSlotCmd = new RelayCommand((x) => OnAddREFSlot());
            this.RemoveSlotCmd = new RelayCommand((x) => OnRemoveComponent(x, true));
            this.RemoveCompCmd = new RelayCommand((x) => OnRemoveComponent(x, false));
        }

        #region METHODS: Update

        private void Update()
        {
            this.ResetGrid();
            this.RecalculateSizes();
            this.RedefineSymbolGrid();
            this.PopulateGrid();
            // etc ...
        }

        #endregion

        #region METHODS: Grid Reset, Resize

        protected void ResetGrid()
        {
            this.Children.Clear();
            this.RowDefinitions.Clear();
            this.ColumnDefinitions.Clear();
        }

        protected void RecalculateSizes()
        {
            // set the original sizes, if not set already
            if (!this.size_original_set)
            {
                this.width_original = this.Width;
                this.height_original = this.Height;
                this.size_original_set = true;
            }

            // save cell nr
            int col_B = Math.Max((int)Math.Floor((this.width_original - ComponentCompPicker.IMG_COL_WIDTH * 2 - ComponentCompPicker.BIG_INFO_COL_WIDTH - ComponentCompPicker.INFO_COL_WIDTH) / (this.nr_columns - 4)), ComponentCompPicker.MIN_COL_WIDTH);
            col_B = Math.Min(col_B, ComponentCompPicker.MAX_COL_WIDTH);
            int row_H = Math.Max((int)Math.Floor(this.height_original / (this.nr_rows_COPY + this.nr_rows_REF + 2)), ComponentCompPicker.MIN_ROW_HEIGHT);
            row_H = Math.Min(row_H, ComponentCompPicker.MAX_ROW_HEIGHT);

            // re-calculate the size of the grid
            this.Width = ComponentCompPicker.IMG_COL_WIDTH * 2 + ComponentCompPicker.BIG_INFO_COL_WIDTH + ComponentCompPicker.INFO_COL_WIDTH + (this.nr_columns - 4) * col_B;
            this.Height = (this.nr_rows_COPY + this.nr_rows_REF + 2) * row_H;

            // save cell sizes            
            this.column_width = col_B;
            this.row_height = row_H;
        }

        protected void RedefineSymbolGrid()
        {
            // COLUMN for the slot symbol
            ColumnDefinition cdIM = new ColumnDefinition();
            cdIM.Width = new GridLength(ComponentCompPicker.IMG_COL_WIDTH);
            this.ColumnDefinitions.Add(cdIM);

            // COLUMN for choosing a slot from a listbox
            ColumnDefinition cd0 = new ColumnDefinition();
            cd0.Width = new GridLength(ComponentCompPicker.BIG_INFO_COL_WIDTH);
            this.ColumnDefinitions.Add(cd0);

            // COLUMN for the additional string
            ColumnDefinition cd1 = new ColumnDefinition();
            cd1.Width = new GridLength(ComponentCompPicker.INFO_COL_WIDTH);
            this.ColumnDefinitions.Add(cd1);

            // COLUMN for the button to pick a component
            ColumnDefinition cd2 = new ColumnDefinition();
            cd2.Width = new GridLength(ComponentCompPicker.IMG_COL_WIDTH);
            this.ColumnDefinitions.Add(cd2);

            // COLUMN to hold the button for selection of a referenced component
            ColumnDefinition cd3 = new ColumnDefinition();
            cd3.Width = new GridLength();
            this.ColumnDefinitions.Add(cd3);

            // ROW to hold the button for adding COPY slots
            RowDefinition rdCOPY = new RowDefinition();
            rdCOPY.Height = new GridLength(this.row_height);
            this.RowDefinitions.Add(rdCOPY);

            // <slot,SUB-component> - Tuples
            for (int i = 0; i < this.nr_rows_COPY; i++)
            {
                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(this.row_height);
                this.RowDefinitions.Add(rd);
            }

            // ROW to hold the button for adding REF slots
            RowDefinition rdREF = new RowDefinition();
            rdREF.Height = new GridLength(this.row_height);
            this.RowDefinitions.Add(rdREF);

            // <slot,REF-component> - Tuples
            for (int i = 0; i < this.nr_rows_REF; i++)
            {
                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(this.row_height);
                this.RowDefinitions.Add(rd);
            }
            
        }

        #endregion

        #region METHODS: Grid Realize

        private void PopulateGrid()
        {
            // buttons for adding and removing slots
            this.PopulateAddRemoveButtons();

            // actual slots and chosen SUB-components
            this.PopulateSubComponents();

            // actual slots and chosen component REFERENCES
            this.PopulateComponentRefs();
        }

        
        private void PopulateAddRemoveButtons()
        {
            // SUB-COMPONENTS
            Button btn_COPY_add = new Button();
            btn_COPY_add.Style = (Style)btn_COPY_add.TryFindResource("ReliefButton");
            Image im_COPY_add = new Image();
            im_COPY_add.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_getSlot.png", UriKind.Relative));
            btn_COPY_add.Content = im_COPY_add;
            btn_COPY_add.Height = 26;
            btn_COPY_add.Width = 26;
            btn_COPY_add.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            btn_COPY_add.ToolTip = "Subkomponenten-Platzhalter hinzufügen";
            btn_COPY_add.Command = this.AddCOPYSlotCmd;

            Grid.SetColumn(btn_COPY_add, 0);
            Grid.SetRow(btn_COPY_add, 0);
            this.Children.Add(btn_COPY_add);

            // REFERENCED COMPONENTS
            Button btn_REF = new Button();
            btn_REF.Style = (Style)btn_REF.TryFindResource("ReliefButton");
            Image im_REF = new Image();
            im_REF.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_getSlot_REF.png", UriKind.Relative));
            btn_REF.Content = im_REF;
            btn_REF.Height = 26;
            btn_REF.Width = 26;
            btn_REF.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            btn_REF.ToolTip = "Referenzkomponenten-Platzhalter hinzufügen";
            btn_REF.Command = this.AddREFSlotCmd;

            Grid.SetColumn(btn_REF, 0);
            Grid.SetRow(btn_REF, this.nr_rows_COPY + 1);
            this.Children.Add(btn_REF);
        }

        private void PopulateSubComponents()
        {
            for (int i = 0; i < this.nr_rows_COPY; i++)
            {
                // split the KEY in its components
                int index = this.key_order_COPY.ElementAt(i).Key;
                string key = this.key_order_COPY.ElementAt(i).Value;
                int key_delim = key.IndexOf(ComponentUtils.COMP_SLOT_DELIMITER);
                if (key_delim < 0 || key_delim >= key.Length) continue;

                string key_main = key.Substring(0, key_delim);
                string key_suppl = key.Substring(key_delim + ComponentUtils.COMP_SLOT_DELIMITER.Length);

                // slot symbol
                Button btn_del = new Button();
                btn_del.Style = (Style)btn_del.TryFindResource("ReliefHiddenBlueButton");
                Image im_del_slot = new Image();
                if (this.ComponentUnderConstr.ContainedComponents.ContainsKey(key) &&
                    this.ComponentUnderConstr.ContainedComponents[key] != null)
                {
                    im_del_slot.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_slot_Copy_FF.png", UriKind.Relative));
                }
                else
                {
                    im_del_slot.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_slot_Copy_E.png", UriKind.Relative));
                }
                btn_del.Content = im_del_slot;
                btn_del.Height = 26;
                btn_del.Width = 26;
                btn_del.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                btn_del.ToolTip = "Subkomponenten-Platzhalter entfernen";
                btn_del.Command = this.RemoveSlotCmd;
                btn_del.CommandParameter = new Point(0, index);

                Grid.SetColumn(btn_del, 0);
                Grid.SetRow(btn_del, i + 1);
                this.Children.Add(btn_del);

                // choose main slot string
                ComboBox cb = new ComboBox();
                cb.ItemsSource = this.slots_generic;
                cb.Height = 26;
                cb.SelectedItem = key_main;
                cb.Tag = new PointAndString { PointValue = new Point(0, index), StringValue = key };
                cb.SelectionChanged += cbCOPY_SelectionChanged;

                Grid.SetColumn(cb, 1);
                Grid.SetRow(cb, i + 1);
                this.Children.Add(cb);

                // type in additional slot string
                TextBox tb = new TextBox();
                tb.Style = (Style)tb.TryFindResource("ValueInput");
                tb.Height = 25;
                tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                tb.Margin = new Thickness(5, 2, 5, 2);
                tb.Text = key_suppl;
                tb.Tag = new PointAndString { PointValue = new Point(0, index), StringValue = key };
                tb.LostFocus += tbCOPY_LostFocus;

                Grid.SetColumn(tb, 2);
                Grid.SetRow(tb, i + 1);
                this.Children.Add(tb);

                // button for adding the component
                ToggleButton tbtn = new ToggleButton();
                tbtn.Style = (Style)tbtn.TryFindResource("ToggleButtonRed");
                Image tbtn_im = new Image();
                tbtn_im.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_getComp.png", UriKind.Relative));
                tbtn.Content = tbtn_im;
                tbtn.Height = 26;
                tbtn.Width = 26;
                tbtn.Name = "COPY_" + (index + 1).ToString();
                tbtn.Tag = new PointAndString { PointValue = new Point(0, index), StringValue = key };
                tbtn.ToolTip = "Subkomponente von der Liste kopieren";
                tbtn.Command = this.PickCompCOPYCmd;
                tbtn.CommandParameter = key;
                tbtn.IsChecked = this.IsChecked && this.tag_current == (tbtn.Tag as PointAndString);
                tbtn.Click += tbtn_Assign_Comp_Click;

                Grid.SetColumn(tbtn, 3);
                Grid.SetRow(tbtn, i + 1);
                this.Children.Add(tbtn);

                // button for removing the component (added 31.10.2016)
                bool sub_comp_full = (this.ComponentUnderConstr.ContainedComponents.ContainsKey(key) && this.ComponentUnderConstr.ContainedComponents[key] != null);
                Button btn_del_only_comp = new Button();
                btn_del_only_comp.Style = (Style)btn_del_only_comp.TryFindResource("ReliefButton");
                Image im_del = new Image();
                im_del.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_delComp.png", UriKind.Relative));
                btn_del_only_comp.Content = im_del;
                btn_del_only_comp.Height = 26;
                btn_del_only_comp.Width = 26;
                btn_del_only_comp.Name = "DEL_" + (index + 1).ToString();
                btn_del_only_comp.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                btn_del_only_comp.ToolTip = "Subkomponente entfernen, Platzhalter behalten";
                btn_del_only_comp.IsEnabled = sub_comp_full;
                btn_del_only_comp.Command = this.RemoveCompCmd;
                btn_del_only_comp.CommandParameter = new Point(0, index);

                Grid.SetColumn(btn_del_only_comp, 4);
                Grid.SetRow(btn_del_only_comp, i + 1);
                this.Children.Add(btn_del_only_comp);
            }
        }

        private void PopulateComponentRefs()
        {
            for (int i = 0; i < this.nr_rows_REF; i++)
            {
                // split the KEY in its components
                int index = this.key_order_REF.ElementAt(i).Key;
                string key = this.key_order_REF.ElementAt(i).Value;
                int key_delim = key.IndexOf(ComponentUtils.COMP_SLOT_DELIMITER);
                if (key_delim < 0 || key_delim >= key.Length) continue;

                string key_main = key.Substring(0, key_delim);
                string key_suppl = key.Substring(key_delim + ComponentUtils.COMP_SLOT_DELIMITER.Length);

                // slot symbol
                Button btn_del = new Button();
                btn_del.Style = (Style)btn_del.TryFindResource("ReliefHiddenBlueButton");
                Image im_del_slot = new Image();
                if (this.ComponentUnderConstr.ReferencedComponents.ContainsKey(key) &&
                    this.ComponentUnderConstr.ReferencedComponents[key] != null)
                {
                    im_del_slot.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_slot_Ref_FF.png", UriKind.Relative));
                }
                else
                {
                    im_del_slot.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_slot_Ref_E.png", UriKind.Relative));
                }
                btn_del.Content = im_del_slot;
                btn_del.Height = 26;
                btn_del.Width = 26;
                btn_del.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                btn_del.ToolTip = "Referenzkomponenten-Platzhalter entfernen";
                btn_del.Command = this.RemoveSlotCmd;
                btn_del.CommandParameter = new Point(1, index);

                Grid.SetColumn(btn_del, 0);
                Grid.SetRow(btn_del, this.nr_rows_COPY + i + 2);
                this.Children.Add(btn_del);

                // choose main slot string
                ComboBox cb = new ComboBox();
                cb.ItemsSource = this.slots_generic;
                cb.Height = 26;
                cb.SelectedItem = key_main;
                cb.Tag = new PointAndString { PointValue = new Point(1, index), StringValue = key };
                cb.SelectionChanged += cbREF_SelectionChanged;

                Grid.SetColumn(cb, 1);
                Grid.SetRow(cb, this.nr_rows_COPY + i + 2);
                this.Children.Add(cb);

                // type in additional slot string
                TextBox tb = new TextBox();
                tb.Style = (Style)tb.TryFindResource("ValueInput");
                tb.Height = 25;
                tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                tb.Margin = new Thickness(5, 2, 5, 2);
                tb.Text = key_suppl;
                tb.Tag = new PointAndString { PointValue = new Point(1, index), StringValue = key };
                tb.LostFocus += tbREF_LostFocus;

                Grid.SetColumn(tb, 2);
                Grid.SetRow(tb, this.nr_rows_COPY + i + 2);
                this.Children.Add(tb);

                // button for adding the component
                ToggleButton tbtn = new ToggleButton();
                tbtn.Style = (Style)tbtn.TryFindResource("ToggleButtonRed");
                Image tbtn_im = new Image();
                tbtn_im.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_getComp.png", UriKind.Relative));
                tbtn.Content = tbtn_im;
                tbtn.Height = 26;
                tbtn.Width = 26;
                tbtn.Name = "REF_" + (index + 1).ToString();
                tbtn.Tag = new PointAndString { PointValue = new Point(1, index), StringValue = key };
                tbtn.ToolTip = "Referenzierte Komponente von der Liste auswählen";
                tbtn.Command = this.PickCompREFCmd;
                tbtn.CommandParameter = key;
                tbtn.IsChecked = this.IsChecked && this.tag_current == (tbtn.Tag as PointAndString);
                tbtn.Click += tbtn_Assign_Comp_Click;

                Grid.SetColumn(tbtn, 3);
                Grid.SetRow(tbtn, this.nr_rows_COPY + i + 2);
                this.Children.Add(tbtn);

                // button for selecting the referenced component
                bool ref_comp_full = (this.ComponentUnderConstr.ReferencedComponents.ContainsKey(key) && this.ComponentUnderConstr.ReferencedComponents[key] != null);
                Button btn_sel = new Button();
                btn_sel.Style = (Style)btn_sel.TryFindResource("ReliefButton");
                Image im_sel = new Image();
                im_sel.Source = new BitmapImage(new Uri(@"./Data/icons/xcomp_slot_Ref_Select.png", UriKind.Relative));
                btn_sel.Content = im_sel;
                btn_sel.Height = 26;
                btn_sel.Width = 26;
                btn_sel.Name = "SELECT_" + (index + 1).ToString();
                btn_sel.Tag = ref_comp_full ? this.ComponentUnderConstr.ReferencedComponents[key].ID : -1;
                btn_sel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                btn_sel.ToolTip = "Zeige Referenzierte Komponente";
                btn_sel.IsEnabled = ref_comp_full;
                btn_sel.Command = this.SelectCompREFCmd;
                btn_sel.CommandParameter = ref_comp_full ? this.ComponentUnderConstr.ReferencedComponents[key].ID : -1;

                Grid.SetColumn(btn_sel, 4);
                Grid.SetRow(btn_sel, this.nr_rows_COPY + i + 2);
                this.Children.Add(btn_sel);

                // name of referenced component
                if (ref_comp_full)
                {
                    TextBlock tb_rC = new TextBlock();
                    tb_rC.Width = ComponentCompPicker.MID_INFO_COL_WIDTH;
                    tb_rC.IsHitTestVisible = false;
                    tb_rC.Foreground = new SolidColorBrush(Colors.Blue);
                    tb_rC.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                    tb_rC.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    tb_rC.FontSize = 9;
                    tb_rC.FontWeight = FontWeights.Bold;
                    tb_rC.Text = "[" + this.ComponentUnderConstr.ReferencedComponents[key].Name + "]";
                    tb_rC.TextAlignment = TextAlignment.Right;

                    Grid.SetColumn(tb_rC, 4);
                    Grid.SetRow(tb_rC, this.nr_rows_COPY + i + 2);
                    this.Children.Add(tb_rC);
                }
            }
        }

        #endregion

        #region COMMANDS

        private void OnAddCOPYSlot()
        {
            if (this.ComponentUnderConstr == null) return;

            // define the NEW UNIQUE key
            int counter = 1;
            string new_key = this.slots_generic[0] + ComponentUtils.COMP_SLOT_DELIMITER + counter.ToString();
            while(this.ComponentUnderConstr.ContainedComponents.ContainsKey(new_key))
            {
                counter++;
                new_key = this.slots_generic[0] + ComponentUtils.COMP_SLOT_DELIMITER + counter.ToString();
            }
            //this.ComponentUnderConstr.ContainedComponents.Add(new_key, null); // OLD
            bool success = this.ComponentUnderConstr.AddSubComponentSlot(new_key, this.UserRole); // NEW
            if (success)
            {
                this.key_order_COPY.Add(this.nr_rows_COPY, new_key);
                this.nr_rows_COPY++;
                this.Update();
            }
        }

        private void OnAddREFSlot()
        {
            if (this.ComponentUnderConstr == null) return;

            // define the NEW UNIQUE key
            int counter = 1;
            string new_key = this.slots_generic[0] + ComponentUtils.COMP_SLOT_DELIMITER + counter.ToString();
            while (this.ComponentUnderConstr.ReferencedComponents.ContainsKey(new_key))
            {
                counter++;
                new_key = this.slots_generic[0] + ComponentUtils.COMP_SLOT_DELIMITER + counter.ToString();
            }
            //this.ComponentUnderConstr.ReferencedComponents.Add(new_key, null); // OLD
            bool success = this.ComponentUnderConstr.AddReferencedComponentSlot(new_key, this.UserRole); // NEW
            if (success)
            {
                this.key_order_REF.Add(this.nr_rows_REF, new_key);
                this.nr_rows_REF++;
                this.Update();
            }
        }

        private void OnRemoveComponent(object _slot, bool _remove_slot)
        {
            if (_slot == null) return;
            if (!(_slot is Point)) return;
            if (this.ComponentUnderConstr == null) return;

            Point indices = (Point)_slot;
            int type = (int)indices.X;
            int index = (int)indices.Y;

            if (type == 0)
            {
                string key = key_order_COPY[index];
                if (!this.ComponentUnderConstr.ContainedComponents.ContainsKey(key)) return;
                bool success = this.ComponentUnderConstr.RemoveSubComponent_Level0(key, _remove_slot, this.UserRole); // changed 31.10.2016
                if (_remove_slot && success)
                {
                    this.key_order_COPY.Remove(index);
                    this.key_order_COPY = ComponentCompPicker.CompressKeys(this.key_order_COPY);
                    this.nr_rows_COPY--;
                }
            }
            else if (type == 1)
            {
                string key = key_order_REF[index];
                if (!this.ComponentUnderConstr.ReferencedComponents.ContainsKey(key)) return;
                bool success = this.ComponentUnderConstr.RemoveReferencedComponent_Level0(key, _remove_slot, this.UserRole); // changed 31.10.2016
                if (_remove_slot && success)
                {
                    this.key_order_REF.Remove(index);
                    this.key_order_REF = ComponentCompPicker.CompressKeys(this.key_order_REF);
                    this.nr_rows_REF--;
                }
            }

            this.Update();
        }

        #endregion

        #region EVENT HANDLERS

        private void compUC_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ParameterStructure.Component.Component comp = sender as ParameterStructure.Component.Component;
            if (comp == null || e == null) return;

            if (e.PropertyName == "ContainedComponents" || e.PropertyName == "ReferencedComponents")
            {
                this.Update();
            }
        }

        private void cbCOPY_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null) return;
            if (cb.SelectedItem == null) return;
            if (cb.Tag == null) return;
            if (!(cb.Tag is PointAndString)) return;

            PointAndString index_key = (PointAndString)cb.Tag;
            double type = index_key.PointValue.X;
            if (type != 0) return;

            int index = (int)index_key.PointValue.Y;
            string key = index_key.StringValue;
            if (!this.ComponentUnderConstr.ContainedComponents.ContainsKey(key)) return;

            string[] key_components = key.Split(new string[] { ComponentUtils.COMP_SLOT_DELIMITER }, StringSplitOptions.RemoveEmptyEntries);
            string key_cb = string.Empty;
            if (key_components.Length == 2)
                key_cb = key_components[0];

            string key_new = key;
            foreach(object child in this.Children)
            {
                TextBox tb = child as TextBox;
                if (tb == null) continue;
                if (tb.Tag == null) continue;
                if (!(tb.Tag is PointAndString)) continue;

                PointAndString tb_index_key = (PointAndString)tb.Tag;
                double tb_type = tb_index_key.PointValue.X;
                if (tb_type != 0) continue;

                int tb_index = (int)tb_index_key.PointValue.Y;
                string tb_key = tb_index_key.StringValue;
                if (tb_index != index || tb_key != key) continue;

                string key_new_candidate = cb.SelectedItem.ToString() + ComponentUtils.COMP_SLOT_DELIMITER + tb.Text;
                if (this.key_order_COPY.ContainsValue(key_new_candidate))
                {
                    cb.SelectedItem = key_cb;
                    break;
                }

                key_new = key_new_candidate;

                // update the tags!!!
                tb.Tag = new PointAndString { PointValue = index_key.PointValue, StringValue = key_new };
                cb.Tag = new PointAndString { PointValue = index_key.PointValue, StringValue = key_new };
                break;
            }

            if (key_new == key) return;

            // communicate the change to the Component
            //var subcomp = this.ComponentUnderConstr.ContainedComponents[key]; // OLD
            //this.ComponentUnderConstr.ContainedComponents.Remove(key); // OLD
            //this.ComponentUnderConstr.ContainedComponents.Add(key_new, subcomp); // OLD
            this.ComponentUnderConstr.RenameSubComponentSlot_Level0(key, key_new, this.UserRole); // NEW
       
            // preserve the key order for DISPLAY
            this.key_order_COPY.Remove((int)index_key.PointValue.Y);
            this.key_order_COPY.Add((int)index_key.PointValue.Y, key_new);  
        }


        private void tbCOPY_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;
            if (tb.Tag == null) return;
            if (!(tb.Tag is PointAndString)) return;

            PointAndString index_key = (PointAndString)tb.Tag;
            double type = index_key.PointValue.X;
            if (type != 0) return;

            int index = (int)index_key.PointValue.Y;
            string key = index_key.StringValue;
            if (!this.ComponentUnderConstr.ContainedComponents.ContainsKey(key)) return;

            string[] key_components = key.Split(new string[] { ComponentUtils.COMP_SLOT_DELIMITER }, StringSplitOptions.RemoveEmptyEntries);
            string key_tb = string.Empty;
            if (key_components.Length == 2)
                key_tb = key_components[1];

            string key_new = key;
            foreach (object child in this.Children)
            {
                ComboBox cb = child as ComboBox;
                if (cb == null) continue;
                if (cb.SelectedItem == null) continue;
                if (cb.Tag == null) continue;
                if (!(cb.Tag is PointAndString)) continue;

                PointAndString cb_index_key = (PointAndString)cb.Tag;
                double cb_type = cb_index_key.PointValue.X;
                if (cb_type != 0) continue;

                int cb_index = (int)cb_index_key.PointValue.Y;
                string cb_key = cb_index_key.StringValue;
                if (cb_index != index || cb_key != key) continue;

                string key_new_candidate = cb.SelectedItem.ToString() + ComponentUtils.COMP_SLOT_DELIMITER + tb.Text;
                if (this.key_order_COPY.ContainsValue(key_new_candidate))
                {
                    tb.Text = (string.IsNullOrEmpty(key_tb)) ? "*" : key_tb;
                    break; 
                }

                key_new = key_new_candidate;

                // update the tags!!!
                tb.Tag = new PointAndString { PointValue = index_key.PointValue, StringValue = key_new };
                cb.Tag = new PointAndString { PointValue = index_key.PointValue, StringValue = key_new };
                break;
            }

            if (key_new == key) return;

            // communicate the change to the Component
            //var subcomp = this.ComponentUnderConstr.ContainedComponents[key]; // OLD
            //this.ComponentUnderConstr.ContainedComponents.Remove(key); // OLD
            //this.ComponentUnderConstr.ContainedComponents.Add(key_new, subcomp); // OLD
            this.ComponentUnderConstr.RenameSubComponentSlot_Level0(key, key_new, this.UserRole); // NEW

            // preserve the key order for DISPLAY
            this.key_order_COPY.Remove((int)index_key.PointValue.Y);
            this.key_order_COPY.Add((int)index_key.PointValue.Y, key_new);
        }

        private void cbREF_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null) return;
            if (cb.SelectedItem == null) return;
            if (cb.Tag == null) return;
            if (!(cb.Tag is PointAndString)) return;

            PointAndString index_key = (PointAndString)cb.Tag;
            double type = index_key.PointValue.X;
            if (type != 1) return;

            int index = (int)index_key.PointValue.Y;
            string key = index_key.StringValue;
            if (!this.ComponentUnderConstr.ReferencedComponents.ContainsKey(key)) return;

            string[] key_components = key.Split(new string[] { ComponentUtils.COMP_SLOT_DELIMITER }, StringSplitOptions.RemoveEmptyEntries);
            string key_cb = string.Empty;
            if (key_components.Length == 2)
                key_cb = key_components[0];

            string key_new = key;
            foreach (object child in this.Children)
            {
                TextBox tb = child as TextBox;
                if (tb == null) continue;
                if (tb.Tag == null) continue;
                if (!(tb.Tag is PointAndString)) continue;

                PointAndString tb_index_key = (PointAndString)tb.Tag;
                double tb_type = tb_index_key.PointValue.X;
                if (tb_type != 1) continue;

                int tb_index = (int)tb_index_key.PointValue.Y;
                string tb_key = tb_index_key.StringValue;
                if (tb_index != index || tb_key != key) continue;

                string key_new_candidate = cb.SelectedItem.ToString() + ComponentUtils.COMP_SLOT_DELIMITER + tb.Text;
                if (this.key_order_REF.ContainsValue(key_new_candidate))
                {
                    cb.SelectedItem = key_cb;
                    break;
                }

                key_new = key_new_candidate;

                // update the tags!!!
                tb.Tag = new PointAndString { PointValue = index_key.PointValue, StringValue = key_new };
                cb.Tag = new PointAndString { PointValue = index_key.PointValue, StringValue = key_new };
                break;
            }

            if (key_new == key) return;

            // communicate the change to the Component
            //var subcomp = this.ComponentUnderConstr.ReferencedComponents[key]; // OLD
            //this.ComponentUnderConstr.ReferencedComponents.Remove(key); // OLD
            //this.ComponentUnderConstr.ReferencedComponents.Add(key_new, subcomp); // OLD
            this.ComponentUnderConstr.RenameReferencedComponentSlot_Level0(key, key_new, this.UserRole); // NEW

            // preserve the key order for DISPLAY
            this.key_order_REF.Remove((int)index_key.PointValue.Y);
            this.key_order_REF.Add((int)index_key.PointValue.Y, key_new);
        }

        private void tbREF_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;
            if (tb.Tag == null) return;
            if (!(tb.Tag is PointAndString)) return;

            PointAndString index_key = (PointAndString)tb.Tag;
            double type = index_key.PointValue.X;
            if (type != 1) return;

            int index = (int)index_key.PointValue.Y;
            string key = index_key.StringValue;
            if (!this.ComponentUnderConstr.ReferencedComponents.ContainsKey(key)) return;

            string[] key_components = key.Split(new string[] { ComponentUtils.COMP_SLOT_DELIMITER }, StringSplitOptions.RemoveEmptyEntries);
            string key_tb = string.Empty;
            if (key_components.Length == 2)
                key_tb = key_components[1];

            string key_new = key;
            foreach (object child in this.Children)
            {
                ComboBox cb = child as ComboBox;
                if (cb == null) continue;
                if (cb.SelectedItem == null) continue;
                if (cb.Tag == null) continue;
                if (!(cb.Tag is PointAndString)) continue;

                PointAndString cb_index_key = (PointAndString)cb.Tag;
                double cb_type = cb_index_key.PointValue.X;
                if (cb_type != 1) continue;

                int cb_index = (int)cb_index_key.PointValue.Y;
                string cb_key = cb_index_key.StringValue;
                if (cb_index != index || cb_key != key) continue;

                string key_new_candidate = cb.SelectedItem.ToString() + ComponentUtils.COMP_SLOT_DELIMITER + tb.Text;
                if (this.key_order_REF.ContainsValue(key_new_candidate))
                {
                    tb.Text = (string.IsNullOrEmpty(key_tb)) ? "*" : key_tb;
                    break;
                }

                key_new = key_new_candidate;

                // update the tags!!!
                tb.Tag = new PointAndString { PointValue = index_key.PointValue, StringValue = key_new };
                cb.Tag = new PointAndString { PointValue = index_key.PointValue, StringValue = key_new };
                break;
            }

            if (key_new == key) return;

            // communicate the change to the Component
            //var subcomp = this.ComponentUnderConstr.ReferencedComponents[key]; // OLD
            //this.ComponentUnderConstr.ReferencedComponents.Remove(key); // OLD
            //this.ComponentUnderConstr.ReferencedComponents.Add(key_new, subcomp); // OLD
            this.ComponentUnderConstr.RenameReferencedComponentSlot_Level0(key, key_new, this.UserRole); // NEW

            // preserve the key order for DISPLAY
            this.key_order_REF.Remove((int)index_key.PointValue.Y);
            this.key_order_REF.Add((int)index_key.PointValue.Y, key_new);
        }

        private void tbtn_Assign_Comp_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = sender as ToggleButton;
            if (tbtn == null) return;
            if (tbtn.Tag == null) return;

            PointAndString pAs = tbtn.Tag as PointAndString;
            if (pAs == null) return;

            this.tag_current = pAs;
        }

        #endregion

        #region UTILS

        protected static SortedDictionary<int, string> CompressKeys(SortedDictionary<int, string> _to_compress)
        {
            SortedDictionary<int, string> compressed = new SortedDictionary<int, string>();
            if (_to_compress == null || _to_compress.Count == 0) return compressed;

            int key_prev = -1;
            foreach(var entry in _to_compress)
            {
                int key = entry.Key;

                if (key == key_prev + 1)
                    compressed.Add(key, entry.Value);
                else if (key > key_prev + 1)
                    compressed.Add(key_prev + 1, entry.Value);

                key_prev++;
            }

            return compressed;
        }

        #endregion
    }
}
