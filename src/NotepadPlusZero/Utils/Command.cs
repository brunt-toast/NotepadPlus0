using System;
using System.Windows.Input;

namespace NotepadPlusZero.Utils
{
    public class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Action _action;
        private Func<bool> _canExecute;

        public Command(Action action, Func<bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute.Invoke();
        public void Execute(object parameter) => _action.Invoke();
    }
}
