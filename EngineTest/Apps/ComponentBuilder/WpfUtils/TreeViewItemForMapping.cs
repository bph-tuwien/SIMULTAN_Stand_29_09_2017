using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Resources;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Media.Imaging;

using ComponentBuilder.WinUtils;
using ComponentBuilder.UIElements;

namespace ComponentBuilder.WpfUtils
{
    class TreeViewItemForMapping : TreeViewItem
    {
        public TreeViewItemForMapping()
            :base()
        {
            this.MouseEnter += eh_MouseEnter;
            this.MouseLeave += eh_MouseLeave;            
            this.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
            this.IsVisibleChanged += TreeViewItemForMapping_IsVisibleChanged;
        }

        private void TreeViewItemForMapping_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.ActOnTreeViewItemExpandedChangedHandler != null)
                this.ActOnTreeViewItemExpandedChangedHandler.Invoke();
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (!(this.PassedEventHanlerToChildren))
            {
                foreach (object item in this.Items)
                {
                    TreeViewItemForMapping tvi = TreeViewQuery.GetSelectedTreeViewItem(this, item) as TreeViewItemForMapping;
                    if (tvi != null)
                    {
                        tvi.ActOnTreeViewItemExpandedChangedHandler = this.ActOnTreeViewItemExpandedChangedHandler;
                    }
                    else
                    {
                        this.PassedEventHanlerToChildren = false;
                        return;
                    }
                }
                this.PassedEventHanlerToChildren = true;
            }
        }

        #region PROPERTIES

        public bool PassedEventHanlerToChildren { get; private set; }
        public WebServiceMapWindow ParentWindow { get; internal set; }

        public bool IsMarked
        {
            get { return (bool)GetValue(IsMarkedProperty); }
            set { SetValue(IsMarkedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMarked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMarkedProperty =
            DependencyProperty.Register("IsMarked", typeof(bool), typeof(TreeViewItemForMapping), new UIPropertyMetadata(false));


        public bool IsMarkingInitiator
        {
            get { return (bool)GetValue(IsMarkingInitiatorProperty); }
            set { SetValue(IsMarkingInitiatorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMarkingInitiator.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMarkingInitiatorProperty =
            DependencyProperty.Register("IsMarkingInitiator", typeof(bool), typeof(TreeViewItemForMapping), new UIPropertyMetadata(false));
        

        #endregion

        #region OVERRIDES

        protected override DependencyObject GetContainerForItemOverride()
        {
            TreeViewItemForMapping container = new TreeViewItemForMapping();
            container.ParentWindow = this.ParentWindow;
            container.ActOnTreeViewItemExpandedChangedHandler = this.ActOnTreeViewItemExpandedChangedHandler;
            container.IsMarked = this.IsMarked;
            container.IsExpanded = this.IsExpanded;
            return container;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItemForMapping;
        }

        #endregion

        #region METHOD: Info Propagation

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

        #region METHOD: Expand or Collapse Propagation

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

        #region EVENT HANDLERS

        protected TreeViewForMapping.ActOnTreeViewItemExpandedChanged act_on_TreeViewItem_IsExpanded_changed;
        public TreeViewForMapping.ActOnTreeViewItemExpandedChanged ActOnTreeViewItemExpandedChangedHandler
        {
            get { return this.act_on_TreeViewItem_IsExpanded_changed; }
            set
            {
                this.act_on_TreeViewItem_IsExpanded_changed = value;
                this.PassedEventHanlerToChildren = false;
            }
        }

        #endregion

        #region EVENT HANDLERS MOUSE ...TODO...

        private void eh_MouseLeave(object sender, MouseEventArgs e)
        {
            TreeViewItemForMapping tvi = sender as TreeViewItemForMapping;
            // header UN-highlighting preparation
            if (tvi != null)
            {
                ParameterStructure.Parameter.Parameter p = tvi.Header as ParameterStructure.Parameter.Parameter;
                ParameterStructure.Component.Component c = tvi.Header as ParameterStructure.Component.Component;
                WebServiceConnections.TypeNode tn = tvi.Header as WebServiceConnections.TypeNode;
                if (p != null)
                {
                    // TODO
                }
                else if (c != null)
                {
                    // TODO
                }
                else if (tn != null)
                {
                    // TODO
                }
            }
        }

        private void eh_MouseEnter(object sender, MouseEventArgs e)
        {
            TreeViewItemForMapping tvi = sender as TreeViewItemForMapping;
            // header highlighting preparation
            if (tvi != null)
            {
                ParameterStructure.Parameter.Parameter p = tvi.Header as ParameterStructure.Parameter.Parameter;
                ParameterStructure.Component.Component c = tvi.Header as ParameterStructure.Component.Component;
                WebServiceConnections.TypeNode tn = tvi.Header as WebServiceConnections.TypeNode;
                if (p != null)
                {
                    // TODO
                }
                else if (c != null)
                {
                    // TODO
                }
                else if (tn != null)
                {
                    // TODO
                }
            }
        }

        #endregion

    }
}
