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

namespace DataStructVisualizer.WpfUtils
{
    public class TreeViewExt : TreeView
    {

        #region OVERRIDES: Class handling the Items

        protected override DependencyObject GetContainerForItemOverride()
        {
            TreeViewItemExt container = new TreeViewItemExt();
            container.DragNDropCmd = this.DragNDropCmd;
            this.Counter++;
            container.Tag = this.Counter.ToString();
            return container;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItemExt;
        }

        #endregion

        public TreeViewExt(): base()
        {
            // selection handling
            this.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(___ICH);        
        }

        #region EVENT HANDLERS
        private void ___ICH(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedItem_ = SelectedItem;
        }

        #endregion

        #region DEPENDENCY PROPERTIES: TreeView Functionality


        public int Counter
        {
            get { return (int)GetValue(CounterProperty); }
            set { SetValue(CounterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Counter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CounterProperty =
            DependencyProperty.Register("Counter", typeof(int), typeof(TreeViewExt), 
            new UIPropertyMetadata(0, new PropertyChangedCallback(Test)));

        private static void Test(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((int)e.NewValue == 0)
            {

            }
        }


        public ICommand DragNDropCmd
        {
            get { return (ICommand)GetValue(DragNDropCmdProperty); }
            set { SetValue(DragNDropCmdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragNDropCmd.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragNDropCmdProperty =
            DependencyProperty.Register("DragNDropCmd", typeof(ICommand), typeof(TreeViewExt), 
            new UIPropertyMetadata(null, new PropertyChangedCallback(PropagateDragNDropCmd)));

        private static void PropagateDragNDropCmd(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeViewExt tve = d as TreeViewExt;
            if (tve != null)
            {
                foreach(object item in tve.Items)
                {
                    TreeViewItemExt tvie = tve.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItemExt;
                    if (tvie != null)
                    {
                        tvie.DragNDropCmd = tve.DragNDropCmd;
                    }
                }
            }
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

        #endregion

        #region DEPENDENCY PROPERTIES: Highlighting a Region

        public bool HighlightRegionMode
        {
            get { return (bool)GetValue(HighlightRegionModeProperty); }
            set { SetValue(HighlightRegionModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HighlightRegionMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightRegionModeProperty =
            DependencyProperty.Register("HighlightRegionMode", typeof(bool), typeof(TreeViewExt),
            new UIPropertyMetadata(false));

        public Rect HighlightRect
        {
            get { return (Rect)GetValue(HighlightRectProperty); }
            set { SetValue(HighlightRectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HighlightRect.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightRectProperty =
            DependencyProperty.Register("HighlightRect", typeof(Rect), typeof(TreeViewExt), 
            new UIPropertyMetadata(new Rect(new Point(0,0), new Point(250,100))));


        #endregion
    }

}
