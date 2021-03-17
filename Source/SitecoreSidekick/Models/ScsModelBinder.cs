using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Sitecore.Publishing;
using Sidekick.Core.Services.Interface;

namespace Sidekick.Core.Models
{
	public class ScsModelBinder : IModelBinder
	{
		private readonly IJsonSerializationService _jsonSerializationService;

		public ScsModelBinder()
		{
			_jsonSerializationService = Bootstrap.Container.Resolve<IJsonSerializationService>();
		}

		internal static IModelBinder Default;
		public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
		{
			var segments = controllerContext.HttpContext?.Request?.Url?.Segments;
			if (segments == null || segments.Length < 2 || !segments[1].Equals("scs/", StringComparison.Ordinal) || !(segments.Last().EndsWith(".json") || segments.Last().EndsWith(".scsvc")))
				return Default.BindModel(controllerContext, bindingContext);
			Stream s = controllerContext.HttpContext.Request.InputStream;
			s.Seek(0, SeekOrigin.Begin);
			return _jsonSerializationService.DeserializeObject(new StreamReader(s).ReadToEnd(), bindingContext.ModelType);
		}
	}
}
