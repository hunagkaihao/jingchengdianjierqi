using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace TuTa.Wms;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.Console())
            .WriteTo.Async(c => c.File(
                "Logs/logs-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30))
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            //builder.Host.AddAppSettingsSecretsJson();
            builder.Host.UseAutofac();
            builder.Host.UseSystemd();
            builder.Host.UseSerilog();
            await builder.AddApplicationAsync<WmsHttpApiHostModule>();

            var configuration = builder.Services.GetConfiguration();
            string[] urls = configuration["Wms:BaseUrl"].Split(",", System.StringSplitOptions.RemoveEmptyEntries);
            builder.WebHost.UseUrls(urls);

            var app = builder.Build();
            await app.InitializeApplicationAsync();
            //Console.WriteLine($"Server is running on: {string.Join(", ", urls)}");
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }
            Console.ReadLine();
            return 1;
        }
    }
}