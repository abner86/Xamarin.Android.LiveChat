using Newtonsoft.Json;

namespace Xamarin.Android.LiveChat.Model
{
    public class NewMessage
    {
        [JsonProperty("messageType")]
        public string MessageType { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("timestamp")]
        public string TimeStamp { get; set; }
        [JsonProperty("author")]
        public Author Author { get; set; }

        public override string ToString()
        {
            return "NewMessageModel{" +
                    "messageType='" + MessageType + '\'' +
                    ", text='" + Text + '\'' +
                    ", id='" + Id + '\'' +
                    ", timestamp='" + TimeStamp + '\'' +
                    ", author=" + Author +
                    '}';
        }
    }
}