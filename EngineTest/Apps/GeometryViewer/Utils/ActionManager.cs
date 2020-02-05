using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace GeometryViewer.Utils
{
    public class ActionManager
    {
        private static ActionManager Instance;

        static ActionManager()
        {
            UndoOneStepCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnUndoOneStepCommand(),
                                                            (x) => CanExecute_OnUndoOneStepCommand());
            RedoOneStepCmd = new HelixToolkit.SharpDX.Wpf.RelayCommand((x) => OnRedoOneStepCommand(),
                                                            (x) => CanExecute_OnRedoOneStepCommand());
        }
        private ActionManager()
        { }

        public static ActionManager GetInstance()
        {
            if (Instance == null)
                Instance = new ActionManager();
            return Instance;
        }

        private static int NR_ALL_ACTIONS = 0;
        private static readonly int MAX_NR_SAVED_ACTIONS = 10;
        private static Stack<int> EXEC_STACK_UNDO = new Stack<int>();
        private static Stack<int> EXEC_STACK_REDO = new Stack<int>();

        private static Dictionary<int, Action<int>> ACTIONS_UNDO = new Dictionary<int, Action<int>>();
        private static Dictionary<int, Action<int>> ACTIONS_REDO = new Dictionary<int, Action<int>>();
        public static ICommand UndoOneStepCmd { get; private set; }
        public static ICommand RedoOneStepCmd { get; private set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ======================================= ACTION MANAGEMENT METHODS ====================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region ACTIONS ON DEPENDENCY PROPERTIES
        public static void RecordModifyCallback(DependencyObject caller, List<DependencyProperty> propRefs)
        {
            if (caller == null || propRefs == null)
                return;
            NR_ALL_ACTIONS++;

            Type callerT = caller.GetType();

            int nrChanges = propRefs.Count;
            List<Object> values = new List<object>();
            string debug = "";
            for(int i = 0; i < nrChanges; i++)
            {
                values.Add(caller.GetValue(propRefs[i]));
                debug += String.Format("SAVED\t {0} : {1:F2}\n", propRefs[i].Name, values[i]);
            }

            Action<int> change = delegate(int _ind)
            {
                string message = "";
                for(int i = 0; i < nrChanges; i++)
                {
                    caller.SetValue(propRefs[i], values[i]);
                    message += String.Format("{0} : {1:F2}\n", propRefs[i].Name, values[i]);
                }
                string header = String.Format("Executing Undo Step NR {0} ({1})", _ind, callerT.ToString());
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
            };

            ACTIONS_UNDO.Add(NR_ALL_ACTIONS, change);
            string debug_header = String.Format("Saving Undo Step NR {0} ({1})", NR_ALL_ACTIONS, callerT.ToString());
            MessageBox.Show(debug, debug_header, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region GENERAL ACTIONS
        public static void RecordActionCallback(DependencyObject caller, Func<int> undoAction, Func<int> redoAction)
        {
            if (caller == null || undoAction == null || redoAction == null)
                return;

            NR_ALL_ACTIONS++;

            Type callerT = caller.GetType();

            Action<int> undoRecord = delegate(int _ind)
            {
                int result = undoAction();
                // DEBUG
                //string message = String.Format("new value: {0}", result);
                //string header = String.Format("Executing Undo Step NR {0} ({1})", _ind, callerT.ToString());
                //MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
            };

            Action<int> redoRecord = delegate(int _ind)
            {
                int result = redoAction();
                // DEBUG
                //string message = String.Format("new value: {0}", result);
                //string header = String.Format("Executing Redo Step NR {0} ({1})", _ind, callerT.ToString());
                //MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
            };

            // update action lists
            ACTIONS_UNDO.Add(NR_ALL_ACTIONS, undoRecord);
            ACTIONS_REDO.Add(NR_ALL_ACTIONS, redoRecord);
            // DEBUG
            //string debug_header = String.Format("Saving Undo/Redo Step NR {0} ({1})", NR_ALL_ACTIONS, callerT.ToString());
            //MessageBox.Show("SAVED", debug_header, MessageBoxButton.OK, MessageBoxImage.Information);

            // update exec stacks          
            EXEC_STACK_UNDO.Push(NR_ALL_ACTIONS);
            EXEC_STACK_REDO.Clear();

            // clean-up action lists
            CleanUpActionLists();
            // string debug = PrintState();
        }
        #endregion

        #region MAINTENANCE OF THE ACTION LISTS AND STACKS
        private static void CleanUpActionLists()
        {
            int n = EXEC_STACK_UNDO.Count;
            int[] indices = EXEC_STACK_UNDO.ToArray();
            if (n > MAX_NR_SAVED_ACTIONS)
            {
                EXEC_STACK_UNDO.Clear();
                for (int i = n - 2; i >= 0; i--)
                {
                    EXEC_STACK_UNDO.Push(indices[i]);
                }
                indices = EXEC_STACK_UNDO.ToArray();
            }
            n = indices.Count();
           
            Dictionary<int, Action<int>> newActions_undo = new Dictionary<int, Action<int>>();
            Dictionary<int, Action<int>> newActions_redo = new Dictionary<int, Action<int>>();
            for(int i = n - 1; i >= 0; i--)
            {
                int index = indices[i];
                newActions_undo.Add(index, ACTIONS_UNDO[index]);
                newActions_redo.Add(index, ACTIONS_REDO[index]);
            }

            ACTIONS_UNDO = new Dictionary<int, Action<int>>(newActions_undo);
            ACTIONS_REDO = new Dictionary<int, Action<int>>(newActions_redo);
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // ================================================== COMMANDS ============================================ //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region COMMANDS
        private static void OnUndoOneStepCommand()
        {
            int execIndex = EXEC_STACK_UNDO.Pop();
            EXEC_STACK_REDO.Push(execIndex);
            Action<int> action = ACTIONS_UNDO[execIndex];
            action(execIndex);
            // string debug = PrintState();
        }

        private static bool CanExecute_OnUndoOneStepCommand()
        {
            return EXEC_STACK_UNDO.Count > 0;
        }

        private static void OnRedoOneStepCommand()
        {
            int execIndex = EXEC_STACK_REDO.Pop();
            EXEC_STACK_UNDO.Push(execIndex);
            Action<int> action = ACTIONS_REDO[execIndex];
            action(execIndex);
            // string debug = PrintState();
        }

        private static bool CanExecute_OnRedoOneStepCommand()
        {
            return EXEC_STACK_REDO.Count > 0;
        }
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // =================================================== UTILS ============================================== //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region DEBUG TO STRING
        private static string PrintState()
        {
            string state = "";
            int i;

            state += "ACTIONS UNDO: ";
            int[] actions_undo = ACTIONS_UNDO.Keys.ToArray();
            for (i = 0; i < actions_undo.Count(); i++)
            {
                state += actions_undo[i].ToString() + " ";
            }
            state += "\n";

            state += "ACTIONS REDO: ";
            int[] actions_redo = ACTIONS_REDO.Keys.ToArray();
            for (i = 0; i < actions_redo.Count(); i++)
            {
                state += actions_redo[i].ToString() + " ";
            }
            state += "\n";

            state += "EXEC STACK UNDO: ";
            int[] exec_undo = EXEC_STACK_UNDO.ToArray();
            for (i = 0; i < exec_undo.Count(); i++)
            {
                state += exec_undo[i].ToString() + " ";
            }
            state += "\n";

            state += "EXEC STACK REDO: ";
            int[] exec_redo = EXEC_STACK_REDO.ToArray();
            for (i = 0; i < exec_redo.Count(); i++)
            {
                state += exec_redo[i].ToString() + " ";
            }

            return state;
        }
        #endregion
    }
}
