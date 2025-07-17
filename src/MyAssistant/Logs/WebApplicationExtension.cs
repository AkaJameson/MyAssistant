using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace MyAssistant.Logs
{
    public static class WebApplicationExtension
    {
        public static WebApplicationBuilder AddLogService(this WebApplicationBuilder builder)
        {
            // 获取日志根目录路径（publish文件的同级目录下的Logs文件夹）
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string logsDirectory = Path.Combine(Directory.GetParent(baseDirectory).FullName, "Logs");

            // 确保日志目录存在
            Directory.CreateDirectory(logsDirectory);

            // 获取当前日期作为子文件夹名
            string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            string datePath = Path.Combine(logsDirectory, dateFolder);
            Directory.CreateDirectory(datePath);

            // 配置Serilog
            Log.Logger = new LoggerConfiguration()
                // 最小日志级别
                .MinimumLevel.Information()
                // 覆盖ASP.NET Core默认的日志级别
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Celora")
                // 配置不同级别的日志输出到不同文件
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Information)
                    .WriteTo.Async(a => a.File(Path.Combine(datePath, "Information-.log"),
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning)
                    .WriteTo.Async(a => a.File(Path.Combine(datePath, "Warning-.log"),
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
                    .WriteTo.Async(a => a.File(Path.Combine(datePath, "Error-.log"),
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Fatal)
                    .WriteTo.Async(a => a.File(Path.Combine(datePath, "Fatal-.log"),
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")))
                .CreateLogger();
            builder.Host.UseSerilog(Log.Logger);
            return builder;
        }
    }
}