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
    public class MValueFunct2DInfo : MValueFunct2DBase
    {
        #region STATIC

        public static readonly string MARK_X = "MARK_F_X";
        public static readonly string MARK_Y = "MARK_F_Y";
        public static readonly string MARK_TXT = "MARK_F_TXT";

        #endregion

        #region PROPERTIES: Data

        public MultiValueFunction DataField
        {
            get { return (MultiValueFunction)GetValue(DataFieldProperty); }
            set { SetValue(DataFieldProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DataField.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataFieldProperty =
            DependencyProperty.Register("DataField", typeof(MultiValueFunction), typeof(MValueFunct2DInfo),
            new UIPropertyMetadata(null, new PropertyChangedCallback(DataFieldPropertyChangedCallback)));

        private static void DataFieldPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct2DInfo instance = d as MValueFunct2DInfo;
            if (instance == null) return;
            if (instance.DataField == null) return;

            instance.UnitX = instance.DataField.MVUnitX;
            instance.UnitY = instance.DataField.MVUnitY;

            // fill in the function graphs
            for (int i = 0; i < instance.DataField.Graphs.Count; i++ )
            {
                List<Point3D> funct = instance.DataField.Graphs[i];
                string name = instance.DataField.Graph_Names[i];
                if (funct != null && funct.Count >= 2 && funct[0].Z == instance.Depth)
                {
                    instance.polylines.Add(funct);
                    instance.polylines_names.Add(name);
                }
            }

            // set the bounds -> triggers rescaling and redrawing of all functions
            instance.Bounds = new Point4D(instance.DataField.MinX, instance.DataField.MaxX,
                                          instance.DataField.MinY, instance.DataField.MaxY);
        }

        public int Depth // table index in a 3D MultiValueTable, otherwise 0
        {
            get { return (int)GetValue(DepthProperty); }
            set { SetValue(DepthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Depth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DepthProperty =
            DependencyProperty.Register("Depth", typeof(int), typeof(MValueFunct2DInfo), 
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

        #region EVENT HANDLER

        protected override void MValueFunct2DBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (!this.grid_set)
            {
                base.MValueFunct2DBase_Loaded(sender, e);
                this.RescaleAxesLabels();
                this.SetAxisValues();
                this.ApplyAxisValues(this.AxisValues);
                this.ReDrawAllLines();
            }

            MultiValPointer pointer = this.DataField.MVDisplayVector;
            if (pointer != MultiValPointer.INVALID && pointer.CellIndices.Count > 2 && pointer.CellIndices[2] == this.Depth)
                this.SetMark(pointer.PosInCell_AbsolutePx, pointer.Value);
        }

        protected override void line_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Line line = sender as Line;
            if (line == null) return;
            if (this.canvas == null) return;
            if (this.fact_x == 0 || this.fact_y == 0) return;

            // save the graph indices (added 27.10.2016)
            int ind_graph = -1;
            int ind_line_segm = -1;
            if (line.Tag != null)
            {
                if (line.Tag is Point)
                {
                    Point p = (Point)line.Tag;
                    ind_graph = (int)p.X;
                    ind_line_segm = (int)p.Y;
                }
            }

            // communicate to others
            this.MarkSet = true;

            Point pos = e.GetPosition(this.canvas);
            // apply snap
            if (Math.Abs(line.X1 - pos.X) <= MValueFunct2DBase.SNAP_DIST_PX &&
                Math.Abs(line.Y1 - pos.Y) <= MValueFunct2DBase.SNAP_DIST_PX)
            {
                pos.X = line.X1;
                pos.Y = line.Y1;
            }
            if (Math.Abs(line.X2 - pos.X) <= MValueFunct2DBase.SNAP_DIST_PX &&
                Math.Abs(line.Y2 - pos.Y) <= MValueFunct2DBase.SNAP_DIST_PX)
            {
                pos.X = line.X2;
                pos.Y = line.Y2;
            }

            double x_left = Math.Min(line.X1, line.X2);
            double x_right = Math.Max(line.X1, line.X2);
            double y_top = Math.Min(line.Y1, line.Y2);
            double y_bottom = Math.Max(line.Y1, line.Y2);
            
            double y_left = 0;
            double y_right = 0;
            if (x_left == line.X1)
            {
                y_left = line.Y1;
                y_right = line.Y2;
            }
            else
            {
                y_left = line.Y2;
                y_right = line.Y1;
            }

            Point pos_rel = new Point(0, 0);
            pos_rel.X = (Math.Abs(line.X1 - line.X2) < ParameterStructure.Parameter.Calculation.MIN_DOUBLE_VAL) ? 1 : Math.Abs(x_left - pos.X) / Math.Abs(line.X1 - line.X2);
            pos_rel.Y = (Math.Abs(line.Y1 - line.Y2) < ParameterStructure.Parameter.Calculation.MIN_DOUBLE_VAL) ? 1 : Math.Abs(y_bottom - pos.Y) / Math.Abs(line.Y1 - line.Y2);
            if (pos_rel.X < 0) pos_rel.X = 0;
            if (pos_rel.X > 1) pos_rel.X = 1;
            if (pos_rel.Y < 0) pos_rel.Y = 0;
            if (pos_rel.Y > 1) pos_rel.Y = 1;

            // calculate the interpolated value
            // scaling ACTUAL -> SCALED
            // double x1_scaled = (_x1 - this.xs[0]) * this.fact_x;
            // double y1_scaled = this.canvas.Height - (_y1 - this.ys[0]) * this.fact_y;

            // scaling SCALED -> ACTUAL
            RectangularValue rv = new RectangularValue();
            if (Math.Abs(x_left - x_right) >= ParameterStructure.Parameter.Calculation.MIN_DOUBLE_VAL)
            {
                rv.LeftBottom = (this.canvas.Height - y_left) / this.fact_y + this.ys[0];
                rv.LeftTop = rv.LeftBottom;
                rv.RightBottom = (this.canvas.Height - y_right) / this.fact_y + this.ys[0];
                rv.RightTop = rv.RightBottom;
            }
            else
            {
                // account for vertical function segments
                rv.LeftBottom = (this.canvas.Height - y_bottom) / this.fact_y + this.ys[0];
                rv.RightBottom = rv.LeftBottom;
                rv.LeftTop = (this.canvas.Height - y_top) / this.fact_y + this.ys[0];
                rv.RightTop = rv.LeftTop;
            }

            MultiValPointer pointer = new MultiValPointer(new List<int> { ind_graph, ind_line_segm, this.Depth },
                                                          new Point(Math.Abs(line.X1 - line.X2), Math.Abs(line.Y1 - line.Y2)),
                                                          pos_rel, 
                                                          false, rv, true,
                                                          x_left, y_top);
            this.DataField.MVDisplayVector = pointer;
            if (pointer != MultiValPointer.INVALID)
                this.SetMark(pos, pointer.Value);
        }

        private void SetMark(Point _pos_on_canvas, double _value)
        {
            // delete the old mark, if drawn
            this.DeleteMark();

            // add mark to show where the user clicked
            Line mark_X = new Line();
            mark_X.X1 = 0;
            mark_X.X2 = this.canvas.Width;
            mark_X.Y1 = _pos_on_canvas.Y+0.5; // 0.5 works against the aliasing algorithm, which doubles a thin line lying exactly btw 2 pixels
            mark_X.Y2 = _pos_on_canvas.Y+0.5;
            mark_X.StrokeThickness = 1;
            mark_X.Stroke = new SolidColorBrush(Colors.Red);
            mark_X.Name = MValueFunct2DInfo.MARK_X + this.Depth.ToString();            
            this.RegisterName(mark_X.Name, mark_X);

            this.canvas.Children.Add(mark_X);

            Line mark_Y = new Line();
            mark_Y.X1 = _pos_on_canvas.X+0.5;
            mark_Y.X2 = _pos_on_canvas.X+0.5;
            mark_Y.Y1 = 0;
            mark_Y.Y2 = this.canvas.Height;
            mark_Y.StrokeThickness = 1;
            mark_Y.Stroke = new SolidColorBrush(Colors.Red);
            mark_Y.Name = MValueFunct2DInfo.MARK_Y + this.Depth.ToString();
            this.RegisterName(mark_Y.Name, mark_Y);

            this.canvas.Children.Add(mark_Y);            

            TextBlock mark_TXT = new TextBlock();
            mark_TXT.HorizontalAlignment = HorizontalAlignment.Left;
            mark_TXT.VerticalAlignment = VerticalAlignment.Top;
            mark_TXT.Margin = new Thickness(_pos_on_canvas.X + 1, _pos_on_canvas.Y + 1, 0, 0);
            mark_TXT.Text = _value.ToString("F2", MValueFunct2DBase.FORMATTER);
            mark_TXT.FontSize = 10;
            mark_TXT.FontWeight = FontWeights.Bold;
            mark_TXT.IsHitTestVisible = false;
            mark_TXT.Foreground = new SolidColorBrush(Colors.Red);
            mark_TXT.Background = new SolidColorBrush(Colors.White);
            mark_TXT.Name = MValueFunct2DInfo.MARK_TXT + this.Depth.ToString();
            this.RegisterName(mark_TXT.Name, mark_TXT);

            Grid.SetRow(mark_TXT, 0);
            Grid.SetRowSpan(mark_TXT, this.nr_rows);
            Grid.SetColumn(mark_TXT, 2);
            Grid.SetColumnSpan(mark_TXT, this.nr_columns);
            this.Children.Add(mark_TXT);
        }

        public void DeleteMark()
        {
            Line old_mark_X = (Line)this.canvas.FindName(MValueFunct2DInfo.MARK_X + this.Depth.ToString());
            if (old_mark_X != null)
            {
                this.canvas.Children.Remove(old_mark_X);
                this.UnregisterName(MValueFunct2DInfo.MARK_X + this.Depth.ToString());
            }
            Line old_mark_Y = (Line)this.canvas.FindName(MValueFunct2DInfo.MARK_Y + this.Depth.ToString());
            if (old_mark_Y != null)
            {
                this.canvas.Children.Remove(old_mark_Y);
                this.UnregisterName(MValueFunct2DInfo.MARK_Y + this.Depth.ToString());
            }
            TextBlock old_mark_TXT = (TextBlock)this.FindName(MValueFunct2DInfo.MARK_TXT + this.Depth.ToString());
            if (old_mark_TXT != null)
            {
                this.Children.Remove(old_mark_TXT);
                this.UnregisterName(MValueFunct2DInfo.MARK_TXT + this.Depth.ToString());
            }
        }

        #endregion
    }
}
