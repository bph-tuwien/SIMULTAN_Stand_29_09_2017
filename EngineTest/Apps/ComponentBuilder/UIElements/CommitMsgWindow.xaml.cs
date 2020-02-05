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

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for CommitMsgWindow.xaml
    /// </summary>
    public partial class CommitMsgWindow : Window
    {
        public CommitMsgWindow()
        {
            InitializeComponent();
            this.Loaded += CommitMsgWindow_Loaded;
        }

        #region PROPETIES

        private string author_of_msg;
        public string AuthorOfMsg
        {
            get { return this.author_of_msg; }
            set
            { 
                this.author_of_msg = value;
                this.tb_author.Text = this.author_of_msg;
            }
        }

        public string Message { get; private set; }

        #endregion

        private void InitContent()
        {
            this.btn_OK.Click += btn_OK_Click;
            this.btn_OK.Focus();
        }
        

        #region EVENT HANDLER
        private void CommitMsgWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitContent();
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Message = this.tb_msg.Text;
            // done
            this.DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
