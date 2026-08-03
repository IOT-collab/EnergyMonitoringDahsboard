using IECGUI.ViewModel;
using IECGUI;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace IECGUI.ViewModel
{
    public class CustomMessageBoxViewModel : ObservableObjectVM
    {
        private string _title;
        private string _message;
        private string _yesText;
        private string _noText;
        private bool _isCancelVisible;
        private SolidColorBrush _yesButtonBrush;

        public Action<bool> CloseRequested;

        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }

        // Constructor
        public CustomMessageBoxViewModel(string message,
            string title, string yesText, string noText, 
            bool isConfirmation) 
        {
            _message = message;
            _title = title;
            _yesText = yesText;
            _noText = noText;

            // Logic: If it's a confirmation (Yes/No), show Cancel button and Red color.
            // If it's just Info (OK), hide Cancel button and use Blue color.
            _isCancelVisible = isConfirmation;

            // Define Colors manually or load from resources if you prefer strict MVVM. 
            // For simplicity, we set them here.
            if (isConfirmation)
            {
                // Red for Danger/Logout
                _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545"));
            }
            else
            {
                // Blue for Info/OK (Matches your Theme InfoBrush)
                _yesButtonBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            }

            YesCommand = new RelayCommand(OnYes);
            NoCommand = new RelayCommand(OnNo);
        }

        // Properties
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public string YesText { get => _yesText; set => SetProperty(ref _yesText, value); }
        public string NoText { get => _noText; set => SetProperty(ref _noText, value); }

        public bool IsCancelVisible { get => _isCancelVisible; set => SetProperty(ref _isCancelVisible, value); }
        public SolidColorBrush YesButtonBrush { get => _yesButtonBrush; set => SetProperty(ref _yesButtonBrush, value); }

        private void OnYes() => CloseRequested?.Invoke(true);
        private void OnNo() => CloseRequested?.Invoke(false);
    }
}