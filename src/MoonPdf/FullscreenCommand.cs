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
using System.Windows;
using System.Windows.Input;

namespace MoonPdf
{
	public class FullscreenCommand : BaseCommand
	{
		private MainWindow wnd;
		private FullscreenHandler fullscreenHandler;

		public FullscreenCommand(string name, MainWindow wnd, InputGesture inputGesture)
			: base(name, inputGesture)
		{
			this.wnd = wnd;
			this.wnd.PreviewKeyDown += wnd_PreviewKeyDown;
			fullscreenHandler = new FullscreenHandler(wnd);
			fullscreenHandler.FullscreenChanged += fullscreenHandler_FullscreenChanged;
		}

		void fullscreenHandler_FullscreenChanged(object sender, EventArgs e)
		{
			wnd.OnFullscreenChanged(fullscreenHandler.IsFullscreen);
		}

		void wnd_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				fullscreenHandler.QuitFullscreen();
		}

		public override bool CanExecute(object parameter)
		{
			return wnd.IsPdfLoaded();
		}

		public override void Execute(object parameter)
		{
			if (fullscreenHandler.IsFullscreen)
				fullscreenHandler.QuitFullscreen();
			else
				fullscreenHandler.StartFullscreen();
		}

		private class FullscreenHandler
		{
			public event EventHandler FullscreenChanged;

			private MainWindow window;
			private WindowState oldWindowState;
			private Visibility oldMenuVisibility;
			private bool isFullscreen;

			public FullscreenHandler(MainWindow window)
			{
				this.window = window;
				oldWindowState = window.WindowState;
				oldMenuVisibility = window.mainMenu.Visibility;
			}

			public bool IsFullscreen
			{
				get => isFullscreen;
				private set
				{
					if (value != isFullscreen)
					{
						isFullscreen = value;
						FullscreenChanged?.Invoke(this, EventArgs.Empty);
					}
				}
			}

			public void StartFullscreen()
			{
				if (IsFullscreen)
					return;

				oldWindowState = window.WindowState;
				oldMenuVisibility = window.mainMenu.Visibility;

				if (window.mainMenu.Visibility == Visibility.Visible)
					window.mainMenu.Visibility = Visibility.Collapsed;

				window.ResizeMode = ResizeMode.NoResize;
				window.WindowStyle = WindowStyle.None;
				window.WindowState = WindowState.Maximized;
				IsFullscreen = true;
			}

			public void QuitFullscreen()
			{
				if (!IsFullscreen)
					return;

				window.ResizeMode = ResizeMode.CanResize;
				window.WindowStyle = WindowStyle.SingleBorderWindow;
				window.WindowState = oldWindowState;
				window.mainMenu.Visibility = oldMenuVisibility;
				IsFullscreen = false;
			}
		}
	}
}
