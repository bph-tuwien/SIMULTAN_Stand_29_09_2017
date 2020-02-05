using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.EntityGeometry
{
    public class ZonedPolygonVertexVis : GeometricEntity
    {

        #region OWNERSHIP Properties

        private ZonedPolygon owner;
        public ZonedPolygon Owner
        {
            get { return this.owner; }
            private set 
            { 
                this.owner = value;
                RegisterPropertyChanged("Owner");
            }
        }

        private int indexInOwner;
        public int IndexInOwner
        {
            get { return this.indexInOwner; }
            private set
            { 
                this.indexInOwner = value;
                RegisterPropertyChanged("IndexInOwner");
            }
        }

        private int zoneInOwner;
        public int ZoneInOwner
        {
            get { return this.zoneInOwner; }
            set 
            { 
                this.zoneInOwner = value;
                RegisterPropertyChanged("ZoneInOwner");
            }
        }

        // this is NOT the zone label
        // it is an alias for displaying purposes
        private string label;
        public string Label
        {
            get { return this.label; }
            set 
            { 
                this.label = value;
                RegisterPropertyChanged("Label");
            }
        }

        #endregion

        #region POSITION Properties

        private Vector3 positionOriginal;
        // position vector
        private Vector3 position;
        public Vector3 Position
        {
            get { return position; }
            private set 
            { 
                position = value;
                RegisterPropertyChanged("Position");
            }
        }

        // position vector
        private Vector3 moveLimForward;
        private Vector3 moveLimBackward;
        // directional vector
        private Vector3 moveVectForward;
        private Vector3 moveVectBackward;

        //private float maxMoveDistForwardCurrent;
        private float maxMoveDistForward;
        public float MaxMoveDistForward
        {
            get { return this.maxMoveDistForward; }
            private set
            {
                this.maxMoveDistForward = value;
                RegisterPropertyChanged("MaxMoveDistForward");
            }
        }

        //private float maxMoveDistBackwardCurrent;
        private float maxMoveDistBackward;
        public float MaxMoveDistBackward
        {
            get { return this.maxMoveDistBackward; }
            set 
            { 
                this.maxMoveDistBackward = value;
                RegisterPropertyChanged("MaxMoveDistBackward");
            }
        }

        #endregion

        public ZonedPolygonVertexVis(ZonedPolygon _owner, int _indInOwner, Layer _layer, Vector3 _position, 
            Vector3 _moveLimitForward, Vector3 _moveLimitBackward) : base(_layer)
        {
            this.Owner = _owner;
            this.AdjustToPolygonChange(_indInOwner, _position, _moveLimitForward, _moveLimitBackward);
        }

        public void AdjustToPolygonChange(int _indInOwner, Vector3 _position, 
                                            Vector3 _moveLimitForward, Vector3 _moveLimitBackward)
        {
            this.IndexInOwner = _indInOwner;

            this.moveLimForward = _moveLimitForward;
            this.moveLimBackward = _moveLimitBackward;
            this.Position = _position;
            this.positionOriginal = _position;

            this.moveVectForward = this.moveLimForward - this.position;
            this.MaxMoveDistForward = this.moveVectForward.Length();
            this.moveVectForward.Normalize();

            this.moveVectBackward = this.moveLimBackward - this.position;
            this.MaxMoveDistBackward = this.moveVectBackward.Length();
            this.moveVectBackward.Normalize();
        }

        public override string GetDafaultName()
        {
            return "Zoned Polygon Vertex";
        }

        public override LineGeometry3D Build(double _startMarkerSize = 0.1)
        {
            LineBuilder b = new LineBuilder();
            b.AddBox(this.Position, _startMarkerSize, _startMarkerSize, _startMarkerSize);
            return b.ToLineGeometry3D();
        }

        public bool SetAt(bool _forward, float _distance)
        {
            float admissibleDist;
            Vector3 e;
            if (_forward)
            {
                admissibleDist = this.MaxMoveDistForward;
                e = this.moveVectForward;  
            }
            else
            {
                admissibleDist = this.MaxMoveDistBackward;
                e = this.moveVectBackward;  
            }
            if (_distance <= admissibleDist)
            {
                this.Position = this.positionOriginal + e * _distance;
                this.Owner.ModifyVertex(this.IndexInOwner, this.Position);
                return true;
            }

            return false;
        }

    }
}
