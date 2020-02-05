using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Globalization;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.EntityGeometry;

namespace GeometryViewer.SpatialOrganization
{

    #region HELPER CLASS: SurfaceBasicInfo
    public class SurfaceBasicInfo
    {
        public static IFormatProvider FROMATTER = new NumberFormatInfo();
        // display
        public LineGeometry3D DisplayLines { get; set; }
        public HelixToolkit.SharpDX.Wpf.MeshGeometry3D DisplayMesh { get; set; }

        // geometry definition
        protected List<Vector3> vertices_in_order;
        public ReadOnlyCollection<Vector3> VerticesInOrder { get { return this.vertices_in_order.AsReadOnly(); } }
        public Vector3 SurfaceNormal { get; private set; } // derived

        // position within the volume (ID)
        public long VolumeID { get; protected set; }
        public bool IsWall { get; protected set; }
        public int LevelOrLabel { get; protected set; }
        public int LabelForOpenings { get; protected set; }
        public long Wall_LowerPoly { get; protected set; }
        public long Wall_UpperPoly { get; protected set; }

        // size
        public double Area_AXES { get; protected set; }
        public double Height_AXES { get; protected set; }
        public double Width_AXES { get; protected set; }
        // material
        public long Material_ID { get; protected set; }
        // openings
        public List<SurfaceBasicInfo> Openings { get; set; }
        
        // processing
        public bool Processed { get; set; }

        public SurfaceBasicInfo(LineGeometry3D _display_lines, HelixToolkit.SharpDX.Wpf.MeshGeometry3D _display_mesh, List<Vector3> _vertices_in_order, 
                                long _volume_id, bool _is_wall, int _level_or_label, int _label_for_opeings, long _wall_lower_poly, long _wall_upper_poly,
                                double _area, double _height, double _width, long _material_id)
        {
            this.DisplayLines = _display_lines;
            this.DisplayMesh = _display_mesh;
            this.vertices_in_order = _vertices_in_order;
            this.SurfaceNormal = Utils.MeshesCustom.GetPolygonNormalNewell(this.vertices_in_order);

            this.VolumeID = _volume_id;
            this.IsWall = _is_wall;
            this.LevelOrLabel = _level_or_label;
            this.LabelForOpenings = _label_for_opeings;
            this.Wall_LowerPoly = _wall_lower_poly;
            this.Wall_UpperPoly = _wall_upper_poly;

            this.Area_AXES = _area;
            this.Height_AXES = _height;
            this.Width_AXES = _width;

            this.Material_ID = _material_id;

            this.Openings = new List<SurfaceBasicInfo>();

            this.Processed = false;
        }

