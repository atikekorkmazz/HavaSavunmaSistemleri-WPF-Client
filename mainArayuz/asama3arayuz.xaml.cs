using AForge.Video.DirectShow;
using AForge.Video;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
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
using System.Windows.Shapes;
using System.Net.Sockets;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using System.Threading;

namespace mainArayuz
{
    public partial class asama3arayuz : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private VideoClient client;


        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

        public asama3arayuz(string port)
        {
            InitializeComponent();

            _libVLC = new LibVLC("--avcodec-hw=dxva2"); // donanım hızlandırma

            var uri = $"udp://@0.0.0.0:{port}";
            var mediaOptions = new[]
{
                ":network-caching=50",          // daha az buffer
                ":live-caching=30",
                ":file-caching=30",
                ":udp-caching=30",
                ":clock-jitter=0",
                ":clock-synchro=0",
                ":drop-late-frames",            // geç gelen kareleri atar
                ":skip-frames",                 // düşük FPS olursa kare atla
                ":codec=avcodec"
            };
            /*var mediaOptions = new[]
            {
                ":network-caching=100",
                ":live-caching=100",
                ":file-caching=100",
                ":clock-jitter=0",
                ":clock-synchro=0"
            };*/

            var media = new Media(_libVLC, uri, FromType.FromLocation, mediaOptions);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.Play(media);

            client = new VideoClient();
            client.Connect();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Pencere_Kucult(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Tam_Ekran(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void Ekrani_Kapat(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        private async void angajman_kabul_Button_Click(object sender, RoutedEventArgs e)
        {
            await client.SendStageCommandAsync("angajman_kabul");
            durumLabel.Content = "Angajman kabul edildi";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            //_mediaPlayer?.Stop();
            //_mediaPlayer?.Dispose();
            //_libVLC?.Dispose();
        }

        public void SetStageInfo(string info)
        {
            asamaBilgisi.Content = info;
        }
    }
}