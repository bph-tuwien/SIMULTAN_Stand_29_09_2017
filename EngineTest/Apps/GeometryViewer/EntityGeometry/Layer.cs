using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HelixToolkit.SharpDX;
using HelixToolkit.SharpDX.Wpf;
using SharpDX;

using GeometryViewer.EntityDXF;

namespace GeometryViewer.EntityGeometry
{
    public class Layer : Entity
    {
        public Layer()
            : base()
        {
            this.ContainedEntities = new List<Entity>();
        }

        public Layer(string _name, Color _color)
            : base(_name, _color)
        {
            this.ContainedEntities = new List<Entity>();
        }

        // only for parsing
        internal Layer(long _id, string _name, SharpDX.Color _color, EntityVisibility _vis, bool _is_valid, bool _assoc_w_comp,
                        float _line_thickness_GUI, List<string> _mLtext, bool _is_top_closure, bool _is_bottom_closure)
            :base(_id, _name, _color, _vis, _is_valid, _assoc_w_comp, _line_thickness_GUI, _mLtext, _is_top_closure, _is_bottom_closure)
        {
            this.ContainedEntities = new List<Entity>();
        }

        #region OVERRIDES
        public override string GetDafaultName()
        {
            return "Layer";
        }

        public override string ToString()
        {
            return "Layer " + this.ID + ": " + this.EntityName;
        }

        public override void AddToExport(ref StringBuilder _sb, bool _with_contained_entites)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());      // 0
            _sb.AppendLine(DXFUtils.GV_LAYER);                                  // GV_LAYER

            _sb.AppendLine(((int)EntitySaveCode.CLASS_NAME).ToString());        // 100 (subclass marker)
            _sb.AppendLine(this.GetType().ToString());                          // GeometryViewer.EntityGeometry.ZonedVolume

            base.AddToExport(ref _sb, _with_contained_entites);
        }
        #endregion

        #region METHODS: Layer Management
        public bool AddEntity(Entity _e)
        {
            // cannot assign self as SubEntity 
            if (Object.ReferenceEquals(this, _e)) return false;

            // cannot create a NULL SubEntity 
            if (Object.ReferenceEquals(_e, null)) return false;

            // cannot take same Subentity twice
            if (this.ContainedEntities.Contains(_e)) return false;

            this.ContainedEntities.Add(_e);
            return true;
        }

        public bool RemoveEntity(Entity _e)
        {
            if (_e == null) return false;
            bool succes_level0 = this.ContainedEntities.Remove(_e);
            if (succes_level0)
                return true;
            else
            {
                foreach(Entity subE in this.ContainedEntities)
                {
                    Layer subL = subE as Layer;
                    if(subL != null)
                    {
                        bool succes_levelN = subL.RemoveEntity(_e);
                        if (succes_levelN)
                            return true;
                    }
                }
                return false;
            } 
        }
        #endregion

        #region METHODS: Layer Info
        public List<Layer> GetFlatLayerList()
        {
            if (this.ContainedEntities == null || this.ContainedEntities.Count < 1)
                return new List<Layer>();

            List<Layer> list = new List<Layer>();
            foreach(Entity e in this.ContainedEntities)
            {
                Layer layer = e as Layer;
                if (layer != null)
                {
                    list.Add(layer);
                    list.AddRange(layer.GetFlatLayerList());
                }
            }
            return list;
        }

        public Dictionary<Layer, string> GetFlatQualifiedLayerNameList()
        {
            if (this.ContainedEntities == null || this.ContainedEntities.Count < 1)
                return new Dictionary<Layer, string>();

            Dictionary<Layer, string> names = new Dictionary<Layer, string>();

            foreach (Entity e in this.ContainedEntities)
            {
                Layer layer = e as Layer;
                if (layer != null)
                {
                    names.Add(layer, this.EntityName + "__" + layer.EntityName);

                    Dictionary<Layer, string> names_sublayers = layer.GetFlatQualifiedLayerNameList();
                    foreach(var entry in names_sublayers)
                    {
                        names.Add(entry.Key, entry.Value);
                    }
                }
            }
            return names;
        }

        public List<GeometricEntity> GetFlatGeometryList()
        {
            if (this.ContainedEntities == null || this.ContainedEntities.Count < 1)
                return new List<GeometricEntity>();

            List<GeometricEntity> list = new List<GeometricEntity>();
            foreach (Entity e in this.ContainedEntities)
            {
                GeometricEntity ge = e as GeometricEntity;
                Layer layer = e as Layer;
                if (ge != null)
                {
                    list.Add(ge);                    
                }
                else if (layer != null)
                {
                    list.AddRange(layer.GetFlatGeometryList());
                }
            }
            return list;
        }
        #endregion
    }
}
