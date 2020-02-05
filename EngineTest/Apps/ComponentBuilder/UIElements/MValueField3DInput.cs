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
    public class MValueField3DInput : TabControl
    {
        #region STATIC

        protected static readonly IFormatProvider FORMATTER = new NumberFormatInfo();

        #endregion

        #region PROPERTIES: Size

        public int NrCellsX
        {
            get { return (int)GetValue(NrCellsXProperty); }
            set { SetValue(NrCellsXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrCellsX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrCellsXProperty =
            DependencyProperty.Register("NrCellsX", typeof(int), typeof(MValueField3DInput),
            new UIPropertyMetadata(0, new PropertyChangedCallback(NrCellsXPropertyChangedCallback)));

        private static void NrCellsXPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField3DInput instance = d as MValueField3DInput;
            if (instance == null) return;
            if (instance.NrCellsX < 1) return;
            foreach (MValueField2DInput entry in instance.tables)
            {
                entry.NrCellsX = instance.NrCellsX;
            }
        }

        public int NrCellsY
        {
            get { return (int)GetValue(NrCellsYProperty); }
            set { SetValue(NrCellsYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrCellsY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrCellsYProperty =
            DependencyProperty.Register("NrCellsY", typeof(int), typeof(MValueField3DInput),
            new UIPropertyMetadata(0, new PropertyChangedCallback(NrCellsYPropertyChangedCallback)));

        private static void NrCellsYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField3DInput instance = d as MValueField3DInput;
            if (instance == null) return;
            if (instance.NrCellsY < 1) return;
            foreach(MValueField2DInput entry in instance.tables)
            {
                entry.NrCellsY = instance.NrCellsY;
            }
        }

        public int NrCellsZ
        {
            get { return (int)GetValue(NrCellsZProperty); }
            set { SetValue(NrCellsZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrCellsZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrCellsZProperty =
            DependencyProperty.Register("NrCellsZ", typeof(int), typeof(MValueField3DInput),
            new UIPropertyMetadata(0, new PropertyChangedCallback(NrCellsZPropertyChangedCallback)));

        private static void NrCellsZPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField3DInput instance = d as MValueField3DInput;
            if (instance == null) return;
            if (instance.NrCellsZ < 1) return;

            instance.AdaptTabsToNrCellZChange();
            // synchronize axis values with first table
            instance.tables[0].SetAxisValues();
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
            DependencyProperty.Register("UnitX", typeof(string), typeof(MValueField3DInput),
            new UIPropertyMetadata("unit X", new PropertyChangedCallback(UnitXPropertyChangedCallback)));

        private static void UnitXPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField3DInput instance = d as MValueField3DInput;
            if (instance == null) return;
            foreach (MValueField2DInput entry in instance.tables)
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
            DependencyProperty.Register("UnitY", typeof(string), typeof(MValueField3DInput),
            new UIPropertyMetadata("unit Y", new PropertyChangedCallback(UnitYPropertyChangedCallback)));

        private static void UnitYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField3DInput instance = d as MValueField3DInput;
            if (instance == null) return;
            foreach (MValueField2DInput entry in instance.tables)
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
            DependencyProperty.Register("UnitZ", typeof(string), typeof(MValueField3DInput),
            new UIPropertyMetadata("unit Z", new PropertyChangedCallback(UnitZPropertyChangedCallback)));

        private static void UnitZPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField3DInput instance = d as MValueField3DInput;
            if (instance == null) return;

            instance.UpdateUnitZTab();
        }

        #endregion

        #region CLASS MEMBERS

        private int nr_cell_z_prev = 0;
        private List<MValueField2DInput> tables = new List<MValueField2DInput>();

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
                for(int i = 0; i < tab_nr_diff; i++)
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
                        MValueField2DInput ti_table = ti.Content as MValueField2DInput;
                        if (ti_table != null)
                            this.tables.Remove(ti_table);
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
            foreach(var item in this.Items)
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

            MValueField2DInput table = new MValueField2DInput();
            table.Width = this.Width - 8;
            table.Height = this.Height - 8;
            table.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            table.VerticalAlignment = System.Windows.VerticalAlignment.Center;            
            table.NrCellsX = this.NrCellsX;
            table.NrCellsY = this.NrCellsY;
            table.UnitX = this.UnitX;
            table.UnitY = this.UnitY;
            table.Tag = _tag;
            table.ShowGridLines = false;
            table.SnapsToDevicePixels = true;
            table.PropertyChanged += table_PropertyChanged;
            tab_item.Content = table;

            this.tables.Add(table);
        }

        #endregion

        #region METHODS: Gather info for Creating the 3D Field

        public void AssembleFieldInfo(out List<double> xs, out List<double> ys, out List<double> zs, out List<List<double>> Fxyzs)
        {
            if (this.tables == null || this.tables.Count < 1)
            {
                xs = new List<double>();
                ys = new List<double>();
                zs = new List<double>();
                Fxyzs = new List<List<double>>();
                return;
            }

            xs = new List<double>(this.tables[0].Xs);
            ys = new List<double>(this.tables[0].Ys);
            zs = this.ExtractZValues();
            
            Fxyzs = new List<List<double>>();

            for(int i = 0; i < this.tables.Count; i++)
            {
                Fxyzs.Add(this.tables[i].Field);
            }
        }

        private List<double> ExtractZValues()
        {
            List<double> zs = new List<double>();
            foreach(var item in this.Items)
            {
                TabItem ti = item as TabItem;
                if (ti == null) continue;
                if (ti.Tag == null) continue;

                TextBox ti_input = ti.Tag as TextBox;
                if (ti_input == null) continue;

                double z_val = 0.0;
                bool success = double.TryParse(ti_input.Text, NumberStyles.Float, MValueField3DInput.FORMATTER, out z_val);
                zs.Add(z_val);
            }
            return zs;
        }

        #endregion

        #region METHODS: Get info from existing Value Field

        public void FillInfoCellsFromExisting(List<double> _xs, List<double> _ys, List<double> _zs)
        {
            if (this.tables == null || this.tables.Count < 1 || _xs == null || _ys == null || _zs == null) return;

            // x, y
            MValueField2DInput table_0 = this.tables[0];
            if (table_0 == null) return;

            List<double> ys = (_ys.Count == 0) ? new List<double> { 0.0 } : _ys;
            table_0.ApplyAxisValues(new TableAxisValues(_xs, ys));
            table_0.SetAxisValues();

            // z (number of tables has to be set before this method is called)
            for (int i = 0; i < this.Items.Count; i++ )               
            {
                if (i > _zs.Count - 1) continue;

                TabItem ti = this.Items[i] as TabItem;
                if (ti == null) continue;

                TextBox ti_input = ti.Tag as TextBox;
                if (ti_input == null) continue;

                ti_input.Text = _zs[i].ToString();
            }
        }

        public void FillDataCellsFromExisting(Dictionary<Point3D, double> _Fxyzs)
        {
            if (this.tables == null || this.tables.Count < 1 || _Fxyzs == null) return;
            for (int t = 0; t < this.tables.Count; t++ )
            {
                this.tables[t].UpdateAllDataCellsFromExisting(_Fxyzs, t);
            }
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
            MValueField2DInput ti_table = ti.Content as MValueField2DInput;
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
            MValueField2DInput table = sender as MValueField2DInput;
            if (table == null || e == null) return;
            
            if (e.PropertyName == "AxisValues")
            {
                if (!(table.Tag is int)) return;
                int table_index = (int)table.Tag;

                for(int i = 0; i < this.tables.Count; i++)
                {
                    if (i == table_index) continue;
                    this.tables[i].ApplyAxisValues(table.AxisValues);
                }
            }

        }

        #endregion
    }
}
