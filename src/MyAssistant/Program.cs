using Blazored.LocalStorage;
using LiteDB;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MyAssistant.Components;
using MyAssistant.Core;
using MyAssistant.IServices;
using MyAssistant.Logs;
using MyAssistant.Repository;
using MyAssistant.ServiceImpl;

namespace MyAssistant;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddLogService();
        builder.Services.AddSingleton<LiteDatabase>(_ =>
        {
            return new LiteDatabase("Filename= MyAssistant.db;Connection=shared");
        });
        var portSection = builder.Configuration.GetSection("Port");
        var port = 8091;
        if (portSection.Exists())
        {
            port = portSection.Get<int>();
        }
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port);
        });
        builder.Services.AddSignalR(e => {
            e.MaximumReceiveMessageSize = 102400000;
        });

        builder.Services.AddScoped<JsInterop>();
        builder.Services.AddSingleton<KernelContext>();
        builder.Services.AddSingleton<ChatContext>();
        builder.Services.AddSingleton<QdrantSupport>();

        builder.Services.AddScoped<ChatSessionRepository>();
        builder.Services.AddScoped<KnowledgeFileRepository>();
        builder.Services.AddScoped<KnowledgeSetRepository>();

        builder.Services.AddScoped<IAgentService, AgentServiceImpl>();
        builder.Services.AddScoped<IKnowledgeService, KnowledgeServiceImpl>();
        builder.Services.AddScoped<IChatService, ChatServiceImpl>();
        builder.Services.AddScoped<IModelService, ModelServiceImpl>();
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
