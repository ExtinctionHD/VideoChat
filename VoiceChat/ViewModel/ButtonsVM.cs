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
    public class ButtonsVM: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        public ImageSource CameraButton
        {
            get => cameraButton;
            set
            {
                cameraButton = value;
                OnPropertyChanged("CameraButton");
            }
        }
        private ImageSource cameraButton;
        private readonly ImageSource[] cameraButtonStates = new ImageSource[2]; // 0 - Off, 1 - On

        public ImageSource MicrophoneButton
        {
            get => microphoneButton;
            set
            {
                microphoneButton = value;
                OnPropertyChanged("MicrophoneButton");
            }
        }
        private ImageSource microphoneButton;
        private readonly ImageSource[] microphoneButtonStates = new ImageSource[2]; // 0 - Off, 1 - On

        private readonly VoiceChatModel model;

        public ButtonsVM(VoiceChatModel model)
        {
            this.model = model;

            InitializeBitmaps(cameraButtonStates, new string[2] { "CameraOff.png", "CameraOn.png" });
            CameraButton = cameraButtonStates[1];

            InitializeBitmaps(microphoneButtonStates, new string[2] { "MicrophoneOff.png", "MicrophoneOn.png" });
            MicrophoneButton = microphoneButtonStates[0];

            model.video.PropertyChanged += VideoSending_PropertyChanged;
            model.audio.PropertyChanged += AudioSending_PropertyChanged;
        }

        private void VideoSending_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSending")
            {
                if ((sender as DataSharing).IsSending)
                {
                    CameraButton = cameraButtonStates[0];
                }
                else
                {
                    CameraButton = cameraButtonStates[1];
                }
            }
        }

        private void AudioSending_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSending")
            {
                if ((sender as DataSharing).IsSending)
                {
                    MicrophoneButton = microphoneButtonStates[0];
                }
                else
                {
                    MicrophoneButton = microphoneButtonStates[1];
                }
            }
        }

        private void InitializeBitmaps(ImageSource[] buttonStates, string[] uri)
        {
            const string PATH = "View/Images/Buttons/";

            for (int i = 0; i < buttonStates.Length; i++)
            {
                buttonStates[i] = LoadBitmapImage(PATH + uri[i]);
            }
        }

        public static BitmapImage LoadBitmapImage(string uri)
        {
            BitmapImage bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.UriSource = new Uri(Environment.CurrentDirectory + '\\' + uri, UriKind.Absolute);
            bitmap.EndInit();

            return bitmap;
        }
    }
}
