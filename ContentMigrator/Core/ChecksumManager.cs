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
using Sitecore.Diagnostics;
using Sitecore.Events;

namespace Sidekick.ContentMigrator.Core
{
	public class ChecksumManager : IChecksumManager
	{
		public static bool ChecksumRefreshing = false;
		public static DateTime LastTimeRan = DateTime.MinValue;
		private object _checksumLocker = new object();
		private bool _checksumRefreshQueued = true;
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
		public void StartChecksumTimer(bool manualChecksumOnly)
		{
			if (!manualChecksumOnly)
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
			}

			Timer t = new Timer(500);
			t.Elapsed += (sender, e) =>
			{
				if (ContentMigrationRegistration.Root != null)
				{
					if ((_checksumRefreshQueued || (_timeSinceLastChecksumRefresh > 120000 && !manualChecksumOnly)) && !ChecksumRefreshing)
					{
						ChecksumRefreshing = true;
						try
						{
							_timeSinceLastChecksumRefresh = 0;
							var newChecksum = new ChecksumGenerator().Generate(ContentMigrationRegistration.Root.Nodes.Select(x => new ID(x.Id)).ToList(), "master");
							LastTimeRan = DateTime.Now;
							_checksum = newChecksum ?? _checksum;
							_checksumRefreshQueued = false;
						}
						catch(Exception ex)
						{
							Log.Error("Problem regenerating checksum", ex, this);
						}
						finally
						{
							ChecksumRefreshing = false;
						}
					}
					_timeSinceLastChecksumRefresh += 500;
				}
			};
			t.Start();
		}
	}
}
