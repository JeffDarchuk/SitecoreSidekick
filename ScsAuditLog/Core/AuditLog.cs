using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using SitecoreSidekick.Services.Interface;
using Directory = Lucene.Net.Store.Directory;
using Version = Lucene.Net.Util.Version;

namespace ScsAuditLog.Core
{
	public class AuditLog
	{
		private readonly IJsonSerializationService _jsonSerializationService;
		private readonly Dictionary<string, AuditStorage> _storage = new Dictionary<string, AuditStorage>();
		private readonly Dictionary<string, IEventType> _types = new Dictionary<string, IEventType>();
		readonly AuditTrie<string> _trie = new AuditTrie<string>();
		private readonly object _locker = new object();
		private readonly HashSet<string> _users = new HashSet<string>(); 
		private static readonly Analyzer Analyzer = new StandardAnalyzer(Version.LUCENE_29);
		private static readonly MultiFieldQueryParser Parser = new MultiFieldQueryParser(Version.LUCENE_29,
				new[] { "content", "date" },
				new StandardAnalyzer(Version.LUCENE_29));
		private int _optimizeTimer = 0;

		readonly ConcurrentQueue<Tuple<AuditSourceRecord, Document>> _writeQueue = new ConcurrentQueue<Tuple<AuditSourceRecord, Document>>(); 

		private Directory _dir;
		private readonly int _logDays;
		private readonly int _recordDays;
		private bool _clearOld = false;
		private readonly HashSet<string> _backupDays = new HashSet<string>();
		private static string _dataDirectory = "";
		internal bool Rebuilding = false;
		internal int Rebuilt = -1;

