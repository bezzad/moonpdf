namespace MoonPdfLib
{
	internal class PageDisplaySettings
	{
		public int ImagesPerRow { get; set; }
		public double HorizontalOffsetBetweenPages { get; set; }
		public ViewType ViewType { get; set; }
		public float ZoomFactor { get; set; }
		public ImageRotation Rotation { get; set; }

		public PageDisplaySettings(int imagesPerRow, ViewType viewType, 
            double horizontalOffsetBetweenPages, 
            ImageRotation rotation = ImageRotation.None, 
            float zoomFactor = 1.0f)
		{
			ImagesPerRow = imagesPerRow;
			ZoomFactor = zoomFactor;
			ViewType = viewType;
			HorizontalOffsetBetweenPages = viewType == ViewType.SinglePage ? 0 : horizontalOffsetBetweenPages;
			Rotation = rotation;
		}
	}
}
