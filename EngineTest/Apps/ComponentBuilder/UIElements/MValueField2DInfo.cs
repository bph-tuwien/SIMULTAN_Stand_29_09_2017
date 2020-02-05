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

using ParameterStructure.Values; 

namespace ComponentBuilder.UIElements
{
    public class MValueField2DInfo : MValueField2DBase
    {
        #region STATIC
        
        public static readonly string MARK_X = "MARK_T_X";
        public static readonly string MARK_Y = "MARK_T_Y";
        public static readonly string MARK_TXT = "MARK_T_TXT";

        #endregion

        #region PROPERTIES: Data

        public MultiValueTable DataField
        {
            get { return (MultiValueTable)GetValue(DataFieldProperty); }
            set { SetValue(DataFieldProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DataField.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataFieldProperty =
            DependencyProperty.Register("DataField", typeof(MultiValueTable), typeof(MValueField2DInfo),
            new UIPropertyMetadata(null, new PropertyChangedCallback(DataFieldPropertyChangedCallback)));

        private static void DataFieldPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField2DInfo instance = d as MValueField2DInfo;
            if (instance == null) return;
            if (instance.DataField == null) return;

            instance.UnitX = instance.DataField.MVUnitX;
            instance.UnitY = instance.DataField.MVUnitY;
            instance.ResetGrid();
            instance.RecalculateTableSizes(instance.DataField.NrX, instance.DataField.NrY);
            instance.RedefineTableGrid();

            if (instance.DataField.NrX == 0)
                instance.xs = new List<double> { 0.0 };
            else
                instance.xs = new List<double>(instance.DataField.Xs);
            if (instance.DataField.NrY == 0)
                instance.ys = new List<double> { 0.0 };
            else
                instance.ys = new List<double>(instance.DataField.Ys);

            instance.LoadDataPoints();
            instance.SetUnitLabels();
            instance.FillInfoCells();
            instance.FillDataCells();

            instance.Loaded += instance.on_Loaded;
        }
        

        public int Depth // table index in a 3D MultiValueTable, otherwise 0
        {
            get { return (int)GetValue(DepthProperty); }
            set { SetValue(DepthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Depth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DepthProperty =
            DependencyProperty.Register("Depth", typeof(int), typeof(MValueField2DInfo), 
            new UIPropertyMetadata(0));

        #endregion

        #region PROPERTIES: Communication w other Tables

        private bool mark_set;
        public bool MarkSet
        {
            get { return this.mark_set; }
            set 
            { 
                this.mark_set = value;
                this.RegisterPropertyChanged("MarkSet");
            }
        }

        #endregion

        #region METHODS: Grid Realize (Info Cells)

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
                    TextBlock tbl = new TextBlock();
                    tbl.HorizontalAlignment = HorizontalAlignment.Left;
                    tbl.VerticalAlignment = VerticalAlignment.Bottom;
                    tbl.Margin = new Thickness(1);
                    tbl.Text = this.xs[r - 2].ToString("F2", MValueField2DBase.FORMATTER);
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
                rect.Width = MValueField2DBase.INFO_COL_WIDTH;
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
                    tbl.Text = this.ys[c - 2].ToString("F2", MValueField2DBase.FORMATTER);
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

        #region METHODS: Grid Realize (Data Cells)

        protected void LoadDataPoints()
        {
            this.InitializeDataPoints();
            for (int r = 0; r < this.ys.Count; r++)
            {
                for (int c = 0; c < this.xs.Count; c++)
                {
                    double value = this.DataField.Field[new Point3D(c, r, this.Depth)];
                    this.Fxys[r * MValueField2DBase.MAX_NR_COLS + c] = new Point3D(c, r, value);
                }
            }
        }

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
                    rect.MouseUp += this.rect_MouseUp;

                    RectangularValue rv = new RectangularValue();
                    rv.LeftBottom = this.DataField.Field[new Point3D(c - 2, r - 2, this.Depth)];
                    rv.RightBottom = (c == this.nr_columns + 1) ? rv.LeftBottom : this.DataField.Field[new Point3D(c - 1, r - 2, this.Depth)];
                    rv.LeftTop = (r == this.nr_rows + 1) ? rv.LeftBottom : this.DataField.Field[new Point3D(c - 2, r - 1, this.Depth)];
                    if (c == this.nr_columns + 1 && r == this.nr_rows + 1)
                        rv.RightTop = rv.LeftBottom;
                    else if (c == this.nr_columns + 1 && r < this.nr_rows + 1)
                        rv.RightTop = this.DataField.Field[new Point3D(c - 2, r - 1, this.Depth)];
                    else if (c < this.nr_columns + 1 && r == this.nr_rows + 1)
                        rv.RightTop = this.DataField.Field[new Point3D(c - 1, r - 2, this.Depth)];
                    else
                        rv.RightTop = this.DataField.Field[new Point3D(c - 1, r - 1, this.Depth)];
                    rect.Tag = rv;

                    Grid.SetRow(rect, this.nr_rows + 1 - r);
                    Grid.SetColumn(rect, c);
                    this.Children.Add(rect);

                    // value
                    TextBlock tbl = new TextBlock();
                    tbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    tbl.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                    tbl.Margin = new Thickness(1);
                    tbl.FontSize = 10;
                    tbl.Foreground = new SolidColorBrush(Colors.Black);
                    tbl.Tag = new Point3D(c - 2, r - 2, 0);
                    tbl.Text = this.Fxys[(r - 2) * MValueField2DBase.MAX_NR_COLS + (c - 2)].Z.ToString("F2", MValueField2DBase.FORMATTER);
                    tbl.IsHitTestVisible = false;

                    Grid.SetRow(tbl, this.nr_rows + 1 - r);
                    Grid.SetColumn(tbl, c);
                    this.Children.Add(tbl);
                }
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void on_Loaded(object sender, RoutedEventArgs e)
        {
            MultiValPointer pointer = this.DataField.MVDisplayVector;
            if (pointer != MultiValPointer.INVALID && pointer.CellIndices.Count > 2 && pointer.CellIndices[2] == this.Depth)
                this.SetMark(pointer.CellIndices[0], pointer.CellIndices[1], pointer.PosInCell_AbsolutePx, pointer.Value);
        }

        private void rect_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Rectangle rect = sender as Rectangle;
            if (rect == null) return;

            // communicate to others
            this.MarkSet = true;

            // detect position in the grid
            string name = rect.Name;
            string[] grid_pos = name.Split(new string[] { "x_", "__y_" }, StringSplitOptions.RemoveEmptyEntries);
            if (grid_pos == null || grid_pos.Length != 2) return;

            int row_index = -1, col_index = -1;
            int.TryParse(grid_pos[0], out col_index);
            int.TryParse(grid_pos[1], out row_index);
            if (row_index < 0 || col_index < 0) return;

            // detect position in the value table
            if (!(rect.Tag is RectangularValue)) return;

            RectangularValue rv = (RectangularValue)rect.Tag;

            Point pos = e.GetPosition(rect);
            // apply snap
            if ((rect.Width - pos.X) <= MValueField2DBase.SNAP_DIST_PX) pos.X = rect.Width;
            if (pos.X <= MValueField2DBase.SNAP_DIST_PX) pos.X = 0;
            if ((rect.Height - pos.Y) <= MValueField2DBase.SNAP_DIST_PX) pos.Y = rect.Height;
            if (pos.Y <= MValueField2DBase.SNAP_DIST_PX) pos.Y = 0;

            MultiValPointer pointer = new MultiValPointer(new List<int> { row_index, col_index, this.Depth },
                                                          new Point(this.column_width, this.row_height),
                                                          pos,
                                                          true, rv, this.DataField.MVCanInterpolate);
            this.DataField.MVDisplayVector = pointer;
            if (pointer != MultiValPointer.INVALID)
                this.SetMark(pointer.CellIndices[0], pointer.CellIndices[1], pointer.PosInCell_AbsolutePx, pointer.Value);
        }

        

        private void SetMark(int _row_index, int _col_index, Point _pos_in_cell, double _value)
        {
            // delete the old mark, if drawn
            this.DeleteMark();

            // add mark to show where the user clicked
            Rectangle mark_X = new Rectangle();
            mark_X.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            //mark_X.Height = Math.Max(this.row_height * this.nr_rows_IN, 1);
            mark_X.Width = 1;
            mark_X.Margin = new Thickness(_pos_in_cell.X - 1, 0, 0, 0);
            mark_X.StrokeThickness = 1;
            mark_X.Stroke = new SolidColorBrush(Colors.Red);
            mark_X.Name = MValueField2DInfo.MARK_X + this.Depth.ToString();
            this.RegisterName(mark_X.Name, mark_X);

            Grid.SetRow(mark_X, 0);
            Grid.SetRowSpan(mark_X, this.nr_rows + 1);
            Grid.SetColumn(mark_X, _col_index);
            this.Children.Add(mark_X);

            Rectangle mark_Y = new Rectangle();
            mark_Y.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            mark_Y.Height = 1;
            //mark_Y.Width = Math.Max(this.column_width * this.nr_columns, 1);
            mark_Y.Margin = new Thickness(0, _pos_in_cell.Y - 1, 0, 0);
            mark_Y.StrokeThickness = 1;
            mark_Y.Stroke = new SolidColorBrush(Colors.Red);
            mark_Y.Name = MValueField2DInfo.MARK_Y + this.Depth.ToString();
            this.RegisterName(mark_Y.Name, mark_Y);

            Grid.SetRow(mark_Y, _row_index);
            Grid.SetColumn(mark_Y, 1);
            Grid.SetColumnSpan(mark_Y, this.nr_columns + 1);
            this.Children.Add(mark_Y);

            TextBlock mark_TXT = new TextBlock();
            mark_TXT.HorizontalAlignment = HorizontalAlignment.Left;
            mark_TXT.VerticalAlignment = VerticalAlignment.Top;
            mark_TXT.Margin = new Thickness(_pos_in_cell.X, _pos_in_cell.Y, 0, 0);
            mark_TXT.Text = _value.ToString("F2", MValueField2DBase.FORMATTER);
            mark_TXT.FontSize = 10;
            mark_TXT.FontWeight = FontWeights.Bold;
            mark_TXT.IsHitTestVisible = false;
            mark_TXT.Foreground = new SolidColorBrush(Colors.Red);
            mark_TXT.Background = new SolidColorBrush(Colors.White);
            mark_TXT.Name = MValueField2DInfo.MARK_TXT + this.Depth.ToString();
            this.RegisterName(mark_TXT.Name, mark_TXT);

            Grid.SetRow(mark_TXT, _row_index);
            Grid.SetRowSpan(mark_TXT, 2);
            Grid.SetColumn(mark_TXT, _col_index);
            Grid.SetColumnSpan(mark_TXT, 2);
            this.Children.Add(mark_TXT);
        }

        public void DeleteMark()
        {
            Rectangle old_mark_X = (Rectangle)this.FindName(MValueField2DInfo.MARK_X + this.Depth.ToString());
            if (old_mark_X != null)
            {
                this.Children.Remove(old_mark_X);
                this.UnregisterName(MValueField2DInfo.MARK_X + this.Depth.ToString());
            }
            Rectangle old_mark_Y = (Rectangle)this.FindName(MValueField2DInfo.MARK_Y + this.Depth.ToString());
            if (old_mark_Y != null)
            {
                this.Children.Remove(old_mark_Y);
                this.UnregisterName(MValueField2DInfo.MARK_Y + this.Depth.ToString());
            }
            TextBlock old_mark_TXT = (TextBlock)this.FindName(MValueField2DInfo.MARK_TXT + this.Depth.ToString());
            if (old_mark_TXT != null)
            {
                this.Children.Remove(old_mark_TXT);
                this.UnregisterName(MValueField2DInfo.MARK_TXT + this.Depth.ToString());
            }
        }

        #endregion
    }
}
