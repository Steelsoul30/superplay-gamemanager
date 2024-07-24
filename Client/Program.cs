using Serilog;
using Serilog.Events;
using System.Net.WebSockets;
using Client.Menu;
using System.ComponentModel;
using Shared.Helpers;
using System.Text.Json;
using Client;
using Shared.Models.Messages;

var deviceId = args.FirstOrDefault() ?? "1234";
var clientState = new ClientState
{
    DeviceId = deviceId,
    IsSafeMode = args.Contains("--safemode")
};
Listener.ClientState = clientState;
var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
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
var retry = 1;
while (true)
{
    try
    {
        await ws.ConnectAsync(new Uri("ws://localhost:16431/"),
            CancellationToken.None);
        break;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to connect to server. Retry #{retry}", retry++);
        await Task.Delay(500);
    }
}


Log.Information("Connected to server");
CancellationTokenSource cts = new CancellationTokenSource();
CancellationToken ct = cts.Token;
var listenerTask = Task.Run(() => Listener.Listen(ws, ct), ct);

var menuTask = Task.Run(async () =>
{
    while (true)
    {
        while (clientState.IsExpectingResponse)
        {
            Console.WriteLine("Waiting for server response...");
            await Task.Delay(500);
        }

        Console.WriteLine(clientState.IsLoggedIn ? $"Logged in as {clientState.PlayerName}" : "Please log in");
        DisplayMenu(clientState);
        var choice = GetChoiceMainMenu(clientState);
        switch (choice)
        {
            case MainMenu.Login:
                Log.Information("Login selected");
                var request = new LoginRequest(new LoginRequestPayload(deviceId));
                clientState.IsExpectingResponse = true;
                if (ws.State != WebSocketState.Open)
                {
	                Log.Error("WebSocket is not open");
	                break;
                }
				await ws.SendAsync(request);
                break;
            case MainMenu.UpdateResources:
                Log.Information("Update Resources selected");
                DisplayUpdateSubMenu();
                ResourceType resourceType;
                while (true)
                {
                    resourceType = GetChoiceUpdateMenu();
                    if (resourceType == ResourceType.InvalidChoice)
                    {
                        Console.WriteLine("Invalid choice. Please try again.");
                        continue;
                    }
                    break;
                }
                if (resourceType == ResourceType.Back)
                    break;
                var amount = GetAmount();
                if (amount == 0)
                    break;
                var resourceTypeStr = resourceType.ToString().ToLower();
                var updateRequest = new UpdateResourcesRequest(new UpdateResourcesRequestPayload(resourceTypeStr, amount));
                if (ws.State != WebSocketState.Open)
				{
					Log.Error("WebSocket is not open");
					break;
				}
                await ws.SendAsync(updateRequest);
                break;
            case MainMenu.SendGift:
                Log.Information("Send Gift selected");
                DisplayUpdateSubMenu();
                while (true)
                {
	                resourceType = GetChoiceUpdateMenu();
	                if (resourceType == ResourceType.InvalidChoice)
	                {
		                Console.WriteLine("Invalid choice. Please try again.");
		                continue;
	                }
	                break;
                }
                if (resourceType == ResourceType.Back)
	                break;
                amount = GetAmount();
                if (amount == 0)
	                break;
                var recipient = GetRecipient();
                if (recipient == 0)
	                break;
                resourceTypeStr = resourceType.ToString().ToLower();
                var sendGiftRequest = new SendGiftRequest(new SendGiftRequestPayload(resourceTypeStr, amount, recipient));
                if (ws.State != WebSocketState.Open)
                {
	                Log.Error("WebSocket is not open");
	                break;
                }
				await ws.SendAsync(sendGiftRequest);
				break;
            case MainMenu.Exit:
                Log.Information("Exit selected");
                if (ws.State != WebSocketState.Open)
                {
	                Log.Error("WebSocket is not open");
	                break;
                }
				await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "User requested close", CancellationToken.None);
                cts.Cancel();
                await listenerTask;
                return;
            case MainMenu.InvalidChoice:
                Log.Warning("Invalid choice selected");
                Console.WriteLine("Invalid choice. Please try again.");
                break;
        }
    }
});

await menuTask;
return;

static void DisplayUpdateSubMenu()
{
    Console.WriteLine("Please choose a resource:\n");
    foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
    {
        if (resource == ResourceType.InvalidChoice)
            continue;
        Console.WriteLine($"[{(int)resource}]:    {GetEnumDescription(resource)}");
    }
    Console.Write("\nEnter your selection: ");
}

static void DisplayMenu(ClientState clientState)
{
    Console.WriteLine("Please choose an option:\n");
    foreach (MainMenu choice in Enum.GetValues(typeof(MainMenu)))
    {
        if (choice == MainMenu.InvalidChoice)
            continue;
        Console.ResetColor();
        switch (choice)
        {
            case MainMenu.Login:
                if (clientState.IsLoggedIn)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
            case MainMenu.UpdateResources:
                if (!clientState.IsLoggedIn)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
            case MainMenu.SendGift:
                if (!clientState.IsLoggedIn)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
        }

        Console.WriteLine($"[{(int)choice}]:    {GetEnumDescription(choice)}");
    }

    Console.Write("\nEnter your selection: ");
}

static ResourceType GetChoiceUpdateMenu()
{
    var choice = GetChoice<ResourceType>();
    return choice;
}

static int GetAmount()
{
    Console.Write("Enter the amount. (0) to cancel: ");
    int amount;
    while (!int.TryParse(Console.ReadLine(), out amount)) Console.WriteLine("Only integers allowed");
    return amount;
}

static int GetRecipient()
{
	Console.Write("Enter the recipient's Id. (0) to cancel: ");
	int recipientId;
	while (!int.TryParse(Console.ReadLine(), out recipientId)) Console.WriteLine("Only numbers allowed");
	return recipientId;
}

static MainMenu GetChoiceMainMenu(ClientState clientState)
{
    var choice = GetChoice<MainMenu>();
    switch (choice)
    {
        case MainMenu.Login:
            if (clientState.IsSafeMode && clientState.IsLoggedIn)
                choice = MainMenu.InvalidChoice;
            break;
        case MainMenu.UpdateResources:
            if (clientState.IsSafeMode && !clientState.IsLoggedIn)
                choice = MainMenu.InvalidChoice;
            break;
        case MainMenu.SendGift:
            if (clientState.IsSafeMode && !clientState.IsLoggedIn)
                choice = MainMenu.InvalidChoice;
            break;
    }
    return choice;
}

static T GetChoice<T>() where T : struct
{
    var choiceStr = Console.ReadLine();
    return Enum.TryParse(choiceStr, out T choiceTmp) && Enum.IsDefined(typeof(T), choiceTmp)
        ? choiceTmp
        : default;
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
