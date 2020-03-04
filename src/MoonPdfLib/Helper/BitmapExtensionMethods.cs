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
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MoonPdfLib.Helper
{
    internal static class BitmapExtensionMethods
    {
        // [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        // public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap bmp)
        {
            var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            int bufferSize = bmpData.Stride * bmp.Height;
            var bms = new System.Windows.Media.Imaging.WriteableBitmap(bmp.Width, bmp.Height, bmp.HorizontalResolution, bmp.VerticalResolution, PixelFormats.Bgr32, null);
            bms.WritePixels(new Int32Rect(0, 0, bmp.Width, bmp.Height), bmpData.Scan0, bufferSize, bmpData.Stride);
            bmp.UnlockBits(bmpData);
            
            return bms;



            // var bitmapData = bmp.LockBits(
            //     new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
            //     System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            //
            // var bitmapSource = BitmapSource.Create(
            //     bitmapData.Width, bitmapData.Height,
            //     bmp.HorizontalResolution, bmp.VerticalResolution,
            //     PixelFormats.Bgr24, null,
            //     bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            //
            // bmp.UnlockBits(bitmapData);
            // return bitmapSource;


            // var bms = new WriteableBitmap(bmp.Width, bmp.Height, 96.0, 96.0, PixelFormats.Bgr24, null);
            // var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            // bms.Lock();
            // CopyMemory(bms.BackBuffer, data.Scan0, (uint)(bms.BackBufferStride * bmp.Height));
            // bms.AddDirtyRect(new Int32Rect(0, 0, bmp.Width, bmp.Height));
            // bms.Unlock();
            // bmp.UnlockBits(data);
            // return bms;
        }
    }
}
