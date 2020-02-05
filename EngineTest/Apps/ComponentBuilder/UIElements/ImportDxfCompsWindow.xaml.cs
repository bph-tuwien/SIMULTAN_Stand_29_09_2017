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
using System.Windows.Shapes;
using System.IO;

using ComponentBuilder.WinUtils;

using ParameterStructure.DXF;
using ParameterStructure.Component;
using ParameterStructure.Parameter;
using ParameterStructure.Values;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for ImportDxfCompsWindow.xaml
    /// </summary>
    public partial class ImportDxfCompsWindow : Window
    {
        public ImportDxfCompsWindow()
        {
            InitializeComponent();
            this.InitContent();
            this.Unloaded += ImportDxfCompsWindow_Unloaded; // added 14.09.2016
        }

        private void ImportDxfCompsWindow_Unloaded(object sender, EventArgs e)
        {
            ComponentFactory.DiscardFactory(this.COMPFactory); // added 14.09.2016
        }

        #region CLASS MEMBERS

        private ComponentFactory COMPFactory;
        private ParameterFactory PFactory;
        private MultiValueFactory MVFactory;

        private List<MultiValue> existing_values;

        #endregion

        #region PROPERTIES

        public List<Component> MarkedForImport { get; private set; }

        #endregion

        private void InitContent()
        {
            // data managers
            this.COMPFactory = new ComponentFactory(ComponentManagerType.ADMINISTRATOR);
            this.PFactory = new ParameterFactory();
            this.MVFactory = new MultiValueFactory();
            this.MarkedForImport = new List<Component>();

            // commands: main function
            this.btn_import.Command = new RelayCommand((x) => ImportComponentsFromDXF(),
                                            (x) => CanExecute_ImportComponentsFromDXF());
            this.btn_import_MV.Command = new RelayCommand((x) => ImportMultiValueFiledsFromDXF(),
                                               (x) => CanExecute_ImportMultiValueFiledsFromDXF());
            this.btn_OK.Command = new RelayCommand((x) => PepareToExportToProject(),
                                        (x) => CanExecute_PepareToExportToProject());
            // commands: display
            this.btn_unfold_all.Command = new RelayCommand((x) =>
            {
                this.COMPFactory.ExpandAll();
                this.tve_components.ItemsSource = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
            });
            this.btn_unfold_selected.Command = new RelayCommand((x) => 
            {
                Component selcomp = this.tve_components.SelectedItem_ as Component;
                this.COMPFactory.ExpandComp(selcomp);
                this.tve_components.ItemsSource = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);  
            },
            (x) => this.tve_components.SelectedItem_ != null);
            this.btn_collapse.Command = new RelayCommand((x) =>
            {
                this.COMPFactory.CollapseAll();
                this.tve_components.ItemsSource = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
            });
            this.btn_mark_all.Command = new RelayCommand((x) => this.COMPFactory.MarkAll(true),
                                                         (x) => this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0);
            this.btn_unmarkall.Command = new RelayCommand((x) => this.COMPFactory.MarkAll(false),
                                                        (x) => this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count > 0);
            this.btn_mark_w_refs.Command = new RelayCommand((x) => 
            {
                Component selcomp = this.tve_components.SelectedItem_ as Component;
                this.COMPFactory.MarkWReferences(selcomp);
            },
            (x) => this.COMPFactory != null && this.tve_components.SelectedItem_ != null);
        }

        #region COMMANDS: Import from DXF

        private void ImportMultiValueFiledsFromDXF()
        {
            // 1. STEP: Import the value fields that are used by the component parameters
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_MULTIVALUES,
                    Title = "Importing value fields..."
                };

                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        //imports the DXF file
                        DXFDecoder dxf_decoder = new DXFDecoder(this.MVFactory);
                        dxf_decoder.LoadFromFile(dlg.FileName);
                        this.existing_values = new List<MultiValue>(this.MVFactory.ValueRecord);
                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "DXF Value Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "DXF Value Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (this.MVFactory.ValueRecord.Count == 0)
            {
                MessageBox.Show("No value fields imported!", "DXF Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private bool CanExecute_ImportMultiValueFiledsFromDXF()
        {
            return (this.MVFactory != null && this.MVFactory.ValueRecord.Count == 0);
        }


        private void ImportComponentsFromDXF()
        {
            // 2. STEP: Import the Components
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog()
                {
                    Filter = "dxf files|*." + ParamStructFileExtensions.FILE_EXT_COMPONENTS,
                    Title = "Importing components..."
                };
                
                if (dlg.ShowDialog().Value)
                {
                    if (File.Exists(dlg.FileName))
                    {
                        //imports the DXF file
                        DXFDecoder dxf_decoder = new DXFDecoder(this.MVFactory, this.PFactory, this.COMPFactory);
                        dxf_decoder.LoadFromFile(dlg.FileName);
                        this.COMPFactory.RestoreReferencesWithinRecord();
                        this.tve_components.ItemsSource = new List<ParameterStructure.Component.Component>(this.COMPFactory.ComponentRecord);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "DXF Component Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "DXF Component Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private bool CanExecute_ImportComponentsFromDXF()
        {
            return (this.MVFactory.ValueRecord.Count > 0 && this.COMPFactory != null && this.COMPFactory.ComponentRecord.Count == 0);
        }


        #endregion

        #region COMMANDS: Export To Project

        private void PepareToExportToProject()
        {
            List<Component> to_copy = new List<Component>();
            foreach(Component c in this.COMPFactory.ComponentRecord)
            {
                if (c.IsMarked)
                    to_copy.Add(c);
            }
            this.MarkedForImport = ComponentFactory.CopyComponents(to_copy);

            // done
            this.DialogResult = true;
            this.Close();
        }

        private bool CanExecute_PepareToExportToProject()
        {
            return (this.COMPFactory != null && this.COMPFactory.GetNrMarkedRecords() > 0);
        }

        #endregion
    }
}
