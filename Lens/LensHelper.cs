using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SelectUnknown.Lens
{
    class LensHelper
    {
        /// <summary>
        /// 开始框定即搜
        /// </summary>
        public static void Start(string selectedWords = "")
        {
            var bmp = CaptureScreen();
            var source = ConvertToBitmapSource(bmp);

            LensWindow lensWindow = new LensWindow(source, selectedWords);
            lensWindow.Show();
            lensWindow.Activate();
        }
        static Bitmap CaptureScreen()
        {
            int width = (int)SystemParameters.VirtualScreenWidth;
            int height = (int)SystemParameters.VirtualScreenHeight;
            int left = (int)SystemParameters.VirtualScreenLeft;
            int top = (int)SystemParameters.VirtualScreenTop;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(left, top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp;
        }

        static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();

            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap); // 非常重要，防止 GDI 泄漏
            }
        }

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);
    }
}
