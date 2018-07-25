using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsContentMigrator.Core.Interface
{
	interface IChecksumManager
	{
		void RegenerateChecksum(object sender = null, EventArgs args = null);
		void StartChecksumTimer();
		int GetChecksum(string id);
	}
}
