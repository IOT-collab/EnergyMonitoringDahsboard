using IECGUI.ViewModel;
using IECGUI;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace IECGUI.ViewModel
{
    public enum CustomMessageBoxResult
    {
        Yes,
        No,
        Cancel
    }

    public class CustomMessageBoxViewModel : ObservableObjectVM
    {
        private string _title;
        private string _message;
        private string _yesText;
        private string _noText;
        private string _cancelText;
        private bool _isCancelVisible;
        private bool _isCancelButtonVisible;
        private SolidColorBrush _yesButtonBrush;

        public Action<bool> CloseRequested;
        public Action<CustomMessageBoxResult> ResultRequested;

        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }
        public ICommand CancelCommand { get; }

        public CustomMessageBoxViewModel(string message,
            string title, string yesText, string noText, 
            bool isConfirmation)
            : this(message, title, yesText, noText, string.Empty, isConfirmation)
        {
        }

        public CustomMessageBoxViewModel(string message,
            string title, string yesText, string noText, string cancelText)
            : this(message, title, yesText, noText, cancelText, true)
        {
        }

        private CustomMessageBoxViewModel(string message,
            string title, string yesText, string noText,
            string cancelText, bool isConfirmation)
        {
            _message = message;
            _title = title;
            _yesText = yesText;
            _noText = noText;
            _cancelText = cancelText ?? string.Empty;

            _isCancelVisible = isConfirmation;
            _isCancelButtonVisible = !string.IsNullOrWhiteSpace(_cancelText);

            if (isConfirmation)
            {
                _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
            }
            else
            {
                _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            }

            YesCommand = new RelayCommand(OnYes);
            NoCommand = new RelayCommand(OnNo);
            CancelCommand = new RelayCommand(OnCancel);
        }

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public string YesText { get => _yesText; set => SetProperty(ref _yesText, value); }
        public string NoText { get => _noText; set => SetProperty(ref _noText, value); }
        public string CancelText { get => _cancelText; set => SetProperty(ref _cancelText, value); }

        public bool IsCancelVisible { get => _isCancelVisible; set => SetProperty(ref _isCancelVisible, value); }
        public bool IsCancelButtonVisible { get => _isCancelButtonVisible; set => SetProperty(ref _isCancelButtonVisible, value); }
        public SolidColorBrush YesButtonBrush { get => _yesButtonBrush; set => SetProperty(ref _yesButtonBrush, value); }

        private void OnYes()
        {
            if (ResultRequested != null)
                ResultRequested(CustomMessageBoxResult.Yes);
            else
                CloseRequested?.Invoke(true);
        }

        private void OnNo()
        {
            if (ResultRequested != null)
                ResultRequested(CustomMessageBoxResult.No);
            else
                CloseRequested?.Invoke(false);
        }

        private void OnCancel() => ResultRequested?.Invoke(CustomMessageBoxResult.Cancel);
    }
}
