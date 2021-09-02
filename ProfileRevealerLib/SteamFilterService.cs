#if !DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Assets.Scripts.Leaderboards;
using Assets.Scripts.Services.Steam;

namespace ProfileRevealerLib {
	/// <summary>A <see cref="ServicesSteam"/> implementation used to block leaderboard requests</summary>
	/// <remarks>This class is a copy of a class from the Tweaks mod.</remarks>
	internal class SteamFilterService : ServicesSteam {
		public static string TargetMissionID;
		private static PropertyInfo SubmitScoreProperty;

		public override void ExecuteLeaderboardRequest(LeaderboardRequest request) {
			if (request is LeaderboardListRequest listRequest && listRequest.SubmitScore && listRequest.MissionID == TargetMissionID) {
				if (SubmitScoreProperty == null) SubmitScoreProperty = typeof(LeaderboardListRequest).GetProperty("SubmitScore");
				SubmitScoreProperty.SetValue(listRequest, false, null);
				TargetMissionID = null;
			}
			base.ExecuteLeaderboardRequest(request);
		}
	}
}
#endif
