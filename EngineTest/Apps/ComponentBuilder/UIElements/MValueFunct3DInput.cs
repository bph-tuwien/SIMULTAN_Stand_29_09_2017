using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Globalization;
using System.ComponentModel;

using ParameterStructure.Parameter;
using ParameterStructure.Values;

namespace ComponentBuilder.UIElements
{
    public class MValueFunct3DInput : TabControl, INotifyPropertyChanged 
    {
        #region STATIC

        protected static readonly IFormatProvider FORMATTER = new NumberFormatInfo();

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

        #region PROPERTIES: Size

        public int NrCellsZ
        {
            get { return (int)GetValue(NrCellsZProperty); }
            set { SetValue(NrCellsZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrCellsZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrCellsZProperty =
            DependencyProperty.Register("NrCellsZ", typeof(int), typeof(MValueFunct3DInput),
            new UIPropertyMetadata(0, new PropertyChangedCallback(NrCellsZPropertyChangedCallback)));

        private static void NrCellsZPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct3DInput instance = d as MValueFunct3DInput;
            if (instance == null) return;
            if (instance.NrCellsZ < 1) return;

            instance.AdaptTabsToNrCellZChange();
            // synchronize axis values with first table
            foreach(MValueFunct2DInput table in instance.tables)
            {
                table.SetAxisValues(instance.tables[0].AxisValues, instance.tables[0].Bounds);
            }           
        }

        public Point4D Bounds
        {
            get { return (Point4D)GetValue(BoundsProperty); }
            set { SetValue(BoundsProperty, value); this.RegisterPropertyChanged("Bounds"); }
        }

        // Using a DependencyProperty as the backing store for Bounds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register("Bounds", typeof(Point4D), typeof(MValueFunct3DInput),
            new UIPropertyMetadata(new Point4D(-1000, 1000, -1000, 1000), new PropertyChangedCallback(BoundsPropertyChangedCallback)));

        private static void BoundsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct3DInput instance = d as MValueFunct3DInput;
            if (instance == null) return;
            if (instance.Bounds == null) return;

            foreach(MValueFunct2DInput table in instance.tables)
            {
                table.Bounds = instance.Bounds;
            }
        }

        #endregion

        #region PROPERTIES: Units

