using System;
using Rainbow.Filtering;

namespace SitecoreSidekick.Rainbow
{
	public class DefaultFieldFilter : IFieldFilter
	{
		public bool Includes(Guid fieldId)
		{
			return true;
		}
	}
}
