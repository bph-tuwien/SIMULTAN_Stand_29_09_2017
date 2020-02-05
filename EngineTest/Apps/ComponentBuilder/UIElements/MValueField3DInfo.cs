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
    public class MValueField3DInfo : TabControl
    {
        #region STATIC

        protected static readonly IFormatProvider FORMATTER = new NumberFormatInfo();

        #endregion

        public MValueField3DInfo()
            :base()
        {
            this.Loaded += MValueField3DInfo_Loaded;
        }

        #region PROPERTIES: Data Field

        public MultiValueTable DataField
        {
            get { return (MultiValueTable)GetValue(DataFieldProperty); }
            set { SetValue(DataFieldProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DataField.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataFieldProperty =
            DependencyProperty.Register("DataField", typeof(MultiValueTable), typeof(MValueField3DInfo),
            new UIPropertyMetadata(null, new PropertyChangedCallback(DataFieldPropertyChangedCallback)));

        private static void DataFieldPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField3DInfo instance = d as MValueField3DInfo;
            if (instance == null) return;
            if (instance.DataField == null) return;

            instance.nr_tables = (instance.DataField.NrZ < 1) ? 1 : instance.DataField.NrZ;
            instance.CreateTabs();
        }

        #endregion

        #region CLASS MEMBERS

        private int nr_tables = 0;
        private List<MValueField2DInfo> tables = new List<MValueField2DInfo>();

        #endregion

        #region METHODS: UIElements TabItems

        private void CreateTabs()
        {

            // add tabs to existing ones
            for (int i = 0; i < nr_tables; i++)
            {
                // for each 2D Table -> define a separate TAB
                TabItem ti = new TabItem();
                ti.Header = "Tab " + i.ToString();

                // input for the table names
                TextBlock ti_label = new TextBlock();
                ti_label.MinWidth = 25;
                ti_label.MinHeight = 15;
                ti_label.Height = 15;
                ti_label.FontSize = 10;
                ti_label.Foreground = new SolidColorBrush(Colors.Blue);
                if (i < this.DataField.Zs.Count)
                    ti_label.Text = this.DataField.Zs[i].ToString("F2", MValueField3DInfo.FORMATTER);

                ti.Tag = ti_label;

                // apply style
                ti.Style = (Style)this.TryFindResource("TabItem_ValueField_Input");

                // add a new table
                this.PopulateTabITem(i, ref ti);

                // add Tab to the TabControl
                this.Items.Add(ti);
            }

            // add the tab holding the unit z label
            this.AddUnitZTab(this.DataField.MVUnitZ);
            
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
            ti_text.Text = _unit_z;
            ti_label.Tag = ti_text;

            ti_label.Style = (Style)this.TryFindResource("TabItem_ValueField_Input");
            this.Items.Add(ti_label);
        }

        #endregion

        #region METHODS: UIElements TabItem Content

        private void PopulateTabITem(int _tag, ref TabItem tab_item)
        {
            if (tab_item == null) return;

            MValueField2DInfo table = new MValueField2DInfo();
            table.Width = this.Width - 8;
            table.Height = this.Height - 8;
            table.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            table.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            table.Depth = _tag;
            table.DataField = this.DataField;          
            table.Tag = _tag;
            table.ShowGridLines = false;
            table.SnapsToDevicePixels = true;
            table.PropertyChanged +=table_PropertyChanged;
            tab_item.Content = table;

            this.tables.Add(table);
        }

        #endregion

        #region EVENT HANDLERS

        private void MValueField3DInfo_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataField == null) return;
            MultiValPointer pointer = this.DataField.MVDisplayVector;
            if (pointer != MultiValPointer.INVALID && pointer.CellIndices.Count > 2)
            {
                this.MarkTab(pointer.CellIndices[2]);
            }
        }

        private void table_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MValueField2DInfo table = sender as MValueField2DInfo;
            if (table == null || e == null) return;

            if (e.PropertyName == "MarkSet")
            {
                if (!(table.Tag is int)) return;
                int table_index = (int)table.Tag;

                // delete mark from all other tables
                for (int i = 0; i < this.tables.Count; i++)
                {
                    if (i == table_index) continue;
                    this.tables[i].DeleteMark();
                }

                this.MarkTab(table_index);
            }

        }

        private void MarkTab(int _index)
        {
            for(int i = 0; i < this.Items.Count; i++)
            {
                TabItem ti = this.Items[i] as TabItem;
                if (ti == null) continue;
                if (ti.Tag == null) continue;

                TextBlock tb = ti.Tag as TextBlock;
                if (tb == null) continue;

                if (i == _index)
                    tb.Foreground = new SolidColorBrush(Colors.Red);
                else
                    tb.Foreground = new SolidColorBrush(Colors.Blue);
            }
        }

        #endregion
    }
}
