using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using ScsContentMigrator.Args;
using ScsContentMigrator.Data;
using ScsContentMigrator.Models;
using ScsContentMigrator.Security;

namespace ScsContentMigrator.Services.Interface
{
	public interface IRemoteContentService
	{
		IItemData GetRemoteItemData(Guid id, string server);
		ChildrenItemDataModel GetRemoteItemDataWithChildren(Guid id, string server);
		IItemData DeserializeYaml(string yaml, string id);
		CompareContentTreeNode GetContentTreeNode(RemoteContentTreeArgs args);
		ScsHmacServer HmacServer { get; }
	}
}
