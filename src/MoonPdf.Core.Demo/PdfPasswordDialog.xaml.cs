/*! MoonPdf - A WPF-based PDF Viewer application that uses the MoonPdfLib library
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
using System.Windows;

namespace MoonPdf
{
    public partial class PdfPasswordDialog : Window
    {
        public string Password { get; private set; }

        public PdfPasswordDialog()
        {
            InitializeComponent();
            Loaded += PdfPasswordDialog_Loaded;
        }

        void PdfPasswordDialog_Loaded(object sender, RoutedEventArgs e)
        {
            txtPassword.Focus();
            txtPassword.SelectAll();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Password = txtPassword.Password;
            DialogResult = true;
            Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
