using PaddleOCRJson;
using SelectUnknown.ConfigManagment;
using SelectUnknown.LogManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using OcrEngine = Windows.Media.Ocr.OcrEngine;

namespace SelectUnknown.Lens
{
    internal class OCRHelper
    {
        public static PaddleOCRJson.OcrEngine engine;
        public static PaddleOCRJson.OcrClient client;
        public static bool IsPaddleOcrEngineReady { get; private set; } = false;
        public static string DownloadUrl = "";
        public static async Task InitOcr()
        {
            // 先检查下载地址
            var handler = new HttpClientHandler { UseProxy = false };

            HttpClient _client = new HttpClient(handler);
            string url = "https://gitee.com/ylLty/ylLtyStaticRes/raw/main/SelectUnknown/DownloadUrl.txt";

            // UA 标识
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; SelectUnknown/1.0)"
            );
            try
            {
                // 发送 GET 请求并获取字符串内容
                DownloadUrl = await _client.GetStringAsync(url);
            }
            catch (HttpRequestException ex)
            {
                LogHelper.Log($"获取下载地址失败: {ex}", LogLevel.Warn);
                return;
            }
            //====
            if (Config.OcrEngineName != "PaddleOCR-json")
            {
                if (IsPaddleOcrEngineReady)
                { 
                    engine.Dispose();
                    client.Dispose();
                    IsPaddleOcrEngineReady = false;
                }
                LogHelper.Log("使用系统内置 OCR 引擎, 无需初始化");
                return;
            }
            string enginePath = GetOcrEnginePath();
            Directory.CreateDirectory(Path.GetDirectoryName(enginePath));
            if (File.Exists(enginePath))
            {
                LogHelper.Log($"PaddleOCR-json 引擎路径: {enginePath}");
            }
            else
            {
                LogHelper.Log($"未找到 PaddleOCR-json 引擎，路径: {enginePath}", LogManagement.LogLevel.Error);
                throw new FileNotFoundException("未找到 PaddleOCR-json 引擎", enginePath);
            }
            LogHelper.Log("正在启动 PaddleOCR-json 引擎...");

            // 创建一个控制台
            AllocConsole();

            // 立刻隐藏
            var hConsole = GetConsoleWindow();
            ShowWindow(hConsole, SW_HIDE);

            var startupArgs = OcrEngineStartupArgs.WithPipeMode(enginePath);

            engine = new PaddleOCRJson.OcrEngine(startupArgs);
            client = engine.CreateClient();

