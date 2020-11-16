namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using TelegramBot.TestBot.Models;

    public class RzhunemoguApi
    {
        private static readonly Random Rnd = new Random();

        public static async Task<RzhunemoguXml?> DownloadRandomJoke()
        {
            RzhunemoguXml? xmlObj;
            int argument = AppSettings.RzhunemoguApiArgumentsList[Rnd.Next(AppSettings.RzhunemoguApiArgumentsList.Count)];
            string requestUri = AppSettings.RzhunemoguApiBaseUrl + argument;
            using var response = await ApiHttpClient.Client.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                // register extended encodings
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var encoding = Encoding.GetEncoding("windows-1251");

                using var sr = new StreamReader(await response.Content.ReadAsStreamAsync(), encoding);
                string xml = sr.ReadToEnd();

                // deserialize received xml
                xmlObj = XmlUtils.XmlDeserializeFromString<RzhunemoguXml>(xml);

                return xmlObj;
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
        }
    }
}
