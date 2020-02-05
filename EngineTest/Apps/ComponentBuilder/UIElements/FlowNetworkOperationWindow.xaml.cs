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
    /// Interaction logic for FlowNetworkOperationWindow.xaml
    /// </summary>
    public partial class FlowNetworkOperationWindow : Window
    {
        public static int NR_ENTRIES_MAX = 10;
        public FlowNetworkOperationWindow()
        {
            InitializeComponent();
            this.content_initialized = false;
            this.Loaded += FlowNetworkOperationWindow_Loaded;
        }

        #region PROPERTIES

        private List<TextBox> suffix_FW_Op;
        private List<TextBox> suffix_BW_Op;

        private List<ComboBox> operators_FW;
        private List<ComboBox> operators_BW;

        private List<TextBox> suffix_FW_Res;
        private List<TextBox> suffix_BW_Res;

        private bool content_initialized;


        private FlNetNode node_current;
        public FlNetNode NodeCurrent
        {
            get { return this.node_current; }
            set 
            { 
                this.node_current = value; 
                // adapt content
                if (this.IsLoaded && !(this.content_initialized))
                    this.InitContent();
            }
        }
        

        #endregion


        #region INIT

        private void InitContent()
        {
            if (this.main_grid == null) return;
            if (this.node_current == null) return;

            List<string> operators = new List<string> { "NoNe", "+", "-", "*", "/", "Min", "Max", ":=" };
            this.btn_OK.Click += btn_OK_Click;

            int counter_F = 0;
            int counter_B = 0;
            this.suffix_FW_Op = new List<TextBox>();
            this.suffix_BW_Op = new List<TextBox>();
            this.operators_FW = new List<ComboBox>();
            this.operators_BW = new List<ComboBox>();
            this.suffix_FW_Res = new List<TextBox>();
            this.suffix_BW_Res = new List<TextBox>();

            // get info from node, if available
            if (this.node_current.CalculationRules != null)
            {               
                foreach (FlowNetworkCalcRule rule in this.node_current.CalculationRules)
                {
                    // iteration check
                    if (counter_F > NR_ENTRIES_MAX - 1 && rule.Direction == FlowNetworkCalcDirection.FORWARD)
                        continue;
                    if (counter_B > NR_ENTRIES_MAX - 1 && rule.Direction == FlowNetworkCalcDirection.BACKWARD)
                        continue;

                    TextBox tb = new TextBox();
                    tb.Width = 50;
                    tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    tb.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    tb.Style = (Style)tb.TryFindResource("CoordinateInput");
                    tb.Text = rule.Suffix_Operands;

                    Grid.SetColumn(tb, (rule.Direction == FlowNetworkCalcDirection.FORWARD) ? 1 : 4);
                    Grid.SetRow(tb, (rule.Direction == FlowNetworkCalcDirection.FORWARD) ? counter_F + 2 : counter_B + 2);
                    this.main_grid.Children.Add(tb);

                    // --

                    ComboBox cb = new ComboBox();
                    cb.Margin = new Thickness(2, 2, 5, 2);
                    cb.ItemsSource = operators;
                    cb.SelectedItem = FlowNetworkCalcRule.OperatorToString(rule.Operator);

                    Grid.SetColumn(cb, (rule.Direction == FlowNetworkCalcDirection.FORWARD) ? 2 : 5);
                    Grid.SetRow(cb, (rule.Direction == FlowNetworkCalcDirection.FORWARD) ? counter_F + 2 : counter_B + 2);
                    this.main_grid.Children.Add(cb);

                    // --

                    TextBox tb_1 = new TextBox();
                    tb_1.Width = 50;
                    tb_1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    tb_1.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    tb_1.Style = (Style)tb_1.TryFindResource("CoordinateInput");
                    tb_1.Text = rule.Suffix_Result;

                    Grid.SetColumn(tb_1, (rule.Direction == FlowNetworkCalcDirection.FORWARD) ? 3 : 6);
                    Grid.SetRow(tb_1, (rule.Direction == FlowNetworkCalcDirection.FORWARD) ? counter_F + 2 : counter_B + 2);
                    this.main_grid.Children.Add(tb_1);

                    // save and count
                    if (rule.Direction == FlowNetworkCalcDirection.FORWARD)
                    {
                        counter_F++;
                        this.suffix_FW_Op.Add(tb);
                        this.operators_FW.Add(cb);
                        this.suffix_FW_Res.Add(tb_1);
                    }
                    else
                    {
                        counter_B++;
                        this.suffix_BW_Op.Add(tb);
                        this.operators_BW.Add(cb);
                        this.suffix_BW_Res.Add(tb_1);
                    }

                }
            }

            // complete list, if necessary
            for(int i = counter_F; i < NR_ENTRIES_MAX; i++)
            {
                TextBox tbF = new TextBox();
                tbF.Width = 50;
                tbF.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tbF.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbF.Style = (Style)tbF.TryFindResource("CoordinateInput");
                tbF.Text = "__" + i.ToString();

                Grid.SetColumn(tbF, 1);
                Grid.SetRow(tbF, i + 2);
                this.main_grid.Children.Add(tbF);

                // --

                ComboBox cbF = new ComboBox();
                cbF.Margin = new Thickness(2, 2, 5, 2);
                cbF.ItemsSource = operators;

                Grid.SetColumn(cbF, 2);
                Grid.SetRow(cbF, i + 2);
                this.main_grid.Children.Add(cbF);

                // --

                TextBox tbF_1 = new TextBox();
                tbF_1.Width = 50;
                tbF_1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tbF_1.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbF_1.Style = (Style)tbF_1.TryFindResource("CoordinateInput");
                tbF_1.Text = "__" + i.ToString();

                Grid.SetColumn(tbF_1, 3);
                Grid.SetRow(tbF_1, i + 2);
                this.main_grid.Children.Add(tbF_1);

                this.suffix_FW_Op.Add(tbF);
                this.operators_FW.Add(cbF);
                this.suffix_FW_Res.Add(tbF_1);
            }

            for(int i = counter_B; i < NR_ENTRIES_MAX; i++)
            {
                TextBox tbB = new TextBox();
                tbB.Width = 50;
                tbB.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tbB.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbB.Style = (Style)tbB.TryFindResource("CoordinateInput");
                tbB.Text = "__" + i.ToString();

                Grid.SetColumn(tbB, 4);
                Grid.SetRow(tbB, i + 2);
                this.main_grid.Children.Add(tbB);

                // --

                ComboBox cbB = new ComboBox();
                cbB.Margin = new Thickness(2, 2, 5, 2);
                cbB.ItemsSource = operators;

                Grid.SetColumn(cbB, 5);
                Grid.SetRow(cbB, i + 2);
                this.main_grid.Children.Add(cbB);

                // --

                TextBox tbB_1 = new TextBox();
                tbB_1.Width = 50;
                tbB_1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tbB_1.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbB_1.Style = (Style)tbB_1.TryFindResource("CoordinateInput");
                tbB_1.Text = "__" + i.ToString();

                Grid.SetColumn(tbB_1, 6);
                Grid.SetRow(tbB_1, i + 2);
                this.main_grid.Children.Add(tbB_1);

                this.suffix_BW_Op.Add(tbB);
                this.operators_BW.Add(cbB);
                this.suffix_BW_Res.Add(tbB_1);
            }

            this.content_initialized = true;

        }

        #endregion

        #region EVENT HANLER
        private void FlowNetworkOperationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitContent();
        }

        private void btn_OK_Click(object sender, RoutedEventArgs e)
        {
            // transfer settings to node
            if (this.node_current != null)
            {
                List<FlowNetworkCalcRule> rules_to_transfer = new List<FlowNetworkCalcRule>();

                int size_FW = this.suffix_FW_Op.Count;
                int size_BW = this.suffix_BW_Op.Count;

                for (int i = 0; i < size_FW; i++)
                {
                    if (this.operators_FW[i].SelectedItem == null)
                        continue;
                    if (this.operators_FW[i].SelectedItem.ToString() == "NoNe")
                        continue;
                    rules_to_transfer.Add(new FlowNetworkCalcRule
                    {
                        Direction = FlowNetworkCalcDirection.FORWARD,
                        Suffix_Operands = this.suffix_FW_Op[i].Text,
                        Suffix_Result = this.suffix_FW_Res[i].Text,
                        Operator = FlowNetworkCalcRule.StringToOperator(this.operators_FW[i].SelectedItem.ToString())
                    });
                }
                for (int i = 0; i < size_BW; i++)
                {
                    if (this.operators_BW[i].SelectedItem == null)
                        continue;
                    if (this.operators_BW[i].SelectedItem.ToString() == "NoNe")
                        continue;
                    rules_to_transfer.Add(new FlowNetworkCalcRule
                    {
                        Direction = FlowNetworkCalcDirection.BACKWARD,
                        Suffix_Operands = this.suffix_BW_Op[i].Text,
                        Suffix_Result = this.suffix_BW_Res[i].Text,
                        Operator = FlowNetworkCalcRule.StringToOperator(this.operators_BW[i].SelectedItem.ToString())
                    });
                }

                this.node_current.CalculationRules = rules_to_transfer;
            }

            // done
            this.DialogResult = true;
            this.Close();
        }

        #endregion
    }
}
