using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick.ContentMigrator.Services.Interface
{
	public interface ILoggingService
	{
		void Audit(object owner, string format, params string[] parameters);
		void Audit(string message, object owner);
		void Audit(string message, string loggerName);
		void Audit(string message, Type ownerType);
		void Audit(Type ownerType, string format, params string[] parameters);
		void Debug(string message, object owner);
		void Debug(string message);
		void Debug(string message, string loggerName);
		void Error(string message, object owner);
		void Error(string message, Type ownerType);
		void Error(string message, Exception exception, object owner);
		void Error(string message, Exception exception, Type ownerType);
		void Error(string message, Exception exception, string loggerName);
		void Fatal(string message, Exception exception, Type ownerType);
		void Fatal(string message, string loggerName);
		void Fatal(string message, object owner);
		void Fatal(string message, Exception exception, object owner);
		void Fatal(string message, Type ownerType);
		void Info(string message, object owner);
		void Info(string message, string loggerName);
		void SingleError(string message, object owner);
		void SingleFatal(string message, Exception exception, object owner);
		void SingleFatal(string message, Exception exception, Type ownerType);
		void SingleWarn(string message, object owner);
		void Warn(string message, object owner);
		void Warn(string message, Exception exception, object owner);
		void Warn(string message, Exception exception, string loggerName);
	}
}
