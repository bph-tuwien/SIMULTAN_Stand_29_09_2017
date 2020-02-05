using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;

using ParameterStructure.Parameter;

namespace ComponentBuilder.UIElements
{
    class CalculationParamPicker : Grid, INotifyPropertyChanged
    {
        #region STATIC

        public static readonly Regex LITERAL = new Regex("[A-Za-z]{1}[A-Za-z0-9_]{0,9}");
        public static readonly Regex FUNCTION_CALL = new Regex("[A-Z]{1}[A-Za-z_.]{0,15}[(]{1}");
        public static readonly char[] FUCNTION_INDICATORS = new char[] { '.', '(' };
        
        protected static readonly int MAX_NR_ROWS = 10;
        protected static readonly int INFO_COL_WIDTH = 36;

        protected static readonly int MIN_COL_WIDTH = 25;
        protected static readonly int MIN_ROW_HEIGHT = 30;

        protected static readonly int MAX_COL_WIDTH = 90;
        protected static readonly int MAX_ROW_HEIGHT = 40;

        protected const string NAME_TEXTBOX_NR_OUPUT_PARAMS = "txt_NR_OUT";
        protected const int MAX_NR_OUTPUT_PARAMS = 6;

        #endregion

        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion 

        #region PROPERTIES: Expression

