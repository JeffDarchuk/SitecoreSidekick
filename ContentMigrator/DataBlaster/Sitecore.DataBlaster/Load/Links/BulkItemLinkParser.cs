using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sitecore.Data;

namespace Sidekick.ContentMigrator.DataBlaster.Sitecore.DataBlaster.Load.Links
{
	/// <summary>
	/// Parses out links in content of bulk items.
	/// </summary>
    public class BulkItemLinkParser
    {
        protected Lazy<Regex> IdRegex = new Lazy<Regex>(() =>
            new Regex(@"\{[0-9A-Z]{8}-[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{12}\}",
                RegexOptions.Compiled | RegexOptions.IgnoreCase));

        protected Lazy<Regex> LinkRegex = new Lazy<Regex>(() =>
            new Regex(@"(?<=~/link\.aspx\?_id=)[0-9A-Z]{32}(?=&amp)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase));

        protected Lazy<HashSet<string>> IgnoredFields = new Lazy<HashSet<string>>(() =>
            new HashSet<string>(new[]
            {
                "__Revision"
            }, StringComparer.OrdinalIgnoreCase));

        public virtual IEnumerable<BulkLoadItem> ExtractLinks(IEnumerable<BulkLoadItem> bulkItems, BulkLoadContext context, 
			LinkedList<BulkItemLink> links)
        {
            foreach (var item in bulkItems)
            {
                ExtractLinks(context, item, links);
                yield return item;
            }
        }

        protected virtual void ExtractLinks(BulkLoadContext context, BulkLoadItem item, LinkedList<BulkItemLink> links)
        {
            foreach (var field in item.Fields)
            {
                ExtractLinks(context, item, field, links);
            }
        }

	    protected virtual void ExtractLinks(BulkLoadContext context, BulkLoadItem item, BulkField field, LinkedList<BulkItemLink> links)
        {
            if (string.IsNullOrWhiteSpace(field.Value)) return;
            if (IgnoredFields.Value.Contains(field.Name)) return;

            var ids = IdRegex.Value.Matches(field.Value).Cast<Match>()
                .Select(x =>
                {
                    ID id;
                    return ID.TryParse(x.Value, out id) ? id : (ID)null;
                })
                .Concat(LinkRegex.Value.Matches(field.Value).Cast<Match>().Select(x =>
                {
                    Guid guid;
                    return Guid.TryParse(x.Value, out guid) ? new ID(guid) : (ID)null;
                }))
                .Where(x => x != (ID) null);

            foreach (var link in ids
                .Select(x => new BulkItemLink(
                    context.Database, new ID(item.Id), new ID(field.Id),
                    context.Database, x, x.ToString())))
            {
                link.ItemAction = item.LoadAction;
                links.AddLast(link);
            }
        }
    }
}