namespace MoonPdfLib.MuPdf
{
    public class MemorySource : IPdfSource
    {
        public byte[] Bytes { get; }

        public MemorySource(byte[] bytes)
        {
            Bytes = bytes;
        }
    }
}
