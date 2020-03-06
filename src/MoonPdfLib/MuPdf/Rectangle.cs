namespace MoonPdfLib.MuPdf
{
    internal struct Rectangle
    {
        public float Left, Top, Right, Bottom;

        public float Width => Right - Left;
        public float Height => Bottom - Top;
    }
}
