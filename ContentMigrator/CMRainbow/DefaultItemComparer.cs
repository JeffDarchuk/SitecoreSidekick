using System.Collections.Generic;
using System.Xml;
using Rainbow.Diff;
using Rainbow.Diff.Fields;

namespace Sidekick.ContentMigrator.CMRainbow
{
	public class DefaultItemComparer : ItemComparer
	{
		public DefaultItemComparer(XmlNode xml): base(xml)
		{

		}

		public DefaultItemComparer(List<IFieldComparer> fieldComparers) : base(fieldComparers)
		{
		}

		public DefaultItemComparer() : base(new List<IFieldComparer>()
		{
			new CheckboxComparison(),
			new MultiLineTextComparison(),
			new MultilistComparison(),
			new DefaultComparison(),
			new XmlComparison()
		})
		{
		}
	}
}