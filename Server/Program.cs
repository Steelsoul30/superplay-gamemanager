using Microsoft.EntityFrameworkCore;
using Serilog;
using Server.DB;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
Log.Logger = logger;
builder.Host.UseSerilog();
builder.Services.AddDbContext<GameContext>( o => o.UseSqlite(builder.Configuration.GetConnectionString("SQLiteDefault")));
builder.Services.AddScoped<IGameService, GameService>();
var app = builder.Build();
app.UseWebSockets();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<GameContext>();
	DbInitializer.Initialize(db);
}

app.Logger.LogInformation("Server started");

app.MapGet("/", async (HttpContext context, IGameService gameService) =>
{
	if (!context.WebSockets.IsWebSocketRequest)
	{
		context.Response.StatusCode = 400;
		await context.Response.WriteAsync("Expected a WebSocket request");
		return;
	}

	using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
	await gameService.ListenOnSocket(webSocket);
});

app.Run();

