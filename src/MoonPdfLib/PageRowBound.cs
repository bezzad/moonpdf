using System.Windows;

namespace MoonPdfLib
{
	internal class PageRowBound
	{
		public Size Size { get; }
		public double VerticalOffset { get; }
		public double HorizontalOffset { get; }

		public PageRowBound(Size size, double verticalOffset, double horizontalOffset)
		{
			Size = size;
			VerticalOffset = verticalOffset;
			HorizontalOffset = horizontalOffset;
		}

		public Size SizeIncludingOffset => new Size(Size.Width + HorizontalOffset, Size.Height + VerticalOffset);
	}
}
