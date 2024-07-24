using Microsoft.EntityFrameworkCore;

namespace Server.DB
{
	public class GameContext : DbContext
	{
		public DbSet<Player> Players { get; set; }

		public GameContext (DbContextOptions<GameContext> options) : base(options)
		{
			
		}
	}

	public class Player
	{
		public int Id { get; set; }
		public string PlayerName { get; set; }
		public string DeviceID { get; set; }
		public int Coins { get; set; }
		public int Rolls { get; set; }
	}
}
