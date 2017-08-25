using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Rainbow.Diff.Fields;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using ScsContentMigrator.Args;
using ScsContentMigrator.Models;
using ScsContentMigrator.Services;
using ScsContentMigrator.Services.Interface;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using SitecoreSidekick.ContentTree;
using SitecoreSidekick.Shared.IoC;
using Version = Sitecore.Data.Version;

namespace ScsContentMigrator.Data
{
	public class CompareContentTreeNode : ContentTreeNode
	{
		private static readonly List<IFieldComparer> Comparers = new List<IFieldComparer>();
		private readonly IRemoteContentService _remoteContent;
		static CompareContentTreeNode()
		{
			Comparers.Add(new CheckboxComparison());
			Comparers.Add(new MultiLineTextComparison());
			Comparers.Add(new MultilistComparison());
			Comparers.Add(new XmlComparison());
			Comparers.Add(new DefaultComparison());			
		}

		public string Data;
		public List<Tuple<string, string>> Status;
		public Dictionary<string, List<Tuple<string, string>>> Compare;
		public bool ChildChanged;
		public bool MissingRemote;
		public int Checksum;
		public string Revision;

		public CompareContentTreeNode()
		{
			_remoteContent = Bootstrap.Container.Resolve<IRemoteContentService>();
		}

		public CompareContentTreeNode(IRemoteContentService remoteContent)
		{
			_remoteContent = remoteContent;
		}
		public CompareContentTreeNode(Item item, bool open = true) : base(item, open)
		{
			SortedSet<string> tmp = new SortedSet<string>();
			foreach (Item version in item.Versions.GetVersions(true))
			{
				tmp.Add(version[FieldIDs.Revision]);
			}
			Revision = string.Join("", tmp);
			Checksum = ContentMigrationRegistration.GetChecksum(item.ID.ToString());
			_remoteContent = Bootstrap.Container.Resolve<IRemoteContentService>();

		}

		private bool AreFieldsEqual(Field local, IItemFieldValue remote)
		{
			var localField = new ItemFieldValue(local, local.Value);
			foreach (IFieldComparer comparer in Comparers)
			{
				if (!comparer.CanCompare(localField, remote)) continue;

				return comparer.AreEqual(localField, remote);
			}
			return false;
		}