        public string UnitX
        {
            get { return (string)GetValue(UnitXProperty); }
            set { SetValue(UnitXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitXProperty =
            DependencyProperty.Register("UnitX", typeof(string), typeof(MValueFunct3DInput),
            new UIPropertyMetadata("unit x", new PropertyChangedCallback(UnitXPropertyChangedCallback)));

        private static void UnitXPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct3DInput instance = d as MValueFunct3DInput;
            if (instance == null) return;
            foreach (MValueFunct2DInput entry in instance.tables)
            {
                entry.UnitX = instance.UnitX;
            }
        }

        public string UnitY
        {
            get { return (string)GetValue(UnitYProperty); }
            set { SetValue(UnitYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitYProperty =
            DependencyProperty.Register("UnitY", typeof(string), typeof(MValueFunct3DInput),
            new UIPropertyMetadata("unit Y", new PropertyChangedCallback(UnitYPropertyChangedCallback)));

        private static void UnitYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct3DInput instance = d as MValueFunct3DInput;
            if (instance == null) return;
            foreach (MValueFunct2DInput entry in instance.tables)
            {
                entry.UnitY = instance.UnitY;
            }
        }

        public string UnitZ
        {
            get { return (string)GetValue(UnitZProperty); }
            set { SetValue(UnitZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitZProperty =
            DependencyProperty.Register("UnitZ", typeof(string), typeof(MValueFunct3DInput),
            new UIPropertyMetadata("unit Z", new PropertyChangedCallback(UnitZPropertyChangedCallback)));

        private static void UnitZPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct3DInput instance = d as MValueFunct3DInput;
            if (instance == null) return;

            instance.UpdateUnitZTab();
        }

        #endregion

        #region PROPERTIES: New Point

        public bool FinalizeFunction
        {
            get { return (bool)GetValue(FinalizeFunctionProperty); }
            set { SetValue(FinalizeFunctionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FinalizeFunction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FinalizeFunctionProperty =
            DependencyProperty.Register("FinalizeFunction", typeof(bool), typeof(MValueFunct3DInput),
            new UIPropertyMetadata(false, new PropertyChangedCallback(FinalizeFunctionPropertyChangedCallback)));

        private static void FinalizeFunctionPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct3DInput instance = d as MValueFunct3DInput;
            if (instance == null) return;

            int t = instance.SelectedIndex;
            if (-1 < t && t < instance.tables.Count && instance.FinalizeFunction)
            {
                instance.tables[t].FunctionName = instance.FunctionName;
                instance.tables[t].FinalizeFunction = true;
            }
            instance.FinalizeFunction = false;
        }


        public string FunctionName
        {
            get { return (string)GetValue(FunctionNameProperty); }
            set { SetValue(FunctionNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FunctionName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FunctionNameProperty =
            DependencyProperty.Register("FunctionName", typeof(string), typeof(MValueFunct3DInput), 
            new UIPropertyMetadata(null));


        public Point NewPoint
        {
            get { return (Point)GetValue(NewPointProperty); }
            set { SetValue(NewPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NewPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NewPointProperty =
            DependencyProperty.Register("NewPoint", typeof(Point), typeof(MValueFunct3DInput),
            new PropertyMetadata(new Point(-1000, -1000), new PropertyChangedCallback(NewPointPropertyChangedCallback)));

        private static void NewPointPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct3DInput instance = d as MValueFunct3DInput;
            if (instance == null) return;
            if (instance.NewPoint == null) return;

            int t = instance.SelectedIndex;
            if (-1 < t && t < instance.tables.Count)
            {
                instance.tables[t].NewPoint = new Point3D(instance.NewPoint.X, instance.NewPoint.Y, t);
            }
        }

        #endregion

        #region CLASS MEMBERS

        private int nr_cell_z_prev = 0;
        private List<MValueFunct2DInput> tables = new List<MValueFunct2DInput>();

        #endregion

        #region METHODS: UIElements TabItems

        private void AdaptTabsToNrCellZChange()
        {
            int tab_nr_diff = this.NrCellsZ - this.nr_cell_z_prev;
            if (tab_nr_diff == 0) return;

            if (tab_nr_diff > 0)
            {
                // remove the tab holding the unit z label
                int nr_items = this.Items.Count;
                if (nr_items > 1)
                    this.Items.RemoveAt(nr_items - 1);

                // add tabs to existing ones
                for (int i = 0; i < tab_nr_diff; i++)
                {
                    // for each 2D Table -> define a separate TAB
                    TabItem ti = new TabItem();
                    ti.Header = "Tab " + i.ToString();

                    // input for the table names
                    TextBox ti_input = new TextBox();
                    ti_input.MinWidth = 25;
                    ti_input.MinHeight = 15;
                    ti_input.Height = 15;
                    ti_input.FontSize = 10;
                    ti_input.Style = (Style)ti_input.TryFindResource("ValueInput");
                    ti_input.IsEnabled = false;

                    ti.Tag = ti_input;
                    ti.LostFocus += this.tabitem_LostFocus;
                    ti.GotFocus += this.tabitem_GotFocus;

                    // apply style
                    ti.Style = (Style)this.TryFindResource("TabItem_ValueField_Input");

                    // add a new table
                    this.PopulateTabITem(i, ref ti);

                    // add Tab to the TabControl
                    this.Items.Add(ti);
                }

                // add the tab holding the unit z label
                this.AddUnitZTab("unit Z");
            }
            else
            {
                // remove tabs from the back
                int nr_items = this.Items.Count;
                for (int i = nr_items - 2; i > this.NrCellsZ - 1; i--)
                {
                    TabItem ti = this.Items[i] as TabItem;
                    if (ti != null)
                    {
                        MValueFunct2DInput ti_table = ti.Content as MValueFunct2DInput;
                        if (ti_table != null)
                        {
                            ti_table.PropertyChanged -= table_PropertyChanged;
                            this.tables.Remove(ti_table);
                        }
                    }
                    this.Items.RemoveAt(i);
                }
            }

            this.nr_cell_z_prev = this.NrCellsZ;
        }

        private void AddUnitZTab(string _unit_z)
        {
            TabItem ti_label = new TabItem();
            ti_label.Header = _unit_z;
            ti_label.IsEnabled = false;

            TextBlock ti_text = new TextBlock();
            ti_text.MinWidth = 25;
            ti_text.MinHeight = 15;
            ti_text.Height = 15;
            ti_text.FontSize = 10;
            ti_text.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));
            ti_text.Text = this.UnitZ;
            ti_text.IsEnabled = false;
            ti_label.Tag = ti_text;

            ti_label.Style = (Style)this.TryFindResource("TabItem_ValueField_Input");
            this.Items.Add(ti_label);
        }

        private void UpdateUnitZTab()
        {
            foreach (var item in this.Items)
            {
                TabItem ti = item as TabItem;
                if (ti == null) continue;
                if (ti.Tag == null) continue;
                if (!(ti.Tag is TextBlock)) continue;

                TextBlock ti_tb = ti.Tag as TextBlock;
                ti_tb.Text = this.UnitZ;
                break;
            }
        }

        #endregion

        #region METHODS: UIElements TabItem Content

        private void PopulateTabITem(int _tag, ref TabItem tab_item)
        {
            if (tab_item == null) return;

            MValueFunct2DInput f_table = new MValueFunct2DInput();
            f_table.Width = this.Width - 8;
            f_table.Height = this.Height - 24;
            f_table.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            f_table.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            f_table.UnitX = this.UnitX;
            f_table.UnitY = this.UnitY;
            f_table.Tag = _tag;
            f_table.ShowGridLines = false;
            f_table.SnapsToDevicePixels = true;
            f_table.PropertyChanged += table_PropertyChanged;
            tab_item.Content = f_table;

            this.tables.Add(f_table);
        }       

        #endregion

        #region METHODS: Select, Deselect, Delete

        public void Deselect()
        {
            foreach(MValueFunct2DInput table in this.tables)
            {
                table.Deselect();
            }
        }

        public void DeleteSelcted()
        {
            int t = this.SelectedIndex;
            if (-1 < t && t < this.tables.Count)
            {
                this.tables[t].DeleteSelected();
            }
        }

        #endregion

        #region METHODS: Gather info for Creating the 3D Function Field

        public void AssembleFieldInfo(out Point4D bounds, out List<double> zs, out List<List<Point3D>> functions, out List<string> fct_names)
        {
            if (this.tables == null || this.tables.Count < 1)
            {
                bounds = this.Bounds;
                zs = new List<double>();
                functions = new List<List<Point3D>>();
                fct_names = new List<string>();
                return;
            }

            bounds = this.Bounds;
            zs = this.ExtractZValues();
            functions = new List<List<Point3D>>();
            fct_names = new List<string>();
            for (int i = 0; i < this.tables.Count; i++)
            {
                functions.AddRange(this.tables[i].FunctionGraphs);
                fct_names.AddRange(this.tables[i].FunctionNames);
            }
        }

        private List<double> ExtractZValues()
        {
            List<double> zs = new List<double>();
            foreach (var item in this.Items)
            {
                TabItem ti = item as TabItem;
                if (ti == null) continue;
                if (ti.Tag == null) continue;

                TextBox ti_input = ti.Tag as TextBox;
                if (ti_input == null) continue;

                double z_val = 0.0;
                bool success = double.TryParse(ti_input.Text, NumberStyles.Float, MValueFunct3DInput.FORMATTER, out z_val);
                zs.Add(z_val);
            }
            return zs;
        }

        #endregion

        #region METHODS: Get info from existing Function Field

        // number of tables has to be set before this method is called
        public void FillInGraphInfoFromExisting(double _min_x, double _max_x, double _min_y, double _max_y,
                                                List<double> _Zs, List<List<Point3D>> _Functions, List<string> _Fct_Names)
        {
            if (_Functions == null || _Functions.Count < 1 || _Zs == null || _Zs.Count < 1 ||
                _max_x < _min_x || _max_y < _min_y ||
                _Fct_Names == null || _Fct_Names.Count != _Functions.Count) return;

            // z
            for (int i = 0; i < this.Items.Count; i++)
            {
                if (i > _Zs.Count - 1) continue;

                TabItem ti = this.Items[i] as TabItem;
                if (ti == null) continue;

                TextBox ti_input = ti.Tag as TextBox;
                if (ti_input == null) continue;

                ti_input.Text = _Zs[i].ToString();
            }

            // fill in the function graphs
            for(int i = 0; i < this.tables.Count; i++)
            {
                for (int n = 0; n < _Functions.Count; n++ )
                {
                    List<Point3D> funct = _Functions[n];
                    string funct_name = _Fct_Names[n];
                    if (funct != null && funct.Count >= 2 && funct[0].Z == i)
                    {
                        this.tables[i].AddFunction(funct, funct_name);
                    }
                }
            }

            // x, y (can only be preformed once each table has been loaded and is ready for display)
            // the SETTER rescales the axes and redraws all lines in all tables
            this.Bounds = new Point4D(_min_x, _max_x, _min_y, _max_y);           
        }

        #endregion

        #region EVENT HANDLERS

        private void tabitem_LostFocus(object sender, RoutedEventArgs e)
        {
            TabItem ti = sender as TabItem;
            if (ti == null) return;

            TextBox ti_input = ti.Tag as TextBox;
            if (ti_input == null) return;

            ti_input.IsEnabled = false;

            // synchronize table axis values in all tabs
            MValueFunct2DInput ti_table = ti.Content as MValueFunct2DInput;
            if (ti_table == null) return;
            ti_table.SetAxisValues();
        }

        private void tabitem_GotFocus(object sender, RoutedEventArgs e)
        {
            TabItem ti = sender as TabItem;
            if (ti == null) return;

            TextBox ti_input = ti.Tag as TextBox;
            if (ti_input == null) return;

            ti_input.IsEnabled = true;
        }

        private void table_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MValueFunct2DInput table = sender as MValueFunct2DInput;            
            if (table == null || e == null) return;
            if (!(table.Tag is int)) return;
            int table_index = (int)table.Tag;

            // debug
            if (table.AxisValues.Xs.Count > 0)
            {
                var debug = table.AxisValues.Xs[0];
            }
            // debug

            if (e.PropertyName == "AxisValues")
            {
                for (int i = 0; i < this.tables.Count; i++)
                {
                    // update all, icluding the sender
                    this.tables[i].ApplyAxisValues(table.AxisValues);
                }
            }
            else if (e.PropertyName == "Bounds")
            {
                this.Bounds = table.Bounds;
            }

        }

        #endregion

    }
}
