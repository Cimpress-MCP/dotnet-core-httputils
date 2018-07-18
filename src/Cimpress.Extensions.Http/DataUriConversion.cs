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
        /// <param name="fallbackFileInfo">A FileInfo to retrieve the local fallback image.</param>
        /// <param name="fallbackMediaType">The media type of the fallback image.</param>
        /// <param name="messageHandler">An optional message handler.</param>
        /// <returns>A string that contains the data uri of the downloaded image, or a default image on any error.</returns>
        public static async Task<string> DownloadImageAndConvertToDataUri(this string url, ILogger logger, IFileInfo fallbackFileInfo, string fallbackMediaType = "image/png", HttpMessageHandler messageHandler = null)
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
                    logger.LogInformation(0, ex, "Error while downloading resource at {URL}", url);
                }
            }

            // any error or invalid URLs just return the default data uri
            return await fallbackFileInfo.ToDataUri(fallbackMediaType);
        }

        /// <summary>
        /// Converts a file info to a data URL.
        /// </summary>
        /// <param name="fileInfo">The fileInfo that points to a file which content should be converted to a Data URI format.</param>
        /// <param name="mediaType">The media type of the file.</param>
        /// <returns>A string that contains the data uri of the provided file content.</returns>
        public static Task<string> ToDataUri(this IFileInfo fileInfo, string mediaType)
        {
            return fileInfo.CreateReadStream().ToDataUri(mediaType);
        }

        /// <summary>
        /// Converts a stream to a data URL.
        /// </summary>
        /// <param name="stream">The stream which provides the content for the data URI.</param>
        /// <param name="mediaType">The media type of the stream.</param>
        /// <returns>A string that contains the data uri of the stream's content.</returns>
        public static async Task<string> ToDataUri(this Stream stream, string mediaType)
        {
            // copy to memory stream and convert the bytes to a base64 encoded string
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                return ms.ToArray().ToDataUri(mediaType);
            }
        }

        /// <summary>
        /// Converts a byte array to a data URL.
        /// </summary>
        /// <param name="data">The data that should be converted to base64 and included in the data URI.</param>
        /// <param name="mediaType">The media type of the content.</param>
        /// <returns>A string that contains the data uri of the provided data.</returns>
        public static string ToDataUri(this byte[] data, string mediaType)
        {
            return Convert.ToBase64String(data).ToDataUri(mediaType);
        }

        /// <summary>
        /// Converts a byte array to a data URL.
        /// </summary>
        /// <param name="base64String">The base64 encoded string that represents the data.</param>
        /// <param name="mediaType">The media type of the content.</param>
        /// <returns>A string that contains the data uri of the provided data.</returns>
        public static string ToDataUri(this string base64String, string mediaType)
        {
            return $"data:{mediaType};base64,{base64String}";
        }
    }
}