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
using System.Diagnostics;

using ParameterStructure.Parameter;
using ParameterStructure.Values;

namespace ComponentBuilder.UIElements
{
    #region ENUMS

    public enum FunctionEditState { REGULAR, IS_BEING_DRAWIN, SELECTED }

    #endregion
    public abstract class MValueFunct2DBase : Grid, INotifyPropertyChanged
    {
        #region STATIC: General

        private static int NR_INSTANCES = 0;

        protected static readonly int MAX_NR_COLS = 10;
        protected static readonly int MAX_NR_ROWS = 10;

        protected static readonly int INFO_COL_WIDTH = 35;
        protected static readonly int INFO_ROW_HEIGHT = 20;

        protected static readonly int MIN_COL_WIDTH = 25;
        protected static readonly int MIN_ROW_HEIGHT = 15;

        protected static readonly int MAX_COL_WIDTH = 90;
        protected static readonly int MAX_ROW_HEIGHT = 30;

        protected static readonly int UNIT_LABEL_COL_HEIGHT = 28;
        protected static readonly string UNIT_LABEL_X_TAG = "UNIT_X";
        protected static readonly string UNIT_LABEL_Y_TAG = "UNIT_Y";

        protected static readonly IFormatProvider FORMATTER = new NumberFormatInfo();

        public static readonly int SNAP_DIST_PX = 4;

        protected static readonly double MIN_X = -1;
        protected static readonly double MAX_X = 1;
        protected static readonly double MIN_Y = -1;
        protected static readonly double MAX_Y = 1;

        protected static Color SetColorAccToState(FunctionEditState _state)
        {
            switch(_state)
            {
                case FunctionEditState.IS_BEING_DRAWIN:
                    return Colors.Black;
                case FunctionEditState.SELECTED:
                    return Colors.OrangeRed;
                default:
                    return Colors.DimGray;
            }
        }

        #endregion

        private int id;
        public MValueFunct2DBase()
            :base()
        {
            this.id = (++NR_INSTANCES);
            this.Bounds = new Point4D(MValueFunct2DBase.MIN_X, MValueFunct2DBase.MAX_X,
                                        MValueFunct2DBase.MIN_Y, MValueFunct2DBase.MAX_Y);            
            this.Loaded += MValueFunct2DBase_Loaded;
        }


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

        #region PROPERIES: Units

