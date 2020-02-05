using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

namespace GeometryViewer.EntityGeometry
{    
    public abstract class GeometricEntity : Entity
    {
        #region PROPERTIES: Layer, Color

        protected Layer entityLayer;
        public Layer EntityLayer
        {
            get { return this.entityLayer; }
            set
            {
                // if new layer is null -> not valid
                if (value == null)
                {
                    this.entityLayer = null;
                    this.IsValid = false;
                    return;
                }
                // do nothing, if layer is the same
                if (Object.ReferenceEquals(this.entityLayer, value))
                    return;

                // remove from old layer
                if (this.entityLayer != null)
                    this.entityLayer.RemoveEntity(this);

                // add to new layer
                this.entityLayer = value;
                this.entityLayer.AddEntity(this);
                this.IsValid = true;

                // force propagation of visibility from layer to geometry
                EntityVisibility layerVis = this.entityLayer.Visibility;
                this.entityLayer.Visibility = layerVis;
            }
        }

        protected bool colorByLayer;
        public bool ColorByLayer
        {
            get { return this.colorByLayer; }
            set 
            { 
                this.colorByLayer = value;
                RegisterPropertyChanged("ColorByLayer");
            }
        }

        #endregion

        #region .CTOR
        public GeometricEntity(Layer _layer) : base()
        {
            this.HasGeometry = true;
            this.EntityLayer = _layer;
            this.ColorByLayer = true;
        }

        public GeometricEntity(string _name, Layer _layer) : base(_name)
        {
            this.HasGeometry = true;
            this.EntityLayer = _layer;
            this.ColorByLayer = true;
        }
        #endregion

        #region PARSING .CTOR
        public GeometricEntity(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp,
                        float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure, Layer _layer, bool _color_by_layer)
            : base(_id, _name, _color, _vis, _is_valid, _assoc_w_comp, _line_thickness_GUI, _mLtext, _is_top_closure, _is_bottom_closure)
        {
            this.HasGeometry = true;
            this.EntityLayer = _layer;
            this.ColorByLayer = _color_by_layer;
        }
        #endregion

        public override string GetDafaultName()
        {
            return "Geometric Entity";
        }

        public virtual LineGeometry3D Build(double _startMarkerSize = 0.1)
        {
            return null;
        }

    }
}
