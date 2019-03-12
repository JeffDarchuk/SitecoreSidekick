using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.Items;
using SitecoreSidekick.Models;

namespace SitecoreSidekick.Services.Interface
{
	public interface ISitecoreDataAccessService
	{
		ScsSitecoreItem GetScsSitecoreItem(string id);
		IItemData GetLatestItemData(string idataId, string database = null);
		IItemData GetLatestItemData(Guid idataId, string database = null);
		IItemData GetItemData(string idataId, string database = null);
		IItemData GetItemData(Guid idataId, string database = null);
		IEnumerable<IItemData> GetChildren(IItemData parent);
		Dictionary<Guid, string> GetItemAndChildrenRevision(Guid idataId, string database = null);
		string GetItemRevision(Guid idataId, string database = null);
		IItemData GetRootItemData(string database = null);
		List<Guid> GetChildrenIds(Guid guid);
		HashSet<Guid> GetSubtreeOfGuids(IEnumerable<Guid> rootIds);
		IEnumerable<string> GetVersions(IItemData itemData);
		string GetIconSrc(IItemData item, int width = 32, int height = 32, string align = "", string margin = "");
		string GetItemYaml(string idataId, Func<object, string> serializationFunc);
		string GetItemYaml(Guid idataId, Func<object, string> serializationFunc);
		void RecycleItem(string id);
		void RecycleItem(Guid id);
		string GetIcon(Guid id);
		List<Database> GetAllDatabases();
	}
}
