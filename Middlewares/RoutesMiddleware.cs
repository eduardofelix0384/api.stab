using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text.Json;
using api.stab.Models;
using api.stab.Repository;
using api.stab.Tools;
using MedCore_Router.Services;

namespace api.stab.Middlewares
{
    public class RoutesMiddleware
    {
        RequestDelegate _next;
        IStreamStorage streamStorage;
        IRequestTraceService traceService;
        IList<ApiUrl> apiUrls;

        public RoutesMiddleware(RequestDelegate next, IStreamStorage storage, IRequestTraceService service)
        { 
            _next = next;
            streamStorage = storage;
            traceService = service;
        }

        List<string> headersToIgnore = new List<string>()
        {
            "Transfer-Encoding"
        };

        public string CurrentUrl { get; set; }

        public async Task InvokeAsync(HttpContext context)
        {
            Log.Register($@"> ApiRouterMiddleware
###########################################
{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");

            try
            {
                apiUrls = ApiUrlsRepository.GetUrls();
                var routes = RoutesRepository.GetRoutes();
                var url = context.Request.GetEncodedUrl();
                this.CurrentUrl = url;
                string cacheKey = null;
                bool enableCache = false;

                int ttl = 0;

                if (url.Last() == '/')
                    url = url.Substring(0, url.Length - 1);

                var targetBaseUrl = this.apiUrls.FirstOrDefault(_ => _.Key == "Default").Url;

                foreach(var route in routes)
                {
                    if (route.Pattern.Last() == '/')
                        route.Pattern = route.Pattern.Substring(0, route.Pattern.Length - 1);

                    if (route.Pattern.Last() == '\\')
                        route.Pattern = route.Pattern.Substring(0, route.Pattern.Length - 1);

                    route.Pattern = route.Pattern.ToLower();
                    route.Pattern = route.Pattern.Replace(@"\\", @"\");

                    if (Regex.Match(url.ToLower(), route.Pattern).Success)
                    {
                        if(!String.IsNullOrEmpty(route.BaseUrlKey))
                        {
                            var apiUrl = this.apiUrls.FirstOrDefault(_ => _.Key == route.BaseUrlKey);

                            if(apiUrl != null)
                                targetBaseUrl = apiUrl.Url;
                        }

                        enableCache = route.EnableCache;
                        ttl = route.TTL;

                        if(!enableCache)
                        {
                            var globalCacheConfig = await GlobalCacheConfigRepository.GetCurrent();

                            if(globalCacheConfig != null && globalCacheConfig.Enabled)
                            {
                                var method = context.Request.Method.ToUpper();

                                if(method == "GET" || (route.Cacheable.HasValue && route.Cacheable.Value))
                                {
                                    enableCache = true;
                                    ttl = globalCacheConfig.TTL;
                                }
                            }
                        }
                        break;
                    }
                }

                var finalUrl = CreateUrl(url, targetBaseUrl, true);

                Log.Register($@"Router BaserUrl: {this.RouterBaseUrl}
Url Original: {url}
Url Final: {finalUrl}");

                if(finalUrl.IndexOf(targetBaseUrl, StringComparison.InvariantCultureIgnoreCase) == -1)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;

                Log.Register($@"
Url não pode ser resolvida
###########################################
");
                    
                    context.Items.Add(Constants.TARGET_URL, finalUrl);
                    await _next.Invoke(context);
                    return;
                }
                
                bool cacheData = false;

                if(enableCache)
                {
                    Log.Register("Cache habilitado");
                    var cacheResult = GetDataByCache(url, context);
                    
                    if(cacheResult != null)
                    {
                        var resultStream = (Stream)cacheResult.resultStream;
                        cacheData = (resultStream != null);
                        cacheKey = (string)cacheResult.cacheKey;

                        if(cacheData)
                        {
                            await streamStorage.Add(Constants.STREAM_RESPONSE, resultStream, context.Response.ContentType);
                            context.Items.Add(Constants.CACHE_KEY, cacheKey);
                            context.Items.Add(Constants.TARGET_URL, finalUrl);
                        }
                    }
                }

                if(context.Response.Headers.Keys.Contains("Track-RouterCache"))
                    context.Response.Headers["Track-RouterCache"] = cacheData.ToString();
                else
                    context.Response.Headers.Add("Track-RouterCache", cacheData.ToString());

                if(context.Response.Headers.Keys.Contains("Track-ApiRouterServer"))
                    context.Response.Headers["Track-ApiRouterServer"] = Environment.MachineName;
                else    
                    context.Response.Headers.Add("Track-ApiRouterServer", Environment.MachineName);

                if(!cacheData)
                {
                    HttpWebResponse response = await ExecuteRequest(finalUrl, context);
                    
                    if(response == null || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.MethodNotAllowed)
                    {
                        Log.Register("Falha no request");
                        Log.Register("Refazendo request sem /");
                        
                        finalUrl = CreateUrl(url, targetBaseUrl, false);
                        response = await ExecuteRequest(finalUrl, context);
                    }

                    context.Items.Add(Constants.TARGET_URL, finalUrl);

                    if(response == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        await _next.Invoke(context);
                        return;
                    }

                    foreach(var headerKey in response.Headers.AllKeys)
                    {
                        if(String.IsNullOrEmpty(headersToIgnore.FirstOrDefault(h => h == headerKey)))
                        {
                            if(context.Response.Headers.ContainsKey(headerKey))
                                context.Response.Headers[headerKey] = response.Headers[headerKey];
                            else
                                context.Response.Headers.Add(headerKey, response.Headers[headerKey]);
                        }
                    }

                    context.Response.ContentType = response.ContentType;
                    context.Response.StatusCode = (int)response.StatusCode;
                    
                    using(var resultStream = response.GetResponseStream())
                    {
                        await streamStorage.Add(Constants.STREAM_RESPONSE, resultStream, response.ContentType);
                    }

                    Log.Register("Request feito para url final.");
                }

                using(var rs = streamStorage.Get(Constants.STREAM_RESPONSE))
                {
                    context.Response.ContentLength = rs.Length;
                    var responseStream = context.Response.Body;
                    await rs.CopyToAsync(responseStream);
                    responseStream.Close();
                }

                if(context.Response.StatusCode == 200 && enableCache && !cacheData)
                {
                    string body = streamStorage.GetBase64(Constants.STREAM_RESPONSE);
                    var newCacheData = new CacheData(body, context.Response.ContentType, context.Response.Headers);

                    RedisCacheManager.SetItemString(cacheKey, newCacheData.ToString(), new TimeSpan(0,0, ttl));
                    Log.Register($"Cache alimentado com a seguinte chave: \"{cacheKey}\"");
                }

                Log.Register($@"StatusCode: {context.Response.StatusCode}
{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}
###########################################");
                
                await _next.Invoke(context);
            }
            catch(Exception ex)
            {
                traceService.Insert(ex);
                throw;
            }
        }

        async Task<HttpWebResponse> ExecuteRequest(string url, HttpContext context)
        {
            for(var i = 0; i < this.Retry502Limit; i++)
            {
                var request = GetRequest(url, context);
                HttpWebResponse response = null;

                var method = context.Request.Method.ToUpper();

                if(method != "GET" && method != "HEAD")
                {
                    var requestStream = request.GetRequestStream();

                    using(var rs = streamStorage.Get(Constants.STREAM_REQUEST))
                    {
                        await rs.CopyToAsync(requestStream);
                    }
                    
                    requestStream.Close();
                }

                try
                {
                    response = (HttpWebResponse)(await request.GetResponseAsync());
                }
                catch(WebException ex)
                {
                    if(ex.Response != null)
                        response = (HttpWebResponse)ex.Response;
                }

                if(response != null && response.StatusCode == HttpStatusCode.BadGateway)                        
                    Log.Register("Tentando novamente por causa de status 502.");
                else
                    return response;
            }

            return null;
        }

        dynamic GetDataByCache(string url, HttpContext context)
        {
            Stream resultStream = null;
            string cacheKey = GetCacheKey(url, context);

            var contentLength = 0;
            var cachedData = RedisCacheManager.GetItemString(cacheKey);

            if(!String.IsNullOrEmpty(cachedData))
            {
                CacheData parsedCacheData = CacheData.Parse(cachedData);
                var cacheBuffer = Convert.FromBase64String(parsedCacheData.Body);
                resultStream = new MemoryStream(cacheBuffer);

                foreach(var headerKey in parsedCacheData.Headers.Keys)
                {
                    if(String.IsNullOrEmpty(headersToIgnore.FirstOrDefault(h => h == headerKey)))
                    {
                        if(context.Response.Headers.ContainsKey(headerKey))
                            context.Response.Headers[headerKey] = parsedCacheData.Headers[headerKey];
                        else
                            context.Response.Headers.Add(headerKey, parsedCacheData.Headers[headerKey]);
                    }
                }

                context.Response.ContentLength = cacheBuffer.Length;
                context.Response.ContentType = parsedCacheData.ContentType;
                context.Response.StatusCode = 200;
                contentLength = cacheBuffer.Length;
                
                Log.Register("Dado resgatado do cache.");
            }

            return new { contentLength, resultStream, cacheKey };
        }

        string CreateUrl(string currentUrl, string targetBaseUrl, bool slash)
        {
            if(currentUrl.Length < this.RouterBaseUrl.Length)
                currentUrl = this.RouterBaseUrl;
                
            var baseUrl = currentUrl.Substring(0, this.RouterBaseUrl.Length);
            currentUrl = Regex.Replace(currentUrl, @"/.[^/]+-x-.[^/]+/", "/");
            var url = currentUrl.Replace(baseUrl, targetBaseUrl.ToLower());

            if(slash)
            {
                if(url.IndexOf("?") != -1)
                {
                    if(url.IndexOf("/?") == -1)
                        url = url.Replace("?", "/?");
                }
                else
                {
                    if (url.Last() != '/')
                        url = $"{url}/";
                }
            }

            return url;
        }

        HttpWebRequest GetRequest(string url, HttpContext context)
        {
            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback((object sender,X509Certificate cert,X509Chain chain, System.Net.Security.SslPolicyErrors error) => true);
            var method = context.Request.Method.ToUpper();

            HttpWebRequest request = null;

            request = (HttpWebRequest)WebRequest.Create(url);
           
            request.Method = method;
            request.Timeout = (1000 * 60) * 15;
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 5;
            request.KeepAlive = context.Request.Headers["Connection"] == "keep-alive";
            request.Accept = context.Request.Headers["Accept"];
            request.Host = context.Request.Headers["Host"];
            request.UserAgent = context.Request.Headers["User-Agent"];
            request.ContentType = context.Request.Headers["Content-Type"];

            if (!String.IsNullOrEmpty(context.Request.Headers["Content-Length"]))
            {
                request.ContentLength = long.Parse(context.Request.Headers["Content-Length"]);
            }

            var headersToIgnore = new List<string>()
            {
                "Connection",
                "Accept",
                "Host",
                "User-Agent",
                "Content-Length",
                "Content-Type"
            };

            foreach (var key in context.Request.Headers.Keys.Where(k => String.IsNullOrEmpty(headersToIgnore.FirstOrDefault(i => i == k))))
            {
                try
                {
                    request.Headers.Add(key, context.Request.Headers[key]);
                }
                catch { }
            }

            return request;
        }

        string GetCacheKey(string finalUrl, HttpContext context)
        {
            var method = context.Request.Method.ToUpper();
            string body = null;

            if(method != "GET" && method != "HEAD")
                body = streamStorage.GetBase64(Constants.STREAM_REQUEST);

            var data = new {
                url = finalUrl,
                headers = context.Request.Headers.Where(h => 
                                    h.Key.IndexOf("x-forwarded", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                                    h.Key.IndexOf("x-amzn", StringComparison.InvariantCultureIgnoreCase) == -1
                                ).Select(h => $"{h.Key.ToLower()}={h.Value}").ToArray(),
                body
            };

            return JsonSerializer.Serialize(data);
        }

        int Retry502Limit
        {
            get
            {
                var text = Config.GetValue("Retry502Limit");

                if(int.TryParse(text, out int result))
                    return result;
                else
                    return 20;
            }
        }

        string RouterBaseUrl
        {
            get
            {
                var value = Config.GetValue("RouterBaseUrl");
                if(String.IsNullOrEmpty(value))
                    return "http://localhost:6000/";
                else
                    return value;
            }
        }
    }

    public static class ApiRouterExtensions
    {
         public static IApplicationBuilder UseApiRoutes(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RoutesMiddleware>();
        }
    }
}