using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Rainbow.Storage;

namespace Sidekick.ContentMigrator.Services.Interface
{
	interface IDatastoreSaver
	{
		void Save(IDataStore store, IItemData data);
	}
}
