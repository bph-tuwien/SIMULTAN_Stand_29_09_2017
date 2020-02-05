using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using GeometryViewer.ComponentInteraction;
using GeometryViewer.ComponentReps;

namespace GeometryViewer.Utils
{
    public class DataTemplateSelectorMaterial : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            CompRepInfo cr = item as CompRepInfo;
            Material mat = item as Material;

            if (cr != null)
                return element.TryFindResource("CompRepInList") as DataTemplate;
            else if (mat != null)
                return element.TryFindResource("MaterialInTreeView") as DataTemplate;
            else
                return element.TryFindResource("DT_Unknown") as DataTemplate;
        }
    }

    public class DataTemplateSelectorSpace : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            CompRepInfo cr = item as CompRepInfo;
            if (cr == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            if (!(cr is CompRepAlignedWith) && !(cr is CompRepContainedIn) && !(cr is CompRepContainedIn_Instance) &&
                !(cr is CompRepConnects) && !(cr is CompRepConnects_Instance))
                return element.TryFindResource("CompRepInList") as DataTemplate;
            else
                return element.TryFindResource("HDT_Empty") as DataTemplate;
        }
    }

    public class DataTemplateSelectorPlacable : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null) return null;
            FrameworkElement element = container as FrameworkElement;
            if (element == null) return null;

            if (item == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            CompRepInfo cr = item as CompRepInfo;
            if (cr == null) return element.TryFindResource("DT_Empty") as DataTemplate;

            if (!(cr is CompRepDescirbes) && !(cr is CompRepDescirbes3D) && !(cr is CompRepDescribes2DorLess) &&
                !(cr is CompRepAlignedWith) && !(cr is CompRepGroups))
                return element.TryFindResource("CompRepInList") as DataTemplate;
            else
                return element.TryFindResource("HDT_Empty") as DataTemplate;
        }
    }
}
