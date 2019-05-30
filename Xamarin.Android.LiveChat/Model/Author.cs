using Newtonsoft.Json;

namespace Xamarin.Android.LiveChat.Model
{
    public class Author
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        public override string ToString()
        {
            return "Author{" +
                "name='" + Name + '\'' +
                '}';
        }
    }
}