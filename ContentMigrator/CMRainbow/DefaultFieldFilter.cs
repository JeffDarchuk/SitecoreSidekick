using System;
using Rainbow.Filtering;

namespace ScsContentMigrator.CMRainbow
{
	public class DefaultFieldFilter : IFieldFilter
	{
		public bool Includes(Guid fieldId)
		{
			return true;
		}
	}
}
