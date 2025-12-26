using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ProjectReport.Services
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class ToastNotificationService : INotifyPropertyChanged
    {
        private static ToastNotificationService? _instance;
        public static ToastNotificationService Instance => _instance ??= new ToastNotificationService();

        private string _message = string.Empty;
        private ToastType _type;
        private bool _isVisible;
        private DispatcherTimer _timer;

        public event PropertyChangedEventHandler? PropertyChanged;

        private ToastNotificationService()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
        }

        public string Message
        {
            get => _message;
            private set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public ToastType Type
        {
            get => _type;
            private set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            private set
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        public void Show(string message, ToastType type = ToastType.Info, int durationSeconds = 3)
        {
            Message = message;
            Type = type;
            IsVisible = true;

            _timer.Stop();
            _timer.Interval = TimeSpan.FromSeconds(durationSeconds);
            _timer.Start();
        }

        public void ShowSuccess(string message) => Show(message, ToastType.Success);
        public void ShowError(string message) => Show(message, ToastType.Error);
        public void ShowWarning(string message) => Show(message, ToastType.Warning);
        public void ShowInfo(string message) => Show(message, ToastType.Info);

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            IsVisible = false;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
