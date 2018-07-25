using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsSitecoreResourceManager.Models
{
	public class SubmitTargetPropertyModel
	{
		public string TargetId { get; set; }
		public string PropertyId { get; set; }
		public string Template { get; set; }
		public string Value { get; set; }
	}
}
