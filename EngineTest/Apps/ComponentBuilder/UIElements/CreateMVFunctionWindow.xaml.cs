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
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;

using ComponentBuilder.WinUtils;
using ParameterStructure.Values;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for CreateMVFunctionWindow.xaml
    /// </summary>
    public partial class CreateMVFunctionWindow : Window
    {
        public CreateMVFunctionWindow()
        {
            InitializeComponent();

            this.funct_field.PropertyChanged += funct_field_PropertyChanged;

            this.btn_Add_Point.Command = new RelayCommand((x) => OnAddPoint());
            this.btn_finalize_fct.Command = new RelayCommand((x) => OnAddPointAndFinalize());
            this.btn_refresh_bounds.Command = new RelayCommand((x) => OnUpdateBounds());

            this.EscapeCmd = new RelayCommand((x) => OnEscape());
            this.DeleteCmd = new RelayCommand((x) => OnDelete());
        }

        #region STATIC

        private static readonly IFormatProvider FORMATTER = new NumberFormatInfo();

        #endregion

        #region CLASS MEMBERS / PROPERTIES
        public MultiValueFactory MVFactory { get; set; }
        private MultiValueFunction to_edit = null;
        private bool in_edit_mode = false;
        public ICommand EscapeCmd { get; private set; }
        public ICommand DeleteCmd { get; private set; }

        #endregion

        #region COMMANDS

        private void OnUpdateBounds()
        {
            double x_min, x_max, y_min, y_max;
            bool success_x_min = double.TryParse(this.in_bound_x_min.Text, NumberStyles.Float, FORMATTER, out x_min);
            bool success_x_max = double.TryParse(this.in_bound_x_max.Text, NumberStyles.Float, FORMATTER, out x_max);
            bool success_y_min = double.TryParse(this.in_bound_y_min.Text, NumberStyles.Float, FORMATTER, out y_min);
            bool success_y_max = double.TryParse(this.in_bound_y_max.Text, NumberStyles.Float, FORMATTER, out y_max);
            if (!success_x_min || !success_x_max || !success_y_min || !success_y_max) return;

            this.funct_field.Bounds = new System.Windows.Media.Media3D.Point4D(x_min, x_max, y_min, y_max);
        }

        private void OnAddPoint()
        {
            double x, y;
            bool success_x = double.TryParse(this.in_Px.Text, NumberStyles.Float, FORMATTER, out x);
            bool success_y = double.TryParse(this.in_Py.Text, NumberStyles.Float, FORMATTER, out y);
            if (!success_x || !success_y) return;

            this.funct_field.NewPoint = new Point(x, y);
        }

        private void OnAddPointAndFinalize()
        {
            this.funct_field.FunctionName = this.in_GrName.Text;
            this.funct_field.FinalizeFunction = true;
        }

        private void OnEscape()
        {
            this.funct_field.Deselect();
        }

        private void OnDelete()
        {
            this.funct_field.DeleteSelcted();
        }

        #endregion

        #region METHODS: Realize in the input

        internal void FillInput(MultiValueFunction _to_edit)
        {
            if (_to_edit == null) return;
            this.to_edit = _to_edit;
            this.in_edit_mode = true;

            // general info and structure
            this.in_unitX.Text = _to_edit.MVUnitX;
            this.in_unitY.Text = _to_edit.MVUnitY;
            this.in_unitZ.Text = _to_edit.MVUnitZ;

            int nr_z = (_to_edit.NrZ < 1) ? 1 : _to_edit.NrZ;
            this.in_nrZ.Text = nr_z.ToString();
            BindingExpression in_nrZ_be = this.in_nrZ.GetBindingExpression(TextBox.TextProperty);
            if (in_nrZ_be != null)
                in_nrZ_be.UpdateSource();

            this.in_Name.Text = _to_edit.MVName;

            // fill in the info cells (along the axes) and draw the function graphs (inside each table)
            this.funct_field.FillInGraphInfoFromExisting(_to_edit.MinX, _to_edit.MaxX, _to_edit.MinY, _to_edit.MaxY,
                                                            _to_edit.Zs, _to_edit.Graphs, _to_edit.Graph_Names);
        }

        #endregion

        #region EVENT HANDLERS

        private void funct_field_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Bounds")
            {
                this.in_bound_x_min.Text = this.funct_field.Bounds.X.ToString("F2", FORMATTER);
                this.in_bound_x_max.Text = this.funct_field.Bounds.Y.ToString("F2", FORMATTER);
                this.in_bound_y_min.Text = this.funct_field.Bounds.Z.ToString("F2", FORMATTER);
                this.in_bound_y_max.Text = this.funct_field.Bounds.W.ToString("F2", FORMATTER);
            }
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            // get the data from the containers
            Point4D bounds;
            List<double> zs;
            List<List<Point3D>> functions;
            List<string> fct_names;
            if (this.MVFactory != null && this.funct_field != null)
            {
                this.funct_field.AssembleFieldInfo(out bounds, out zs, out functions, out fct_names);
                if (this.in_edit_mode && this.to_edit != null)
                {
                    // TODO: save after edit
                    this.MVFactory.UpdateFunction(this.to_edit, this.in_unitX.Text, this.in_unitY.Text, this.in_unitZ.Text,
                                                    bounds, zs, functions, fct_names, this.in_Name.Text);
                    this.in_edit_mode = false;
                }
                else
                {
                    MultiValueFunction created = this.MVFactory.CreateFunction(this.in_unitX.Text, this.in_unitY.Text, this.in_unitZ.Text,
                                                                                    bounds, zs, functions, fct_names, this.in_Name.Text);
                }
            }

            this.DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
