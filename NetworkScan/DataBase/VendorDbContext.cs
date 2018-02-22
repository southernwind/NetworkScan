using System;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SandBeige.NetworkScan.DataBase {
	public class VendorDbContext : DbContext {
		private readonly string _dbPath;
		public VendorDbContext(string dbPath) {
			this._dbPath = dbPath;
		}
		public DbSet<Vendor> Vendors {
			get;
			set;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			optionsBuilder.UseSqlite(new SqliteConnectionStringBuilder { DataSource = this._dbPath }.ToString());
#if DEBUG
		//	var factory = new LoggerFactory(new[] { new VendorDbLoggerProvider() });
		//	optionsBuilder.UseLoggerFactory(factory);
#endif
		}
	}
	
	public class VendorDbLoggerProvider : ILoggerProvider {
		public ILogger CreateLogger(string categoryName) {
			return new ConsoleLogger();
		}

		public void Dispose() {
		}
		
		private class ConsoleLogger : ILogger {
			public IDisposable BeginScope<TState>(TState state) => null;
			public bool IsEnabled(LogLevel logLevel) => true;
			public void Log<TState>(
				LogLevel logLevel, EventId eventId,
				TState state, Exception exception,
				Func<TState, Exception, string> formatter) {
				Console.WriteLine(formatter(state, exception));
			}
		}
	}
}
