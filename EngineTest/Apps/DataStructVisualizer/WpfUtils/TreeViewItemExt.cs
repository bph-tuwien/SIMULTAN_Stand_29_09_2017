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

namespace DataStructVisualizer.WpfUtils
{
    public class TreeViewItemExt : TreeViewItem
    {

        #region STATIC

        private static Cursor cur_DD;
        private static Cursor cur_DD_notAllowed;
        private static readonly Brush DD_TARGET;
        private static readonly Brush STANDARD;
        private static List<TreeViewItemExt> drop_target_chain;

        static TreeViewItemExt()
        {
            TreeViewItemExt.DD_TARGET = Brushes.RoyalBlue;
            TreeViewItemExt.STANDARD = Brushes.Black;
            TreeViewItemExt.drop_target_chain = new List<TreeViewItemExt>();
            try
            {
                StreamResourceInfo sriCursor = Application.GetResourceStream(new Uri("/Data/icons/cursor_dragndrop.cur", UriKind.Relative));
                if (sriCursor != null)
                    TreeViewItemExt.cur_DD = new Cursor(sriCursor.Stream);
            }
            catch
            {
                TreeViewItemExt.cur_DD = Cursors.Arrow;
            }
            finally 
            {
                TreeViewItemExt.cur_DD_notAllowed = Cursors.No;
            }
        }

        private static void RestoreVisualStylesAfterDrop()
        {
            if (TreeViewItemExt.drop_target_chain.Count > 0)
            {
                foreach (TreeViewItemExt t in TreeViewItemExt.drop_target_chain)
                {
                    // restore visual style
                    t.CouldBeDropTarget = false;
                    System.Windows.Style copy = new Style(typeof(TreeViewItem), t.Style);
                    copy.Setters.Add(new Setter(TreeViewItem.BorderBrushProperty, TreeViewItemExt.STANDARD));
                    copy.Setters.Add(new Setter(TreeViewItem.BorderThicknessProperty, new Thickness(0)));
                    t.Style = copy;
                }
                TreeViewItemExt.drop_target_chain.Clear();
            }
        }

        #endregion

        #region CLASS MEMBERS

        // drag n drop
        private Point mouseMove_startingP;
        public bool IsDragging { get; private set; }
        public bool CouldBeDropTarget { get; private set; }
        public ICommand DragNDropCmd { get; set; }

        // numbering of subnodes
        private int nr_subnodes;

        #endregion

        public TreeViewItemExt() :base()
        {
            // drag n drop handling
            this.AllowDrop = true;
            this.IsDragging = false;
            CouldBeDropTarget = false;
            this.PreviewMouseLeftButtonDown += TreeViewItemExt_PreviewMouseLeftButtonDown;
            this.PreviewMouseMove += TreeViewItemExt_PreviewMouseMove; // tunnels down
            this.MouseMove += TreeViewItemExt_MouseMove; // bubbles up
            this.GiveFeedback += TreeViewItemExt_GiveFeedback;
            this.DragEnter += TreeViewItemExt_DragEnter;
            this.DragOver += TreeViewItemExt_DragOver;
            this.DragLeave += TreeViewItemExt_DragLeave;
            this.Drop += TreeViewItemExt_Drop;
            // numbering of subnodes
            this.nr_subnodes = 0;
            this.Items.CurrentChanged += Items_CurrentChanged;
        }

        


        #region OVERRIDES
        protected override DependencyObject GetContainerForItemOverride()
        {
            TreeViewItemExt container = new TreeViewItemExt();
            container.DragNDropCmd = this.DragNDropCmd;
            container.Tag = this.Tag + "." + (++this.nr_subnodes).ToString();
            return container;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItemExt;
        }
        #endregion

        #region EVENT HANDLERS

        private void Items_CurrentChanged(object sender, EventArgs e)
        {
            this.nr_subnodes = 0;
        }

        private void TreeViewItemExt_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItemExt tvie = sender as TreeViewItemExt;
            if (tvie != null && e != null)
            {
                this.mouseMove_startingP = e.GetPosition(null);
            }
        }

