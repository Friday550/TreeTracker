// Program.cs
using TreeTracker.Components;
using TreeTracker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register TreeService
builder.Services.AddScoped<TreeService>();
builder.Services.AddScoped<TreeService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ManualLogService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
