using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.ComponentModel;
using VoiceChat.Model;

namespace VoiceChat.ViewModel
{
    public class VoiceChatVM: INotifyPropertyChanged
    {
        // Объект модели
        private VoiceChatModel model;

        // Состояния 
        #region ModelStates

        public bool WaitCall
        {
            get
            {
                return model.State == VoiceChatModel.States.WaitCall;
            }
        }

        public bool OutcomingCall
        {
            get
            {
                return model.State == VoiceChatModel.States.OutcomingCall;
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

        public VoiceChatVM()
        {
            model = new VoiceChatModel();

            model.PropertyChanged += VM_StatesChanged;

            InitializeCommands();
        }

        private void VM_StatesChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("WaitCall");
            OnPropertyChanged("OutcomingCall");
            OnPropertyChanged("IncomingCall");
            OnPropertyChanged("Talk");
        }

        // Привязка событий к командам
        private void InitializeCommands()
        {
            BeginCall = new Command(BeginCall_Executed);
            EndCall = new Command(EndCall_Executed);
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        // Команда вызова
        public Command BeginCall { get; set; }
        public void BeginCall_Executed(object parameter)
        {
            model.BeginCall();
        }

        // Команда завершения вызова
        public Command EndCall { get; set; }
        public void EndCall_Executed(object parameter)
        {
            model.EndCall();
        }

        // Закрытие приложения
        public void Closing_Executed(object sender, EventArgs e)
        {
            model.Closing();
        }
    }
}
