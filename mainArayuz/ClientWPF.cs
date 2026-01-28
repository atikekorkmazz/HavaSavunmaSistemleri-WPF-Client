using System;
using LibVLCSharp.Shared;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class VideoClient
{
    private string videoUri;

    private string dataHost;
    private string messageHost;

    private int dataPort;
    private int messagePort;

    private TcpClient dataClient;
    private TcpClient messageClient;

    private NetworkStream dataStream;
    private NetworkStream messageStream;

    public LibVLC _libVLC;
    public MediaPlayer _mediaPlayer;

    public VideoClient(
        string videoUri = "udp://@:1234",
        string dataHost = "10.180.255.30",
        string messageHost = "10.180.255.30",
        int dataPort = 9000,
        int messagePort = 7000)
    {
        this.videoUri = videoUri;
        this.dataHost = dataHost;
        this.messageHost = messageHost;
        this.dataPort = dataPort;
        this.messagePort = messagePort;

        dataClient = new TcpClient();
        messageClient = new TcpClient();

        Core.Initialize();
        _libVLC = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);
    }

    public void Connect()
    {
        try
        {
            var mediaOptions = new[] {
                ":network-caching=150",  // 150 ms buffer
                ":clock-jitter=0",
                ":clock-synchro=0",
                ":live-caching=150"
            };

            var media = new Media(_libVLC, videoUri, FromType.FromLocation, mediaOptions);
            _mediaPlayer.Play(media);

            Console.WriteLine($"Ek veri için bağlanıyor: {dataHost}:{dataPort}");
            dataClient.Connect(dataHost, dataPort);
            dataStream = dataClient.GetStream();
            Console.WriteLine("Ek veri bağlantısı başarılı.");

            Console.WriteLine($"Mesajlar için bağlanıyor: {messageHost}:{messagePort}");
            messageClient.Connect(messageHost, messagePort);
            messageStream = messageClient.GetStream();
            Console.WriteLine("Mesaj bağlantısı başarılı.");

            if (dataStream == null || messageStream == null)
                throw new Exception("Bağlantı eksik veya başarısız.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Bağlantı hatası: {e.Message}");
            throw new Exception("Sunucuya bağlanılamadı. Lütfen sunucuyu kontrol edin.");
        }
    }

    public string ReceiveData()
    {
        try
        {
            byte[] sizeBuffer = new byte[4];
            int bytesRead = dataStream.Read(sizeBuffer, 0, sizeBuffer.Length);
            if (bytesRead < 4) throw new Exception("Ek veri boyutu alınamadı.");

            int dataSize = BitConverter.ToInt32(sizeBuffer, 0);
            byte[] data = ReadExactBytes(dataStream, dataSize);

            return Encoding.UTF8.GetString(data);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ek veri alınırken hata: {e.Message}");
            return null;
        }
    }

    public void SendData(string message)
    {
        try
        {
            byte[] encodedMessage = Encoding.UTF8.GetBytes(message);
            byte[] sizePrefix = BitConverter.GetBytes(encodedMessage.Length);

            dataStream.Write(sizePrefix, 0, sizePrefix.Length);
            dataStream.Write(encodedMessage, 0, encodedMessage.Length);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Veri gönderimi sırasında hata: {e.Message}");
        }
    }

    /*public string ReceiveMessage()
    {
        try
        {
            byte[] sizeBuffer = new byte[4];
            int bytesRead = messageStream.Read(sizeBuffer, 0, sizeBuffer.Length);
            if (bytesRead < 4) throw new Exception("Mesaj veri boyutu alınamadı.");

            int dataSize = BitConverter.ToInt32(sizeBuffer, 0);
            byte[] data = ReadExactBytes(messageStream, dataSize);

            return Encoding.UTF8.GetString(data);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Mesaj verisi alınırken hata: {e.Message}");
            return null;
        }
    }*/

    public string ReceiveButtonMessage()
    {
        try
        {
            byte[] sizeBuffer = new byte[4];
            int bytesRead = messageStream.Read(sizeBuffer, 0, sizeBuffer.Length);
            if (bytesRead < 4)
                throw new Exception("Sunucudan mesaj boyutu alınamadı.");

            int dataSize = BitConverter.ToInt32(sizeBuffer, 0);
            byte[] data = ReadExactBytes(messageStream, dataSize);

            string message = Encoding.UTF8.GetString(data);
            Console.WriteLine($"[ReceiveButtonMessage] Sunucudan gelen: {message}");
            return message;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ReceiveButtonMessage] Hata: {e.Message}");
            return null;
        }
    }

    public void SendMessage(string message)
    {
        try
        {
            byte[] encodedMessage = Encoding.UTF8.GetBytes(message);
            byte[] sizePrefix = BitConverter.GetBytes(encodedMessage.Length);

            messageStream.Write(sizePrefix, 0, sizePrefix.Length);
            messageStream.Write(encodedMessage, 0, encodedMessage.Length);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Mesaj gönderimi sırasında hata: {e.Message}");
        }
    }

    public void Close()
    {
        try
        {
            dataStream?.Close();
            messageStream?.Close();
            dataClient?.Close();
            messageClient?.Close();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            Console.WriteLine("Bağlantılar kapatıldı.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Soket kapatma hatası: {e.Message}");
        }
    }

    private byte[] ReadExactBytes(NetworkStream stream, int size)
    {
        byte[] buffer = new byte[size];
        int totalRead = 0;

        while (totalRead < size)
        {
            int read = stream.Read(buffer, totalRead, size - totalRead);
            if (read == 0)
                throw new IOException("Bağlantı kesildi veya veri alınamadı.");
            totalRead += read;
        }
        return buffer;
    }

    public async Task SendStageCommandAsync(string message)
    {
        try
        {
            Console.WriteLine($"[SendStageCommandAsync] başladı: {message}");

            if (messageStream == null)
            {
                Console.WriteLine("[SendStageCommandAsync] messageStream NULL!");
                return;
            }

            if (messageStream.CanWrite)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] sizeBytes = BitConverter.GetBytes(messageBytes.Length);

                await messageStream.WriteAsync(sizeBytes, 0, 4);
                await messageStream.WriteAsync(messageBytes, 0, messageBytes.Length);

                byte[] responseSizeBytes = new byte[4];
                int readSize = await messageStream.ReadAsync(responseSizeBytes, 0, 4);
                if (readSize < 4) return;

                int responseSize = BitConverter.ToInt32(responseSizeBytes, 0);
                byte[] responseBytes = new byte[responseSize];

                int totalRead = 0;
                while (totalRead < responseSize)
                {
                    int read = await messageStream.ReadAsync(responseBytes, totalRead, responseSize - totalRead);
                    if (read == 0) break;
                    totalRead += read;
                }

                string response = Encoding.UTF8.GetString(responseBytes);
                Console.WriteLine("[SendStageCommandAsync] Sunucu cevabı: " + response);
            }
            else
            {
                Console.WriteLine("[SendStageCommandAsync] messageStream yazılabilir değil.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SendStageCommandAsync] HATA: " + ex.Message);
        }
    }
    public NetworkStream GetDataStream() => dataStream;
    public NetworkStream GetMessageStream() => messageStream;
}
