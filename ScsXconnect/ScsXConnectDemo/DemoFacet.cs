using System;
using Sitecore.XConnect;

namespace Sidekick.XConnectDemo
{
	[FacetKey("demo")]
	[Serializable]
	public class DemoFacet : Facet
	{
		public const string DefaultFacetKey = "Demo";
		public string Stuff { get; set; }
	}
}