/*! MoonPdfLib - Provides a WPF user control to display PDF files
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace MoonPdfLib
{
	internal class MoonPdfPanelInputHandler
	{
		private MouseHookListener mouseHookListener;
		private MoonPdfPanel source;
		private Point? lastMouseDownLocation;
		private double lastMouseDownVerticalOffset;
		private double lastMouseDownHorizontalOffset;

		public MoonPdfPanelInputHandler(MoonPdfPanel source)
		{
			this.source = source;

			this.source.PreviewKeyDown += source_PreviewKeyDown;
			this.source.PreviewMouseWheel += source_PreviewMouseWheel;
			this.source.PreviewMouseLeftButtonDown += source_PreviewMouseLeftButtonDown;

			mouseHookListener = new MouseHookListener(new GlobalHooker());
			mouseHookListener.Enabled = false;
			mouseHookListener.MouseMove += mouseHookListener_MouseMove;
			mouseHookListener.MouseUp += mouseHookListener_MouseUp;
		}

		void source_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			/* maybe for future use
			if (e.OriginalSource is Image)
			{
				var pos = e.GetPosition((Image)e.OriginalSource);
				MessageBox.Show(pos.X + " x " + pos.Y);
			}
			*/

            if( IsScrollBarChild(e.OriginalSource as DependencyObject) ) // if the mouse click comes from the scrollbar, then we do not scroll
				lastMouseDownLocation = null;
			else
			{
				if (source.ScrollViewer != null)
				{
					mouseHookListener.Enabled = true;

					lastMouseDownVerticalOffset = source.ScrollViewer.VerticalOffset;
					lastMouseDownHorizontalOffset = source.ScrollViewer.HorizontalOffset;
					lastMouseDownLocation = source.PointToScreen(e.GetPosition(source));
				}
			}
		}

        private static bool IsScrollBarChild(DependencyObject o)
        {
            var parent = o;

            while(parent != null)
            {
                if (parent is ScrollBar)
                    return true;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return false;
        }

		void source_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			var ctrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

			if (ctrlDown || e.RightButton == MouseButtonState.Pressed)
			{
				if (e.Delta > 0)
					source.ZoomIn();
				else
					source.ZoomOut();

				e.Handled = true;
			}
			else if (!ctrlDown && (source.ScrollViewer == null || source.ScrollViewer.ComputedVerticalScrollBarVisibility != Visibility.Visible) && source.PageRowDisplay == PageRowDisplayType.SinglePageRow)
			{
				if (e.Delta > 0)
					source.GotoPreviousPage();
				else
					source.GotoNextPage();

				e.Handled = true;
			}
			else if (source.ScrollViewer != null && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
			{
				if (e.Delta > 0)
					source.ScrollViewer.LineLeft();
				else
					source.ScrollViewer.LineRight();

				e.Handled = true;
			}
		}

		void source_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Home)
				source.GotoPage(1);
			else if (e.Key == Key.End)
				source.GotoLastPage();
			else if (e.Key == Key.Add || e.Key == Key.OemPlus)
				source.ZoomIn();
			else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
				source.ZoomOut();

			if (source.ScrollViewer != null && source.ScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
				return;

			if (e.Key == Key.Left)
				source.GotoPreviousPage();
			else if (e.Key == Key.Right)
				source.GotoNextPage();
		}

		void mouseHookListener_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			mouseHookListener.Enabled = false;
			lastMouseDownLocation = null;
		}

		void mouseHookListener_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (lastMouseDownLocation != null)
			{
				var currentPos = e.Location;
                var proposedYOffset = lastMouseDownVerticalOffset + lastMouseDownLocation.Value.Y - currentPos.Y;
                var proposedXOffset = lastMouseDownHorizontalOffset + lastMouseDownLocation.Value.X - currentPos.X;

                if (proposedYOffset <= 0 || proposedYOffset > source.ScrollViewer.ScrollableHeight)
                {
                    lastMouseDownVerticalOffset = proposedYOffset <= 0 ? 0 : source.ScrollViewer.ScrollableHeight;
                    lastMouseDownLocation = new Point(lastMouseDownLocation.Value.X, e.Y);

                    proposedYOffset = lastMouseDownVerticalOffset + lastMouseDownLocation.Value.Y - currentPos.Y;
                }

                source.ScrollViewer.ScrollToVerticalOffset(proposedYOffset);

                
                if (proposedXOffset <= 0 || proposedXOffset > source.ScrollViewer.ScrollableWidth)
                {
                    lastMouseDownHorizontalOffset = proposedXOffset <= 0 ? 0 : source.ScrollViewer.ScrollableWidth;
                    lastMouseDownLocation = new Point(e.X, lastMouseDownLocation.Value.Y);
                    proposedXOffset = lastMouseDownHorizontalOffset + lastMouseDownLocation.Value.X - currentPos.X;
                }

                source.ScrollViewer.ScrollToHorizontalOffset(proposedXOffset);   
			}
		}
	}
}
