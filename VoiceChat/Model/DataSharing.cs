using BDTP;
using NAudio.Wave;
using System;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

namespace VoiceChat.Model
{
    public abstract class DataSharing
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        public int LineIndex { get; set; }

        private readonly VoiceChatModel model;

        protected BdtpClient BdtpClient
        {
            get => model.bdtpClient;
        }

        private Thread receiveThread;

        public DataSharing(VoiceChatModel model)
        {
            this.model = model;
        }

        public abstract void BeginSend();

        public virtual void BeginReceive()
        {
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.Start();
        }

        public abstract void EndSend();

        public virtual void EndReceive()
        {
            receiveThread?.Abort();
        }

        protected virtual void Send(object sender, EventArgs e)
        {
            if (model.State != ModelStates.Talk)
            {
                return;
            }
        }

        protected void ReceiveLoop()
        {
            while (BdtpClient.Connected && model.State == ModelStates.Talk)
            {
                Receive();
                Thread.Sleep(0);
            }
        }

        protected abstract void Receive();
    }
}
