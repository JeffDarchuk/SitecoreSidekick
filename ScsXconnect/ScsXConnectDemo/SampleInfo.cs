using System;
using Sitecore.XConnect;

namespace Sidekick.XConnectDemo
{
	[FacetKey("Sample")]
	[Serializable]
	public class SampleInfo : Facet
	{
		public const string DefaultFacetKey = "Sample";
		public string Things { get; set; }
	}
}
