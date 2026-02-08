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
using Clipboard = System.Windows.Forms.Clipboard;

namespace SelectUnknown.Lens
{
    class LensHelper
    {
        /// <summary>
        /// 开始框定即搜
        /// </summary>
        public static void Start(string selectedWords = "")
        {
            var bmp = ScreencatchHelper.CaptureScreen();

            LensWindow lensWindow = new LensWindow(bmp, selectedWords);
            lensWindow.Show();
            lensWindow.Activate();
        }
        public static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
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
