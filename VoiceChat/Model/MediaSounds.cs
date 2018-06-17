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
    public class MediaSounds
    {
        private readonly VoiceChatModel model;

        private MediaPlayer ringtone;
        private MediaPlayer dialtone;

        public MediaSounds(VoiceChatModel model)
        {
            this.model = model;

            LoadMedia(ref ringtone, "Source/Ringtone.mp3");

            LoadMedia(ref dialtone, "Source/Dialtone.mp3");
            dialtone.Volume = 0.1;
        }

        // Работа со свуками
        private void LoadMedia(ref MediaPlayer media, string path)
        {
            media = new MediaPlayer();
            media.Open(new Uri(path, UriKind.Relative));
            media.MediaEnded += Media_Restart;
        }

        private void Media_Restart(object sender, EventArgs e)
        {
            MediaPlayer media = sender as MediaPlayer;
            media.Stop();
            media.Play();
        }

        public void ControlSounds()
        {
            BindSoundToState(ringtone, ModelStates.IncomingCall);
            BindSoundToState(dialtone, ModelStates.OutgoingCall);
        }

        private void BindSoundToState(MediaPlayer media, ModelStates state)
        {
            if (media == null)
            {
                return;
            }
            try
            {
                if (model.State == state)
                {
                    media.Dispatcher.Invoke(() => media.Play());
                }
                else
                {
                    media.Dispatcher.Invoke(() => media.Stop());
                }
            }
            catch { }
        }
    }
}
