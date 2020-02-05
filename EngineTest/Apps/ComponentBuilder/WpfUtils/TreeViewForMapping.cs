using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Resources;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;

using ComponentBuilder.UIElements;
using ComponentBuilder.WebServiceConnections;

namespace ComponentBuilder.WpfUtils
{
    class TreeViewForMapping : TreeView
    {
        public TreeViewForMapping()
           :base()
        {
            this.Loaded += TreeViewForMapping_Loaded;
        }

        protected ScrollViewer child_scroll_viewer;
        private void TreeViewForMapping_Loaded(object sender, RoutedEventArgs e)
        {
            this.child_scroll_viewer = TreeViewQuery.GetScrollViewerChildOf(this);
            if (this.act_on_TreeViewItem_IsExpanded_changed != null && this.child_scroll_viewer != null)
            {
                this.child_scroll_viewer.ScrollChanged += child_scroll_viewer_ScrollChanged;
            }                
        }

        #region OVERRIDES

        protected override DependencyObject GetContainerForItemOverride()
        {
            TreeViewItemForMapping container = new TreeViewItemForMapping();
            container.ActOnTreeViewItemExpandedChangedHandler = this.ActOnTreeViewItemExpandedChangedHandler;
            return container;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItemForMapping;
        }

        #endregion

        #region EVENT HANDLER PROPERTIES

        public delegate void ActOnTreeViewItemExpandedChanged();

        protected ActOnTreeViewItemExpandedChanged act_on_TreeViewItem_IsExpanded_changed;
        public ActOnTreeViewItemExpandedChanged ActOnTreeViewItemExpandedChangedHandler 
        {
            get { return this.act_on_TreeViewItem_IsExpanded_changed; }
            set
            {
                this.act_on_TreeViewItem_IsExpanded_changed = value;
                if (this.act_on_TreeViewItem_IsExpanded_changed != null && this.child_scroll_viewer != null)
                {
                    this.child_scroll_viewer.ScrollChanged += child_scroll_viewer_ScrollChanged;
                }

                foreach (object item in this.Items)
                {
                    TreeViewItemForMapping tvi = TreeViewQuery.ContainerFromItem(this, item) as TreeViewItemForMapping;
                    if (tvi != null)
                    {
                        tvi.ActOnTreeViewItemExpandedChangedHandler = value;
                    }
                }
            }
        }

        protected void child_scroll_viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (this.act_on_TreeViewItem_IsExpanded_changed != null)
                this.act_on_TreeViewItem_IsExpanded_changed.Invoke();
        }

        #endregion

        #region METHOD: Info Propagation

        public void PropagateContentInfoToItems(WebServiceMapWindow _win)
        {
            if (_win == null) return;

            foreach(object item in this.Items)
            {
                TreeViewItemForMapping tvi = this.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItemForMapping;
                if (tvi != null)
                {
                    tvi.ParentWindow = _win;
                }
            }
        }

        public void PropagateMarkingToItems(bool _mark)
        {
            foreach (object item in this.Items)
            {
                TreeViewItemForMapping tvi = this.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItemForMapping;
                if (tvi != null)
                {
                    tvi.IsMarked = _mark;
                    tvi.PropagateMarkingToItems(_mark);
                }
            }
        }

        #endregion

        #region METHODS: Expand or Collapse Propagation

        public void ChangeExpandedState(bool _is_expanded)
        {
            foreach (object item in this.Items)
            {
                TreeViewItemForMapping tvi = this.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItemForMapping;
                if (tvi != null)
                {
                    tvi.IsExpanded = _is_expanded;
                    tvi.ChangeExpandedState(_is_expanded);
                }
            }
        }

        #endregion
    }
}
