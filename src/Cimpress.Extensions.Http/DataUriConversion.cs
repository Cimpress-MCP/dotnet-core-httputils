using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Cimpress.Extensions.Http
{
    public static class DataUriConversion
    {
        /// <summary>
        /// Downloads an image at the provided URL and converts it to a valid Data Uri scheme (https://en.wikipedia.org/wiki/Data_URI_scheme)
        /// </summary>
        /// <param name="url">The url where the image is located.</param>
        /// <param name="logger">A logger.</param>
        /// <param name="fileInfo">A FileInfo to retrieve the local fallback image.</param>
        /// <param name="messageHandler">An optional message handler.</param>
        /// <returns>A string that contains the data uri of the downloaded image, or a default image on any error.</returns>
        public static async Task<string> DownloadImageAndConvertToDataUri(this string url, ILogger logger, IFileInfo fileInfo, HttpMessageHandler messageHandler)
        {
            // exclude completely invalid URLs
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    // set a timeout to 10 seconds to avoid waiting on that forever
                    using (var client = new HttpClient(messageHandler) { Timeout = TimeSpan.FromSeconds(10) })
                    {
                        var response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        // set the media type and default to JPG if it wasn't provided
                        string mediaType = response.Content.Headers.ContentType?.MediaType;
                        mediaType = string.IsNullOrWhiteSpace(mediaType) ? "image/jpeg" : mediaType;

                        // return the data URI according to the standard
                        return (await response.Content.ReadAsByteArrayAsync()).ToDataUri(mediaType);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(0, ex, "Error while downloading resource.");
                }
            }

            // any error or invalid URLs just return the default data uri
            return await DefaultPreviewToDataUri(fileInfo);
        }

        private static async Task<string> DefaultPreviewToDataUri(IFileInfo fileInfo)
        {
            // copy to memory stream and convert the bytes to a base64 encoded string
            using (var ms = new MemoryStream())
            {
                using (var stream = fileInfo.CreateReadStream())
                {
                    await stream.CopyToAsync(ms);
                }
                return ms.ToArray().ToDataUri("image/png");
            }
        }

        private static string ToDataUri(this byte[] data, string mediaType)
        {
            return $"data:{mediaType};base64,{Convert.ToBase64String(data)}";
        }
    }
}