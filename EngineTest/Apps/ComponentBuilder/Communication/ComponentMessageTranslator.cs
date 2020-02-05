using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using ParameterStructure.Component;
using ParameterStructure.Parameter;
using InterProcCommunication;
using InterProcCommunication.Specific;

namespace ComponentBuilder.Communication
{
    public static class ComponentMessageTranslator
    {
        public static ComponentMessage AssembleComponentMessage(Component _comp, MessagePositionInSeq _pos, long _parent_id)
        {
            if (_comp == null) return null;
            if (_comp.R2GInstances == null || _comp.R2GInstances.Count == 0) return null;

            List<ComponentMessage> messages = new List<ComponentMessage>();
            
            // translate Pt.1
            // translate the geometry type(s)
            Relation2GeomType gr_type_main = Relation2GeomType.NONE;
            List<InterProcCommunication.Specific.GeometricRelationship> translated_geom_relationships = new List<InterProcCommunication.Specific.GeometricRelationship>();
            foreach(ParameterStructure.Geometry.GeometricRelationship gr in _comp.R2GInstances)
            {
                Relation2GeomType gr_type = InterProcCommunication.Specific.GeometryUtils.StringToRelationship2Geometry(ParameterStructure.Geometry.GeometryUtils.Relationship2GeometryToString(gr.State.Type));
                if (gr_type_main == Relation2GeomType.NONE)
                    gr_type_main = gr_type;
                Relation2GeomState gr_state = new Relation2GeomState { Type = gr_type, IsRealized = gr.State.IsRealized };
                InterProcCommunication.Specific.GeometricRelationship gr_translated = new InterProcCommunication.Specific.GeometricRelationship(gr.ID, gr.Name, gr_state, gr.GeomIDs, gr.GeomCS, gr.TRm_WC2LC, gr.TRm_LC2WC, 
                                                                                                                                                gr.InstanceSize, gr.InstanceNWElementID, gr.InstanceNWElementName, gr.InstancePath);
                translated_geom_relationships.Add(gr_translated);
            }

            // translate Pt.2
            // assemble the parameter dictionary according to geometry relationship type
            Dictionary<string, double> guide = Comp2GeomCommunication.GetReservedParamDictionary(gr_type_main);
            Dictionary<string, double> p_dict = Comp2GeomCommunication.GetReservedParamDictionary(gr_type_main);
            foreach (var entry in guide)
            {
                Parameter p = _comp.ContainedParameters.Values.FirstOrDefault(x => x.Name == entry.Key);
                if (p != null)
                    p_dict[entry.Key] = p.ValueCurrent;
            }

            // done
            return new ComponentMessage(_pos, _parent_id, _comp.ID, _comp.IsAutomaticallyGenerated, _comp.CurrentSlot + " " + _comp.Name + " " + _comp.Description, 
                                        _comp.GetIdsOfAllReferencedComponents() ,p_dict, translated_geom_relationships, -1L, -1L, MessageAction.NONE);       
        }

        public static List<ComponentMessage> AssembleMultipleComponentMessages(Component _comp_main)
        {
            List<ComponentMessage> messages = new List<ComponentMessage>();
            if (_comp_main == null) return messages;

            // get all subcomponents w geometric relationships and translate them (recursive)
            Dictionary<Component, long> relevant_subcomps = _comp_main.GetSubcompsWGeometricRelationships();
            int nr_subcomps = relevant_subcomps.Count;

            // translate the main component (set the parent as -1, event if it exists -> has no effect on return)
            ComponentMessage cmsg_main;
            if (nr_subcomps == 0)                
                cmsg_main = AssembleComponentMessage(_comp_main, MessagePositionInSeq.SINGLE_MESSAGE, -1);
            else
                cmsg_main = AssembleComponentMessage(_comp_main, MessagePositionInSeq.SEQUENCE_START_MESSAGE, -1);
            messages.Add(cmsg_main);

            for (int i = 0; i < nr_subcomps; i++ )
            {
                MessagePositionInSeq pos = MessagePositionInSeq.MESSAGE_INSIDE_SEQUENCE;
                if (i == nr_subcomps - 1)
                    pos = MessagePositionInSeq.SEQUENCE_END_MESSAGE;

                ComponentMessage cmsg_i = AssembleComponentMessage(relevant_subcomps.ElementAt(i).Key, pos, relevant_subcomps.ElementAt(i).Value);
                messages.Add(cmsg_i);
            }

            return messages;
        }

