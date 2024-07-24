using System.Net.WebSockets;
using Shared.Models.Messages;

namespace Server.Interfaces;

public interface IPlayerService
{
	Task<LoginResponse> Login(string deviceId, WebSocket socket);
}