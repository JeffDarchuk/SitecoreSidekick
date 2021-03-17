using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sidekick.ContentMigrator.Services.Interface;
using Sitecore.Diagnostics;

namespace Sidekick.ContentMigrator.Services
{
	public class LoggingService : ILoggingService
	{
		public void Audit(object owner, string format, params string[] parameters) => Log.Audit(owner, format, parameters);
		public void Audit(string message, object owner) => Log.Audit(message, owner);
		public void Audit(string message, string loggerName) => Log.Audit(message, loggerName);
		public void Audit(string message, Type ownerType) => Log.Audit(message, ownerType);
		public void Audit(Type ownerType, string format, params string[] parameters) => Log.Audit(ownerType, format, parameters);
		public void Debug(string message, object owner) => Log.Debug(message, owner);
		public void Debug(string message) => Log.Debug(message);
		public void Debug(string message, string loggerName) => Log.Debug(message, loggerName);
		public void Error(string message, object owner) => Log.Error(message, owner);
		public void Error(string message, Type ownerType) => Log.Error(message, ownerType);
		public void Error(string message, Exception exception, object owner) => Log.Error(message, exception, owner);
		public void Error(string message, Exception exception, Type ownerType) => Log.Error(message, exception, ownerType);
		public void Error(string message, Exception exception, string loggerName) => Log.Error(message, exception, loggerName);
		public void Fatal(string message, Exception exception, Type ownerType) => Log.Fatal(message, exception, ownerType);
		public void Fatal(string message, string loggerName) => Log.Fatal(message, loggerName);
		public void Fatal(string message, object owner) => Log.Fatal(message, owner);
		public void Fatal(string message, Exception exception, object owner) => Log.Fatal(message, exception, owner);
		public void Fatal(string message, Type ownerType) => Log.Fatal(message, ownerType);
		public void Info(string message, object owner) => Log.Info(message, owner);
		public void Info(string message, string loggerName) => Log.Info(message, loggerName);
		public void SingleError(string message, object owner) => Log.SingleError(message, owner);
		public void SingleFatal(string message, Exception exception, object owner) => Log.SingleFatal(message, exception, owner);
		public void SingleFatal(string message, Exception exception, Type ownerType) => Log.SingleFatal(message, exception, ownerType);
		public void SingleWarn(string message, object owner) => Log.SingleWarn(message, owner);
		public void Warn(string message, object owner) => Log.Warn(message, owner);
		public void Warn(string message, Exception exception, object owner) => Log.Warn(message, exception, owner);
		public void Warn(string message, Exception exception, string loggerName) => Log.Warn(message, exception, loggerName);
	}
}
