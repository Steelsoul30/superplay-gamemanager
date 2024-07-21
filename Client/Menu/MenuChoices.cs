using System.ComponentModel;

namespace Client.Menu
{
	internal enum MenuChoices
	{
		[Description("Invalid Choice"), ]
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
}
