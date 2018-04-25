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
    public class VoiceChatVM
    {
        private VoiceChatModel model;

        public string LocalIP
        {
            get
            {
                return model.LocalIP.ToString();
            }
        }

        public VoiceChatVM() => model = new VoiceChatModel();

        public void Closing(object sender, EventArgs e)
        {
            model.Closing();
        }
    }
}
