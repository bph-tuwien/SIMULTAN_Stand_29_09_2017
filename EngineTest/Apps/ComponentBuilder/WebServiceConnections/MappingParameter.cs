using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

using ParameterStructure.Geometry;
using ParameterStructure.Parameter;
using ParameterStructure.Component;

namespace ComponentBuilder.WebServiceConnections
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================= ENUMS, UTILS ============================================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region Error Handling
    public enum MappingError
    {
        NONE = 0,
        MISSING_MAPPING_END = 1,
        TOO_FEW_PARAMETERS_FOR_TYPE = 2,
        PARAM_TO_TYPE_MISMATCH = 3,
        COMPONENT_TO_TYPE_MISMATCH = 4,
        NO_MAPPING_TO_ID_ALLOWED = 5,
        INSTANTIATION_OF_TYPE_IMPOSSIBLE = 6,
        ID_DUPLICATION = 7
    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================== BASE TYPE: ABSTRACT MAPPING (MappingObject) ============================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // --------------------------------------------- BASE TYPE ------------------------------------------------ //

    #region ABSTRACT BASE
    public abstract class MappingObject
    {

        #region STATIC

        public static bool StringsCouldMeanTheSame(string _s1, string _s2)
        {
            if (string.IsNullOrEmpty(_s1) || string.IsNullOrEmpty(_s2)) return false;

            string s1_lc = _s1.ToLowerInvariant();
            string s1_UC = _s1.ToUpperInvariant();

            string s2_lc = _s2.ToLowerInvariant();
            string s2_UC = _s2.ToUpperInvariant();

            if (_s1.Contains(_s2) || _s1.Contains(s2_lc) || _s1.Contains(s2_UC)) return true;
            if (s1_lc.Contains(_s2) || s1_lc.Contains(s2_lc) || s1_lc.Contains(s2_UC)) return true;
            if (s1_UC.Contains(_s2) || s1_UC.Contains(s2_lc) || s1_UC.Contains(s2_UC)) return true;

            if (_s2.Contains(_s1) || _s2.Contains(s1_lc) || _s2.Contains(s1_UC)) return true;
            if (s2_lc.Contains(_s1) || s2_lc.Contains(s1_lc) || s2_lc.Contains(s1_UC)) return true;
            if (s2_UC.Contains(_s1) || s2_UC.Contains(s1_lc) || s2_UC.Contains(s1_UC)) return true;

            return false;
        }

        #endregion

        public TypeNode MappedToType { get; protected set; }
        public Component DirectParent { get; private set; }

        /// <summary>
        /// If true, the mapping can be used as a template for structurally equivalent elements.
        /// </summary>
        public bool IsMappedAsExample { get; protected set; }

        public virtual bool IsHighLighted { get; set; }

        protected MappingObject(Component _direct_parent, TypeNode _tn, bool _as_example)
        {
            this.DirectParent = _direct_parent;
            this.MappedToType = _tn;
            this.IsMappedAsExample = _as_example;
        }

        public virtual bool CanBeAppliedTo(object _o)
        {
            if (_o == null) return false;
            if (!(this.IsMappedAsExample)) return false;
            return true;
        }

        public virtual object InstantiateMapping()
        {
            return null;
        }
    }
    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ============================================ SPECIFIC MAPPINGS ========================================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // --------------------------------------- MAPPING OF A STRING -------------------------------------------- //

    #region MAPING OF STRINGS BOUND TO COMPONENT IDS

    /// <summary>
    /// Controls the mapping to a string id.
    /// </summary>
    public class MappingString : MappingObject
    {
        #region STATIC

        public static bool PreliminaryMapping(Component _source, string _input, TypeNode _tn, bool _as_example, out MappingError err)
        {
            err = MappingError.NONE;
            if (_source == null || string.IsNullOrEmpty(_input) || _tn == null)
            {
                err = MappingError.MISSING_MAPPING_END;
                return false;
            }
            // strings can be mapped only to ids
            if (_tn.BindingType != TypeNodeContentBindingType.KEY)
            {
                err = MappingError.PARAM_TO_TYPE_MISMATCH;
                return false;
            }

            return true;
        }

        public static MappingString Create(Component _source, string _input, TypeNode _tn, bool _as_example, out MappingError err)
        {
            if (!MappingString.PreliminaryMapping(_source, _input, _tn, _as_example, out err))
                return null;

            MappingString ms = new MappingString(_source, _input, _tn, _as_example);
            _tn.MostRecentMapping = ms;
            return ms;
        }

        #endregion

        public string MappedStringContent { get; private set; }

        protected MappingString(Component _source, string _input, TypeNode _tn, bool _as_example)
            :base(_source, _tn, _as_example)
        {
            this.MappedStringContent = _input;
        }

        public override object InstantiateMapping()
        {
            this.MappedToType.InstantiationInput = new object[] { this.MappedStringContent };
            return this.MappedToType.CreateInstance();
        }

    }

    #endregion

    // ---------------------------------- MAPPING OF SINGLE PARAMETER ----------------------------------------- //

    #region MAPPING OF PARAMETERS
    /// <summary>
    /// Controls the mapping to elementary types (e.g. int)
    /// </summary>
    public class MappingParameter : MappingObject
    {
        #region STATIC

        public static bool PerliminaryMapping(Parameter _p, Component _direct_parent, TypeNode _tn, bool _as_example, bool _match_name, out MappingError err)
        {
            err = MappingError.NONE;
            if (_p == null || _direct_parent  == null || _tn == null)
            {
                err = MappingError.MISSING_MAPPING_END;
                return false;
            }
            // parameters can be mapped only to simple types (e.g. int)
            if (_tn.BindingType != TypeNodeContentBindingType.SIMPLE)
            {
                err = MappingError.PARAM_TO_TYPE_MISMATCH;
                return false;
            }

            if (_match_name)
                return (MappingObject.StringsCouldMeanTheSame(_p.Name, _tn.Label));
            else
                return true;
        }


        public static MappingParameter Create(Parameter _p, Component _direct_parent, TypeNode _tn, bool _as_example, bool _match_name, out MappingError err)
        {
            if (!(MappingParameter.PerliminaryMapping(_p, _direct_parent, _tn, _as_example, _match_name, out err))) 
                return null;

            MappingParameter mp = new MappingParameter(_p, _direct_parent, _tn, _as_example);
            _tn.MostRecentMapping = mp;
            return mp;
        }

        #endregion

        
        public Parameter MappedParameter { get; private set; }

        protected MappingParameter(Parameter _p, Component _direct_parent, TypeNode _tn, bool _as_example)
            : base(_direct_parent, _tn, _as_example)
        {
            this.MappedParameter = _p;
        }

        #region OVERRIDES: CanBeAppliedTo

        /// <summary>
        /// If this mapping can be applied as an example, it checks for name and unit match.
        /// </summary>
        /// <param name="_o"></param>
        /// <returns></returns>
        public override bool CanBeAppliedTo(object _o)
        {
            if (_o == null) return false;
            if (!(_o is Parameter)) return false;
            if (!(this.IsMappedAsExample)) return false;

            Parameter p = _o as Parameter;
            return (this.MappedParameter.Name == p.Name && this.MappedParameter.Unit == p.Unit);
        }
        #endregion

        #region OVERRIDES: Instantiation

        public override object InstantiateMapping()
        {
            this.MappedToType.InstantiationInput = new object[] { this.MappedParameter.ValueCurrent };
            return this.MappedToType.CreateInstance();
        }

        #endregion
    }
    #endregion

    // --------------------------- MAPPING OF SINGLE GEOMETRIC RELATIONSHIP ----------------------------------- //

    #region MAPPING OF A POINT

    public class MappingSinglePoint : MappingObject
    {
        #region STATIC CREATE

        public static Dictionary<string, TypeNode> PreliminaryMapping(Point3DContainer _point_container, Component _direct_parent, TypeNode _tn, bool _as_example, out MappingError err)
        {
            err = MappingError.NONE;
            if (_point_container == null || _direct_parent == null || _tn == null)
            {
                err = MappingError.MISSING_MAPPING_END;
                return null;
            }

            // check if the mapping is possible...
            if (_tn.SubNodes == null)
            {
                err = MappingError.TOO_FEW_PARAMETERS_FOR_TYPE;
                return null;
            }
            if (_tn.BindingType == TypeNodeContentBindingType.NOT_BINDABLE)
            {
                err = MappingError.COMPONENT_TO_TYPE_MISMATCH;
                return null;
            }
            if (_tn.BindingType == TypeNodeContentBindingType.KEY)
            {
                err = MappingError.NO_MAPPING_TO_ID_ALLOWED;
                return null;
            }

            if (_tn.SubNodes.Count != 3)
            {
                err = MappingError.INSTANTIATION_OF_TYPE_IMPOSSIBLE;
                return null;
            }

            // attempt a preliminary mapping of coordinates
            Dictionary<string, TypeNode> coords_mapping = new Dictionary<string, TypeNode>()
            {
                {"x", null},
                {"y", null},
                {"z", null}
            };

            foreach (TypeNode stn in _tn.SubNodes)
            {
                if (stn.BindingType != TypeNodeContentBindingType.SIMPLE)
                {
                    err = MappingError.TOO_FEW_PARAMETERS_FOR_TYPE;
                    return null;
                }
                if (coords_mapping["x"] == null && MappingObject.StringsCouldMeanTheSame("x", stn.Label))
                    coords_mapping["x"] = stn;
                else if (coords_mapping["y"] == null && MappingObject.StringsCouldMeanTheSame("y", stn.Label))
                    coords_mapping["y"] = stn;
                else if (coords_mapping["z"] == null && MappingObject.StringsCouldMeanTheSame("z", stn.Label))
                    coords_mapping["z"] = stn;
            }

            return coords_mapping;
        }

        public static MappingSinglePoint Create(Point3DContainer _point_container, Component _direct_parent, TypeNode _tn, bool _as_example, out MappingError err)
        {
            // attempt a preliminary mapping of coordinates
            Dictionary<string, TypeNode> coords_mapping = MappingSinglePoint.PreliminaryMapping(_point_container, _direct_parent, _tn, _as_example, out err);
            if (err != MappingError.NONE) return null;

            // perform actual mapping
            if (coords_mapping["x"] != null && coords_mapping["y"] != null && coords_mapping["z"] != null)
            {
                MappingSinglePoint msp = new MappingSinglePoint(_point_container, _direct_parent, _tn, _as_example);
                msp.coords_mapping = coords_mapping;
                _tn.MostRecentMapping = msp;
                return msp;
            }
            else
            {
                err = MappingError.INSTANTIATION_OF_TYPE_IMPOSSIBLE;
                return null;
            }
        }

        #endregion

        public Point3DContainer MappedPointC { get; private set; }

        protected Dictionary<string, TypeNode> coords_mapping;
        public ReadOnlyDictionary<string, TypeNode> CoordsMapping { get { return new ReadOnlyDictionary<string, TypeNode>(this.coords_mapping); } }

        protected MappingSinglePoint(Point3DContainer _point_container, Component _direct_parent, TypeNode _tn, bool _as_example)
            : base(_direct_parent, _tn, _as_example)
        {
            this.MappedPointC = _point_container;
            this.coords_mapping = new Dictionary<string, TypeNode>
            {
                {"x", null},
                {"y", null},
                {"z", null}
            };
        }

        public override object InstantiateMapping()
        {
            this.coords_mapping["x"].InstantiationInput = new object[] { ((Point3D)this.MappedPointC.Content).X };
            this.coords_mapping["y"].InstantiationInput = new object[] { ((Point3D)this.MappedPointC.Content).Y };
            this.coords_mapping["z"].InstantiationInput = new object[] { ((Point3D)this.MappedPointC.Content).Z };

            List<object> constr_params = new List<object>();
            foreach(TypeNode sN in this.MappedToType.SubNodes)
            {
                object instance = sN.CreateInstance();
                if (instance == null) return null;
                constr_params.Add(instance);
            }

            this.MappedToType.InstantiationInput = constr_params.ToArray();
            return this.MappedToType.CreateInstance();
        }

    }

    #endregion

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // ================================= SPECIFIC MAPPINGS OF MULTIPLE VALUES ================================= //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // ----------------------- MAPPING OF A COMPONENT: SIMPLE (subcomponents considered)----------------------- //

    #region Mapping of Components SIMPLE
    /// <summary>
    /// Controls the mapping of a component, including its sub-components, parameters and geometric relationships
    /// </summary>
    public static class MappingComponent
    {
        #region STATIC: Structure matching test

        public const string PREFIX_COMP = "comp_";
        public const string PREFIX_PARAM = "param_";
        public const string PREFIX_P3DC = "p3Dc_";
        public const string PREFIX_ID = "id_";

        /// <summary>
        /// This method accepts both a partial and a full matching.
        /// The matching rules are: 1. Components can match only COMPLEX TypeNodes. 2. Parameters can match only SIMPLE TypeNodes.
        /// 3. GeometricRelationshipscan only match a NON_BINDABLE TypeNode with exactly one SubNode of COMPLEX type.
        /// </summary>
        public static void MatchStructure(Component _comp, TypeNode _tn, ref List<KeyValuePair<string, TypeNode>> correspondencies, out MappingError err)
        {
            if (correspondencies == null)
                correspondencies = new List<KeyValuePair<string, TypeNode>>();
            err = MappingError.NONE;

            if (_comp == null || _tn == null)
            {
                err = MappingError.MISSING_MAPPING_END;
                return;
            }

            if (_tn.BindingType == TypeNodeContentBindingType.NOT_BINDABLE || 
                _tn.BindingType == TypeNodeContentBindingType.SIMPLE)
            {
                err = MappingError.COMPONENT_TO_TYPE_MISMATCH;
                return;
            }
            if (_tn.BindingType == TypeNodeContentBindingType.KEY)
            {
                err = MappingError.NO_MAPPING_TO_ID_ALLOWED;
                return;
            }

            if (correspondencies.Where(x => x.Key == MappingComponent.PREFIX_COMP + _comp.ID.ToString()).Count() > 0)
            {
                err = MappingError.ID_DUPLICATION;
                return;
            }

            // at least top-level matching is possible
            List<KeyValuePair<string, TypeNode>> new_correspondencies = new List<KeyValuePair<string, TypeNode>>();
            new_correspondencies.Add(new KeyValuePair<string, TypeNode>(MappingComponent.PREFIX_COMP + _comp.ID.ToString(), _tn));
            bool transferred_id = false;

            if (_tn.SubNodes == null || _tn.SubNodes.Count == 0)
                return;

            // attempt matching of structure
            foreach(TypeNode sN in _tn.SubNodes)
            {
                if (sN.BindingType == TypeNodeContentBindingType.SIMPLE)
                {
                    // look for an existing mapping
                    bool found_existing = false;
                    if (sN.AllMappings != null && sN.AllMappings.Count > 0)
                    {
                        MappingObject mo = sN.AllMappings.FirstOrDefault(x => (x is MappingParameter) && _comp.ContainedParameters.ContainsKey((x as MappingParameter).MappedParameter.ID));
                        if (mo != null)
                        {
                            string p_key = MappingComponent.PREFIX_PARAM + (mo as MappingParameter).MappedParameter.ID.ToString();
                            if (!(correspondencies.Where(x => x.Key == p_key).Count() > 0) && !(new_correspondencies.Where(x => x.Key == p_key).Count() > 0))
                                new_correspondencies.Add(new KeyValuePair<string, TypeNode>(p_key, sN));
                            found_existing = true;
                        }
                    }
                    // try to match it to a parameter
                    if (!found_existing && _comp.ContainedParameters.Count() > 0)
                    {
                        Parameter p = _comp.ContainedParameters.FirstOrDefault(x => x.Value != null && MappingObject.StringsCouldMeanTheSame(x.Value.Name, sN.Label)).Value;
                        if (p != null)
                        {
                            string p_key = MappingComponent.PREFIX_PARAM + p.ID.ToString();
                            if (!(correspondencies.Where(x => x.Key == p_key).Count() > 0) && !(new_correspondencies.Where(x => x.Key == p_key).Count() > 0))
                                new_correspondencies.Add(new KeyValuePair<string, TypeNode>(p_key, sN));
                        }                       
                    }
                }                
                else if (sN.BindingType == TypeNodeContentBindingType.NOT_BINDABLE)
                {
                    // try to match it to a geometric relationship
                    if (sN.IsEnumerable && sN.SubNodes != null && sN.SubNodes.Count == 1)
                    {
                        TypeNode sN_1 = sN.SubNodes[0];
                        if (sN_1.BindingType == TypeNodeContentBindingType.COMPLEX)
                        {
                            bool match_w_geometry_found = false;
                            foreach (GeometricRelationship gr in _comp.R2GInstances)
                            {
                                // get the path points as identifiable containers
                                List<HierarchicalContainer> geometry = gr.GeometricContent;
                                if (geometry.Count == 0) continue;

                                foreach(HierarchicalContainer hc in geometry)
                                {
                                    Point3DContainer p3dc = hc as Point3DContainer;
                                    if (p3dc == null) continue;

                                    // CHECK IF A MAPPING IS AT ALL POSSIBLE
                                    MappingError sN_1_test_err = MappingError.NONE;
                                    MappingSinglePoint.PreliminaryMapping(p3dc, _comp, sN_1, false, out sN_1_test_err);
                                    if (sN_1_test_err != MappingError.NONE)
                                        break;

                                    // look for an existing mapping
                                    bool found_existing = false;
                                    if (sN_1.AllMappings != null && sN_1.AllMappings.Count > 0)
                                    {
                                        MappingObject mo = sN_1.AllMappings.FirstOrDefault(x => (x is MappingSinglePoint) &&
                                            (x as MappingSinglePoint).MappedPointC.ID_primary == p3dc.ID_primary && (x as MappingSinglePoint).MappedPointC.ID_secondary == p3dc.ID_secondary);
                                        if (mo != null)
                                        {
                                            string p_key = MappingComponent.PREFIX_P3DC + (mo as MappingSinglePoint).MappedPointC.ID_primary.ToString() +
                                                                                    "_" + (mo as MappingSinglePoint).MappedPointC.ID_secondary.ToString();
                                            if (!(correspondencies.Where(x => x.Key == p_key).Count() > 0) && !(new_correspondencies.Where(x => x.Key == p_key).Count() > 0))
                                                new_correspondencies.Add(new KeyValuePair<string, TypeNode>(p_key, sN_1));
                                            found_existing = true;
                                            match_w_geometry_found = true;
                                        }
                                    }
                                    // try to create a new mapping
                                    if (!found_existing)
                                    {
                                        MappingError sN_1_m_err = MappingError.NONE;
                                        MappingSinglePoint.PreliminaryMapping(p3dc, _comp, sN_1, false, out sN_1_m_err);
                                        if (sN_1_m_err == MappingError.NONE)
                                        {
                                            // mapping successful
                                            string p_key = MappingComponent.PREFIX_P3DC + p3dc.ID_primary.ToString() +
                                                                                    "_" + p3dc.ID_secondary.ToString();
                                            if (!(correspondencies.Where(x => x.Key == p_key).Count() > 0) && !(new_correspondencies.Where(x => x.Key == p_key).Count() > 0))
                                                new_correspondencies.Add(new KeyValuePair<string, TypeNode>(p_key, sN_1));
                                            match_w_geometry_found = true;
                                        }
                                    }
                                }
                            }

                            if (!match_w_geometry_found)
                            {
                                // try to match it to a sub-component -> RECURSION
                                if (sN.SubNodes != null && sN.SubNodes.Count > 0)
                                {
                                    foreach (TypeNode ssN in sN.SubNodes)
                                    {
                                        int nr_current_correspondencies = correspondencies.Count;
                                        // attempt a matching
                                        foreach (var entry in _comp.ContainedComponents)
                                        {
                                            Component sC = entry.Value;
                                            if (sC == null) continue;

                                            MappingError sC_err = MappingError.NONE;
                                            MappingComponent.MatchStructure(sC, ssN, ref correspondencies, out sC_err);
                                            if (sC_err == MappingError.NONE && correspondencies.Count > nr_current_correspondencies)
                                            {
                                                // success
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }                        
                    }
                }
                else if (sN.BindingType == TypeNodeContentBindingType.KEY)
                {
                    // pass the ID of the component
                    string key_id = MappingComponent.PREFIX_ID + _comp.ID.ToString();
                    if (!(correspondencies.Where(x => x.Key == key_id).Count() > 0) && !(new_correspondencies.Where(x => x.Key == key_id).Count() > 0))
                    {
                        new_correspondencies.Add(new KeyValuePair<string, TypeNode>(key_id, sN));
                        transferred_id = true;
                    }
                }
                else if (sN.BindingType == TypeNodeContentBindingType.COMPLEX)
                {
                    // try to match it to a sub-component -> RECURSION
                    if (sN.SubNodes != null && sN.SubNodes.Count > 0)
                    {                        
                        foreach (TypeNode ssN in sN.SubNodes)
                        {
                            int nr_current_correspondencies = correspondencies.Count;
                            // attempt a matching
                            foreach(var entry in _comp.ContainedComponents)
                            {
                                Component sC = entry.Value;
                                if (sC == null) continue;

                                MappingError sC_err = MappingError.NONE;
                                MappingComponent.MatchStructure(sC, ssN, ref correspondencies, out sC_err);
                                if (sC_err == MappingError.NONE && correspondencies.Count > nr_current_correspondencies)
                                {
                                    // success
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if ( (new_correspondencies.Count < 2) || (new_correspondencies.Count < 3 && transferred_id))
            {
                // only a match at the highest level -> not enough -> do not add
            }
            else
            {
                // the method traverses the component from BOTTOM to TOP
                // the mapping is performed TOP to BOTTOM -> insert at the start
                for (int i = 0; i < new_correspondencies.Count; i++ )
                {
                    var entry = new_correspondencies[i];
                    correspondencies.Insert(i, new KeyValuePair<string, TypeNode>(entry.Key, entry.Value));
                }
            }
        }


        #endregion

        #region STATIC: Create

        public static List<MappingObject> CreateMultipleFrom(Component _comp, TypeNode _tn, bool _as_example, List<KeyValuePair<string, TypeNode>> _correspondencies, out MappingError err)
        {
            err = MappingError.NONE;
            List<MappingObject> created_mappings = new List<MappingObject>();
            if (_comp == null || _tn == null || _correspondencies == null || _correspondencies.Count < 2)
            {
                err = MappingError.MISSING_MAPPING_END;
                return created_mappings;
            }

            Component current_comp = _comp;
            MappingError tmp_err = MappingError.NONE;
            foreach(var entry in _correspondencies)
            {
                string[] key_comps = entry.Key.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                if (key_comps == null || key_comps.Length < 2)
                {
                    err = MappingError.MISSING_MAPPING_END;
                    return created_mappings;
                }
                
                if ((key_comps[0] + "_") == MappingComponent.PREFIX_COMP)
                {
                    // -------------------------- COMPONENTS --------------------------- //
                    long comp_id = -1;
                    bool success = long.TryParse(key_comps[1], out comp_id);
                    if (!success)
                    {
                        err = MappingError.MISSING_MAPPING_END;
                        return created_mappings;
                    }

                    if (comp_id != current_comp.ID)
                    {
                        // look for the component in the children of current_comp
                        List<Component> all_sub_comps = current_comp.GetFlatSubCompList();
                        Component next = all_sub_comps.FirstOrDefault(x => x.ID == comp_id);
                        if (next  == null)
                        {
                            err = MappingError.MISSING_MAPPING_END;
                            return created_mappings;
                        }
                        else
                        {
                            current_comp = next;
                        }
                    }
                }
                else if ((key_comps[0] + "_") == MappingComponent.PREFIX_ID)
                {
                    // ------------------------ COMPONENT IDS -------------------------- //
                    MappingString ms = MappingString.Create(current_comp, entry.Key, entry.Value, _as_example, out tmp_err);
                    if (tmp_err != MappingError.NONE)
                    {
                        err = tmp_err;
                        return created_mappings;
                    }
                    else
                    {
                        created_mappings.Add(ms);
                    }
                }
                else if ((key_comps[0] + "_") == MappingComponent.PREFIX_PARAM)
                {
                    // -------------------------- PARAMETERS --------------------------- //
                    long param_id = -1;
                    bool success = long.TryParse(key_comps[1], out param_id);
                    if (!success || !current_comp.ContainedParameters.ContainsKey(param_id))
                    {
                        err = MappingError.MISSING_MAPPING_END;
                        return created_mappings;
                    }
                    Parameter p = current_comp.ContainedParameters[param_id];
                    MappingParameter mp = MappingParameter.Create(p, current_comp, entry.Value, _as_example, false, out tmp_err);
                    if (tmp_err != MappingError.NONE)
                    {
                        err = tmp_err;
                        return created_mappings;
                    }
                    else
                    {
                        created_mappings.Add(mp);
                    }
                }
                else if ((key_comps[0] + "_") == MappingComponent.PREFIX_P3DC)
                {
                    // --------------------------- GEOMETRY ---------------------------- //
                    if (key_comps.Length < 3)
                    {
                        err = MappingError.MISSING_MAPPING_END;
                        return created_mappings;
                    }
                    long id_primary = -1;
                    int id_second = -1;
                    bool success_1 = long.TryParse(key_comps[1], out id_primary);
                    bool success_2 = int.TryParse(key_comps[2], out id_second);
                    if (!success_1 || !success_2)
                    {
                        err = MappingError.MISSING_MAPPING_END;
                        return created_mappings;
                    }
                    GeometricRelationship gr = current_comp.R2GInstances.FirstOrDefault(x => x.ID == id_primary);
                    if (gr == null)
                    {
                        err = MappingError.MISSING_MAPPING_END;
                        return created_mappings;
                    }
                    Point3DContainer p3dc = gr.GeometricContent.FirstOrDefault(x => x.ID_primary == id_primary && x.ID_secondary == id_second) as Point3DContainer;
                    if (p3dc == null)
                    {
                        err = MappingError.MISSING_MAPPING_END;
                        return created_mappings;
                    }

                    MappingSinglePoint msp = MappingSinglePoint.Create(p3dc, current_comp, entry.Value, _as_example, out tmp_err);
                    if (tmp_err != MappingError.NONE)
                    {
                        err = tmp_err;
                        return created_mappings;
                    }
                    else
                    {
                        created_mappings.Add(msp);
                    }
                }

            }

            return created_mappings;
        }

        #endregion

    }
    #endregion
}