        public override int GetHashCode()
        {
            int p = 10007; // prime
            //Random rnd = new Random();
            //int a = rnd.Next();
            int a = 1993;

            int hash = this.VolumeID.GetHashCode() * (int)(Math.Pow(a, 1)) +
                       this.IsWall.GetHashCode() * (int)(Math.Pow(a, 2)) +
                       this.LevelOrLabel.GetHashCode() * (int)(Math.Pow(a, 3)) +
                       this.LabelForOpenings.GetHashCode() * (int)(Math.Pow(a, 4)) +
                       this.Wall_LowerPoly.GetHashCode() * (int)(Math.Pow(a, 5)) +
                       this.Wall_UpperPoly.GetHashCode() * (int)(Math.Pow(a, 6));
            hash %= p;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SurfaceBasicInfo))
                return base.Equals(obj);
            else
            {
                SurfaceBasicInfo obj_as_sbi = obj as SurfaceBasicInfo;
                return (this.VolumeID == obj_as_sbi.VolumeID && this.IsWall == obj_as_sbi.IsWall &&
                        this.LevelOrLabel == obj_as_sbi.LevelOrLabel && this.LabelForOpenings == obj_as_sbi.LabelForOpenings &&
                        this.Wall_LowerPoly == obj_as_sbi.Wall_LowerPoly && this.Wall_UpperPoly == obj_as_sbi.Wall_UpperPoly);
            }
        }

        public static bool operator==(SurfaceBasicInfo _s1, SurfaceBasicInfo _s2)
        {
            if (Object.ReferenceEquals(_s1, null) || Object.ReferenceEquals(_s2, null))
                return false;

            return (_s1.VolumeID == _s2.VolumeID && _s1.IsWall == _s2.IsWall &&
                    _s1.LevelOrLabel == _s2.LevelOrLabel && _s1.LabelForOpenings == _s2.LabelForOpenings &&
                    _s1.Wall_LowerPoly == _s2.Wall_LowerPoly && _s1.Wall_UpperPoly == _s2.Wall_UpperPoly);
        }

        public static bool operator!=(SurfaceBasicInfo _s1, SurfaceBasicInfo _s2)
        {
            return !(_s1 == _s2);
        }

        public override string ToString()
        {
            string wall = (this.IsWall) ? "wall" : "slab";
            return "[ V" + this.VolumeID + " " + wall + " L" + this.LevelOrLabel + " oL" + this.LabelForOpenings + " wL" + this.Wall_LowerPoly + " wU" + this.Wall_UpperPoly + " ]: " + 
                this.Area_AXES.ToString("F2", SurfaceBasicInfo.FROMATTER) + " m²";
        }

    }
    #endregion
    
    #region HELPER CLASSES Surface Matching

    public class SurfaceMatch
    {
        #region STATIC

        public static int MIN_NR_MATCHING_VERTICES = 3;

        public static bool operator==(SurfaceMatch _sm1, SurfaceMatch _sm2)
        {
            if (Object.ReferenceEquals(_sm1, null) || Object.ReferenceEquals(_sm2, null)) return false;
            if (_sm1.Match01 == null || _sm1.Match02 == null || _sm2.Match01 == null || _sm2.Match02 == null) return false;

            return ((_sm1.Match01 == _sm2.Match01 && _sm1.Match02 == _sm2.Match02) ||
                    (_sm1.Match01 == _sm2.Match02 && _sm1.Match02 == _sm2.Match01));
        }

        public static bool operator!=(SurfaceMatch _sm1, SurfaceMatch _sm2)
        {
            return !(_sm1 == _sm2);
        }

        #endregion
        public SurfaceBasicInfo Match01 { get; private set; }
        public SurfaceBasicInfo Match02 { get; private set; }

        #region PROPERTIES: alignment, containment, match

        private bool is_alignment;
        public bool IsAlignment 
        {
            get { return this.is_alignment; }
            private set
            {
                this.is_alignment = value;
                if (!(this.is_alignment))
                {
                    this.is_containment = false;
                    this.is_match = false;
                }
            }
        }

        private bool is_containment;
        public bool IsContainment 
        {
            get { return this.is_containment; }
            private set
            {
                this.is_containment = value;
                if (this.is_containment)
                    this.is_alignment = true;
                else
                    this.is_match = false;
            }
        }

        private bool is_match;
        public bool IsMatch 
        {
            get { return this.is_match; }
            private set
            {
                this.is_match = value;
                if (this.is_match)
                {
                    this.is_alignment = true;
                    this.is_containment = true;
                }
            }
        }
        #endregion

        private float tolerance;

        public SurfaceMatch(SurfaceBasicInfo _sbi_1, SurfaceBasicInfo _sbi_2, float _matching_tolerance = 0.001f)
        {
            this.Match01 = _sbi_1;
            this.Match02 = _sbi_2;
            this.tolerance = _matching_tolerance;
            this.IsAlignment = false;
        }

        #region METHODS: test ALIGNMENT, CONTAINMENT, MATCH
        public void PerformAlignmentTest()
        {
            if (this.IsNotTestable()) return;

            // preliminary check
            float cosN = Vector3.Dot(this.Match02.SurfaceNormal, this.Match01.SurfaceNormal);
            bool aligned_check = (Math.Abs(cosN) >= 1 - this.tolerance);
            if (!aligned_check)
            {
                this.IsAlignment = false;
                return;
            }

            int nrV02 = this.Match02.VerticesInOrder.Count;
            Vector3 pivot01 = Utils.MeshesCustom.GetPolygonPivot(this.Match01.VerticesInOrder.ToList());
            int count_aligned_vertices = 0;
            for(int c = 0; c < nrV02; c++)
            {
                Vector3 test = this.Match02.VerticesInOrder[c] - pivot01;
                if (test.Length() < this.tolerance)
                {
                    this.IsAlignment = false;
                    continue;
                }                    

                test.Normalize();                
                float cos = Vector3.Dot(test, this.Match01.SurfaceNormal);
                if (Math.Abs(cos) >= this.tolerance)
                {
                    this.IsAlignment = false;
                    return;
                }
                count_aligned_vertices++;
            }

            if (count_aligned_vertices > 2)
                this.IsAlignment = true;
            else
                this.IsAlignment = false;
        }

        public void PerformContainmentTest(bool _full)
        {
            if (this.IsNotTestable()) return;

            bool first_contains_second = Utils.MeshesCustom.PolygonIsContainedInPolygon(this.Match01.VerticesInOrder.ToList(), this.Match02.VerticesInOrder.ToList(), false);
            if (first_contains_second)
            {
                this.IsContainment = true;
                return;
            }
            bool second_contains_first = Utils.MeshesCustom.PolygonIsContainedInPolygon(this.Match02.VerticesInOrder.ToList(), this.Match01.VerticesInOrder.ToList(), false);
            if (second_contains_first)
                this.IsContainment = true;
            else
                this.IsContainment = false;
        }

        public void PerformPartialContainmentTest(double _tolerance)
        {
            if (this.IsNotTestable()) return;
            // debug
            if (this.Match01.VolumeID == 25 && !this.Match01.IsWall && this.Match01.LevelOrLabel == 0 &&
                this.Match02.VolumeID == 39 && !this.Match02.IsWall && this.Match02.LevelOrLabel == 4)
            {

            }
            if (this.Match01.VolumeID == 25 && this.Match01.IsWall && this.Match01.LevelOrLabel == 0 &&
                this.Match02.VolumeID == 39 && this.Match02.IsWall && this.Match02.LevelOrLabel == 13 && 
                this.Match02.Wall_LowerPoly == 19 && this.Match02.Wall_UpperPoly == 21)
            {

            }
            if (this.Match01.VolumeID == 25 && this.Match01.IsWall && this.Match01.LevelOrLabel == 1 &&
                this.Match02.VolumeID == 39 && this.Match02.IsWall && this.Match02.LevelOrLabel == 12 &&
                this.Match02.Wall_LowerPoly == 19 && this.Match02.Wall_UpperPoly == 21)
            {

            }
            if (this.Match01.VolumeID == 25 && this.Match01.IsWall && this.Match01.LevelOrLabel == 0 &&
                this.Match02.VolumeID == 39 && this.Match02.IsWall && this.Match02.LevelOrLabel == 23 &&
                this.Match02.Wall_LowerPoly == 20 && this.Match02.Wall_UpperPoly == 22)
            {

            }
            if (this.Match01.VolumeID == 28 && this.Match01.IsWall && this.Match01.LevelOrLabel == 4 &&
                this.Match02.VolumeID == 39 && this.Match02.IsWall && this.Match02.LevelOrLabel == 11 &&
                this.Match02.Wall_LowerPoly == 19 && this.Match02.Wall_UpperPoly == 21)
            {

            }
            if (this.Match01.VolumeID == 105 && this.Match01.IsWall && this.Match01.LevelOrLabel == 2 &&
                this.Match02.VolumeID == 97 && this.Match02.IsWall && this.Match02.LevelOrLabel == 0)
            {

            }

            //bool intersection = Utils.MeshesCustom.PolygonIntersectsPolygon(this.Match01.VerticesInOrder.ToList(), this.Match02.VerticesInOrder.ToList());
            bool intersection = Utils.MeshesCustom.PolygonIntersectsPolygon(this.Match01.VerticesInOrder.ToList(), this.Match02.VerticesInOrder.ToList(), _tolerance);
            if (intersection)
                this.IsContainment = true;
            else
                this.IsContainment = false;
        }

        /// <summary>
        /// <para>Performs polygon heighborhood test by matching 3 vertices in general position.</para>
        /// </summary>
        public void PerformMatchingTest()
        {
            if (this.IsNotTestable()) return;

            int nrV01 = this.Match01.VerticesInOrder.Count;
            int nrV02 = this.Match02.VerticesInOrder.Count;
            
            // we are looking for 3 points in a general position (not lying on the same point or line)
            // that match, as they define a plane
            Utils.Vector3Comparer vcomp = new Utils.Vector3Comparer(this.tolerance);
            List<Vector3> matching_vertices = new List<Vector3>(); // saves matching vertices from 1st surface

            for (int i01 = 0; i01 < nrV01; i01++)
            {
                for(int i02 = 0; i02 < nrV02; i02++)
                {
                    if (vcomp.Equals(this.Match01.VerticesInOrder[i01], this.Match02.VerticesInOrder[i02]))
                    {
                        matching_vertices.Add(this.Match01.VerticesInOrder[i01]);
                        if (matching_vertices.Count >= SurfaceMatch.MIN_NR_MATCHING_VERTICES)
                        {
                            bool can_stop = Utils.MeshesCustom.PointsContainValidTriangle(matching_vertices, this.tolerance);
                            if (can_stop)
                            {
                                this.IsMatch = true;
                                return;
                            }
                        }
                    }
                }
            }

            this.IsMatch = false;
                
        }
        #endregion

        #region METHODS: Overrides

        public override string ToString()
        {
            string representation = "";
            if (this.Match01 != null)
                representation += this.Match01.ToString();
            if (this.Match02 != null)
                representation += " | " + this.Match02.ToString();

            return representation;
        }

        #endregion

        #region METHODS: Utils
        private bool IsNotTestable()
        {
            bool is_testable = ((this.Match01 == null || this.Match02 == null) || this.Match01 == this.Match02 ||
                                (this.Match01.VerticesInOrder == null || this.Match02.VerticesInOrder == null) ||
                                (this.Match01.VerticesInOrder.Count < 3 || this.Match02.VerticesInOrder.Count < 3) ||
                                (this.Match01.SurfaceNormal == Vector3.Zero || this.Match02.SurfaceNormal == Vector3.Zero));
            if (!(is_testable))
                this.IsAlignment = false;

            return is_testable;
        }

        public override int GetHashCode()
        {
            int p = 10007;
            int a = 541;

            return ((this.Match01.GetHashCode() * a + this.Match02.GetHashCode() * a * a) % p);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SurfaceBasicInfo))
                return base.Equals(obj);
            else
            {
                SurfaceMatch obj_as_sm = obj as SurfaceMatch;

                if (this.Match01 == null || this.Match02 == null || obj_as_sm.Match01 == null || obj_as_sm.Match02 == null) return false;

                return ((this.Match01 == obj_as_sm.Match01 && this.Match02 == obj_as_sm.Match02) ||
                        (this.Match01 == obj_as_sm.Match02 && this.Match02 == obj_as_sm.Match01));
            }
        }

        #endregion
    }

    #endregion

    public class NeighborhoodGraph : GroupModel3D
    {
        #region PRIVATE HELPER CLASS: Navigation
        private class NeighbNode
        {
            public ZonedVolume Volume { get; private set; }
            public int Degree { get; private set; }
            public List<NeighbEdge> Edges { get; private set; }
            public bool IsValid { get; private set; }
            public bool IsBeingProcessed { get; set; }

            public NeighbNode(ZonedVolume _volume)
            {
                this.Volume = _volume;
                this.Edges = new List<NeighbEdge>();
                this.IsValid = (this.Volume != null);
                if (this.IsValid)
                {
                    this.Degree = this.Volume.GetNrOfEnclosingSurfaces();
                }
                this.IsBeingProcessed = true;
            }

            public void AddEdge(NeighbEdge _e)
            {
                if (_e == null) return;
                if (this.Edges.Contains(_e)) return;
                if (!(this.IsBeingProcessed)) return;

                this.Edges.Add(_e);
                if (this.Edges.Count == this.Degree)
                    this.IsBeingProcessed = false;
            }

            public override string ToString()
            {
                string representation = "{n ";
                if (this.Volume != null)
                    representation += "V " + this.Volume.ID;
                representation += "}";
                return representation;
            }
        }

        private class NeighbEdge
        {
            public SurfaceMatch Match { get; private set; }
            public NeighbNode Node1 { get; private set; }
            public NeighbNode Node2 { get; private set; }

            public bool IsValid { get; private set; }

            public NeighbEdge(NeighbNode _n1, NeighbNode _n2, SurfaceMatch _match)
            {
                this.Node1 = _n1;
                this.Node2 = _n2;
                this.Match = _match;
                this.SetValidity();
                // set navigation
                if (this.IsValid)
                {
                    _n1.AddEdge(this);
                    _n2.AddEdge(this);
                }
            }

            private void SetValidity()
            {
                bool nodes_valid = (this.Node1 != null && this.Node2 != null && this.Node1.Volume != null && this.Node2.Volume != null);
                if (!nodes_valid)
                {
                    this.IsValid = false;
                    return;
                }
                bool match_valid = (this.Match != null && this.Match.Match01 != null && this.Match.Match02 != null);
                if (!match_valid)
                {
                    this.IsValid = false;
                    return;
                }
                    
                bool match_matches_nodes = ((this.Match.Match01.VolumeID == this.Node1.Volume.ID && this.Match.Match02.VolumeID == this.Node2.Volume.ID) ||
                                            (this.Match.Match02.VolumeID == this.Node1.Volume.ID && this.Match.Match01.VolumeID == this.Node2.Volume.ID));
                this.IsValid = nodes_valid && match_valid && match_matches_nodes;
            }

            public override string ToString()
            {
                string representation = "{e ";
                if (this.Match != null)
                    representation += "M [" + this.Match.ToString() + "]";
                representation += "}";
                return representation;
            }
        }

        #endregion

        #region PROPERTIES: data

        public EntityGeometry.EntityManager VolumeManager
        {
            get { return (EntityGeometry.EntityManager)GetValue(VolumeManagerProperty); }
            set { SetValue(VolumeManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VolumeManager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VolumeManagerProperty =
            DependencyProperty.Register("VolumeManager", typeof(EntityGeometry.EntityManager), typeof(NeighborhoodGraph),
            new UIPropertyMetadata(null, new PropertyChangedCallback(VolumeManagerPropertyChangedCallback)));

        private static void VolumeManagerPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NeighborhoodGraph instance = d as NeighborhoodGraph;
            if (instance == null || instance.VolumeManager == null)
            {
                instance.volumes_tested = new Dictionary<ZonedVolume, bool>();
                return;
            }
            
            // exract volumes to sort
            instance.ExtractVolumesToSort();
        }


        public bool VolumeManagerContentChange
        {
            get { return (bool)GetValue(VolumeManagerContentChangeProperty); }
            set { SetValue(VolumeManagerContentChangeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VolumeManagerContentChange.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VolumeManagerContentChangeProperty =
            DependencyProperty.Register("VolumeManagerContentChange", typeof(bool), typeof(NeighborhoodGraph),
            new UIPropertyMetadata(false, new PropertyChangedCallback(VolumeManagerContentChangePropertyChangedCallback)));

        private static void VolumeManagerContentChangePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NeighborhoodGraph instance = d as NeighborhoodGraph;
            if (instance == null) return;

            // exract volumes to sort
            instance.ExtractVolumesToSort();
        }


        public bool IncludeVolumesOnOFFLayers
        {
            get { return (bool)GetValue(IncludeVolumesOnOFFLayersProperty); }
            set { SetValue(IncludeVolumesOnOFFLayersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IncludeVolumesOnOFFLayers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IncludeVolumesOnOFFLayersProperty =
            DependencyProperty.Register("IncludeVolumesOnOFFLayers", typeof(bool), typeof(NeighborhoodGraph),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IncludeVolumesOnOFFLayersPropertyChangedCallback)));

        private static void IncludeVolumesOnOFFLayersPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NeighborhoodGraph instance = d as NeighborhoodGraph;
            if (instance == null) return;

            instance.ExtractVolumesToSort();
        }


        public bool NeighborsUpToDate
        {
            get { return (bool)GetValue(NeighborsUpToDateProperty); }
            set { SetValue(NeighborsUpToDateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NeighborsUpToDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NeighborsUpToDateProperty =
            DependencyProperty.Register("NeighborsUpToDate", typeof(bool), typeof(NeighborhoodGraph), 
            new UIPropertyMetadata(false));


        #endregion

        #region PROPERTIES: display

        public ICommand PerformNeighborhoodTestCmd { get; private set; }

        public bool ShowNeighbors
        {
            get { return (bool)GetValue(ShowNeighborsProperty); }
            set { SetValue(ShowNeighborsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowNeighbors.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowNeighborsProperty =
            DependencyProperty.Register("ShowNeighbors", typeof(bool), typeof(NeighborhoodGraph),
            new UIPropertyMetadata(true, new PropertyChangedCallback(ShowNeighborsPropertyChangedCallback)));

        private static void ShowNeighborsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NeighborhoodGraph instance = d as NeighborhoodGraph;
            if (instance == null) return;

            if (instance.nodes_vis == null || instance.edge_vis == null) return;
            if (instance.ShowNeighbors)
            {
                instance.nodes_vis.Visibility = Visibility.Visible;
                instance.edge_vis.Visibility = Visibility.Visible;
            }
            else
            {
                instance.nodes_vis.Visibility = Visibility.Collapsed;
                instance.edge_vis.Visibility = Visibility.Collapsed;
            }
        }

        public double MaxOverlapError
        {
            get { return (double)GetValue(MaxOverlapErrorProperty); }
            set { SetValue(MaxOverlapErrorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxOverlapError.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxOverlapErrorProperty =
            DependencyProperty.Register("MaxOverlapError", typeof(double), typeof(NeighborhoodGraph), 
            new UIPropertyMetadata(0.25));



        #endregion

        #region CLASS MEMBERS

        private Dictionary<ZonedVolume, bool> volumes_tested;

        private List<NeighbNode> nodes;
        private List<NeighbEdge> edges;

        private MeshGeometryModel3D nodes_vis;
        private LineGeometryModel3D edge_vis;

        #endregion

        public NeighborhoodGraph()
        {
            this.PerformNeighborhoodTestCmd = new RelayCommand(x => { this.FindNeighbors(); this.BuildGraphVisualization(); }, 
                                                               x => this.volumes_tested != null && this.volumes_tested.Count > 1);
        }

        #region METHODS: main test

        private void FindNeighbors()
        {
            if (this.volumes_tested.Count < 2) return;

            // ---------------------------------------- ALIGNMENT ----------------------------------------------

            // extract the surfaces of the 'first' volume and set them as cluster initiators
            ZonedVolume v1 = this.volumes_tested.ElementAt(0).Key;
            this.volumes_tested[v1] = true;

            List<SurfaceBasicInfo> v1_surf = v1.ExportBasicInfoForNeighborhoodTest();
            Dictionary<SurfaceBasicInfo, int> surf_clusters = NeighborhoodGraph.ClusterAccToAlignment(v1_surf);

            for(int i = 1; i < this.volumes_tested.Count; i++)
            {
                // get the next volume
                ZonedVolume v_next = this.volumes_tested.ElementAt(i).Key;
                this.volumes_tested[v_next] = true;

                // extract its enclosing surfaces
                List<SurfaceBasicInfo> v_next_surf = v_next.ExportBasicInfoForNeighborhoodTest();
                // try to place them in a cluster
                NeighborhoodGraph.ClusterAccToAlignment(ref surf_clusters, v_next_surf);                
            }

            // assemble the clusters
            List<List<SurfaceBasicInfo>> clusters_alignment = NeighborhoodGraph.AssembleClusters(surf_clusters);

            // debug
            string debug = string.Empty;
            foreach (List<SurfaceBasicInfo> cluster in clusters_alignment)
            {
                if (cluster.Count < 2) continue;
                foreach(SurfaceBasicInfo sbi in cluster)
                {
                    debug += sbi.ToString() + "\n";
                }
                debug += "\n";
            }
            string test = debug;

            // --------------------------------------- OVERLAPPING ---------------------------------------------

            // start overlap testing in each cluster
            this.ClearGraph();
            List<SurfaceMatch> overlaps_all = new List<SurfaceMatch>();
            foreach (List<SurfaceBasicInfo> cluster in clusters_alignment)
            {
                if (cluster.Count < 2) continue;

                List<SurfaceMatch> overlaps = NeighborhoodGraph.FindOverlaps(cluster, this.MaxOverlapError);
                overlaps_all.AddRange(overlaps);               
            }
            this.AddToGraph(overlaps_all);
        }

        #endregion

        #region METHODS: Clustering according to Alignment
        private static Dictionary<SurfaceBasicInfo, int> ClusterAccToAlignment(List<SurfaceBasicInfo> _surfaces)
        {
            Dictionary<SurfaceBasicInfo, int> processed = new Dictionary<SurfaceBasicInfo, int>();
            if (_surfaces == null || _surfaces.Count == 0) return processed;
           
            foreach(SurfaceBasicInfo s in _surfaces)
            {
                processed.Add(s, -1);
            }

            SurfaceBasicInfo s1 = processed.ElementAt(0).Key;
            processed[s1] = 0;
            int cluster_counter = 1;

            NeighborhoodGraph.ClusterAccToAlignmentAlgorithm(ref processed, ref cluster_counter);
            return processed;
        }

        private static void ClusterAccToAlignment(ref Dictionary<SurfaceBasicInfo, int> _processed, List<SurfaceBasicInfo> _surfaces)
        {
            if (_surfaces == null || _surfaces.Count == 0) return;
            if (_processed == null || _processed.Count == 0)
            {
                _processed = NeighborhoodGraph.ClusterAccToAlignment(_surfaces);
                return;
            }

            // initiate
            int cluster_counter = _processed.Select(x => x.Value).Max() + 1;            
            foreach(SurfaceBasicInfo s in _surfaces)
            {
                _processed.Add(s, -1);
            }

            NeighborhoodGraph.ClusterAccToAlignmentAlgorithm(ref _processed, ref cluster_counter);
        }

        private static void ClusterAccToAlignmentAlgorithm(ref Dictionary<SurfaceBasicInfo, int> _processed, ref int _cluster_counter)
        {
            for (int i = 0; i < _processed.Count; i++)
            {
                SurfaceBasicInfo s = _processed.ElementAt(i).Key;
                int s_cluster = _processed.ElementAt(i).Value;
                if (s_cluster > -1) continue;

                // debug
                if (s.VolumeID == 39 && s.IsWall && s.LevelOrLabel == 2 && s.Wall_LowerPoly == 16 && s.Wall_UpperPoly == 17)
                {

                }

                // try to add to cluster
                List<int> tested_clusters = new List<int>();
                for (int c = 0; c < _processed.Count; c++)
                {
                    SurfaceBasicInfo clusterCenter = _processed.ElementAt(c).Key;
                    int clusterIndex = _processed.ElementAt(c).Value;

                    // debug
                    if (clusterCenter.VolumeID == 25 && clusterCenter.IsWall && clusterCenter.LevelOrLabel == 0)
                    {

                    }

                    if (tested_clusters.Contains(clusterIndex)) continue;
                    tested_clusters.Add(clusterIndex);

                    SurfaceMatch match_test = new SurfaceMatch(s, clusterCenter, 0.1f); // tolerance for the cos
                    match_test.PerformAlignmentTest();
                    if (match_test.IsAlignment)
                    {
                        _processed[s] = clusterIndex;
                        break;
                    }
                }

                // if unsuccessful, initiate another cluster
                if (_processed[s] < 0)
                {
                    _processed[s] = _cluster_counter;
                    _cluster_counter++;
                }
            }
        }

        private static List<List<SurfaceBasicInfo>> AssembleClusters(Dictionary<SurfaceBasicInfo, int> _input)
        {
            int cluster_counter = _input.Select(x => x.Value).Max() + 1;
            List<List<SurfaceBasicInfo>> clusters = new List<List<SurfaceBasicInfo>>();
            for (int c = 0; c < cluster_counter; c++)
            {
                List<SurfaceBasicInfo> found = _input.Where(x => x.Value == c).Select(x => x.Key).ToList();
                clusters.Add(found);
            }

            return clusters;
        }
        #endregion

        #region METHODS: Overlap testing

        private static List<SurfaceMatch> FindOverlaps(List<SurfaceBasicInfo> _aligned_surfaces, double _tolerance)
        {
            List<SurfaceMatch> matches = new List<SurfaceMatch>();

            if (_aligned_surfaces == null) return matches;
            if (_aligned_surfaces.Count < 2) return matches;

            Dictionary<SurfaceBasicInfo, bool> processed = new Dictionary<SurfaceBasicInfo, bool>();
            foreach(SurfaceBasicInfo s in _aligned_surfaces)
            {
                processed.Add(s, false);
            }

            int nrS = _aligned_surfaces.Count;
            for(int i = 0; i < nrS; i++)
            {
                if (processed[_aligned_surfaces[i]])
                    continue;

                for(int j = 0; j < nrS; j++)
                {
                    if (i == j) continue;
                    // allow testing against already processed surfaces
                    // in case of one surface overlapping multiple ones (bad building physics design!)

                    // do not test surfaces within the same volume -> by design they should not overlap
                    if (_aligned_surfaces[i].VolumeID == _aligned_surfaces[j].VolumeID) continue;

                    SurfaceMatch test_match = new SurfaceMatch(_aligned_surfaces[i], _aligned_surfaces[j]);
                    test_match.PerformPartialContainmentTest(_tolerance);
                    if (test_match.IsContainment)
                    {
                        matches.Add(test_match);
                        processed[_aligned_surfaces[i]] = true;
                        processed[_aligned_surfaces[j]] = true;
                    }
                }
            }

            return matches;
        }

        #endregion

        #region METHODS: Graph construction

        private void ClearGraph()
        {
            this.nodes = new List<NeighbNode>();
            this.edges = new List<NeighbEdge>();
        }

        private void AddToGraph(List<SurfaceMatch> _matches)
        {
            if (_matches == null || _matches.Count == 0) return;

            if (this.nodes == null)
                this.nodes = new List<NeighbNode>();
            if (this.edges == null)
                this.edges = new List<NeighbEdge>();

            foreach (SurfaceMatch match in _matches)
            {
                if (!match.IsContainment) continue;

                // look for the nodes
                NeighbNode n1 = this.nodes.FirstOrDefault(x => x.IsValid && x.Volume.ID == match.Match01.VolumeID);
                if (n1 == null)
                {
                    ZonedVolume v1 = this.volumes_tested.Where(x => x.Key.ID == match.Match01.VolumeID).Select(x => x.Key).FirstOrDefault();
                    if (v1 != null)
                    {
                        n1 = new NeighbNode(v1);
                        this.nodes.Add(n1);
                    }                       
                }

                NeighbNode n2 = this.nodes.FirstOrDefault(x => x.IsValid && x.Volume.ID == match.Match02.VolumeID);
                if (n2 == null)
                {
                    ZonedVolume v2 = this.volumes_tested.Where(x => x.Key.ID == match.Match02.VolumeID).Select(x => x.Key).FirstOrDefault();
                    if (v2 != null)
                    {
                        n2 = new NeighbNode(v2);
                        this.nodes.Add(n2);
                    }                       
                }

                // create the edge
                if (n1 != null && n2 != null)
                    this.edges.Add(new NeighbEdge(n1, n2, match));
            }
        }

        #endregion

        #region METHODS: Visualization

        private void BuildGraphVisualization()
        {
            if (this.nodes == null || this.edges == null) return;

            this.Children.Clear();

            // pepare
            PhongMaterial material_nodes = new PhongMaterial();
            material_nodes.DiffuseColor = new Color4(1f);
            material_nodes.AmbientColor = new Color4(0.8f, 0.8f, 0.8f, 1f);
            material_nodes.SpecularColor = new Color4(1f, 0.75f, 0f, 1f);
            material_nodes.SpecularShininess = 3;
            material_nodes.EmissiveColor = new Color4(0.5f, 0.5f, 0.5f, 1f);

            LineBuilder lb = new LineBuilder();
            MeshBuilder mb = new MeshBuilder();

            // define geometry           
            foreach(NeighbEdge e in this.edges)
            {
                if (e.IsValid)
                {
                    Vector3 connector1 = Utils.MeshesCustom.GetPolygonPivot(e.Match.Match01.VerticesInOrder.ToList());
                    Vector3 connector2 = Utils.MeshesCustom.GetPolygonPivot(e.Match.Match02.VerticesInOrder.ToList());
                    
                    lb.AddLine(e.Node1.Volume.Volume_Pivot, connector1);
                    lb.AddLine(connector1, connector2);
                    lb.AddLine(connector2, e.Node2.Volume.Volume_Pivot);

                    mb.AddSphere(connector1, 0.1f, 8, 8);
                    mb.AddSphere(connector2, 0.1f, 8, 8);
                }
            }
            foreach (NeighbNode n in this.nodes)
            {
                if (n.IsValid)
                {
                    mb.AddSphere(n.Volume.Volume_Pivot, 0.25f, 8, 8);
                }
            }

            // define visualization
            this.edge_vis = new LineGeometryModel3D()
            {
                Geometry = lb.ToLineGeometry3D(),
                Color = Color.White,
                Thickness = 1f,
                Visibility = System.Windows.Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
            };
            this.Children.Add(this.edge_vis);
            
            this.nodes_vis = new MeshGeometryModel3D()
            {
                Geometry = mb.ToMeshGeometry3D(),
                Material = material_nodes,
                Visibility = Visibility.Visible,
                IsHitTestVisible = false,
                Transform = new MatrixTransform3D(Matrix3D.Identity),
            };
            this.Children.Add(this.nodes_vis);

            // re-attach to renderer
            if (this.renderHost != null)
                this.Attach(this.renderHost);

            this.NeighborsUpToDate = true;
        }

        #endregion

        #region METHODS: Info

        public List<ZonedVolume> GetNeighborsOf(ZonedVolume _zv)
        {
            List<ZonedVolume> neighbors = new List<ZonedVolume>();

            if (this.nodes == null) return neighbors;
            if (this.nodes.Count < 1) return neighbors;
            if (_zv == null) return neighbors;

            NeighbNode n = this.nodes.FirstOrDefault(x => x.IsValid && x.Volume.ID == _zv.ID);
            if (n == null) return neighbors;
            if (n.Edges.Count == 0) return neighbors;

            foreach(NeighbEdge e in n.Edges)
            {
                if (!e.IsValid)
                    continue;

                if (e.Node1.Volume.ID == _zv.ID)
                    neighbors.Add(e.Node2.Volume);
                else if (e.Node2.Volume.ID == _zv.ID)
                    neighbors.Add(e.Node1.Volume);
            }

            return neighbors;
        }

        /// <summary>
        /// <para>Identification is specified unambiguously by volume id, label/level,</para>
        /// <para>index within the level / label and id of the lower defining polygon of a wall.</para>
        /// </summary>
        /// <param name="_volume_id"></param>
        /// <param name="_is_wall"></param>
        /// <param name="_level_or_label"></param>
        /// <param name="_label_for_openings"></param>
        /// <param name="_wall_lower_poly"></param>
        /// <returns></returns>
        public ZonedVolume GetOtherSideOf(long _volume_id, bool _is_wall, int _level_or_label, int _label_for_openings, long _wall_lower_poly)
        {
            // find the volume
            if (this.nodes == null) return null;
            if (this.nodes.Count < 1) return null;

            NeighbNode n = this.nodes.FirstOrDefault(x => x.IsValid && x.Volume.ID == _volume_id);
            if (n == null) return null;
            if (n.Edges.Count == 0) return null;

            foreach (NeighbEdge e in n.Edges)
            {
                if (!e.IsValid)
                    continue;

                if ((e.Match.Match01.VolumeID == _volume_id &&
                     e.Match.Match01.IsWall == _is_wall && 
                     e.Match.Match01.LevelOrLabel == _level_or_label && 
                     e.Match.Match01.LabelForOpenings == _label_for_openings &&
                     e.Match.Match01.Wall_LowerPoly == _wall_lower_poly) ||
                   (e.Match.Match02.VolumeID == _volume_id &&
                    e.Match.Match02.IsWall == _is_wall &&
                    e.Match.Match02.LevelOrLabel == _level_or_label &&
                    e.Match.Match02.LabelForOpenings == _label_for_openings &&
                    e.Match.Match02.Wall_LowerPoly == _wall_lower_poly))
                {
                    if (e.Node1.Volume.ID == _volume_id)
                        return e.Node2.Volume;
                    else if (e.Node2.Volume.ID == _volume_id)
                        return e.Node1.Volume;
                }                
            }
            return null;
        }

        #endregion

        #region UTILS

        private void ExtractVolumesToSort()
        {
            this.volumes_tested = new Dictionary<ZonedVolume, bool>();
            List<GeometricEntity> allGE = this.VolumeManager.GetFlatGeometryList();

            foreach (GeometricEntity ge in allGE)
            {
                if (!this.IncludeVolumesOnOFFLayers)
                {
                    if (ge.Visibility != EntityVisibility.ON)
                        continue;
                }                   

                if (ge is ZonedVolume)
                {
                    this.volumes_tested.Add(ge as ZonedVolume, false);
                }
            }
            this.NeighborsUpToDate = false;
        }

        #endregion
    }
}
