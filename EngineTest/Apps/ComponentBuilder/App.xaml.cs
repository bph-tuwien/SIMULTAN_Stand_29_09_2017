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

using ComponentBuilder.WpfUtils;
using ComponentBuilder.WinUtils;

namespace ComponentBuilder
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

        #endregion

        #region OVERRIDES
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            EventManager.RegisterClassHandler(typeof(TreeView), TreeView.MouseUpEvent, new RoutedEventHandler(TreeView_MouseUp));
            EventManager.RegisterClassHandler(typeof(TreeView), TreeView.MouseDownEvent, new RoutedEventHandler(TreeView_MouseDown));
            EventManager.RegisterClassHandler(typeof(TreeViewItem), TreeViewItem.MouseUpEvent, new RoutedEventHandler(TreeViewItem_MouseUp));
            EventManager.RegisterClassHandler(typeof(TreeViewItem), TreeViewItem.SelectedEvent, new RoutedEventHandler(TreeViewItem_OnSelected));

            // added 16.08.2017 - exception handling (caused by System.Windows.Automation.ElementNotAvailableException in PresentationFramework)
            this.DispatcherUnhandledException += Dispatcher_UnhandledException;
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            System.Windows.Forms.Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        #endregion

        #region EVENT HANDLER: Error Handling

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
 
    }
}
