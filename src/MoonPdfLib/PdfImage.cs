using System;
using System.Windows;
using System.Windows.Media;

namespace MoonPdfLib
{
    internal class PdfImage : IDisposable
    {
        public ImageSource ImageSource { get; set; }

        // we use only the "Right"-property of "Thickness", but we choose the "Thickness"
        // structure instead of a simple double, because it makes data binding easier.
        public Thickness Margin { get; set; }

        ~PdfImage()
        {
            Dispose();
        }

        public void Dispose()
        {
            ImageSource = null;
            GC.SuppressFinalize(this);
            GC.Collect();
        }
    }
}
