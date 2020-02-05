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
using System.Globalization;

using ParameterStructure.Values;

namespace ComponentBuilder.UIElements
{
    #region HELPER CLASS

    internal class BigTableRow
    {
        internal static IFormatProvider FORMATTER = new NumberFormatInfo();
        internal const string FORMAT = "F4";
        private const string NOTHING = "-";

        public string Col00 { get; private set; }
        public string Col01 { get; private set; }
        public string Col02 { get; private set; }
        public string Col03 { get; private set; }
        public string Col04 { get; private set; }
        public string Col05 { get; private set; }
        public string Col06 { get; private set; }
        public string Col07 { get; private set; }
        public string Col08 { get; private set; }
        public string Col09 { get; private set; }
        public string Col10 { get; private set; }

        public int Col00_Index { get; private set; }

        public string GetColAt(int _index)
        {
            switch(_index)
            {
                case 0:
                    return this.Col00;
                case 1:
                    return this.Col01;
                case 2:
                    return this.Col02;
                case 3:
                    return this.Col03;
                case 4:
                    return this.Col04;
                case 5:
                    return this.Col05;
                case 6:
                    return this.Col06;
                case 7:
                    return this.Col07;
                case 8:
                    return this.Col08;
                case 9:
                    return this.Col09;
                case 10:
                    return this.Col10;
                default:
                    return this.Col00;
            }
        }

        public BigTableRow(int _index, List<double> _input)
        {
            this.Col00 = _index.ToString();
            this.Col01 = NOTHING;
            this.Col02 = NOTHING;
            this.Col03 = NOTHING;
            this.Col04 = NOTHING;
            this.Col05 = NOTHING;
            this.Col06 = NOTHING;
            this.Col07 = NOTHING;
            this.Col08 = NOTHING;
            this.Col09 = NOTHING;
            this.Col10 = NOTHING;

            this.Col00_Index = _index;

            if (_input == null) return;

            this.Col01 = (_input.Count >= 1) ? _input[0].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col02 = (_input.Count >= 2) ? _input[1].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col03 = (_input.Count >= 3) ? _input[2].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col04 = (_input.Count >= 4) ? _input[3].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col05 = (_input.Count >= 5) ? _input[4].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col06 = (_input.Count >= 6) ? _input[5].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col07 = (_input.Count >= 7) ? _input[6].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col08 = (_input.Count >= 8) ? _input[7].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col09 = (_input.Count >= 9) ? _input[8].ToString(FORMAT, FORMATTER) : NOTHING;
            this.Col10 = (_input.Count >= 10) ? _input[9].ToString(FORMAT, FORMATTER) : NOTHING;
        }

        public BigTableRow(int _index, string _row_name, List<double> _input)
            :this(0, _input)
        {
            this.Col00 = _row_name;
            this.Col00_Index = _index;
        }

        public BigTableRow(List<string> _input)
        {
            this.Col00 = "";
            this.Col01 = NOTHING;
            this.Col02 = NOTHING;
            this.Col03 = NOTHING;
            this.Col04 = NOTHING;
            this.Col05 = NOTHING;
            this.Col06 = NOTHING;
            this.Col07 = NOTHING;
            this.Col08 = NOTHING;
            this.Col09 = NOTHING;
            this.Col10 = NOTHING;

            if (_input == null) return;

            this.Col01 = (_input.Count >= 1) ? _input[0] : NOTHING;
            this.Col02 = (_input.Count >= 2) ? _input[1] : NOTHING;
            this.Col03 = (_input.Count >= 3) ? _input[2] : NOTHING;
            this.Col04 = (_input.Count >= 4) ? _input[3] : NOTHING;
            this.Col05 = (_input.Count >= 5) ? _input[4] : NOTHING;
            this.Col06 = (_input.Count >= 6) ? _input[5] : NOTHING;
            this.Col07 = (_input.Count >= 7) ? _input[6] : NOTHING;
            this.Col08 = (_input.Count >= 8) ? _input[7] : NOTHING;
            this.Col09 = (_input.Count >= 9) ? _input[8] : NOTHING;
            this.Col10 = (_input.Count >= 10) ? _input[9] : NOTHING;
        }
    }

    #endregion

    /// <summary>
    /// Interaction logic for ShowMVBigTableWindow.xaml
    /// </summary>
    public partial class ShowMVBigTableWindow : Window
    {
        public ShowMVBigTableWindow()
        {
            InitializeComponent();
            //this.table.ItemsSource = new List<List<double>> { new List<double> { 1, 2 }, new List<double> { 3, 4 } };
        }

        #region CLASS MEMBERS / PROPERTIES

        private MultiValueBigTable data;
        public MultiValueBigTable Data
        {
            get { return this.data; }
            set
            {
                this.data = value;
                this.table.DataField = this.data;
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
