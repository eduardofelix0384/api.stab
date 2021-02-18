using System.ComponentModel.Design;
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
using MedCore_Router.Services;
using api.stab.Tools;
using api.stab.Repository;

namespace api.stab.Middlewares
{
    public class BlacklistMiddleware
    {
        RequestDelegate _next;
        IStreamStorage streamStorage;
        IRequestTraceService traceService;

        public BlacklistMiddleware(RequestDelegate next, IStreamStorage storage, IRequestTraceService service)
        { 
            _next = next;
            streamStorage = storage;
            traceService = service;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Log.Register(@"> BlacklistMiddleware");
            
            var block = false;
            string ip = context.Request.Headers["X-Forwarded-For"];

            if(!String.IsNullOrEmpty(ip))
            {
                var blacklist = await BlacklistRepository.GetItems();

                foreach(var item in blacklist)
                {
                    if(ip == item.IP)
                    {
                        block = true;
                        break;
                    }
                }
            }

            if(block)
            {
                context.Response.StatusCode = 401;
                traceService.Insert(null, true);
            }
            else
                await _next.Invoke(context);
        }
    }

    public static class BlacklistMiddlewareExtension 
    {
         public static IApplicationBuilder UseBlacklist(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BlacklistMiddleware>();
        }
    }
}