		public void BuildDiff(string server)
		{
			Compare = new Dictionary<string, List<Tuple<string, string>>>();
			IItemData itemData = null;
			itemData = _remoteContent.GetRemoteItemData(Guid.Parse(Id), server);
				using (new SecurityDisabler())
			{
				var localItem = Factory.GetDatabase("master", true).DataManager.DataEngine.GetItem(new ID(Id), LanguageManager.DefaultLanguage, Sitecore.Data.Version.Latest);

				foreach (var chk in itemData.SharedFields)
				{

					if (AreFieldsEqual(localItem.Fields[new ID(chk.FieldId)], chk))
					{
						continue;
					}

					if (!Compare.ContainsKey("shared"))
					{
						Compare["shared"] = new List<Tuple<string, string>>();
					}

					Compare["shared"].Add(chk.BlobId != null
						? new Tuple<string, string>(chk.NameHint, "Blob value changed")
						: new Tuple<string, string>(chk.NameHint, HtmlDiff.HtmlDiff.Execute(HttpUtility.HtmlEncode(localItem[new ID(chk.FieldId)].Replace("\r", "")), HttpUtility.HtmlEncode(chk.Value.Replace("\r", "")))));
				}
				Dictionary<string, int> tracker = new Dictionary<string, int>();
				foreach (var ver in itemData.Versions)
				{
					if (!tracker.ContainsKey(LanguageManager.GetLanguage(ver.Language.Name).Name))
					{
						tracker.Add(LanguageManager.GetLanguage(ver.Language.Name).Name, 1);
					}
					else
					{
						tracker[LanguageManager.GetLanguage(ver.Language.Name).Name]++;
					}
					Item verItem = localItem.Database.DataManager.DataEngine.GetItem(new ID(Id), LanguageManager.GetLanguage(ver.Language.Name), Version.Parse(ver.VersionNumber));
					foreach (var verfield in ver.Fields)
					{
						if (AreFieldsEqual(verItem.Fields[new ID(verfield.FieldId)], verfield))
						{
							continue;
						}
						string key = ver.Language.Name + " v" + ver.VersionNumber;
						if (!Compare.ContainsKey(key))
						{
							Compare[key] = new List<Tuple<string, string>>();
						}
						Compare[key].Add(verfield.BlobId != null
							? new Tuple<string, string>(verfield.NameHint, "Blob value changed")
							: new Tuple<string, string>(verfield.NameHint, HtmlDiff.HtmlDiff.Execute(HttpUtility.HtmlEncode(localItem[new ID(verfield.FieldId)].Replace("\r", "")), HttpUtility.HtmlEncode(verfield.Value.Replace("\r", "")))));
					}
				}
				foreach (var lang in localItem.Languages.Where(x => !tracker.ContainsKey(x.Name)))
				{
					Item langItem = localItem.Database.DataManager.DataEngine.GetItem(new ID(Id), lang, Version.Latest);
					for (int ver = 1; ver <= langItem.Versions.Count; ver++)
					{
						string key = "Extra version";
						if (!Compare.ContainsKey(key))
						{
							Compare[key] = new List<Tuple<string, string>>();
						}
						Compare[key].Add(new Tuple<string, string>("Extra local version exists that's not on the remote.", $"{lang.Name} v{ver}"));
					}
				}
				foreach (var lang in localItem.Languages.Where(x => tracker.ContainsKey(x.Name)))
				{
					Item langItem = localItem.Database.DataManager.DataEngine.GetItem(new ID(Id), lang, Version.Latest);
					for (int ver = tracker[lang.Name]+1; ver <= langItem.Versions.Count; ver++)
					{
						string key = "Extra version";
						if (!Compare.ContainsKey(key))
						{
							Compare[key] = new List<Tuple<string, string>>();
						}
						Compare[key].Add(new Tuple<string, string>("Extra local version exists that's not on the remote.", $"{lang.Name} v{ver}"));
					}
				}
				foreach (var unver in itemData.UnversionedFields)
				{
					Item verItem = localItem.Database.DataManager.DataEngine.GetItem(new ID(Id), LanguageManager.GetLanguage(unver.Language.Name), Version.Latest);
					foreach (var unverfield in unver.Fields)
					{
						if (AreFieldsEqual(verItem.Fields[new ID(unverfield.FieldId)], unverfield))
						{
							continue;
						}
						if (!Compare.ContainsKey(unver.Language.Name))
						{
							Compare[verItem.Language.Name] = new List<Tuple<string, string>>();
						}
						Compare[unver.Language.Name].Add(unverfield.BlobId != null
							? new Tuple<string, string>(unverfield.NameHint, "Blob value changed")
							: new Tuple<string, string>(unverfield.NameHint, HtmlDiff.HtmlDiff.Execute(HttpUtility.HtmlEncode(localItem[new ID(unverfield.FieldId)].Replace("\r", "")), HttpUtility.HtmlEncode(unverfield.Value.Replace("\r", "")))));
					}
				}
			}

		}
		public void SimpleCompare(string database, string itemId)
		{
			try
			{
				using (new SecurityDisabler())
				{
					Status = new List<Tuple<string, string>>();
					var localItem = Factory.GetDatabase(database, true).DataManager.DataEngine.GetItem(new ID(itemId), LanguageManager.DefaultLanguage, Sitecore.Data.Version.Latest);
					if (localItem == null)
					{
						Status.Add(new Tuple<string, string>("cmmissing", "This content item only exists on the remote server."));
						return;
					}

					ChildChanged = Checksum != ContentMigrationRegistration.GetChecksum(localItem.ID.ToString());
					CompareContentTreeNode local = new CompareContentTreeNode(localItem);
					if (Revision != local.Revision)
					{
						Status.Add(new Tuple<string, string>("cmfieldchanged", "This content item exists on the local server, however the fields have different values."));
					}
					else
					{
						Status.Add(new Tuple<string, string>("cmequal", "This content item is equivalent on the source and target servers."));
					}
					if (ChildChanged)
					{
						Status.Add(new Tuple<string, string>("cmchildchanged", "There are changes in the children of this item."));
					}
					HashSet<string> tracker = new HashSet<string>();

					foreach (ContentTreeNode child in Nodes)
					{
						tracker.Add(child.Id);
					}

					foreach (Item child in localItem.Children)
					{
						if (tracker.Contains(child.ID.ToString())) continue;
						var newnode = new CompareContentTreeNode(child, false)
						{
							Status = new List<Tuple<string, string>>
								{
									new Tuple<string, string>("cmextra", "This item only exists on the local server.")
								},
							MissingRemote = true
						};
						Nodes.Add(newnode);
					}

				}
			}
			catch (Exception e)
			{
				Log.Error("Problem assembling diff for " + itemId, e, this);
			}
		}
	}
}
