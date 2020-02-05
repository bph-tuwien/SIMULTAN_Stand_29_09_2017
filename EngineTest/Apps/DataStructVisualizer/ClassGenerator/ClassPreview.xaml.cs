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
using Microsoft.CSharp;
using System.IO;

using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;
using DataStructVisualizer.Nodes;
using ClassGenerator.CodeSnippets;

namespace DataStructVisualizer.ClassGenerator
{
    internal struct KeyWordPosition
    {
        public TextPointer start_pos;
        public TextPointer end_pos;
        public string keyword;

        public override string ToString()
        {
            return "<" + keyword + ">";
        }
    }

    /// <summary>
    /// Interaction logic for ClassPreview.xaml
    /// </summary>
    public partial class ClassPreview : Window
    {

        #region STATIC

        private static char[] RESERVED_CHARS = new char[] { '.', ',', ';', ':', '(', ')', '{', '}', '[', ']', '<', '>', '\n', '\t', '\r', '=', '!', '?', '\\', '/', '|' };

        private static bool IsReservedChar(char _in)
        {
            return (ClassPreview.RESERVED_CHARS.Contains(_in) || Char.IsNumber(_in));
        }

        public static IEnumerable<string> ReadLines(string _path)
        {            
            using (StreamReader reader = File.OpenText(_path))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }           
        }

        #endregion

        #region PROPERTIES: Node, Checks for Repeats

        private Node node_in_preview;
        public Node NodeInPreview
        {
            get { return this.node_in_preview; }
            set
            {
                this.node_in_preview = value;
                if (this.node_in_preview != null)
                {
                    // set the name
                    this.tb_class_name.Text = this.node_in_preview.NodeName;
                    this.tb_namespace.Text = ClassGenerator.DEFAULT_NAMESPACE;
                    // the class generator was called by the main window
                    this.UpdateClassText(this.classGen.Snippets);
                    this.lv_existing_types.ItemsSource = this.classGen.ClassRecords;
                }
            }
        }

