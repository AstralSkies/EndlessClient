// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

namespace EndlessClient.Rendering
{
	public enum ArmorShieldSpriteType
	{
							//dir1/dir2
		Standing = 1,		//1/2
		WalkFrame1 = 3,		//3/7
		WalkFrame2 = 4,		//4/8
		WalkFrame3 = 5,		//5/9
		WalkFrame4 = 6,		//6/10
		SpellCast = 11,		//11/12
		PunchFrame1 = 13,	//13/15
		PunchFrame2 = 14,	//14/16
		
		//not valid for shields:
		SitChair = 17,		//17/18
		SitGround = 19,		//19/20
		Bow = 21,			//21/22
	}
}