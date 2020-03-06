using MoonPdfLib.Helper;
/*
 * 2013 - Modified and extended version of W. Jordan's code (see AUTHORS file)
 */
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MoonPdfLib.MuPdf
{
	public static class MuPdfWrapper
	{
		/// <summary>
		/// Extracts a PDF page as a Bitmap for a given pdf filename and a page number.
		/// </summary>
		/// <param name="pageNumber">Page number, starting at 1</param>
		/// <param name="zoomFactor">Used to get a smaller or bigger Bitmap, depending on the specified value</param>
		/// <param name="password">The password for the pdf file (if required)</param>
		public static Bitmap ExtractPage(IPdfSource source, int pageNumber, float zoomFactor = 1.0f, string password = null)
		{
			var pageNumberIndex = Math.Max(0, pageNumber - 1); // pages start at index 0

            using var stream = new PdfFileStream(source);
            ValidatePassword(stream.Document, password);

            var p = NativeMethods.LoadPage(stream.Document, pageNumberIndex); // loads the page
            var bmp = RenderPage(stream.Context, stream.Document, p, zoomFactor);
            NativeMethods.FreePage(stream.Document, p); // releases the resources consumed by the page

            return bmp;
        }

		/// <summary>
		/// Gets the page bounds for all pages of the given PDF. If a relevant rotation is supplied, the bounds will
		/// be rotated accordingly before returning.
		/// </summary>
		/// <param name="rotation">The rotation that should be applied</param>
		/// <param name="password">The password for the pdf file (if required)</param>
		/// <returns></returns>
		public static System.Windows.Size[] GetPageBounds(IPdfSource source, ImageRotation rotation = ImageRotation.None, string password = null)
		{
			Func<double, double, System.Windows.Size> sizeCallback = (width, height) => new System.Windows.Size(width, height);

			if (rotation == ImageRotation.Rotate90 || rotation == ImageRotation.Rotate270)
				sizeCallback = (width, height) => new System.Windows.Size(height, width); // switch width and height

            using var stream = new PdfFileStream(source);
            ValidatePassword(stream.Document, password);

            var pageCount = NativeMethods.CountPages(stream.Document); // gets the number of pages in the document
            var resultBounds = new System.Windows.Size[pageCount];

            for (var i = 0; i < pageCount; i++)
            {
                var p = NativeMethods.LoadPage(stream.Document, i); // loads the page
                var pageBound = NativeMethods.BoundPage(stream.Document, p);

                resultBounds[i] = sizeCallback(pageBound.Width, pageBound.Height);

                NativeMethods.FreePage(stream.Document, p); // releases the resources consumed by the page
            }

            return resultBounds;
        }

		/// <summary>
		/// Return the total number of pages for a give PDF.
		/// </summary>
		public static int CountPages(IPdfSource source, string password = null)
        {
            using var stream = new PdfFileStream(source);
            ValidatePassword(stream.Document, password);

            return NativeMethods.CountPages(stream.Document); // gets the number of pages in the document
        }

		public static bool NeedsPassword(IPdfSource source)
        {
            using var stream = new PdfFileStream(source);
            return NeedsPassword(stream.Document);
        }

		private static void ValidatePassword(IntPtr doc, string password)
		{
			if (NeedsPassword(doc) && NativeMethods.AuthenticatePassword(doc, password) == 0)
				throw new MissingOrInvalidPdfPasswordException();
		}

		private static bool NeedsPassword(IntPtr doc)
		{
			return NativeMethods.NeedsPassword(doc) != 0;
		}

		static Bitmap RenderPage(IntPtr context, IntPtr document, IntPtr page, float zoomFactor)
		{
			var pageBound = NativeMethods.BoundPage(document, page);
			var ctm = new Matrix();
			var pix = IntPtr.Zero;
			var dev = IntPtr.Zero;

			var currentDpi = DpiHelper.GetCurrentDpi();
			var zoomX = zoomFactor * (currentDpi.HorizontalDpi / DpiHelper.DEFAULT_DPI);
			var zoomY = zoomFactor * (currentDpi.VerticalDpi / DpiHelper.DEFAULT_DPI);

			// gets the size of the page and multiplies it with zoom factors
			var width = (int)(pageBound.Width * zoomX);
			var height = (int)(pageBound.Height * zoomY);

			// sets the matrix as a scaling matrix (zoomX,0,0,zoomY,0,0)
			ctm.A = zoomX;
			ctm.D = zoomY;

			// creates a pixmap the same size as the width and height of the page
			pix = NativeMethods.NewPixmap(context, NativeMethods.FindDeviceColorSpace(context, "DeviceRGB"), width, height);
			// sets white color as the background color of the pixmap
			NativeMethods.ClearPixmap(context, pix, 0xFF);

			// creates a drawing device
			dev = NativeMethods.NewDrawDevice(context, pix);
			// draws the page on the device created from the pixmap
			NativeMethods.RunPage(document, page, dev, ctm, IntPtr.Zero);

			NativeMethods.FreeDevice(dev); // frees the resources consumed by the device
			dev = IntPtr.Zero;

			// creates a colorful bitmap of the same size of the pixmap
			var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
			var imageData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);
			unsafe
			{ // converts the pixmap data to Bitmap data
				var ptrSrc = (byte*)NativeMethods.GetSamples(context, pix); // gets the rendered data from the pixmap
				var ptrDest = (byte*)imageData.Scan0;
				for (var y = 0; y < height; y++)
				{
					var pl = ptrDest;
					var sl = ptrSrc;
					for (var x = 0; x < width; x++)
					{
						//Swap these here instead of in MuPDF because most pdf images will be rgb or cmyk.
						//Since we are going through the pixels one by one anyway swap here to save a conversion from rgb to bgr.
						pl[2] = sl[0]; //b-r
						pl[1] = sl[1]; //g-g
						pl[0] = sl[2]; //r-b
									   //sl[3] is the alpha channel, we will skip it here
						pl += 3;
						sl += 4;
					}
					ptrDest += imageData.Stride;
					ptrSrc += width * 4;
				}
			}
			bmp.UnlockBits(imageData);

			NativeMethods.DropPixmap(context, pix);
			bmp.SetResolution(currentDpi.HorizontalDpi, currentDpi.VerticalDpi);

			return bmp;
		}

		/// <summary>
		/// Helper class for an easier disposing of unmanaged resources
		/// </summary>
		private sealed class PdfFileStream : IDisposable
		{
			const uint FZ_STORE_DEFAULT = 256 << 20;

			public IntPtr Context { get; }
			public IntPtr Stream { get; }
			public IntPtr Document { get; }

			public PdfFileStream(IPdfSource source)
			{
				if (source is FileSource)
				{
					var fs = (FileSource)source;
					Context = NativeMethods.NewContext(IntPtr.Zero, IntPtr.Zero, FZ_STORE_DEFAULT); // Creates the context
					Stream = NativeMethods.OpenFile(Context, fs.Filename); // opens file as a stream
					Document = NativeMethods.OpenDocumentStream(Context, ".pdf", Stream); // opens the document
				}
				else if (source is MemorySource)
				{
					var ms = (MemorySource)source;
					Context = NativeMethods.NewContext(IntPtr.Zero, IntPtr.Zero, FZ_STORE_DEFAULT); // Creates the context
					var pinnedArray = GCHandle.Alloc(ms.Bytes, GCHandleType.Pinned);
					var pointer = pinnedArray.AddrOfPinnedObject();
					Stream = NativeMethods.OpenStream(Context, pointer, ms.Bytes.Length); // opens file as a stream
					Document = NativeMethods.OpenDocumentStream(Context, ".pdf", Stream); // opens the document
					pinnedArray.Free();
				}
			}

			public void Dispose()
			{
				NativeMethods.CloseDocument(Document); // releases the resources
				NativeMethods.CloseStream(Stream);
				NativeMethods.FreeContext(Context);
			}
		}

		private static class NativeMethods
		{
			[DllImport("libmupdf.dll", EntryPoint = "fz_new_context", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr NewContext(IntPtr alloc, IntPtr locks, uint max_store);

			[DllImport("libmupdf.dll", EntryPoint = "fz_free_context", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr FreeContext(IntPtr ctx);

			[DllImport("libmupdf.dll", EntryPoint = "fz_open_file_w", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr OpenFile(IntPtr ctx, string fileName);

			[DllImport("libmupdf.dll", EntryPoint = "fz_open_document_with_stream", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr OpenDocumentStream(IntPtr ctx, string magic, IntPtr stm);

			[DllImport("libmupdf.dll", EntryPoint = "fz_close", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr CloseStream(IntPtr stm);

			[DllImport("libmupdf.dll", EntryPoint = "fz_close_document", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr CloseDocument(IntPtr doc);

			[DllImport("libmupdf.dll", EntryPoint = "fz_count_pages", CallingConvention = CallingConvention.Cdecl)]
			public static extern int CountPages(IntPtr doc);

			[DllImport("libmupdf.dll", EntryPoint = "fz_bound_page", CallingConvention = CallingConvention.Cdecl)]
			public static extern Rectangle BoundPage(IntPtr doc, IntPtr page);

			[DllImport("libmupdf.dll", EntryPoint = "fz_clear_pixmap_with_value", CallingConvention = CallingConvention.Cdecl)]
			public static extern void ClearPixmap(IntPtr ctx, IntPtr pix, int byteValue);

			[DllImport("libmupdf.dll", EntryPoint = "fz_find_device_colorspace", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr FindDeviceColorSpace(IntPtr ctx, string colorspace);

			[DllImport("libmupdf.dll", EntryPoint = "fz_free_device", CallingConvention = CallingConvention.Cdecl)]
			public static extern void FreeDevice(IntPtr dev);

			[DllImport("libmupdf.dll", EntryPoint = "fz_free_page", CallingConvention = CallingConvention.Cdecl)]
			public static extern void FreePage(IntPtr doc, IntPtr page);

			[DllImport("libmupdf.dll", EntryPoint = "fz_load_page", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr LoadPage(IntPtr doc, int pageNumber);

			[DllImport("libmupdf.dll", EntryPoint = "fz_new_draw_device", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr NewDrawDevice(IntPtr ctx, IntPtr pix);

			[DllImport("libmupdf.dll", EntryPoint = "fz_new_pixmap", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr NewPixmap(IntPtr ctx, IntPtr colorspace, int width, int height);

			[DllImport("libmupdf.dll", EntryPoint = "fz_run_page", CallingConvention = CallingConvention.Cdecl)]
			public static extern void RunPage(IntPtr doc, IntPtr page, IntPtr dev, Matrix transform, IntPtr cookie);

			[DllImport("libmupdf.dll", EntryPoint = "fz_drop_pixmap", CallingConvention = CallingConvention.Cdecl)]
			public static extern void DropPixmap(IntPtr ctx, IntPtr pix);

			[DllImport("libmupdf.dll", EntryPoint = "fz_pixmap_samples", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr GetSamples(IntPtr ctx, IntPtr pix);

			[DllImport("libmupdf.dll", EntryPoint = "fz_needs_password", CallingConvention = CallingConvention.Cdecl)]
			public static extern int NeedsPassword(IntPtr doc);

			[DllImport("libmupdf.dll", EntryPoint = "fz_authenticate_password", CallingConvention = CallingConvention.Cdecl)]
			public static extern int AuthenticatePassword(IntPtr doc, string password);

			[DllImport("libmupdf.dll", EntryPoint = "fz_open_memory", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr OpenStream(IntPtr ctx, IntPtr data, int len);
		}
	}
}
