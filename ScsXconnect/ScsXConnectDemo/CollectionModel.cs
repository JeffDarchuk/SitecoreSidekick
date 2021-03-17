using Sitecore.XConnect;
using Sitecore.XConnect.Schema;

namespace Sidekick.XConnectDemo
{
	public class CollectionModel
	{
		public static XdbModel Model {get;} = BuildModel();

		private static XdbModel BuildModel()
		{
			var builder = new XdbModelBuilder("Sidekick.XConnect.XConnect", new XdbModelVersion(0, 1));
			builder.ReferenceModel(Sitecore.XConnect.Collection.Model.CollectionModel.Model);
			builder.DefineFacet<Contact, SampleInfo>(SampleInfo.DefaultFacetKey);

			return builder.BuildModel();
		}
	}
}