        public string UnitX
        {
            get { return (string)GetValue(UnitXProperty); }
            set { SetValue(UnitXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitXProperty =
            DependencyProperty.Register("UnitX", typeof(string), typeof(MValueFunct2DBase),
            new UIPropertyMetadata("unit x", new PropertyChangedCallback(UnitXYPropertyChangedCallback)));

        public string UnitY
        {
            get { return (string)GetValue(UnitYProperty); }
            set { SetValue(UnitYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitYProperty =
            DependencyProperty.Register("UnitY", typeof(string), typeof(MValueFunct2DBase),
            new UIPropertyMetadata("unit y", new PropertyChangedCallback(UnitXYPropertyChangedCallback)));

        private static void UnitXYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct2DBase instance = d as MValueFunct2DBase;
            if (instance == null) return;
            instance.UpdateUnitLabels();
        }

        #endregion

        #region PROPERTIES: Bounds

        protected Point4D bounds;
        public virtual Point4D Bounds
        {
            get { return this.bounds; }
            set 
            {
                this.bounds = value;
                this.RescaleAxesLabels();
                this.SetAxisValues();
                this.ReDrawAllLines();
                this.RegisterPropertyChanged("Bounds");
            }
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

        #region CLASS MEMBERS

        protected bool grid_set = false;

        protected bool size_original_set = false;
        protected double width_original = 0;
        protected double height_original = 0;

        protected int nr_columns = 0;
        protected int nr_rows = 0;
        protected int column_width = 0;
        protected int row_height = 0;

        protected List<double> xs; // labels for the x-axis
        protected List<double> ys; // labels for the y-axis
        protected double fact_x; // scaling factor for drawing on the canvas
        protected double fact_y; // scaling factor for drawing on the canvas

        protected Canvas canvas;
        protected List<List<Point3D>> polylines = new List<List<Point3D>>();
        protected List<string> polylines_names = new List<string>();
        protected int ind_sel_polyline = -1;

        public List<List<Point3D>> FunctionGraphs { get { return new List<List<Point3D>>(this.polylines); } }
        public List<string> FunctionNames { get { return new List<string>(this.polylines_names); } }

        #endregion

        #region METHODS: Grid Reset, Resize

        protected void ResetGrid()
        {
            this.Children.Clear();
            this.RowDefinitions.Clear();
            this.ColumnDefinitions.Clear();
        }

        protected void RecalculateTableSizes(int _nrX, int _nrY)
        {
            // set the original sizes, if not set already
            if (!this.size_original_set)
            {
                this.width_original = this.Width;
                this.height_original = this.Height;
                this.size_original_set = true;
            }

            // save cell nr
            this.nr_columns = (_nrX < 1) ? 1 : _nrX;
            this.nr_rows = (_nrY < 1) ? 1 : _nrY;

            int col_B = Math.Max((int)Math.Floor((this.width_original - MValueFunct2DBase.INFO_COL_WIDTH - MValueFunct2DBase.UNIT_LABEL_COL_HEIGHT) / this.nr_columns), MValueFunct2DBase.MIN_COL_WIDTH);
            col_B = Math.Min(col_B, MValueFunct2DBase.MAX_COL_WIDTH);
            int row_H = Math.Max((int)Math.Floor((this.height_original - MValueFunct2DBase.INFO_ROW_HEIGHT - MValueFunct2DBase.UNIT_LABEL_COL_HEIGHT) / this.nr_rows), MValueFunct2DBase.MIN_ROW_HEIGHT);
            row_H = Math.Min(row_H, MValueFunct2DBase.MAX_ROW_HEIGHT);

            // re-calculate the size of the grid
            this.Width = MValueFunct2DBase.INFO_COL_WIDTH + MValueFunct2DBase.UNIT_LABEL_COL_HEIGHT + this.nr_columns * col_B;
            this.Height = MValueFunct2DBase.INFO_ROW_HEIGHT + MValueFunct2DBase.UNIT_LABEL_COL_HEIGHT + this.nr_rows * row_H;

            // save cell sizes            
            this.column_width = col_B;
            this.row_height = row_H;
        }

        protected void RedefineTableGrid()
        {

            ColumnDefinition cd_uY = new ColumnDefinition();
            cd_uY.Width = new GridLength(MValueFunct2DBase.UNIT_LABEL_COL_HEIGHT);
            this.ColumnDefinitions.Add(cd_uY);

            ColumnDefinition cd0 = new ColumnDefinition();
            cd0.Width = new GridLength(MValueFunct2DBase.INFO_COL_WIDTH);
            this.ColumnDefinitions.Add(cd0);

            for (int i = 0; i < this.nr_columns; i++)
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

            RowDefinition rd0 = new RowDefinition();
            rd0.Height = new GridLength(MValueFunct2DBase.INFO_ROW_HEIGHT);
            this.RowDefinitions.Add(rd0);

            RowDefinition rd_uX = new RowDefinition();
            rd_uX.Height = new GridLength(MValueFunct2DBase.UNIT_LABEL_COL_HEIGHT);
            this.RowDefinitions.Add(rd_uX);
        }



        #endregion

        #region METHODS: Update Unit Labels

        protected void SetUnitLabels()
        {
            TextBlock tbX = new TextBlock();
            tbX.Width = 120;
            tbX.Height = 25;
            tbX.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            tbX.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
            tbX.Text = this.UnitX;
            tbX.FontSize = 10;
            tbX.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));
            tbX.Tag = MValueFunct2DBase.UNIT_LABEL_X_TAG;

            Grid.SetRow(tbX, this.nr_rows + 1);
            Grid.SetColumn(tbX, 2);
            Grid.SetColumnSpan(tbX, this.nr_columns + 1);
            this.Children.Add(tbX);

            TextBlock tbY = new TextBlock();
            tbY.Width = 120;
            tbY.Height = 25;
            tbY.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            tbY.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            tbY.Text = this.UnitY;
            tbY.FontSize = 10;
            tbY.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));
            tbY.RenderTransformOrigin = new Point(0.1, 0.25);
            tbY.RenderTransform = new RotateTransform(-90.0);
            tbY.Tag = MValueFunct2DBase.UNIT_LABEL_Y_TAG;

            Grid.SetRow(tbY, this.nr_rows - 1);
            Grid.SetRowSpan(tbY, this.nr_rows);
            Grid.SetColumn(tbY, 0);
            Grid.SetColumnSpan(tbY, this.nr_columns + 1);
            this.Children.Add(tbY);
        }

        protected void UpdateUnitLabels()
        {
            foreach (var child in this.Children)
            {
                TextBlock tb = child as TextBlock;
                if (tb == null) continue;
                if (tb.Tag == null) continue;
                if (!(tb.Tag is string)) continue;

                if (tb.Tag.ToString() == MValueFunct2DBase.UNIT_LABEL_X_TAG)
                    tb.Text = this.UnitX;
                if (tb.Tag.ToString() == MValueFunct2DBase.UNIT_LABEL_Y_TAG)
                    tb.Text = this.UnitY;
            }
        }

        #endregion

        #region METHODS: Grid Realize (Info Cells)

        protected virtual void FillInfoCells()
        {
            // fill the info cells
            for (int r = 1; r < this.nr_columns + 2; r++)
            {
                Rectangle rect = new Rectangle();
                rect.Height = MValueFunct2DBase.INFO_ROW_HEIGHT;
                rect.Width = (r == 1) ? MValueFunct2DBase.INFO_COL_WIDTH : this.column_width;
                rect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                rect.StrokeThickness = 1;
                rect.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DimGray);

                Grid.SetRow(rect, this.nr_rows);
                Grid.SetColumn(rect, r);
                this.Children.Add(rect);

                if (r > 1)
                {
                    TextBlock tbl = new TextBlock();
                    tbl.HorizontalAlignment = HorizontalAlignment.Left;
                    tbl.VerticalAlignment = VerticalAlignment.Bottom;
                    tbl.Margin = new Thickness(1);
                    tbl.Text = this.xs[r - 2].ToString("F2", MValueFunct2DBase.FORMATTER);
                    tbl.Tag = new Point(r - 2, -1);
                    tbl.FontSize = 10;
                    tbl.Foreground = new SolidColorBrush(Colors.Blue);

                    Grid.SetRow(tbl, this.nr_rows);
                    Grid.SetColumn(tbl, r);
                    this.Children.Add(tbl);
                }
            }


            for (int c = 2; c < this.nr_rows + 2; c++)
            {
                Rectangle rect = new Rectangle();
                rect.Height = this.row_height;
                rect.Width = MValueFunct2DBase.INFO_COL_WIDTH;
                rect.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                rect.StrokeThickness = 1;
                rect.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DimGray);

                Grid.SetRow(rect, this.nr_rows + 1 - c);
                Grid.SetColumn(rect, 1);
                this.Children.Add(rect);

                if (this.nr_rows > 1)
                {
                    TextBlock tbl = new TextBlock();
                    tbl.HorizontalAlignment = HorizontalAlignment.Left;
                    tbl.VerticalAlignment = VerticalAlignment.Bottom;
                    tbl.Margin = new Thickness(1);
                    tbl.Text = this.ys[c - 2].ToString("F2", MValueFunct2DBase.FORMATTER);
                    tbl.Tag = new Point(-1, c - 2);
                    tbl.FontSize = 10;
                    tbl.Foreground = new SolidColorBrush(Colors.Blue);

                    Grid.SetRow(tbl, this.nr_rows + 1 - c);
                    Grid.SetColumn(tbl, 1);
                    this.Children.Add(tbl);
                }
            }
        }


