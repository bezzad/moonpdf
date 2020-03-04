/*! MoonPdfLib - Provides a WPF user control to display PDF files
Copyright (C) 2013  (see AUTHORS file)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
!*/

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
                var bitmapData = bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
                var bitmapSrc = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height,
                    bmp.HorizontalResolution, bmp.VerticalResolution,
                    PixelFormats.Bgr24, null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                bmp.UnlockBits(bitmapData);

                // var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
                // var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                // var bufferSize = bmpData.Stride * bmp.Height;
                // var bitmapSrc = new System.Windows.Media.Imaging.WriteableBitmap(bmp.Width, bmp.Height, bmp.HorizontalResolution, bmp.VerticalResolution, PixelFormats.Bgr32, null);
                // bitmapSrc.WritePixels(new Int32Rect(0, 0, bmp.Width, bmp.Height), bmpData.Scan0, bufferSize, bmpData.Stride);
                // bmp.UnlockBits(bmpData);

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
