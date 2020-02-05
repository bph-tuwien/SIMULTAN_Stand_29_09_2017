using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;

namespace ComponentBuilder.WinUtils
{
    public struct TwoObjects
    {
        public object object1;
        public object object2;
    }
    public class RelayCommandTwoIn : ICommand
    {
        private readonly Predicate<object> _canExecute1;
        private readonly Predicate<object> _canExecute2;
        private readonly Action<object, object> _execute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommandTwoIn(Action<object, object> execute)
            : this(execute, null, null)
        {
        }

        public RelayCommandTwoIn(Action<object, object> execute, Predicate<object> canExecute1, Predicate<object> canExecute2)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            this._execute = execute;
            this._canExecute1 = canExecute1;
            this._canExecute2 = canExecute2;
        }

        [DebuggerStepThrough]
        private bool CanExecute1(object parameter)
        {
            if (this._canExecute1 != null)
            {
                return this._canExecute1(parameter);
            }
            return true;
        }

        [DebuggerStepThrough]
        private bool CanExecute2(object parameter)
        {
            if (this._canExecute2 != null)
            {
                return this._canExecute2(parameter);
            }
            return true;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            if (parameter is TwoObjects)
            {
                TwoObjects pair = (TwoObjects)parameter;
                return CanExecute1(pair.object1) && CanExecute2(pair.object2);
            }
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is TwoObjects)
            {
                TwoObjects pair = (TwoObjects)parameter;
                this._execute(pair.object1, pair.object2);
            }
        }

    }
}
