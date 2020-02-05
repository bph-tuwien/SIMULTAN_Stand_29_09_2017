using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace InterProcCommunication.Specific
{

    // =============================== NOTE: BAD DESIGN ================================= //
    // this class has to correspond to ParameterStructure.Geometry.GeometricRelationship  //
    // ================================================================================== //
    public class GeometricRelationship
    {
        #region STATIC COMPARER

        /// <summary>
        /// <para>This method compares two geometric relationships according to content, NOT id or name.</para>
        /// <para>The transformation matrices are not being compared for now...</para>
        /// </summary>
        /// <param name="_gr1"></param>
        /// <param name="_gr2"></param>
        /// <returns></returns>
        public static bool HaveSameContent(GeometricRelationship _gr1, GeometricRelationship _gr2)
        {
            if (_gr1 == null && _gr2 == null) return true;
            if (_gr1 == null && _gr2 != null) return false;
            if (_gr1 != null && _gr2 == null) return false;

            if (_gr1.gr_state.Type != _gr2.gr_state.Type) return false;
            if (_gr1.gr_state.IsRealized != _gr2.gr_state.IsRealized) return false;

            if (_gr1.gr_ids.X != _gr2.gr_ids.X || _gr1.gr_ids.Y != _gr2.gr_ids.Y || _gr1.gr_ids.Z != _gr2.gr_ids.Z) return false;
            
            // if (_gr1.gr_ucs != _gr2.gr_ucs) return false; // checks for EXACT equality -> NaN and rounding errors can lead to false negatives
            if (!GeometricTransformations.LogicalEquality(_gr1.gr_ucs, _gr2.gr_ucs)) return false;

            // skipping transformation matrices for now...

            if (_gr1.inst_size == null && _gr2.inst_size != null) return false;
            if (_gr1.inst_size != null && _gr2.inst_size == null) return false;
            if (_gr1.inst_size != null && _gr2.inst_size != null)
            {
                int nrSize = _gr1.inst_size.Count;
                if (nrSize != _gr2.inst_size.Count) return false;

                if (nrSize > 0)
                {
                    bool same_size = _gr1.inst_size.SequenceEqual(_gr2.inst_size);
                    if (!same_size) return false;
                }
            }

            if (_gr1.inst_nwe_id != _gr2.inst_nwe_id) return false;
            if (_gr1.inst_nwe_name != _gr2.inst_nwe_name) return false;

            if (_gr1.inst_path == null && _gr2.inst_path != null) return false;
            if (_gr1.inst_path != null && _gr2.inst_path == null) return false;
            if (_gr1.inst_path != null && _gr2.inst_path != null)
            {
                int nrPathP = _gr1.inst_path.Count;
                if (nrPathP != _gr2.inst_path.Count) return false;

                if (nrPathP > 0)
                {
                    bool same_path = _gr1.inst_path.Zip(_gr2.inst_path, (x, y) => GeometricTransformations.LogicalEquality(x, y)).Aggregate(true, (a, x) => a && x);
                    if (!same_path) return false;
                }
            }

            return true;
        }

        public static bool HaveListsWSameContent(List<GeometricRelationship> _lgr1, List<GeometricRelationship> _lgr2)
        {
            if (_lgr1 == null && _lgr2 != null) return false;
            if (_lgr1 != null && _lgr2 == null) return false;
            if (_lgr1 == null && _lgr2 == null) return true;

            int nrL1 = _lgr1.Count;
            if (nrL1 != _lgr2.Count) return false;

            GeometricRelationshipComparer comparer = new GeometricRelationshipComparer();
            
            List<GeometricRelationship> l1_sorted = new List<GeometricRelationship>(_lgr1);
            l1_sorted.Sort(comparer);
            List<GeometricRelationship> l2_sorted = new List<GeometricRelationship>(_lgr2);
            l2_sorted.Sort(comparer);

            bool same_lists = l1_sorted.Zip(l2_sorted, (x, y) => GeometricRelationship.HaveSameContent(x, y)).Aggregate(true, (a, x) => a && x);
            if (!same_lists) return false;

            return true;
        }

        #endregion

        #region CLASS MEMBERS

        protected long gr_id;
        protected string gr_name;
        protected Relation2GeomState gr_state;
        protected Point4D gr_ids;
        protected Matrix3D gr_ucs;
        protected Matrix3D gr_trWC2LC;
        protected Matrix3D gr_trLC2WC;

        protected List<double> inst_size;
        protected long inst_nwe_id;
        protected string inst_nwe_name;
        protected List<Point3D> inst_path;

        #endregion

        #region PROPERTIES (Get only)

        public long GrID { get { return this.gr_id; } }
        public string GrName { get { return this.gr_name; } }
        public Relation2GeomState GrState { get { return this.gr_state; } }
        public Point4D GrIds { get { return this.gr_ids; } }
        public Matrix3D GrUCS { get { return this.gr_ucs; } }
        public Matrix3D GrTrWC2LC { get { return this.gr_trWC2LC; } }
        public Matrix3D GrTrLC2WC { get { return this.gr_trLC2WC; } }


        public ReadOnlyCollection<double> InstSize { get { return this.inst_size.AsReadOnly(); } }
        public long InstNWeId { get { return this.inst_nwe_id; } }
        public string InstNWeName { get { return this.inst_nwe_name; } }
        public ReadOnlyCollection<Point3D> InstPath { get { return this.inst_path.AsReadOnly(); } }

        #endregion

        #region .CTORS
        public GeometricRelationship(long _gr_id, string _gr_name, Relation2GeomState _gr_state, Point4D _gr_ids,
                                      Matrix3D _gr_ucs, Matrix3D _gr_trWC2LC, Matrix3D _gr_trLC2WC)
        {
            this.gr_id = _gr_id;
            this.gr_name = _gr_name;
            this.gr_state = _gr_state;
            this.gr_ids = _gr_ids;
            this.gr_ucs = _gr_ucs;
            this.gr_trWC2LC = _gr_trWC2LC;
            this.gr_trLC2WC = _gr_trLC2WC;

            this.inst_size = new List<double>();
            this.inst_nwe_id = -1L;
            this.inst_nwe_name = "NW_Element";
            this.inst_path = new List<Point3D>();
        }

        public GeometricRelationship(long _gr_id, string _gr_name, Relation2GeomState _gr_state, Point4D _gr_ids,
                                      Matrix3D _gr_ucs, Matrix3D _gr_trWC2LC, Matrix3D _gr_trLC2WC,
                                      List<double> _inst_size, long _inst_nwe_id, string _inst_nwe_name, List<Point3D> _inst_path)
        {
            this.gr_id = _gr_id;
            this.gr_name = _gr_name;
            this.gr_state = _gr_state;
            this.gr_ids = _gr_ids;
            this.gr_ucs = _gr_ucs;
            this.gr_trWC2LC = _gr_trWC2LC;
            this.gr_trLC2WC = _gr_trLC2WC;

            this.inst_size = new List<double>(_inst_size);
            this.inst_nwe_id = _inst_nwe_id;
            this.inst_nwe_name = (string.IsNullOrEmpty(_inst_nwe_name)) ? "NW_Element" : _inst_nwe_name;
            this.inst_path = new List<Point3D>(_inst_path);
        }

        public GeometricRelationship(GeometricRelationship _original)
        {
            this.gr_id = _original.gr_id;
            this.gr_name = _original.gr_name;
            this.gr_state = new Relation2GeomState { Type = _original.gr_state.Type, IsRealized = false };
            this.gr_ids = _original.gr_ids;
            this.gr_ucs = _original.gr_ucs;
            this.gr_trWC2LC = _original.gr_trWC2LC;
            this.gr_trLC2WC = _original.gr_trLC2WC;

            this.inst_size = new List<double>(_original.InstSize);
            this.inst_nwe_id = _original.InstNWeId;
            this.inst_nwe_name = _original.InstNWeName;
            this.inst_path = new List<Point3D>(_original.InstPath);
        }

        public void CopyPlacableContent(GeometricRelationship _original)
        {
            this.gr_ucs = _original.gr_ucs;
            this.gr_trWC2LC = _original.gr_trWC2LC;
            this.gr_trLC2WC = _original.gr_trLC2WC;

            this.inst_path = new List<Point3D>(_original.InstPath);
        }

        #endregion

        #region METHODS: Info

        public List<double> GetSizeInfo(bool _max, double _scale = 1.0)
        {
            List<double> size = new List<double>();
            if (_max)
            {
                if (this.InstSize.Count > 5)
                    size = new List<double> { this.InstSize[3] * _scale, this.InstSize[4] * _scale, this.InstSize[5] * _scale };
            }
            else
            {
                if (this.InstSize.Count > 2)
                    size = new List<double> { this.InstSize[0] * _scale, this.InstSize[1] * _scale, this.InstSize[2] * _scale };
            }

            return size;
        }

        public Point3D GetOriginUCS()
        {
            return new Point3D(this.gr_ucs.OffsetX, this.gr_ucs.OffsetY, this.gr_ucs.OffsetZ);
        }

        #endregion

        #region METHODS: Management
        public void SetTypeToNotRealized()
        {
            this.gr_state.IsRealized = false;
            this.gr_ids = new Point4D(-1, -1, -1, -1);
        }

        public void SetTypeToRealized(Point4D _ids)
        {
            this.gr_state.IsRealized = true;
            this.gr_ids = new Point4D(_ids.X, _ids.Y, _ids.Z, _ids.W);
        }

        public void SetTypeToRealized(Matrix3D _geom_ucs, Matrix3D _geom_trWC2LC, Matrix3D _geom_trLC2WC, Point4D _geom_ids)
        {
            this.gr_state.IsRealized = true;

            this.gr_ucs = _geom_ucs;
            this.gr_trWC2LC = _geom_trWC2LC;
            this.gr_trLC2WC = _geom_trLC2WC;

            this.gr_ids = new Point4D(_geom_ids.X, _geom_ids.Y, _geom_ids.Z, _geom_ids.W);
        }
        #endregion

        #region METHODS: Geometrical Transformations
        public void MoveAlongUCSx(double _amount)
        {
            // this.gr_ucs.OffsetX += _amount; // wrong
            GeometricTransformations.TranslateMatrixAlongX(ref this.gr_ucs, _amount);
        }

        public void MoveAlongUCSy(double _amount)
        {
            // this.gr_ucs.OffsetY += _amount; // wrong
            GeometricTransformations.TranslateMatrixAlongY(ref this.gr_ucs, _amount);
        }

        public void MoveAlongUCSz(double _amount)
        {
            // this.gr_ucs.OffsetZ += _amount; // wrong
            GeometricTransformations.TranslateMatrixAlongZ(ref this.gr_ucs, _amount);
        }
        #endregion

        #region METHODS: Path Transfer

        /// <summary>
        /// <para>Only the internal points are transferred.</para>
        /// <para>The first point in the path contains connectivity info and cannot be changed.</para>
        /// <para>The second and last points are the connection to adjacent components and cannot be changed.</para>
        /// </summary>
        /// <param name="_internal_points"></param>
        public void PathTransfer(List<Point3D> _internal_points)
        {
            // replace the internal points
            List<Point3D> path_new = new List<Point3D>();

            path_new.Add(this.inst_path[0]);
            path_new.Add(this.inst_path[1]);
            if (_internal_points != null)
                path_new.AddRange(_internal_points);
            path_new.Add(this.inst_path[this.inst_path.Count - 1]);

            this.inst_path = path_new;
        }

        public void PathConnectivityUpate(Point3D _connection_start, Point3D _connection_end)
        {
            if (_connection_start == null || _connection_end == null) return;

            this.inst_path[1] = _connection_start;
            this.inst_path[this.inst_path.Count - 1] = _connection_end;
        }

        /// <summary>
        /// To be used by CompRepContainedIn_Instance for surface geometry transfer.
        /// </summary>
        /// <param name="_connectivity"></param>
        /// <param name="_polygon"></param>
        public void PathAsPolygonGeometry(Point3D _connectivity, List<Point3D> _polygon)
        {
            if (_polygon == null || _polygon.Count < 3) return;
            if (this.gr_state.Type != Relation2GeomType.DESCRIBES_2DorLESS) return;

            this.inst_path = new List<Point3D> { _connectivity };
            this.inst_path.AddRange(_polygon);
        }

        #endregion
    }

    #region CUSTOM COMPARER

    public class GeometricRelationshipComparer : IComparer<GeometricRelationship>
    {
        public int Compare(GeometricRelationship _gr1, GeometricRelationship _gr2)
        {
            if (_gr1 == null && _gr2 != null) return -1;
            if (_gr1 == null && _gr2 == null) return 0;
            if (_gr1 != null && _gr2 == null) return 1;

            if (_gr1.GrID == _gr2.GrID)
            {
                // compare the placement ids too
                Point4DComparer p4Dc = new Point4DComparer();
                return p4Dc.Compare(_gr1.GrIds, _gr2.GrIds);
            }
            else if (_gr1.GrID > _gr2.GrID)
                return 1;
            else
                return -1;
        }
    }

    #endregion
}
