using System;
using System.Windows.Input;

public class RelayCommand : ICommand
{
    private readonly Action<object> _executeWithParam;
    private readonly Action _executeWithoutParam;
    private readonly Func<object, bool> _canExecute;

    // 🔹 Para comandos con parámetro
    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _executeWithParam = execute;
        _canExecute = canExecute;
    }

    // 🔹 Para comandos SIN parámetro (legacy seguro)
    public RelayCommand(Action execute)
    {
        _executeWithoutParam = execute;
    }

    public bool CanExecute(object parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object parameter)
    {
        if (_executeWithParam != null)
            _executeWithParam(parameter);
        else
            _executeWithoutParam?.Invoke();
    }

    public event EventHandler CanExecuteChanged;

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}