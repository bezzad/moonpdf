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
/*
 * 2013 - Modified version of Paul McClean's code (see AUTHORS file)
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MoonPdfLib.Virtualizing
{
	internal class CustomVirtualizingPanel : VirtualizingPanel, IScrollInfo
	{
		public CustomVirtualizingPanel()
		{
			// For use in the IScrollInfo implementation
			RenderTransform = _trans;
		}

        /// <summary>
        /// the bounds are a combination of all pages from the same row (plus the offset borders, if not ViewType.SinglePage)
		/// </summary>
		public Size[] PageRowBounds { get; set; }

		public int GetItemIndexByVerticalOffset(double yOffset)
		{
			var sum = 0.0;

			for (var i = 0; i < PageRowBounds.Length; i++)
			{
				sum += PageRowBounds[i].Height;

				if( yOffset < sum )
					return i;
			}

			return PageRowBounds.Length - 1;
		}
		
		public double GetVerticalOffsetByItemIndex(int itemIndex)
		{
			// sum the heights of all previous pages: this is where the current y offset should be
			return PageRowBounds.Take(itemIndex).Sum(f => f.Height);
		}

		/// <summary>
		/// Measure the children
		/// </summary>
		/// <param name="availableSize">Size available</param>
		/// <returns>Size desired</returns>
		protected override Size MeasureOverride(Size availableSize)
		{
			UpdateScrollInfo(availableSize);

			if (PageRowBounds == null || PageRowBounds.Length == 0)
				return availableSize;

			// Figure out range that's visible based on layout algorithm
			int firstVisibleItemIndex, lastVisibleItemIndex;
			GetVisibleRange(out firstVisibleItemIndex, out lastVisibleItemIndex);

			if (firstVisibleItemIndex == -1)
				return availableSize;

			// We need to access InternalChildren before the generator to work around a bug
			var children = InternalChildren;
			var generator = ItemContainerGenerator;

			// Get the generator position of the first visible data item
			var startPos = generator.GeneratorPositionFromIndex(firstVisibleItemIndex);

			// Get index where we'd insert the child for this position. If the item is realized
			// (position.Offset == 0), it's just position.Index, otherwise we have to add one to
			// insert after the corresponding child
			var childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;

			using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
			{
				for (var itemIndex = firstVisibleItemIndex; itemIndex <= lastVisibleItemIndex; ++itemIndex, ++childIndex)
				{
					bool newlyRealized;

					// Get or create the child
					var child = generator.GenerateNext(out newlyRealized) as UIElement;
					if (newlyRealized)
					{
						// Figure out if we need to insert the child at the end or somewhere in the middle
						if (childIndex >= children.Count)
						{
							AddInternalChild(child);
						}
						else
						{
							InsertInternalChild(childIndex, child);
						}
						generator.PrepareItemContainer(child);
					}
					else
					{
						// The child has already been created, let's be sure it's in the right spot
						Debug.Assert(child == children[childIndex], "Wrong child was generated");
					}

					// Measurements will depend on layout algorithm
					child.Measure(PageRowBounds[itemIndex]);
				}
			}

			// Note: this could be deferred to idle time for efficiency
			CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);

			return availableSize;
		}

		/// <summary>
		/// Arrange the children
		/// </summary>
		/// <param name="finalSize">Size available</param>
		/// <returns>Size used</returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			var generator = ItemContainerGenerator;

			UpdateScrollInfo(finalSize);

			for (var i = 0; i < Children.Count; i++)
			{
				var child = Children[i];

				// Map the child offset to an item offset
				var itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));

				ArrangeChild(itemIndex, child, finalSize);
			}

			return finalSize;
		}

		/// <summary>
		/// Revirtualize items that are no longer visible
		/// </summary>
		/// <param name="minDesiredGenerated">first item index that should be visible</param>
		/// <param name="maxDesiredGenerated">last item index that should be visible</param>
		private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
		{
			var children = InternalChildren;
			var generator = ItemContainerGenerator;

			for (var i = children.Count - 1; i >= 0; i--)
			{
				var childGeneratorPos = new GeneratorPosition(i, 0);
				var itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);

				if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
				{
					generator.Remove(childGeneratorPos, 1);
					RemoveInternalChildRange(i, 1);
				}
			}
		}

		#region Layout specific code
		// Paul McClean's comment: I've isolated the layout specific code to this region.
		// If you want to do something other than tiling, this is where you'll make your changes

		/// <summary>
		/// Calculate the extent of the view based on the available size
		/// </summary>
		/// <param name="availableSize">available size</param>
		/// <param name="itemCount">number of data items</param>
		/// <returns></returns>
		private Size CalculateExtent(Size availableSize, int itemCount)
		{
			if (PageRowBounds == null || PageRowBounds.Length == 0)
				return new Size(availableSize.Width, _extent.Height);

			// we get the pdf page with the greatest width, so we know how broad the extent must be
			var maxWidth = PageRowBounds.Select(f => f.Width).Max();

			// we get the sum of all pdf page heights, so we know how high the extent must be
			var totalHeight = PageRowBounds.Sum(f => f.Height);

			return new Size(maxWidth, totalHeight);
		}

		/// <summary>
		/// Get the range of children that are visible
		/// </summary>
		/// <param name="firstVisibleItemIndex">The item index of the first visible item</param>
		/// <param name="lastVisibleItemIndex">The item index of the last visible item</param>
		private void GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
		{
			firstVisibleItemIndex = -1;
			lastVisibleItemIndex = -1;

			if (PageRowBounds == null || PageRowBounds.Length == 0)
				return;

			var sum = 0.0;
			var bottom = _offset.Y + _viewport.Height;

			// we iterate through all the items and add the height of them to "sum"
			for (var i = 0; i < PageRowBounds.Length; i++)
			{
				sum += PageRowBounds[i].Height;

				// when we detect, that the current y offset (_offset.Y) is smaller than
				// this "sum", we set first and last visible index
				if (_offset.Y < sum)
				{
					firstVisibleItemIndex = i;
					lastVisibleItemIndex = i;

					// used to determine the lastVisibleItemIndex
					for (var k = i + 1; k < PageRowBounds.Length; k++)
					{
						sum += PageRowBounds[k].Height;

						if (bottom < sum || k == PageRowBounds.Length - 1)
						{
							lastVisibleItemIndex = k;
							break;
						}
					}

					break;
				}
			}

			var itemsControl = ItemsControl.GetItemsOwner(this);
			var itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

			if (lastVisibleItemIndex >= itemCount)
				lastVisibleItemIndex = itemCount - 1;
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);

			if (sizeInfo.WidthChanged && sizeInfo.NewSize.Width > sizeInfo.PreviousSize.Width) // only necessary when width was increased
			{
				var widthOffset = sizeInfo.NewSize.Width - sizeInfo.PreviousSize.Width;
				ScrollOwner.ScrollToHorizontalOffset(ScrollOwner.HorizontalOffset - widthOffset);
			}
		}

		/// <summary>
		/// Position a child
		/// </summary>
		/// <param name="itemIndex">The data item index of the child</param>
		/// <param name="child">The element to position</param>
		/// <param name="finalSize">The size of the panel</param>
		private void ArrangeChild(int itemIndex, UIElement child, Size finalSize)
		{
			var size = PageRowBounds[itemIndex];
			var x = Math.Max(0, (finalSize.Width / 2) - (size.Width / 2)); // used to center the content horizontally
			var y = GetVerticalOffsetByItemIndex(itemIndex);

			child.Arrange(new Rect(x, y, size.Width, size.Height));
		}
		#endregion

		#region IScrollInfo implementation
		// Paul McClean's comment: See Ben Constable's series of posts at http://blogs.msdn.com/bencon/
		
		private void UpdateScrollInfo(Size availableSize)
		{
			// See how many items there are
			var itemsControl = ItemsControl.GetItemsOwner(this);
			var itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

			var extent = CalculateExtent(availableSize, itemCount);
			// Update extent
			if (extent != _extent)
			{
				_extent = extent;
				if (_owner != null)
					_owner.InvalidateScrollInfo();
			}

			// Update viewport
			if (availableSize != _viewport)
			{
				_viewport = availableSize;
				if (_owner != null)
					_owner.InvalidateScrollInfo();
			}
		}

		public ScrollViewer ScrollOwner
		{
			get => _owner;
            set => _owner = value;
        }

		public bool CanHorizontallyScroll
		{
			get => _canHScroll;
            set => _canHScroll = value;
        }

		public bool CanVerticallyScroll
		{
			get => _canVScroll;
            set => _canVScroll = value;
        }

		public double HorizontalOffset => _offset.X;

        public double VerticalOffset => _offset.Y;

        public double ExtentHeight => _extent.Height;

        public double ExtentWidth => _extent.Width;

        public double ViewportHeight => _viewport.Height;

        public double ViewportWidth => _viewport.Width;

        private double CalculateVerticalScrollOffset()
		{
			return ViewportHeight * 0.06;
		}

		private double CalculateHorizontalScrollOffset()
		{
			return ViewportWidth * 0.06;
		}

		public void LineUp()
		{
			SetVerticalOffset(VerticalOffset - CalculateVerticalScrollOffset());
		}

		public void LineDown()
		{
			SetVerticalOffset(VerticalOffset + CalculateVerticalScrollOffset());
		}

		public void PageUp()
		{
			SetVerticalOffset(VerticalOffset - _viewport.Height);
		}

		public void PageDown()
		{
			SetVerticalOffset(VerticalOffset + _viewport.Height);
		}

		public void MouseWheelUp()
		{
			SetVerticalOffset(VerticalOffset - (3 * CalculateVerticalScrollOffset()));
		}

		public void MouseWheelDown()
		{
			SetVerticalOffset(VerticalOffset + (3 * CalculateVerticalScrollOffset()));
		}

		public void LineLeft()
		{
			SetHorizontalOffset(HorizontalOffset - CalculateHorizontalScrollOffset());
		}

		public void LineRight()
		{
			SetHorizontalOffset(HorizontalOffset + CalculateHorizontalScrollOffset());
		}

		public Rect MakeVisible(Visual visual, Rect rectangle)
		{
			return new Rect();
		}

		public void MouseWheelLeft()
		{
			LineLeft();
		}

		public void MouseWheelRight()
		{
			LineRight();
		}

		public void PageLeft()
		{
			SetHorizontalOffset(HorizontalOffset - _viewport.Width);
		}

		public void PageRight()
		{
			SetHorizontalOffset(HorizontalOffset + _viewport.Width);
		}

		public void SetHorizontalOffset(double offset)
		{
			if (offset < 0 || _viewport.Width >= _extent.Width)
			{
				offset = 0;
			}
			else if (offset + _viewport.Width >= _extent.Width)
			{
				offset = _extent.Width - _viewport.Width;
			}

			_offset.X = offset;

			if (_owner != null)
				_owner.InvalidateScrollInfo();

			_trans.X = -offset;

			// Force us to realize the correct children
			InvalidateMeasure();
		}

		public void SetVerticalOffset(double offset)
		{
			if (offset < 0 || _viewport.Height >= _extent.Height)
			{
				offset = 0;
			}
			else if (offset + _viewport.Height >= _extent.Height)
			{
				offset = _extent.Height - _viewport.Height;
			}

			_offset.Y = offset;

			if (_owner != null)
				_owner.InvalidateScrollInfo();

			_trans.Y = -offset;

			// Force us to realize the correct children
			InvalidateMeasure();
		}

		private TranslateTransform _trans = new TranslateTransform();
		private ScrollViewer _owner;
		private bool _canHScroll = false;
		private bool _canVScroll = false;
		private Size _extent = new Size(0, 0);
		private Size _viewport = new Size(0, 0);
		private Point _offset;

		#endregion
	}
}
