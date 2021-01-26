namespace TelegramBot.TestBot.Helpers
{
    using System.IO;
    using System.Text;
    using System.Xml.Serialization;

    public static class XmlUtils
    {
        public static string XmlSerializeToString(object objectInstance)
        {
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            serializer.Serialize(writer, objectInstance);

            return sb.ToString();
        }

        public static T? XmlDeserializeFromString<T>(string objectData)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(objectData);

            return (T)serializer.Deserialize(reader);
        }
    }
}
