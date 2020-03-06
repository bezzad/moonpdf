namespace MoonPdfLib.MuPdf
{
    public class FileSource : IPdfSource
    {
        public string Filename { get; }

        public FileSource(string filename)
        {
            Filename = filename;
        }
    }
}
