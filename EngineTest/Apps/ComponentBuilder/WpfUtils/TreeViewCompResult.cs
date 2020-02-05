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
using System.Globalization;

using ComponentBuilder.UIElements;

namespace ComponentBuilder.WpfUtils
{
    class TreeViewCompResult : TreeView
    {
        #region OVERRIDES

        protected override DependencyObject GetContainerForItemOverride()
        {
            TreeViewItemCompResult container = new TreeViewItemCompResult();
            return container;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItemCompResult;
        }

        #endregion

        #region DEPENDENCY PROPERTIES

        public ParameterStructure.Component.ComponentManagerType User
        {
            get { return (ParameterStructure.Component.ComponentManagerType)GetValue(UserProperty); }
            set { SetValue(UserProperty, value); }
        }

        // Using a DependencyProperty as the backing store for User.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(ParameterStructure.Component.ComponentManagerType), typeof(TreeViewCompResult),
            new PropertyMetadata(ParameterStructure.Component.ComponentManagerType.GUEST));

        #endregion

        #region METHODS: Info Propagation

        public void PropagateCompInfoToItems(ParameterStructure.Component.Component _comp, CompCompareWindow _win)
        {
            if (_comp == null) return;
            foreach (object item in this.Items)
            {
                TreeViewItemCompResult tvi = this.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItemCompResult;
                if (tvi != null)
                {
                    tvi.OtherParentComp = _comp;
                    tvi.ParentWindow = _win;
                    tvi.User = this.User;
                }
            }
        }

        #endregion

    }
}
