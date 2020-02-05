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
using System.ComponentModel;

using ComponentBuilder.WinUtils;
using ComponentBuilder.WpfUtils;

using ParameterStructure.Component;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for CompCompareWindow.xaml
    /// </summary>
    public partial class CompCompareWindow : Window, INotifyPropertyChanged
    {
        public CompCompareWindow(ComponentManagerType _user)
        {
            this.user = _user;
            InitializeComponent();
            this.InitControls();
            this.Closed += CompCompareWindow_Closed;
        }


        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region CLASS MEMBERS

        private ComponentManagerType user;

        #endregion

        #region PROPERTIES

        private ParameterStructure.Component.Component c_1;
        public ParameterStructure.Component.Component C1
        {
            get { return this.c_1; }
            set 
            {
                if (!(value.IsLocked))
                {
                    if (this.c_1 != null)
                        this.c_1.CompareContainedPCWith_RemoveMarkings();
                    this.c_1 = value;
                    this.RegisterPropertyChanged("C1");
                    this.IsPickingC1 = false;
                    this.UpdateComponentContent();
                }
            }
        }

        private bool is_picking_c_1;
        public bool IsPickingC1
        {
            get { return this.is_picking_c_1; }
            private set
            {
                this.is_picking_c_1 = value;
                this.RegisterPropertyChanged("IsPickingC1");
                this.UpdateButtons();
            }
        }

        private ParameterStructure.Component.Component c_2;
        public ParameterStructure.Component.Component C2
        {
            get { return this.c_2; }
            set
            {
                if (!(value.IsLocked))
                {
                    if (this.c_2 != null)
                        this.c_2.CompareContainedPCWith_RemoveMarkings();
                    this.c_2 = value;
                    this.RegisterPropertyChanged("C2");
                    this.IsPickingC2 = false;
                    this.UpdateComponentContent();
                }
            }
        }

        private bool is_picking_c_2;
        public bool IsPickingC2
        {
            get { return this.is_picking_c_2; }
            private set
            {
                this.is_picking_c_2 = value;
                this.RegisterPropertyChanged("IsPickingC2");
                this.UpdateButtons();
            }
        }

        #endregion

        #region METHODS: Init, Update

        private void InitControls()
        {
            this.IsPickingC1 = false;
            this.IsPickingC2 = false;

            this.tbtn_get_C1.Command = new RelayCommand((x) => this.IsPickingC1 = !(this.IsPickingC1),
                                                        (x) => this.IsPickingC2 == false);
            this.tbtn_get_C1.IsChecked = (this.IsPickingC1 == true && this.IsPickingC2 == false);

            this.tbtn_get_C2.Command = new RelayCommand((x) => this.IsPickingC2 = !(this.IsPickingC2),
                                                        (x) => this.IsPickingC1 == false);
            this.tbtn_get_C2.IsChecked = (this.IsPickingC1 == false && this.IsPickingC2 == true);

            this.btn_OK.Command = new RelayCommand((x) => this.OnCompareCmd(),
                                                   (x) => this.C1 != null && this.C2 != null);
        }

        private void UpdateButtons()
        {
            this.tbtn_get_C1.IsChecked = (this.IsPickingC1 == true && this.IsPickingC2 == false);
            this.tbtn_get_C2.IsChecked = (this.IsPickingC1 == false && this.IsPickingC2 == true);
        }

        private void OnCompareCmd()
        {
            this.C1.CompareContainedPCWith_RemoveMarkings();
            this.C2.CompareContainedPCWith_RemoveMarkings();
            this.C1.CompareContainedPCWith(this.C2);
            this.UpdateComponentContent();
        }

        private void UpdateComponentContent()
        {
            if (this.c_1 != null)
                this.tve_C1.ItemsSource = new List<ParameterStructure.Component.Component> { this.c_1 };
            else
                this.tve_C1.ItemsSource = new List<ParameterStructure.Component.Component>();

            if (this.c_2 != null)
                this.tve_C2.ItemsSource = new List<ParameterStructure.Component.Component> { this.c_2 };
            else
                this.tve_C2.ItemsSource = new List<ParameterStructure.Component.Component>();

            this.tve_C1.User = this.user;
            this.tve_C2.User = this.user;

            this.tve_C1.PropagateCompInfoToItems(this.c_2, this);
            this.tve_C2.PropagateCompInfoToItems(this.c_1, this);
        }

        #endregion

        #region EVENT HANDLER

        private void CompCompareWindow_Closed(object sender, EventArgs e)
        {
            if (this.C1 != null)
                this.C1.CompareContainedPCWith_RemoveMarkings();
            if (this.C2 != null)
                this.C2.CompareContainedPCWith_RemoveMarkings();
        }

        #endregion
    }
}
