using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Reflection;
using System.Diagnostics;

using DataStructVisualizer.WpfUtils;

namespace DataStructVisualizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        #region CLASS MEMBERS

        private bool treeViewItem_clicked = false;
        private bool treeView_rightClicked = false;
        private TreeView treeView = null;

        private ListBox lb_text_completion_source;
        private TextBox tb_text_completion_target;
        private string DEBUG = "";
        private int DEBUG_CTR = 0;
        private bool text_completion_peformed = false;
        private string text_completion_Text_Old = string.Empty;

        private bool highlight_rect_def_running = false;
        private bool highlight_done = false;
        Point h_rect_ul = new Point(0, 0);
        Point h_rect_dr = new Point(0, 0);

        #endregion

        #region OVERRIDES
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.LostFocusEvent, new RoutedEventHandler(TextBox_LostFocus));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));

            EventManager.RegisterClassHandler(typeof(TreeView), TreeView.MouseUpEvent, new RoutedEventHandler(TreeView_MouseUp));
            EventManager.RegisterClassHandler(typeof(TreeView), TreeView.MouseDownEvent, new RoutedEventHandler(TreeView_MouseDown));
            EventManager.RegisterClassHandler(typeof(TreeViewItem), TreeViewItem.MouseUpEvent, new RoutedEventHandler(TreeViewItem_MouseUp));
            EventManager.RegisterClassHandler(typeof(TreeViewItem), TreeViewItem.SelectedEvent, new RoutedEventHandler(TreeViewItem_OnSelected));

            //EventManager.RegisterClassHandler(typeof(TreeViewItemExt), TreeViewItemExt.PreviewMouseMoveEvent, new RoutedEventHandler(TreeViewItemExt_OnPreviewMouseMove));
            //EventManager.RegisterClassHandler(typeof(TreeViewItemExt), TreeViewItemExt.MouseMoveEvent, new RoutedEventHandler(TreeViewItemExt_OnMouseMove));
            EventManager.RegisterClassHandler(typeof(TreeViewExt), TreeViewExt.MouseUpEvent, new RoutedEventHandler(TreeViewExt_MouseUpEvent));

            EventManager.RegisterClassHandler(typeof(ListBox), ListBox.SelectionChangedEvent, new RoutedEventHandler(ListBox_OnSelectionChanged));
        }

        #endregion

        #region UIElements Search

        private void FindTextCompletionUIElements()
        {
            var target_obj = this.MainWindow.FindName("tb_text_completion_target");
            this.tb_text_completion_target = target_obj as TextBox;
            
            var source_obj = this.MainWindow.FindName("lb_text_completion_source");
            this.lb_text_completion_source = source_obj as ListBox;
            if (this.lb_text_completion_source != null)
                this.lb_text_completion_source.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region TEXTBOX
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.DEBUG_CTR++;
            this.DEBUG += "\nTB Click NR " + this.DEBUG_CTR + "\n";
            this.DEBUG += "TextBox_GotFocus\n";
            this.DEBUG += "-- COMPLETION PERFORMED: " + this.text_completion_peformed + " --\n";
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                //tb.SelectAll();

                if (this.lb_text_completion_source == null)
                    this.FindTextCompletionUIElements();

                if (tb.Name == "tb_text_completion_target" && this.lb_text_completion_source != null)
                {
                    this.text_completion_Text_Old = tb.Text;
                    this.lb_text_completion_source.Visibility = Visibility.Visible;                   
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.DEBUG += "TextBox_LostFocus\n";
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                if (tb.Tag != null && tb.Tag.ToString() == "UPDATE_ON_NO_CHANGE")
                {
                    // force update
                    BindingExpression binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
                    string text_old = tb.Text;
                    tb.Text = "";
                    binding.UpdateSource();
                    tb.Text = text_old;
                    binding.UpdateSource();
                }
                if (tb.Name == "tb_text_completion_target")
                {                    
                    if (this.lb_text_completion_source == null)
                        this.FindTextCompletionUIElements();

                    if (this.lb_text_completion_source != null)
                    {
                        if (!this.lb_text_completion_source.IsFocused)
                        {
                            this.lb_text_completion_source.Visibility = Visibility.Collapsed;
                        }
                        this.DEBUG += "-- COMPLETION PERFORMED: " + this.text_completion_peformed + " --\n";                        
                        if (!this.text_completion_peformed)
                        {                           
                            if (this.lb_text_completion_source.Items.Count <= 1)
                            {
                                // rename when new name is not contained in any other in the text completion ListBox
                                this.DEBUG += "-- case 'NO COMPLETION && Items <= 1'\n";
                                BindingExpression tb_be = this.tb_text_completion_target.GetBindingExpression(TextBox.TextProperty);
                                tb_be.UpdateSource();
                            }
                            else if (this.lb_text_completion_source.Items.Count > 1)
                            {
                                // the new name is contained in another already existing name
                                this.DEBUG += "-- case 'NO COMPLETION && Items > 1'\n";
                                this.lb_text_completion_source.SelectedIndex = -1;
                                this.lb_text_completion_source.Focus();                              
                            }
                        }
                        this.text_completion_peformed = false;
                        this.DEBUG += "-- COMPLETION PERFORMED: " + this.text_completion_peformed + " --\n";
                    }
                }
            }
        }
        #endregion

        #region TREEVIEW

        private void TreeView_MouseDown(object sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;
            this.treeView = sender as TreeView;

            MouseEventArgs mea = e as MouseEventArgs;
            if (mea == null)
                return;

            if (mea.RightButton == MouseButtonState.Pressed)
                this.treeView_rightClicked = true;
        }

        private void TreeView_MouseUp(object sender, RoutedEventArgs e)
        {
            if (sender == null)
                return;

            if (this.treeView != sender as TreeView)
                return;

            if (this.treeView_rightClicked)
            {
                this.treeView_rightClicked = false;
                return;
            }

            TreeView tv = sender as TreeView;
            if (tv == null)
                return;

            if (this.treeViewItem_clicked)
            {
                this.treeViewItem_clicked = false;
            }
            else
            {
                if (tv.SelectedItem != null)
                {
                    DependencyObject selObj = TreeViewQuery.GetSelectedTreeViewItem(tv);
                    TreeViewItem tvi = selObj as TreeViewItem;
                    if (tvi != null)
                        tvi.IsSelected = false;
                }

            }
        }

        private void TreeViewItem_MouseUp(object sender, RoutedEventArgs e)
        {
            if ((sender as TreeViewItem) != null)
            {
                this.treeViewItem_clicked = true;
            }
        }

        private void TreeViewItem_OnSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if (item != null)
                item.BringIntoView();
        }


        #endregion

        #region TREEVIEW EXTENDED

        private void TreeViewExt_MouseUpEvent(object sender, RoutedEventArgs e)
        {
            MouseEventArgs mea = e as MouseEventArgs;
            TreeViewExt tve = sender as TreeViewExt;
            if (mea != null && tve != null && tve.HighlightRegionMode)
            {
                if (!this.highlight_done)
                {
                    Point pos = mea.GetPosition(tve);
                    double debug1 = tve.ActualHeight;
                    double debug2 = tve.ActualWidth;
                    if (!this.highlight_rect_def_running)
                    {
                        this.h_rect_ul = pos;
                        this.h_rect_dr = new Point(tve.ActualWidth, tve.ActualHeight);
                        this.highlight_rect_def_running = true;
                    }
                    else
                    {
                        this.h_rect_dr = pos;
                        this.highlight_rect_def_running = false;
                        tve.HighlightRect = new Rect(this.h_rect_ul, this.h_rect_dr);
                        this.highlight_done = true;
                    }
                }
            }
            else
            {
                this.highlight_rect_def_running = false;
                this.highlight_done = false;
                tve.HighlightRect = new Rect(new Point(0,0), new Point(tve.ActualWidth, tve.ActualHeight));
            }
        }


        // when the mouse moves over a LEAF TreeViewItem
        // the handler gets called from the ROOT to the LEAF
        private void TreeViewItemExt_OnPreviewMouseMove(object sender, RoutedEventArgs e)
        {
            //TreeViewItemExt tvie = sender as TreeViewItemExt;
            //if (tvie != null && e != null)
            //{
            //    Debug.WriteLine(string.Format("PreviewMouseMoveEvent on: {0} from {1}", tvie.Header.ToString(), e.OriginalSource.ToString()));
            //}
        }

        // when the mouse moves over a LEAF TreeViewItem
        // the handler gets called from the LEAF to the ROOT
        private void TreeViewItemExt_OnMouseMove(object sender, RoutedEventArgs e)
        {
            //TreeViewItemExt tvie = sender as TreeViewItemExt;
            //if (tvie != null && e != null)
            //{
            //    Debug.WriteLine(string.Format("       MouseMoveEvent on: {0} from {1}", tvie.Header.ToString(), e.OriginalSource.ToString()));
            //}
        }

        #endregion

        #region LISTBOX

        private void ListBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            this.DEBUG += "ListBox_OnSelectionChanged: ";
            ListBox lb = sender as ListBox;
            if (lb != null)
            {
                this.DEBUG += "Selection(" + lb.SelectedItem + ")\n";
                if (lb.Name == "lb_text_completion_source")
                {
                    if (this.tb_text_completion_target == null)
                        this.FindTextCompletionUIElements();

                    if (this.tb_text_completion_target != null)
                    {
                        if (lb.SelectedItem != null)
                        {
                            this.DEBUG += "-- COMPLETION PERFORMED: " + this.text_completion_peformed + " --\n";
                            bool proceed = this.CheckBeforeTextCompletionOp();
                            if (proceed)
                            {
                                if (lb.SelectedItem.ToString() != StringListToSmallStringListConverter.EMPTY)
                                {
                                    BindingExpression lb_be = lb.GetBindingExpression(ListBox.SelectedItemProperty);
                                    lb_be.UpdateSource();
                                }
                                BindingExpression tb_be = this.tb_text_completion_target.GetBindingExpression(TextBox.TextProperty);
                                tb_be.UpdateSource();
                                this.text_completion_peformed = true;
                            }                               
                            this.DEBUG += "-- COMPLETION PERFORMED: " + this.text_completion_peformed + " --\n";
                            this.DEBUG += "TB.Focus()\n";
                            this.tb_text_completion_target.Focus();
                            lb.Visibility = Visibility.Collapsed;                            
                        }

                    }
                }
            }  
        }

        private bool CheckBeforeTextCompletionOp()
        {
            if (this.tb_text_completion_target == null || this.lb_text_completion_source == null)
                return false;

            string text_old = this.text_completion_Text_Old;
            
            object obj_new = this.lb_text_completion_source.SelectedItem;
            string text_new = string.Empty;
            if (obj_new != null)
                text_new = obj_new.ToString();

            // cannot replace text with nothing
            if (string.IsNullOrEmpty(text_new))
                return false;

            // check for a string indicating that nothing is selected:
            if (text_new == StringListToSmallStringListConverter.EMPTY)
                return true;

            // check if the old text is matched
            bool exact_match_found_old = false;
            foreach (object item in this.lb_text_completion_source.Items)
            {
                if (item == null) continue;
                string str = item.ToString();
                if (str == text_old)
                {
                    exact_match_found_old = true;
                    break;
                }
            }
            if (exact_match_found_old)
            {
                // ask user what to do
                string message = "Do you really want to replace all '" + text_old + "' definitions with '" + text_new + "' ?";
                string caption = "Merging Node Definitions ...";
                MessageBoxResult answer = MessageBox.Show(message, caption,
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (answer == MessageBoxResult.Yes)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        #endregion

    }
}
