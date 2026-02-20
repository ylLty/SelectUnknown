using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SelectUnknown.Lens
{
    internal class LitterboxUploader
    {
        /// <summary>
        /// 上传图片到 Litterbox 并获取返回的 URL
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static async Task<string> SendImageToLitterboxAndGetUrl(Bitmap bitmap)
        {
            // 第一步：尝试跟随系统（可能走代理，也可能裸连，取决于系统设置）
            // 这里的 false 表示不强制禁用代理
            string result = await ExecuteUpload(bitmap, useProxy: true);

            // 第二步：异常重试逻辑
            // 如果第一次失败是因为“积极拒绝”（代理残留），则强制裸连再试一次
            if (result == "PROXY_REJECTED")
            {
                LogManagement.LogHelper.Log("检测到代理端口拒绝连接，正在尝试强制直连模式...", LogManagement.LogLevel.Warn);
                result = await ExecuteUpload(bitmap, useProxy: false);
            }

            return result;
        }

        // 这里是你原本的代码逻辑，封装进一个支持开关代理的方法里
        private static async Task<string> ExecuteUpload(Bitmap bitmap, bool useProxy)
        {
            const string ApiUrl = "https://litterbox.catbox.moe/resources/internals/api.php";

            // 根据参数决定是否信任系统代理
            var handler = new HttpClientHandler { UseProxy = useProxy };
            if (!useProxy) handler.Proxy = null;

            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(20); // 设置超时，防止代理死掉时卡死
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.Add("Origin", "https://litterbox.catbox.moe");

                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    byte[] bitmapData = memoryStream.ToArray();

                    var boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
                    using (var form = new MultipartFormDataContent(boundary))
                    {
                        // --- 以下是你完全原有的构建逻辑，没有任何修改 ---
                        var reqtypeContent = new StringContent("fileupload");
                        reqtypeContent.Headers.Remove("Content-Disposition");
                        reqtypeContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"reqtype\"");
                        form.Add(reqtypeContent);

                        var timeContent = new StringContent("1h");
                        timeContent.Headers.Remove("Content-Disposition");
                        timeContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"time\"");
                        form.Add(timeContent);

                        var fileContent = new ByteArrayContent(bitmapData);
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                        fileContent.Headers.Remove("Content-Disposition");
                        fileContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"fileToUpload\"; filename=\"snip.png\"");
                        form.Add(fileContent);

                        form.Headers.ContentType.Parameters.Clear();
                        form.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("boundary", boundary));

                        try
                        {
                            var response = await client.PostAsync(ApiUrl, form);
                            string result = await response.Content.ReadAsStringAsync();
                            LogManagement.LogHelper.Log($"Litterbox 上传成功", LogManagement.LogLevel.Info);
                            return response.IsSuccessStatusCode ? result.Trim() : null;
                        }
                        catch (Exception ex)
                        {
                            // 关键点：判断是否为代理导致的“积极拒绝”
                            if (ex.Message.Contains("积极拒绝") || ex.InnerException?.Message.Contains("积极拒绝") == true)
                            {
                                return "PROXY_REJECTED"; // 返回特殊标识，触发外层重试
                            }

                            LogManagement.LogHelper.Log("Litterbox 上传异常：" + ex.Message, LogManagement.LogLevel.Error);
                            return null;
                        }
                    }
                }
            }
        }
    }
}