        public string ExpressionString
        {
            get { return (string)GetValue(ExpressionStringProperty); }
            set { SetValue(ExpressionStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExpressionString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExpressionStringProperty =
            DependencyProperty.Register("ExpressionString", typeof(string), typeof(CalculationParamPicker),
            new PropertyMetadata(string.Empty, new PropertyChangedCallback(ExpressionStringPropertyChangedCallback)));

        private static void ExpressionStringPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CalculationParamPicker instance = d as CalculationParamPicker;
            if (instance == null) return;
            if (string.IsNullOrEmpty(instance.ExpressionString)) return;
            if (instance.CalcUnderConstr == null) return;

            var matches_L = CalculationParamPicker.LITERAL.Matches(instance.ExpressionString);
            var matches_F = CalculationParamPicker.FUNCTION_CALL.Matches(instance.ExpressionString);
            List<string> function_name_comps = new List<string>();
            if (matches_F.Count > 0)
            {
                foreach(Match m in matches_F)
                {
                    string[] comps = m.Value.Split(CalculationParamPicker.FUCNTION_INDICATORS);
                    if (comps != null)
                        function_name_comps.AddRange(comps);
                }
            }
            if (matches_L.Count > 0)
            {
                List<string> literal_symbols_old = instance.CalcUnderConstr.InputParams.Keys.ToList();
                List<string> literal_symbols_new = new List<string>();
                foreach (Match m in matches_L)
                {
                    string symbol = m.Value;
                    if (function_name_comps.Contains(symbol)) continue;

                    bool success = literal_symbols_old.Remove(symbol);
                    if (!success)
                        literal_symbols_new.Add(symbol);
                }
                if (literal_symbols_old.Count > 0)
                {
                    foreach(string s in literal_symbols_old)
                    {
                        instance.CalcUnderConstr.InputParams.Remove(s);
                    }
                }
                if (literal_symbols_new.Count > 0)
                {
                    foreach(string s in literal_symbols_new)
                    {
                        instance.CalcUnderConstr.InputParams.Add(s, null);
                    }
                }

                instance.CalcUnderConstr.Expression = instance.ExpressionString;
                instance.UpdateGridStructure();
            }
        }

        #endregion

        #region PROPERIES: Name

        public string CalcName
        {
            get { return (string)GetValue(CalcNameProperty); }
            set { SetValue(CalcNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Name.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CalcNameProperty =
            DependencyProperty.Register("CalcName", typeof(string), typeof(CalculationParamPicker),
            new UIPropertyMetadata("Calculation", new PropertyChangedCallback(CalcNamePropertyChangedCallback)));

        private static void CalcNamePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CalculationParamPicker instance = d as CalculationParamPicker;
            if (instance == null) return;
            if (instance.CalcUnderConstr == null) return;

            instance.CalcUnderConstr.Name = instance.CalcName;
        }

        #endregion

        #region PROPERTIES: Parameter Picking

        public ICommand PickInputParamCmd
        {
            get { return (ICommand)GetValue(PickInputParamCmdProperty); }
            set { SetValue(PickInputParamCmdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PickInputParamCmd.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PickInputParamCmdProperty =
            DependencyProperty.Register("PickInputParamCmd", typeof(ICommand), typeof(CalculationParamPicker),
            new UIPropertyMetadata(null, new PropertyChangedCallback(PickInputParamCmdPropertyChangedCallback)));

        private static void PickInputParamCmdPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CalculationParamPicker instance = d as CalculationParamPicker;
            if (instance == null) return;
            foreach(object child in instance.Children)
            {
                ToggleButton btn = child as ToggleButton;
                if (btn == null) continue;
                if (btn.Name == null) continue;
                if (btn.Name.Contains("IN"))
                {
                    btn.Command = instance.PickInputParamCmd;
                    btn.CommandParameter = btn.Tag;
                    btn.IsChecked = instance.IsChecked && instance.symbol_current == btn.Tag.ToString();
                }
            }
        }

        public ICommand PickOutputParamCmd
        {
            get { return (ICommand)GetValue(PickOutputParamCmdProperty); }
            set { SetValue(PickOutputParamCmdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PickOutputParamCmd.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PickOutputParamCmdProperty =
            DependencyProperty.Register("PickOutputParamCmd", typeof(ICommand), typeof(CalculationParamPicker),
            new PropertyMetadata(null, new PropertyChangedCallback(PickOutputParamCmdPropertyChangedCallback)));

        private static void PickOutputParamCmdPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CalculationParamPicker instance = d as CalculationParamPicker;
            if (instance == null) return;
            foreach (object child in instance.Children)
            {
                ToggleButton btn = child as ToggleButton;
                if (btn == null) continue;
                if (btn.Name == null) continue;
                if (btn.Name.Contains("OUT"))
                {
                    btn.Command = instance.PickOutputParamCmd;
                    btn.CommandParameter = btn.Tag;
                    btn.IsChecked = instance.IsChecked && instance.symbol_current == btn.Tag.ToString();
                }
            }
        }

        #endregion

        #region PROPERTIES: Output Parameter Nr

        public int NrOutputParams
        {
            get { return (int)GetValue(NrOutputParamsProperty); }
            set { SetValue(NrOutputParamsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NrOutputParams.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NrOutputParamsProperty =
            DependencyProperty.Register("NrOutputParams", typeof(int), typeof(CalculationParamPicker),
            new UIPropertyMetadata(1, new PropertyChangedCallback(NrOutputParamsPropertyChangedCallback)));

        private static void NrOutputParamsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CalculationParamPicker instance = d as CalculationParamPicker;
            if (instance == null) return;
            if (instance.NrOutputParams < 1 || instance.NrOutputParams > CalculationParamPicker.MAX_NR_OUTPUT_PARAMS) return;
            if (instance.CalcUnderConstr == null) return;

            List<string> out_symbols_old = instance.CalcUnderConstr.ReturnParams.Keys.ToList();
            List<string> out_symbols_new = new List<string>();            
            for (int i = 0; i < instance.NrOutputParams; i++ )
            {
                string symbol = "out0" + (i + 1).ToString();
                bool success = out_symbols_old.Remove(symbol);
                if (!success)
                    out_symbols_new.Add(symbol);
            }

            if (out_symbols_old.Count > 0)
            {
                foreach(string s in out_symbols_old)
                {
                    instance.CalcUnderConstr.ReturnParams.Remove(s);
                }
            }
            if (out_symbols_new.Count > 0)
            {
                foreach(string s in out_symbols_new)
                {
                    instance.CalcUnderConstr.ReturnParams.Add(s, null);
                }
            }

            instance.UpdateGridStructure();
        }

        #endregion

        #region PROPERTIES: State

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsChecked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(CalculationParamPicker),
            new UIPropertyMetadata(false, new PropertyChangedCallback(IsCheckedPropertyChangedCallback)));

        private static void IsCheckedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CalculationParamPicker instance = d as CalculationParamPicker;
            if (instance == null) return;
            foreach (object child in instance.Children)
            {
                ToggleButton btn = child as ToggleButton;
                if (btn == null) continue;
                btn.IsChecked = instance.IsChecked && instance.symbol_current == btn.Tag.ToString();
            }
        }

        #endregion

        #region PROPERTIES: Calculation

        public Calculation CalcUnderConstr
        {
            get { return (Calculation)GetValue(CalcUnderConstrProperty); }
            set { SetValue(CalcUnderConstrProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CalcUnderConstr.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CalcUnderConstrProperty =
            DependencyProperty.Register("CalcUnderConstr", typeof(Calculation), typeof(CalculationParamPicker),
            new UIPropertyMetadata(null, new PropertyChangedCallback(CalcUnderConstrPropertyChangedCallback),
                new CoerceValueCallback(CalcUnderConstrCoerceValueCallback)));

        private static object CalcUnderConstrCoerceValueCallback(DependencyObject d, object baseValue)
        {
            CalculationParamPicker instance = d as CalculationParamPicker;
            if (instance == null) return baseValue;
            if (instance.CalcUnderConstr != null)
                instance.CalcUnderConstr.PropertyChanged -= instance.calcUC_PropertyChanged;

            return baseValue;
        }

        private static void CalcUnderConstrPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CalculationParamPicker instance = d as CalculationParamPicker;
            if (instance == null) return;
            if (instance.CalcUnderConstr == null) return;

            instance.CalcUnderConstr.PropertyChanged += instance.calcUC_PropertyChanged;

            instance.CalcName = instance.CalcUnderConstr.Name;
            instance.ExpressionString = instance.CalcUnderConstr.Expression;
            instance.UpdateGridStructure(true);
        }

        #endregion

        #region CLASS MEMBERS
        
        string symbol_current = string.Empty;

        private bool size_original_set = false;
        protected double width_original = 0;
        protected double height_original = 0;

        protected int nr_columns = 0;
        protected int nr_rows_IN = 0;
        protected int nr_rows_OUT = 1;
        protected int column_width = 0;
        protected int row_height = 0;

        #endregion

        #region METHODS: Grid Reset, Resize

        protected void ResetGrid()
        {
            this.Children.Clear();
            this.RowDefinitions.Clear();
            this.ColumnDefinitions.Clear();
        }

        protected void RecalculateSizes()
        {
            // set the original sizes, if not set already
            if (!this.size_original_set)
            {
                this.width_original = this.Width;
                this.height_original = this.Height;
                this.size_original_set = true;
            }

            // save cell nr
            this.nr_columns = 3;
            this.nr_rows_IN = this.CalcUnderConstr.InputParams.Count();
            this.nr_rows_OUT = this.CalcUnderConstr.ReturnParams.Count();

            int col_B = Math.Max((int)Math.Floor((this.width_original - CalculationParamPicker.INFO_COL_WIDTH * 2) / this.nr_columns), CalculationParamPicker.MIN_COL_WIDTH);
            col_B = Math.Min(col_B, CalculationParamPicker.MAX_COL_WIDTH);
            int row_H = Math.Max((int)Math.Floor(this.height_original / (this.nr_rows_IN + this.nr_rows_OUT + 1)), CalculationParamPicker.MIN_ROW_HEIGHT);
            row_H = Math.Min(row_H, CalculationParamPicker.MAX_ROW_HEIGHT);

            // re-calculate the size of the grid
            this.Width = CalculationParamPicker.INFO_COL_WIDTH * 2 + this.nr_columns * col_B;
            this.Height = (this.nr_rows_IN + this.nr_rows_OUT + 1) * row_H;

            // save cell sizes            
            this.column_width = col_B;
            this.row_height = row_H;
        }

        protected void RedefineSymbolGrid()
        {
            ColumnDefinition cd0 = new ColumnDefinition();
            cd0.Width = new GridLength(CalculationParamPicker.INFO_COL_WIDTH);
            this.ColumnDefinitions.Add(cd0);
            ColumnDefinition cd1 = new ColumnDefinition();
            cd1.Width = new GridLength(CalculationParamPicker.INFO_COL_WIDTH);
            this.ColumnDefinitions.Add(cd1);
            ColumnDefinition cd2 = new ColumnDefinition();
            cd2.Width = new GridLength();
            this.ColumnDefinitions.Add(cd2);
            
            // INPUT params
            for (int i = 0; i < this.nr_rows_IN; i++)
            {
                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(this.row_height);
                this.RowDefinitions.Add(rd);
            }

            // setting the nr of output params
            RowDefinition rdX = new RowDefinition();
            rdX.Height = new GridLength(this.row_height);
            this.RowDefinitions.Add(rdX);

            // OUPUT params
            for (int i = 0; i < this.nr_rows_OUT; i++)
            {
                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(this.row_height);
                this.RowDefinitions.Add(rd);
            }
        }

        #endregion

        #region METHODS: Grid Realize

        private void PopulateGrid_IN()
        {
            SortedDictionary<string, Parameter> sorted_input_params = new SortedDictionary<string, Parameter>(this.CalcUnderConstr.InputParams); // added 31.08.2017
            for(int i = 0; i < this.nr_rows_IN; i++)
            {
                string symbol_IN = sorted_input_params.ElementAt(i).Key;
                Parameter param_IN = sorted_input_params.ElementAt(i).Value;

                TextBlock tbl = new TextBlock();
                tbl.HorizontalAlignment = HorizontalAlignment.Right;
                tbl.VerticalAlignment = VerticalAlignment.Bottom;
                tbl.Height = 20;
                tbl.Margin = new Thickness(5, 2, 1, 2);
                tbl.Text = symbol_IN;
                tbl.FontSize = 12;
                tbl.FontWeight = FontWeights.Normal;
                tbl.Foreground = new SolidColorBrush(Colors.Black);

                Grid.SetRow(tbl, i);
                Grid.SetColumn(tbl, 0);
                this.Children.Add(tbl);

                ToggleButton btn = new ToggleButton();
                btn.Style = (Style)btn.TryFindResource("ToggleButtonRed");
                Image im = new Image();
                im.Source = new BitmapImage(new Uri(@"./Data/icons/calc_getP.png", UriKind.Relative));
                btn.Content = im;
                btn.Height = 26;
                btn.Width = 26;
                btn.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                btn.Margin = new Thickness(5, 2, 5, 2);
                btn.Name = "IN_" + (i + 1).ToString();
                btn.Tag = symbol_IN;
                btn.Command = this.PickInputParamCmd;
                btn.CommandParameter = symbol_IN;
                btn.IsChecked = this.IsChecked && this.symbol_current == symbol_IN;
                btn.Click += btn_IN_Click;

                Grid.SetRow(btn, i);
                Grid.SetColumn(btn, 1);
                this.Children.Add(btn);

                TextBlock tb = new TextBlock();
                tb.Height = 20;
                tb.Margin = new Thickness(5, 2, 5, 2);
                tb.Text = (param_IN != null) ? param_IN.Name + ": " + param_IN.ValueCurrent.ToString("F2", Parameter.NR_FORMATTER) : "parameter " + symbol_IN;
                tb.FontWeight = (param_IN != null) ? FontWeights.Bold : FontWeights.Normal;
                tb.Tag = symbol_IN;

                Grid.SetRow(tb, i);
                Grid.SetColumn(tb, 2);
                this.Children.Add(tb);
            }           
        }

        private void PopulateGrid_OUT()
        {
            // setting the NR of output params
            TextBlock tbNrLabel = new TextBlock();
            tbNrLabel.HorizontalAlignment = HorizontalAlignment.Left;
            tbNrLabel.Height = 20;
            tbNrLabel.Margin = new Thickness(0, 2, 0, 2);
            tbNrLabel.Text = "Output-Params:";
            tbNrLabel.FontSize = 10;
            tbNrLabel.Foreground = new SolidColorBrush(Colors.DimGray);

            Grid.SetRow(tbNrLabel, this.nr_rows_IN);
            Grid.SetColumn(tbNrLabel, 0);
            Grid.SetColumnSpan(tbNrLabel, 2);
            this.Children.Add(tbNrLabel);

            TextBox tbNr = new TextBox();
            tbNr.Height = 20;
            tbNr.Width = 30;
            tbNr.Margin = new Thickness(5, 4, 5, 2);
            tbNr.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            tbNr.Style = (Style)tbNr.TryFindResource("ValueInput");
            tbNr.Text = this.NrOutputParams.ToString();
            tbNr.Name = CalculationParamPicker.NAME_TEXTBOX_NR_OUPUT_PARAMS;

            Grid.SetRow(tbNr, this.nr_rows_IN);
            Grid.SetColumn(tbNr, 2);
            this.Children.Add(tbNr);

            Button btnNr = new Button();
            btnNr.Style = (Style)btnNr.FindResource("ReliefButton");
            Image im = new Image();
            im.Source = new BitmapImage(new Uri(@"./Data/icons/btn_OK.png", UriKind.Relative));
            btnNr.Content = im;
            btnNr.Height = 26;
            btnNr.Width = 26;
            btnNr.Margin = new Thickness(5, 2, 5, 2);
            btnNr.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;           
            btnNr.Click += btn_NR_OUT_Click;

            Grid.SetRow(btnNr, this.nr_rows_IN);
            Grid.SetColumn(btnNr, 2);
            this.Children.Add(btnNr);

            // set the output params
            for (int i = 0; i < this.nr_rows_OUT; i++)
            {
                string symbol_OUT = this.CalcUnderConstr.ReturnParams.ElementAt(i).Key;
                Parameter param_OUT = this.CalcUnderConstr.ReturnParams.ElementAt(i).Value;

                TextBlock tbl = new TextBlock();
                tbl.HorizontalAlignment = HorizontalAlignment.Right;
                tbl.VerticalAlignment = VerticalAlignment.Bottom;
                tbl.Height = 20;
                tbl.Margin = new Thickness(5, 2, 1, 2);
                tbl.Text = symbol_OUT;
                tbl.FontSize = 12;
                tbl.FontWeight = FontWeights.Normal;
                tbl.Foreground = new SolidColorBrush(Colors.Black);

                Grid.SetRow(tbl, this.nr_rows_IN + 1 + i);
                Grid.SetColumn(tbl, 0);
                this.Children.Add(tbl);

                ToggleButton btn = new ToggleButton();
                btn.Style = (Style)btn.TryFindResource("ToggleButtonRed");
                Image im1 = new Image();
                im1.Source = new BitmapImage(new Uri(@"./Data/icons/calc_getRP.png", UriKind.Relative));
                btn.Content = im1;
                btn.Height = 26;
                btn.Width = 26;
                btn.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                btn.Margin = new Thickness(5, 2, 5, 2);
                btn.Name = "OUT_" + (i + 1).ToString();
                btn.Tag = symbol_OUT;
                btn.Command = this.PickOutputParamCmd;
                btn.CommandParameter = symbol_OUT;
                btn.IsChecked = this.IsChecked && this.symbol_current == symbol_OUT;
                btn.Click += btn_OUT_Click;

                Grid.SetRow(btn, this.nr_rows_IN + 1 + i);
                Grid.SetColumn(btn, 1);
                this.Children.Add(btn);

                TextBlock tb = new TextBlock();
                tb.Height = 20;
                tb.Margin = new Thickness(5, 2, 5, 2);
                tb.Text = (param_OUT != null) ? param_OUT.Name + ": " + param_OUT.ValueCurrent.ToString("F2", Parameter.NR_FORMATTER) : "parameter " + symbol_OUT;
                tb.FontWeight = (param_OUT != null) ? FontWeights.Bold : FontWeights.Normal;
                tb.Tag = symbol_OUT;

                Grid.SetRow(tb, this.nr_rows_IN + 1 + i);
                Grid.SetColumn(tb, 2);
                this.Children.Add(tb);
            }
        }

        #endregion

        #region METHODS: Frid Realize (Data)

        private void UpdateGridStructure(bool _update_data = false)
        {
            this.ResetGrid();
            this.RecalculateSizes();
            this.RedefineSymbolGrid();
            this.PopulateGrid_IN();
            this.PopulateGrid_OUT();

            if (_update_data)
            {
                this.PopulateInputParams();
                this.PopulateOutputParams();
            }
        }

        private void PopulateInputParams()
        {
            if (this.CalcUnderConstr == null) return;

            foreach(var entry in this.CalcUnderConstr.InputParams)
            {
                if (entry.Value == null) continue;
                this.AdaptTextBlock(entry.Key, entry.Value.Name + ": " + entry.Value.ValueCurrent.ToString("F2", Parameter.NR_FORMATTER));              
            }
        }

        private void PopulateOutputParams()
        {
            if (this.CalcUnderConstr == null) return;

            foreach (var entry in this.CalcUnderConstr.ReturnParams)
            {
                if (entry.Value == null) continue;
                this.AdaptTextBlock(entry.Key, entry.Value.Name + ": " + entry.Value.ValueCurrent.ToString("F2", Parameter.NR_FORMATTER));
            }
        }

        private void AdaptTextBlock(string _tag, string _text)
        {
            if (string.IsNullOrEmpty(_tag)) return;
            foreach (var child in this.Children)
            {
                TextBlock tb = child as TextBlock;
                if (tb == null) continue;
                if (tb.Tag == null) continue;

                if (tb.Tag.ToString() == _tag)
                {
                    tb.Text = _text;
                    tb.FontWeight = FontWeights.Bold;
                    break;
                }
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void btn_IN_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = sender as ToggleButton;
            if (tbtn == null) return;
            if (tbtn.Tag == null) return;
            
            this.symbol_current = tbtn.Tag.ToString();
        }

        private void btn_OUT_Click(object sender, RoutedEventArgs e)
        {
            this.btn_IN_Click(sender, e);
        }

        private void btn_NR_OUT_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            foreach(var child in this.Children)
            {
                if (!(child is TextBox)) continue;
                TextBox txt = child as TextBox;
                if (txt.Name != null && txt.Name == CalculationParamPicker.NAME_TEXTBOX_NR_OUPUT_PARAMS)
                {
                    int nr = 0;
                    bool success = int.TryParse(txt.Text, out nr);
                    if (success)
                        this.NrOutputParams = Math.Min(6, Math.Max(nr, 1));
                    else
                        this.NrOutputParams = 1;
                    break;
                }
            }
        }

        private void calcUC_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Calculation calc = sender as Calculation;
            if (calc == null || e == null) return;

            if (e.PropertyName == "InputParams")
            {
                this.PopulateInputParams();
            }
            else if (e.PropertyName == "ReturnParams")
            {
                this.PopulateOutputParams();
            }
        }

        #endregion
    }
}
