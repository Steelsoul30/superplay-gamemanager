using Shared.Models.Messages;

namespace Server.Interfaces;

public interface IPlayerService
{
	Task<LoginResponse> Login(string deviceId, IWebSocketWrapper socket);
}