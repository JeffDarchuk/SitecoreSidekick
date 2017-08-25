using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScsContactSearch.Models;

namespace ScsContactSearch.Services
{
	interface IContactAccessService
	{
		void EnsureIndexExists();
		string QueryContacts(string query);
	}
}
