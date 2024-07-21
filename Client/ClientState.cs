﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
	internal class ClientState
	{
		public string DeviceId { get; init; } = string.Empty;
		public string PlayerName { get; set; } = string.Empty;
		public bool IsLoggedIn {get;set;}
		public bool IsExpectingResponse {get;set;}
		public bool IsSafeMode { get; set; }
	}
}
