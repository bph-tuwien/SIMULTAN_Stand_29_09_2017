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

using DataStructVisualizer.UIElements;
using DataStructVisualizer.ClassGenerator;

namespace DataStructVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.classGen = new ClassGenerator.ClassGenerator();
        }

        #region EXCEL IMPORT

        private TableNameWin tnWindow;

        public string OpenTableNameWindow()
        {
            this.tnWindow = new TableNameWin();
            this.tnWindow.ShowDialog();
            return this.tnWindow.TableName;
        }

        #endregion

        #region C_SHARP EXPORT

        private ClassPreview cpWindow;
        private ClassGenerator.ClassGenerator classGen;

        public void OpenClassPreviewWindow(Nodes.Node n)
        {
            bool success = this.classGen.GenerateClassText(n, ClassGenerator.ClassGenerator.DEFAULT_NAMESPACE);
            if (!success)
            {
                MessageBox.Show("A class or enum by the same name exists already!", "Warning Generating Class Text",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.cpWindow = new ClassPreview(this.classGen);
            this.cpWindow.NodeInPreview = n;
            this.cpWindow.ShowDialog();
        }


        #endregion


    }
}
