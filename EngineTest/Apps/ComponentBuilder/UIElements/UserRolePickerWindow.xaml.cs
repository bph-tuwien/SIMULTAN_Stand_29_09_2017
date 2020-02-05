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

using ParameterStructure.Component;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for UserRolePickerWindow.xaml
    /// </summary>
    public partial class UserRolePickerWindow : Window
    {
        public UserRolePickerWindow()
        {
            InitializeComponent();
        }

        #region GIT Info
        private string git_msg_short;

        public string GitMsgShort
        {
            get { return this.git_msg_short; }
            set
            {
                this.git_msg_short = value;
                this.tb_git_msg_short.Text = this.git_msg_short.Trim();
            }
        }


        private string git_msg_long;

        public string GitMsgLong
        {
            get { return this.git_msg_long; }
            set 
            { 
                this.git_msg_long = value;
                this.tb_git_msg_long.Text = this.git_msg_long.Trim();
            }
        }

        private bool git_ok;

        public bool GitOK
        {
            get { return this.git_ok; }
            set 
            { 
                this.git_ok = value;
                if (this.git_ok)
                {
                    this.img_git_ok.Source = new BitmapImage(new Uri(@"../Data/icons/btn_OK.png", UriKind.Relative));
                    this.tb_git_msg_short.Foreground = new SolidColorBrush(Colors.White);
                    this.tb_git_msg_long.Foreground = new SolidColorBrush(Colors.White);
                }
                else
                {
                    this.img_git_ok.Source = new BitmapImage(new Uri(@"../Data/icons/btn_WARN.png", UriKind.Relative));
                    this.tb_git_msg_short.Foreground = new SolidColorBrush(Colors.Yellow);
                    this.tb_git_msg_long.Foreground = new SolidColorBrush(Colors.Yellow);
                }
            }
        }

        #endregion
        public ComponentManagerType UserProfile { get; private set; }
        public bool UserProfileSelected { get; private set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.UserProfileSelected = false;
            if (sender == null) return;

            Button btn = sender as Button;
            if (btn == null) return;
            if (btn.Tag == null) return;

            this.UserProfile = ComponentUtils.StringToComponentManagerType(btn.Tag.ToString());
            this.UserProfileSelected = true;
            this.Close();
        }
    }
}
