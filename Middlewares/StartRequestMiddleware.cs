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
using api.stab.Tools;
using MedCore_Router.Services;

namespace api.stab.Middlewares
{
    public class StartRequestMiddleware
    {
        RequestDelegate _next;
        IStreamStorage streamStorage;
        IRequestTraceService traceService;

        public StartRequestMiddleware(RequestDelegate next, IStreamStorage storage, IRequestTraceService service)
        { 
            _next = next;
            streamStorage = storage;
            traceService = service;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Log.Register("> StartRequestMiddleware");

            try
            {
                await streamStorage.Add(Constants.STREAM_REQUEST, context.Request.Body, context.Request.ContentType);
                context.Items.Add(Constants.REQUEST_START_DATETIME, DateTime.Now);
                context.Items.Add(Constants.REQUEST_URL, context.Request.GetEncodedUrl());
            }
            catch(Exception ex)
            {
                traceService.Insert(ex);
                throw;
            }

            if(_next != null)
                await _next.Invoke(context);
        }
    }

    public static class StartRequestiddlewareExtension 
    {
         public static IApplicationBuilder UseStartRequest(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StartRequestMiddleware>();
        }
    }
}