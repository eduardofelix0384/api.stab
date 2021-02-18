using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using api.stab;
using api.stab.Models;
using api.stab.Repository;
using api.stab.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace MedCore_Router.Services
{
    public class RequestTraceService : IRequestTraceService
    {
        IHttpContextAccessor contextAccessor;
        IStreamStorage streamStorage;

        public RequestTraceService(IHttpContextAccessor  ctx, IStreamStorage storage)
        {
            this.contextAccessor = ctx;
            this.streamStorage = storage;
        }

        public async Task Insert(Exception exception = null, bool? blacklist = null)
        {
            try
            {
                if(Config.EnableTrace)
                {
                    var context = contextAccessor.HttpContext;
                    var item = new RequestTraceItem();

                    DateTime? start = DateTime.Now;

                    if(context.Items[Constants.REQUEST_START_DATETIME] != null)
                        start = context.Items[Constants.REQUEST_START_DATETIME] as DateTime?;

                    var end = DateTime.Now;

                    if(exception == null)
                        item.StatusCode = context.Response.StatusCode;
                    else
                    {
                        item.StatusCode = 500;

                        item.RouterException = $@"ErrorMessage: {exception.Message}
    StackTrace: {exception.StackTrace}
    InnerException ErrorMessage: {((exception.InnerException == null) ? String.Empty : exception.InnerException.Message)}
    InnerException InnerStackTrace: {((exception.InnerException == null) ? String.Empty : exception.InnerException.StackTrace)}";
                    }
                    
                    item.Url = context.Items[Constants.REQUEST_URL] != null ? context.Items[Constants.REQUEST_URL].ToString() : null;
                    item.TargetUrl = context.Items[Constants.TARGET_URL] != null ? context.Items[Constants.TARGET_URL].ToString() : null;
                    item.CleanUrl = item.Url != null ? Regex.Replace(item.Url.Replace(this.RouterBaseUrl, "/").Split('?')[0], @"[\d-]", string.Empty) : null;
                    item.RouterServer = Environment.MachineName;
                    item.Duration = (end - start.Value).TotalMilliseconds;
                    item.RequestTimeStamp = Int64.Parse(start.Value.ToString("yyyyMMddHHmmss"));
                    item.Method = context.Request.Method;
                    item.IP = context.Request.Headers["X-Forwarded-For"];
                    item.UserAgent = context.Request.Headers["User-Agent"];
                    item.Cache = context.Items[Constants.CACHE_KEY] != null;
                    item.Blacklist = blacklist.HasValue ? blacklist.Value : false;

                    if(item.Cache)
                        item.CacheKey = context.Items[Constants.CACHE_KEY].ToString();
                    
                    if(item.StatusCode != 200)
                    {
                        string requestBody = null;

                        using(var requestStream = streamStorage.Get(Constants.STREAM_REQUEST))
                        {
                            if(requestStream != null)
                            {
                                using(var reader = new StreamReader(requestStream))
                                {
                                    requestBody = await reader.ReadToEndAsync();
                                }
                            }
                        }

                        item.RequestBody = requestBody;

                        string responseBody = null;

                        using(var responseStream = streamStorage.Get(Constants.STREAM_RESPONSE))
                        {
                            if(responseStream != null)
                            {
                                using(var reader = new StreamReader(responseStream))
                                {
                                    responseBody = await reader.ReadToEndAsync();
                                }
                            }
                        }

                        item.ResponseBody = responseBody;

                        item.RequestHeaders = String.Join(" | ", context.Request.Headers.Select(_ => $"{_.Key} = {_.Value}").ToArray());
                        item.ResponseHeaders = String.Join(" | ", context.Response.Headers.Select(_ => $"{_.Key} = {_.Value}").ToArray());
                    }

                    if(context.Response.Headers.TryGetValue("Track-ApiServer", out StringValues apiServer))
                        item.ApiServer = apiServer;

                    if(context.Response.Headers.TryGetValue("Track-ApiVersion", out StringValues apiVersion))
                        item.ApiVersion = apiVersion;

                    await RequestTraceRepository.Insert(item);
                }
            }
            catch(Exception ex)
            {
                Log.Register($@"<<<<<<<<<<<<<<!>>>>>>>>>>>>>>");
                Log.Register($@"Erro ao salvar Trace:
ErrorMessage: {ex.Message}
StackTrace: {ex.StackTrace}");
                Log.Register($@"<<<<<<<<<<<<<<!>>>>>>>>>>>>>>");
            }
        }

        string RouterBaseUrl
        {
            get
            {
                var value = Config.BaseUrl;
                
                if(String.IsNullOrEmpty(value))
                    return "http://localhost:6000/";
                else
                    return value;
            }
        }
    } 

    public interface IRequestTraceService
    {
        Task Insert(Exception exception = null, bool? blacklist = null);
    }     
}