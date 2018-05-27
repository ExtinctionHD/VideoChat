using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using VoiceChat.Model;

namespace VoiceChat.ViewModel
{
    public class VoiceChatVM: INotifyPropertyChanged
    {
        // Объект модели
        private VoiceChatModel model;

        // Состояния 
        #region ModelStates

        public bool Connected
        {
            get
            {
                return model.Connected;
            }
        }

        public bool Disconnected
        {
            get
            {
                return !model.Connected;
            }
        }

        public bool WaitCall
        {
            get
            {
                return model.State == VoiceChatModel.States.WaitCall;
            }
        }

        public bool OutgoingCall
        {
            get
            {
                return model.State == VoiceChatModel.States.OutgoingCall;
            }
        }

        public bool IncomingCall
        {
            get
            {
                return model.State == VoiceChatModel.States.IncomingCall;
            }
        }

        public bool Talk
        {
            get
            {
                return model.State == VoiceChatModel.States.Talk;
            }
        }

        #endregion

        public string LocalIP
        {
            get
            {
                return model.LocalIP.ToString();
            }
            set { }
        }

        public string RemoteIP
        {
            get
            {
                return model.RemoteIP?.ToString();
            }
            set
            {
                try
                {
                    model.RemoteIP = IPAddress.Parse(value);
                }
                catch { model.RemoteIP = null; }
                OnPropertyChanged("RemoteIP");
            }
        }

        public string CallTime
        {
            get
            {
                return model.CallTime.ToString("c");
            }
        }

        public VoiceChatVM()
        {
            model = new VoiceChatModel();
            model.PropertyChanged += VM_StatesChanged;

            InitializeCommands();
        }

        private void VM_StatesChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("WaitCall");
            OnPropertyChanged("OutgoingCall");
            OnPropertyChanged("IncomingCall");
            OnPropertyChanged("Talk");
            OnPropertyChanged("RemoteIP");
            OnPropertyChanged("CallTime");
        }

        // Привязка событий к командам
        private void InitializeCommands()
        {
            BeginCall = new Command(BeginCall_Executed, (obj) => RemoteIP != null);
            EndCall = new Command(EndCall_Executed);
            AcceptCall = new Command(AcceptCall_Executed);
            DeclineCall = new Command(DeclineCall_Executed);
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        // Команда вызова
        public Command BeginCall { get; set; }
        private async void BeginCall_Executed(object parameter)
        {
            await Task.Run(() => model.BeginCall());
        }

        // Команда завершения вызова
        public Command EndCall { get; set; }
        private void EndCall_Executed(object parameter)
        {
            model.EndCall();
        }

        // Команда завершения вызова
        public Command AcceptCall { get; set; }
        private void AcceptCall_Executed(object parameter)
        {
            model.AcceptCall();
        }

        // Команда завершения вызова
        public Command DeclineCall { get; set; }
        private void DeclineCall_Executed(object parameter)
        {
            model.DeclineCall();
        }

        // Закрытие приложения
        public void Closing_Executed(object sender, EventArgs e)
        {
            model.Closing();
        }
    }
}
