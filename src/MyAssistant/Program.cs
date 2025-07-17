using LiteDB;
using MyAssistant.Components;
using MyAssistant.Core;
using MyAssistant.Hubs;
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
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<LiteDatabase>(_ =>
        {
            return new LiteDatabase("Filename= MyAssistant.db;Connection=shared");
        });
        builder.Services.AddSingleton<KernelContext>();
        builder.Services.AddSingleton<ChatContext>();
        builder.Services.AddSingleton<QdrantSupport>();

        builder.Services.AddScoped<ChatSessionRepository>();
        builder.Services.AddScoped<KnowledgeFileRepository>();
        builder.Services.AddScoped<KnowledgeSetRepository>();
        builder.Services.AddScoped<IChatService, ChatServiceImpl>();
        builder.Services.AddScoped<IKnowledgeService, KnowledgeServiceImpl>();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();


        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapHub<ChatHub>("/chathub");
        app.Run();
    }
}
