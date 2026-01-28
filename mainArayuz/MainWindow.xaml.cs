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
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Threading;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

namespace mainArayuz
{
    public partial class MainWindow : Window
    {
        private asama1arayuz asama1Arayuz;
        private asama2arayuz asama2Arayuz;
        private asama3arayuz asama3Arayuz;

        public event Action<BitmapImage> OnFrameCaptured;

        private VideoClient client;
        private NetworkStream messageStream;
        private CancellationTokenSource cancellationTokenSource;

        private bool isUpdatingUI = false;

        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

        public MainWindow()
        {   
            InitializeComponent();
            ToggleButtons(false);

            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            cancellationTokenSource?.Cancel();
            client?.Close();
            base.OnClosed(e);
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

        private void ToggleButtons(bool isEnabled)
        {
            Asama1_Button.IsEnabled = isEnabled;
            Asama2_Button.IsEnabled = isEnabled;
            Asama3_Button.IsEnabled = isEnabled;
        }

        private async void Asama1_Button_Click(object sender, RoutedEventArgs e)
        {
            await client.SendStageCommandAsync("asama1");
            StartMainVLC("8001");

            if (asama1Arayuz == null || !asama1Arayuz.IsVisible)
            {
                asama1Arayuz = new asama1arayuz("8001");
                asama1Arayuz.Show();
                //OnFrameCaptured += asama1Arayuz.SetFrame;             

                asama1Arayuz.Closed += (s, args) =>
                {
                    //OnFrameCaptured -= asama1Arayuz.SetFrame;
                    asama1Arayuz = null;
                    ToggleButtons(true);
                    cancellationTokenSource = new CancellationTokenSource();
                    Task.Run(() => ReceiveLoopAsync(cancellationTokenSource.Token));

                    StartMainVLC("1234");
                };
                
                ToggleButtons(false);
                StopMainVLC();
                asama1Arayuz.SetStageInfo("  Aşama 1 Aktif");
            }
            else
            {
                asama1Arayuz.Focus();
            }
        }
        
        private async void Asama2_Button_Click(object sender, RoutedEventArgs e)
        {
            await client.SendStageCommandAsync("asama2");
            StartMainVLC("8002");
              
            //cancellationTokenSource?.Cancel();              //bunu ekleyince aşama2 ye görüntü hiç gitmiyor
            //cameraImage.Source = null;

            if (asama2Arayuz == null || !asama2Arayuz.IsVisible)
            {
                asama2Arayuz = new asama2arayuz("8002");
                //OnFrameCaptured += asama2Arayuz.SetFrame;

                asama2Arayuz.Closed += (s, args) =>
                {
                    //OnFrameCaptured -= asama2Arayuz.SetFrame;
                    asama2Arayuz = null;
                    ToggleButtons(true);
                    cancellationTokenSource = new CancellationTokenSource();
                    Task.Run(() => ReceiveLoopAsync(cancellationTokenSource.Token));

                    StartMainVLC("1234");
                };

                ToggleButtons(false);
                StopMainVLC();
                asama2Arayuz.Show();
                asama2Arayuz.SetStageInfo("  Aşama 2 Aktif");
            }
            else
            {
                asama2Arayuz.Focus();
            }
        }

        private async void Asama3_Button_Click(object sender, RoutedEventArgs e)
        {
            await client.SendStageCommandAsync("asama3");
            StartMainVLC("8003");

            //cameraImage.Source = null;

            if (asama3Arayuz == null || !asama3Arayuz.IsVisible)
            {
                asama3Arayuz = new asama3arayuz("8003");
                //OnFrameCaptured += asama3Arayuz.SetFrame;

                asama3Arayuz.Closed += (s, args) =>
                {
                    //OnFrameCaptured -= asama3Arayuz.SetFrame;
                    asama3Arayuz = null;
                    ToggleButtons(true);
                    cancellationTokenSource = new CancellationTokenSource();
                    Task.Run(() => ReceiveLoopAsync(cancellationTokenSource.Token));

                    StartMainVLC("1234");
                };

                ToggleButtons(false);
                StopMainVLC();
                asama3Arayuz.Show();
                asama3Arayuz.SetStageInfo("  Aşama 3 Aktif");
            }
            else
            {
                asama3Arayuz.Focus();
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new VideoClient();
                client.Connect();
                StartMainVLC("1234");

                messageStream = client.GetMessageStream();
                Console.WriteLine("MainWindow: messageStream alındı.");

                if (client == null)
                    throw new Exception("VideoClient oluşturulamadı.");

                ToggleButtons(true);

                cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => ReceiveLoopAsync(cancellationTokenSource.Token));

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı kurulamadı: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    //Bitmap frame = await Task.Run(() => client.ReceiveFrame());

                    /*if (frame != null && !isUpdatingUI)
                    {
                        isUpdatingUI = true;

                        BitmapImage imageSourceForUI = await Task.Run(() => ConvertBitmapToImageSource(frame));
                        BitmapImage imageSourceForChildren = imageSourceForUI; // aynı frame kullanılıyor

                        if (imageSourceForUI != null)
                        {
                            await Dispatcher.BeginInvoke(new Action(() =>
                            {
                                cameraImage.Source = imageSourceForUI;
                                OnFrameCaptured?.Invoke(imageSourceForChildren);
                            }));
                        }
                        isUpdatingUI = false;
                    }*/
                    // Ek veri oku
                    try
                    {
                        var additionalData = client.ReceiveData();
                        if (!string.IsNullOrEmpty(additionalData))
                        {
                            await Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AdditionalDataText.Text = additionalData;
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ek veri okunurken hata: " + ex.Message);
                    }
                    //await Task.Delay(1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Genel ReceiveLoop hatası: " + ex.Message);
                    //await Task.Delay(1);
                }
            }
        }

        public static BitmapImage ConvertBitmapToImageSource(Bitmap bitmap)
        {
            if (bitmap == null)
                return null;

            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    using (Bitmap tempBitmap = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    {
                        using (Graphics g = Graphics.FromImage(tempBitmap))
                        {
                            g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                        }

                        tempBitmap.Save(memory, ImageFormat.Bmp);
                    }

                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // WPF thread-safety
                    return bitmapImage;
                }
            }           
            catch (Exception ex)
            {
                Console.WriteLine("Görüntü dönüştürme hatası: " + ex.Message);
                return null;
            }
        }

        private void StartMainVLC(string port)
        {
            StopMainVLC(); // Önce eski VLC'yi durdur

            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;

            var uri = $"udp://@0.0.0.0:{port}";
            var mediaOptions = new[] {
                ":network-caching=150",
                ":clock-jitter=0",
                ":clock-synchro=0",
                ":live-caching=100",
                ":file-caching=100"
            };
            var media = new Media(_libVLC, uri, FromType.FromLocation, mediaOptions);
            _mediaPlayer.Play(media);

            Console.WriteLine("Main VLC başlatıldı: " + uri);
        }

        private void StopMainVLC()
        {
            if (_mediaPlayer == null) return;

            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();

            _mediaPlayer = null;
            _libVLC = null;

            Console.WriteLine("Main VLC durduruldu");
        }
    }
}