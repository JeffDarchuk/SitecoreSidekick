using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SitecoreSidekick.Models
{
	public class ContentSelectedRelatedModel
	{
		public string Server { get; set; }
		public List<string> SelectedIds { get; set; }
	}
}
