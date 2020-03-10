using System.Windows;
using System.Windows.Media;

namespace MoonPdfLib.Helper
{
	internal static class VisualTreeHelperEx
	{
		public static T FindChild<T>(DependencyObject o) where T : DependencyObject
		{
			if (o is T obj)
				return obj;

			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = VisualTreeHelper.GetChild(o, i);
				var result = FindChild<T>(child);

				if (result != null)
					return result;
			}

			return null;
		}
	}
}
