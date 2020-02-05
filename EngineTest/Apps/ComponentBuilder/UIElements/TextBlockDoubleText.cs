using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Globalization;
using System.ComponentModel;

namespace ComponentBuilder.UIElements
{
    public class TextBlockDoubleText : TextBlock
    {
        #region PROPERTIES: Event Handler

        public Action<object> TextChangedEventHandler
        {
            get { return (Action<object>)GetValue(TextChangedEventHandlerProperty); }
            set { SetValue(TextChangedEventHandlerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextChangedEventHandler.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextChangedEventHandlerProperty =
            DependencyProperty.Register("TextChangedEventHandler", typeof(Action<object>), typeof(TextBlockDoubleText), 
            new PropertyMetadata(null));

        #endregion

        #region PROPERTIES: Text Copy

        public string  TextCopy
        {
            get { return (string )GetValue(TextCopyProperty); }
            set { SetValue(TextCopyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextCopy.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextCopyProperty =
            DependencyProperty.Register("TextCopy", typeof(string ), typeof(TextBlockDoubleText),
            new UIPropertyMetadata(string.Empty, new PropertyChangedCallback(TextCopyPropertyChangedCallback)));

        private static void TextCopyPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBlockDoubleText instance = d as TextBlockDoubleText;
            if (instance == null) return;
            instance.Text = instance.TextCopy;
            if (instance.TextChangedEventHandler != null)
                instance.TextChangedEventHandler.Invoke(instance);
        }

        #endregion

        #region PROPETIES: Nr of Clicks

        public int NrClicks { get; private set; }

        #endregion

        public TextBlockDoubleText()
            :base()
        {
            this.NrClicks = 0;
            this.MouseUp += TextBlockDoubleText_MouseUp;
        }

        private void TextBlockDoubleText_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.NrClicks++;
        }
    }
}
