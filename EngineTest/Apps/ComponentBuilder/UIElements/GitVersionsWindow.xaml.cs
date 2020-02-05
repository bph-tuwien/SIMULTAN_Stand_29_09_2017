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
    public class CommitInfo
    {
        public string Key { get; private set; }
        public string ChangedFile { get; private set; }
        public string ChangeLog { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public string TimeStampAsStr { get { return this.TimeStamp.ToLongDateString(); } }
        public string AuthorName { get; private set; }
        public bool IsCurrent { get; private set; }

        public CommitInfo(string _key, string _changed_file, string _change_log, DateTime _time_stamp, string _author, bool _is_current)
        {
            this.Key = _key;
            this.ChangedFile = _changed_file;
            this.ChangeLog = _change_log;
            this.TimeStamp = _time_stamp;
            this.AuthorName = _author;
            this.IsCurrent = _is_current;
        }

        public override string ToString()
        {
            return this.Key + ": " + this.TimeStamp.ToLongDateString();
        }
    }


    /// <summary>
    /// Interaction logic for GitVersionsWindow.xaml
    /// </summary>
    public partial class GitVersionsWindow : Window
    {
        public GitVersionsWindow()
        {
            InitializeComponent();
            this.Loaded += GitVersionsWindow_Loaded;
        }


        #region PROPERTIES

        private List<CommitInfo> records_to_show;
        public List<CommitInfo> RecordsToShow
        {
            get { return this.records_to_show; }
            set
            {
                this.records_to_show = value;
                this.lv_versions.ItemsSource = this.records_to_show;
            }
        }

        public string KeyToReturnTo { get; private set; }

        #endregion

        private void InitContent()
        {
            this.btn_OK.Click += btn_OK_Click;
            this.btn_Cancel.Click += btn_Cancel_Click;
            this.lv_versions.SelectionChanged += lv_versions_SelectionChanged;
        }

        #region EVENT HANDLER

        private void GitVersionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitContent();
        }

        private void lv_versions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null || e == null) return;

            ListView lv = sender as ListView;
            if (lv == null) return;

            CommitInfo sel_item = lv.SelectedItem as CommitInfo;
            if (sel_item != null)
            {
                this.tb_author.Text = sel_item.AuthorName;
                this.tb_file_name.Text = sel_item.ChangedFile;
                this.tb_time.Text = sel_item.TimeStamp.ToString();
                this.tb_message.Text = sel_item.ChangeLog;
                this.tb_key.Text = sel_item.Key;
            }
            else
            {
                this.tb_author.Text = string.Empty;
                this.tb_file_name.Text = string.Empty;
                this.tb_time.Text = string.Empty;
                this.tb_message.Text = string.Empty;
                this.tb_key.Text = string.Empty;
            }
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            if (this.lv_versions != null)
            {
                CommitInfo selected = this.lv_versions.SelectedItem as CommitInfo;
                if (selected != null)
                {
                    this.KeyToReturnTo = selected.Key;
                }
            }

            // done
            this.DialogResult = true;
            this.Close();
        }

        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.KeyToReturnTo = string.Empty;
        }

        #endregion
    }
}
