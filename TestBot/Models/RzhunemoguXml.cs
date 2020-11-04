namespace TelegramBot.TestBot.Models
{
    using System.Xml.Serialization;

    [XmlRoot("root")]
    public class RzhunemoguXml
    {
        [XmlElement("content")]
        public string Content { get; set; } = string.Empty;
    }
}
