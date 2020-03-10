using MoonPdfLib.MuPdf;
using System.Windows.Controls;

namespace MoonPdfLib
{
	/// <summary>
	/// Common interface for the two different display types,
	/// single pages (SinglePageMoonPdfPanel) and continuous pages (ContinuousMoonPdfPanel)
	/// </summary>
	internal interface IMoonPdfPanel
	{
		ScrollViewer ScrollViewer { get; }
		UserControl Instance { get; }
		float CurrentZoom { get; }
		void Load(IPdfSource source, string password = null);
		void Unload();
		void Zoom(double zoomFactor);
		void ZoomIn();
		void ZoomOut();
		void ZoomToWidth();
		void ZoomToHeight();
		void GotoPage(int pageNumber);
		void GotoPreviousPage();
		void GotoNextPage();
		int GetCurrentPageIndex(ViewType viewType);
	}
}