        #endregion

        #region METHODS: Grid Realize (Canvas)

        protected void SetCanvas()
        {
            Canvas canv = new Canvas();
            canv.Width = this.Width - MValueFunct2DBase.INFO_COL_WIDTH - MValueFunct2DBase.UNIT_LABEL_COL_HEIGHT;
            canv.Height = this.Height - MValueFunct2DBase.INFO_ROW_HEIGHT - MValueFunct2DBase.UNIT_LABEL_COL_HEIGHT;
            canv.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEFEFFF"));

            Grid.SetRow(canv, 0);
            Grid.SetRowSpan(canv, this.nr_rows);
            Grid.SetColumn(canv, 2);
            Grid.SetColumnSpan(canv, this.nr_columns);
            this.Children.Add(canv);
            this.canvas = canv;
        }

        #endregion

        #region METHODS: Grid Realize (Grid Overlay)
        protected virtual void FillOverlay()
        {
            for (int c = 2; c < this.nr_columns + 2; c++)
            {
                for (int r = 2; r < this.nr_rows + 2; r++)
                {
                    // cell
                    Rectangle rect = new Rectangle();
                    rect.Height = Math.Max(this.row_height, 1);
                    rect.Width = Math.Max(this.column_width, 1);
                    //rect.Realize = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99FFFFFF"));
                    rect.StrokeThickness = 0.5;
                    rect.StrokeDashArray = new DoubleCollection { 5, 5 };
                    rect.Stroke = new SolidColorBrush(Colors.Black);
                    rect.Name = "x_" + c + "__y_" + (this.nr_rows + 1 - r);
                    rect.IsHitTestVisible = false;
                    //rect.MouseUp += this.rect_MouseUp;

                    //RectangularValue rv = new RectangularValue();
                    //rv.LeftBottom = this.DataField.Field[new Point3D(c - 2, r - 2, this.Depth)];
                    //rv.RightBottom = (c == this.nr_columns + 1) ? rv.LeftBottom : this.DataField.Field[new Point3D(c - 1, r - 2, this.Depth)];
                    //rv.LeftTop = (r == this.nr_rows_IN + 1) ? rv.LeftBottom : this.DataField.Field[new Point3D(c - 2, r - 1, this.Depth)];
                    //if (c == this.nr_columns + 1 && r == this.nr_rows_IN + 1)
                    //    rv.RightTop = rv.LeftBottom;
                    //else if (c == this.nr_columns + 1 && r < this.nr_rows_IN + 1)
                    //    rv.RightTop = this.DataField.Field[new Point3D(c - 2, r - 1, this.Depth)];
                    //else if (c < this.nr_columns + 1 && r == this.nr_rows_IN + 1)
                    //    rv.RightTop = this.DataField.Field[new Point3D(c - 1, r - 2, this.Depth)];
                    //else
                    //    rv.RightTop = this.DataField.Field[new Point3D(c - 1, r - 1, this.Depth)];
                    //rect.Tag = rv;

                    Grid.SetRow(rect, this.nr_rows + 1 - r);
                    Grid.SetColumn(rect, c);
                    this.Children.Add(rect);
                }
            }
        }
        #endregion

