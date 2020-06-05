using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsContentMigrator.Models
{
	public class PresetModel : PullItemModel
	{
		public string Name { get; set; }
		public string Desc { get; set; }
		public HashSet<string> WhiteList { get; set; } = new HashSet<string>();
		public HashSet<string> BlackList { get; set; } = new HashSet<string>();

	}
}
