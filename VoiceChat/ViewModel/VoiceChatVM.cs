using System;
using System.Windows.Media;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using VoiceChat.Model;
using BDTP;
using NAudio.Wave;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Windows.Interop;


namespace VoiceChat.ViewModel
{
    public class VoiceChatVM: INotifyPropertyChanged
    {
        // Объект модели
        private VoiceChatModel model;

        public ImageSource RemoteFrame
        {
            get
            {
                return model.video.RemoteFrame;
            }
        }
        public ImageSource LocalFrame
        {
            get
            {
                return model.video.LocalFrame;
            }
        }

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
                return model.State == ModelStates.WaitCall;
            }
        }

        public bool OutgoingCall
        {
            get
            {
                return model.State == ModelStates.OutgoingCall;
            }
        }

        public bool IncomingCall
        {
            get
            {
                return model.State == ModelStates.IncomingCall;
            }
        }

        public bool Talk
        {
            get
            {
                return model.State == ModelStates.Talk;
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
                return model.callTimer.CallTime.ToString("c");
            }
        }

        public VoiceChatVM()
        {
            model = new VoiceChatModel();

            InitializeEvents();
            InitializeCommands();
        }

        // Привязка событий к командам
        private void InitializeCommands()
        {
            BeginCall = new Command(BeginCall_Executed, (obj) => RemoteIP != null);
            EndCall = new Command(EndCall_Executed);
            AcceptCall = new Command(AcceptCall_Executed);
            DeclineCall = new Command(DeclineCall_Executed);
        }

        private void InitializeEvents()
        {
            model.PropertyChanged += VM_PropertyChanged;
            model.callTimer.PropertyChanged += (sender, e) => OnPropertyChanged("CallTime");
            model.video.PropertyChanged += (sender, e) =>
            {
                OnPropertyChanged("RemoteFrame");
                OnPropertyChanged("LocalFrame");
            };
        }

        private void VM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("WaitCall");
            OnPropertyChanged("OutgoingCall");
            OnPropertyChanged("IncomingCall");
            OnPropertyChanged("Talk");
            OnPropertyChanged("RemoteIP");
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