        public static List<ComponentMessage> AssembleUnrelatedComponentMessages(List<Component> _comps, ComponentFactory _factory)
        {
            List<ComponentMessage> messages = new List<ComponentMessage>();
            if (_comps == null) return messages;

            // get the parent ids
            List<long> parent_ids = new List<long>();
            if (_factory != null)
            {
                foreach(Component c in _comps)
                {
                    long c_parent_id = -1L;
                    List<Component> parent_chain = _factory.GetParentComponentChain(c);
                    if (parent_chain.Count > 1)
                    {
                        Component direct_parent = parent_chain[parent_chain.Count - 2];
                        if (direct_parent != null)
                            c_parent_id = direct_parent.ID;
                    }
                    parent_ids.Add(c_parent_id);                        
                }
            }
            else
            {
                parent_ids = Enumerable.Repeat(-1L, _comps.Count).ToList();
            }

            // assemble the messages
            int nrM = _comps.Count;
            if (nrM == 1)
                messages.Add(ComponentMessageTranslator.AssembleComponentMessage(_comps[0], MessagePositionInSeq.SINGLE_MESSAGE, parent_ids[0]));
            else
            {
                for (int i = 0; i < nrM; i++)
                {
                    MessagePositionInSeq pos = MessagePositionInSeq.UNKNOWN;
                    if (i == 0)
                        pos = MessagePositionInSeq.SEQUENCE_START_MESSAGE;
                    else if (i == nrM - 1)
                        pos = MessagePositionInSeq.SEQUENCE_END_MESSAGE;
                    else
                        pos = MessagePositionInSeq.MESSAGE_INSIDE_SEQUENCE;

                    messages.Add(ComponentMessageTranslator.AssembleComponentMessage(_comps[i], pos, parent_ids[i]));
                }
            }

            return messages;
        }

        public static void TranslateIntoComponents(List<ComponentMessage> _messages, ComponentFactory _comp_factory, 
                                                    out List<ParameterStructure.Component.Component> new_comps_created)
        {
            new_comps_created = new List<ParameterStructure.Component.Component>();
            if (_messages == null) return;
            if (_messages.Count == 0) return;
            if (_comp_factory == null) return;

            // 1. Modify the components
            Dictionary<Component, ComponentAction> updated = new Dictionary<Component, ComponentAction>();
            foreach(ComponentMessage msg in _messages)
            {
                ParameterStructure.Component.Component comp = null;
                ComponentAction action_to_take = ComponentAction.NONE;
                if (msg.CompID > -1)
                {
                    // handling of existing components
                    comp = _comp_factory.GetByID(msg.CompID);
                    action_to_take = (msg.ActionToTake == MessageAction.DELETE) ? ComponentAction.DELETE : ComponentAction.UPDATE;
                }
                else
                {
                    Debug.WriteLine("CB: creating new component from {0}", msg.CompDescr);
                    // create a new component (adds it to the top level of the component record of the Factory)
                    // NOTE: should happen only for components describing a space
                    comp = _comp_factory.CreateEmptyComponent(false);
                    new_comps_created.Add(comp);
                    ////comp.Name = GeometryUtils.Relationship2GeometryToCompNameDE(msg.GeomType);
                    comp.Name = msg.CompDescr;
                    comp.Description = "Representation";
                    action_to_take = ComponentAction.CREATE;
                }
                if (comp != null)
                {
                    Debug.WriteLine("CB: calling 'UpdateComponentFromMessage' with {0} for {1}", action_to_take, msg.CompDescr);
                    ComponentMessageTranslator.UpdateComponentFromMessage(comp, msg, _comp_factory.Caller, true);
                    _comp_factory.UpdateConnectivity(comp); // for Relation2Geometry Type CONTAINED_IN -> propagates realization
                    if (!(updated.ContainsKey(comp)))
                        updated.Add(comp, action_to_take);
                }
            }

            // 2. Adjust the relationships btw the components
            // NOTE: includes deletion of all automatically generated sub-components and 
            // replacing them with the new automatically created components

            List<long> parent_ids = new List<long>();
            foreach(ComponentMessage msg in _messages)
            {
                if (msg.CompParentID < 0 && msg.CompRepParentID > -1)
                {
                    // the parent was just generated...
                    int index = _messages.FindIndex(x => x.CompRepID == msg.CompRepParentID);
                    long new_parent_id = updated.ElementAt(index).Key.ID;
                    parent_ids.Add(new_parent_id);
                }
                else
                {
                    // the parent existed already before the call to this method OR
                    // there is no parent
                    parent_ids.Add(msg.CompParentID);
                }
            }
            
            List<List<long>> ref_comp_ids = _messages.Select(x => new List<long>(x.CompRefIds)).ToList();
            
            // happens independent of the current user (references have a higher priority than writing access)
            _comp_factory.AdjustSubAndRefComponentDependenciesAfterAutomaticGeneration(updated, parent_ids, ref_comp_ids);           
        }

