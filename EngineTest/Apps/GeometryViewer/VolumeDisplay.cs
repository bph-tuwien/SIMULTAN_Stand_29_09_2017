using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.Utils;

namespace GeometryViewer
{
    public class VolumeDisplay : GroupModel3D
    {

        private MeshGeometryModel3D volume;
        private LineGeometryModel3D volumeEdges;
        private LineGeometryModel3D volumeNormals;
        private LineGeometryModel3D polygons;

        private List<List<Point3D>> coords_polygons;
        private List<bool> reverse_polygons;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== INITIALIZATION ========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INITIALIZATION
        static VolumeDisplay()
        {
            //Diffuse_Map = new BitmapImage(new Uri(@"./Data/Maps/unsinn.jpg", UriKind.RelativeOrAbsolute));
            //Diffuse_Map.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            //Diffuse_Map.Freeze();

            string test = @"C:\_TU\Code-Test\c#\EngineTest\Apps\GeometryViewer\bin\Debug\Data\Maps\opacity_map_2.jpg";
            Diffuse_Map = BitmapFrame.Create(new Uri(test, UriKind.RelativeOrAbsolute));

            VolumeMat = new PhongMaterial();
            VolumeMat.DiffuseColor = new Color4(1f, 1f, 1f, 1f);
            VolumeMat.DiffuseMap = Diffuse_Map;          
            VolumeMat.AmbientColor = new Color4(0.8f, 0.8f, 0.8f, 1f);
            VolumeMat.EmissiveColor = new Color4(0.25f, 0.25f, 0.25f, 0f);
            VolumeMat.SpecularColor = new Color4(1f, 1f, 1f, 1f);
            VolumeMat.SpecularShininess = 3;

            AlertMat = new PhongMaterial();
            AlertMat.DiffuseColor = new Color4(0.8f, 0f, 0f, 0.25f);
            AlertMat.AmbientColor = new Color4(0.6f, 0f, 0f, 1f);
            AlertMat.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            AlertMat.SpecularShininess = 1;
        }

