using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using ScsAuditLog.Model;
using ScsAuditLog.Model.Interface;
using ScsAuditLog.Pipelines;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;

namespace ScsAuditLog.Core
{
	public class AuditLog
	{
		private Dictionary<string, AuditStorage> _storage = new Dictionary<string, AuditStorage>();
		private Dictionary<string, IEventType> _types = new Dictionary<string, IEventType>();
		AuditTrie<string> trie = new AuditTrie<string>(null);
		private bool _updateLog = false;
		private object locker = new object();
		private HashSet<string> users = new HashSet<string>(); 
		private static Analyzer analyzer = new StandardAnalyzer(Version.LUCENE_29);
		private static readonly MultiFieldQueryParser Parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_29,
				new[] { "content", "date" },
				new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29));
		private int _optimizeTimer = 0;
		ConcurrentQueue<Document> writeQueue = new ConcurrentQueue<Document>(); 
		//private IndexWriter _writer;
		private Directory _dir;
		private int _logDays;
		private int _recordDays;
		private bool _clearOld = false;
		private HashSet<string> _backupDays = new HashSet<string>(); 
		public AuditLog(int DaysToKeepLog, int DaysToKeepRecords)
		{
			string dir = GetDataDirectory();
			_logDays = DaysToKeepLog;
			_recordDays = DaysToKeepRecords;
			Task.Run(() =>
			{
				while (IsFileLocked(new FileInfo($"{dir}/write.lock")))
				{
					Thread.Sleep(500);
				}
				if (File.Exists($"{dir}/write.lock"))
					File.Delete($"{dir}/write.lock");
				_dir = FSDirectory.Open(new DirectoryInfo(GetDataDirectory()));
				
				var reader = GetSearcher();
				var terms = reader.IndexReader.Terms();
				while (terms.Next())
				{
					if (terms.Term.Field == "content")
						trie[terms.Term.Text] = terms.Term.Text;
					if (terms.Term.Field == "user")
						users.Add(terms.Term.Text);
				}
				ValidateBackup();
			});
		}
		protected virtual bool IsFileLocked(FileInfo file)
		{
			if (!file.Exists)
				return false;
			FileStream stream = null;

			try
			{
				stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
			}
			catch (IOException)
			{
				//the file is unavailable because it is:
				//still being written to
				//or being processed by another thread
				//or does not exist (has already been processed)
				return true;
			}
			finally
			{
				stream?.Close();
			}

			//file is not locked
			return false;
		}
		public void RegisterEventType(IEventType eventType)
		{
			_types.Add(eventType.Id, eventType);
		}
		public IEventType GetEventType(string id)
		{
			if (_types.ContainsKey(id))
				return _types[id];
			return null;
		}

		public HashSet<string> GetUsers()
		{
			return users;
		} 
		public IDictionary<string, IEventType> GetAllEventTypes()
		{
			return _types;
		}
		public void Log(Item item, string typeId, string note = "")
		{
			Log(item, AuditLogger.Current.GetEventType(typeId), note);
		}
		public void Log(Item item, IEventType type, string note = "")
		{
			Log(item, type.Id, type.Color, type.Label, note);
		}
		public void Log(Item item, string eventId, string color, string label, string note = "")
		{
			try
			{
				string dateKey = DateTime.Now.ToString("yyyyMMdd");
				ItemAuditEntry entry = new ItemAuditEntry(eventId, label, color, item);
				entry.Note = note;
				StringBuilder sb = new StringBuilder();
				if (item != null)
				{
					var fieldList = item.Fields.Where(x => !x.Name.StartsWith("__")).Select(x => x.Value);
					foreach (string str in fieldList)
					{
						sb.Append(str);
						sb.Append("|");
					}
				}
				Log(entry, sb.ToString());
			}
			catch (Exception e)
			{
				Sitecore.Diagnostics.Log.Error("issue writing item log audit log", e, this);
			}
		}

		public void Log(IAuditEntry entry, string content = "")
		{

			Document doc = new Document();
			AddField(doc, "user", entry.User.ToLower(), Field.Index.NOT_ANALYZED);
			if (!users.Contains(entry.User.ToLower()))
				users.Add(entry.User.ToLower());
			AddField(doc, "path", entry.Path, Field.Index.ANALYZED);
			AddField(doc, "id", entry.Id.ToShortID().ToString().ToLower(), Field.Index.ANALYZED);
			AddField(doc, "date", entry.TimeStamp.ToString("yyyyMMdd"), Field.Index.ANALYZED);
			AddField(doc, "timestamp", entry.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fff"), Field.Index.ANALYZED);
			AddField(doc, "note", entry.Note, Field.Index.ANALYZED);
			AddField(doc, "database", entry.Database, Field.Index.ANALYZED);
			foreach (var role in entry.Role)
				AddField(doc, "role", role.ToLower(), Field.Index.NOT_ANALYZED);
			AddField(doc, "event", entry.EventId, Field.Index.ANALYZED);
			if (!string.IsNullOrWhiteSpace(content))
				AddField(doc, "content", content, Field.Index.ANALYZED);
			writeQueue.Enqueue(doc);
			KickOptimizeTimer();
		}

		private void KickOptimizeTimer()
		{
			if (_optimizeTimer == 0)
			{
				_optimizeTimer = 100;
				Task.Run(() =>
				{
					ValidateBackup();
					using (var _writer = new IndexWriter(_dir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
					{
						if (_recordDays > 0 && _clearOld)
						{
							var query = Parser.Parse($"date:[0 TO {DateTime.Now.AddDays(_recordDays*-1).ToString("yyyyMMdd")}]");
							_writer.DeleteDocuments(query);
							_clearOld = false;
						}
						while (_optimizeTimer > 1)
						{
							while (writeQueue.Any())
							{
								Document doc;
								if (writeQueue.TryDequeue(out doc))
									_writer.AddDocument(doc);
							}
							_optimizeTimer--;
							Thread.Sleep(100);
						}
						_writer.Commit();
						_writer.Optimize();
						_optimizeTimer = 0;
					}
				});
			}
			else
			{
				_optimizeTimer = 100;
			}
		}

		private void AddField(Document doc, string name, string value, Field.Index type)
		{
			Field f = new Field(name, value, Field.Store.YES, type);
			doc.Add(f);
		}

		public static string GetDataDirectory()
		{
			string filepath = "";
			if (System.Text.RegularExpressions.Regex.IsMatch(Settings.DataFolder, @"^(([a-zA-Z]:\\)|(//)).*")) //if we have an absolute path, rather than relative to the site root
				filepath = Settings.DataFolder +
						   @"\AuditLog";
			else
				filepath = HttpRuntime.AppDomainAppPath + Settings.DataFolder.Substring(1) +
						   @"\AuditLog";
			if (!System.IO.Directory.Exists(filepath))
				System.IO.Directory.CreateDirectory(filepath);
			return filepath;
		}

		public TopDocs Query(string query, IndexSearcher searcher = null)
		{
			if (searcher == null)
				searcher = GetSearcher();
			try
			{
				var lq = Parser.Parse(query);
				var sorter = new Sort(new SortField("timestamp", SortField.LONG, true));
				return searcher.Search(lq, null, int.MaxValue, sorter);
			}
			catch (Exception e)
			{
				Sitecore.Diagnostics.Log.Error("issue querying the index", e, this);
			}
			return null;
		}

		public IndexSearcher GetSearcher()
		{
			return new IndexSearcher(_dir, false);
		}
		public TopDocs QueryIds(DateTime start, DateTime end, string query, IndexSearcher searcher = null)
		{
			if (searcher == null)
				searcher = GetSearcher();
			try
			{
				var lq = Parser.Parse(query + $" AND date:[{start.ToString("yyyyMMdd")} TO {end.ToString("yyyyMMdd")}]");
				return searcher.Search(lq, int.MaxValue);
			}
			catch (Exception e)
			{
				Sitecore.Diagnostics.Log.Error("issue querying the index", e, this);
			}
			return null;
		}

		public IEnumerable<KeyValuePair<string, int>> AutoComplete(string text, string start, string end, List<object> eventTypes)
		{
			StringBuilder types = new StringBuilder();
			foreach (var eventType in eventTypes)
			{
				types.Append("event:");
				types.Append(eventType);
				types.Append(" OR ");
			}
			if (types.Length > 4)
				types.Remove(types.Length - 4, 4);
			return trie.Autocomplete(text,
				x => x == null ? 0 : QueryIds(DateTime.Parse(start), DateTime.Parse(end), "(content:" + x + " OR user:" + x + ")" + (types.Length > 0 ? " AND ("+types.ToString()+")" : "")).TotalHits, 10);
			//return new KeyValuePair<string, int>[0];
		}

		public IAuditEntry GetDocument(string dateKey, string first)
		{
			return _storage[dateKey].Documents[first];
		}

		private void ValidateBackup()
		{
			if (_logDays == -1)
				return;
			var date = DateTime.Now.ToString("yyyy-MMM-dd")+".zip";
			if (!_backupDays.Contains(date))
			{
				_clearOld = true;
				string dir = GetDataDirectory();
				string backupDir = dir + "-backup";
				if (!System.IO.Directory.Exists(backupDir))
					System.IO.Directory.CreateDirectory(backupDir);
				if (!File.Exists(backupDir + "\\" + date))
				{
					ZipFile.CreateFromDirectory(dir, backupDir + "\\" + date);
				}
				if (_logDays > 0)
				{
					for (int i = 0; i < _logDays; i++)
					{
						_backupDays.Add(DateTime.Now.AddDays(i * -1).ToString("yyyy-MMM-dd") + ".zip");
					}
					foreach (string backup in System.IO.Directory.EnumerateFiles(backupDir))
					{
						if (!_backupDays.Contains(Path.GetFileName(backup)))
							File.Delete(backup);
					}
				}
			}
		}
	}
}
