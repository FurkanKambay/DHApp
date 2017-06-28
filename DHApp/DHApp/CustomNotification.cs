using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToastNotifications.Core;

namespace DHApp
{
    public class CustomNotification : NotificationBase, INotifyPropertyChanged
    {
        private string _content;
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        private string _url;
        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged();
            }
        }

        public CustomNotification(string content, string url = null)
        {
            Content = content;
            Url = url;
        }

        private CustomDisplayPart displayPart;
        public override NotificationDisplayPart DisplayPart => displayPart ?? (displayPart = new CustomDisplayPart(this));

        protected void OnPropertyChanged([CallerMemberName]string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
