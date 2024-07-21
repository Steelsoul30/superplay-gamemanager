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
				new Player{PlayerName="Carson",DeviceID="1234",Coins="100"},
				new Player{PlayerName="Meredith",DeviceID="5678",Coins="200"},
				new Player{PlayerName="Arturo",DeviceID="9012",Coins="300"},
				new Player{PlayerName="Gytis",DeviceID="3456",Coins="400"},
				new Player{PlayerName="Yan",DeviceID="7890",Coins="500"},
				new Player{PlayerName="Peggy",DeviceID="2345",Coins="600"},
				new Player{PlayerName="Laura",DeviceID="6789",Coins="700"}
			};
			context.Players.AddRange(players);
			context.SaveChanges();
		}
	}
}