        #region METHODS: Grid Re(Realize) (Functon Graphs) -VIRTUAL-

        protected virtual void FillDataCells()
        { }

        #endregion

        #region METHODS: Grid Update (Info Cells)

        private void UpdateInfoCells()
        {
            foreach (object child in this.Children)
            {
                this.UpdateOneInfoCellFromInternalArray(child);
            }
        }

        private void UpdateOneInfoCellFromInternalArray(object _info_cell_candidate)
        {
            TextBlock tb = _info_cell_candidate as TextBlock;
            if (tb == null) return;
            if (tb.Tag == null || !(tb.Tag is Point)) return;

            Point tb_info = (Point)tb.Tag;
            if (tb_info.Y == -1)
            {
                int index_x = (int)tb_info.X;
                if (index_x < 0 || index_x > this.xs.Count - 1) return;
                tb.Text = this.xs[index_x].ToString("F2", MValueFunct2DBase.FORMATTER);
            }
            else if (tb_info.X == -1)
            {
                int index_y = (int)tb_info.Y;
                if (index_y < 0 || index_y > this.ys.Count - 1) return;
                tb.Text = this.ys[index_y].ToString("F2", MValueFunct2DBase.FORMATTER);
            }
        }

        #endregion

        #region METHODS: Grid Update (Canvas)

