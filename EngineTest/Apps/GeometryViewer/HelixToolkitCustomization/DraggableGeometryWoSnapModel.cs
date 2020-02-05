using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.HelixToolkitCustomization
{
    public class DraggableGeometryWoSnapModel3D : DraggableGeometryModel3D
    {
        private MatrixTransform3D dragTransform;
        
        public DraggableGeometryWoSnapModel3D()
        {
            this.dragTransform = new MatrixTransform3D(base.Transform.Value);
        }

        public override void OnMouse3DMove(object sender, RoutedEventArgs e)
        {
            base.OnMouse3DMove(sender, e);
        }

        public void OnMouse3DMoveDelayed(Point3D _pos_override)
        {
            if (this.isCaptured)
            {
                Vector3D vectord2 = _pos_override - this.lastHitPos;
                Matrix3D matrixd = base.Transform.Value;
                if (this.DragX)
                {
                    matrixd.OffsetX += vectord2.X;
                }
                if (this.DragY)
                {
                    matrixd.OffsetY += vectord2.Y;
                }
                if (this.DragZ)
                {
                    matrixd.OffsetZ += vectord2.Z;
                }
                this.dragTransform.Matrix = matrixd;
                base.Transform = this.dragTransform;
                this.lastHitPos = _pos_override;                
            }
        }

        public void OnMouseDownOverride(Point3D _pos_override)
        {
            if (this.isCaptured)
            {
                this.lastHitPos = _pos_override;
            }
        }

    }
}
