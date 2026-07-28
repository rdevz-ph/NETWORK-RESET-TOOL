using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetworkResetTool.Models
{
    public class SharingStatusItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _serviceName = string.Empty;
        private string _statusText = "Unknown";
        private bool _isActive = false;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ServiceName
        {
            get => _serviceName;
            set
            {
                if (_serviceName != value)
                {
                    _serviceName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusBrush));
                    OnPropertyChanged(nameof(CanStart));
                }
            }
        }

        public bool CanStart => !IsActive;

        public string StatusBrush => IsActive ? "#4CAF50" : "#F44336"; // Green if active, Red if inactive

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
