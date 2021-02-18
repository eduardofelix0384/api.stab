using MedCore_Router.Services;
using api.stab.Middlewares;
using api.stab.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace api.stab
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<IStreamStorage, StreamStorage>();
            services.AddSingleton<IRequestTraceService, RequestTraceService>();

            services.AddCors(opt => {
                opt.AddDefaultPolicy(bldr => {
                    bldr.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
            
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(
                options => options.SetIsOriginAllowed(x => _ = true)
                                    .AllowAnyOrigin()
                                    .AllowAnyHeader()
                                    .AllowAnyMethod());

            app.UseHealthCheckUrl();
            app.UseStartRequest();
            app.UseBlacklist();
            app.UseApiRoutes();
            app.UseEndRequest();
        }
    }
}
