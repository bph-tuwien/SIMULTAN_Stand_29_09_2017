using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GeometryViewer.UIElements
{
    public class TreeViewExt : TreeView
    {
        public TreeViewExt(): base()
        {
            this.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(___ICH);
        }

        private void ___ICH(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedItem_ = SelectedItem;
        }

        public object SelectedItem_
        {
            get { return (object)GetValue(SelectedItem_Property); }
            set { SetValue(SelectedItem_Property, value); }
        }
        public static readonly DependencyProperty SelectedItem_Property = 
            DependencyProperty.Register("SelectedItem_", typeof(object), typeof(TreeViewExt), 
            new UIPropertyMetadata(null, new PropertyChangedCallback(TransferBackToSelectedItem)));

        private static void TransferBackToSelectedItem(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeViewExt tve = d as TreeViewExt;
            if (tve != null)
            {
                if (tve.SelectedItem_ == null)
                    return;

                TreeViewItem item = TreeViewQuery.ContainerFromItem(tve, tve.SelectedItem_);
                if (item != null)
                    item.IsSelected = true;
                else
                {
                    if (tve.Items.Count > 0)
                    {
                        item = (TreeViewItem)tve.ItemContainerGenerator.ContainerFromItem(tve.Items[0]);
                        if (item != null)
                            item.IsSelected = true;
                    }
                }
            }
        }



        public bool AllowNoSelection
        {
            get { return (bool)GetValue(AllowNoSelectionProperty); }
            set { SetValue(AllowNoSelectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowNoSelection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowNoSelectionProperty =
            DependencyProperty.Register("AllowNoSelection", typeof(bool), typeof(TreeViewExt), 
            new UIPropertyMetadata(false));


    }

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
    }
}
