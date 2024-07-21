using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Net.WebSockets;
using Client.Menu;
using System.ComponentModel;
using System;
using Shared.Helpers;
using System.Text.Json;
using Client;
using System.Net.Sockets;
using System.Text;

var deviceId = args.FirstOrDefault() ?? "1234";
var clientState = new ClientState
{
	DeviceId = deviceId,
	IsSafeMode = args.Contains("--safemode")
};
Listener.ClientState = clientState;
Log.Logger = new LoggerConfiguration()
							.WriteTo.Console()

							.WriteTo.File($"{deviceId}-important.logs",
										  restrictedToMinimumLevel: LogEventLevel.Warning)
							.WriteTo.File($"{deviceId}-all-.logs",
										  rollingInterval: RollingInterval.Day)
							.MinimumLevel.Debug()
							.CreateLogger();
var ws = new ClientWebSocket();
Log.Information("Connecting to server");
await ws.ConnectAsync(new Uri("ws://localhost:16431/"),
  CancellationToken.None);
Log.Information("Connected to server");
CancellationTokenSource cts = new CancellationTokenSource();
CancellationToken ct = cts.Token;
var listenerTask = Task.Run(() => Listener.Listen(ws, ct), ct);

var menuTask = Task.Run(async () =>
{
	while (true)
	{
		while (clientState.IsExpectingResponse) 		{
			Console.WriteLine("Waiting for server response...");
			await Task.Delay(100);
		}
		if (clientState.IsLoggedIn)
		{
			Console.WriteLine($"Logged in as {clientState.PlayerName}");
		}
		else
		{
			Console.WriteLine("Please log in");
		}
		DisplayMenu(clientState);
		var choice = GetUserChoicAndVerify(clientState);
		switch (choice)
		{
			case MenuChoices.Login:
				Log.Information("Login selected");
				var request = new Shared.Models.Messages.LoginRequest(new Shared.Models.Messages.LoginRequestPayload(deviceId));
				var data = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
				clientState.IsExpectingResponse = true;
				await ws.SendAsync(data);
				break;
			case MenuChoices.UpdateResources:
				Log.Information("Update Resources selected");
				break;
			case MenuChoices.SendGift:
				Log.Information("Send Gift selected");
				break;
			case MenuChoices.Exit:
				Log.Information("Exit selected");
				await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "User requested close", CancellationToken.None);
				cts.Cancel();
				await listenerTask;	
				return;
			case MenuChoices.InvalidChoice:
				Log.Warning("Invalid choice selected");
				Console.WriteLine("Invalid choice. Please try again.");
				break;
		}
	}
});

await menuTask;

static void DisplayMenu(ClientState clientState)
{
	Console.WriteLine("Please choose an option:\n");
	foreach (MenuChoices choice in Enum.GetValues(typeof(MenuChoices)))
		if (choice != MenuChoices.InvalidChoice)
		{
			Console.ResetColor();
			switch (choice)
			{
				case MenuChoices.Login:
					if (clientState.IsLoggedIn)
						Console.ForegroundColor = ConsoleColor.DarkGray;
					break;
				case MenuChoices.UpdateResources:
					if (!clientState.IsLoggedIn)
						Console.ForegroundColor = ConsoleColor.DarkGray;
					break;
				case MenuChoices.SendGift:
					if (!clientState.IsLoggedIn)
						Console.ForegroundColor = ConsoleColor.DarkGray;
					break;
			}
			Console.WriteLine($"[{(int)choice}]:    {GetEnumDescription(choice)}");
		}

	Console.Write("\nEnter your selection: ");
}

static MenuChoices GetUserChoicAndVerify(ClientState clientState)
{
	var choiceStr = Console.ReadLine();
	var choice = Enum.TryParse(choiceStr, out MenuChoices choiceTmp) && Enum.IsDefined(typeof(MenuChoices), choiceTmp)
		? choiceTmp
		: MenuChoices.InvalidChoice;
	switch (choice)
	{
		case MenuChoices.Login:
			if (clientState.IsSafeMode && clientState.IsLoggedIn)
				choice = MenuChoices.InvalidChoice;
			break;
		case MenuChoices.UpdateResources:
			if (clientState.IsSafeMode &&  !clientState.IsLoggedIn)
				choice = MenuChoices.InvalidChoice;
			break;
		case MenuChoices.SendGift:
			if (clientState.IsSafeMode &&  !clientState.IsLoggedIn)
				choice = MenuChoices.InvalidChoice;
			break;
		default:
			break;
	}
	return choice;
}

static string GetEnumDescription(Enum value)
{
	var field = value.GetType()
					 .GetField(value.ToString());
	var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(
		field!, typeof(DescriptionAttribute)
	);
	return attribute == null ? value.ToString() : attribute.Description;
}
