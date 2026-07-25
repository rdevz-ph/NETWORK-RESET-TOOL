using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetworkResetTool.Models
{
    public enum StepStatus
    {
        Pending,
        Running,
        Success,
        Failed
    }

    public class ResetStep : INotifyPropertyChanged
    {
        private StepStatus _status = StepStatus.Pending;
        private string _statusText = "Pending";
        private bool _isSelected = true;

        public string Command { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public StepStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusBrush));
                    OnPropertyChanged(nameof(IconData));
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

        public string StatusBrush
        {
            get
            {
                return Status switch
                {
                    StepStatus.Pending => "#757575",  // Slate Gray
                    StepStatus.Running => "#2196F3",  // Material Blue
                    StepStatus.Success => "#4CAF50",  // Material Green
                    StepStatus.Failed => "#F44336",   // Material Red
                    _ => "#757575"
                };
            }
        }

        public string IconData
        {
            get
            {
                return Status switch
                {
                    // Clock/Pending outline
                    StepStatus.Pending => "M12,20A8,8 0 1,1 20,12A8,8 0 0,1 12,20M12,2A10,10 0 1,0 22,12A10,10 0 0,0 12,2M12.5,7H11V13L16.25,16.15L17,14.92L12.5,12.25V7Z",
                    // Refresh/Spinner path
                    StepStatus.Running => "M12,4V2A10,10 0 0,0 2,12H4A8,8 0 0,1 12,4Z",
                    // Checkmark path
                    StepStatus.Success => "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z",
                    // Cross path
                    StepStatus.Failed => "M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z",
                    _ => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2"
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
