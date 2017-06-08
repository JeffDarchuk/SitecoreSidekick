using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsContentMigrator.Models
{
	public class OperationStatusRequestModel
	{
		public string OperationId { get; set; }
		public int LineNumber { get; set; }
	}
}
