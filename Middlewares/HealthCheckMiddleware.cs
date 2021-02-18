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

namespace api.stab.Middlewares
{
    public class HealthCheckMiddleware
    {
        RequestDelegate _next;

        public HealthCheckMiddleware(RequestDelegate next)
        { 
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Log.Register(@"
============================================================================================
> HealthCheckMiddleware");
            
            var url = context.Request.GetEncodedUrl();
            
            if(url.IndexOf("api.stab/health-check", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("api.stab is working!");
            }
            else
                await _next.Invoke(context);
        }
    }

    public static class HealthCheckMiddlewareExtension 
    {
         public static IApplicationBuilder UseHealthCheckUrl(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HealthCheckMiddleware>();
        }
    }
}