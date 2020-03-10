using MoonPdfLib.Helper;
using MoonPdfLib.MuPdf;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MoonPdfLib
{
	internal partial class SinglePageMoonPdfPanel : UserControl, IMoonPdfPanel
	{
		private MoonPdfPanel parent;
		private ScrollViewer scrollViewer;
		private PdfImageProvider imageProvider;
		private int currentPageIndex = 0; // starting at 0

		public SinglePageMoonPdfPanel(MoonPdfPanel parent)
		{
			InitializeComponent();
			this.parent = parent;
			SizeChanged += SinglePageMoonPdfPanel_SizeChanged;
		}

		void SinglePageMoonPdfPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			scrollViewer = VisualTreeHelperEx.FindChild<ScrollViewer>(this);
		}

		public void Load(IPdfSource source, string password = null)
		{
			scrollViewer = VisualTreeHelperEx.FindChild<ScrollViewer>(this);
			imageProvider = new PdfImageProvider(source, parent.TotalPages,
				new PageDisplaySettings(parent.GetPagesPerRow(), parent.ViewType, parent.HorizontalMargin, parent.Rotation), false, password);

			currentPageIndex = 0;

			if (scrollViewer != null)
				scrollViewer.Visibility = Visibility.Visible;

			if (parent.ZoomType == ZoomType.Fixed)
				SetItemsSource();
			else if (parent.ZoomType == ZoomType.FitToHeight)
				ZoomToHeight();
			else if (parent.ZoomType == ZoomType.FitToWidth)
				ZoomToWidth();
		}

		public void Unload()
		{
			scrollViewer.Visibility = Visibility.Collapsed;
			scrollViewer.ScrollToHorizontalOffset(0);
			scrollViewer.ScrollToVerticalOffset(0);
			currentPageIndex = 0;

			imageProvider = null;
		}

		ScrollViewer IMoonPdfPanel.ScrollViewer => scrollViewer;

		UserControl IMoonPdfPanel.Instance => this;

		void IMoonPdfPanel.GotoPage(int pageNumber)
		{
			currentPageIndex = pageNumber - 1;
			SetItemsSource();

			scrollViewer?.ScrollToTop();
		}

		void IMoonPdfPanel.GotoPreviousPage()
		{
			var prevPageIndex = PageHelper.GetPreviousPageIndex(currentPageIndex, parent.ViewType);

			if (prevPageIndex == -1)
				return;

			currentPageIndex = prevPageIndex;

			SetItemsSource();

			scrollViewer?.ScrollToTop();
		}

		void IMoonPdfPanel.GotoNextPage()
		{
			var nextPageIndex = PageHelper.GetNextPageIndex(currentPageIndex, parent.TotalPages, parent.ViewType);

			if (nextPageIndex == -1)
				return;

			currentPageIndex = nextPageIndex;

			SetItemsSource();

			scrollViewer?.ScrollToTop();
		}

		private void SetItemsSource()
		{
			var startIndex = PageHelper.GetVisibleIndexFromPageIndex(currentPageIndex, parent.ViewType);
			itemsControl.ItemsSource = imageProvider.FetchRange(startIndex, parent.GetPagesPerRow()).FirstOrDefault();
		}

		public int GetCurrentPageIndex(ViewType viewType)
		{
			return currentPageIndex;
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
			var zoomFactor = (parent.ActualWidth - scrollBarWidth) / parent.PageRowBounds[currentPageIndex].SizeIncludingOffset.Width;
			var pageBound = parent.PageRowBounds[currentPageIndex];

			if (scrollBarWidth == 0 && ((pageBound.Size.Height * zoomFactor) + pageBound.VerticalOffset) >= parent.ActualHeight)
				scrollBarWidth += SystemParameters.VerticalScrollBarWidth;

			scrollBarWidth += 2; // Magic number, sorry :)
			zoomFactor = (parent.ActualWidth - scrollBarWidth) / parent.PageRowBounds[currentPageIndex].SizeIncludingOffset.Width;

			ZoomInternal(zoomFactor);
		}

		public void ZoomToHeight()
		{
			var scrollBarHeight = scrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible ? SystemParameters.HorizontalScrollBarHeight : 0;
			var zoomFactor = (parent.ActualHeight - scrollBarHeight) / parent.PageRowBounds[currentPageIndex].SizeIncludingOffset.Height;
			var pageBound = parent.PageRowBounds[currentPageIndex];

			if (scrollBarHeight == 0 && ((pageBound.Size.Width * zoomFactor) + pageBound.HorizontalOffset) >= parent.ActualWidth)
				scrollBarHeight += SystemParameters.HorizontalScrollBarHeight;

			zoomFactor = (parent.ActualHeight - scrollBarHeight) / parent.PageRowBounds[currentPageIndex].SizeIncludingOffset.Height;

			ZoomInternal(zoomFactor);
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

			imageProvider.Settings.ZoomFactor = (float)zoomFactor;

			SetItemsSource();
		}
		#endregion
	}
}
