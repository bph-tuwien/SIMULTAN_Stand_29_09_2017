using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace ComponentBuilder.UIElements
{
    public static class Utils
    {
        public static List<Line> DrawConnection(double _half_height_source, double half_height_target, double _side_width, double _mid_width,
                                                Point _source, Point _intermediate, Point _target, bool _only_source_part, Color _line_color, double _thickness, double _connection_offset = 0.0)
        {
            List<Line> lines = new List<Line>();

            // determine the offset sign
            int sign = (_source.Y + _half_height_source > _intermediate.Y + half_height_target) ? 1 : -1;

            // draw the SOURCE pointer
            Line s_L1 = new Line();
            s_L1.X1 = 0;
            s_L1.Y1 = _source.Y + _half_height_source;
            s_L1.X2 = _side_width + _connection_offset * sign;
            s_L1.Y2 = s_L1.Y1;
            s_L1.IsHitTestVisible = false;
            s_L1.Stroke = new SolidColorBrush(_line_color);
            s_L1.StrokeThickness = _thickness;

            Line s_L2 = new Line();
            s_L2.X1 = 0;
            s_L2.Y1 = _source.Y;
            s_L2.X2 = 0;
            s_L2.Y2 = _source.Y + _half_height_source * 2;
            s_L2.IsHitTestVisible = false;
            s_L2.Stroke = new SolidColorBrush(_line_color);
            s_L2.StrokeThickness = _thickness;

            Line s_L3 = new Line();
            s_L3.X1 = -10;
            s_L3.Y1 = _source.Y;
            s_L3.X2 = 0;
            s_L3.Y2 = s_L3.Y1;
            s_L3.IsHitTestVisible = false;
            s_L3.Stroke = new SolidColorBrush(_line_color);
            s_L3.StrokeThickness = _thickness;

            Line s_L4 = new Line();
            s_L4.X1 = -10;
            s_L4.Y1 = _source.Y + _half_height_source * 2;
            s_L4.X2 = 0;
            s_L4.Y2 = s_L4.Y1;
            s_L4.IsHitTestVisible = false;
            s_L4.Stroke = new SolidColorBrush(_line_color);
            s_L4.StrokeThickness = _thickness;

            lines.Add(s_L1);
            lines.Add(s_L2);
            lines.Add(s_L3);
            lines.Add(s_L4);

            if (!(_only_source_part))
            {
                // draw the current pointer
                Line c_L1 = new Line();
                c_L1.X1 = s_L1.X2;
                c_L1.Y1 = s_L1.Y2;
                c_L1.X2 = s_L1.X2;
                c_L1.Y2 = _intermediate.Y + half_height_target;
                c_L1.IsHitTestVisible = false;
                c_L1.Stroke = new SolidColorBrush(_line_color);
                c_L1.StrokeThickness = _thickness;

                Line c_L2 = new Line();
                c_L2.X1 = c_L1.X2;
                c_L2.Y1 = c_L1.Y2;
                c_L2.X2 = _mid_width;
                c_L2.Y2 = _intermediate.Y + half_height_target;
                c_L2.IsHitTestVisible = false;
                c_L2.Stroke = new SolidColorBrush(_line_color);
                c_L2.StrokeThickness = _thickness;

                lines.Add(c_L1);
                lines.Add(c_L2);

                // draw the TARGET pointer
                Line pointer = new Line();
                pointer.X1 = _mid_width;
                pointer.Y1 = _target.Y + half_height_target;
                pointer.X2 = _mid_width + _target.X;
                pointer.Y2 = _target.Y + half_height_target;
                pointer.IsHitTestVisible = false;
                pointer.Stroke = new SolidColorBrush(_line_color);
                pointer.StrokeThickness = _thickness;

                Line pointer_V = new Line();
                pointer_V.X1 = _mid_width + _target.X;
                pointer_V.Y1 = _target.Y;
                pointer_V.X2 = _mid_width + _target.X;
                pointer_V.Y2 = _target.Y + half_height_target * 2;
                pointer_V.IsHitTestVisible = false;
                pointer_V.Stroke = new SolidColorBrush(_line_color);
                pointer_V.StrokeThickness = _thickness;

                Line pointer_H1 = new Line();
                pointer_H1.X1 = _mid_width + _target.X;
                pointer_H1.Y1 = _target.Y;
                pointer_H1.X2 = _mid_width + _target.X + half_height_target;
                pointer_H1.Y2 = _target.Y;
                pointer_H1.IsHitTestVisible = false;
                pointer_H1.Stroke = new SolidColorBrush(_line_color);
                pointer_H1.StrokeThickness = _thickness;

                Line pointer_H2 = new Line();
                pointer_H2.X1 = _mid_width + _target.X;
                pointer_H2.Y1 = _target.Y + half_height_target * 2;
                pointer_H2.X2 = _mid_width + _target.X + half_height_target;
                pointer_H2.Y2 = _target.Y + half_height_target * 2;
                pointer_H2.IsHitTestVisible = false;
                pointer_H2.Stroke = new SolidColorBrush(_line_color);
                pointer_H2.StrokeThickness = _thickness;

                lines.Add(pointer);
                lines.Add(pointer_V);
                lines.Add(pointer_H1);
                lines.Add(pointer_H2);
            }

            return lines;
        }


    }
}
