using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Resources;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Media.Imaging;

using ComponentBuilder.WinUtils;
using ComponentBuilder.UIElements;
using ParameterStructure.Parameter;
using ParameterStructure.Component;

namespace ComponentBuilder.WpfUtils
{
    class TreeViewItemCompResult : TreeViewItem
    {
        public TreeViewItemCompResult()
            :base()
        {
            // header highlighting handling
            this.MouseEnter += eh_MouseEnter;
            this.MouseLeave += eh_MouseLeave;
            this.MouseDoubleClick += eh_MouseDoubleClick;
        }

        

        #region DEPENDENCY PROPERTIES

        public Component OtherParentComp
        {
            get { return (Component)GetValue(ParentCompProperty); }
            set { SetValue(ParentCompProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OtherParentComp.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentCompProperty =
            DependencyProperty.Register("OtherParentComp", typeof(Component), typeof(TreeViewItemCompResult), 
            new UIPropertyMetadata(null));


        public ComponentManagerType User
        {
            get { return (ComponentManagerType)GetValue(UserProperty); }
            set { SetValue(UserProperty, value); }
        }

        // Using a DependencyProperty as the backing store for User.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(ComponentManagerType), typeof(TreeViewItemCompResult),
            new PropertyMetadata(ComponentManagerType.GUEST));

        public CompCompareWindow ParentWindow { get; internal set; }

        #endregion

        #region OVERRIDES

        protected override DependencyObject GetContainerForItemOverride()
        {
            TreeViewItemCompResult container = new TreeViewItemCompResult();
            container.OtherParentComp = this.OtherParentComp;
            container.ParentWindow = this.ParentWindow;
            container.User = this.User;
            return container;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItemCompResult;
        }

        #endregion

        #region METHODS: Context Menu

        protected ContextMenu BuildContextMenu()
        {
            // menu according to header
            if (this.Header != null)
            {
                Parameter p = this.Header as Parameter;
                if (p != null)
                {
                    if (p.Buddy != null && p.CompResult < ComparisonResult.SAME)
                    {
                        ContextMenu cm = new ContextMenu();
                        cm.UseLayoutRounding = true;
                        MenuItem mi1 = new MenuItem();
                        mi1.Header = "Eigenschaften übernehmen";
                        mi1.Command = new RelayCommand((x) => p.AdoptPropertiesOfBuddy());
                        mi1.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_OK.png", UriKind.Relative)), Width = 16, Height = 16 };
                        cm.Items.Add(mi1);
                        return cm;
                    }
                    else if (p.Buddy == null && p.CompResult == ComparisonResult.UNIQUE && this.OtherParentComp != null)
                    {
                        ContextMenu cm = new ContextMenu();
                        cm.UseLayoutRounding = true;
                        MenuItem mi1 = new MenuItem();
                        mi1.Header = "in andere Komponente übertragen";
                        mi1.Command = new RelayCommand((x) => this.OtherParentComp.AddCopyOfParameter(p, this.User));
                        mi1.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_REF_on.png", UriKind.Relative)), Width = 16, Height = 16 };
                        cm.Items.Add(mi1);
                        return cm;
                    }
                }
                Calculation calc = this.Header as Calculation;
                if (calc != null)
                {
                    if (calc.Buddy != null && calc.CompResult == ComparisonResult.SAMENAMEUNIT_DIFFVALUE)
                    {
                        ContextMenu cm = new ContextMenu();
                        cm.UseLayoutRounding = true;
                        MenuItem mi1 = new MenuItem();
                        mi1.Header = "Ausdruck übernehmen";
                        mi1.Command = new RelayCommand((x) => calc.AdoptPropertiesOfBuddy());
                        mi1.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_OK.png", UriKind.Relative)), Width = 16, Height = 16 };
                        cm.Items.Add(mi1);
                        return cm;
                    }
                    else if (calc.Buddy == null && calc.CompResult == ComparisonResult.UNIQUE && this.OtherParentComp != null)
                    {
                        ContextMenu cm = new ContextMenu();
                        cm.UseLayoutRounding = true;
                        MenuItem mi1 = new MenuItem();
                        mi1.Header = "in andere Komponente übertragen";
                        mi1.Command = new RelayCommand((x) => this.OtherParentComp.AddCopyOfCalculation(calc, this.User));
                        mi1.Icon = new Image { Source = new BitmapImage(new Uri(@"../Data/icons/menu/menu_REF_on.png", UriKind.Relative)), Width = 16, Height = 16 };
                        cm.Items.Add(mi1);
                        return cm;
                    }
                }
            }

            return null;
        }

        #endregion

        #region EVENT HANDLERS: Header Comparison

        private void eh_MouseLeave(object sender, MouseEventArgs e)
        {
            TreeViewItemCompResult tvie = sender as TreeViewItemCompResult;
            // header-highlighting preparation
            if (tvie != null && tvie.Header is ParameterStructure.Parameter.ComparableContent)
            {
                ParameterStructure.Parameter.ComparableContent cc = tvie.Header as ParameterStructure.Parameter.ComparableContent;
                if (cc.Buddy != null)
                {
                    cc.Buddy.IsHLByBuddy = false;
                }
            }
        }

        private void eh_MouseEnter(object sender, MouseEventArgs e)
        {
            TreeViewItemCompResult tvie = sender as TreeViewItemCompResult;            
            // header-highlighting preparation
            if (tvie != null && tvie.Header is ParameterStructure.Parameter.ComparableContent)
            {
                tvie.ContextMenu = tvie.BuildContextMenu();
                ParameterStructure.Parameter.ComparableContent cc = tvie.Header as ParameterStructure.Parameter.ComparableContent;
                if (cc.Buddy != null)
                {
                    cc.Buddy.IsHLByBuddy = true;
                }
            }

        }

        private void eh_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItemCompResult tvie = sender as TreeViewItemCompResult;
            if (tvie != null && tvie.Header is ParameterStructure.Component.Component)
            {
                ParameterStructure.Component.Component comp = tvie.Header as ParameterStructure.Component.Component;
                if (comp != null && this.ParentWindow != null)
                {
                    if (this.OtherParentComp != null && this.ParentWindow.C1 != null && this.ParentWindow.C2 != null)
                    {
                        // choose this component for comparison...
                        if (this.OtherParentComp.ID == this.ParentWindow.C1.ID)
                            this.ParentWindow.C2 = comp;
                        else
                            this.ParentWindow.C1 = comp;
                    }
                }
            }
        }

        #endregion
    }
}
