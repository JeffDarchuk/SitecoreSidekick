using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Sidekick.ContentMigrator.Core.Interface;
using Sidekick.ContentMigrator.Data;
using Sitecore.Data;
using Sitecore.Events;

namespace Sidekick.ContentMigrator.Core
{
	public class ChecksumManager : IChecksumManager
	{
		private object _checksumLocker = new object();
		private bool _checksumRefreshQueued = false;
		private bool _checksumRefreshing = false;
		private int _timeSinceLastChecksumRefresh = 0;
		private string[] _events = new[] { "item:saved", "item:renamed", "item:moved", "item:deleted", "item:added", "item:copied", "item:versionAdded", "item:versionRemoved" };
		private static Checksum _checksum;
		public void RegenerateChecksum(object sender = null, EventArgs args = null)
		{
			_checksumRefreshQueued = true;
		}

		public int GetChecksum(string id)
		{
			return _checksum?.GetChecksum(id) ?? -1;
		}
		public void StartChecksumTimer()
		{
			var method = GetType().GetMethod("RegenerateChecksum");

			EventHandler events = (sender, eventArgs) =>
			{
				method.Invoke(this, new[] { sender, eventArgs });
			};
			foreach (var eventName in _events)
			{
				Event.Subscribe(eventName, events);
			}

			Timer t = new Timer(500);
			t.Elapsed += (sender, e) =>
			{
				if (ContentMigrationRegistration.Root != null)
				{
					if ((_checksumRefreshQueued || _timeSinceLastChecksumRefresh > 120000) && !_checksumRefreshing)
					{
						_checksumRefreshing = true;
						_timeSinceLastChecksumRefresh = 0;
						var newChecksum = new ChecksumGenerator().Generate(ContentMigrationRegistration.Root.Nodes.Select(x => new ID(x.Id)).ToList(), "master");
						_checksum = newChecksum ?? _checksum;
						_checksumRefreshing = false;
						_checksumRefreshQueued = false;
					}
					_timeSinceLastChecksumRefresh += 500;
				}
			};
			t.Start();
		}
	}
}
