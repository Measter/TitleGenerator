using System;
using System.Collections.Generic;
using System.IO;

namespace TitleGenerator
{
	public class Logger
	{
		private readonly List<string> m_settingsLog;
		private readonly Queue<string> m_generateLog;
		private readonly Queue<string> m_dataLog;
		private readonly List<string> m_errorLog;

		public Logger()
		{
			m_settingsLog = new List<string>();
			m_generateLog = new Queue<string>();
			m_dataLog = new Queue<string>();
			m_errorLog = new List<string>();
		}

		public bool FullLog
		{
			get;
			set;
		}

		public UInt16 LogLength
		{
			get;
			set;
		}

		private void LogSetting( List<string> list, string message )
		{
			list.Add( message );
		}


		public void Log( string message, LogType type )
		{
			Queue<string> log = null;
			switch ( type )
			{
				case LogType.Generate:
					log = m_generateLog;
					break;
				case LogType.Data:
					log = m_dataLog;
					break;
				case LogType.Setting:
					LogSetting( m_settingsLog, message );
					return;
				case LogType.Error:
					LogSetting( m_errorLog, message );
					return;
			}

			log.Enqueue( message );
			// Limit length.
			if ( !FullLog && log.Count > LogLength )
				log.Dequeue();
		}

		public enum LogType
		{
			Generate,
			Data,
			Setting,
			Error
		}

		public void Dump( string filename )
		{
			using ( StreamWriter sw = new StreamWriter( filename ) )
			{
				foreach ( string s in m_settingsLog )
					sw.WriteLine( s );

				if( m_dataLog.Count > 0 )
				{
					sw.WriteLine();
					sw.WriteLine( "---- DataLog ----" );
					sw.WriteLine();

					foreach( string s in m_dataLog )
						sw.WriteLine( s ); 
				}

				if( m_generateLog.Count > 0 )
				{
					sw.WriteLine();
					sw.WriteLine( "---- Generate Log ----" );
					sw.WriteLine();

					foreach( string s in m_generateLog )
						sw.WriteLine( s );
				}

				if( m_errorLog.Count > 0 )
				{
					sw.WriteLine();
					sw.WriteLine( "---- Error Log ----" );
					sw.WriteLine();

					foreach( string s in m_errorLog )
						sw.WriteLine( s );
				}
			}
		}
	}
}