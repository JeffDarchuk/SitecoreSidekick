using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rainbow.Model;
using Sitecore.Data.Items;

namespace ScsContentMigrator.Services.Interface
{
	public interface ISitecoreAccessService
	{
		IItemData GetItemData(Guid idataId);
		string GetItemIconSrc(IItemData localData);
		ConcurrentHashSet<Guid> GetSubtreeOfGuids(IEnumerable<Guid> rootIds);
		void RecycleItem(Guid itemId);

		Item GetItem(Guid id);
		List<Guid> GetChildrenIds(Guid guid);
	}
}
