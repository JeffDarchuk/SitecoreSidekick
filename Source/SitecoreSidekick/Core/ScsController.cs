using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Web;
using System.Web.Mvc;
using SitecoreSidekick.Services.Interface;
using SitecoreSidekick.Shared.IoC;

namespace SitecoreSidekick.Core
{
	public abstract class ScsController : Controller
	{
		private readonly ConcurrentDictionary<string, string> _resourceCache = new ConcurrentDictionary<string, string>();
		private readonly ConcurrentDictionary<string, byte[]> _imageCache = new ConcurrentDictionary<string, byte[]>();
		private readonly IScsRegistrationService _registration;

		protected ScsController()
		{
			_registration = Bootstrap.Container.Resolve<IScsRegistrationService>();
		}

		[ScsLoggedIn]
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
			}catch(ScsEmbeddedResourceNotFoundException){}
			Response.StatusCode = 404;
			return Content("Requested resource not found");

		}
		/// <summary>
		/// sets up the context for specific content
		/// </summary>
		/// <param name="context"></param>
		/// <param name="message"></param>
		/// <param name="contentType"></param>
		/// <param name="status"></param>
		/// <param name="endResponse"></param>
		protected void ReturnResponse(HttpContextBase context, string message, string contentType = "text/plain", HttpStatusCode status = HttpStatusCode.OK, bool endResponse = false)
		{
			if (!string.IsNullOrWhiteSpace(message)) context.Response.Write(message);
			context.Response.StatusCode = (int)status;
			context.Response.ContentType = contentType;
			if (endResponse) context.Response.End();
		}

		/// <summary>
		/// return not found response
		/// </summary>
		/// <param name="context"></param>
		/// <param name="message"></param>
		protected void NotFound(HttpContextBase context, string message = null)
		{
			ReturnResponse(context, message, status: HttpStatusCode.NotFound);
		}

		/// <summary>
		/// returns an error response
		/// </summary>
		/// <param name="context"></param>
		/// <param name="e"></param>
		/// <param name="message"></param>
		protected void Error(HttpContextBase context, Exception e, string message = null)
		{
			message = string.IsNullOrWhiteSpace(message) ? e.ToString() : message + "\r\n" + e;
			ReturnResponse(context, message, status: HttpStatusCode.InternalServerError);
		}

		/// <summary>
		/// return specific file resource stored in the binary
		/// </summary>
		/// <param name="context"></param>
		/// <param name="file"></param>
		/// <param name="contentType"></param>
		protected void ReturnResource(HttpContextBase context, string file, string contentType)
		{
			var result = GetResource(file);
			if (!string.IsNullOrWhiteSpace(result))
				ReturnResponse(context, result, contentType);
		}

		/// <summary>
		/// return image resource from the binary
		/// </summary>
		/// <param name="context"></param>
		/// <param name="file"></param>
		/// <param name="imageFormat"></param>
		/// <param name="contentType"></param>
		protected void ReturnImage(HttpContextBase context, string file, ImageFormat imageFormat, string contentType)
		{
			var buffer = GetImage(file, imageFormat);
			if (buffer == null || !buffer.Any()) return;
			context.Response.StatusCode = 200;
			context.Response.ContentType = "image/png";
			context.Response.BinaryWrite(buffer);
			context.Response.Flush();
		}

		/// <summary>
		/// return json resource
		/// </summary>
		/// <param name="context"></param>
		/// <param name="o"></param>
		protected void ReturnJson(HttpContextBase context, object o)
		{
			if (o == null) return;
			context.Response.StatusCode = 200;
			var json = JsonNetWrapper.SerializeObject(o);
			context.Response.AppendHeader("Cache-Control", "no-cache, no-store, must-revalidate"); // HTTP 1.1.
			context.Response.AppendHeader("Pragma", "no-cache"); // HTTP 1.0.
			context.Response.AppendHeader("Expires", "0"); // Proxies.
			ReturnResponse(context, json, "application/json");
		}

		public virtual ActionResult ScsJson(object o)
		{
			return Content(JsonNetWrapper.SerializeObject(o), "application/json");
		}
		/// <summary>
		/// extracts the resource out of the binary
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public string GetResource(string filename)
		{
			filename = filename.ToLowerInvariant();
			string result;
			if (_resourceCache.TryGetValue(filename, out result)) return result;
			using (var stream = GetType().Assembly.GetManifestResourceStream(_registration.GetScsRegistration(GetType()).ResourcesPath + "." + filename))
			{
				if (stream != null)
				{
					using (var reader = new StreamReader(stream))
					{
						result = reader.ReadToEnd();
					}
				}else throw new ScsEmbeddedResourceNotFoundException();
			}

			_resourceCache[filename] = result;
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
			byte[] result;
			if (_imageCache.TryGetValue(filename, out result)) return result;
			using (var stream = GetType().Assembly.GetManifestResourceStream(_registration.GetScsRegistration(GetType()).ResourcesPath + "." + filename))
			{
				if (stream != null)
				{
					using (var ms = new MemoryStream())
					{
						var bmp = new Bitmap(stream);
						bmp.Save(ms, imageFormat);
						result = ms.ToArray();
					}
				}
				else throw new ScsEmbeddedResourceNotFoundException();
			}

			_imageCache[filename] = result;
			return result;
		}
	}
}
