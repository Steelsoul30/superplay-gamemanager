namespace Server.DB
{
	public static class DbInitializer
	{
		public static void Initialize(GameContext context)
		{
			context.Database.EnsureCreated();
			if (context.Players.Any())
			{
				return;
			}
			var players = new Player[]
			{
				new() { PlayerId = 1, PlayerName = "John", DeviceID = "1234", Coins = 100, Rolls = 50},
				new() { PlayerId = 2, PlayerName = "Jane", DeviceID = "5678", Coins = 200, Rolls = 100},
				new() { PlayerId = 3, PlayerName = "Jim", DeviceID = "9012", Coins = 300, Rolls = 150},
			};
			context.Players.AddRange(players);
			context.SaveChanges();
		}
	}
}
