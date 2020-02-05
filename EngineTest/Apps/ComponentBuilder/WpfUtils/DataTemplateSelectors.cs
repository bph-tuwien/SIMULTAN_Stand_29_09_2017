using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using ParameterStructure.Component;
using ParameterStructure.Parameter;

namespace ComponentBuilder.WpfUtils
{
    public class ComponentDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate( object item, DependencyObject container )
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;
            
            ParameterStructure.Component.Component component = item as ParameterStructure.Component.Component;
            ParameterStructure.Component.EmptyComponentSlot compslot = new EmptyComponentSlot { SlotType = 0, SlotName = string.Empty, SlotDescr = string.Empty};
            if (item is ParameterStructure.Component.EmptyComponentSlot)
                compslot = (ParameterStructure.Component.EmptyComponentSlot)item;
            ParameterStructure.Parameter.Parameter parameter = item as ParameterStructure.Parameter.Parameter;
            ParameterStructure.Parameter.Calculation calc = item as ParameterStructure.Parameter.Calculation;

            if (component != null)
            {
                if(component.IsHidden)
                    return element.TryFindResource("ComponentInListHidden") as DataTemplate;
                else
                    return element.TryFindResource("ComponentInList") as DataTemplate;
            }
            else if (!(string.IsNullOrEmpty(compslot.SlotName)))
            {
                if (compslot.IsLarge)
                    return element.TryFindResource("ComponentSlotInParamList") as DataTemplate;
                else
                    return element.TryFindResource("ComponentSlotInList") as DataTemplate;
            }
            else if (parameter != null)
                return element.TryFindResource("ParamInList") as DataTemplate;
            else if (calc != null)
                return element.TryFindResource("CalcInList") as DataTemplate;
            else
                return null;
        }
    }

    /// <summary>
    /// A copy of class ComponentDataTemplateSelector, with the exception of the handling of the parameter template.
    /// </summary>
    public class ComponentDataTemplateSelector_Inst : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            ParameterStructure.Component.Component component = item as ParameterStructure.Component.Component;
            ParameterStructure.Component.EmptyComponentSlot compslot = new EmptyComponentSlot { SlotType = 0, SlotName = string.Empty, SlotDescr = string.Empty };
            if (item is ParameterStructure.Component.EmptyComponentSlot)
                compslot = (ParameterStructure.Component.EmptyComponentSlot)item;
            ParameterStructure.Parameter.Parameter parameter = item as ParameterStructure.Parameter.Parameter;
            ParameterStructure.Parameter.Calculation calc = item as ParameterStructure.Parameter.Calculation;

            if (component != null)
            {
                if (component.IsHidden)
                    return element.TryFindResource("ComponentInListHidden") as DataTemplate;
                else
                    return element.TryFindResource("ComponentInList") as DataTemplate;
            }
            else if (!(string.IsNullOrEmpty(compslot.SlotName)))
            {
                if (compslot.IsLarge)
                    return element.TryFindResource("ComponentSlotInParamList") as DataTemplate;
                else
                    return element.TryFindResource("ComponentSlotInList") as DataTemplate;
            }
            else if (parameter != null)
                return element.TryFindResource("ParamOfCompInList") as DataTemplate;
            else if (calc != null)
                return element.TryFindResource("CalcInList") as DataTemplate;
            else
                return null;
        }
    }

    public class ComponentDataTemplateSelectorMapping : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            ParameterStructure.Component.Component component = item as ParameterStructure.Component.Component;
            ParameterStructure.Component.EmptyComponentSlot compslot = new EmptyComponentSlot { SlotType = 0, SlotName = string.Empty, SlotDescr = string.Empty };
            if (item is ParameterStructure.Component.EmptyComponentSlot)
                compslot = (ParameterStructure.Component.EmptyComponentSlot)item;
            ParameterStructure.Parameter.Parameter parameter = item as ParameterStructure.Parameter.Parameter;
            ParameterStructure.Parameter.Calculation calc = item as ParameterStructure.Parameter.Calculation;

            if (component != null)
            {
                if (component.IsHidden)
                    return element.TryFindResource("ComponentInListHidden") as DataTemplate;
                else
                    return element.TryFindResource("ComponentWoControlsInList") as DataTemplate;
            }
            else if (!(string.IsNullOrEmpty(compslot.SlotName)))
            {
                if (compslot.IsLarge)
                    return element.TryFindResource("ComponentSlotInParamList") as DataTemplate;
                else
                    return element.TryFindResource("ComponentSlotInList") as DataTemplate;
            }
            else if (parameter != null)
                return element.TryFindResource("ParamInListMapping") as DataTemplate;
            else if (calc != null)
                return element.TryFindResource("CalcInListMapping") as DataTemplate;
            else
                return null;
        }
    }

    public class ComponentDataTemplateSelectorMappingToWebServ : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            ParameterStructure.Component.Component component = item as ParameterStructure.Component.Component;
            ParameterStructure.Component.EmptyComponentSlot compslot = new EmptyComponentSlot { SlotType = 0, SlotName = string.Empty, SlotDescr = string.Empty };
            if (item is ParameterStructure.Component.EmptyComponentSlot)
                compslot = (ParameterStructure.Component.EmptyComponentSlot)item;
            ParameterStructure.Parameter.Parameter parameter = item as ParameterStructure.Parameter.Parameter;
            ParameterStructure.Parameter.Calculation calc = item as ParameterStructure.Parameter.Calculation;
            ParameterStructure.Geometry.GeometricRelationship geom_rel = item as ParameterStructure.Geometry.GeometricRelationship;
            ParameterStructure.Geometry.HierarchicalContainer hcontainer = item as ParameterStructure.Geometry.HierarchicalContainer;

            if (component != null)
            {
                if (component.IsHidden)
                    return element.TryFindResource("ComponentInListHidden") as DataTemplate;
                else
                    return element.TryFindResource("ComponentWInstancesWoControlsInList") as DataTemplate;
            }
            else if (!(string.IsNullOrEmpty(compslot.SlotName)))
            {
                if (compslot.IsLarge)
                    return element.TryFindResource("ComponentSlotInParamList") as DataTemplate;
                else
                    return element.TryFindResource("ComponentSlotInList") as DataTemplate;
            }
            else if (parameter != null)
                return element.TryFindResource("ParamInListMapping") as DataTemplate;
            else if (calc != null)
                return element.TryFindResource("CalcInListMapping") as DataTemplate;
            else if (geom_rel != null)
                return element.TryFindResource("GeomRelationshipInMappingTree") as DataTemplate;
            else if (hcontainer != null)
                return element.TryFindResource("HierarchicalContainerInList") as DataTemplate;
            else
                return null;
        }
    }

    public class ParameterChildrenTemplateSelectorShort : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            ParameterStructure.Component.EmptyComponentSlot compslot = new EmptyComponentSlot { SlotType = 0, SlotName = string.Empty, SlotDescr = string.Empty };
            if (item is ParameterStructure.Component.EmptyComponentSlot)
                compslot = (ParameterStructure.Component.EmptyComponentSlot)item;
            ParameterStructure.Parameter.Parameter parameter = item as ParameterStructure.Parameter.Parameter;

            if (!(string.IsNullOrEmpty(compslot.SlotName)))
            {
                return element.TryFindResource("ComponentSlotShortInParamList") as DataTemplate;
            }
            else if (parameter != null)
                return element.TryFindResource("ParamShortInList") as DataTemplate;
            else
                return null;
        }
    }

    public class FlowNetworkDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            FlNetNode node = item as FlNetNode;
            FlNetEdge edge = item as FlNetEdge;
            FlowNetwork network = item as FlowNetwork;

            if (node != null && network == null)
                return element.TryFindResource("FlNetNodeInList") as DataTemplate;
            else if (edge != null)
                return element.TryFindResource("FlNetEdgeInList") as DataTemplate;
            else if (network != null)
                return element.TryFindResource("NetworkInList") as DataTemplate;
            else
                return null;
        }
    }
}
