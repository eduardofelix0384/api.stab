using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace api.stab.Models
{
    public class CacheData
    {
        public CacheData() { }

        public CacheData(string body, string contentType, IDictionary<string, StringValues> headers)
        {
            this.Body = body;
            this.ContentType = contentType;
            this.Headers = new Dictionary<string, string>();
            
            foreach(var item in headers)
            {
                this.Headers.Add(item.Key, item.Value);
            }
        }

        public string Body { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public static CacheData Parse(string text)
        {
            return JsonSerializer.Deserialize<CacheData>(text);
        }
    }
}