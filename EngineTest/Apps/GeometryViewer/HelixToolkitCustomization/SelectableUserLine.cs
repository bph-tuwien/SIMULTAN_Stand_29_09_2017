using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.HelixToolkitCustomization
{
    class SelectableUserLine : UserLine
    {
        // DEPENDENCY PROPPERTIES FOR BINDING
        public ulong? IndexSelected
        {
            get { return (ulong?)GetValue(IndexSelectedProperty); }
            set { SetValue(IndexSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IndexSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IndexSelectedProperty =
            DependencyProperty.Register("IndexSelected", typeof(ulong?), typeof(SelectableUserLine),
                    new UIPropertyMetadata(null));

        public Point3D HitPos
        {
            get { return (Point3D)GetValue(HitPosProperty); }
            set { SetValue(HitPosProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HitPos.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HitPosProperty =
            DependencyProperty.Register("HitPos", typeof(Point3D), typeof(SelectableUserLine),
            new UIPropertyMetadata(new Point3D()));

 
        public override bool HitTest(Ray rayWS, ref List<HitTestResult> hits)
        {
            if (!this.IsHitTestVisible)
                return false;

            var result = MyHitTest(rayWS, ref hits);
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (result)
                {
                    ulong tagVal = 0;
                    bool tagValIsValid = System.UInt64.TryParse(this.hResult.Tag.ToString(), out tagVal);
                    if (tagValIsValid)
                    {
                        // in order to trigger event even when the same index is selected twice in succession
                        this.IndexSelected = null;
                        this.IndexSelected = tagVal;
                        this.HitPos = this.hResult.PointHit;
                    }
                }
                else
                {
                    this.IndexSelected = null;
                    this.HitPos = new Point3D();
                }
            }

            return result;
        }

    }
}
