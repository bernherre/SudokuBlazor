using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sudoku.Web;                 // <= IMPORTANTE: para que encuentre App
using Sudoku.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app"); // Debe existir App.razor bajo namespace Sudoku.Web

builder.Services.AddScoped<SudokuService>();
builder.Services.AddScoped<TimerService>();

await builder.Build().RunAsync();
