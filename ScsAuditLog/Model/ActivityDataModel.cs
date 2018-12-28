using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsAuditLog.Model
{
	public class ActivityDataModel
	{
		public string Start;
		public string End;
		public List<object> Filters;
		public List<object> EventTypes;
		public string Field;
		public int Page;
		public Dictionary<string, bool> Databases;
	}
}
