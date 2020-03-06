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
using MoonPdfLib.Helper;
using MoonPdfLib.MuPdf;
using MoonPdfLib.Virtualizing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MoonPdfLib
{
	internal partial class ContinuousMoonPdfPanel : UserControl, IMoonPdfPanel
	{
		private MoonPdfPanel parent;
		private ScrollViewer scrollViewer;
		private CustomVirtualizingPanel virtualPanel;
		private PdfImageProvider imageProvider;
		private VirtualizingCollection<IEnumerable<PdfImage>> virtualizingPdfPages;

		public ContinuousMoonPdfPanel(MoonPdfPanel parent)
		{
			InitializeComponent();

			this.parent = parent;
			SizeChanged += ContinuousMoonPdfPanel_SizeChanged;
		}

		void ContinuousMoonPdfPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			scrollViewer = VisualTreeHelperEx.FindChild<ScrollViewer>(this);
		}

		public void Load(IPdfSource source, string password = null)
		{
			virtualPanel = VisualTreeHelperEx.FindChild<CustomVirtualizingPanel>(this);
			scrollViewer = VisualTreeHelperEx.FindChild<ScrollViewer>(this);
			virtualPanel.PageRowBounds = parent.PageRowBounds.Select(f => f.SizeIncludingOffset).ToArray();
			imageProvider = new PdfImageProvider(source, parent.TotalPages,
										new PageDisplaySettings(parent.GetPagesPerRow(), parent.ViewType, parent.HorizontalMargin, parent.Rotation),
										password: password);

			if (parent.ZoomType == ZoomType.Fixed)
				CreateNewItemsSource();
			else if (parent.ZoomType == ZoomType.FitToHeight)
				ZoomToHeight();
			else if (parent.ZoomType == ZoomType.FitToWidth)
				ZoomToWidth();

			if (scrollViewer != null)
			{
				scrollViewer.Visibility = Visibility.Visible;
				scrollViewer.ScrollToTop();
			}
		}

		public void Unload()
		{
			scrollViewer.Visibility = Visibility.Collapsed;
			scrollViewer.ScrollToHorizontalOffset(0);
			scrollViewer.ScrollToVerticalOffset(0);
			imageProvider = null;

			if (virtualizingPdfPages != null)
			{
				virtualizingPdfPages.CleanUpAllPages();
				virtualizingPdfPages = null;
			}

			itemsControl.ItemsSource = null;
		}

		private void CreateNewItemsSource()
		{
			var pageTimeout = TimeSpan.FromSeconds(2);

            virtualizingPdfPages?.CleanUpAllPages();

            virtualizingPdfPages = new AsyncVirtualizingCollection<IEnumerable<PdfImage>>(imageProvider, parent.GetPagesPerRow(), pageTimeout);
			itemsControl.ItemsSource = virtualizingPdfPages;
		}

		#region Zoom specific code
		public float CurrentZoom
		{
			get
			{
				if (imageProvider != null)
					return imageProvider.Settings.ZoomFactor;

				return 1.0f;
			}
		}

		public void ZoomToWidth()
		{
			var scrollBarWidth = scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible ? SystemParameters.VerticalScrollBarWidth : 0;
			scrollBarWidth += 2; // Magic number, sorry :)

			ZoomInternal((ActualWidth - scrollBarWidth) / parent.PageRowBounds.Max(f => f.SizeIncludingOffset.Width));
		}

		public void ZoomToHeight()
		{
			var scrollBarHeight = scrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible ? SystemParameters.HorizontalScrollBarHeight : 0;

			ZoomInternal((ActualHeight - scrollBarHeight) / parent.PageRowBounds.Max(f => f.SizeIncludingOffset.Height));
		}

		public void ZoomIn()
		{
			ZoomInternal(CurrentZoom + parent.ZoomStep);
		}

		public void ZoomOut()
		{
			ZoomInternal(CurrentZoom - parent.ZoomStep);
		}

		public void Zoom(double zoomFactor)
		{
			ZoomInternal(zoomFactor);
		}

		private void ZoomInternal(double zoomFactor)
		{
			if (zoomFactor > parent.MaxZoomFactor)
				zoomFactor = parent.MaxZoomFactor;
			else if (zoomFactor < parent.MinZoomFactor)
				zoomFactor = parent.MinZoomFactor;

			var yOffset = scrollViewer.VerticalOffset;
			var xOffset = scrollViewer.HorizontalOffset;
			var zoom = CurrentZoom;

			if (Math.Abs(Math.Round(zoom, 2) - Math.Round(zoomFactor, 2)) == 0.0)
				return;

			virtualPanel.PageRowBounds = parent.PageRowBounds.Select(f => new Size(f.Size.Width * zoomFactor + f.HorizontalOffset, f.Size.Height * zoomFactor + f.VerticalOffset)).ToArray();
			imageProvider.Settings.ZoomFactor = (float)zoomFactor;

			CreateNewItemsSource();

			scrollViewer.ScrollToHorizontalOffset((xOffset / zoom) * zoomFactor);
			scrollViewer.ScrollToVerticalOffset((yOffset / zoom) * zoomFactor);
		}
		#endregion

		public void GotoPreviousPage()
		{
			if (scrollViewer == null)
				return;

			var currentPageIndex = GetCurrentPageIndex(parent.ViewType);

			if (currentPageIndex == 0)
				return;

			var startIndex = PageHelper.GetVisibleIndexFromPageIndex(currentPageIndex - 1, parent.ViewType);
			var verticalOffset = virtualPanel.GetVerticalOffsetByItemIndex(startIndex);
			scrollViewer.ScrollToVerticalOffset(verticalOffset);
		}

		public void GotoNextPage()
		{
			var nextIndex = PageHelper.GetNextPageIndex(GetCurrentPageIndex(parent.ViewType), parent.TotalPages, parent.ViewType);

			if (nextIndex == -1)
				return;

			GotoPage(nextIndex + 1);
		}

		public void GotoPage(int pageNumber)
		{
			if (scrollViewer == null)
				return;

			var startIndex = PageHelper.GetVisibleIndexFromPageIndex(pageNumber - 1, parent.ViewType);
			var verticalOffset = virtualPanel.GetVerticalOffsetByItemIndex(startIndex);
			scrollViewer.ScrollToVerticalOffset(verticalOffset);
		}

		public int GetCurrentPageIndex(ViewType viewType)
		{
			if (scrollViewer == null)
				return 0;

			var pageIndex = virtualPanel.GetItemIndexByVerticalOffset(scrollViewer.VerticalOffset);

			if (pageIndex > 0)
			{
				if (viewType == ViewType.Facing)
					pageIndex *= 2;
				else if (viewType == ViewType.BookView)
					pageIndex = (pageIndex * 2) - 1;
			}

			return pageIndex;
		}

		ScrollViewer IMoonPdfPanel.ScrollViewer => scrollViewer;

		UserControl IMoonPdfPanel.Instance => this;
	}
}
