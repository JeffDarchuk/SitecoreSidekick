using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ScsContactSearch.Models;

namespace ScsContactSearch.Services
{
	public class ContactAccessService : IContactAccessService
	{
		private MongoCollection<BsonDocument> collection;
		private const string _mongoFunctionRegex = ":\\s+[^\\(\"]+\\(([^\\)]*)\\)";
		public ContactAccessService()
		{
			string connectionString = ConfigurationManager.ConnectionStrings["analytics"].ConnectionString;
			var mongoUrl = new MongoUrl(connectionString);
			var server = (new MongoClient(connectionString)).GetServer();
			var database = server.GetDatabase(mongoUrl.DatabaseName);
			collection = database.GetCollection("Contacts");
		}
		public void EnsureIndexExists()
		{
			collection.CreateIndex(IndexKeys.TextAll(), IndexOptions.SetName("Searchable").SetBackground(true));
		}

		public string QueryContacts(string query)
		{
			var textSearch = new CommandDocument
			{
				{"text", collection.Name},
				{"search", query}
			};
			return Regex.Replace(collection.Database.RunCommand(textSearch).Response.Elements.ToJson(), _mongoFunctionRegex, ": $1", RegexOptions.None);
		}
	}
}
