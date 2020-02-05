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
using System.Globalization;

using ParameterStructure.Values;

namespace ComponentBuilder.UIElements
{
    public class MValueBigTableDataGrid : DataGrid
    {
        public MValueBigTableDataGrid()
            :base()
        {
            this.Loaded += table_Loaded;
            this.IsVisibleChanged += table_IsVisibleChanged;
            this.PreviewMouseDoubleClick += table_MouseDoubleClick;
            this.IsReadOnly = true;
        }

        private void table_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e == null) return;
            e.Handled = true;
        }


        #region PROPERTIES: Data

        public MultiValueBigTable DataField
        {
            get { return (MultiValueBigTable)GetValue(DataFieldProperty); }
            set { SetValue(DataFieldProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DataField.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataFieldProperty =
            DependencyProperty.Register("DataField", typeof(MultiValueBigTable), typeof(MValueBigTableDataGrid),
            new UIPropertyMetadata(null, new PropertyChangedCallback(DataFieldPropertyChangedCallback)));

        private static void DataFieldPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueBigTableDataGrid instance = d as MValueBigTableDataGrid;
            if (instance == null) return;
            instance.PopulateDataGrid();
        }

        #endregion

        #region METHODS: Place data in the DataGrid

        private void PopulateDataGrid()
        {
            // define the data source
            List<BigTableRow> dg_rows = new List<BigTableRow>();
            if (this.DataField.RowNames == null || this.DataField.RowNames.Count == 0)
            {
                for (int i = 0; i < this.DataField.Values.Count; i++)
                {
                    BigTableRow dg_row = new BigTableRow(i + 1, this.DataField.Values[i]);
                    dg_rows.Add(dg_row);
                }
            }
            else
            {
                for (int i = 0; i < this.DataField.Values.Count; i++)
                {
                    BigTableRow dg_row = new BigTableRow(i + 1, this.DataField.RowNames[i], this.DataField.Values[i]);
                    dg_rows.Add(dg_row);
                }
            }
            this.ItemsSource = dg_rows;

            // define the headers
            DataGridTextColumn col_nr = new DataGridTextColumn();
            col_nr.Header = "Nr";
            col_nr.Binding = new Binding("Col00");
            col_nr.FontWeight = FontWeights.Bold;
            col_nr.CellStyle = (Style)this.TryFindResource("BigTable_DataGridCell");
            this.Columns.Add(col_nr);
            if (this.DataField.RowNames == null)
            {
                for (int i = 0; i < this.DataField.Names.Count; i++)
                {
                    DataGridTextColumn col_i = new DataGridTextColumn();
                    col_i.Header = this.DataField.Names[i] + "\n" + this.DataField.Units[i];
                    col_i.Binding = new Binding("Col" + (i + 1).ToString("D2"));
                    this.Columns.Add(col_i);
                }
            }
            else
            {
                for (int i = 1; i < this.DataField.Names.Count; i++)
                {
                    DataGridTextColumn col_i = new DataGridTextColumn();
                    col_i.Header = this.DataField.Names[i] + "\n" + this.DataField.Units[i];
                    col_i.Binding = new Binding("Col" + (i).ToString("D2"));
                    this.Columns.Add(col_i);
                }
            }

            // define the selection (in the Loaded event handler)

            // define the event handlers
            this.SelectedCellsChanged += table_SelectedCellsChanged;
            
        }

        #endregion

        #region EVENT HANDLERS

        private void table_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.scroll_pending)
            {
                // both needed in this sequence, otherwise the scroll in the parameter editing expander does not work
                this.ScrollIntoView(this.Items[this.items_ind], this.Columns[this.column_ind]);
                this.ScrollIntoView(this.Items[this.items_ind]);
                this.scroll_pending = false;
            }
        }

        private bool scroll_pending = false;
        private int items_ind = 0;
        private int column_ind = 0;
        private void table_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataField == null) return;
            if (this.DataField.MVDisplayVector == MultiValPointer.INVALID) return;
            if (this.DataField.MVDisplayVector.NrDim < 2) return;

            int ind_row = this.DataField.MVDisplayVector.CellIndices[0];
            int ind_col = this.DataField.MVDisplayVector.CellIndices[1];

            // define the selected cell
            if ((this.Items.Count > ind_row) && (this.Columns.Count > ind_col))
            {
                this.CurrentCell = new DataGridCellInfo(this.Items[ind_row], this.Columns[ind_col]);
                this.SelectedCells.Clear();
                this.SelectedCells.Add(this.CurrentCell);
                if (this.IsVisible)
                    this.ScrollIntoView(this.Items[ind_row], this.Columns[ind_col]);
                else
                {
                    this.scroll_pending = true;
                    this.items_ind = ind_row;
                    this.column_ind = ind_col;
                }
            }
        }

        private void table_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            if (dg == null || e == null) return;

            if (e.AddedCells != null && e.AddedCells.Count > 0)
            {
                BigTableRow item = e.AddedCells[0].Item as BigTableRow;
                if (item != null)
                {
                    int row_ind = item.Col00_Index - 1;

                    int col_ind = e.AddedCells[0].Column.DisplayIndex;

                    string content = item.GetColAt(col_ind);
                    double value = 0;
                    bool success = double.TryParse(content, NumberStyles.Float, BigTableRow.FORMATTER, out value);
                    if (success)
                    {
                        RectangularValue rv = new RectangularValue()
                        {
                            RightBottom = value,
                            RightTop = value,
                            LeftBottom = value,
                            LeftTop = value
                        };
                        MultiValPointer pointer = new MultiValPointer(new List<int> { row_ind, col_ind, 0 }, new Point(1, 1), new Point(0, 0), true, rv, false);
                        if (pointer != MultiValPointer.INVALID)
                            this.DataField.MVDisplayVector = pointer;
                    }
                }
            }
        }

        #endregion
    }
}
