using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;

namespace SelectUnknown.Lens
{
    internal class OCRHelper
    {
        public static async Task<string> RecognizeAsync(Bitmap bitmap)
        {
            bitmap = OptimizeBitmapForOcr(bitmap);
            Clipboard.SetImage(bitmap);
            string tmpPath = Path.Combine(Path.GetTempPath(), $"ocr_temp_{Guid.NewGuid()}.png");
            bitmap.Save(tmpPath, System.Drawing.Imaging.ImageFormat.Png);
            
            string txt = await RecognizeTextByWinSysOcrAsync(tmpPath);
            
            // 清理临时文件
            if (File.Exists(tmpPath))
            {
                try
                {
                    File.Delete(tmpPath);
                }
                catch { /* 忽略删除失败 */ }
            }
            txt = Normalize(txt);
            return txt;
        }
        /// <summary>
        /// 规范化
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string s = text;

            // 中文之间误插空格
            s = Regex.Replace(s, @"(?<=[\u4e00-\u9fa5])\s+(?=[\u4e00-\u9fa5])", "");

            // 中文与标点
            s = Regex.Replace(s, @"(?<=[\u4e00-\u9fa5])\s+(?=[，。！？：；）])", "");
            s = Regex.Replace(s, @"(?<=[（])\s+(?=[\u4e00-\u9fa5])", "");

            // ✅ 单个小写字母向左合并（a 例外）
            // 例： "tes t" → "test"
            s = Regex.Replace(
                s,
                @"(?<=[A-Za-z]{2,})\s+([b-z])\b",
                "$1",
                RegexOptions.IgnoreCase
            );

            // 数字拆分
            s = Regex.Replace(s, @"(?<=\d)\s+(?=\d)", "");

            // 清理多余空格
            s = Regex.Replace(s, @"\s{2,}", " ").Trim();

            return s;
        }
        #region Bitmap 优化（OCR 前预处理）

        /// <summary>
        /// OCR 前 Bitmap 预处理总入口（小尺寸自动放大）
        /// </summary>
        public static Bitmap OptimizeBitmapForOcr(Bitmap src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            Bitmap bmp = ToGrayscaleBitmap(src);// 灰度化
            Clipboard.SetImage(bmp);
            bool invert = ShouldInvert(bmp); // 自动判断是否需要反色

            bmp = ResizeInterpolation(bmp, 2.7f);// 放大
            Clipboard.SetImage(bmp);

            bmp = Blur(bmp, 1);// 轻微模糊，减少噪点
            Clipboard.SetImage(bmp);

            bmp = BinarizeBitmap(bmp, 25, false);// 二值化
            Clipboard.SetImage(bmp);

            bmp = PadBitmapToMinSize(bmp, MinOcrSize);// 填充到最小尺寸
            Clipboard.SetImage(bmp);

            //bmp = Dilation(bmp, DilationStrength);// 膨胀
            //Clipboard.SetImage(bmp);

            return bmp;
        }
        // ===== OCR Bitmap 调优参数 =====

        // 最小 OCR 尺寸
        private const int MinOcrSize = 45;

        // 二值化阈值 越小越狠
        private static byte BinarizeThreshold = 128;

        // 膨胀开关
        private static bool EnableDilation = true;

        // 膨胀强度（1 = 很轻，2 = 中等，3 = 很狠）
        private static byte DilationStrength = 1;

        // 膨胀半径（像素）
        private static int DilationRadius = 1;

        // 背景色（通常白色最稳）
        private static Color PadColor = Color.White;
        private static Bitmap PadBitmapToMinSize(Bitmap src, int minSize)
        {
            // 计算新图像的尺寸（取当前尺寸与 minSize 的较大值）
            int newWidth = Math.Max(src.Width, minSize);
            int newHeight = Math.Max(src.Height, minSize);

            // 如果尺寸没有变化，直接返回原图副本或原图
            if (newWidth == src.Width && newHeight == src.Height)
            {
                return new Bitmap(src);
            }

            // 创建目标位图，建议保持与原图相同的像素格式
            Bitmap paddedBitmap = new Bitmap(newWidth, newHeight, src.PixelFormat);

            using (Graphics g = Graphics.FromImage(paddedBitmap))
            {
                // 将背景涂白（根据需求，也可以改为透明 Color.Transparent）
                g.Clear(ColorTranslator.FromHtml("#FFFFFF"));

                // 将原图绘制(居中)，右侧和下方会自动留下空白
                g.DrawImage(src, (newWidth / 2) - (src.Width / 2), (newHeight / 2) - (src.Height / 2), src.Width, src.Height);
            }
            Console.WriteLine(paddedBitmap.Height + "kaun" + paddedBitmap.Width);

            return paddedBitmap;
        }
        /// <summary>
        /// 灰度化
        /// </summary>
        private static Bitmap ToGrayscaleBitmap(Bitmap src)
        {
            var bmp = new Bitmap(src.Width, src.Height);

            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    var c = src.GetPixel(x, y);
                    int gray = (int)(0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
                    bmp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                }

            return bmp;
        }
        private static Bitmap Blur(Bitmap srcBmp, int radius)
        {
            if (radius <= 0) return (Bitmap)srcBmp.Clone();

            int width = srcBmp.Width;
            int height = srcBmp.Height;
            PixelFormat format = srcBmp.PixelFormat;

            BitmapData data = srcBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, format);
            int stride = data.Stride;
            int bytesPerPixel = Image.GetPixelFormatSize(format) / 8;
            int totalBytes = stride * height;

