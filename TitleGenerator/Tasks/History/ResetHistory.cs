using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Province;
using Parsers.Title;

namespace TitleGenerator.Tasks.History
{
	class ResetHistory : SharedTask
	{
		public ResetHistory( Options options, Logger log )
			: base( options, log )
		{

		}

		protected override bool Execute()
		{
			Log( "Resetting History" );
			SendMessage( "Resetting History" );

			ResetProvinces();
			ResetTitles();

			return true;
		}

		private void ResetTitles()
		{
			Log( " --Resetting Counties" );
			ResetTitleList( m_options.Data.Counties );

			Log( " --Resetting Duchies" );
			ResetTitleList( m_options.Data.Duchies );

			Log( " --Resetting Kingdoms" );
			ResetTitleList( m_options.Data.Kingdoms );

			Log( " --Resetting Empires" );
			ResetTitleList( m_options.Data.Empires );
		}

		private void ResetTitleList( ReadOnlyDictionary<string, Title> titles )
		{
			foreach( var c in titles )
			{
				if( c.Value.CustomFlags.ContainsKey( "old_cul" ) )
					c.Value.Culture = (string)c.Value.CustomFlags["old_cul"];
				c.Value.CustomFlags.Clear();
			}
		}

		private void ResetProvinces()
		{
			Log( " --Resetting Provinces" );

			foreach( var p in m_options.Data.Provinces )
			{												
				foreach( Settlement s in p.Value.Settlements )
				{
					if( s.CustomFlags.ContainsKey( "oldType" ) )
					{
						s.Type = (string)s.CustomFlags["oldType"];
						s.CustomFlags.Clear();
					}
				}

				if( p.Value.CustomFlags.ContainsKey( "old_cul" ) )
					p.Value.Culture = (string)p.Value.CustomFlags["old_cul"];

				if( p.Value.CustomFlags.ContainsKey( "old_rel" ) )
					p.Value.Religion = (string)p.Value.CustomFlags["old_rel"];

				p.Value.CustomFlags.Clear();
			}
		}
	}
}