        // runs from ROOT to LEAF
        private void TreeViewItemExt_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //TreeViewItemExt tvie = sender as TreeViewItemExt;
            //if (tvie != null && e != null)
            //{
            //    if (!tvie.IsSelected)
            //        return;
            //    if (e.LeftButton == MouseButtonState.Pressed && !IsDragging)
            //    {
            //        Point pos = e.GetPosition(null);
            //        // check if the mouse moved far enough:
            //        bool enoughX = Math.Abs(pos.X - mouseMove_startingP.X) > 2 * SystemParameters.MinimumHorizontalDragDistance;
            //        bool enoughY = Math.Abs(pos.Y - mouseMove_startingP.Y) > 2 * SystemParameters.MinimumVerticalDragDistance;
            //        if (enoughX || enoughY)
            //        {
            //            this.mouseMove_startingP = pos;
            //            this.StartDrag(e);
            //        }
            //    }
            //}
        }

        // runs from LEAF to ROOT
        private void TreeViewItemExt_MouseMove(object sender, MouseEventArgs e)
        {
            TreeViewItemExt tvie = sender as TreeViewItemExt;
            if (tvie != null && e != null)
            {
                if (!tvie.IsSelected)
                    return;
                if (e.LeftButton == MouseButtonState.Pressed && !IsDragging)
                {
                    Point pos = e.GetPosition(null);
                    // check if the mouse moved far enough:
                    bool enoughX = Math.Abs(pos.X - mouseMove_startingP.X) > 2 * SystemParameters.MinimumHorizontalDragDistance;
                    bool enoughY = Math.Abs(pos.Y - mouseMove_startingP.Y) > 2 * SystemParameters.MinimumVerticalDragDistance;
                    if (enoughX || enoughY)
                    {
                        this.mouseMove_startingP = pos;
                        this.StartDrag(e);
                    }
                }
            }
        }

        // dragging across Apps (e.g. UIElements in the same window, or WORD)
        private void StartDrag(MouseEventArgs e)
        {
            if (this.Header == null)
                return;

            // actual dragging
            this.IsDragging = true;
            DataObject data = new DataObject(this.Header.GetType().ToString(), this.Header);
            // NO mouse or cursor event fires on DoDragDrop
            DragDropEffects de = DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            this.IsDragging = false;
        }

        private void TreeViewItemExt_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (e != null && this.IsDragging)
            {
                Mouse.SetCursor(TreeViewItemExt.cur_DD);
                e.UseDefaultCursors = false;
                e.Handled = true;
            }
        }

        private void TreeViewItemExt_DragEnter(object sender, DragEventArgs e)
        {
            TreeViewItemExt tvie = sender as TreeViewItemExt;
            if (tvie != null && !tvie.IsDragging)
            {
                this.CouldBeDropTarget = true;
                TreeViewItemExt.drop_target_chain.Add(this);
                System.Windows.Style copy = new Style(typeof(TreeViewItem), this.Style);
                copy.Setters.Add(new Setter(TreeViewItem.BorderBrushProperty, TreeViewItemExt.DD_TARGET));
                copy.Setters.Add(new Setter(TreeViewItem.BorderThicknessProperty, new Thickness(1)));
                this.Style = copy;
            }
        }

        private void TreeViewItemExt_DragLeave(object sender, DragEventArgs e)
        {
            TreeViewItemExt tvie = sender as TreeViewItemExt;
            if (tvie != null && !tvie.IsDragging)
            {
                this.CouldBeDropTarget = false;
                TreeViewItemExt.drop_target_chain.Remove(this);
                System.Windows.Style copy = new Style(typeof(TreeViewItem), this.Style);
                copy.Setters.Add(new Setter(TreeViewItem.BorderBrushProperty, TreeViewItemExt.STANDARD));
                copy.Setters.Add(new Setter(TreeViewItem.BorderThicknessProperty, new Thickness(0)));
                this.Style = copy;               
            }
        }

        private void TreeViewItemExt_DragOver(object sender, DragEventArgs e)
        {
            TreeViewItemExt tvie = sender as TreeViewItemExt;
            if (tvie != null && !tvie.IsDragging && e != null)
            {
                IDataObject data = e.Data;
                if (!data.GetDataPresent(typeof(Nodes.Node).ToString()))
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                }
            }
        }

        private void TreeViewItemExt_Drop(object sender, DragEventArgs e)
        {
            TreeViewItemExt tvie = sender as TreeViewItemExt;
            if (tvie != null && !tvie.IsDragging && e != null)
            {
                IDataObject data = e.Data;
                if (data.GetDataPresent(typeof(Nodes.Node).ToString()))
                {
                    Nodes.Node payload = data.GetData(typeof(Nodes.Node)) as Nodes.Node;
                    if (payload != null && this.DragNDropCmd != null)
                    {
                        // inform user
                        string message = string.Format("moving {0}\nto {1}", payload.ToString(), this.Header.ToString());
                        string caption = "Moving Node";
                        MessageBoxResult answer = MessageBox.Show( message, caption, 
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (answer == MessageBoxResult.Yes)
                        {
                            // execute drop command
                            this.DragNDropCmd.Execute(new WinUtils.TwoObjects { object1 = payload, object2 = this.Header });
                            e.Handled = true; 
                        }
                        
                        // restore the visual style of any containing nodes
                        TreeViewItemExt.RestoreVisualStylesAfterDrop();                        
                    }
                }
            }
        }

        

        #endregion

    }

}
