# GameManager Solution

The GameManager solution is a comprehensive framework designed to facilitate the development of games with a focus on real-time interactions and resource management. It leverages modern technologies and patterns to provide a robust foundation for building scalable, multiplayer online games.

## Features

- **WebSocket Communication**: Utilizes WebSockets for real-time bi-directional communication between the server and clients, ensuring fast and efficient message exchange.
- **Resource Management**: Offers a sophisticated resource management system, allowing for the tracking and updating of player resources such as coins and rolls.
- **Extensible Command Handling**: Implements a command handler pattern, making it easy to extend the game's functionality by adding new commands and associated handlers.
- **Logging and Monitoring**: Integrates advanced logging capabilities, facilitating debugging and monitoring of the game's operations through various log levels.

## Components

### GameManager.Server

The server component is responsible for handling client connections, processing commands, and managing game state. Key features include:

- **WebSocketService**: Manages WebSocket connections, including sending and receiving messages.
- **ResourceService**: Handles operations related to resource management, such as updating player resources.
- **Command Handlers**: A collection of handlers for processing specific game commands.
- **PlayerService**: Manages player related actions. Currently only login but can extend to registration, authentication, and profile updates.

Possible future extensions include:
- **GameSessionService**: Coordinates game sessions, including creation, joining, and management of active sessions.
- **MatchmakingService**: Implements matchmaking logic to pair players for games based on skill levels, preferences, or other criteria.
- **LeaderboardService**: Maintains and updates the game's leaderboards, tracking top players and achievements.
- **ChatService**: Facilitates in-game communication between players through text chat.


### GameManager.Client

The client component (not detailed here) would typically interact with the server via WebSockets, sending commands and receiving updates about the game state.
It is currently used for testing the server component.

## Extensions

The solution includes several extension methods and utilities to enhance its functionality:

- **WebSocket Extensions**: Simplifies sending messages over WebSocket connections by serializing objects to JSON.
- **Logger Extensions**: Enhances the logging capabilities, allowing for easy verification of log messages in unit tests.

## Testing

Unit testing is a first-class citizen in the GameManager solution. It includes:

- **FakeItEasy Integration**: Utilizes the FakeItEasy library to mock dependencies in tests, providing a flexible way to simulate various scenarios.
- **LoggerExtensions**: Contains methods for asserting the presence or absence of log messages at specific log levels.

## Getting Started

To get started with the GameManager solution, clone the repository and open the solution file in Visual Studio. Ensure you have the necessary dependencies installed, including .NET Core and any relevant NuGet packages.

Just run the server project and connect to it using the client project.
Client project is a console application That has two arguments. First argument is the Device Id which will allow a player to login.  
The second is the argument --safemode. Adding this argument will ensure the client cannot perform actions that are grayed out in the menu.
The default is without this flag, as this is useful for testing the server's response to invalid commands.  
The server's database is preseeded with players on first database creation (on first run of the server).



```csharp
var players = new Player[]
{
	new() { Id = 1, PlayerName = "John", DeviceID = "1234", Coins = 100, Rolls = 50},
	new() { Id = 2, PlayerName = "Jane", DeviceID = "5678", Coins = 200, Rolls = 100},
	new() { Id = 3, PlayerName = "Jim", DeviceID = "9012", Coins = 300, Rolls = 150},
};
```


## License

This is not production ready code. It is a proof of concept.

The GameManager solution is open-source and available under the [MIT License](LICENSE).

---

This README provides a high-level overview of the GameManager solution. For more detailed information, please refer to the documentation within each project.
