using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Sitecore.Mvc.Extensions;
using Sitecore.Pipelines;
using SitecoreSidekick.Core;
using SitecoreSidekick.Pipelines.HttpRequestBegin;

namespace SitecoreSidekick.Handlers
{

	public abstract class ScsHandler : Controller, ISidekick
	{
		private readonly ConcurrentDictionary<string, string> _resourceCache = new ConcurrentDictionary<string, string>();
		private readonly ConcurrentDictionary<string, byte[]> _imageCache = new ConcurrentDictionary<string, byte[]>();

		public virtual bool IsReusable => true;
		public bool AdminOnly { get; }
		public List<string> Roles { get; }
		public List<string> Users { get; }

		protected ScsHandler()
		{
		}

		protected ScsHandler(string roles, string isAdmin, string users)
		{
			AdminOnly = isAdmin == "true";
			Roles = roles.Split('|').Where(x => !x.IsWhiteSpaceOrNull()).ToList();
			Users = users.Split('|').Where(x => !x.IsWhiteSpaceOrNull()).ToList();
		}
		public void Process(PipelineArgs args)
		{
			ScsMainHandlerController.RegisterSideKick(this);
		}

		public string CompileEmbeddedResource(string fileExtension)
		{
			StringBuilder sb = new StringBuilder();

			foreach (var resource in GetType().Assembly.GetManifestResourceNames().Where(x => x.EndsWith(fileExtension) && x.StartsWith(ResourcesPath)).Select(x => x.Substring(ResourcesPath.Length + 1)))
			{
				if (!resource.Equals("scsangular.js"))
				{
					sb.Append(GetResource(resource));
				}
			}

			return sb.ToString();
		}

		public void ProcessResourceRequest(HttpContextBase context, string filename, dynamic data)
		{
			if (filename.EndsWith(".scs"))
				ReturnResource(context, filename, "text/html");
			else if (filename.EndsWith(".html"))
				ReturnResource(context, filename, "text/html");
			else if (filename.EndsWith(".gif"))
				ReturnImage(context, filename, ImageFormat.Gif, "image/gif");
			else if (filename.EndsWith(".png"))
				ReturnImage(context, filename, ImageFormat.Png, "image/png");
			else if (filename.EndsWith(".jpg"))
				ReturnImage(context, filename, ImageFormat.Jpeg, "image/jpg");
			else if (filename.EndsWith(".bmp"))
				ReturnImage(context, filename, ImageFormat.Bmp, "image/bmp");
			else if (filename.EndsWith(".emf"))
				ReturnImage(context, filename, ImageFormat.Emf, "image/emf");
			else if (filename.EndsWith(".ico"))
				ReturnImage(context, filename, ImageFormat.Icon, "image/icon");
			else if (filename.EndsWith(".tiff"))
				ReturnImage(context, filename, ImageFormat.Tiff, "image/tiff");
			else if (filename.EndsWith(".wmf"))
				ReturnImage(context, filename, ImageFormat.Wmf, "image/wmf");
			else if (filename.EndsWith(".svg"))
				ReturnResource(context, filename, "image/svg+xml");
			else if (filename.EndsWith(".js"))
				ReturnResource(context, filename, "text/javascript");
		}

		public bool ApplicableSidekick()
		{
			bool admin = IsAdmin.CurrentUserAdmin();
			if (admin)
				return true;
			if (AdminOnly)
				return false;
			if (Roles.Count == 0)
				return true;
			return IsAdmin.CurrentUserInRoleList(Roles);
		}

		/// <summary>
		/// gets post data from post request
		/// </summary>
		/// <param name="instream"></param>
		/// <returns>dynamic object containing post javascript object</returns>
		public static dynamic GetPostData(Stream instream)
		{
			instream.Seek(0, SeekOrigin.Begin);
			using (StreamReader sr = new StreamReader(instream))
			{
				string payload = sr.ReadToEnd();
				dynamic ret = null;
				if (payload.StartsWith("{") || payload == "")
					ret = JsonNetWrapper.DeserializeObject<ExpandoObject>(payload);
				if (ret == null)
					ret = new ExpandoObject();
				ret.payload = payload;
				return ret;
			}
		}
		/// <summary>
		/// processes http request
		/// </summary>
		/// <param name="context"></param>
		public void ProcessRequest(HttpContext context, string filename, dynamic data)
		{
			ProcessRequest(new HttpContextWrapper(context), filename, data);
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
			using (var stream = GetType().Assembly.GetManifestResourceStream(ResourcesPath + "." + filename))
			{
				if (stream != null)
				{
					using (var reader = new StreamReader(stream))
					{
						result = reader.ReadToEnd();
					}
				}
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
			using (var stream = GetType().Assembly.GetManifestResourceStream(ResourcesPath + "." + filename))
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
			}

			_imageCache[filename] = result;
			return result;
		}

		public abstract string Directive { get; set; }
		public abstract NameValueCollection DirectiveAttributes { get; set; }
		public abstract string ResourcesPath { get; set; }
		public abstract string Icon { get; }
		public abstract string Name { get; }
		public abstract string CssStyle { get; }
		public abstract ActionResult ProcessRequest(HttpContextBase context, string filename, dynamic data);
		public virtual bool RequestValid(HttpContextBase context, string filename, dynamic data)
		{
			var user = Sitecore.Context.User;
			if (!user.IsAuthenticated)
			{
				return false;
			}

			return true;
		}
	}
}
