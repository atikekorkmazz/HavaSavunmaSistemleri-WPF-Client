using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

public class SharedFrameReader : IDisposable
{
    private readonly int width = 640;
    private readonly int height = 480;
    private readonly int channels = 4;
    private readonly string memoryName = "video_frame";
    private MemoryMappedFile mmf;
    private MemoryMappedViewAccessor accessor;

    public SharedFrameReader()
    {
        int frameSize = width * height * channels;
        mmf = MemoryMappedFile.OpenExisting(memoryName, MemoryMappedFileRights.Read);
        accessor = mmf.CreateViewAccessor(0, frameSize, MemoryMappedFileAccess.Read);
    }

    public Bitmap ReadFrame()
    {
        byte[] buffer = new byte[width * height * channels];
        accessor.ReadArray(0, buffer, 0, buffer.Length);

        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);
        Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
        bmp.UnlockBits(data);

        return bmp;
    }

    public void Dispose()
    {
        accessor?.Dispose();
        mmf?.Dispose();
    }
}
