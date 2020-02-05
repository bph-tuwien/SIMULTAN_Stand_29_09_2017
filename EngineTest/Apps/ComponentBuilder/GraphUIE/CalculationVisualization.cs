using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Globalization;

namespace ComponentBuilder.GraphUIE
{
    class CalculationVisualization : NodeVisualization
    {
        #region CLASS MEMBERS

        protected ParameterStructure.Parameter.Calculation node_calculation;

        #endregion

        #region .CTOR

        public CalculationVisualization(CompGraph _parent_canvas, ParameterStructure.Parameter.Calculation _data, double _offset_hrzt, double _offset_vert)
            :base(_parent_canvas, _data,
                    NodeVisualization.NODE_WIDTH_DEFAULT, NodeVisualization.NODE_HEIGHT_SMALL, _offset_hrzt, _offset_vert,
                    NodeVisualization.NODE_COLOR_STROKE_ACTIVE, NodeVisualization.NODE_COLOR_FILL_ACTIVE_4, NodeVisualization.NODE_COLOR_FILL_ACTIVE_3)
        {
            this.Width = this.node_width;
            this.Height = this.node_height;
            this.Extents = new BoundingBox()
            {
                UpperLeft = this.position,
                LowerRight = new Point(this.position.X + this.Width, this.position.Y + this.Height)
            };

            if (this.node_data != null)
            {
                this.node_calculation = this.node_data as ParameterStructure.Parameter.Calculation;
                this.node_calculation.PropertyChanged += node_calc_PropertyChanged;
            }
        }

        #endregion

        #region PROPERTIES: Info

        public override long VisID
        {
            get
            {
                if (this.node_calculation == null)
                    return -1;
                else
                    return this.node_calculation.ID;
            }
        }

        public List<long> ParamsInIDs
        {
            get
            {
                if (this.node_calculation == null)
                    return new List<long>();
                else
                {
                    List<long> ids_in = new List<long>();
                    foreach(var entry in this.node_calculation.InputParams)
                    {
                        if (entry.Value != null)
                            ids_in.Add(entry.Value.ID);
                    }
                    return ids_in;
                }
            }
        }

        public List<long> ParamsOutIDs
        {
            get
            {
                if (this.node_calculation == null)
                    return new List<long>();
                else
                {
                    List<long> ids_out = new List<long>();
                    foreach (var entry in this.node_calculation.ReturnParams)
                    {
                        if (entry.Value != null)
                            ids_out.Add(entry.Value.ID);
                    }
                    return ids_out;
                }
            }
        }

        #endregion

        #region METHODS: Display Update

        protected override void RedefineGrid()
        {
            // reset grid
            this.Children.Clear();
        }

        protected override void PopulateGrid()
        {
            if (this.node_calculation == null) return;

            // MAIN NODE
            Rectangle rect = new Rectangle();
            rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            rect.Height = this.node_height;
            rect.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            rect.Stroke = new SolidColorBrush(this.node_contour);
            rect.StrokeThickness = 1.5;
            //rect.StrokeDashArray = new DoubleCollection{ 5, 5};
            rect.Fill = new LinearGradientBrush(this.node_fill_color_1, this.node_fill_color_2, new Point(0.5, 0), new Point(0.5, 1));
            rect.RadiusX = NodeVisualization.NODE_RADIUS * 2;
            rect.RadiusY = NodeVisualization.NODE_RADIUS * 2;
            rect.ToolTip = this.node_calculation.ToLongString();

            this.node_main = rect;
            this.Children.Add(rect);

            // MAIN NODE TEXT
            TextBlock tb = new TextBlock();
            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tb.Padding = new Thickness(2);
            tb.Text = this.node_calculation.ToShortString();
            tb.FontSize = 10;
            tb.Foreground = new SolidColorBrush(NodeVisualization.NODE_FOREGROUND);
            tb.ToolTip = this.node_calculation.ToLongString();

            this.text_main = tb;
            this.Children.Add(tb);
        }

        #endregion

        #region METHODS: Info

        public override string ToString()
        {
            if (this.node_calculation != null)
                return "CalculationViz: " + this.node_calculation.ID.ToString() + " " + this.node_calculation.Expression;
            else
                return "CalculationViz: ";
        }

        #endregion

        #region EVENT HANDLER

        protected void node_calc_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ParameterStructure.Parameter.Parameter param = sender as ParameterStructure.Parameter.Parameter;
            if (param == null || e == null) return;

            if (e.PropertyName == "InputParams" || e.PropertyName == "ReturnParams" || e.PropertyName == "Expression")
            {
                this.UpdateContent();
            }
        }

        #endregion
    }
}
