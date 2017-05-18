using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicroCHAP.Server;

namespace ScsContentMigrator.Security
{
	public class UniqueChallengeStore : IChallengeStore
	{
		HashSet<string> Challenges = new HashSet<string>();
		public void AddChallenge(string challenge, int expirationTimeInMsec)
		{
			
		}

		public bool ConsumeChallenge(string challenge)
		{
			bool valid = !Challenges.Contains(challenge);
			Challenges.Add(challenge);
			return valid;
		}
	}
}
