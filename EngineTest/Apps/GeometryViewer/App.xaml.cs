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

using GeometryViewer.UIElements;
using InterProcCommunication;

namespace GeometryViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region CLASS MEMBER

        private bool treeViewItem_clicked = false;
        private bool treeView_rightClicked = false;
        private TreeView treeView = null;

        // communication with ComponentBuilder
        public string CallingUser { get; private set; }
        public string CU_client_name { get; private set; }
        public string CU_server_name { get; private set; }
        public string CU_authentication { get; private set; }
        public bool CU_send_updates_back { get; private set; }

        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // handle input args
            if (e != null && e.Args != null && e.Args.Length > 0)
            {
                // if the app was started with args -> it was called by another process
                // try to extract the user and the comm unit parameters
                string arg0 = e.Args[0];
                string[] args0_comps = arg0.Split(new string[] { CommMessageUtils.STR_ARG_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
                if (args0_comps.Length > 0)
                    this.CallingUser = args0_comps[0];
                if (args0_comps.Length > 1)
                    this.CU_client_name = args0_comps[1];
                if (args0_comps.Length > 2)
                    this.CU_server_name = args0_comps[2];
                if (args0_comps.Length > 3)
                    this.CU_authentication = args0_comps[3];
                if (args0_comps.Length > 4)
                {
                    bool arg_as_bool = true;
                    bool success_parsing_bool = bool.TryParse(args0_comps[4], out arg_as_bool);
                    if (success_parsing_bool)
                        this.CU_send_updates_back = arg_as_bool;
                    else
                        this.CU_send_updates_back = true; // default
                }

                //string args_all = string.Join("\t", args0_comps);
                //MessageBox.Show(args_all, "[" + e.Args.Length + "] Inpurt Params", MessageBoxButton.OK);
            }


            // these cause HUGE DELAY when many textboxes are close to each other
            //EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
            //EventManager.RegisterClassHandler(typeof(TextBox), TextBox.MouseUpEvent, new RoutedEventHandler(TextBox_GotFocus));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.LostFocusEvent, new RoutedEventHandler(TextBox_LostFocus));

            EventManager.RegisterClassHandler(typeof(TreeView), TreeView.MouseUpEvent, new RoutedEventHandler(TreeView_MouseUp));
            EventManager.RegisterClassHandler(typeof(TreeView), TreeView.MouseDownEvent, new RoutedEventHandler(TreeView_MouseDown));
            EventManager.RegisterClassHandler(typeof(TreeViewItem), TreeViewItem.MouseUpEvent, new RoutedEventHandler(TreeViewItem_MouseUp));
            EventManager.RegisterClassHandler(typeof(TreeViewItem), TreeViewItem.SelectedEvent, new RoutedEventHandler(TreeViewItem_OnSelected));

            EventManager.RegisterClassHandler(typeof(ListBoxItem), ListBoxItem.SelectedEvent, new RoutedEventHandler(ListBoxItem_OnSelected));

            // added 16.08.2017 - exception handling (caused by System.Windows.Automation.ElementNotAvailableException in PresentationFramework)
            this.DispatcherUnhandledException += Dispatcher_UnhandledException;
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            System.Windows.Forms.Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        #region ERROR HANDLING

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // MessageBox.Show("CurrentDomain_UnhandledException", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            if (e.IsTerminating)
            {
                MessageBox.Show("I don't want to live any more...", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            // MessageBox.Show("CurrentDomain_FirstChanceException:\n" + e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            // string errorMessage = string.Format("An unhandled thread exception occurred: {0}", e.Exception.Message);
            // MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // MessageBox.Show("TaskScheduler_UnobservedTaskException:\n" + e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.SetObserved();
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
            // MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        #endregion

        #region TEXTBOX
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                tb.SelectAll();
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null && tb.Tag != null && tb.Tag.ToString() == "UPDATE_ON_NO_CHANGE")
            {
                // force update
                BindingExpression binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
                string text_old = tb.Text;
                tb.Text = "";
                binding.UpdateSource();
                tb.Text = text_old;
                binding.UpdateSource();
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

        #region LISTBOX

        private void ListBoxItem_OnSelected(object sender, RoutedEventArgs e)
        {
            ListBoxItem item = e.OriginalSource as ListBoxItem;
            if (item != null)
                item.BringIntoView();
        }

        #endregion

    }

    public static class AppHelpers
    {
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
