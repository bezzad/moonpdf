using System;

namespace MoonPdfLib.Helper
{
	internal static class PageHelper
	{
		/// <summary>
		/// Returns the next page index or -1 if already at the last page
		/// </summary>
		/// <param name="currentPageIndex">The page index starting with 0</param>
		public static int GetPreviousPageIndex(int currentPageIndex, ViewType viewType)
		{
			var subtract = 1; // if single page view type

			if (viewType == ViewType.Facing)
				subtract = (currentPageIndex % 2 == 0 ? 2 : 3);
			else if (viewType == ViewType.BookView)
				subtract = (currentPageIndex % 2 == 0 ? 3 : 2);

			if (Math.Max(0, currentPageIndex - subtract) == 0 && currentPageIndex == 0) // we are already at the first visible page
				return -1;

			return Math.Max(0, currentPageIndex - subtract);
		}

		/// <summary>
		/// Returns the previous page index or -1 if already at the first page
		/// </summary>
		/// <param name="currentPageIndex">The page index starting with 0</param>
		public static int GetNextPageIndex(int currentPageIndex, int totalPages, ViewType viewType)
		{
			var add = 1; // if single page view type

			if (viewType == ViewType.Facing)
				add = (currentPageIndex % 2 == 0) ? 2 : 1;
			else if (viewType == ViewType.BookView)
				add = (currentPageIndex == 0 || currentPageIndex % 2 == 0) ? 1 : 2;

			if ((currentPageIndex + add) >= totalPages) // we are already at the last visible page
				return -1;

			return currentPageIndex + add;
		}

		/// <summary>
		/// The visible index can differ from the page index, if the ViewType is not SinglePage.
		/// Example: If you call this methode with currentPageIndex=1 and viewType=SinglePage you get a
		/// visible index of 1. If you call it with viewType=Facing, you would get a visible index of 0,
		/// because the second page (page index = 1) is in the same row as the first page (which is row 0).
		/// </summary>
		/// <param name="currentPageIndex">The page index starting with 0</param>
		public static int GetVisibleIndexFromPageIndex(int currentPageIndex, ViewType viewType)
		{
			var visibleIndex = currentPageIndex; // for single page

			if (viewType == ViewType.Facing)
				visibleIndex = ((currentPageIndex + ((currentPageIndex % 2 == 0) ? 3 : 1)) / 2) - 1;
			else if (viewType == ViewType.BookView && currentPageIndex > 0)
				visibleIndex = (currentPageIndex + ((currentPageIndex % 2 == 0) ? 0 : 1)) / 2;

			return visibleIndex;
		}
	}
}