            LogHelper.Log("PaddleOCR-json 引擎启动成功");
            IsPaddleOcrEngineReady = true;
        }
        
        public static async Task<string> RecognizeAsync(Bitmap bitmap)
        {
            string tmpFolderPath;
            string tmpPath;
            string txt;
            if (Config.OcrEngineName == "PaddleOCR-json")
            {
                // PaddleOCR-json 引擎逻辑
                tmpFolderPath = Path.Combine(Path.GetTempPath(), "SelectUnknown", "tmp");
                Directory.CreateDirectory(tmpFolderPath);
                tmpPath = Path.Combine(tmpFolderPath, $"ocr_temp_{Guid.NewGuid()}.png");
                bitmap.Save(tmpPath, System.Drawing.Imaging.ImageFormat.Png);

                txt = await PaddleOcrRecognizeAsync(client, tmpPath);
                txt = ReadJsonForPaddleOCR(txt);
                // 清理临时文件
                if (File.Exists(tmpPath))
                {
                    try
                    {
                        File.Delete(tmpPath);
                    }
                    catch { /* 忽略删除失败 */ }
                }
                return txt;
            }
            // 系统内置 OCR 逻辑
            bitmap = await OptimizeBitmapForOcr(bitmap);
            //Clipboard.SetImage(bitmap);
            tmpFolderPath = Path.Combine(Path.GetTempPath(), "SelectUnknown", "tmp");
            Directory.CreateDirectory(tmpFolderPath);
            tmpPath = Path.Combine(tmpFolderPath, $"ocr_temp_{Guid.NewGuid()}.png");
            bitmap.Save(tmpPath, System.Drawing.Imaging.ImageFormat.Png);
            
            txt = await RecognizeTextByWinSysOcrAsync(tmpPath);
            
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
        public static async Task<Bitmap> OptimizeBitmapForOcr(Bitmap src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            Bitmap bmp = ToGrayscaleBitmap(src);// 灰度化
            //Clipboard.SetImage(bmp);
            bool invert = ShouldInvert(bmp); // 自动判断是否需要反色

            bmp = ResizeInterpolation(bmp, 2.7f);// 放大
            //Clipboard.SetImage(bmp);

            bmp = Blur(bmp, 1);// 轻微模糊，减少噪点
            //Clipboard.SetImage(bmp);
            bmp.Save(Path.Combine(Path.GetTempPath(), "blur.png"));

            bmp = await BinarizeBitmap(bmp, 140);// 二值化
            //Clipboard.SetImage(bmp);

            if (invert)
                bmp = InvertBitmap(bmp);// 反转颜色

            bmp = PadBitmapToMinSize(bmp, 128);// 填充到最小尺寸
            //Clipboard.SetImage(bmp);

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
                // 将背景涂色
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
        public static Bitmap ToGrayscaleBitmap(Bitmap src)
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
        private static Bitmap Blur(Bitmap sourceBitmap, int radius)
        {
            if (radius <= 0) return (Bitmap)sourceBitmap.Clone();

            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // 1. 锁定内存，准备字节数组
            BitmapData srcData = sourceBitmap.LockBits(new Rectangle(0, 0, width, height),
                                                      ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int bytes = srcData.Stride * height;
            byte[] srcBuffer = new byte[bytes];
            byte[] dstBuffer = new byte[bytes];
            Marshal.Copy(srcData.Scan0, srcBuffer, 0, bytes);
            sourceBitmap.UnlockBits(srcData);

            // 2. 核心算法：带边缘钳位的均值模糊
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 如果是在最边缘像素，完全不处理，保留原色
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    {
                        CopyPixel(srcBuffer, dstBuffer, x, y, srcData.Stride);
                        continue;
                    }

                    long r = 0, g = 0, b = 0, a = 0;
                    int count = 0;

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        // 像素钳位 (Clamping)：采样超出边界时，取边界像素值
                        int py = Math.Max(0, Math.Min(height - 1, y + ky));

                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int px = Math.Max(0, Math.Min(width - 1, x + kx));

                            int offset = (py * srcData.Stride) + (px * 4);
                            b += srcBuffer[offset];
                            g += srcBuffer[offset + 1];
                            r += srcBuffer[offset + 2];
                            a += srcBuffer[offset + 3];
                            count++;
                        }
                    }

                    // 计算均值并写回
                    int destOffset = (y * srcData.Stride) + (x * 4);
                    dstBuffer[destOffset] = (byte)(b / count);
                    dstBuffer[destOffset + 1] = (byte)(g / count);
                    dstBuffer[destOffset + 2] = (byte)(r / count);
                    dstBuffer[destOffset + 3] = (byte)(a / count);
                }
            }

            // 3. 将处理好的字节存回 Bitmap
            Bitmap resultBitmap = new Bitmap(width, height);
            BitmapData dstData = resultBitmap.LockBits(new Rectangle(0, 0, width, height),
                                                      ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(dstBuffer, 0, dstData.Scan0, bytes);
            resultBitmap.UnlockBits(dstData);

            return resultBitmap;
        }

        private static void CopyPixel(byte[] src, byte[] dst, int x, int y, int stride)
        {
            int offset = (y * stride) + (x * 4);
            dst[offset] = src[offset];
            dst[offset + 1] = src[offset + 1];
            dst[offset + 2] = src[offset + 2];
            dst[offset + 3] = src[offset + 3];
        }
        /// <summary>
        /// 二值化 (非 unsafe 版)
        /// </summary>
        private static async Task<Bitmap> BinarizeBitmap(Bitmap original, byte threshold)
        {
            Bitmap newBmp = new Bitmap(original.Width, original.Height);
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color c = original.GetPixel(x, y);
                    // 计算灰度值
                    int gray = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                    // 根据阈值设为黑或白
                    newBmp.SetPixel(x, y, gray >= threshold ? Color.White : Color.Black);
                }
            }
            return newBmp;
        }

        private static Bitmap InvertBitmap(Bitmap source)
        {
            Bitmap result = new Bitmap(source.Width, source.Height);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color c = source.GetPixel(x, y);
                    // 反色逻辑：255 减去当前通道值，保持 Alpha 通道不变
                    Color inverted = Color.FromArgb(c.A, 255 - c.R, 255 - c.G, 255 - c.B);
                    result.SetPixel(x, y, inverted);
                }
            }
            return result;
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
        #region PaddleOCR-json 引擎
        public static string GetOcrEnginePath()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PaddleOCR-json", "PaddleOCR-json.exe");
            return path;
        }
        public static async Task<string> PaddleOcrRecognizeAsync(OcrClient client, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("无效的路径", imagePath);
            }

            var startTime = DateTime.Now;
            try
            {
                var result = client.FromImageFile(imagePath);
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                LogHelper.Log($"识别完成 (耗时: {elapsed:F2}ms)");
                return result;
            }
            catch (Exception ex)
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                LogHelper.Log($"识别失败 (耗时: {elapsed:F2}ms): {ex.Message}");
                return "";
            }
        }
        public static string? ReadJsonForPaddleOCR(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                // 检查是否存在code属性
                if (!root.TryGetProperty("code", out JsonElement codeElement))
                    return null;

                // 检查code是否为100
                if (codeElement.GetInt32() != 100)
                {
                    LogHelper.Log($"PaddleOCR-json 识别失败，code: {codeElement.GetInt32()}", LogLevel.Error);
                    return null;
                }

                // 检查是否存在data属性
                if (!root.TryGetProperty("data", out JsonElement dataElement))
                    return null;

                // 检查data是否为数组且长度大于0
                if (dataElement.ValueKind != JsonValueKind.Array || dataElement.GetArrayLength() == 0)
                    return null;

                // 获取第一个元素
                JsonElement firstItem = dataElement[0];

                // 检查是否存在text属性
                if (!firstItem.TryGetProperty("text", out JsonElement textElement))
                    return null;

                // 获取text值
                return textElement.GetString();
            }
            catch (JsonException)
            {
                // JSON格式错误
                return null;
            }
            catch (Exception)
            {
                // 其他异常
                return null;
            }
        }
        #region 隐藏控制台用
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        #endregion
        #endregion
    }
}
