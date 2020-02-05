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
    /// Interaction logic for ShowMVTableWindow.xaml
    /// </summary>
    public partial class ShowMVTableWindow : Window
    {
        public ShowMVTableWindow()
        {
            InitializeComponent();
        }

        #region CLASS MEMBERS / PROPERTIES

        private MultiValueTable data;
        public MultiValueTable Data
        {
            get { return this.data; }
            set
            {
                this.data = value;
                this.value_field.DataField = this.data;
                this.chb_interp.IsChecked = this.data.MVCanInterpolate;
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