        protected virtual void ReDrawAllLines()
        {
            if (this.canvas == null) return;

            this.canvas.Children.Clear();

            for (int pi = 0; pi < this.polylines.Count; pi++)
            {
                List<Point3D> pl = this.polylines[pi];
                if (pl == null) continue;
                int nrP = pl.Count;
                for (int i = 0; i < nrP; i++)
                {
                    this.DrawPoint(pl[i].X, pl[i].Y, pi,
                        (pi == this.ind_sel_polyline) ? FunctionEditState.SELECTED : FunctionEditState.REGULAR);
                    if (i == 0) continue;
                    this.DrawLine(pl[i - 1].X, pl[i - 1].Y, pl[i].X, pl[i].Y, new Point(pi, i - 1),
                        (pi == this.ind_sel_polyline) ? FunctionEditState.SELECTED : FunctionEditState.REGULAR);
                    if (i == nrP - 1)
                        this.DrawText(pl[i].X, pl[i].Y, pi, this.polylines_names[pi],
                            (pi == this.ind_sel_polyline) ? FunctionEditState.SELECTED : FunctionEditState.REGULAR);
                }
            }
        }

        #endregion

        #region METHODS: Communication w other Tables

        public void SetAxisValues()
        {
            this.AxisValues = new TableAxisValues(this.xs, this.ys);
            //// debug
            //var id = this.id;
            //double y0 = (this.ys != null && this.ys.Count > 0) ? this.ys[0] : double.MinValue;
            //// debug
        }

        public void SetAxisValues(TableAxisValues _input_axis, Point4D _input_bounds)
        {
            //// debug
            //var id = this.id;
            //double y0 = (this.ys != null && this.ys.Count > 0) ? this.ys[0] : double.MinValue;
            //double y0_in = (_input_axis.Ys.Count > 0) ? _input_axis.Ys[0] : double.MinValue;
            //// debug

            if (this.xs != null && this.ys != null) return;
            if (_input_axis.Xs.Count < 1 || _input_axis.Ys.Count < 1) return;

            this.bounds = _input_bounds;
            this.xs = new List<double>(_input_axis.Xs);
            this.ys = new List<double>(_input_axis.Ys);
            this.UpdateInfoCells();
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

        protected virtual void MValueFunct2DBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (!this.grid_set)
            {
                if (this.xs == null || this.xs.Count == 0)
                    this.xs = Enumerable.Range(-5, MValueFunct2DBase.MAX_NR_COLS).Select(x => (double)x * 2.0 / MValueFunct2DBase.MAX_NR_COLS).ToList();
                if (this.ys == null || this.ys.Count == 0)
                    this.ys = Enumerable.Range(-5, MValueFunct2DBase.MAX_NR_ROWS).Select(x => (double)x * 2.0 / MValueFunct2DBase.MAX_NR_ROWS).ToList();
                this.ResetGrid();
                this.RecalculateTableSizes(MValueFunct2DBase.MAX_NR_COLS, MValueFunct2DBase.MAX_NR_ROWS);
                this.RedefineTableGrid();
                this.SetUnitLabels();
                this.FillInfoCells();
                this.SetCanvas();
                this.FillOverlay();
                this.grid_set = true;
            }
        }

        protected virtual void line_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Line line = sender as Line;
            if (line == null) return;
            if (line.Tag == null) return;
            if (!(line.Tag is Point)) return;
            if (this.canvas == null) return;

            Point p = (Point)line.Tag;
            int index = (int)p.X;
            if (-1 < index && index < this.polylines.Count)
            {
                this.ind_sel_polyline = index;
            }
        }

        public virtual void Deselect()
        {
            this.ind_sel_polyline = -1;
        }

        #endregion

        #region METHODS: Utils (Scaling)

