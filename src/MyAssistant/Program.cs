using LiteDB;
using MyAssistant.Components;
using MyAssistant.Data;
using MyAssistant.Hubs;

namespace MyAssistant;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<LiteDatabase>(_ =>
        {
            return new LiteDatabase("Filename= MyAssistant.db;Connection=shared");
        });
        builder.Services.AddScoped<ChatSessionRepository>();
        builder.Services.AddScoped<KnowledgeFileRepository>();
        builder.Services.AddScoped<KnowledgeSetRepository>();
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
