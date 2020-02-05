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

using System.ComponentModel;
using System.Runtime.CompilerServices;

using GeometryViewer.Utils;

namespace GeometryViewer.HelixToolkitCustomization
{
    class UserMesh : MeshGeometryModel3D, INotifyPropertyChanged
    {
        // GENERAL PROPERTIES
        private static int NR_UM = 0;
        public int UMID { get; private set; }

        private PhongMaterial initMaterial = PhongMaterials.White;

        public PhongMaterial PassiveMaterial { get; set; }
        public PhongMaterial SelectionMaterial { get; set; }

        public ActionManager UndoManager { get; private set; }

        // DEPENDENCY PROPPERTIES FOR BINDING
        public Point3D HitPoint
        {
            get { return (Point3D)GetValue(HitPointProperty); }
            set { SetValue(HitPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HitPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HitPointProperty =
            DependencyProperty.Register("HitPoint", typeof(Point3D), typeof(UserMesh),
            new UIPropertyMetadata(new Point3D(0f, 0f, 0f)));

        public Point3D HitPointPrev
        {
            get { return (Point3D)GetValue(HitPointPrevProperty); }
            set { SetValue(HitPointPrevProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HitPointPrev.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HitPointPrevProperty =
            DependencyProperty.Register("HitPointPrev", typeof(Point3D), typeof(UserMesh),
            new UIPropertyMetadata(new Point3D(0f, 0f, 0f)));


        public int NrConseqHits
        {
            get { return (int)GetValue(NrConseqHitsProperty); }
            set { SetValue(NrConseqHitsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrConseqHits.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrConseqHitsProperty =
            DependencyProperty.Register("NrConseqHits", typeof(int), typeof(UserMesh), new UIPropertyMetadata(0));



        public bool SnapToGrid
        {
            get { return (bool)GetValue(SnapToGridProperty); }
            set { SetValue(SnapToGridProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnapToGrid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnapToGridProperty =
            DependencyProperty.Register("SnapToGrid", typeof(bool), typeof(UserMesh),
            new UIPropertyMetadata(false));

        public float SnapMagnet
        {
            get { return (float)GetValue(SnapMagnetProperty); }
            set { SetValue(SnapMagnetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnapMagnet.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnapMagnetProperty =
            DependencyProperty.Register("SnapMagnet", typeof(float), typeof(UserMesh),
            new UIPropertyMetadata(0f));

        // PROPERTIES WITH AN EVENT LISTENER
        private string hitPointString = "-";
        public string HitPointString
        {
            get { return this.hitPointString; }
            private set
            {
                this.hitPointString = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("HitPointString"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // CONSTRUCTORS
        public UserMesh() : base()
        {
            UMID = ++NR_UM;
            this.PassiveMaterial = PhongMaterials.White;
            this.SelectionMaterial = PhongMaterials.Red;
            this.UndoManager = ActionManager.GetInstance();
        }

        // PRIVATE UTILITY METHODS
        private string point3ToString()
        {
            string x = this.HitPoint.X.ToString("F3");
            string y = this.HitPoint.Y.ToString("F3");
            string z = this.HitPoint.Z.ToString("F3");
            return "[" + x + ", " + y + ", " + z + "]";
        }

        // INHERITED METHODS
        public override bool HitTest(Ray rayWS, ref List<HitTestResult> hits)
        {
            if (initMaterial == PhongMaterials.White)
            {
                initMaterial = this.phongMaterial;
            }
            var result = base.HitTest(rayWS, ref hits);
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                this.phongMaterial = result ? this.SelectionMaterial : this.PassiveMaterial;

                if(result)
                {
                    // test property changed event of HitControl
                    if (NrConseqHits > 0)
                    {
                        this.HitPointPrev = this.HitPoint;
                    }
                    this.HitPoint = hits[0].PointHit;
                    AdjustForSnap();
                    NrConseqHits++;
                    this.HitPointString = point3ToString();
                }
                else
                {
                    NrConseqHits = 0;
                    this.HitPoint = new System.Windows.Media.Media3D.Point3D();
                    this.HitPointPrev = new System.Windows.Media.Media3D.Point3D();
                    this.HitPointString = "-";
                }
                    
            }
            // ActionManager.RecordModifyCallback(this, new List<DependencyProperty> { HitPointProperty, HitPointPrevProperty, NrConseqHitsProperty });
            return result;
        }

        private void AdjustForSnap()
        {
            if (this.SnapToGrid && this.SnapMagnet > 0.0001)
            {
                var x = this.HitPoint.X / this.SnapMagnet;
                var xi = Math.Round(x);
                var y = this.HitPoint.Y / this.SnapMagnet;
                var yi = Math.Round(y);
                var z = this.HitPoint.Z / this.SnapMagnet;
                var zi = Math.Round(z);
                this.HitPoint = new Point3D(xi * this.SnapMagnet, yi * this.SnapMagnet, zi * this.SnapMagnet);
            }
        }

    }

}