        protected void RescaleAxesLabels()
        {
            if (this.xs == null || this.ys == null) return;

            double min_x = this.xs.Min();
            double max_x = this.xs.Max();
            double step_x = this.xs[1] - this.xs[0];
            double range_x = max_x - min_x + step_x;

            double min_y = this.ys.Min();
            double max_y = this.ys.Max();
            double step_y = this.ys[1] - this.ys[0];
            double range_y = max_y - min_y + step_y;

            double scale_x = (this.Bounds.Y - this.Bounds.X) / range_x;
            double offset_x = this.Bounds.X - min_x * scale_x;

            double scale_y = (this.Bounds.W - this.Bounds.Z) / range_y;
            double offset_y = this.Bounds.Z - min_y * scale_y;

            List<double> xs_new = this.xs.Select(a => a * scale_x + offset_x).ToList();
            this.xs = new List<double>(xs_new);
            List<double> ys_new = this.ys.Select(a => a * scale_y + offset_y).ToList();
            this.ys = new List<double>(ys_new);

            this.fact_x = this.canvas.Width / (this.xs.Max() - this.xs.Min() + this.xs[1] - this.xs[0]);
            this.fact_y = this.canvas.Height / (this.ys.Max() - this.ys.Min() + this.ys[1] - this.ys[0]);
        }

        #endregion

        #region METHODS: Utils (Drawing)

        protected void DrawText(double _x, double _y, int _tag, string _content, FunctionEditState _state = FunctionEditState.REGULAR)
        {
            // scale input for drawing on the canvas
            double x_scaled = (_x - this.xs[0]) * this.fact_x;
            double y_scaled = this.canvas.Height - (_y - this.ys[0]) * this.fact_y;

            TextBlock tb = new TextBlock();
            tb.Height = 20;
            tb.Width = 60;
            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            tb.Margin = new Thickness(x_scaled - 60, y_scaled - 20, 0, 0);
            tb.Foreground = new SolidColorBrush(MValueFunct2DBase.SetColorAccToState(_state));
            tb.Tag = _tag;
            tb.Text = _content;

            this.canvas.Children.Add(tb);
        }

        protected void DrawPoint(double _x, double _y, int _tag, FunctionEditState _state = FunctionEditState.REGULAR)
        {
            // scale input for drawing on the canvas
            double x_scaled = (_x - this.xs[0]) * this.fact_x;
            double y_scaled = this.canvas.Height - (_y - this.ys[0]) * this.fact_y;

            Rectangle rect = new Rectangle();
            rect.Height = 6;
            rect.Width = 6;
            rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            rect.Margin = new Thickness(x_scaled - 3, y_scaled - 3, 0, 0);
            rect.Stroke = new SolidColorBrush(MValueFunct2DBase.SetColorAccToState(_state));
            rect.StrokeThickness = 2;
            rect.Tag = _tag;
            rect.ToolTip = "(" + _x.ToString("F2", MValueFunct2DBase.FORMATTER) + " " +
                                 _y.ToString("F2", MValueFunct2DBase.FORMATTER) + ")";
            this.canvas.Children.Add(rect);
        }

        protected void DrawLine(double _x1, double _y1, double _x2, double _y2, Point _tag, FunctionEditState _state = FunctionEditState.REGULAR)
        {
            // scale input for drawing on the canvas
            double x1_scaled = (_x1 - this.xs[0]) * this.fact_x;
            double y1_scaled = this.canvas.Height - (_y1 - this.ys[0]) * this.fact_y;
            double x2_scaled = (_x2 - this.xs[0]) * this.fact_x;
            double y2_scaled = this.canvas.Height - (_y2 - this.ys[0]) * this.fact_y;

            Line li = new Line();
            li.X1 = x1_scaled;
            li.Y1 = y1_scaled;
            li.X2 = x2_scaled;
            li.Y2 = y2_scaled;
            li.Stroke = new SolidColorBrush(MValueFunct2DBase.SetColorAccToState(_state));
            li.StrokeThickness = 2;
            li.Tag = _tag;
            li.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
            li.MouseUp += line_MouseUp;
            this.canvas.Children.Add(li);  
        }
        #endregion
    }
}
