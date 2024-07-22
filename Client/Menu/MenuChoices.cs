using System.ComponentModel;

namespace Client.Menu
{
	internal enum MainMenu
	{
		[Description("Invalid Choice")]
		InvalidChoice,
		[Description("Login")]
		Login,
		[Description("Update Resources")]
		UpdateResources,
		[Description("Send Gift")]
		SendGift,
		[Description("Exit")]
		Exit	
	}

	internal enum ResourceType
    {
        [Description("Invalid Choice")]
        InvalidChoice,
        [Description("Coins")]
        Coins,
        [Description("Rolls")]
		Rolls,
		[Description("Back")]
		Back
    }
}
