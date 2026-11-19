using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PythonStudyCompanion;
using PythonStudyCompanion.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<FlashcardService>();
builder.Services.AddScoped<PracticeService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ProgressService>();

await builder.Build().RunAsync();