            byte[] srcPixels = new byte[totalBytes];
            byte[] dstPixels = new byte[totalBytes];
            Marshal.Copy(data.Scan0, srcPixels, 0, totalBytes);
            srcBmp.UnlockBits(data);

            // 遍历像素进行区域求和取平均
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int rSum = 0, gSum = 0, bSum = 0, count = 0;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int tx = x + kx;
                            int ty = y + ky;

                            if (tx >= 0 && tx < width && ty >= 0 && ty < height)
                            {
                                int idx = ty * stride + tx * bytesPerPixel;
                                bSum += srcPixels[idx];
                                gSum += srcPixels[idx + 1];
                                rSum += srcPixels[idx + 2];
                                count++;
                            }
                        }
                    }

                    int currentIdx = y * stride + x * bytesPerPixel;
                    dstPixels[currentIdx] = (byte)(bSum / count);
                    dstPixels[currentIdx + 1] = (byte)(gSum / count);
                    dstPixels[currentIdx + 2] = (byte)(rSum / count);
                    if (bytesPerPixel == 4) dstPixels[currentIdx + 3] = 255;
                }
            }

            Bitmap resBmp = new Bitmap(width, height, format);
            BitmapData resData = resBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, format);
            Marshal.Copy(dstPixels, 0, resData.Scan0, totalBytes);
            resBmp.UnlockBits(resData);
            return resBmp;
        }
        /// <summary>
        /// 自适应二值化 (非 unsafe 版)
        /// </summary>
        /// <param name="srcBmp">灰度图</param>
        /// <param name="threshold">对应自适应中的偏移量 C，建议传入 10-20</param>
        /// <param name="shouldInvert">是否反色</param>
        private static Bitmap BinarizeBitmap(Bitmap srcBmp, int threshold, bool shouldInvert)
        {
            int width = srcBmp.Width;
            int height = srcBmp.Height;
            PixelFormat format = srcBmp.PixelFormat;

            BitmapData data = srcBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, format);
            int stride = data.Stride;
            int bytesPerPixel = Image.GetPixelFormatSize(format) / 8;
            byte[] srcPixels = new byte[stride * height];
            byte[] dstPixels = new byte[stride * height];
            Marshal.Copy(data.Scan0, srcPixels, 0, srcPixels.Length);
            srcBmp.UnlockBits(data);

            // 针对 3.1x 放大，窗口必须足够大才能“跳出”边框的干扰
            int blockSize = 71;
            int radius = blockSize / 2;
            int C = threshold;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 边缘保护：距离边界太近的像素直接判为背景 (解决外边框问题)
                    if (x < 5 || x > width - 5 || y < 5 || y > height - 5)
                    {
                        SetPixelWhite(dstPixels, y * stride + x * bytesPerPixel, bytesPerPixel);
                        continue;
                    }

                    long sum = 0;
                    int count = 0;
                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        int ty = y + ky;
                        if (ty < 0 || ty >= height) continue;
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int tx = x + kx;
                            if (tx >= 0 && tx < width)
                            {
                                sum += srcPixels[ty * stride + tx * bytesPerPixel];
                                count++;
                            }
                        }
                    }

                    int avg = (int)(sum / count);
                    int idx = y * stride + x * bytesPerPixel;
                    byte current = srcPixels[idx];

                    bool isText = false;
                    if (avg < 110) // 按钮内
                        isText = current > (avg + C + 5); // 增加额外阈值，防止把边框边缘误判
                    else // 背景区
                        isText = current < (avg - C);

                    byte finalVal = isText ? (byte)0 : (byte)255;
                    if (shouldInvert) finalVal = (byte)(255 - finalVal);

                    dstPixels[idx] = dstPixels[idx + 1] = dstPixels[idx + 2] = finalVal;
                    if (bytesPerPixel == 4) dstPixels[idx + 3] = 255;
                }
            }

            Bitmap resBmp = new Bitmap(width, height, format);
            BitmapData resData = resBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, format);
            Marshal.Copy(dstPixels, 0, resData.Scan0, dstPixels.Length);
            resBmp.UnlockBits(resData);
            return resBmp;
        }

        private static void SetPixelWhite(byte[] data, int index, int bpp)
        {
            data[index] = data[index + 1] = data[index + 2] = 255;
            if (bpp == 4) data[index + 3] = 255;
        }
        private static bool ShouldInvert(Bitmap src)
        {
            long totalBrightness = 0;
            int count = 0;
            // 抽样检查图片的四条边
            for (int x = 0; x < src.Width; x += 5)
            {
                totalBrightness += src.GetPixel(x, 0).R;
                totalBrightness += src.GetPixel(x, src.Height - 1).R;
                count += 2;
            }
            for (int y = 0; y < src.Height; y += 5)
            {
                totalBrightness += src.GetPixel(0, y).R;
                totalBrightness += src.GetPixel(src.Width - 1, y).R;
                count += 2;
            }
            // 如果平均亮度低于 128，说明背景倾向于黑色
            return (totalBrightness / count) < 128;
        }
        /// <summary>
        /// 使用双三次插值将图像放大，提高 OCR 对小字符的识别率
        /// </summary>
        /// <param name="src">源图像</param>
        /// <param name="scale">放大倍数，截图建议 2.0 或 3.0</param>
        private static Bitmap ResizeInterpolation(Bitmap src, float scale)
        {
            if (src.Width >= 250 && src.Height >= 100)
                return src;// 够大了就别再大了，免得二值化太久
            int newWidth = (int)(src.Width * scale);
            int newHeight = (int)(src.Height * scale);

            Bitmap res = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(res))
            {
                // 设置高质量插值模式
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                // 背景填充白色（防止透明截图变黑）
                g.Clear(Color.White);

                g.DrawImage(src, new Rectangle(0, 0, newWidth, newHeight),
                            new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
            }
            return res;
        }
        /// <summary>
        /// 膨胀（加粗笔画，修断笔）
        /// </summary>
        private static Bitmap Dilation(Bitmap srcBmp, byte strength)
        {
            if (strength == 0) return (Bitmap)srcBmp.Clone();

            int width = srcBmp.Width;
            int height = srcBmp.Height;
            PixelFormat format = srcBmp.PixelFormat;

            // 1. 锁定内存
            BitmapData data = srcBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, format);
            int stride = data.Stride;
            int bytesPerPixel = Image.GetPixelFormatSize(format) / 8;
            int totalBytes = stride * height;

            // 2. 将图像数据拷贝到托管数组
            byte[] srcPixels = new byte[totalBytes];
            byte[] dstPixels = new byte[totalBytes];
            Marshal.Copy(data.Scan0, srcPixels, 0, totalBytes);
            srcBmp.UnlockBits(data);

            // 3. 初始化目标数组为全白 (255)
            // 注意：如果是 32位 ARGB，Alpha 通道通常也需要设为 255
            for (int i = 0; i < totalBytes; i++) dstPixels[i] = 255;

            int radius = strength;
            const byte darkThreshold = 128;

            // 4. 遍历处理
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentIdx = y * stride + x * bytesPerPixel;

                    // 如果源像素是黑色（文字）
                    if (srcPixels[currentIdx] < darkThreshold)
                    {
                        // 扩散周围
                        for (int ky = -radius; ky <= radius; ky++)
                        {
                            for (int kx = -radius; kx <= radius; kx++)
                            {
                                int targetX = x + kx;
                                int targetY = y + ky;

                                if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                                {
                                    int targetIdx = targetY * stride + targetX * bytesPerPixel;
                                    // 将 RGB 通道设为黑 (0)
                                    dstPixels[targetIdx] = 0;     // B
                                    dstPixels[targetIdx + 1] = 0; // G
                                    dstPixels[targetIdx + 2] = 0; // R
                                                                  // 如果是 32 位，保持 Alpha 不变或设为 255
                                    if (bytesPerPixel == 4) dstPixels[targetIdx + 3] = 255;
                                }
                            }
                        }
                    }
                }
            }

            // 5. 将处理完的数组写回新的 Bitmap
            Bitmap resBmp = new Bitmap(width, height, format);
            BitmapData resData = resBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, format);
            Marshal.Copy(dstPixels, 0, resData.Scan0, totalBytes);
            resBmp.UnlockBits(resData);

            return resBmp;
        }

        #endregion


        #region Windows 系统 OCR 引擎
        public static async Task<string> RecognizeTextByWinSysOcrAsync(string imagePath)
        {
            // 获取当前输入的语言标签字符串
            string langTag = Windows.Globalization.Language.CurrentInputMethodLanguageTag;

            // 将其转换为 Language 对象
            var lang = new Windows.Globalization.Language(langTag);

            // 进行检查
            if (!OcrEngine.IsLanguageSupported(lang))
            {
                return "当前系统语言暂不支持 OCR";
            }

            // 2. 初始化 OCR 引擎（使用系统当前语言）
            OcrEngine engine = OcrEngine.TryCreateFromUserProfileLanguages();

            // 3. 加载图片文件并解码
            StorageFile file = await StorageFile.GetFileFromPathAsync(imagePath);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync();

                // 4. 执行识别
                OcrResult result = await engine.RecognizeAsync(bitmap);

                // 5. 拼接结果
                return result.Text; // 直接返回完整文本
            }
        }
        #endregion
    }
}