        public VolumeDisplay()
        {
            coords_polygons = new List<List<Point3D>>();

            // fill in polygon data with test values

            // CASE 1
            //coords_polygons.Add(VolumeDisplay.TestPoly1);

            //Matrix3D M2 = Matrix3D.Identity;
            //M2.Scale(new Vector3D(0.5, 1, 0.5));
            //Point3D[] testPoly2_ar = VolumeDisplay.TestPoly2.ToArray();
            //M2.Transform(testPoly2_ar);
            //coords_polygons.Add(new List<Point3D>(testPoly2_ar));

            //reverse_polygons = new List<bool> { false, false };

            // CASE 2
            coords_polygons.Add(VolumeDisplay.TestPolyA1);
            coords_polygons.Add(VolumeDisplay.TestPolyA2);
            coords_polygons.Add(VolumeDisplay.TestPolyA3);
            coords_polygons.Add(VolumeDisplay.TestPolyA4);

            reverse_polygons = new List<bool> { true, true, true, true };

            // Geometry Model Definitions
            this.polygons = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = Color.LightBlue,
                Thickness = 1,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(polygons);

            this.volume = new MeshGeometryModel3D()
            {
                Geometry = null,
                Material = VolumeDisplay.VolumeMat,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(volume);

            this.volumeEdges = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = Color.White,
                Thickness = 0.5,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(volumeEdges);

            this.volumeNormals = new LineGeometryModel3D()
            {
                Geometry = null,
                Color = Color.DarkBlue,
                Thickness = 0.5,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
                Instances = new List<SharpDX.Matrix>(),
            };
            this.Children.Add(volumeNormals);      

            UpdateGeometry();

        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =========================================== GEOMETRY DEFINITIONS ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region GEOMETRY DEFINITIONS

        private void UpdateGeometry()
        {
            // show the defining polygons
            LineBuilder b = new LineBuilder();
            int n = coords_polygons.Count;
            if (n > 0)
            {               
                for (int i = 0; i < n; i++)
                {
                    // transfer current polygon
                    List<Point3D> cp = coords_polygons[i];
                    int m = cp.Count;
                    if (m > 0)
                    {
                        // lines
                        for(int j = 0; j < m; j++)
                        {
                            b.AddLine(cp[j % m].ToVector3(), cp[(j + 1) % m].ToVector3());
                        }
                        // start maker
                        b.AddBox(cp[0].ToVector3(), START_MARKER, 0, START_MARKER);
                    }
                }
            }
            this.polygons.Geometry = b.ToLineGeometry3D();

            // show the resulting volume
            Vector3[] ar1 = CommonExtensions.ConvertPoint3DArToVector3Ar(this.coords_polygons[0].ToArray());
            Vector3[] ar2 = CommonExtensions.ConvertPoint3DArToVector3Ar(this.coords_polygons[1].ToArray());
            List<List<Vector3>> coords_poly_asV3 = CommonExtensions.ConvertPoints3DListListToVector3ListList(this.coords_polygons);
            bool capBottom = false;
            bool capTop = true;
            
            HelixToolkit.SharpDX.Wpf.MeshGeometry3D volMesh =
                MeshesCustom.MeshFromNPolygons(coords_poly_asV3, this.reverse_polygons, capBottom, capTop);

            this.volume.Geometry = volMesh;
            this.volumeNormals.Geometry = MeshesCustom.GetVertexNormalsAsLines(volMesh, 0.25f);
            this.volumeEdges.Geometry = MeshesCustom.GetEdgesAsLines(volMesh);
        }


        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================ STATIC DATA =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly double START_MARKER = 0.1;

        private static PhongMaterial VolumeMat;
        private static PhongMaterial AlertMat;
        private static BitmapSource Diffuse_Map;

        #region STATIC DATA FOR TESTING

        private static List<Point3D> TestPoly1 = new List<Point3D>()
        {
            new Point3D(-2, 0, -2.5),
            new Point3D( 3, 0, -2.5),
            new Point3D( 3, 0, 1.5),
            new Point3D( 2, 0, 2.5),
        };

        private static List<Point3D> TestPoly2 = new List<Point3D>()
        {
            new Point3D(-2, 2, -2.5),
            new Point3D( 3, 2, -2.5),
            new Point3D( 3, 2, 1.5),
            new Point3D( 2, 2, 2.5),
            new Point3D( -2.5, 2, 0),
            new Point3D( -3, 2, -1),
        };


        private static List<Point3D> TestPolyA1 = new List<Point3D>()
        {
            new Point3D(-2, 0,  2),
            new Point3D( 1, 0,  2),
            new Point3D( 1, 0,  3),
            new Point3D( 3, 0,  3),
            new Point3D( 5, 0, -2),
            new Point3D(-2, 0, -2),
            new Point3D(-2, 0,  1),
        };

        private static List<Point3D> TestPolyA2 = new List<Point3D>()
        {
            new Point3D(-2, 3,  2),
            new Point3D( 1, 3,  2),
            new Point3D( 1, 3,  3),
            new Point3D( 3, 3,  3),
            new Point3D( 5, 3, -2),
            new Point3D(-2, 3, -2),
            new Point3D(-2, 3,  1),
        };

        private static List<Point3D> TestPolyA3 = new List<Point3D>()
        {
            new Point3D(-3, 3,  3),
            new Point3D( 1, 3,  3),
            new Point3D( 1, 3,  3),
            new Point3D( 3, 3,  3),
            new Point3D( 5, 3, -2),
            new Point3D(-3, 3, -2),
            new Point3D(-3, 3,  1),
        };

        private static List<Point3D> TestPolyA4 = new List<Point3D>()
        {
            new Point3D(-3, 6,  3),
            new Point3D( 1, 6,  3),
            new Point3D( 1, 6,  3),
            new Point3D( 4, 6,  3),
            new Point3D( 6, 6, -2),
            new Point3D(-5, 6, -2),
            new Point3D(-3, 6,  1),
        };

        #endregion
    }
}
