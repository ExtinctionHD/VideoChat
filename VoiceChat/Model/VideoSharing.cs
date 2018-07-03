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
using System.Drawing.Imaging;

namespace VoiceChat.Model
{
    public class VideoSharing : DataSharing
    {
        private VideoCaptureDevice videoDevice;

        public bool IsEnable
        {
            get => videoDevice != null;
        }

        private ImageSource remoteFrame;
        public ImageSource RemoteFrame
        {
            get => remoteFrame;
            set
            {
                if (IsComes)
                {
                    remoteFrame = value;
                }
                else
                {
                    remoteFrame = null;
                }
                OnPropertyChanged("RemoteFrame");
            }
        }

        private ImageSource localFrame;
        public ImageSource LocalFrame
        {
            get => localFrame;
            set
            {
                if (IsSending)
                {
                    localFrame = value;
                }
                else
                {
                    localFrame = null;
                }
                OnPropertyChanged("LocalFrame");
            }
        }

        public VideoSharing(VoiceChatModel model) : base(model)
        {
            LineIndex = 1;

            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count != 0)
            {
                videoDevice = new VideoCaptureDevice(videoDevices[0].MonikerString);
            }
        }
        
        public void ReceiveFlags(byte[] buffer)
        {
            if (VoiceChatModel.IsFlag(Flags.BeginVideoSend, buffer))
                IsComes = true;
            
            if (VoiceChatModel.IsFlag(Flags.EndVideoSend, buffer))
                IsComes = false;
        }

        public override void BeginSend()
        {
            base.BeginSend();

            if (videoDevice == null)
                return;

            videoDevice.NewFrame += Send;
            videoDevice.Start();

            BdtpClient.SendReceipt(new byte[] { (byte)Flags.BeginVideoSend });
        }

        public override void BeginReceive()
        {
            base.BeginReceive();
        }

        public override void EndSend()
        {
            base.EndSend();

            if (videoDevice == null)
                return;

            videoDevice.NewFrame -= Send;
            videoDevice.SignalToStop();

            BdtpClient.SendReceipt(new byte[] { (byte)Flags.EndVideoSend });

            LocalFrame = null;
        }

        public override void EndReceive()
        {
            base.EndReceive();
            IsComes = false;
        }

        public void ClearFrames()
        {
            RemoteFrame = null;
            LocalFrame = null;
        }

        protected override void Send(object sender, EventArgs e)
        {
            Bitmap bitmap = (e as NewFrameEventArgs).Frame;

            Application.Current.Dispatcher.Invoke(() =>
            {
                LocalFrame = BitmapToImageSource(bitmap);
            });

            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Jpeg);
                BdtpClient.Send(stream.ToArray(), LineIndex);
            }

        }

        protected override void Receive()
        {
            byte[] data = BdtpClient.Receive(LineIndex);
            
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    using (MemoryStream stream = new MemoryStream(data))
                    {
                        using (Bitmap bitmap = new Bitmap(stream))
                        {
                            RemoteFrame = BitmapToImageSource(bitmap);
                        }
                    }
                });
            }
            catch { }
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        private ImageSource BitmapToImageSource(Bitmap bitmap)
        {
            IntPtr handle = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }
}