		public AuditLog(int daysToKeepLog, int daysToKeepRecords)
		{
			_jsonSerializationService = Bootstrap.Container.Resolve<IJsonSerializationService>();
			string dir = GetDataDirectory();
			_logDays = daysToKeepLog;
			_recordDays = daysToKeepRecords;
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
						_trie[terms.Term.Text] = terms.Term.Text;
					if (terms.Term.Field == "user")
						_users.Add(terms.Term.Text);
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
			return _users;
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

		public void Log(IAuditEntry entry, string content = "", bool newRecord = true)
		{

			Document doc = new Document();
			AddField(doc, "user", entry.User.ToLower(), Field.Index.NOT_ANALYZED);
			if (!_users.Contains(entry.User.ToLower()))
				_users.Add(entry.User.ToLower());
			AddField(doc, "path", entry.Path, Field.Index.ANALYZED);
			AddField(doc, "id", entry.Id.ToLower(), Field.Index.ANALYZED);
			AddField(doc, "date", entry.TimeStamp.ToString("yyyyMMdd"), Field.Index.ANALYZED);
			AddField(doc, "timestamp", entry.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fff"), Field.Index.ANALYZED);
			AddField(doc, "note", entry.Note, Field.Index.ANALYZED);
			AddField(doc, "database", entry.Database, Field.Index.ANALYZED);
			foreach (var role in entry.Role)
				AddField(doc, "role", role.ToLower(), Field.Index.NOT_ANALYZED);
			AddField(doc, "event", entry.EventId, Field.Index.ANALYZED);
			if (!string.IsNullOrWhiteSpace(content))
				AddField(doc, "content", content, Field.Index.ANALYZED);
			_writeQueue.Enqueue(newRecord
				? new Tuple<AuditSourceRecord, Document>(new AuditSourceRecord(entry as ItemAuditEntry, content), doc)
				: new Tuple<AuditSourceRecord, Document>(null, doc));
			KickOptimizeTimer();
		}

		private void WriteSource(AuditSourceRecord record, StringBuilder sb)
		{
			if (record.Entry == null) return;
			sb.Append("<|||>" + _jsonSerializationService.SerializeObject(record) + "<|||>");
		}

		public void Rebuild()
		{
			if (Rebuilding) return;
			Rebuilt = 0;
			Task.Run(() =>
			{
				Rebuilding = true;
				lock (_locker)
				{
					_dir.ClearLock("rebuilding");
					using (var writer = new IndexWriter(_dir, Analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
					{
						writer.DeleteAll();
					}
				}
				DateTime start = DateTime.Now.Subtract(TimeSpan.FromDays(_recordDays));
				while (start < DateTime.Now.AddDays(1))
				{
					string file = _dataDirectory + "/source/" + start.ToString("yyyy-MMM-dd") + "/source.src";
					if (File.Exists(file))
					{
						byte[] txt = File.ReadAllBytes(file);
						foreach (string entry in StringZipper.Unzip(txt).Split(new[] {"<|||>"}, StringSplitOptions.RemoveEmptyEntries))
						{
							AuditSourceRecord record = _jsonSerializationService.DeserializeObject<AuditSourceRecord>(entry);
							Task.Delay(1000).Wait();
							Log(record.Entry, record.Content, false);
							Rebuilt++;
						}
					}
					start = start.AddDays(1);
				}
				Rebuilt = -1;
				Rebuilding = false;
			});
		}
		private void KickOptimizeTimer()
		{
			if (_optimizeTimer == 0)
			{
				_optimizeTimer = 100;
				Task.Run(() =>
				{
					try
					{
						ValidateBackup();
						lock (_locker)
						{
							StringBuilder sb = new StringBuilder();
							using (var writer = new IndexWriter(_dir, Analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
							{
								if (_recordDays > 0 && _clearOld)
								{
									var query = Parser.Parse($"date:[0 TO {DateTime.Now.AddDays(_recordDays*-1):yyyyMMdd}]");
									writer.DeleteDocuments(query);
									_clearOld = false;
								}
								while (_optimizeTimer > 1)
								{
									while (_writeQueue.Any())
									{
										Tuple<AuditSourceRecord, Document> doc;
										if (_writeQueue.TryDequeue(out doc))
										{
											writer.AddDocument(doc.Item2);
											if (doc.Item1 != null)
												WriteSource(doc.Item1, sb);
										}
									}
									_optimizeTimer--;
									Thread.Sleep(100);
								}
								try
								{
									string dir = _dataDirectory + "/source/" + DateTime.Now.ToString("yyyy-MMM-dd");
									if (!System.IO.Directory.Exists(dir))
										System.IO.Directory.CreateDirectory(dir);
									if (!File.Exists(dir + "/source.src"))
										File.Create(dir + "/source.src");
									int count = 1;
									while (count < 100 && IsFileLocked(new FileInfo(dir + "/source.src")))
									{
										Task.Delay(100).Wait();
										count++;
									}
									byte[] bytes = File.ReadAllBytes(dir + "/source.src");
									File.WriteAllBytes(dir + "/source.src",
										bytes.Length != 0 ? StringZipper.Zip(StringZipper.Unzip(bytes) + sb) : StringZipper.Zip(sb.ToString()));
								}
								catch (Exception e)
								{
									Sitecore.Diagnostics.Log.Error("Unable to write the audit logger source", e, this);
								}
								writer.Commit();
								writer.Optimize();
								_optimizeTimer = 0;
							}
						}
					}
					catch (Exception e)
					{
						Sitecore.Diagnostics.Log.Error("Unable to commit to audit logger log", e, this);
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
			string filepath;
			if (System.Text.RegularExpressions.Regex.IsMatch(Settings.DataFolder, @"^(([a-zA-Z]:\\)|(//)).*")) //if we have an absolute path, rather than relative to the site root
				filepath = Settings.DataFolder +
						   @"\AuditLog";
			else
				filepath = HttpRuntime.AppDomainAppPath + Settings.DataFolder.Substring(1) +
						   @"\AuditLog";
			if (!System.IO.Directory.Exists(filepath))
				System.IO.Directory.CreateDirectory(filepath);
			_dataDirectory = filepath;
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
			if (_dir == null)
				_dir = FSDirectory.Open(new DirectoryInfo(GetDataDirectory()));
			return new IndexSearcher(_dir, false);
		}
		public TopDocs QueryIds(DateTime start, DateTime end, string query, IndexSearcher searcher = null)
		{
			if (searcher == null)
				searcher = GetSearcher();
			try
			{
				var lq = Parser.Parse(query + $" AND date:[{start:yyyyMMdd} TO {end:yyyyMMdd}]");
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
			return _trie.Autocomplete(text,
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
				string sourceDir = dir + "/source";
				if (_logDays > 0)
				{
					for (int i = 0; i < _logDays; i++)
					{
						_backupDays.Add(DateTime.Now.AddDays(i * -1).ToString("yyyy-MMM-dd") + ".zip");
						_backupDays.Add(DateTime.Now.AddDays(i * -1).ToString("yyyy-MMM-dd"));
					}
					foreach (string backup in System.IO.Directory.EnumerateFiles(backupDir))
					{
						if (!_backupDays.Contains(Path.GetFileName(backup)))
							File.Delete(backup);
					}
					foreach (string source in System.IO.Directory.EnumerateDirectories(sourceDir))
					{
						if (!_backupDays.Contains(Path.GetFileName(source)))
							System.IO.Directory.Delete(source, true);
					}
				}
			}
		}
	}
}
