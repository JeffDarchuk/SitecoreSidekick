using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.ContentMigrator.Core.Interface
{
	interface IChecksumManager
	{
		void RegenerateChecksum(object sender = null, EventArgs args = null);
		void StartChecksumTimer(bool manualChecksumOnly);
		int GetChecksum(string id);
	}
}
