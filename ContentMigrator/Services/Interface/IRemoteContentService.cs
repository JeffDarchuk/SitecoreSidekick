using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Sidekick.ContentMigrator.Args;
using Sidekick.ContentMigrator.Data;
using Sidekick.ContentMigrator.Models;
using Sidekick.ContentMigrator.Security;

namespace Sidekick.ContentMigrator.Services.Interface
{
	public interface IRemoteContentService
	{
		IItemData GetRemoteItemData(Guid id, string server);
		ChildrenItemDataModel GetRemoteItemDataWithChildren(Guid id, string server, Dictionary<Guid, string> rev = null);
		IItemData DeserializeYaml(string yaml, Guid id);
		object ChecksumIsGenerating(string server);
		bool ChecksumRegenerate(string server);
		CompareContentTreeNode GetContentTreeNode(RemoteContentTreeArgs args);
		HmacServer HmacServer { get; }
	}
}
