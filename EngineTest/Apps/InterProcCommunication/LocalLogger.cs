using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using System.IO;
using System.Diagnostics;

namespace InterProcCommunication
{
    #region HELPER ENUMS / CLASSES
    public enum LoggerEntryType
    {
        INFO = 0,
        WARNING = 1,
        ERROR = 2,
        COMM = 3,
        SERVER = 4,
        CLIENT = 5
    }

    [ValueConversion(typeof(LoggerEntryType), typeof(bool))]
    public class LoggerEntryTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is LoggerEntryType) || parameter == null)
                return false;

            string param_str = parameter.ToString();
            switch (param_str)
            {
                case "INFO":
                    return (LoggerEntryType)value == LoggerEntryType.INFO;
                case "WARNING":
                    return (LoggerEntryType)value == LoggerEntryType.WARNING;
                case "ERROR":
                    return (LoggerEntryType)value == LoggerEntryType.ERROR;
                case "COMM":
                    return (LoggerEntryType)value == LoggerEntryType.COMM;
                case "SERVER":
                    return (LoggerEntryType)value == LoggerEntryType.SERVER;
                case "CLIENT":
                    return (LoggerEntryType)value == LoggerEntryType.CLIENT;
                default:
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    [ValueConversion(typeof(LoggerEntryType), typeof(System.Windows.Media.Color))]
    public class LoggerEntryTypeToWColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is LoggerEntryType))
                return (Color)ColorConverter.ConvertFromString("#ffffffff");

            LoggerEntryType type = (LoggerEntryType)value;

            switch (type)
            {
                case LoggerEntryType.INFO:
                    return (Color)ColorConverter.ConvertFromString("#ffffffff");
                case LoggerEntryType.WARNING:
                    return (Color)ColorConverter.ConvertFromString("#ffffff00");
                case LoggerEntryType.ERROR:
                    return (Color)ColorConverter.ConvertFromString("#ffff4500");
                case LoggerEntryType.COMM:
                    return (Color)ColorConverter.ConvertFromString("#ff00ffff");
                case LoggerEntryType.SERVER:
                    return (Color)ColorConverter.ConvertFromString("#ff0000ff");
                case LoggerEntryType.CLIENT:
                    return (Color)ColorConverter.ConvertFromString("#ff0099ff");
                default:
                    return (Color)ColorConverter.ConvertFromString("#ffffffff");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
    }

    public class LoggerEntry
    {
        public string Prefix { get; private set; }
        public string Text { get; private set; }
        public LoggerEntryType Type { get; private set; }

        public LoggerEntry(string _prefix, string _content, LoggerEntryType _type)
        {
            this.Prefix = _prefix;
            this.Text = _content;
            this.Type = _type;
        }
    }

    #endregion
    public class LocalLogger : INotifyPropertyChanged
    {
        #region STATIC

        public const double TIME_BTW_CLEANUPS = 10000.0; // miliseconds
        public const int MAX_NR_ENTRIES = 32;
        private const string DEFAULT_LOG_FILE_NAME = "SIMULTAN_LOG";

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region PROPERTIES

        private bool new_entry_to_display;
        public bool NewEntryToDisplay
        {
            get { return this.new_entry_to_display; }
            private set
            {
                this.new_entry_to_display = value;
                this.RegisterPropertyChanged("NewEntryToDisplay");
            }
        }

        private List<LoggerEntry> entries;
        public IList<LoggerEntry> Entries { get { return this.entries.AsReadOnly(); } }

        public string Prefix { get; set; }

        #endregion

        #region CLASS MEMBERS

        private DateTime last_cleanup;

        #endregion

        #region .CTOR

        public LocalLogger(string _prefix)
        {
            this.last_cleanup = DateTime.Now;
            this.Prefix = (string.IsNullOrEmpty(_prefix)) ? "Anonym" : _prefix;
            this.entries = new List<LoggerEntry>();
            string prefix_current = DateTime.Now.ToString("HH:mm:ss") + " " + this.Prefix + ": ";
            this.entries.Add(new LoggerEntry(prefix_current, "Start der Sitzung", LoggerEntryType.INFO));
            this.NewEntryToDisplay = true;
        }

        #endregion

        #region METHODS: Logging

        public void LogInfo(string _input)
        {
            this.LogMessage(_input, LoggerEntryType.INFO);
        }

        public void LogWarning(string _input)
        {
            this.LogMessage(_input, LoggerEntryType.WARNING);
        }

        public void LogError(string _input)
        {
            this.LogMessage(_input, LoggerEntryType.ERROR);
        }

        private void LogMessage(string _input, LoggerEntryType _type)
        {
            string prefix_current = DateTime.Now.ToString("HH:mm:ss") + " " + this.Prefix + ": ";
            this.entries.Add(new LoggerEntry(prefix_current, _input, _type));
            this.NewEntryToDisplay = true;
            this.CleanUp();
        }

        private void CleanUp()
        {
            if (this.entries.Count <= LocalLogger.MAX_NR_ENTRIES)
                return;

            TimeSpan time_since_last_cleanup = DateTime.Now - this.last_cleanup;
            if (time_since_last_cleanup.TotalMilliseconds < LocalLogger.TIME_BTW_CLEANUPS)
                return;

            this.entries.RemoveRange(0, this.entries.Count - LocalLogger.MAX_NR_ENTRIES);

            this.last_cleanup = DateTime.Now;
            this.NewEntryToDisplay = false;
        }

        #endregion

        #region METHODS: Merging Logs

        public void MergeLast(LocalLogger _other_logger)
        {
            if (_other_logger == null) return;

            try
            {
                LoggerEntry last = _other_logger.entries.Last();
                this.entries.Add(last);
                this.NewEntryToDisplay = true;
                this.CleanUp();
            }
            catch
            {
                // it does not matter, just carry on
            }
            
        }

        #endregion

        #region Inter-Process Communication Logging

        public void LogAsProcess(string _input, params object[] _p)
        {
            this.LogCommunication(this.AssembleMessage(_input, _p), LoggerEntryType.INFO);
        }

        public void LogCommUnit(string _input, params object[] _p)
        {
            this.LogCommunication(this.AssembleMessage(_input, _p), LoggerEntryType.COMM);
        }

        public void LogServer(string _input, params object[] _p)
        {
            this.LogCommunication(this.AssembleMessage(_input, _p), LoggerEntryType.SERVER);
        }

        public void LogClient(string _input, params object[] _p)
        {
            this.LogCommunication(this.AssembleMessage(_input, _p), LoggerEntryType.CLIENT);
        }

        private void LogCommunication(string _input, LoggerEntryType _type)
        {
            int cp_id = Process.GetCurrentProcess().Id;
            int ct_id = Thread.CurrentThread.ManagedThreadId;

            string prefix_current = DateTime.Now.ToString("HH:mm:ss") + " " + this.Prefix + ": -[" + cp_id + "." + ct_id + "]- ";
            this.entries.Add(new LoggerEntry(prefix_current, _input, _type));
            this.NewEntryToDisplay = true;
            this.CleanUp();
        }

        #endregion

        #region UTILS

        private string AssembleMessage(string _msg, params object[] _p)
        {
            string message = _msg;
            if (_p == null) return message;
            if (_p.Length == 0) return message;

            for (int i = 0; i < _p.Length; i++)
            {
                if (_p[i] == null)
                    continue;
                message = message.Replace("{" + i.ToString() + "}", _p[i].ToString());
            }
            return message;
        }


        #endregion

        #region METHODS: Saving

        public void SaveLogToFile(string _note)
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = true,
                    FileName = LocalLogger.DEFAULT_LOG_FILE_NAME, // Default file name
                    DefaultExt = ".txt", // Default file extension
                    Filter = "text files|*.txt" // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    string content = this.CreateFileContent(_note);
                    using (FileStream fs = File.Create(dlg.FileName))
                    {
                        byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                        fs.Write(content_B, 0, content_B.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving Log Entries", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        private string CreateFileContent(string _note)
        {
            // create the export string: gather entries
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Simultan Log File: " + _note);
            sb.AppendLine("Es werden maximal 32 Einträge gespeichert:");
            sb.AppendLine();
            foreach (LoggerEntry e in this.entries)
            {
                sb.AppendLine(e.Prefix + " " + Enum.GetName(typeof(LoggerEntryType), e.Type) + " " + e.Text);
            }
            return sb.ToString();
        }


        #endregion
    }
}
