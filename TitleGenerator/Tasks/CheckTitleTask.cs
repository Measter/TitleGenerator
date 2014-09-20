using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Province;
using Parsers.Title;
using TitleGenerator.Includes;

namespace TitleGenerator.Tasks
{
	class CheckTitleTask : SharedTask
	{
		public CheckTitleTask( Options options, Logger log ) : base( options, log )
		{

		}

		protected override bool Execute()
		{
			lock( TaskStatus.TitleCheck )
			{
				Log( "Checking Titles" );
				SendMessage( "Checking Titles" );

				#region Counties
				Log( " --Checking Counties" );
				SendMessage( "Checking Counties" );
				foreach( var pair in m_options.Data.Counties )
				{
					if( TaskStatus.Abort )
						return false;

					Log( " --" + pair.Value.TitleID );

					Title c = pair.Value;
					Province prov;

					if( c.HolyOrder || c.Mercenary )
						m_options.RuleSet.IgnoredTitles.Add( c.TitleID );

					if( !m_options.Data.Provinces.TryGetValue( c.CountyID, out prov ) )
						continue;

					c.Capital = c.CountyID;

					if( String.IsNullOrEmpty( c.Culture ) )
						c.Culture = prov.Culture;
					if( String.IsNullOrEmpty( c.Religion ) )
						c.Religion = prov.Religion;
				}
				#endregion

				Log( "Checking Duchies" );
				SendMessage( "Checking Duchies" );
				CheckDuchyKingdom( m_options.Data.Duchies );

				if( TaskStatus.Abort )
					return false;

				Log( "Checking Kingdoms" );
				SendMessage( "Checking Kingdoms" );
				CheckDuchyKingdom( m_options.Data.Kingdoms );

				if( TaskStatus.Abort )
					return false;

				Log( "Checking Empires" );
				SendMessage( "Checking Empires" );
				CheckDuchyKingdom( m_options.Data.Empires );

				return true;
			}
		}



		private void CheckDuchyKingdom( ReadOnlyDictionary<string, Title> titles )
		{
			foreach( var pair in titles )
			{
				if( TaskStatus.Abort )
					return;

				Log( " --" + pair.Value.TitleID );

				Title d = pair.Value;

				if ( d.HolyOrder || d.Mercenary )
					m_options.RuleSet.IgnoredTitles.Add( d.TitleID );

				if( d.Primary || d.Landless || ( d.IsTitular && d.Capital == -1 ) )
					continue;

				if( d.Capital == -1 )
					d.Capital = d.SubTitles.ElementAt( 0 ).Value.Capital;

				if( !m_options.Data.Provinces.ContainsKey( d.Capital ) )
				{
					d.Primary = true;
					continue;
				}

				if( String.IsNullOrEmpty( d.Culture ) )
					d.Culture = m_options.Data.Provinces[d.Capital].Culture;
				if( String.IsNullOrEmpty( d.Religion ) )
					d.Religion = m_options.Data.Provinces[d.Capital].Religion;
			}
		}
	}
}
