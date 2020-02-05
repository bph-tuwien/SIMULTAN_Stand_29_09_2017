using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GeometryViewer.UIElements
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        public ColorPickerWindow()
        {
            InitializeComponent();
        }

        public Color PickedColor { get; private set; }
        public int PickedIndex { get; private set; }

        // if a selection is made in one listbox, invalidate the selection in all others
        private void lb_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ListBox lbSender = sender as ListBox;
            if (lbSender == null)
                return;

            // saved picked color
            object pickedObj = lbSender.SelectedItem;
            if(pickedObj != null)
            {
                IndexColor pickedIC = pickedObj as IndexColor;
                if (pickedIC != null)
                {
                    this.PickedColor = pickedIC.Color;
                    this.PickedIndex = pickedIC.Index;
                }
            }

            // invalidate selection in other listboxes
            var listBoxes = AppHelpers.FindVisualChildren<ListBox>(this);
            foreach(ListBox lb in listBoxes)
            {
                if (lb != lbSender)
                {
                    lb.SelectedItem = null;
                }
            }
        }


    }
}
