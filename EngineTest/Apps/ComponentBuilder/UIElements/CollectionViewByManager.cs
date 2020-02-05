using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ParameterStructure.Component;

namespace ComponentBuilder.UIElements
{
    class CollectionViewByManager : StackPanel, INotifyPropertyChanged
    {
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

        #region PROPERTIES: Switch btw Managers / Categories

        // true = Managers, false = Categories
        private bool switch_ManCat;
        public bool SwitchManCat
        {
            get { return this.switch_ManCat; }
            set 
            { 
                this.switch_ManCat = value;
                this.RegisterPropertyChanged("SwitchManCat");
                this.PopulateStackPanel();
            }
        }

        #endregion

        #region PROPERTIES: Component Factory

        public ComponentFactory Factory
        {
            get { return (ComponentFactory)GetValue(FactoryProperty); }
            set { SetValue(FactoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Factory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FactoryProperty =
            DependencyProperty.Register("Factory", typeof(ComponentFactory), typeof(CollectionViewByManager),
            new PropertyMetadata(null, new PropertyChangedCallback(FactoryPropertyChangedCallback)));

        private static void FactoryPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CollectionViewByManager instance = d as CollectionViewByManager;
            if (instance == null) return;
            if (instance.Factory == null) return;

            instance.FilterFactoryRecord();
        }

        #endregion

        #region CLASS MEMBERS

        private ToggleButton main_switch;
        private ToggleButton and_or_switch;

        private List<Category> marked_categories;
        private List<ComponentManagerType> marked_users;
        private bool operand_is_AND;

        #endregion

        public CollectionViewByManager()
        {
            this.Loaded += CollectionViewByManager_Loaded;

            this.marked_categories = new List<Category>();
            this.marked_users = new List<ComponentManagerType>();
            this.operand_is_AND = false;
        }        

        #region METHODS: Realize StackPanel

        private void PopulateStackPanel()
        {
            // reset
            this.Children.Clear();

            // OK Button
            Button btn_Apply = new Button();
            btn_Apply.Style = (Style)btn_Apply.TryFindResource("ReliefButtonRound");
            btn_Apply.Width = 26;
            btn_Apply.Height = 26;
            Image im = new Image();
            im.Source = new BitmapImage(new Uri(@"./Data/icons/btn_OK.png", UriKind.Relative));
            btn_Apply.Content = im;
            btn_Apply.Margin = new Thickness(2, 0, 2, 0);
            btn_Apply.ToolTip = "Anwenden";
            btn_Apply.Cursor = Cursors.Hand;
            btn_Apply.Click += btn_Apply_Click;

            this.Children.Add(btn_Apply);

            // main toggle: btw Managers and Categories
            ToggleButton tbtn_MC = new ToggleButton();
            tbtn_MC.Style = (Style)tbtn_MC.TryFindResource("ToggleButtonBlueRound");
            tbtn_MC.Margin = new Thickness(2, 0, 24, 0);
            tbtn_MC.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tbtn_MC.IsChecked = this.SwitchManCat;
            tbtn_MC.ToolTip = (this.SwitchManCat) ? "zu Kategorien umschalten" : "zu Rollen umschalten";
            tbtn_MC.Cursor = Cursors.Hand;
            tbtn_MC.Click += main_switch_Click;
            
            this.main_switch = tbtn_MC;
            this.Children.Add(tbtn_MC);

            // single option toggles
            if (this.SwitchManCat)
                this.PopulateStackPanel_Managers();
            else
                this.PopulateStackPanel_Categories();

            // AND / OR toggle
            ToggleButton tbtn_AO = new ToggleButton();
            tbtn_AO.Style = (Style)tbtn_AO.TryFindResource("ToggleButtonBlueRound");
            tbtn_AO.Content = "|";
            tbtn_AO.FontSize = 16;
            tbtn_AO.Margin = new Thickness(8, 0, 0, 0);
            tbtn_AO.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tbtn_AO.ToolTip = "oder";
            tbtn_AO.Cursor = Cursors.Hand;
            tbtn_AO.Click += and_or_Click;

            this.and_or_switch = tbtn_AO;
            this.Children.Add(tbtn_AO);
        }

        private void PopulateStackPanel_Managers()
        {
            // manager toggles
            int nr_managers = Enum.GetNames(typeof(ComponentManagerType)).Count();
            for (int i = 0; i < nr_managers; i++)
            {
                ToggleButton tbtn = new ToggleButton();
                tbtn.Style = (Style)tbtn.TryFindResource("ToggleButtonBWRound");
                tbtn.Content = ComponentUtils.ComponentManagerTypeToLetter((ComponentManagerType)i);
                tbtn.FontFamily = new FontFamily(new Uri("pack://application:,,,/ComponentBuilder;component/Data/fonts/"), "./#managers");
                tbtn.FontSize = 18;
                tbtn.Margin = new Thickness(0, 0, 2, 0);
                tbtn.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbtn.ToolTip = ComponentUtils.ComponentManagerTypeToDescrDE((ComponentManagerType)i);
                tbtn.Tag = (ComponentManagerType)i;
                tbtn.Cursor = Cursors.Hand;
                tbtn.Click += manager_Click;

                this.Children.Add(tbtn);
            }
        }

        private void PopulateStackPanel_Categories()
        {
            // category toggles
            int nr_categories = Enum.GetNames(typeof(Category)).Count();
            for (int i = 0; i < nr_categories - 2; i++)
            {
                ToggleButton tbtn = new ToggleButton();
                tbtn.Style = (Style)tbtn.TryFindResource("ToggleButtonBWRound");
                tbtn.Content = ComponentUtils.CATEGORY_NONE_AS_STR[i].ToString().ToUpper();
                tbtn.FontFamily = new FontFamily(new Uri("pack://application:,,,/ComponentBuilder;component/Data/fonts/"), "./#categories");
                tbtn.FontSize = 18;
                tbtn.Margin = new Thickness(0, 0, 2, 0);
                tbtn.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                tbtn.ToolTip = ComponentUtils.CategoryStringToDescription(tbtn.Content.ToString());
                tbtn.Tag = ComponentUtils.StringToCategory(tbtn.Content.ToString());
                tbtn.Cursor = Cursors.Hand;
                tbtn.Click += category_CLick;

                this.Children.Add(tbtn);
            }  
        }

        #endregion

        #region METHODS: Filter the Component Factory Record

        private void FilterFactoryRecord()
        {
            if (this.Factory == null) return;

            if (this.SwitchManCat)
            {
                // filter by: MANAGERS
                this.Factory.CreateView(this.marked_users, !this.operand_is_AND);
            }
            else
            {
                // filter by: CATEGORIES
                this.Factory.CreateView(this.marked_categories, !this.operand_is_AND);
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void CollectionViewByManager_Loaded(object sender, RoutedEventArgs e)
        {
            this.PopulateStackPanel();
            this.switch_ManCat = false;
        }

        private void main_switch_Click(object sender, RoutedEventArgs e)
        {
            if (this.main_switch == null) return;
            this.SwitchManCat = this.main_switch.IsChecked.Value;

            this.marked_categories = new List<Category>();
            this.marked_users = new List<ComponentManagerType>();
        }

        private void manager_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = sender as ToggleButton;
            if (tbtn == null) return;
            if (tbtn.Tag == null) return;
            if (!(tbtn.Tag is ComponentManagerType)) return;

            ComponentManagerType marked = (ComponentManagerType)tbtn.Tag;
            if (this.marked_users.Contains(marked))
                this.marked_users.Remove(marked);
            else
                this.marked_users.Add(marked);
        }

        private void category_CLick(object sender, RoutedEventArgs e)
        {
            ToggleButton tbtn = sender as ToggleButton;
            if (tbtn == null) return;
            if (tbtn.Tag == null) return;
            if (!(tbtn.Tag is Category)) return;

            Category marked = (Category)tbtn.Tag;
            if (this.marked_categories.Contains(marked))
                this.marked_categories.Remove(marked);
            else
                this.marked_categories.Add(marked);
        }

        private void and_or_Click(object sender, RoutedEventArgs e)
        {
            if (this.and_or_switch == null) return;

            if (this.and_or_switch.Content.ToString() == "|")
            {
                // switch to AND
                this.and_or_switch.Content = "&";
                this.and_or_switch.ToolTip = "UND";
                this.operand_is_AND = true;
            }
            else if (this.and_or_switch.Content.ToString() == "&")
            {
                // switch to OR
                this.and_or_switch.Content = "|";
                this.and_or_switch.ToolTip = "ODER";
                this.operand_is_AND = false;
            }
        }

        private void btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            // main functionality
            this.FilterFactoryRecord();
        }

        #endregion

    }
}
