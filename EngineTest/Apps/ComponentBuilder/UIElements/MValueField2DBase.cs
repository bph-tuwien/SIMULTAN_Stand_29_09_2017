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
    public abstract class MValueField2DBase : Grid, INotifyPropertyChanged
    {
        #region STATIC: General

        private static int NR_INSTANCES = 0;

        protected static readonly int MAX_NR_COLS = 10;
        protected static readonly int MAX_NR_ROWS = 10;

        protected static readonly int INFO_COL_WIDTH = 35;
        protected static readonly int INFO_ROW_HEIGHT = 20;

        protected static readonly int MIN_COL_WIDTH = 25;
        protected static readonly int MIN_ROW_HEIGHT = 20;

        protected static readonly int MAX_COL_WIDTH = 90;
        protected static readonly int MAX_ROW_HEIGHT = 30;

        protected static readonly int UNIT_LABEL_COL_HEIGHT = 28;
        protected static readonly string UNIT_LABEL_X_TAG = "UNIT_X";
        protected static readonly string UNIT_LABEL_Y_TAG = "UNIT_Y";

        protected static readonly Point3D INVALID_DATA = new Point3D(-1, -1, -1);

        protected static readonly IFormatProvider FORMATTER = new NumberFormatInfo();

        public static readonly int SNAP_DIST_PX = 3;

        #endregion

        private int id;
        public MValueField2DBase()
            :base()
        {
            this.id = (++NR_INSTANCES);
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
            DependencyProperty.Register("UnitX", typeof(string), typeof(MValueField2DInput),
            new UIPropertyMetadata("unit X", new PropertyChangedCallback(UnitXYPropertyChangedCallback)));

        public string UnitY
        {
            get { return (string)GetValue(UnitYProperty); }
            set { SetValue(UnitYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UnitY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitYProperty =
            DependencyProperty.Register("UnitY", typeof(string), typeof(MValueField2DInput),
            new UIPropertyMetadata("unit Y", new PropertyChangedCallback(UnitXYPropertyChangedCallback)));

        private static void UnitXYPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueField2DBase instance = d as MValueField2DBase;
            if (instance == null) return;
            instance.UpdateUnitLabels();
        }

        #endregion

        #region CLASS MEMBERS

        //protected MultiValueTable data_table;
        protected List<double> xs;
        protected List<double> ys;
        protected List<Point3D> Fxys;

        protected bool size_original_set = false;
        protected double width_original = 0;
        protected double height_original = 0;

        protected int nr_columns = 0;
        protected int nr_rows = 0;
        protected int column_width = 0;
        protected int row_height = 0;

        public List<double> Xs { get { return this.xs; } }
        public List<double> Ys { get { return this.ys; } }

        public List<double>Field
        {
            get
            {
                List<double> values = new List<double>();
                for (int r = 0; r < this.ys.Count; r++)
                {
                    for (int c = 0; c < this.xs.Count; c++)
                    {
                        values.Add(this.Fxys[r * MValueField2DBase.MAX_NR_COLS + c].Z);
                    }
                }
                return values;
            }
        }

        #endregion

        #region METHOD: Initialize Internal Value Containers

        //   0 1 2 3 c
        // 0 - - - >
        // 1 - - - >
        // r
        protected void InitializeDataPoints()
        {
            this.Fxys = new List<Point3D>();
            for (int r = 0; r < MValueField2DBase.MAX_NR_ROWS; r++)
            {
                for(int c = 0; c < MValueField2DBase.MAX_NR_COLS; c++)
                {
                    // [r * MValueField2DBase.MAX_NR_COLS + c]
                    this.Fxys.Add(MValueField2DBase.INVALID_DATA);
                }
            }
        }

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
            this.nr_columns = _nrX;
            this.nr_rows = (_nrY == 0) ? 1 : _nrY;

            int col_B = Math.Max((int)Math.Floor((this.width_original - MValueField2DBase.INFO_COL_WIDTH - MValueField2DBase.UNIT_LABEL_COL_HEIGHT) / this.nr_columns), MValueField2DBase.MIN_COL_WIDTH);
            col_B = Math.Min(col_B, MValueField2DBase.MAX_COL_WIDTH);
            int row_H = Math.Max((int)Math.Floor((this.height_original - MValueField2DBase.INFO_ROW_HEIGHT - MValueField2DBase.UNIT_LABEL_COL_HEIGHT) / this.nr_rows), MValueField2DBase.MIN_ROW_HEIGHT);
            row_H = Math.Min(row_H, MValueField2DBase.MAX_ROW_HEIGHT);

            // re-calculate the size of the grid
            this.Width = MValueField2DBase.INFO_COL_WIDTH + MValueField2DBase.UNIT_LABEL_COL_HEIGHT + this.nr_columns * col_B;
            this.Height = MValueField2DBase.INFO_ROW_HEIGHT + MValueField2DBase.UNIT_LABEL_COL_HEIGHT + this.nr_rows * row_H;

            // save cell sizes            
            this.column_width = col_B;
            this.row_height = row_H;
        }

        protected void RedefineTableGrid()
        {
            
            ColumnDefinition cd_uY = new ColumnDefinition();
            cd_uY.Width = new GridLength(MValueField2DBase.UNIT_LABEL_COL_HEIGHT);
            this.ColumnDefinitions.Add(cd_uY);

            ColumnDefinition cd0 = new ColumnDefinition();
            cd0.Width = new GridLength(MValueField2DBase.INFO_COL_WIDTH);
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
            rd0.Height = new GridLength(MValueField2DBase.INFO_ROW_HEIGHT);
            this.RowDefinitions.Add(rd0);

            RowDefinition rd_uX = new RowDefinition();
            rd_uX.Height = new GridLength(MValueField2DBase.UNIT_LABEL_COL_HEIGHT);
            this.RowDefinitions.Add(rd_uX);

            //this.ShowGridLines = true;
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
            tbX.Tag = MValueField2DBase.UNIT_LABEL_X_TAG;

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
            tbY.Tag = MValueField2DBase.UNIT_LABEL_Y_TAG;

            Grid.SetRow(tbY, this.nr_rows - 1);
            Grid.SetRowSpan(tbY, this.nr_rows);
            Grid.SetColumn(tbY, 0);
            Grid.SetColumnSpan(tbY, this.nr_columns + 1);
            this.Children.Add(tbY);
        }

        protected void UpdateUnitLabels()
        {
            foreach(var child in this.Children)
            {
                TextBlock tb = child as TextBlock;
                if (tb == null) continue;
                if (tb.Tag == null) continue;
                if (!(tb.Tag is string)) continue;

                if (tb.Tag.ToString() == MValueField2DBase.UNIT_LABEL_X_TAG)
                    tb.Text = this.UnitX;
                if (tb.Tag.ToString() == MValueField2DBase.UNIT_LABEL_Y_TAG)
                    tb.Text = this.UnitY;
            }
        }

        #endregion

        #region METHODS: Grid (Re)Realize (Info Cells) -VIRTUAL-

        protected virtual void FillInfoCells()
        {        }

        #endregion

        #region METHODS: Grid Re(Realize) (Data Cells) -VIRTUAL-

        protected virtual void FillDataCells()
        {     }

        #endregion

    }
}
