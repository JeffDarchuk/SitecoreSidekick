using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Pipelines.HttpRequest;
using SitecoreSidekick.ContentTree;
using Sitecore.Diagnostics;
using System.Web.Configuration;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using Rainbow.Model;
using ScsEditingContext.Services.Interface;
using Sitecore.Pipelines.RenderField;

namespace ScsEditingContext.Pipelines.HttpRequestBegin
{
	public class RegisterEvent : HttpRequestProcessor
	{
		private readonly ISitecoreDataAccessService _sitecoreDataAccessService;
		public RegisterEvent()
		{
			_sitecoreDataAccessService = Bootstrap.Container.Resolve<ISitecoreDataAccessService>();
		}

		public override void Process(HttpRequestArgs args)
		{
			try
			{
				var guidString = args.Context.Request.Form["__PARAMETERS"];
				if (guidString != null)
				{
					int guidStart = guidString.IndexOf('"') + 1;
					int guidStop = guidString.LastIndexOf('"');
					if (guidStart != -1 && guidStop > guidStart)
					{
						guidString = guidString.Substring(guidStart, guidStop - guidStart);
						HttpCookie myCookie = args.Context.Request.Cookies["scseditorcontext" + Regex.Replace(Context.GetUserName(), "[^a-zA-Z0-9 -]", string.Empty)];

						if (!_sitecoreDataAccessService.TryGetItemData(guidString, out IItemData item))
							return;

						if (item != null)
						{
                            SessionStateSection SessionSettings = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");

                            EditingContextRegistration.Related[HttpContext.Current.Request.Cookies[SessionSettings.CookieName]?.Value ?? ""] = _sitecoreDataAccessService.GetItemReferences(item).Select(x=> new TypeContentTreeNode(x)).OrderBy(x => x.DisplayName).ToList();
							EditingContextRegistration.Referrers[HttpContext.Current.Request.Cookies[SessionSettings.CookieName]?.Value ?? ""] = _sitecoreDataAccessService.GetItemReferrers(item).Select(x => new TypeContentTreeNode(x)).OrderBy(x => x.DisplayName).ToList();
						}
						ContentTreeNode current = new ContentTreeNode(item, false);
						if (string.IsNullOrWhiteSpace(current.DisplayName))
							return;
						if (myCookie?.Value != null)
						{
							var list = GetValue(myCookie, current.Id);
							list.Add($"{current.Id}|{current.DatabaseName}|{current.DisplayName}|{current.Icon}");
							if (list.Count > 20)
								list.RemoveAt(0);
							SetValue(myCookie, string.Join(",", list));
							args.Context.Response.Cookies.Add(myCookie);
						}
						else
						{
							myCookie = new HttpCookie("scseditorcontext" + Regex.Replace(Context.GetUserName(), "[^a-zA-Z0-9 -]", string.Empty));
							SetValue(myCookie, $"{current.Id}|{current.DatabaseName}|{current.DisplayName}|{current.Icon}");
							myCookie.Expires = DateTime.Now.AddDays(1d);
							args.Context.Response.Cookies.Add(myCookie);
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Warn("unable to register action for SCS Editing Context", e, this);
			}
		}

		private List<string> GetValue(HttpCookie cookie, string currentId)
		{
			try
			{
				string txt = Encoding.UTF8.GetString(System.Convert.FromBase64String(cookie.Value));
				return txt.Split(',').Where(x => !x.StartsWith(currentId)).ToList();
			}
			catch (Exception)
			{
				return new List<string>(); //cookie not base64
			}
		}

		private void SetValue(HttpCookie cookie, string value)
		{
			cookie.Value = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
		}
	}
}
