using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace ComponentBuilder.WinUtils
{
    public class GeneralCommands
    {
        public ICommand CopyToClipboradCmd { get; private set; }

        public GeneralCommands()
        {
            this.CopyToClipboradCmd = new RelayCommand((x) => OnCopyToClipboard(x),
                                            (x) => CanExecute_OnCopyToClipboard(x));
        }

        #region COMMANDS

        private void OnCopyToClipboard(object x)
        {
            Clipboard.SetText(x.ToString());
        }

        private bool CanExecute_OnCopyToClipboard(object x)
        {
            return (x != null);
        }

        #endregion
    }
}
