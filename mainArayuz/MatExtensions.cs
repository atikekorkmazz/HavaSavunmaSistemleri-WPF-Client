using System.IO;
using OpenCvSharp;

public static class MatExtensions
{
    public static MemoryStream ToMemoryStream(this Mat mat, string ext = ".bmp")
    {
        byte[] imageBytes = mat.ToBytes(ext);
        return new MemoryStream(imageBytes);
    }
}
