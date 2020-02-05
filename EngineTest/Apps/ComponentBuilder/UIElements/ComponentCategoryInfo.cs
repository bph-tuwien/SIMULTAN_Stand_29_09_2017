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
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Input;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;

using ParameterStructure.Component;

namespace ComponentBuilder.UIElements
{
    class ComponentCategoryInfo : Grid, INotifyPropertyChanged
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

        #region PROPERTIES: Category

        public Category CategoryToShow
        {
            get { return (Category)GetValue(CategoryToShowProperty); }
            set { SetValue(CategoryToShowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CategoryToShow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CategoryToShowProperty =
            DependencyProperty.Register("CategoryToShow", typeof(Category), typeof(ComponentCategoryInfo),
            new UIPropertyMetadata(Category.MSR, new PropertyChangedCallback(CategoryToShowPropertyChangedCallback)));

        private static void CategoryToShowPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComponentCategoryInfo instance = d as ComponentCategoryInfo;
            if (instance == null) return;
            if (instance.IsLoaded)
                instance.AdaptTextToCategory();
        }

        #endregion

        #region CLASS MEMBERS

        private bool size_original_set = false;
        protected double width_original = 0;
        protected double height_original = 0;

        protected int nr_columns = Enum.GetNames(typeof(Category)).Length - 2;
        protected int nr_rows = 1;
        protected int column_width = 22;
        protected int row_height = 26;

        private StackPanel sp_Symbols = null;

        #endregion

        #region .CTOR

        public ComponentCategoryInfo()
            :base()
        {
            this.Loaded += ComponentCategoryInfo_Loaded;
        }

        #endregion

        #region METHODS: Grid Realize

        protected void CalculateSize()
        {
            // set the original sizes, if not set already
            if (!this.size_original_set)
            {
                this.width_original = this.Width;
                this.height_original = this.Height;
                this.size_original_set = true;
            }

            // re-calculate the size of the grid
            this.Width = this.nr_columns * this.column_width;
            this.Height = this.nr_rows * this.row_height;
        }

        private void PopulteGrid()
        {
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Effect = new DropShadowEffect()
            {
                Color = (Color)ColorConverter.ConvertFromString("#FF551700"),
                Direction = 315,
                ShadowDepth = 3,
                BlurRadius = 3,
                Opacity = 0.5
            };

            string category_basic = ComponentUtils.CATEGORY_NONE_AS_STR;
            for (int i = 0; i < this.nr_columns; i++)
            {
                // category symbols
                TextBlockDoubleText tb = new TextBlockDoubleText();
                tb.Width = 22;
                tb.Height = 26;
                tb.TextCopy = category_basic[i].ToString();
                tb.ToolTip = "IST NICHT " + ComponentUtils.CategoryStringToDescription(tb.Text.ToUpper());
                tb.Padding = new Thickness(2, 2, 1, 1);
                tb.FontSize = 20;
                tb.FontFamily = new FontFamily(new Uri("pack://application:,,,/ComponentBuilder;component/Data/fonts/"), "./#categories");
                tb.Style = (Style)tb.TryFindResource("CategoryLabel");
                tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00044d"));
                tb.TextChangedEventHandler += TextBlock_category_AppearanceAccToContent;
                tb.Tag = i;

                sp.Children.Add(tb);
            }

            Grid.SetRow(sp, 0);
            Grid.SetColumn(sp, 0);
            this.sp_Symbols = sp;
            this.Children.Add(sp);
        }

        #endregion

        #region EVENT HANDLERS

        private void ComponentCategoryInfo_Loaded(object sender, RoutedEventArgs e)
        {
            this.CalculateSize();
            this.PopulteGrid();
            this.AdaptTextToCategory();
        }

        private void TextBlock_category_AppearanceAccToContent(object sender)
        {
            TextBlock tb = sender as TextBlock;
            if (tb == null) return;

            if (ComponentUtils.LOWER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.ToolTip = "IST NICHT " + ComponentUtils.CategoryStringToDescription(tb.Text);
                tb.Effect = null;
            }
            else if (ComponentUtils.UPPER_CASE_SINGLE.IsMatch(tb.Text))
            {
                tb.ToolTip = "IST " + ComponentUtils.CategoryStringToDescription(tb.Text);
                tb.Effect = new DropShadowEffect()
                {
                    Color = (Color)ColorConverter.ConvertFromString("#FF0000ff"),
                    Direction = 315,
                    ShadowDepth = 2,
                    BlurRadius = 1,
                    Opacity = 1
                };
            }
        }

        private void AdaptTextToCategory()
        {
            if (this.sp_Symbols == null) return;

            string category_current = ComponentUtils.CategoryToString(this.CategoryToShow);
            foreach (var child in this.sp_Symbols.Children)
            {
                TextBlockDoubleText tbDT = child as TextBlockDoubleText;
                if (tbDT == null) continue;
                if (tbDT.Tag == null) continue;
                if (!(tbDT.Tag is int)) continue;

                tbDT.TextCopy = category_current[(int)tbDT.Tag].ToString();
            }
        }

        #endregion
    }
}
