using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ZXing;
using ZXing.Common;

namespace SelectUnknown.Lens
{
    public class CodeResult
    {
        public string Text { get; init; }
        public ZXing.BarcodeFormat Format { get; init; }

        /// <summary>
        /// 码在 Bitmap 中的中心点（像素坐标）
        /// </summary>
        public System.Drawing.PointF Center { get; init; }
    }

    public static class QrcodeHelper
    {
        private static readonly BarcodeReaderGeneric _reader = new BarcodeReaderGeneric
        {
            AutoRotate = false, // 禁用自带旋转以保证坐标可控
            TryInverted = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat>
                {
                    BarcodeFormat.QR_CODE,
                    BarcodeFormat.CODE_128,
                    BarcodeFormat.CODE_39,
                    BarcodeFormat.EAN_13,
                    BarcodeFormat.EAN_8,
                    BarcodeFormat.ITF,
                    BarcodeFormat.DATA_MATRIX,
                    BarcodeFormat.PDF_417
                }
            }
        };

        /// <summary>
        /// 从 Bitmap 中识别所有二维码 / 条形码，并返回内容及中心点
        /// 逻辑：循环旋转 0°, 90°, 180°, 270° 尝试识别并还原坐标
        /// </summary>
        public static IReadOnlyList<CodeResult> Decode(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            int originalWidth = bitmap.Width;
            int originalHeight = bitmap.Height;

            // 克隆一份副本进行旋转操作，不影响原始图片
            using (Bitmap workingBitmap = (Bitmap)bitmap.Clone())
            {
                // 尝试四个角度：0, 90, 180, 270
                for (int i = 0; i < 4; i++)
                {
                    int currentAngle = i * 90;
                    Result[] results = null;

                    try
                    {
                        results = _reader.DecodeMultiple(workingBitmap);
                    }
                    catch
                    {
                        // 发生异常则继续下一个角度
                    }

                    if (results != null && results.Length > 0)
                    {
                        List<CodeResult> list = new();
                        foreach (var r in results)
                        {
                            var points = r.ResultPoints;
                            if (points == null || points.Length == 0) continue;

                            // 1. 计算当前旋转状态下的中心点
                            float minX = points.Min(p => p.X);
                            float minY = points.Min(p => p.Y);
                            float maxX = points.Max(p => p.X);
                            float maxY = points.Max(p => p.Y);

                            float currentCenterX = (minX + maxX) / 2f;
                            float currentCenterY = (minY + maxY) / 2f;

                            // 2. 将中心点坐标还原回原始图像坐标系
                            PointF restoredCenter = RestorePoint(currentCenterX, currentCenterY, currentAngle, originalWidth, originalHeight);

                            list.Add(new CodeResult
                            {
                                Text = r.Text,
                                Format = r.BarcodeFormat,
                                Center = restoredCenter
                            });
                        }
                        // 只要在某个角度识别成功，就返回结果（避免重复识别）
                        return list;
                    }

                    // 如果没识别到，将图片顺时针旋转 90 度准备下次循环
                    if (i < 3)
                    {
                        workingBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    }
                }
            }

            return Array.Empty<CodeResult>();
        }

        /// <summary>
        /// 坐标还原算法：将旋转后的坐标 $(x', y')$ 映射回原图坐标 $(x, y)$
        /// </summary>
        private static PointF RestorePoint(float x, float y, int angle, int origW, int origH)
        {
            return angle switch
            {
                90 => new PointF(y, origH - x),        // 顺时针 90° 还原
                180 => new PointF(origW - x, origH - y), // 180° 还原
                270 => new PointF(origW - y, x),        // 顺时针 270° 还原
                _ => new PointF(x, y)                   // 0° 保持不变
            };
        }
    }
}