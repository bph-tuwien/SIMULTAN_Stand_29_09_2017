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

using ParameterStructure.Parameter;
using ParameterStructure.Component;

namespace ComponentBuilder.GraphUIE
{
    class ParameterVisualization : NodeVisualization
    {
        #region CLASS MEMBERS

        private ParameterStructure.Parameter.Parameter node_parameter;

        #endregion

        #region .CTOR

        public ParameterVisualization(CompGraph _parent_canvas, ParameterStructure.Parameter.Parameter _data, double _offset_hrzt, double _offset_vert)
            :base(_parent_canvas, _data,
                    NodeVisualization.NODE_WIDTH_DEFAULT, NodeVisualization.NODE_HEIGHT_SMALL, _offset_hrzt, _offset_vert,
                    NodeVisualization.NODE_COLOR_STROKE_ACTIVE, NodeVisualization.NODE_COLOR_FILL_ACTIVE_3, NodeVisualization.NODE_COLOR_FILL_ACTIVE_2)
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
                this.node_parameter = this.node_data as ParameterStructure.Parameter.Parameter;
                this.node_parameter.PropertyChanged += node_param_PropertyChanged;
            }
        }

        #endregion

        #region PROPERTIES: Info
        public override Category VisCategory
        {
            get
            {
                if (this.node_parameter == null)
                    return Category.NoNe;
                else
                    return this.node_parameter.Category;
            }
        }

        public override long VisID
        {
            get
            {
                if (this.node_parameter == null)
                    return -1;
                else
                    return this.node_parameter.ID;
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
            if (this.node_parameter == null) return;

            // MAIN NODE
            Rectangle rect = new Rectangle();
            rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            rect.Height = this.node_height;
            rect.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            rect.Stroke = new SolidColorBrush(this.node_contour);
            rect.StrokeThickness = 1;
            rect.Fill = new LinearGradientBrush(this.node_fill_color_1, this.node_fill_color_2, new Point(0.5, 0), new Point(0.5, 1));
            rect.RadiusX = NodeVisualization.NODE_RADIUS_SMALL;
            rect.RadiusY = NodeVisualization.NODE_RADIUS_SMALL;
            rect.ToolTip = this.node_parameter.ToLongString();

            this.node_main = rect;
            this.Children.Add(rect);

            // MAIN NODE TEXT
            TextBlock tb = new TextBlock();
            tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tb.Padding = new Thickness(2);
            tb.Text = this.node_parameter.ToShortString();
            tb.FontSize = 10;
            tb.FontStyle = (this.node_parameter.Propagation == ParameterStructure.Component.InfoFlow.REF_IN) ? FontStyles.Italic : FontStyles.Normal;
            tb.FontWeight = (this.node_parameter.Propagation == InfoFlow.OUPUT || this.node_parameter.Propagation == InfoFlow.MIXED) ? FontWeights.Bold : FontWeights.Normal;
            tb.Foreground = new SolidColorBrush(NodeVisualization.NODE_FOREGROUND);
            tb.ToolTip = this.node_parameter.ToLongString();

            this.text_main = tb;
            this.Children.Add(tb);
        }

        #endregion

        #region METHODS: Info

        public override string ToString()
        {
            if (this.node_parameter != null)
                return "ParamViz: " + this.node_parameter.ID.ToString() + " " + this.node_parameter.Name;
            else
                return "ParamViz: ";
        }

        #endregion

        #region EVENT HANDLER

        protected void node_param_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ParameterStructure.Parameter.Parameter param = sender as ParameterStructure.Parameter.Parameter;
            if (param == null || e == null) return;

            if (e.PropertyName == "ValueCurrent" || e.PropertyName == "TextValue")
            {
                this.UpdateContent();
            }
        }

        #endregion
    }
}
