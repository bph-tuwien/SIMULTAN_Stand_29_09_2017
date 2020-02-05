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
    public class MValueFunct2DInput : MValueFunct2DBase
    {
        #region PROPERTIES: New Point

        public bool FinalizeFunction
        {
            get { return (bool)GetValue(FinalizeFunctionProperty); }
            set { SetValue(FinalizeFunctionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FinalizeFunction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FinalizeFunctionProperty =
            DependencyProperty.Register("FinalizeFunction", typeof(bool), typeof(MValueFunct2DBase),
            new UIPropertyMetadata(false, new PropertyChangedCallback(FinalizeFunctionPropertyChangedCallback)));

        private static void FinalizeFunctionPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct2DInput instance = d as MValueFunct2DInput;
            if (instance == null) return;

            if (instance.FinalizeFunction)
            {
                instance.polylines.Add(instance.polyline);
                instance.polylines_names.Add(instance.FunctionName);
                instance.polyline = new List<Point3D>();
                instance.FinalizeFunction = false;
                instance.ReDrawAllLines();
            }
        }

        public string  FunctionName
        {
            get { return (string )GetValue(FunctionNameProperty); }
            set { SetValue(FunctionNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FunctionName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FunctionNameProperty =
            DependencyProperty.Register("FunctionName", typeof(string ), typeof(MValueFunct2DInput), 
            new UIPropertyMetadata(null));

        public Point3D NewPoint
        {
            get { return (Point3D)GetValue(NewPointProperty); }
            set { SetValue(NewPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NewPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NewPointProperty =
            DependencyProperty.Register("NewPoint", typeof(Point3D), typeof(MValueFunct2DInput),
            new UIPropertyMetadata(new Point3D(0, 0, -1), new PropertyChangedCallback(NewPointPropertyChangedCallback)));

        private static void NewPointPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MValueFunct2DInput instance = d as MValueFunct2DInput;
            if (instance == null) return;
            if (instance.NewPoint == null) return;

            instance.AddPointToFunction();
            instance.AddLineToCanvas();
        }

        #endregion

        #region CLASS MEMBERS

        protected List<Point3D> polyline = new List<Point3D>();

        #endregion

        #region METHODS: Add Point To Function, Add Function to Table

        private void AddPointToFunction()
        {
            // adapt the bounds
            //                                  min x           max x        min y           max y
            Point4D bounds_new = new Point4D(this.Bounds.X, this.Bounds.Y, this.Bounds.Z, this.Bounds.W);
            if (this.NewPoint.X < bounds_new.X)
                bounds_new.X = this.NewPoint.X;
            if (this.NewPoint.X > bounds_new.Y)
                bounds_new.Y = this.NewPoint.X;

            if (this.NewPoint.Y < bounds_new.Z)
                bounds_new.Z = this.NewPoint.Y;
            if (this.NewPoint.Y > bounds_new.W)
                bounds_new.W = this.NewPoint.Y;

            if (this.Bounds != bounds_new)
                this.Bounds = new Point4D(bounds_new.X, bounds_new.Y, bounds_new.Z, bounds_new.W);

            this.polyline.Add(this.NewPoint);
        }

        public void AddFunction(List<Point3D> _function, string _function_name)
        {
            if (_function == null || _function.Count < 1) return;

            if (this.polylines == null)
                this.polylines = new List<List<Point3D>>();
            if (this.polylines_names == null)
                this.polylines_names = new List<string>();

            this.polylines.Add(_function);
            this.polylines_names.Add(_function_name);
        }

        #endregion

        #region METHODS: Grid Update (Canvas)

        protected void AddLineToCanvas()
        {
            if (this.canvas == null) return;
            int pl_index = this.polylines.Count;
            // POINT
            int nrP = this.polyline.Count;
            if (nrP < 1) return;
            this.DrawPoint(this.polyline[nrP - 1].X, this.polyline[nrP - 1].Y, pl_index, FunctionEditState.IS_BEING_DRAWIN);

            // LINE
            if (nrP < 2) return;
            this.DrawLine(this.polyline[nrP - 2].X, this.polyline[nrP - 2].Y,
                          this.polyline[nrP - 1].X, this.polyline[nrP - 1].Y, new Point(pl_index, nrP - 2), FunctionEditState.IS_BEING_DRAWIN);  
        }

        protected override void ReDrawAllLines()
        {
            base.ReDrawAllLines();

            int nrP_a = this.polyline.Count;
            for (int i = 0; i < nrP_a; i++)
            {
                this.DrawPoint(this.polyline[i].X, this.polyline[i].Y, this.polylines.Count, FunctionEditState.IS_BEING_DRAWIN);
                if (i == 0) continue;
                this.DrawLine(this.polyline[i - 1].X, this.polyline[i - 1].Y,
                              this.polyline[i].X, this.polyline[i].Y, new Point(this.polylines.Count, i - 1), FunctionEditState.IS_BEING_DRAWIN);               
            }
        }

        #endregion

        #region EVENT HANDLERS

        protected override void MValueFunct2DBase_Loaded(object sender, RoutedEventArgs e)
        {
            if (!this.grid_set)
            {
                base.MValueFunct2DBase_Loaded(sender, e);
                this.RescaleAxesLabels();
                this.SetAxisValues();
                this.ReDrawAllLines();
            }
        }

        protected override void line_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            base.line_MouseUp(sender, e);
            this.ReDrawAllLines();
        }

        public override void Deselect()
        {
            if (this.ind_sel_polyline != -1)
            {
                this.ind_sel_polyline = -1;
                this.ReDrawAllLines();
            }
        }

        public void DeleteSelected()
        {
            if (-1 < this.ind_sel_polyline && this.ind_sel_polyline < this.polylines.Count)
            {
                this.polylines.RemoveAt(this.ind_sel_polyline);
                this.polylines_names.RemoveAt(this.ind_sel_polyline);
                this.ind_sel_polyline = -1;
                this.ReDrawAllLines();
            }
        }

        #endregion

    }
}
