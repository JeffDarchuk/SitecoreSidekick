using System;
using System.Collections.Generic;
using Rainbow.Model;

namespace ScsContentMigrator
{
	public class ErrorItemData : IItemData, IItemMetadata
	{
		public Guid Id { get; set; }
		public Guid ParentId { get; }
		public Guid TemplateId { get; }
		public string Path { get; set; }
		public string SerializedItemId { get; }
		public IEnumerable<IItemData> GetChildren()
		{
			throw new NotImplementedException();
		}

		public string DatabaseName { get; set; }
		public string Name { get; set; }
		public Guid BranchId { get; }
		public IEnumerable<IItemFieldValue> SharedFields { get; }
		public IEnumerable<IItemLanguage> UnversionedFields { get; }
		public IEnumerable<IItemVersion> Versions { get; }
	}
}
