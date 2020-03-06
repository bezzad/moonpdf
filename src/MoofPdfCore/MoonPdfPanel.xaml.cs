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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MoonPdfLib
{
	public partial class MoonPdfPanel : UserControl
	{
		public event EventHandler PdfLoaded;
		public event EventHandler ZoomTypeChanged;
		public event EventHandler ViewTypeChanged;
		public event EventHandler PageRowDisplayChanged;
		public event EventHandler<PasswordRequiredEventArgs> PasswordRequired;

		private ZoomType zoomType = ZoomType.Fixed;
		private IMoonPdfPanel innerPanel;
		private MoonPdfPanelInputHandler inputHandler;
		private DispatcherTimer resizeTimer;

		#region Dependency properties
		public static readonly DependencyProperty PageMarginProperty = DependencyProperty.Register("PageMargin", typeof(Thickness),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(new Thickness(0, 2, 4, 2)));

		public static readonly DependencyProperty ZoomStepProperty = DependencyProperty.Register("ZoomStep", typeof(double),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(0.25));

		public static readonly DependencyProperty MinZoomFactorProperty = DependencyProperty.Register("MinZoomFactor", typeof(double),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(0.15));

		public static readonly DependencyProperty MaxZoomFactorProperty = DependencyProperty.Register("MaxZoomFactor", typeof(double),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(6.0));

		public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register("ViewType", typeof(ViewType),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(ViewType.SinglePage));

		public static readonly DependencyProperty RotationProperty = DependencyProperty.Register("Rotation", typeof(ImageRotation),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(ImageRotation.None));

		public static readonly DependencyProperty PageRowDisplayProperty = DependencyProperty.Register("PageRowDisplay", typeof(PageRowDisplayType),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(PageRowDisplayType.SinglePageRow));

		public Thickness PageMargin
		{
			get => (Thickness)GetValue(PageMarginProperty);
            set => SetValue(PageMarginProperty, value);
        }

		public double ZoomStep
		{
			get => (double)GetValue(ZoomStepProperty);
            set => SetValue(ZoomStepProperty, value);
        }

		public double MinZoomFactor
		{
			get => (double)GetValue(MinZoomFactorProperty);
            set => SetValue(MinZoomFactorProperty, value);
        }

		public double MaxZoomFactor
		{
			get => (double)GetValue(MaxZoomFactorProperty);
            set => SetValue(MaxZoomFactorProperty, value);
        }

		public ViewType ViewType
		{
			get => (ViewType)GetValue(ViewTypeProperty);
            set => SetValue(ViewTypeProperty, value);
        }

		public ImageRotation Rotation
		{
			get => (ImageRotation)GetValue(RotationProperty);
            set => SetValue(RotationProperty, value);
        }

		public PageRowDisplayType PageRowDisplay
		{
			get => (PageRowDisplayType)GetValue(PageRowDisplayProperty);
            set => SetValue(PageRowDisplayProperty, value);
        }
		#endregion

		public double HorizontalMargin => PageMargin.Right;
        public IPdfSource CurrentSource { get; private set; }
		public string CurrentPassword { get; private set; }
		public int TotalPages { get; private set; }
        internal PageRowBound[] PageRowBounds { get; private set; }

        public ZoomType ZoomType
		{
			get => zoomType;
            private set
			{
				if (zoomType != value)
				{
					zoomType = value;

                    ZoomTypeChanged?.Invoke(this, EventArgs.Empty);
                }
			}
		}

		internal ScrollViewer ScrollViewer => innerPanel.ScrollViewer;

        public float CurrentZoom => innerPanel.CurrentZoom;

        public MoonPdfPanel()
		{
			InitializeComponent();

			ChangeDisplayType(PageRowDisplay);
			inputHandler = new MoonPdfPanelInputHandler(this);

			SizeChanged += PdfViewerPanel_SizeChanged;

			resizeTimer = new DispatcherTimer();
			resizeTimer.Interval = TimeSpan.FromMilliseconds(150);
			resizeTimer.Tick += resizeTimer_Tick;
		}

		void PdfViewerPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (CurrentSource == null)
				return;

			resizeTimer.Stop();
			resizeTimer.Start();
		}

		void resizeTimer_Tick(object sender, EventArgs e)
		{
			resizeTimer.Stop();

			if (CurrentSource == null)
				return;

			if (ZoomType == ZoomType.FitToWidth)
				ZoomToWidth();
			else if (ZoomType == ZoomType.FitToHeight)
				ZoomToHeight();
		}

		public void OpenFile(string pdfFilename, string password = null)
		{
			if (!File.Exists(pdfFilename))
				throw new FileNotFoundException(string.Empty, pdfFilename);

			Open(new FileSource(pdfFilename), password);
		}

		public void Open(IPdfSource source, string password = null)
		{
			var pw = password;

			if (PasswordRequired != null && MuPdfWrapper.NeedsPassword(source) && pw == null)
			{
				var e = new PasswordRequiredEventArgs();
				PasswordRequired(this, e);

				if (e.Cancel)
					return;

				pw = e.Password;
			}

			LoadPdf(source, pw);
			CurrentSource = source;
			CurrentPassword = pw;

            PdfLoaded?.Invoke(this, EventArgs.Empty);
        }

		public void Unload()
		{
			CurrentSource = null;
			CurrentPassword = null;
			TotalPages = 0;

			innerPanel.Unload();

            PdfLoaded?.Invoke(this, EventArgs.Empty);
        }

		private void LoadPdf(IPdfSource source, string password)
		{
			var pageBounds = MuPdfWrapper.GetPageBounds(source, Rotation, password);
			PageRowBounds = CalculatePageRowBounds(pageBounds, ViewType);
			TotalPages = pageBounds.Length;
			innerPanel.Load(source, password);
		}

		private PageRowBound[] CalculatePageRowBounds(Size[] singlePageBounds, ViewType viewType)
		{
			var pagesPerRow = Math.Min(GetPagesPerRow(), singlePageBounds.Length); // if multiple page-view, but pdf contains less pages than the pages per row
			var finalBounds = new List<PageRowBound>();
			var verticalBorderOffset = (PageMargin.Top + PageMargin.Bottom);

			if (viewType == ViewType.SinglePage)
			{
				finalBounds.AddRange(singlePageBounds.Select(p => new PageRowBound(p, verticalBorderOffset, 0)));
			}
			else
			{
				var horizontalBorderOffset = HorizontalMargin;

				for (var i = 0; i < singlePageBounds.Length; i++)
				{
					if (i == 0 && viewType == ViewType.BookView)
					{
						finalBounds.Add(new PageRowBound(singlePageBounds[0], verticalBorderOffset, 0));
						continue;
					}

					var subset = singlePageBounds.Take(i, pagesPerRow).ToArray();

					// we get the max page-height from all pages in the subset and the sum of all page widths of the subset plus the offset between the pages
					finalBounds.Add(new PageRowBound(new Size(subset.Sum(f => f.Width), subset.Max(f => f.Height)), verticalBorderOffset, horizontalBorderOffset * (subset.Length - 1)));
					i += (pagesPerRow - 1);
				}
			}

			return finalBounds.ToArray();
		}

		internal int GetPagesPerRow()
		{
			return ViewType == ViewType.SinglePage ? 1 : 2;
		}

		public int GetCurrentPageNumber()
		{
			if (innerPanel == null)
				return -1;

			return innerPanel.GetCurrentPageIndex(ViewType) + 1;
		}

		public void ZoomToWidth()
		{
			innerPanel.ZoomToWidth();
			ZoomType = ZoomType.FitToWidth;
		}

		public void ZoomToHeight()
		{
			innerPanel.ZoomToHeight();
			ZoomType = ZoomType.FitToHeight;
		}

		public void ZoomIn()
		{
			innerPanel.ZoomIn();
			ZoomType = ZoomType.Fixed;
		}

		public void ZoomOut()
		{
			innerPanel.ZoomOut();
			ZoomType = ZoomType.Fixed;
		}

		public void Zoom(double zoomFactor)
		{
			innerPanel.Zoom(zoomFactor);
			ZoomType = ZoomType.Fixed;
		}

		/// <summary>
		/// Sets the ZoomType back to Fixed
		/// </summary>
		public void SetFixedZoom()
		{
			ZoomType = ZoomType.Fixed;
		}

		public void GotoPreviousPage()
		{
			innerPanel.GotoPreviousPage();
		}

		public void GotoNextPage()
		{
			innerPanel.GotoNextPage();
		}

		public void GotoPage(int pageNumber)
		{
			innerPanel.GotoPage(pageNumber);
		}

		public void GotoFirstPage()
		{
			GotoPage(1);
		}

		public void GotoLastPage()
		{
			GotoPage(TotalPages);
		}

		public void RotateRight()
		{
			if (Rotation != ImageRotation.Rotate270)
				Rotation = (ImageRotation)Rotation + 1;
			else
				Rotation = ImageRotation.None;
		}

		public void RotateLeft()
		{
			if ((int)Rotation > 0)
				Rotation = (ImageRotation)Rotation - 1;
			else
				Rotation = ImageRotation.Rotate270;
		}

		public void Rotate(ImageRotation rotation)
		{
			var currentPage = innerPanel.GetCurrentPageIndex(ViewType) + 1;
			LoadPdf(CurrentSource, CurrentPassword);
			innerPanel.GotoPage(currentPage);
		}

		public void TogglePageDisplay()
		{
			PageRowDisplay = (PageRowDisplay == PageRowDisplayType.SinglePageRow) ? PageRowDisplayType.ContinuousPageRows : PageRowDisplayType.SinglePageRow;
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property.Name.Equals("PageRowDisplay"))
				ChangeDisplayType((PageRowDisplayType)e.NewValue);
			else if (e.Property.Name.Equals("Rotation"))
				Rotate((ImageRotation)e.NewValue);
			else if (e.Property.Name.Equals("ViewType"))
				ApplyChangedViewType((ViewType)e.OldValue);
		}

		private void ApplyChangedViewType(ViewType oldViewType)
		{
			UpdateAndReload(() => { }, oldViewType);

            ViewTypeChanged?.Invoke(this, EventArgs.Empty);
        }

		private void ChangeDisplayType(PageRowDisplayType pageRowDisplayType)
		{
			UpdateAndReload(() =>
				{
					// we need to remove the current innerPanel
					pnlMain.Children.Clear();

					if (pageRowDisplayType == PageRowDisplayType.SinglePageRow)
						innerPanel = new SinglePageMoonPdfPanel(this);
					else
						innerPanel = new ContinuousMoonPdfPanel(this);

					pnlMain.Children.Add(innerPanel.Instance);
				}, ViewType);

            PageRowDisplayChanged?.Invoke(this, EventArgs.Empty);
        }

		private void UpdateAndReload(Action updateAction, ViewType viewType)
		{
			var currentPage = -1;
			var zoom = 1.0f;

			if (CurrentSource != null)
			{
				currentPage = innerPanel.GetCurrentPageIndex(viewType) + 1;
				zoom = innerPanel.CurrentZoom;
			}

			updateAction();

			if (currentPage > -1)
			{
				Action reloadAction = () =>
					{
						LoadPdf(CurrentSource, CurrentPassword);
						innerPanel.Zoom(zoom);
						innerPanel.GotoPage(currentPage);
					};

				if (innerPanel.Instance.IsLoaded)
					reloadAction();
				else
				{
					// we need to wait until the controls are loaded and then reload the pdf
					innerPanel.Instance.Loaded += (s, e) => { reloadAction(); };
				}
			}
		}

		/// <summary>
		/// Will only be triggered if the AllowDrop-Property is set to true
		/// </summary>
		/// <param name="e"></param>
		protected override void OnDrop(DragEventArgs e)
		{
			base.OnDrop(e);

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
				var filename = filenames.FirstOrDefault();

				if (filename != null && File.Exists(filename))
				{
					string pw = null;

					if (MuPdfWrapper.NeedsPassword(new FileSource(filename)))
					{
						if (PasswordRequired == null)
							return;

						var args = new PasswordRequiredEventArgs();
						PasswordRequired(this, args);

						if (args.Cancel)
							return;

						pw = args.Password;
					}

					try
					{
						OpenFile(filename, pw);
					}
					catch (Exception ex)
					{
						MessageBox.Show(string.Format("An error occured: " + ex.Message));
					}
				}
			}
		}
	}
}
