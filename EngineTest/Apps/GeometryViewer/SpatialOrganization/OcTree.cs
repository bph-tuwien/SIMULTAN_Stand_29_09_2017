using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Data;
using System.ComponentModel;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.SpatialOrganization
{
    public class OcTree
    {
        private const int NR_LIFE_CYCLES_INIT = 2;
        public const double VOLUME_SIZE_MAX = 1;
        private const float ENCLOSING_BOX_TOLERANCE = 0.1f;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ========================================== INTERNAL CLASS MEMBERS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private List<Utils.LineWHistory> nodeLines;
        private OcTree[] childNodes;
        private byte activeNodes;
        private OcTree parent;
        // private int lifeCyclesLeft;
        private BoundingBox region;
        private double minVolSize;
        public bool TreeBuilt { get; private set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =============================================== CONSTRUCTORS =========================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public OcTree()
        {
            this.nodeLines = new List<Utils.LineWHistory>();
            this.childNodes = new OcTree[8];
            this.activeNodes = 0;
            this.parent = null;
            // this.lifeCyclesLeft = OcTree.NR_LIFE_CYCLES_INIT;
            this.region = new BoundingBox(Vector3.Zero, Vector3.Zero);
            this.minVolSize = VOLUME_SIZE_MAX;
            this.TreeBuilt = false;

        }

        private OcTree createNode(BoundingBox _region, List<Utils.LineWHistory> _lines, double _minVolSize)
        {
            if (_lines == null || _lines.Count == 0)
                return null;

            OcTree n = new OcTree();
            n.region = _region;
            n.nodeLines = _lines;
            n.minVolSize = _minVolSize;
            n.parent = this;

            return n;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================= UPDATING METHODS ========================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Build / Reset
        /// <summary>
        /// Builds the tree the FIRST time it receives any objects.
        /// </summary>
        public void BuildTree(double _minVolumeSize = VOLUME_SIZE_MAX, List<Utils.LineWHistory> _linesIn = null)
        {
            // if the tree is being rebuilt, reset it
            if (this.TreeBuilt)
                ResetTree();

            // insert the input lines in the node
            if (_linesIn != null)
                this.nodeLines = new List<Utils.LineWHistory>(_linesIn);

            // no more subdivision necessary if only one or less line segments contained
            if (this.nodeLines.Count <= 1)
                return;

            // check the size (happens in the root node)
            Vector3 dimensions = this.region.Maximum - this.region.Minimum;
            if (dimensions == Vector3.Zero)
            {
                FindEnclosingCube();
                dimensions = this.region.Maximum - this.region.Minimum;
            }

            // check for reaching minimum size
            if (dimensions.X <= _minVolumeSize || dimensions.Y <= _minVolumeSize || dimensions.Z <= _minVolumeSize)
                return;


            //creating new regions for each octant
            this.minVolSize = _minVolumeSize;
            BoundingBox[] octants = getChildRegions(this.region, this.minVolSize);
            if (octants != null)
            {

                // containers for the lines contained in each octant
                List<Utils.LineWHistory>[] octantLines = new List<Utils.LineWHistory>[8];
                for (int i = 0; i < 8; i++) octantLines[i] = new List<Utils.LineWHistory>();
                // remember which nodeLines to remove from this node
                List<Utils.LineWHistory> delFromThis = new List<Utils.LineWHistory>();

                foreach (var line in this.nodeLines)
                {
                    BoundingBox bb = line.BB;
                    for (int a = 0; a < 8; a++)
                    {
                        if (octants[a].Contains(ref bb) == ContainmentType.Contains)
                        {
                            octantLines[a].Add(line);
                            delFromThis.Add(line);
                        }
                    }
                }

                // remove lines from this node if they are contained completely in an octant
                foreach (var line in delFromThis)
                    this.nodeLines.Remove(line);

                // create the child nodes
                for (int a = 0; a < 8; a++)
                {
                    if (octantLines[a].Count > 0)
                    {
                        this.childNodes[a] = createNode(octants[a], octantLines[a], this.minVolSize);
                        this.activeNodes |= (byte)(1 << a);
                        this.childNodes[a].BuildTree(this.minVolSize);
                    }
                }
            }
            // finish building
            this.TreeBuilt = true;
        }

        private void ResetTree()
        {
            this.nodeLines = new List<Utils.LineWHistory>();
            this.childNodes = new OcTree[8];
            this.activeNodes = 0;
            this.region = new BoundingBox(Vector3.Zero, Vector3.Zero);
            this.minVolSize = VOLUME_SIZE_MAX;
            this.TreeBuilt = false;
        }

        /// <summary>
        /// Updates the tree after a change in the contained objects.
        /// </summary>
        public void UpdateTree(List<Utils.LineWHistory> _linesIn = null)
        {
            if (!this.TreeBuilt)
                return;

            if (_linesIn == null)
                return;

            // REDISTRIBUTE THE CHANGED LINES ALONG THE TREE
            // 0 - no change
            // 1 - position changed (unselect)
            // 2 - new line (draw - add at the end)
            // 3 - deleted line (delete - compact and close the gap)

            // LineGenerator3D          coords0     coords0_change
            // 222222   (draw)          123456      123456
            // 000110   (edit)          123456      123456
            // 00000022 (draw)          12345678    1235678
            // 01100111 (edit)          12345678    12345678
            // 33000000 (delete)        345678      12345678
            //   110000 (edit)...       345678      345678

            bool successUpdate = true;
            foreach (var line in _linesIn)
            {
                if (line.History == Utils.LineChange.DELETED)
                {
                    successUpdate &= Delete(line);
                }
                else if (line.History == Utils.LineChange.DRAWN)
                {
                    //// debug
                    //var test1 = this.ToString();
                    successUpdate &= Insert(line);
                    //// debug
                    //var test2 = this.ToString();
                }
                else if (line.History == Utils.LineChange.EDITED)
                {
                    successUpdate &= Delete(line);
                    successUpdate &= Insert(line);
                }
                if (!successUpdate)
                    break;
            }

            // if the update was not successful, 
            // we probably went out of the root node region with one of the lines
            // so we must rebuild the tree
            // OTHERWISE: prune empty children nodes
            if (!successUpdate)
                BuildTree(this.minVolSize, _linesIn);
            else
                Prune();

        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ====================================== PRIVATE MODIFICATION METHODS ==================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Tree MODIFICATION ( INSERT, DELETE, PRUNE)
        private bool Insert(Utils.LineWHistory _line)
        {
            if (_line == null)
                return false;

            BoundingBox bb = _line.BB;
            if (this.region.Contains(ref bb) != ContainmentType.Contains)
            {
                // something went wrong OR the tree needs to be rebuilt, 
                // because the line is not contained in the root node
                return false;
            }

            bool insertedLine = false;
            // try inserting it in any of the children nodes
            BoundingBox[] octants = getChildRegions(this.region, this.minVolSize); 
            for (int index = 0; index < 8; index++)
            {
                if (this.childNodes[index] != null)
                {
                    // existing child node
                    if (this.childNodes[index].region.Contains(ref bb) == ContainmentType.Contains)
                        insertedLine = this.childNodes[index].Insert(_line);
                    if (insertedLine && !this.childNodes[index].TreeBuilt)
                    {
                        this.childNodes[index].BuildTree(this.minVolSize);
                        break;
                    }
                }
                else
                {
                    // possible new child node
                    if ((octants != null) && (octants[index].Contains(ref bb) == ContainmentType.Contains))
                    {
                        this.childNodes[index] = createNode(octants[index], new List<Utils.LineWHistory>() { _line}, this.minVolSize);
                        this.activeNodes |= (byte)(1 << index);
                        // this.childNodes[index].BuildTree(this.minVolSize);
                        insertedLine = true;
                        break;
                    }
                }
            }
            
            // if none of the children could take the line, it remains in this node
            if (!insertedLine)
            {
                this.nodeLines.Add(_line);
                insertedLine = true;
            }

            return insertedLine;
        }

        private bool Delete(Utils.LineWHistory _line)
        {
            if (_line == null)
                return false;

            bool deletedLine = false;
            // try deleting in this node
            foreach(var line in this.nodeLines)
            {
                if (line.ID == _line.ID)
                {
                    this.nodeLines.Remove(line);
                    deletedLine = true;
                    break;
                }
            }

            // if not successful, try in the child nodes
            if (!deletedLine && this.hasChildren())
            {
                for (int flags = this.activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                {
                    if ((flags & 1) == 1)
                    {
                        deletedLine = this.childNodes[index].Delete(_line);
                        if (deletedLine)
                            break;
                    }
                }
            }

            return deletedLine;
        }

        private void Prune()
        {
            if (this.parent == null && this.nodeLines.Count == 0 && !this.hasChildren())
            {
                ResetTree();
                return;
            }

            if (this.hasChildren())
            {
                byte activeNodes_updated = 0;
                for (int flags = this.activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                {
                    if ((flags & 1) == 1)
                    {
                        if (this.childNodes[index].hasChildren())
                        {
                            this.childNodes[index].Prune();
                            if (this.childNodes[index].hasChildren() || this.childNodes[index].nodeLines.Count > 0)
                                activeNodes_updated |= (byte)(1 << index);
                            else
                                this.childNodes[index] = null;
                        }
                        else 
                        {
                            if (this.childNodes[index].nodeLines.Count == 0)
                                this.childNodes[index] = null;
                            else
                                activeNodes_updated |= (byte)(1 << index);
                        }
                    }
                }
                this.activeNodes = activeNodes_updated;
            }
        }
        #endregion

        #region INFO
        public List<BoundingBox> GetRegionList()
        {
            List<BoundingBox> list = new List<BoundingBox>();

            list.Add(this.region);
            if (this.hasChildren())
            {
                for (int flags = this.activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                {
                    if ((flags & 1) == 1)
                    {
                        list.AddRange(this.childNodes[index].GetRegionList());
                    }
                }
            }

            return list;
        }

        private List<Utils.LineWHistory> GetAllLines()
        {
            List<Utils.LineWHistory> lines = new List<Utils.LineWHistory>();
            lines.AddRange(this.nodeLines);
            if (this.hasChildren())
            {
                for (int flags = this.activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                {
                    if ((flags & 1) == 1)
                    {
                        lines.AddRange(this.childNodes[index].GetAllLines());
                    }
                }
            }

            return lines;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================= COLLISION DETECTION METHODS ==================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region COLLISION / VISIBILITY
        public void GetCollisionRegions(ref List<BoundingBox> _regions, ref List<Vector3> _colPos,
                                        double _tolerance, List<Utils.LineWHistory> _parentList = null)
        {

            List<Utils.LineWHistory> allLines = new List<Utils.LineWHistory>(this.nodeLines);
            if (_parentList != null && _parentList.Count > 0)
                allLines.AddRange(_parentList);

            List<Vector3> pp;
            int nrDet = Utils.LineWHistory.AllCollisionsDetected(allLines, _tolerance, out pp, true);
            if (nrDet > 0)
            {
                _regions.Add(this.region);
                _colPos.AddRange(pp);
            }

            if (this.hasChildren())
            {
                for (int flags = this.activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                {
                    if ((flags & 1) == 1)
                    {
                        this.childNodes[index].GetCollisionRegions(ref _regions, ref _colPos, _tolerance, this.nodeLines);
                    }
                }
            }

        }

        
        public void GetVisibleRegions(ref List<BoundingBox> _regions, ref List<Point3D> _pointsE, ref List<Point3D> _pointsM,
                                            ViewFrustumFunctions _vff)
        {
            if (_vff == null)
                return;
            if (_regions == null)
                _regions = new List<BoundingBox>();
            if (_pointsE == null)
                _pointsE = new List<Point3D>();
            if (_pointsM == null)
                _pointsM = new List<Point3D>();

            bool notOutside = !_vff.IsOutsideFrustum(this.region);
            if (notOutside)
            {
                _regions.Add(this.region);
                Utils.LineWHistory.LinesWH2UniqueP3D(this.nodeLines, ref _pointsE, ref _pointsM, true);

                // check deeper into the tree
                if (this.hasChildren())
                {
                    for (int flags = this.activeNodes, index = 0; flags > 0; flags >>= 1, index++)
                    {
                        if ((flags & 1) == 1)
                        {
                            this.childNodes[index].GetVisibleRegions(ref _regions, ref _pointsE, ref _pointsM, _vff);
                        }
                    }
                }
            }
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ============================================== UTILITY METHODS ========================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region UTILITIES
        private void FindEnclosingCube()
        {
            FindEnclosingBoundingBox();

            // find the enclosing cube with side length the POWER of 2
            Vector3 size = this.region.Maximum - this.region.Minimum;
            float maxLen = Math.Max(size.X, Math.Max(size.Y, size.Z));
            int nextPow2 = 0;
            for (int b = 0; b < 32; b++)
            {
                int bs = (1 << b);
                if (maxLen > bs)
                    nextPow2 = bs;
                else
                    break;
            }
            if (maxLen > nextPow2)
                nextPow2 <<= 1;

            this.region = new BoundingBox(this.region.Minimum, this.region.Minimum + new Vector3(nextPow2));
        }

        private void FindEnclosingBoundingBox()
        {
            int n = this.nodeLines.Count;
            Vector3 global_min = this.region.Minimum;
            Vector3 global_max = this.region.Maximum;

            for (int i = 0; i < n; i++)
            {
                BoundingBox bbi = this.nodeLines[i].BB;

                if (bbi.Minimum.X < global_min.X) global_min.X = bbi.Minimum.X;
                if (bbi.Minimum.Y < global_min.Y) global_min.Y = bbi.Minimum.Y;
                if (bbi.Minimum.Z < global_min.Z) global_min.Z = bbi.Minimum.Z;

                if (bbi.Maximum.X > global_max.X) global_max.X = bbi.Maximum.X;
                if (bbi.Maximum.Y > global_max.Y) global_max.Y = bbi.Maximum.Y;
                if (bbi.Maximum.Z > global_max.Z) global_max.Z = bbi.Maximum.Z;
            }

            Vector3 tolerance = new Vector3(ENCLOSING_BOX_TOLERANCE, ENCLOSING_BOX_TOLERANCE, ENCLOSING_BOX_TOLERANCE);
            global_min -= tolerance;
            global_max += tolerance;

            this.region = new BoundingBox(global_min, global_max);

        }

        private bool hasChildren()
        {
            //bool result = true;
            //for (int i = 0; i < 8; i++)
            //{
            //    result &= (this.childNodes[i] != null);
            //}
            //return result;

            return this.activeNodes != 0;
        }

        private static BoundingBox[] getChildRegions(BoundingBox _region, double _minVolSize)
        {           
            if (_region == null)
                return null;

            Vector3 dimensions = _region.Maximum - _region.Minimum;
            Vector3 half = dimensions / 2f;
            Vector3 center = _region.Minimum + half;
            if (half.X < _minVolSize || half.Y < _minVolSize || half.Z < _minVolSize)
                return null;

            BoundingBox[] octants = new BoundingBox[8];
            // lower octants:   3 2  higher octatnts: 7 6
            //                  0 1                   4 5
            octants[0] = new BoundingBox(_region.Minimum, center);
            octants[1] = new BoundingBox(new Vector3(center.X, _region.Minimum.Y, _region.Minimum.Z), new Vector3(_region.Maximum.X, center.Y, center.Z));
            octants[2] = new BoundingBox(new Vector3(center.X, _region.Minimum.Y, center.Z), new Vector3(_region.Maximum.X, center.Y, _region.Maximum.Z));
            octants[3] = new BoundingBox(new Vector3(_region.Minimum.X, _region.Minimum.Y, center.Z), new Vector3(center.X, center.Y, _region.Maximum.Z));

            octants[4] = new BoundingBox(new Vector3(_region.Minimum.X, center.Y, _region.Minimum.Z), new Vector3(center.X, _region.Maximum.Y, center.Z));
            octants[5] = new BoundingBox(new Vector3(center.X, center.Y, _region.Minimum.Z), new Vector3(_region.Maximum.X, _region.Maximum.Y, center.Z));
            octants[6] = new BoundingBox(center, _region.Maximum);
            octants[7] = new BoundingBox(new Vector3(_region.Minimum.X, center.Y, center.Z), new Vector3(center.X, _region.Maximum.Y, _region.Maximum.Z));

            return octants;
        }

        public string ToString(int _index = -1, string _indent = "")
        {
            string tree = _indent + _index.ToString() + ":[ ";
            foreach(var line in this.nodeLines)
            {
                tree += line.ID.ToString() + " ";
            }
            tree += "]\n";

            for (int i = 0; i < 8; i++ )
            {
                if (this.childNodes[i] != null)
                    tree += this.childNodes[i].ToString(i, _indent + "\t");
            }
            return tree;
        }
        #endregion
    }
}
