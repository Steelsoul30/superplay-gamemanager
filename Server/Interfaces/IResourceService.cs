﻿using Shared.Models.Messages;
using System.Net.WebSockets;

namespace Server.Interfaces;

public interface IResourceService
{
	Task<UpdateResourcesResponse> UpdateResources(string resourceType, int resourceValue, IWebSocketWrapper socket);
	Task<SendGiftResponse> SendGift(string resourceType, int resourceValue, int recipientId, IWebSocketWrapper socket);
}