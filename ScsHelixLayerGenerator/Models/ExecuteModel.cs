using ScsHelixLayerGenerator.Data.Properties.Collectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Models
{
	public class ExecuteModel
	{
		public List<DefaultCollector> Properties;
		public string Template;
		public string Target;
	}
}
