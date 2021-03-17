using Sitecore.XConnect;
using Sitecore.XConnect.Schema;

namespace Sidekick.XConnectDemo
{
	public class DemoModel
	{
		public static XdbModel Model { get; } = BuildModel();

		private static XdbModel BuildModel()
		{
			var builder = new XdbModelBuilder("Demo", new XdbModelVersion(0, 1));
			builder.ReferenceModel(Sitecore.XConnect.Collection.Model.CollectionModel.Model);
			builder.DefineFacet<Contact, DemoFacet>(DemoFacet.DefaultFacetKey);

			return builder.BuildModel();
		}
	}
}