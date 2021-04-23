namespace TelegramBot.TestBot.Helpers
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using TelegramBot.TestBot.Models;

    public class RzhunemoguApi
    {
        private static readonly Random Rng = new Random();

        public static async Task<RzhunemoguXml?> DownloadRandomJoke()
        {
            // append random parameter from argument list to the base request string
            string requestUri = AppSettings.RzhunemoguApiBaseUrl + AppSettings.RzhunemoguApiArguments[Rng.Next(AppSettings.RzhunemoguApiArguments.Count)];
            using var response = await ApiHttpClient.Client.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                // register extended encodings
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var encoding = Encoding.GetEncoding("windows-1251");

                using var sr = new StreamReader(await response.Content.ReadAsStreamAsync(), encoding);
                string xml = sr.ReadToEnd();

                // deserialize received xml
                var xmlObj = XmlDeserializeFromString<RzhunemoguXml>(xml);

                return xmlObj;
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
        }

        private static string XmlSerializeToString(object objectInstance)
        {
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            serializer.Serialize(writer, objectInstance);

            return sb.ToString();
        }

        private static T? XmlDeserializeFromString<T>(string objectData)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(objectData);

            return (T?)serializer.Deserialize(reader);
        }
    }
}
