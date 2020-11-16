namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class LoremPicsumApi
    {
        public static async Task<string> DownloadRandomImage()
        {
            using var response = await ApiHttpClient.Client.GetAsync(AppSettings.LoremPicsumApiBaseUrl);

            if (response.IsSuccessStatusCode)
            {
                string? fileName = response.Content.Headers.ContentDisposition?.FileName?.Replace("\"", string.Empty);

                if (fileName is null)
                {
                    throw new NullReferenceException(nameof(fileName));
                }

                string filePath = Path.Combine(AppSettings.PicsDirectory, fileName);
                using var sr = await response.Content.ReadAsStreamAsync();

                // save received picture file if its new
                if (!File.Exists(filePath))
                {
                    using var fs = File.Create(filePath);
                    sr.Seek(0, SeekOrigin.Begin);
                    sr.CopyTo(fs);
                }

                // return exist image path
                return filePath;
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
        }
    }
}
