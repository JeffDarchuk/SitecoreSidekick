using System;
using Rainbow.Filtering;

namespace Sidekick.ContentMigrator.CMRainbow
{
	public class DefaultFieldFilter : IFieldFilter
	{
		public bool Includes(Guid fieldId)
		{
			return true;
		}
	}
}
