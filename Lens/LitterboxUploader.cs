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
        public static async Task<string> SendImageToLitterboxAndGetUrl(Bitmap bitmap)// 特别鸣谢 Gemini , 同样的问题，让ChatGPT解决了一下午都未能解决（一直400），在 Gemini 这边三句话就完了
        {
            const string ApiUrl = "https://litterbox.catbox.moe/resources/internals/api.php";

            using (var client = new HttpClient())
            {
                // 1. 设置更真实的浏览器 Headers
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                // 有些防火墙会检查 Origin
                client.DefaultRequestHeaders.Add("Origin", "https://litterbox.catbox.moe");

                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    byte[] bitmapData = memoryStream.ToArray();

                    // 2. 这里的 Boundary 必须干净，不要带引号
                    var boundary = "----WebKitFormBoundary" + DateTime.Now.Ticks.ToString("x");
                    using (var form = new MultipartFormDataContent(boundary))
                    {
                        // 关键点：BunkerWeb 有时会拦截不带双引号的 name 属性
                        // 我们通过重写 ContentDisposition 确保格式严丝合缝

                        // reqtype 字段
                        var reqtypeContent = new StringContent("fileupload");
                        reqtypeContent.Headers.Remove("Content-Disposition");
                        reqtypeContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"reqtype\"");
                        form.Add(reqtypeContent);

                        // time 字段
                        var timeContent = new StringContent("1h");
                        timeContent.Headers.Remove("Content-Disposition");
                        timeContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"time\"");
                        form.Add(timeContent);

                        // fileToUpload 字段
                        var fileContent = new ByteArrayContent(bitmapData);
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                        fileContent.Headers.Remove("Content-Disposition");
                        fileContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"fileToUpload\"; filename=\"snip.png\"");
                        form.Add(fileContent);

                        // 3. 移除特殊的 Content-Type 限制
                        // 强制让 Multipart 的 Header 看起来更标准
                        form.Headers.ContentType.Parameters.Clear();
                        form.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("boundary", boundary));

                        try
                        {
                            var response = await client.PostAsync(ApiUrl, form);
                            string result = await response.Content.ReadAsStringAsync();

                            if (response.IsSuccessStatusCode)
                            {
                                return result.Trim();
                            }
                            else
                            {
                                // 如果还是 400，可以观察下 result 是否变了
                                return null;
                            }
                        }
                        catch (Exception ex)
                        {
                            return null;
                        }
                    }
                }
            }
        }
    }
}
