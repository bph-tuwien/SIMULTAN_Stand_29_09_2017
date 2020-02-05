using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
//using System.Windows.Automation;

using GeometryViewer.UIElements;
using GeometryViewer.ComponentReps;

namespace GeometryViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.GotFocus += MainWindow_GotFocus;
            // this.DataContext = new AppViewModel(); // was transferred to the XAML file (Intellicense works better)
        }

        // catch ElementNotAvailableException 
        void MainWindow_GotFocus(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);
            //AutomationElement main_win_AE = AutomationElement.FromHandle(helper.Handle);
            //Automation.AddStructureChangedEventHandler(main_win_AE, TreeScope.Element, delegate { });
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool close_really = false;

#if DEBUG
            close_really = true;
#else
            MessageBoxResult result = MessageBox.Show("Möchten Sie die Anwendung wirklich verlassen?", "Anwendung Schliessen", MessageBoxButton.OKCancel);
            close_really = (result == MessageBoxResult.OK);
#endif
            if (close_really)
            {
                // clean-up
                if (this.MainView != null)
                {
                    this.MainView.ShutDownCommUnit(false);
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        #region COLOR PICKER

        private ColorPickerWindow colPicker;

        public Color OpenColorPicker()
        {
            this.colPicker = new ColorPickerWindow();
            this.colPicker.ShowDialog();
            return this.colPicker.PickedColor;
        }

        public void OpenColorPicker(out Color pickedCol, out int pickedInd)
        {
            this.colPicker = new ColorPickerWindow();
            this.colPicker.ShowDialog();
            pickedCol = this.colPicker.PickedColor;
            pickedInd = this.colPicker.PickedIndex;
        }

        #endregion

        #region SPACE GENERATOR

        private ComponentRepWindow compViewer;

        public void OpenComponentViewerForInfo(CompRepDescirbes _comp)
        {
            if (_comp == null)
                return;

            this.compViewer = new ComponentRepWindow(ComponentRepWindowMode.INFO, _comp);
            this.compViewer.Zone = _comp.Geom_Zone;
            this.compViewer.ShowDialog();
        }

        public void OpenComponentViewerForEditing(CompRepDescirbes _comp)
        {
            if (_comp == null)
                return;

            this.compViewer = new ComponentRepWindow(ComponentRepWindowMode.EDIT, _comp);
            this.compViewer.Zone = _comp.Geom_Zone;
            this.compViewer.ShowDialog();
        }

        #endregion
    }
}
