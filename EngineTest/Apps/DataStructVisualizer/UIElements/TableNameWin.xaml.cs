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

namespace DataStructVisualizer.UIElements
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class TableNameWin : Window
    {
        public string TableName;
        public TableNameWin()
        {
            InitializeComponent();
            this.TableName = "Tabelle1";
        }

        private void txb_table_name_KeyUp(object sender, KeyEventArgs e)
        {
            this.TableName = txb_table_name.Text;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

    }
}
