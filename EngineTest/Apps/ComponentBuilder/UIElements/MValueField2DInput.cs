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
    #region HELPER CLASSES

    public class TableAxisValues
    {
        public List<double> Xs { get; private set; }
        public List<double> Ys { get; private set; }

        public TableAxisValues(List<double> _xs, List<double> _ys)
        {
            this.Xs = (_xs == null) ? new List<double>() : _xs;
            this.Ys = (_ys == null) ? new List<double>() : _ys;
        }
    }

    #endregion

    public class MValueField2DInput : MValueField2DBase
    {
        #region PROPERTIES: Size

        public int NrCellsX
        {
            get { return (int)GetValue(NrCellsXProperty); }
            set { SetValue(NrCellsXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrCellsX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrCellsXProperty =
            DependencyProperty.Register("NrCellsX", typeof(int), typeof(MValueField2DInput),
            new UIPropertyMetadata(0, new PropertyChangedCallback(NrCellsXYPropertyChangedCallback)));

        
        public int NrCellsY
        {
            get { return (int)GetValue(NrCellsYProperty); }
            set { SetValue(NrCellsYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrCellsY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrCellsYProperty =
            DependencyProperty.Register("NrCellsY", typeof(int), typeof(MValueField2DInput),
            new UIPropertyMetadata(0, new PropertyChangedCallback(NrCellsXYPropertyChangedCallback)));

        private static void NrCellsXYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField2DInput instance = d as MValueField2DInput;
            if (instance == null) return;
            if (instance.NrCellsX < 1 || instance.NrCellsX > MValueField2DBase.MAX_NR_COLS) return;
            if (instance.NrCellsY < 1 || instance.NrCellsY > MValueField2DBase.MAX_NR_ROWS) return;
            // reset grid
            instance.ResetGrid();
            instance.RecalculateTableSizes(instance.NrCellsX, instance.NrCellsY);
            instance.RedefineTableGrid();
            instance.AdaptValueContainers(instance.NrCellsX, instance.NrCellsY);
            instance.FillInfoCells();
            instance.SetUnitLabels();
            instance.FillDataCells();
        }

        #endregion

        #region PROPERTIES: Communication w other Tables

        protected TableAxisValues axis_values;
        public TableAxisValues AxisValues
        {
            get { return this.axis_values; }
            protected set
            {
                this.axis_values = value;
                this.RegisterPropertyChanged("AxisValues");
            }
        }

        #endregion

        #region METHOD: Adapt Internal Value Containers

        protected void AdaptValueContainers(int _nrX, int _nrY)
        {
            if (this.xs == null)
                this.xs = new List<double>();
            if (this.ys == null)
                this.ys = new List<double>();
            if (this.Fxys == null)
                this.InitializeDataPoints();

            int diff_x = _nrX - this.xs.Count;
            int diff_y = _nrY - this.ys.Count;

            //if (this.id == 1) Debugger.Break();

            if (diff_x != 0)
            {
                if (diff_x > 0)
                {
                    this.AddDataPoints(diff_x, true);
                    for (int i = 0; i < diff_x; i++)
                    {
                        this.xs.Add(0.0);
                    }
                }
                else
                {
                    this.RemoveDataPoints(-diff_x, true);
                    for (int i = xs.Count - 1; i > _nrX - 1; i--)
                    {
                        this.xs.RemoveAt(i);
                    }
                }
            }

            if (diff_y != 0)
            {
                if (diff_y > 0)
                {
                    this.AddDataPoints(diff_y, false);
                    for (int i = 0; i < diff_y; i++)
                    {
                        this.ys.Add(0.0);
                    }
                }
                else
                {
                    this.RemoveDataPoints(-diff_y, false);
                    for (int i = ys.Count - 1; i > _nrY - 1; i--)
                    {
                        this.ys.RemoveAt(i);
                    }
                }
            }
        }

        private void AddDataPoints(int _nr, bool _along_x_axis)
        {
            if (_nr < 1) return;
            if (_along_x_axis)
            {
                for (int r = 0; r < this.ys.Count; r++)
                {
                    for (int c = this.xs.Count; c < this.xs.Count + _nr; c++)
                    {
                        this.Fxys[r * MValueField2DBase.MAX_NR_COLS + c] = new Point3D(c, r, 0.0);
                    }
                }
            }
            else
            {
                for (int r = this.ys.Count; r < this.ys.Count + _nr; r++)
                {
                    for (int c = 0; c < this.xs.Count; c++)
                    {
                        this.Fxys[r * MValueField2DBase.MAX_NR_COLS + c] = new Point3D(c, r, 0.0);
                    }
                }
            }
        }

        private void RemoveDataPoints(int _nr, bool _along_x_axis)
        {
            if (_nr < 1) return;
            if (_along_x_axis)
            {
                for (int r = 0; r < this.ys.Count; r++)
                {
                    for (int c = this.xs.Count - _nr; c < this.xs.Count; c++)
                    {
                        this.Fxys[r * MValueField2DBase.MAX_NR_COLS + c] = MValueField2DBase.INVALID_DATA;
                    }
                }
            }
            else
            {
                for (int r = this.ys.Count - _nr; r < this.ys.Count; r++)
                {
                    for (int c = 0; c < this.xs.Count; c++)
                    {
                        this.Fxys[r * MValueField2DBase.MAX_NR_COLS + c] = MValueField2DBase.INVALID_DATA;
                    }
                }
            }
        }

        #endregion

        #region METHODS: Grid (Re)Realize (Info Cells)

        protected override void FillInfoCells()
        {
            // fill the info cells
            for (int r = 1; r < this.nr_columns + 2; r++)
            {
                Rectangle rect = new Rectangle();
                rect.Height = MValueField2DBase.INFO_ROW_HEIGHT;
                rect.Width = (r == 1) ? MValueField2DBase.INFO_COL_WIDTH : this.column_width;
                rect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                rect.StrokeThickness = 1;
                rect.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DimGray);

                Grid.SetRow(rect, this.nr_rows);
                Grid.SetColumn(rect, r);
                this.Children.Add(rect);

                if (r > 1)
                {
                    TextBox tb = new TextBox();
                    tb.HorizontalAlignment = HorizontalAlignment.Left;
                    tb.VerticalAlignment = VerticalAlignment.Bottom;
                    tb.Margin = new Thickness(1);
                    tb.Text = this.xs[r - 2].ToString("F2", MValueField2DBase.FORMATTER);
                    tb.Tag = new Point(r - 2, -1);
                    tb.LostFocus += tb_LostFocus;
                    tb.FontSize = 10;
                    tb.Foreground = new SolidColorBrush(Colors.Blue);

                    Grid.SetRow(tb, this.nr_rows);
                    Grid.SetColumn(tb, r);
                    this.Children.Add(tb);
                }
            }


            for (int c = 2; c < this.nr_rows + 2; c++)
            {
                Rectangle rect = new Rectangle();
                rect.Height = this.row_height;
                rect.Width = MValueField2DBase.INFO_COL_WIDTH;
                rect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                rect.StrokeThickness = 1;
                rect.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DimGray);

                Grid.SetRow(rect, this.nr_rows + 1 - c);
                Grid.SetColumn(rect, 1);
                this.Children.Add(rect);

                if (this.nr_rows > 1)
                {
                    TextBox tb = new TextBox();
                    tb.HorizontalAlignment = HorizontalAlignment.Left;
                    tb.VerticalAlignment = VerticalAlignment.Bottom;
                    tb.Margin = new Thickness(1);
                    tb.Text = this.ys[c - 2].ToString("F2", MValueField2DBase.FORMATTER);
                    tb.Tag = new Point(-1, c - 2);
                    tb.LostFocus += tb_LostFocus;
                    tb.FontSize = 10;
                    tb.Foreground = new SolidColorBrush(Colors.Blue);

                    Grid.SetRow(tb, this.nr_rows + 1 - c);
                    Grid.SetColumn(tb, 1);
                    this.Children.Add(tb);
                }
            }
        }
        #endregion

        #region METHODS: Grid Update (Info Cells)

        private void UpdateInfoCells()
        {
            foreach (object child in this.Children)
            {
                this.UpdateOneInfoCellFromInternalArray(child);
            }
        }

        private void UpdateOneInfoCellFromUserInput(object _info_cell_candidate)
        {
            TextBox tb = _info_cell_candidate as TextBox;
            if (tb == null) return;
            if (tb.Tag == null || !(tb.Tag is Point)) return;

            Point tb_info = (Point)tb.Tag;
            if (tb_info.Y == -1)
            {
                int index_x = (int)tb_info.X;
                if (index_x < 0 || index_x > this.xs.Count - 1) return;

                double content = double.NaN;
                bool success = double.TryParse(tb.Text, NumberStyles.Float, MValueField2DBase.FORMATTER, out content);
                if (success)
                    this.xs[index_x] = content;
            }
            else if (tb_info.X == -1)
            {
                int index_y = (int)tb_info.Y;
                if (index_y < 0 || index_y > this.ys.Count - 1) return;

                double content = double.NaN;
                bool success = double.TryParse(tb.Text, NumberStyles.Float, MValueField2DBase.FORMATTER, out content);
                if (success)
                    this.ys[index_y] = content;
            }
        }

        private void UpdateOneInfoCellFromInternalArray(object _info_cell_candidate)
        {
            TextBox tb = _info_cell_candidate as TextBox;
            if (tb == null) return;
            if (tb.Tag == null || !(tb.Tag is Point)) return;

            Point tb_info = (Point)tb.Tag;
            if (tb_info.Y == -1)
            {
                int index_x = (int)tb_info.X;
                if (index_x < 0 || index_x > this.xs.Count - 1) return;
                tb.Text = this.xs[index_x].ToString("F2", MValueField2DBase.FORMATTER);
            }
            else if (tb_info.X == -1)
            {
                int index_y = (int)tb_info.Y;
                if (index_y < 0 || index_y > this.ys.Count - 1) return;
                tb.Text = this.ys[index_y].ToString("F2", MValueField2DBase.FORMATTER);
            }
        }

        #endregion

        #region METHODS: Grid Re(Realize) (Data Cells)

        protected override void FillDataCells()
        {
            for (int c = 2; c < this.nr_columns + 2; c++)
            {
                for (int r = 2; r < this.nr_rows + 2; r++)
                {
                    // cell
                    Rectangle rect = new Rectangle();
                    rect.Height = Math.Max(this.row_height, 1);
                    rect.Width = Math.Max(this.column_width, 1);
                    rect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCFFFFFF"));
                    rect.StrokeThickness = 0.5;
                    rect.StrokeDashArray = new DoubleCollection { 5, 5 };
                    rect.Stroke = new SolidColorBrush(Colors.Black);
                    rect.Name = "x_" + c + "__y_" + (this.nr_rows + 1 - r);

                    Grid.SetRow(rect, this.nr_rows + 1 - r);
                    Grid.SetColumn(rect, c);
                    this.Children.Add(rect);

                    // value
                    TextBox tb = new TextBox();
                    tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    tb.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                    tb.Margin = new Thickness(1);
                    tb.FontSize = 10;
                    tb.Tag = new Point3D(c - 2, r - 2, 0);
                    //tb.Text = rect.Name + " " + tb.Tag.ToString();
                    //tb.Text = this.Fxys[(r - 2) * MValueField2DBase.MAX_NR_COLS + (c - 2)].ToString();
                    tb.Text = this.Fxys[(r - 2) * MValueField2DBase.MAX_NR_COLS + (c - 2)].Z.ToString("F2", MValueField2DBase.FORMATTER);
                    tb.LostFocus += tb_data_LostFocus;

                    Grid.SetRow(tb, this.nr_rows + 1 - r);
                    Grid.SetColumn(tb, c);
                    this.Children.Add(tb);
                }
            }
        }

        #endregion

        #region METHODS: Grid Update (Data Cells)

        private void UpdateOneDataCellFromUserInput(object _data_cell_candidate)
        {
            TextBox tb = _data_cell_candidate as TextBox;
            if (tb == null) return;
            if (tb.Tag == null || !(tb.Tag is Point3D)) return;

            Point3D tb_data = (Point3D)tb.Tag;
            int index_x = (int)tb_data.X;
            int index_y = (int)tb_data.Y;
            if (index_x < 0 || index_x > MValueField2DBase.MAX_NR_COLS - 1) return;
            if (index_y < 0 || index_y > MValueField2DBase.MAX_NR_ROWS - 1) return;

            double content = double.NaN;
            bool success = double.TryParse(tb.Text, NumberStyles.Float, MValueField2DBase.FORMATTER, out content);
            if (success)
                this.Fxys[index_y * MValueField2DBase.MAX_NR_COLS + index_x] = new Point3D(index_x, index_y, content);
        }

        private void UpdateOneDataCellFromExisting(Point3D _data, double _value)
        {
            int index_x = (int)_data.X;
            int index_y = (int)_data.Y;
            if (index_x < 0 || index_x > MValueField2DBase.MAX_NR_COLS - 1) return;
            if (index_y < 0 || index_y > MValueField2DBase.MAX_NR_ROWS - 1) return;

            this.Fxys[index_y * MValueField2DBase.MAX_NR_COLS + index_x] = new Point3D(index_x, index_y, _value);
        }

        public void UpdateAllDataCellsFromExisting(Dictionary<Point3D, double> _data, int _z_index)
        {
            if (_data == null || _data.Count < 1) return;
            foreach (var entry in _data)
            {
                if (entry.Key.Z != _z_index) continue;
                this.UpdateOneDataCellFromExisting(entry.Key, entry.Value);
            }

            // update the text boxes
            foreach (var child in this.Children)
            {
                TextBox tb = child as TextBox;
                if (tb == null) continue;
                if (tb.Tag == null) continue;
                if (!(tb.Tag is Point3D)) continue;

                Point3D p = (Point3D)tb.Tag;
                tb.Text = this.Fxys[(int)p.Y * MValueField2DBase.MAX_NR_COLS + (int)p.X].Z.ToString("F2", MValueField2DBase.FORMATTER);
            }
        }

        #endregion

        #region METHODS: Communication w other Tables

        public void SetAxisValues()
        {
            this.AxisValues = new TableAxisValues(this.xs, this.ys);
        }

        public void ApplyAxisValues(TableAxisValues _input)
        {
            if (_input == null) return;
            if (_input.Xs == null || _input.Ys == null) return;
            if (this.xs == null || this.ys == null) return;
            if (_input.Xs.Count != this.xs.Count || _input.Ys.Count != this.ys.Count) return;

            this.xs = new List<double>(_input.Xs);
            this.ys = new List<double>(_input.Ys);
            this.UpdateInfoCells();
        }

        #endregion

        #region EVENT HANDLERS
        protected void tb_LostFocus(object sender, RoutedEventArgs e)
        {
            this.UpdateOneInfoCellFromUserInput(sender);
        }

        protected void tb_data_LostFocus(object sender, RoutedEventArgs e)
        {
            this.UpdateOneDataCellFromUserInput(sender);
        }

        #endregion
    }
}
