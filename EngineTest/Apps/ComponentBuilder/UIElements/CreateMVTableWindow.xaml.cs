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

using ParameterStructure.Values;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for CreateMVTableWindow.xaml
    /// </summary>
    public partial class CreateMVTableWindow : Window
    {
        public CreateMVTableWindow()
        {
            InitializeComponent();
        }

        #region CLASS MEMBERS / PROPERTIES

        public MultiValueFactory MVFactory { get; set; }
        private MultiValueTable to_edit = null;
        private bool in_edit_mode = false;

        #endregion

        #region METHODS: Realize in the input

        internal void FillInput(MultiValueTable _to_edit)
        {
            if (_to_edit == null) return;
            this.to_edit = _to_edit;
            this.in_edit_mode = true;

            // general info and structure
            int nr_y = (_to_edit.NrY < 1) ? 1 : _to_edit.NrY;
            int nr_z = (_to_edit.NrZ < 1) ? 1 : _to_edit.NrZ;

            this.in_nrZ.Text = nr_z.ToString();
            BindingExpression in_nrZ_be = this.in_nrZ.GetBindingExpression(TextBox.TextProperty);
            if (in_nrZ_be != null)
                in_nrZ_be.UpdateSource();
            this.in_unitZ.Text = _to_edit.MVUnitZ;

            this.in_nrX.Text = _to_edit.NrX.ToString();
            BindingExpression in_nrX_be = this.in_nrX.GetBindingExpression(TextBox.TextProperty);
            if (in_nrX_be != null)
                in_nrX_be.UpdateSource();
            this.in_unitX.Text = _to_edit.MVUnitX;

            this.in_nrY.Text = nr_y.ToString();
            BindingExpression in_nrY_be = this.in_nrY.GetBindingExpression(TextBox.TextProperty);
            if (in_nrY_be != null)
                in_nrY_be.UpdateSource();
            this.in_unitY.Text = _to_edit.MVUnitY;

            this.in_Name.Text = _to_edit.MVName;

            this.chb_interp.IsChecked = _to_edit.MVCanInterpolate;

            // fill in the info cells (along the axes)
            this.value_field.FillInfoCellsFromExisting(_to_edit.Xs, _to_edit.Ys, _to_edit.Zs);
            // fill in the data cells (inside each table)
            this.value_field.FillDataCellsFromExisting(_to_edit.Field);
        }

        #endregion

        #region EVENT HANDLERS

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            // get the data from the containers
            List<double> xs, ys, zs;
            List<List<double>> Fxyzs;
            if (this.MVFactory != null && this.value_field != null)
            {
                this.value_field.AssembleFieldInfo(out xs, out ys, out zs, out Fxyzs);
                List<double> Fxyzs_flat = new List<double>();
                foreach(List<double> entry in Fxyzs)
                {
                    Fxyzs_flat.AddRange(entry);
                }

                // update or save new
                if (this.in_edit_mode && this.to_edit != null)
                {
                    this.MVFactory.UpdateValueTable(this.to_edit,
                                                    xs, this.in_unitX.Text,
                                                    ys, this.in_unitY.Text,
                                                    zs, this.in_unitZ.Text,
                                                    Fxyzs_flat, this.chb_interp.IsChecked.Value, this.in_Name.Text);
                    this.in_edit_mode = false;
                }
                else
                {
                    MultiValueTable created = this.MVFactory.CreateValueTable(xs, this.in_unitX.Text,
                                                                              ys, this.in_unitY.Text,
                                                                              zs, this.in_unitZ.Text,
                                                                              Fxyzs_flat, this.chb_interp.IsChecked.Value, this.in_Name.Text);
                }

            }

            this.DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
