using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ComponentBuilder.WinUtils;

using ParameterStructure.Component;
using ParameterStructure.Utils;

namespace ComponentBuilder.UIElements
{
    /// <summary>
    /// Interaction logic for ComponentGraphWindow.xaml
    /// </summary>
    public partial class ComponentGraphWindow : Window
    {
        #region STATIC

        protected static double CANVAS_EXPANSION_STEP = 100.0;

        #endregion

        public ComponentGraphWindow()
        {
            InitializeComponent();
            this.InitControls();
        }

        #region PROPETIES
        private ComponentFactory comp_manager;
        public ComponentFactory CompManager 
        {
            get { return this.comp_manager; }
            set
            {
                this.comp_manager = value;
                if (this.canv != null)
                    this.canv.CompFactory = this.comp_manager;
            }
        }
        #endregion

        #region CLASS MEMBERS

        private List<SelectableString> existing_categories;
        private List<SelectableString> existing_manager_types;

        bool category_or;
        bool managers_or;

        #endregion

        #region METHODS: Init
        private void InitControls()
        {
            // category highlighting
            this.existing_categories = new List<SelectableString>(ComponentUtils.CATEGORIES_SELECTABLE);           
            this.cb_categories.ItemsSource = this.existing_categories;

            this.category_or = true;
            this.tbtn_C_and_or.IsChecked = this.category_or;
            this.tbtn_C_and_or.Command = new RelayCommand((x) => OnAND2ORToggle_Category());

            this.btn_categories.Command = new RelayCommand((x) => OnHighlightSelectedCategories());

            // manager type highlighting
            this.existing_manager_types = new List<SelectableString>(ComponentUtils.MANAGER_TYPES_SELECTABLE);
            this.cb_managers.ItemsSource = this.existing_manager_types;

            this.managers_or = true;
            this.tbtn_MT_and_or.IsChecked = this.managers_or;
            this.tbtn_MT_and_or.Command = new RelayCommand((x) => OnAND2ORToggle_ManagerType());
            
            this.btn_managers.Command = new RelayCommand((x) => OnHighlightSelectedManagerTypes());

            // canvas resizing
            this.btn_resize_canv.Command = new RelayCommand((x) => this.canv.FitSize2Content());
            this.btn_expX_canv.Command = new RelayCommand((x) => this.canv.ExpandCanvas(CANVAS_EXPANSION_STEP, 0));
            this.btn_expY_canv.Command = new RelayCommand((x) => this.canv.ExpandCanvas(0, CANVAS_EXPANSION_STEP));
            // canvas saving            
            this.btn_save_canv.Command = new RelayCommand(
                (x) =>
                {
                    // resize to avoid bug in the canvas rendering
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.Width = this.MinWidth;
                    this.Height = this.MinHeight;
                    this.scrl_main.ScrollToTop();
                    this.scrl_main.ScrollToLeftEnd();
                    this.canv.SaveCanvasAsImage();
                });
        }

        #endregion

        #region METHODS

        public void SelectComponentInGraph(Component _comp)
        {
            if (this.canv == null) return;
            this.canv.SelectComponent(_comp);
        }

        #endregion

        #region COMMANDS

        private void OnHighlightSelectedCategories()
        {
            if (this.comp_manager == null) return;

            List<string> selected_categories = new List<string>(this.existing_categories.Where(x => x.IsSelected == true).Select(x => x.ObjectData));
            List<Category> sc = new List<Category>();
            foreach (string cat in selected_categories)
            {
                sc.Add(ComponentUtils.StringToCategory(cat));
            }

            this.canv.Highlight(sc, this.category_or);           
        }

        private void OnHighlightSelectedManagerTypes()
        {
            if (this.comp_manager == null) return;

            List<string> selected_managers = new List<string>(this.existing_manager_types.Where(x => x.IsSelected == true).Select(x => x.ObjectData));
            List<ComponentManagerType> sm = new List<ComponentManagerType>();
            foreach (string man in selected_managers)
            {
                sm.Add(ComponentUtils.StringToComponentManagerType(man));
            }

            this.canv.Highlight(sm, this.managers_or);                     
        }

        private void OnAND2ORToggle_Category()
        {
            this.category_or = !this.category_or;
            if (this.category_or)
            {
                Image im = new Image();
                im.Source = new BitmapImage(new Uri(@"../Data/icons/a_OR.png", UriKind.Relative));
                tbtn_C_and_or.Content = im;
            }
            else
            {
                Image im = new Image();
                im.Source = new BitmapImage(new Uri(@"../Data/icons/a_AND.png", UriKind.Relative));
                tbtn_C_and_or.Content = im;
            }
                
        }

        private void OnAND2ORToggle_ManagerType()
        {
            this.managers_or = !this.managers_or;
            if (this.managers_or)
            {
                Image im = new Image();
                im.Source = new BitmapImage(new Uri(@"../Data/icons/a_OR.png", UriKind.Relative));
                tbtn_MT_and_or.Content = im;
            }
            else
            {
                Image im = new Image();
                im.Source = new BitmapImage(new Uri(@"../Data/icons/a_AND.png", UriKind.Relative));
                tbtn_MT_and_or.Content = im;
            }

        }

        #endregion

    }
}
