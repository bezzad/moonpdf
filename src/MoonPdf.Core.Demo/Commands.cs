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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace MoonPdf
{
	public class Commands
	{
		public DelegateCommand OpenCommand { get; }
		public DelegateCommand ExitCommand { get; }

		public DelegateCommand RotateRightCommand { get; }
		public DelegateCommand RotateLeftCommand { get; }

		public DelegateCommand NextPageCommand { get; }
		public DelegateCommand PreviousPageCommand { get; }
		public DelegateCommand FirstPageCommand { get; }
		public DelegateCommand LastPageCommand { get; }
		
		public DelegateCommand SinglePageCommand { get; }
		public DelegateCommand FacingCommand { get; }
		public DelegateCommand BookViewCommand { get; }

		public DelegateCommand TogglePageDisplayCommand { get; }

		public FullscreenCommand FullscreenCommand { get; }

		public DelegateCommand ZoomInCommand { get; }
		public DelegateCommand ZoomOutCommand { get; }
		public DelegateCommand FitWidthCommand { get; }
		public DelegateCommand FitHeightCommand { get; }
		public DelegateCommand CustomZoomCommand { get; }

		public DelegateCommand ShowAboutCommand { get; }
		public DelegateCommand GotoPageCommand { get; }

		public Commands(MainWindow wnd)
		{
			var pdfPanel = wnd.MoonPdfPanel;
			Predicate<object> isPdfLoaded = f => wnd.IsPdfLoaded(); // used for the CanExecute callback

			OpenCommand = new DelegateCommand("Open...", f =>
				{
					var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Select PDF file...", DefaultExt = ".pdf", Filter = "PDF file (.pdf)|*.pdf",CheckFileExists = true };

                    if (dlg.ShowDialog() == true)
                    {
                        try
                        {
                            pdfPanel.OpenFile(dlg.FileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(string.Format("An error occured: " + ex.Message));
                        }
                    }
				}, f => true, new KeyGesture(Key.O, ModifierKeys.Control));

			ExitCommand = new DelegateCommand("Exit", f => wnd.Close(), f => true, new KeyGesture(Key.Q, ModifierKeys.Control));

			PreviousPageCommand = new DelegateCommand("Previous page", f => pdfPanel.GotoPreviousPage(), isPdfLoaded, new KeyGesture(Key.Left));
			NextPageCommand = new DelegateCommand("Next page", f => pdfPanel.GotoNextPage(), isPdfLoaded, new KeyGesture(Key.Right));
			FirstPageCommand = new DelegateCommand("First page", f => pdfPanel.GotoFirstPage(), isPdfLoaded, new KeyGesture(Key.Home));
			LastPageCommand = new DelegateCommand("Last page", f => pdfPanel.GotoLastPage(), isPdfLoaded, new KeyGesture(Key.End));
			GotoPageCommand = new DelegateCommand("Goto page...", f =>
			{
				var dlg = new GotoPageDialog(pdfPanel.GetCurrentPageNumber(), pdfPanel.TotalPages);

				if (dlg.ShowDialog() == true)
					pdfPanel.GotoPage(dlg.SelectedPageNumber.Value);
			}, isPdfLoaded, new KeyGesture(Key.G, ModifierKeys.Control));

			RotateRightCommand = new DelegateCommand("Rotate right", f => pdfPanel.RotateRight(), isPdfLoaded, new KeyGesture(Key.Add, ModifierKeys.Control | ModifierKeys.Shift));
			RotateLeftCommand = new DelegateCommand("Rotate left", f => pdfPanel.RotateLeft(), isPdfLoaded, new KeyGesture(Key.Subtract, ModifierKeys.Control | ModifierKeys.Shift));

			ZoomInCommand = new DelegateCommand("Zoom in", f => pdfPanel.ZoomIn(), isPdfLoaded, new KeyGesture(Key.Add));
			ZoomOutCommand = new DelegateCommand("Zoom out", f => pdfPanel.ZoomOut(), isPdfLoaded, new KeyGesture(Key.Subtract));

			FitWidthCommand = new DelegateCommand("Fit width", f => pdfPanel.ZoomToWidth(), isPdfLoaded, new KeyGesture(Key.D4, ModifierKeys.Control));
			FitHeightCommand = new DelegateCommand("Fit height", f => pdfPanel.ZoomToHeight(), isPdfLoaded, new KeyGesture(Key.D5, ModifierKeys.Control));
			CustomZoomCommand = new DelegateCommand("Custom zoom", f => pdfPanel.SetFixedZoom(), isPdfLoaded, new KeyGesture(Key.D6, ModifierKeys.Control));

			TogglePageDisplayCommand = new DelegateCommand("Show pages continuously", f => pdfPanel.TogglePageDisplay(), isPdfLoaded, new KeyGesture(Key.D7, ModifierKeys.Control));

			FullscreenCommand = new FullscreenCommand("Full screen", wnd, new KeyGesture(Key.L, ModifierKeys.Control));

			SinglePageCommand = new DelegateCommand("Single page", f => { pdfPanel.ViewType = MoonPdfLib.ViewType.SinglePage; }, isPdfLoaded, new KeyGesture(Key.D1, ModifierKeys.Control));
			FacingCommand = new DelegateCommand("Facing", f => { pdfPanel.ViewType = MoonPdfLib.ViewType.Facing; }, isPdfLoaded, new KeyGesture(Key.D2, ModifierKeys.Control));
			BookViewCommand = new DelegateCommand("Book view", f => { pdfPanel.ViewType = MoonPdfLib.ViewType.BookView; }, isPdfLoaded, new KeyGesture(Key.D3, ModifierKeys.Control));

			ShowAboutCommand = new DelegateCommand("About", f => new AboutWindow().ShowDialog(), f => true, null);

			RegisterInputBindings(wnd);
		}

		private void RegisterInputBindings(MainWindow wnd)
		{
			// register inputbindings for all commands (properties)
			foreach (var pi in typeof(Commands).GetProperties())
			{
				var cmd = (BaseCommand)pi.GetValue(this, null);

				if (cmd.InputBinding != null)
					wnd.InputBindings.Add(cmd.InputBinding);
			}
		}
	}
}
