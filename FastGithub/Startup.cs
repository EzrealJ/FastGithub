using FastGithub.Configuration;
using FastGithub.FlowAnalyze;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace FastGithub
{
    /// <summary>
    /// ������
    /// </summary>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// ���÷���
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppOptions>(this.Configuration);
            services.Configure<FastGithubOptions>(this.Configuration.GetSection(nameof(FastGithub)));

            services.AddConfiguration();
            services.AddDomainResolve();
            services.AddHttpClient();
            services.AddReverseProxy();
            services.AddFlowAnalyze();
            services.AddHostedService<AppHostedService>();

            if (false)
            {
                services.AddPacketIntercept();
            }
        }

        /// <summary>
        /// �����м��
        /// </summary>
        /// <param name="app"></param>
        public void Configure(IApplicationBuilder app)
        {
            var httpProxyPort = app.ApplicationServices.GetRequiredService<IOptions<FastGithubOptions>>().Value.HttpProxyPort;
            app.MapWhen(context => context.Connection.LocalPort == httpProxyPort, appBuilder =>
            {
                appBuilder.UseHttpProxy();
            });

            app.MapWhen(context => context.Connection.LocalPort != httpProxyPort, appBuilder =>
            {
                appBuilder.UseRequestLogging();
                appBuilder.UseHttpReverseProxy();

                appBuilder.UseRouting();
                appBuilder.DisableRequestLogging();
                appBuilder.UseEndpoints(endpoint =>
                {
                    endpoint.MapGet("/flowStatistics", context =>
                    {
                        var flowStatistics = context.RequestServices.GetRequiredService<IFlowAnalyzer>().GetFlowStatistics();
                        return context.Response.WriteAsJsonAsync(flowStatistics);
                    });
                });
            });
        }
    }
}
