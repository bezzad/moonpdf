using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MoonPdfLib.Helper
{
    internal static class BitmapExtensionMethods
    {
        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap bmp)
        {
            try
            {
                var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);

                var bitmapData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                var bufferSize = bitmapData.Stride * bmp.Height;

                var bitmapSrc = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height,
                    bmp.HorizontalResolution, bmp.VerticalResolution,
                    PixelFormats.Bgr32, null,
                    bitmapData.Scan0, bufferSize, bitmapData.Stride);

                bmp.UnlockBits(bitmapData);

                return bitmapSrc;
            }
            finally
            {
                // Collect all generations of memory.
                GC.Collect();
            }
        }
    }
}
