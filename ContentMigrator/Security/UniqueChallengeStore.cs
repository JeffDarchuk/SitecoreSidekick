﻿using System;
using System.Collections.Generic;
using MicroCHAP.Server;

namespace Sidekick.ContentMigrator.Security
{
	/// <summary>
	/// Implements a reactive challenge store.
	/// This enables HMAC authenticated requests that do not use a challenge issued by the server,
	/// to increase performance.
	/// </summary>
	public class UniqueChallengeStore : IChallengeStore
	{
		readonly HashSet<string> _challenges = new HashSet<string>();
		readonly Queue<string> _cleaner = new Queue<string>();

		public void AddChallenge(string challenge, int expirationTimeInMsec)
		{
			
		}

		public bool ConsumeChallenge(string challenge)
		{
			bool valid = !_challenges.Contains(challenge);

			_challenges.Add(challenge);
			_cleaner.Enqueue(challenge);
			if (_cleaner.Count > 100000)
			{
				_challenges.Remove(_cleaner.Dequeue());
			}
			return valid;
		}
	}
}
