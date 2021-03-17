using Sidekick.Core.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Resources;
using System.Web;
using System.Web.Mvc;
using Sidekick.Core.Services.Interface;
using Sidekick.Core.Shared.IoC;

namespace Sidekick.Core
{
	public abstract class ScsController : Controller
	{
		private readonly IJsonSerializationService _jsonSerializationService;
		private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, string>> _resourceCache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, string>>();
		private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, byte[]>> _imageCache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, byte[]>>();
		private readonly IScsRegistrationService _registration;
		private readonly IMainfestResourceStreamService _manifestResourceStreamService;
		private static DateTime StartTime = DateTime.Now;
		protected ScsController()
		{
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
			_jsonSerializationService = Bootstrap.Container.Resolve<IJsonSerializationService>();
			_manifestResourceStreamService = Bootstrap.Container.Resolve<IMainfestResourceStreamService>();
		}

		[LoggedIn]
		public virtual ActionResult Resources(string filename)
		{
			try
			{
				if (filename.EndsWith(".scs"))
					return Content(GetResource(filename), "text/html");
				if (filename.EndsWith(".html"))
					return Content(GetResource(filename), "text/html");
				if (filename.EndsWith(".gif"))
					return File(GetImage(filename, ImageFormat.Gif), "image/gif");
				if (filename.EndsWith(".png"))
					return File(GetImage(filename, ImageFormat.Png), "image/png");
				if (filename.EndsWith(".jpg"))
					return File(GetImage(filename, ImageFormat.Jpeg), "image/jpg");
				if (filename.EndsWith(".bmp"))
					return File(GetImage(filename, ImageFormat.Bmp), "image/bmp");
				if (filename.EndsWith(".emf"))
					return File(GetImage(filename, ImageFormat.Emf), "image/emf");
				if (filename.EndsWith(".ico"))
					return File(GetImage(filename, ImageFormat.Icon), "image/icon");
				if (filename.EndsWith(".tiff"))
					return File(GetImage(filename, ImageFormat.Tiff), "image/tiff");
				if (filename.EndsWith(".wmf"))
					return File(GetImage(filename, ImageFormat.Wmf), "image/wmf");
				if (filename.EndsWith(".svg"))
					return Content(GetResource(filename), "image/svg+xml");
				if (filename.EndsWith(".js"))
					return Content(GetResource(filename), "text/javascript");
			}
			catch (ScsEmbeddedResourceNotFoundException) { }
			Response.StatusCode = 404;
			return Content("Requested resource not found");

		}

		public virtual ActionResult ScsJson(object o)
		{
			return Content(_jsonSerializationService.SerializeObject(o), "application/json");
		}
		/// <summary>
		/// extracts the resource out of the binary
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public string GetResource(string filename)
		{
			filename = filename.ToLowerInvariant();
			Sitecore.Context.SetActiveSite("scs");
			_resourceCache.TryGetValue(GetType(), out var cache);
			if (cache != null && cache.TryGetValue(filename, out var result)) return result;

			result = _manifestResourceStreamService.GetManifestResourceText(GetType(), _registration.GetScsRegistration(GetType()).ResourcesPath + "." + filename, ()=>throw new ScsEmbeddedResourceNotFoundException());
			if (!_resourceCache.ContainsKey(GetType()))
			{
				_resourceCache[GetType()] = new ConcurrentDictionary<string, string>();
			}
			_resourceCache[GetType()][filename] = result;
			return result;
		}

		/// <summary>
		/// returns image resource
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="imageFormat"></param>
		/// <returns></returns>
		private byte[] GetImage(string filename, ImageFormat imageFormat)
		{
			filename = filename.ToLowerInvariant();
			Sitecore.Context.SetActiveSite("scs");
			_imageCache.TryGetValue(GetType(), out var cache);
			if (cache != null && cache.TryGetValue(filename, out var result)) return result;

			result = _manifestResourceStreamService.GetManifestResourceImage(GetType(), _registration.GetScsRegistration(GetType()).ResourcesPath + "." + filename, imageFormat, () => throw new ScsEmbeddedResourceNotFoundException());
			if (!_imageCache.ContainsKey(GetType()))
			{
				_imageCache[GetType()] = new ConcurrentDictionary<string, byte[]>();
			}
			_imageCache[GetType()][filename] = result;
			return result;
		}
	}
}
