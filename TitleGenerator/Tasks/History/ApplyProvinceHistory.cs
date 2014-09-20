using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Measter;
using Parsers.Options;
using Parsers.Province;

namespace TitleGenerator.Tasks.History
{
	class ApplyProvinceHistory : SharedTask
	{
		private readonly EventOptionDateComparer m_dateComparer = new EventOptionDateComparer();

		public ApplyProvinceHistory( Options options, Logger log )
			: base( options, log )
		{

		}

		protected override bool Execute()
		{
			Log( "Applying Province History" );
			SendMessage( "Applying Province History." );

			foreach( var p in m_options.Data.Provinces )
			{
				ApplyHistory( p.Value );
			}

			return true;
		}

		private void ApplyHistory( Province prov )
		{											  
			prov.History.Sort( m_dateComparer );

			foreach( EventOption ev in prov.History )
			{
				if ( ev.Date.Year > m_options.StartDate )
					break;

				foreach( Option op in ev.SubOptions )
				{
					
					if( op.GetIDString == "culture" )
					{
						if ( !prov.CustomFlags.ContainsKey( "old_cul" ) )
							prov.CustomFlags["old_cul"] = prov.Culture;
						prov.Culture = ( (StringOption)op ).GetValue;
					} else if( op.GetIDString == "religion" )
					{
						if( !prov.CustomFlags.ContainsKey( "old_rel" ) )
							prov.CustomFlags["old_rel"] = prov.Religion;
						prov.Religion = ( (StringOption)op ).GetValue;
					}
				}
			}
		}
	}
}
