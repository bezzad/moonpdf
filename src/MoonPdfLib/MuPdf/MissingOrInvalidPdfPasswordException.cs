using System;

namespace MoonPdfLib.MuPdf
{
    public class MissingOrInvalidPdfPasswordException : Exception
    {
        public MissingOrInvalidPdfPasswordException()
            : base("A password for the pdf document was either not provided or is invalid.")
        { }
    }
}
