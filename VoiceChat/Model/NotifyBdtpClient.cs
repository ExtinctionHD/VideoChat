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
using BDTP;

namespace VoiceChat.Model
{
    public class NotifyBdtpClient: BdtpClient, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        public event Action Disconnected;

        public override IPAddress RemoteIP
        {
            get => base.RemoteIP;
            protected set
            {
                base.RemoteIP = value;
                OnPropertyChanged("RemoteIP");
                OnPropertyChanged("Connected");
            }
        }

        public NotifyBdtpClient(IPAddress localIP): base(localIP) { }

        protected override void WaitForDisconnect()
        {
            base.WaitForDisconnect();

            Disconnected?.Invoke();
        }
    }
}
