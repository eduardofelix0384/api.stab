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
using Microsoft.Extensions.Primitives;
using api.stab.Models;
using api.stab.Repository;
using api.stab.Tools;
using MedCore_Router.Services;

namespace api.stab.Middlewares
{
    public class EndRequestMiddleware
    {
        RequestDelegate _next;
        IRequestTraceService requestTraceService;

        public EndRequestMiddleware(RequestDelegate next, IRequestTraceService service)
        {
            _next = next;
            requestTraceService = service;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Log.Register("> EndRequestMiddleware");
            requestTraceService.Insert();
            Log.Register(@"============================================================================================");
        }
    }

    public static class EndRequestiddlewareExtension 
    {
         public static IApplicationBuilder UseEndRequest(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EndRequestMiddleware>();
        }
    }
}