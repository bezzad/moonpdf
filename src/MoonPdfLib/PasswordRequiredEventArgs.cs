using System;

namespace MoonPdfLib
{
    public class PasswordRequiredEventArgs : EventArgs
    {
        public string Password { get; set; }
        public bool Cancel { get; set; }
    }
}
