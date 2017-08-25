using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScsContactSearch.Models
{
	public class ContactModel
	{
		[BsonElement("_id")]
		public string Id { get; set; }
		[BsonElement("Personal.FirstName")]
		public string FirstName { get; set; }
		[BsonElement("Personal.Surname")]
		public string LastName { get; set; }
		[BsonElement("Emails.Entries.Preferred.SmtpAddress")]
		public string Email { get; set; }
	}
}
