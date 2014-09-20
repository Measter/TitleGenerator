using System.Collections.Generic;
using Parsers.Mod;

namespace TitleGenerator.Tasks
{
	class LoadTask : ITask
	{
		public event MessageUpdate Message;
		public List<string> Errors
		{
			get;
			set;
		}

		private CK2Data m_dataHolder;
		private Logger m_log;
		private List<Mod> m_mods;

		public LoadTask( CK2Data dataHolder, Logger log, List<Mod> selected )
		{
			Errors = new List<string>();
			m_dataHolder = dataHolder;
			m_log = log;
			m_mods = selected;
		}


		public bool Run()
		{
			try
			{
				#region Loading Data.
				SendMessage( "Loading Titles" );
				if( !m_dataHolder.LoadData( m_mods, CK2Data.DataTypes.LandedTitles ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}

				SendMessage( "Loading Provinces" );
				if( !m_dataHolder.LoadData( m_mods, CK2Data.DataTypes.Provinces ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}

				SendMessage( "Loading Cultures" );
				if( !m_dataHolder.LoadData( m_mods, CK2Data.DataTypes.Cultures ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}

				SendMessage( "Loading Religions" );
				if( !m_dataHolder.LoadData( m_mods, CK2Data.DataTypes.Religions ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}

				SendMessage( "Loading Dynasties" );
				if( !m_dataHolder.LoadData( m_mods, CK2Data.DataTypes.Dynasties ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}

				SendMessage( "Loading Localisations" );
				if( !m_dataHolder.LoadData( m_mods, CK2Data.DataTypes.Localisations ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}

				SendMessage( "Loading EUIV Converter" );
				if( !m_dataHolder.LoadData( m_mods, CK2Data.DataTypes.ConvertTable ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}

				SendMessage( "Loading Markov Chains" );
				if( !m_dataHolder.LoadData( m_mods, CK2Data.DataTypes.MarkovChains ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}
				#endregion

				#region Linking Data
				SendMessage( "Linking Titles and Provinces" );
				if( !m_dataHolder.LinkData( m_mods, CK2Data.DataTypes.LandedTitles | CK2Data.DataTypes.Provinces ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}

				SendMessage( "Linking Cultures and Dynasties" );
				if( !m_dataHolder.LinkData( m_mods, CK2Data.DataTypes.Dynasties | CK2Data.DataTypes.Cultures ) )
				{
					Errors.Add( m_dataHolder.Error );
					return false;
				}
				#endregion

				return true;
			} catch( System.Exception ex )
			{
				Errors.Add( ex.ToString() );
				return false;
			}
		}

		private void SendMessage( string message )
		{
			if( Message != null )
				Message( message );
		}
	}
}
