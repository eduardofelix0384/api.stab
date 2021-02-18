using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace api.stab.Tools
{
    public class StreamStorage : IStreamStorage
    {
        IHttpContextAccessor contextAccessor;

        public StreamStorage(IHttpContextAccessor  ctx)
        {
            this.contextAccessor = ctx;
        }

        public async Task Add(string key, Stream stream, string contentType)
        {
            string base64 = null;

            using(var memoryStream =  new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                var buffer = memoryStream.GetBuffer();

                if(String.IsNullOrEmpty(contentType) || contentType.IndexOf("image", StringComparison.InvariantCultureIgnoreCase) == -1)
                    buffer = buffer.Where(_ => _ != (byte)0).ToArray();

                base64 = Convert.ToBase64String(buffer);
            }
            
            this.contextAccessor.HttpContext.Items.Add(key, base64);
        }

        public MemoryStream Get(string key)
        {
            var contextItem = this.contextAccessor.HttpContext.Items[key];
            
            if(contextItem == null)
                return null;

            var base64 = contextItem.ToString();
            var buffer = Convert.FromBase64String(base64);
            return new MemoryStream(buffer);
        }

        public string GetBase64(string key)
        {
            var contextItem = this.contextAccessor.HttpContext.Items[key];
            
            if(contextItem == null)
                return null;

            return contextItem.ToString();
        }
    }

    public interface IStreamStorage
    {
        Task Add(string key, Stream stream, string contentType);
        MemoryStream Get(string key);
        string GetBase64(string key);
    }
}