        public static void UpdateComponentFromMessage(Component _comp, ComponentMessage _cmsg, 
                                                      ComponentManagerType _user, bool _add_missing_params)
        {
            if (_comp == null || _cmsg == null) return;
            if (_cmsg.CompID > -1 && _comp.ID != _cmsg.CompID) return;

            // 1. TRANSFER THE GEOMETRIC RELATIONSHIPS
            // extract the main geometric relationship type (even for multiple relationships, there should only be ONE type)
            List<InterProcCommunication.Specific.GeometricRelationship> geom_rels = new List<InterProcCommunication.Specific.GeometricRelationship>(_cmsg.GeomRelationships);
            if (geom_rels.Count == 0)
            {
                // ... Houston, we have a problem!!!
                // This should not happen!
            }
            else
            {
                // combine the incoming relationships with processing info
                Dictionary<InterProcCommunication.Specific.GeometricRelationship, bool> geom_rels_processing = new Dictionary<GeometricRelationship, bool>();                
                foreach (InterProcCommunication.Specific.GeometricRelationship gr in geom_rels)
                {
                    geom_rels_processing.Add(gr, false);
                }
                // combine the old relationships with updating info (is used to remove obsolete relationships)
                List<bool> old_geom_rels_updating = Enumerable.Repeat(false, _comp.R2GInstances.Count).ToList();
                
                // update the existing geometric relationship(s)
                for (int i = 0; i < _comp.R2GInstances.Count; i++ )
                {
                    InterProcCommunication.Specific.GeometricRelationship gr_corresponding = geom_rels.FirstOrDefault(x => x.GrID == _comp.R2GInstances[i].ID);
                    if (gr_corresponding != null)
                    {
                        ParameterStructure.Geometry.Relation2GeomType gr_type = ParameterStructure.Geometry.GeometryUtils.StringToRelationship2Geometry(InterProcCommunication.Specific.GeometryUtils.Relationship2GeometryToString(gr_corresponding.GrState.Type));                   
                        _comp.R2GInstances[i].Name = gr_corresponding.GrName;
                        _comp.R2GInstances[i].State = new ParameterStructure.Geometry.Relation2GeomState { IsRealized = gr_corresponding.GrState.IsRealized, Type = gr_type };
                        _comp.R2GInstances[i].GeomIDs = gr_corresponding.GrIds;
                        _comp.R2GInstances[i].GeomCS = gr_corresponding.GrUCS;
                        _comp.R2GInstances[i].TRm_WC2LC = gr_corresponding.GrTrWC2LC;
                        _comp.R2GInstances[i].TRm_LC2WC = gr_corresponding.GrTrLC2WC;

                        // instance info cannot change, except for the path
                        _comp.R2GInstances[i].InstancePath = new List<System.Windows.Media.Media3D.Point3D>(gr_corresponding.InstPath);

                        geom_rels_processing[gr_corresponding] = true;
                        old_geom_rels_updating[i] = true;
                    }
                }

                // add new relationships
                List<ParameterStructure.Geometry.GeometricRelationship> to_be_added = new List<ParameterStructure.Geometry.GeometricRelationship>();
                foreach(var entry in geom_rels_processing)
                {
                    if (entry.Value)
                        continue; // skip processed

                    ParameterStructure.Geometry.Relation2GeomType type = ParameterStructure.Geometry.GeometryUtils.StringToRelationship2Geometry(InterProcCommunication.Specific.GeometryUtils.Relationship2GeometryToString(entry.Key.GrState.Type));
                    ParameterStructure.Geometry.Relation2GeomState state = new ParameterStructure.Geometry.Relation2GeomState {Type = type, IsRealized = entry.Key.GrState.IsRealized};
                    ParameterStructure.Geometry.GeometricRelationship new_gr = new ParameterStructure.Geometry.GeometricRelationship(entry.Key.GrName, state,
                                                                                        entry.Key.GrIds, entry.Key.GrUCS, entry.Key.GrTrWC2LC, entry.Key.GrTrLC2WC);
                    new_gr.InstancePath = new List<System.Windows.Media.Media3D.Point3D>(entry.Key.InstPath); // added 06.09.2017
                    to_be_added.Add(new_gr);                    
                }

                // communicate to component (performs the deleletion of the relationships that were not updated)
                _comp.UpdateGeometricRelationships(old_geom_rels_updating, to_be_added, _user);
            }

            // 2. TRANSFER THE PARAMETERS
            Dictionary<string, double> p_dict = Comp2GeomCommunication.GetReservedParamDictionary(geom_rels[0].GrState.Type);
            foreach(var entry in p_dict)
            {
                // retrieve value
                double val = _cmsg[entry.Key];
                if (double.IsNaN(val)) continue;

                // transfer to component
                _comp.SetParameterValue(entry.Key, val, _user, _add_missing_params);
            }

        }

        public static void UpdateComponentReferences(List<ComponentMessage> _messages, ComponentFactory _comp_factory)
        {
            if (_messages == null) return;
            if (_messages.Count == 0) return;
            if (_comp_factory == null) return;

            Debug.WriteLine("CB: calling 'UpdateComponentReferences' for all {0} transferred components...", _messages.Count);
            List<long> comp_ids = _messages.Select(x => x.CompID).ToList();
            List<List<long>> ref_comp_ids = _messages.Select(x => new List<long>(x.CompRefIds)).ToList();

            _comp_factory.AdjustRefComponentDependencies(comp_ids, ref_comp_ids);
        }
    }
}
