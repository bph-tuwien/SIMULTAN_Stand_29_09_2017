using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ComponentBuilder.WpfUtils
{
    public static class TreeViewItemExtensions
    {
        public static int GetDepth(this TreeViewItem item)
        {
            TreeViewItem parent;
            while ((parent = GetParent(item)) != null)
            {
                return GetDepth(parent) + 1;
            }
            return 0;
        }

        private static TreeViewItem GetParent(TreeViewItem item)
        {
            var parent = VisualTreeHelper.GetParent(item);
            if (parent == null) return null; // added 01.09.2016

            while (!(parent is TreeViewItem || parent is TreeView))
            {
                if (parent == null) return null; // added 01.09.2016
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as TreeViewItem;
        }

        public static int GetExpansionDepth(this TreeViewItem item)
        {
            TreeViewItem parent;
            while ((parent = GetParent(item)) != null)
            {
                if (parent.IsExpanded)
                    return GetExpansionDepth(parent) + 1;
                else
                    return GetExpansionDepth(parent);
            }
            return 0;
        }

    }
}
