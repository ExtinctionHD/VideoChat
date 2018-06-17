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
using BDTP;

namespace VoiceChat.Model
{
    public class AudioSharing : DataSharing
    {
        private WaveIn input;
        private WaveOut output;
        private BufferedWaveProvider bufferStream;

        public AudioSharing(VoiceChatModel model) : base(model)
        {
            LineIndex = 0;

            // Cоздаем поток для записи нашей речи определяем его формат - 
            // частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
            input = new WaveIn();
            input.WaveFormat = new WaveFormat(8000, 16, 1);

            // Создание потока для прослушивания входящиего звука
            bufferStream = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            output = new WaveOut();
            output.Init(bufferStream);
        }

        public override void BeginSend()
        {
            input.DataAvailable += Send;
            input.StartRecording();
        }

        public override void BeginReceive()
        {
            bufferStream.ClearBuffer();
            output.Play();

            base.BeginReceive();
        }

        public override void EndSend()
        {
            input.StopRecording();
            input.DataAvailable -= Send;
        }

        public override void EndReceive()
        {
            base.EndReceive();
            output.Stop();
        }

        protected override void Send(object sender, EventArgs e)
        {
            base.Send(sender, e);
            BdtpClient.Send((e as WaveInEventArgs).Buffer, LineIndex);
        }

        protected override void Receive()
        {
            byte[] data = BdtpClient.Receive(LineIndex);
            bufferStream.AddSamples(data, 0, data.Length);
        }
    }
}
