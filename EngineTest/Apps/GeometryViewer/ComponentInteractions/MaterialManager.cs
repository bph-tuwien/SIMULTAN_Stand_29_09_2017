using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

using GeometryViewer.ComponentReps;
using GeometryViewer.EntityDXF;
using InterProcCommunication;

namespace GeometryViewer.ComponentInteraction
{
    public class MaterialManager
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ==================================== CLASS MEMEBERS AND INITIALIZATION ================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region INIT

        private List<Material> materials;
        public ReadOnlyCollection<Material> Materials { get { return this.materials.AsReadOnly(); } }
        private Material default_material;

        public MaterialManager()
        {
            this.Reset();
        }

        internal void Reset()
        {
            this.default_material = Material.Default;
            this.materials = new List<Material>();
            this.materials.Add(this.default_material);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =========================================== COMPONENT MANAGEMENT ======================================= //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region MANAGEMENT Materials

        public bool AddMaterial(Material _new_material, bool _allowDuplicateNames = true)
        {
            if (_new_material == null) return false;
            Material duplicate = this.materials.FirstOrDefault(x => x.ID == _new_material.ID);
            if (duplicate != null) return false;

            // no duplicate names
            if (!_allowDuplicateNames)
            {
                List<Material> materials_w_same_name = this.materials.FindAll(x => x.Name == _new_material.Name);
                if (materials_w_same_name != null && materials_w_same_name.Count > 0)
                    return false;
            }

            this.materials.Add(_new_material);
            return true;
        }

        private bool RemoveMaterial(Material _material)
        {
            if (_material == null) return false;

            return this.materials.Remove(_material);
        }    

        /// <summary>
        /// Binds the material to the component representation, if found. If not, it calls the CreateMaterial method of CompRepAlignedWith.
        /// </summary>
        /// <param name="_cra"></param>
        /// <returns></returns>
        public bool AddMaterial(CompRepAlignedWith _cra)
        {
            if (_cra == null) return false;
            
            Material m = this.materials.FirstOrDefault(x => x.Name == _cra.Comp_Description);
            if (m != null)
            {
                m.BoundCR = _cra;
                return true;
            }

            this.materials.Add(_cra.CreateMaterial());
            return true;
        }

        /// <summary>
        /// <para>Call both for realized and not realized components.</para>
        /// </summary>
        /// <param name="_cra"></param>
        /// <returns></returns>
        public bool ReAssociate(CompRepAlignedWith _cra)
        {
            // look for the corresponding material first
            Material found = this.materials.FirstOrDefault(x => x.BoundCRID == _cra.Comp_ID);
            if (found != null)
            {
                // restore the binding
                found.BoundCR = _cra;
                return true;
            }
            else
            {
                // create a new material
                return this.AddMaterial(_cra);
            }
        }
        #endregion

        #region INFO

        internal Material FindByID(long _id)
        {
            Material found = this.materials.FirstOrDefault(x => x.ID == _id);
            return found;
        }

        #endregion

        #region MANAGEMENT: Parsing

        internal Material ReconstructMaterial(long _id, string _name, float _thickness, MaterialPosToWallAxisPlane _pos, float _accA, int _nr_surf, bool _is_bound2cr, long _bound_crid)
        {            
            Material m_duplicate = this.materials.FirstOrDefault(x => x.ID == _id);
            if (m_duplicate != null) return null;

            Material m = new Material(_id, _name, _thickness, _pos, _accA, _nr_surf, _is_bound2cr, _bound_crid);
            this.materials.Add(m);
            Material.NR_MATERIALS = this.materials.Select(x => x.ID).Max() + 1;
            return m;
        }

        internal void ResetMaterialCounter()
        {
            Material.NR_MATERIALS = (this.materials == null) ? 0 : (this.materials.Count);
        }

        #endregion

        #region SAVING as DXF

        public StringBuilder ExportMaterials(bool _finalize)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            sb.AppendLine(DXFUtils.SECTION_START);                                  // SECTION
            sb.AppendLine(((int)EntitySaveCode.ENTITY_NAME).ToString());            // 2
            sb.AppendLine(DXFUtils.MATERIAL_SECTION);                               // MATERIALS

            foreach(Material m in this.materials)
            {
                m.AddToExport(ref sb);
            }

            sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());           // 0
            sb.AppendLine(DXFUtils.SECTION_END);                                    // ENDSEC

            if (_finalize)
            {
                sb.AppendLine(((int)EntitySaveCode.ENTITY_START).ToString());       // 0
                sb.AppendLine(DXFUtils.EOF);                                        // EOF
            }

            return sb;
        }

        #endregion
    }
}
