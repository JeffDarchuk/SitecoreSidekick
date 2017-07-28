using System;
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

namespace ScsEditingContext.Pipelines.HttpRequestBegin
{
	public class RegisterEvent : HttpRequestProcessor
	{
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
						HttpCookie myCookie = args.Context.Request.Cookies["scseditorcontext" + Context.GetUserName()];
						var database = Context.ContentDatabase ?? Context.Database ?? Factory.GetDatabase("master");
						ID tmp;
						try
						{
							tmp = new ID(guidString);
						}
						catch (Exception)
						{
							return;
						}
						Item item = database.GetItem(tmp);
						if (item != null)
						{
                            SessionStateSection SessionSettings = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");

                            EditingContextRegistration.Related[HttpContext.Current.Request.Cookies[SessionSettings.CookieName]?.Value ?? ""] = Globals.LinkDatabase.GetItemReferences(item, true).Select(x => new TypeContentTreeNode(x.GetTargetItem())).OrderBy(x => x.DisplayName).ToList();
							EditingContextRegistration.Referrers[HttpContext.Current.Request.Cookies[SessionSettings.CookieName]?.Value ?? ""] = Globals.LinkDatabase.GetItemReferrers(item, true).Select(x => new TypeContentTreeNode(x.GetSourceItem())).OrderBy(x => x.DisplayName).ToList();
						}
						ContentTreeNode current = new ContentTreeNode(item, false);
						if (string.IsNullOrWhiteSpace(current.DisplayName))
							return;
						if (myCookie?.Value != null)
						{
							var list = myCookie.Value.Split(',').Where(x => !x.StartsWith(current.Id)).ToList();
							list.Add($"{current.Id}|{current.DatabaseName}|{current.DisplayName}|{current.Icon}");
							if (list.Count > 20)
								list.RemoveAt(0);
							myCookie.Value = string.Join(",", list);
							args.Context.Response.Cookies.Add(myCookie);
						}
						else
						{
							myCookie = new HttpCookie("scseditorcontext" + Context.GetUserName());
							myCookie.Value = HttpUtility.UrlEncode($"{current.Id}|{current.DatabaseName}|{current.DisplayName}|{current.Icon}");
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
	}
}