        private bool success_gen_class_text;
        public bool SuccessGeneratingClassText 
        {
            get { return this.success_gen_class_text; }
            private set
            {
                this.success_gen_class_text = value;
                if (!this.success_gen_class_text)
                {
                    MessageBox.Show("A class or enum by the same name exists already!", "Error Generating Class Text",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    this.DialogResult = true;
                    this.Close();
                }
            }
        }

        #endregion

        #region CLASS MEMBERS: general

        private ClassGenerator classGen;
        private string class_file_path;
        private string log_file_path;
        
        #endregion

        #region CLASS MEMBERS: highlighting keywords

        private CSharpCodeProvider csharpCP;
        private List<KeyWordPosition> keywords;
        private List<KeyWordPosition> specialwords;
        private int caret_offset_add;

        #endregion

        public ClassPreview(ClassGenerator _classGen)
        {
            InitializeComponent();

            this.classGen = _classGen;            
            this.class_file_path = null;
            this.log_file_path = null;
            this.SuccessGeneratingClassText = true;
            
            // for highlighting key wordsin text
            this.csharpCP = new CSharpCodeProvider();
            this.keywords = new List<KeyWordPosition>();
            this.specialwords = new List<KeyWordPosition>();
            this.caret_offset_add = -2;

            //// DEBUG
            //Regex rx_spaces = new Regex(@"^\s+$");
            //Regex rx_params_int = new Regex("ip[0-9]{1}");

            //// <-- assignment, --> goal
            //string test = "x <-- ip0 + ip1; x --> x < 10 || x > 20";
            //string ASSIGN = "<--";
            //string GOAL = "-->";
            //string DUMMY_PARAM = "INPUT";

            //// separate calculations
            //string[] delims = new string[] { ";" };
            //string[] calc_comps = test.Split(delims, StringSplitOptions.RemoveEmptyEntries);
            //string[] calc_comps_1 = calc_comps.Where(x => !rx_spaces.IsMatch(x)).ToArray();
            //string[] calc_comps_2 = calc_comps_1.Select(x => x.Trim()).ToArray();
            
            //// analyze first calculation
            //bool is_assign, is_goal;
            //int ind_assign = calc_comps_2[0].IndexOf(ASSIGN);
            //if (ind_assign > 0 && ind_assign < calc_comps_2[0].Length)
            //{
            //    string[] calc_i_comps = calc_comps_2[0].Split(new string[] {ASSIGN}, StringSplitOptions.RemoveEmptyEntries);
            //    string ME = calc_i_comps[0].Trim();
            //    string expression = rx_params_int.Replace(calc_i_comps[1].Trim(), DUMMY_PARAM);
            //}

            //// DEBUG

            // DEBUG 1

            // get geometric properties of a reference object (e.g. BOX_01)
            Matrix3D tr_WC2LC, tr_LC2WC;
            GeometryTransf.ComputePCATransf(GeometryTransf.BOX_01, out tr_WC2LC, out tr_LC2WC);
            List<Point3D> BOX_01_lc = GeometryTransf.TransformBy(GeometryTransf.BOX_01, tr_WC2LC);
            string BOX_01_lc_str = GeometryTransf.PointListToString(BOX_01_lc);
            
            // position a new object (e.g. BOX_02_wc) relative to the one above
            List<Point3D> BOX_02_wc = GeometryTransf.TransformBy(GeometryTransf.BOX_02_LC, tr_LC2WC);
            string BOX_02_wc_str = GeometryTransf.PointListToString(BOX_02_wc);

            // test test
            GeometryTransf.ComputePCATransf(GeometryTransf.OBJ_01, GeometryTransf.OBJ_01_CORRECTION, out tr_WC2LC, out tr_LC2WC);
            List<Point3D> OBJ_01_lc = GeometryTransf.TransformBy(GeometryTransf.OBJ_01, tr_WC2LC);
            string OBJ_01_lc_str = GeometryTransf.PointListToString(OBJ_01_lc);

            // DEBUG 1
        }

        #region CLASS METHODS: Class Text Update and Highlighting
        private void UpdateClassText(List<string> _snippets)
        {
            if (_snippets == null || _snippets.Count < 1) return;

            FlowDocument fD = new FlowDocument();

            int counter = 0;
            foreach (string snippet in _snippets)
            {
                Paragraph para = new Paragraph();
                if (counter % 2 == 0)
                {
                    para.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffececec"));
                    para.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff000000"));
                }
                else
                {
                    para.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffcbcbcb"));
                    para.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff000000"));
                }

                // highlighting key words in the event handler

                para.Inlines.Add(new Run(snippet));
                para.Margin = new Thickness(0);
                fD.Blocks.Add(para);
                counter++;
            }

            this.tb_class_text.Document = fD;
        }

        private void LookForKeyordsInRun(Run _run, string _text)
        {
            if (_run == null || _text == null || this.classGen == null || this.classGen.ClassText == null) return;

            // saving tags:
            int sIndex = 0;
            int eIndex = 0;

            for (int i = 0; i < _text.Length; i++)
            {
                if (char.IsWhiteSpace(_text[i]) || ClassPreview.IsReservedChar(_text[i]))
                {
                    if (i > 0 && !(char.IsWhiteSpace(_text[i - 1]) || ClassPreview.IsReservedChar(_text[i - 1])))
                    {
                        eIndex = i - 1;
                        string word = _text.Substring(sIndex, eIndex - sIndex + 1);                        
                        if (!string.IsNullOrEmpty(word) && !this.csharpCP.IsValidIdentifier(word))
                        {
                            KeyWordPosition kwp = new KeyWordPosition();
                            kwp.start_pos = _run.ContentStart.GetPositionAtOffset(sIndex, LogicalDirection.Forward);
                            kwp.end_pos = _run.ContentStart.GetPositionAtOffset(eIndex + 1, LogicalDirection.Backward);
                            kwp.keyword = word;
                            this.keywords.Add(kwp);
                        }
                        else
                        {
                            if (this.IsSpecialWord(word))
                            {
                                KeyWordPosition kwp = new KeyWordPosition();
                                kwp.start_pos = _run.ContentStart.GetPositionAtOffset(sIndex, LogicalDirection.Forward);
                                kwp.end_pos = _run.ContentStart.GetPositionAtOffset(eIndex + 1, LogicalDirection.Backward);
                                kwp.keyword = word;
                                this.specialwords.Add(kwp);
                            }
                        }
                    }
                    sIndex = i + 1;
                }

            }

            // look at last word too
            string word_last = _text.Substring(sIndex, _text.Length - sIndex);
            if (!string.IsNullOrEmpty(word_last) && !this.csharpCP.IsValidIdentifier(word_last))
            {
                KeyWordPosition kwp = new KeyWordPosition();
                kwp.start_pos = _run.ContentStart.GetPositionAtOffset(sIndex, LogicalDirection.Forward);
                kwp.end_pos = _run.ContentStart.GetPositionAtOffset(_text.Length, LogicalDirection.Backward);
                kwp.keyword = word_last;
                this.keywords.Add(kwp);
            }
            else
            {
                if (this.IsSpecialWord(word_last))
                {
                    KeyWordPosition kwp = new KeyWordPosition();
                    kwp.start_pos = _run.ContentStart.GetPositionAtOffset(sIndex, LogicalDirection.Forward);
                    kwp.end_pos = _run.ContentStart.GetPositionAtOffset(_text.Length, LogicalDirection.Backward);
                    kwp.keyword = word_last;
                    this.specialwords.Add(kwp);
                }
            }

        }

        private bool IsSpecialWord(string _word)
        {
            if (string.IsNullOrEmpty(_word)) return false;

            if (_word == this.tb_namespace.Text || _word == this.tb_class_name.Text ||
                ClassGenerator.RESERVED_STRINGS.Contains(_word))
                return true;
            else
                return false;
        }

        #endregion


        #region CLASS METHODS: Save Class File

        private void SetFilePath()
        {
            try
            {
                // Configure save file dialog box
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    OverwritePrompt = false,
                    FileName = this.classGen.ClassNameFull, // Default file name
                    DefaultExt = ".cs", // Default file extension
                    Filter = "c# files|*.cs" // Filter files by extension
                };

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result.HasValue && result == true)
                {
                    // set the log file path
                    this.log_file_path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dlg.FileName),
                                                                ClassGenerator.LOG_FILE_NAME);
                    this.tb_file_path_LOG.Text = this.log_file_path;
                    // load log entries
                    if (this.classGen.ClassRecords.Count == 0 && File.Exists(this.log_file_path))
                    {
                        bool success_reading_logged_records = true;
                        
                        foreach (string line in ClassPreview.ReadLines(this.log_file_path))
                        {
                            success_reading_logged_records &= this.classGen.ReadClassRecord(line);
                            if (!success_gen_class_text) break;
                        }
                        if (!success_gen_class_text)
                        {
                            MessageBox.Show("Log File contains duplicates of the currents class records!",
                                            "Error Synchronizing Records",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        this.lv_existing_types.ItemsSource = this.classGen.ClassRecords;
                    }
                    // insert the namespace as subdirectory
                    string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dlg.FileName), 
                                                         this.tb_namespace.Text,
                                                         System.IO.Path.GetFileName(dlg.FileName));
                    this.tb_file_path.Text = path;
                    this.class_file_path = path;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Setting File Path", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Setting File Path", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void OnSaveClassFile()
        {
            bool success_writing_class_record = this.classGen.SaveClassRecord();
            if (!success_writing_class_record)
            {
                MessageBox.Show("A class or enum by the same name exists already!", "Error Saving Class Record", MessageBoxButton.OK, MessageBoxImage.Error);
                this.classGen.ClearClassRecords();
                this.lv_existing_types.ItemsSource = this.classGen.ClassRecords;
                return;
            }

            // retrieve full text
            TextRange txtR = new TextRange(this.tb_class_text.Document.ContentStart,
                                           this.tb_class_text.Document.ContentEnd);
            string content = txtR.Text;
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("Nothing to save ...", "Saving File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // set file path, if necessary
            if (string.IsNullOrEmpty(this.class_file_path))
                this.SetFilePath();

            try
            {         
                // create any subdirectories
                new FileInfo(this.class_file_path).Directory.Create();
                // write file
                using (FileStream fs = File.Create(this.class_file_path))
                {
                    byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                    fs.Write(content_B, 0, content_B.Length);
                }
                // write log file
                using (FileStream fs = File.Create(this.log_file_path))
                {
                    this.classGen.WriteClassRecords(fs);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Saving File", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Error Saving File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region EVENT HANDLERS: Text in RichTextBox changed

        private void tb_class_text_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.tb_class_text.Document == null) return;

            // this prevents infinite loops
            this.tb_class_text.TextChanged -= tb_class_text_TextChanged;

            this.keywords.Clear();
            this.specialwords.Clear();

            //clear all formats
            TextRange documentRange = new TextRange(this.tb_class_text.Document.ContentStart, this.tb_class_text.Document.ContentEnd);
            documentRange.ClearAllProperties();

            // save caret position
            TextPointer caretPos = this.tb_class_text.CaretPosition;
            int offset = caretPos.GetOffsetToPosition(this.tb_class_text.Document.ContentStart);

            // combine runs (user input is placed in a new run...)
            Dictionary<Paragraph, string> current_content = new Dictionary<Paragraph, string>();
            var rtb_blocks = this.tb_class_text.Document.Blocks;
            foreach (var block in rtb_blocks)
            {
                Paragraph p = block as Paragraph;
                if (p != null)
                {                                  
                    // combine runs in the paragraph
                    var inlines = p.Inlines;     
                    string p_content = "";
                    foreach(var inline in inlines)
                    {
                        Run r = inline as Run;
                        if (r != null)
                        {
                            p_content += r.Text;
                        }
                    }
                    current_content.Add(p, p_content);
                }
            }
            foreach(var entry in current_content)
            {
                entry.Key.Inlines.Clear();
                entry.Key.Inlines.Add(new Run(entry.Value));
            }

            // restore caret position
            this.tb_class_text.CaretPosition = this.tb_class_text.Document.ContentStart.GetPositionAtOffset(-offset + this.caret_offset_add, caretPos.LogicalDirection);           

            // find all keywords
            TextPointer navigator = this.tb_class_text.Document.ContentStart;
            while (navigator.CompareTo(this.tb_class_text.Document.ContentEnd) < 0)
            {
                TextPointerContext context = navigator.GetPointerContext(LogicalDirection.Backward);
                if (context == TextPointerContext.ElementStart && navigator.Parent is Run)
                {
                    string text_snippet = ((Run)navigator.Parent).Text;
                    if (text_snippet != "")
                    {
                        LookForKeyordsInRun((Run)navigator.Parent, text_snippet);
                    }
                }
                navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
            }

            //only after all keywords are found, then we highlight them
            for (int i = 0; i < this.keywords.Count; i++)
            {
                try
                {
                    TextRange range = new TextRange(this.keywords[i].start_pos, this.keywords[i].end_pos);
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
                    range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                }
                catch { }
            }
            for (int i = 0; i < this.specialwords.Count; i++)
            {
                try
                {
                    TextRange range = new TextRange(this.specialwords[i].start_pos, this.specialwords[i].end_pos);
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff008ab7")));
                }
                catch { }
            }

            // this prevents infinite loops
            this.tb_class_text.TextChanged += tb_class_text_TextChanged;
        }

        private void tb_class_text_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                this.caret_offset_add = 0;
            }
            else
            {
                this.caret_offset_add = -2;
            }
        }

        #endregion

        #region EVENT HANDLERS: Text in TextBoxes changed

        private void tb_namespace_LostFocus(object sender, RoutedEventArgs e)
        {
            // since an empty namespace is not allowed -> set a default namespace
            if (string.IsNullOrEmpty(this.tb_namespace.Text))
                this.tb_namespace.Text = ClassGenerator.DEFAULT_NAMESPACE;

            // call the class generator
            this.SuccessGeneratingClassText = this.classGen.GenerateClassText(this.node_in_preview, this.tb_namespace.Text);
            this.UpdateClassText(this.classGen.Snippets);
        }

        #endregion

        
        #region EVENT HANDLERS: Buttons

        private void btn_Browse_Click(object sender, RoutedEventArgs e)
        {
            this.SetFilePath();  
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            this.OnSaveClassFile();
            this.DialogResult = true;
            this.Close();
        }

        #endregion

        



    }
}
