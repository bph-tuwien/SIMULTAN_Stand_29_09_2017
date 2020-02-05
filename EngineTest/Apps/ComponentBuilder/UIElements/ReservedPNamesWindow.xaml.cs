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

using ParameterStructure.Parameter;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for ReservedPNamesWindow.xaml
    /// </summary>
    public partial class ReservedPNamesWindow : Window
    {
        public ReservedPNamesWindow()
        {
            InitializeComponent();
        }

        #region PROPERTIES

        private List<ParameterReservedNameRecord> record;
        public List<ParameterReservedNameRecord> Record
        {
            get { return this.record; }
            set 
            {
                this.record = value;
                this.list.ItemsSource = new List<ParameterReservedNameRecord>(this.record);
            }
        }


        #endregion
    }
}
