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
using MoonPdfLib;
using MoonPdfLib.MuPdf;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace MoonPdf
{
	public partial class MainWindow : Window
	{
		private static string appName;
		private MainWindowDataContext dataContext;

		internal MoonPdfPanel MoonPdfPanel => moonPdfPanel;

		public MainWindow()
		{
			InitializeComponent();

			dataContext = new MainWindowDataContext(this);
			Icon = MoonPdf.Properties.Resources.moon.ToBitmapSource();
			DataContext = dataContext;
			UpdateTitle();

			moonPdfPanel.ViewTypeChanged += moonPdfPanel_ViewTypeChanged;
			moonPdfPanel.ZoomTypeChanged += moonPdfPanel_ZoomTypeChanged;
			moonPdfPanel.PageRowDisplayChanged += moonPdfPanel_PageDisplayChanged;
			moonPdfPanel.PdfLoaded += moonPdfPanel_PdfLoaded;
			moonPdfPanel.PasswordRequired += moonPdfPanel_PasswordRequired;

			UpdatePageDisplayMenuItem();
			UpdateZoomMenuItems();
			UpdateViewTypeItems();

			Loaded += MainWindow_Loaded;
		}

		void moonPdfPanel_PasswordRequired(object sender, PasswordRequiredEventArgs e)
		{
			var dlg = new PdfPasswordDialog();

			if (dlg.ShowDialog() == true)
				e.Password = dlg.Password;
			else
				e.Cancel = true;
		}

		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			var args = Environment.GetCommandLineArgs();

			// if a filename was given via command line
			if (args.Length > 1 && File.Exists(args[1]))
			{
				try
				{
					moonPdfPanel.OpenFile(args[1]);
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("An error occured: " + ex.Message));
				}
			}
		}

		void moonPdfPanel_PageDisplayChanged(object sender, EventArgs e)
		{
			UpdatePageDisplayMenuItem();
		}

		private void UpdatePageDisplayMenuItem()
		{
			itmContinuously.IsChecked = (moonPdfPanel.PageRowDisplay == PageRowDisplayType.ContinuousPageRows);
		}

		void moonPdfPanel_ZoomTypeChanged(object sender, EventArgs e)
		{
			UpdateZoomMenuItems();
		}

		private void UpdateZoomMenuItems()
		{
			itmFitHeight.IsChecked = moonPdfPanel.ZoomType == ZoomType.FitToHeight;
			itmFitWidth.IsChecked = moonPdfPanel.ZoomType == ZoomType.FitToWidth;
			itmCustomZoom.IsChecked = moonPdfPanel.ZoomType == ZoomType.Fixed;
		}

		void moonPdfPanel_ViewTypeChanged(object sender, EventArgs e)
		{
			UpdateViewTypeItems();
		}

		private void UpdateViewTypeItems()
		{
			switch (moonPdfPanel.ViewType)
			{
				case ViewType.SinglePage:
					viewSingle.IsChecked = true;
					break;
				case ViewType.Facing:
					viewFacing.IsChecked = true;
					break;
				case ViewType.BookView:
					viewBook.IsChecked = true;
					break;
			}
		}

		void moonPdfPanel_PdfLoaded(object sender, EventArgs e)
		{
			UpdateTitle();
		}

		private void UpdateTitle()
		{
			if (appName == null)
				appName = ((AssemblyProductAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), true).First()).Product;

			if (IsPdfLoaded())
			{
                if (moonPdfPanel.CurrentSource is FileSource fs)
				{
					Title = $"{Path.GetFileName(fs.Filename)} - {appName}";
					return;
				}
			}

			Title = appName;
		}

		internal bool IsPdfLoaded()
		{
			return moonPdfPanel.CurrentSource != null;
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			if (e.SystemKey == Key.LeftAlt)
			{
				mainMenu.Visibility = (mainMenu.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed);

				if (mainMenu.Visibility == Visibility.Collapsed)
					e.Handled = true;
			}
		}

		internal void OnFullscreenChanged(bool isFullscreen)
		{
			itmFullscreen.IsChecked = isFullscreen;
		}
	}
}
