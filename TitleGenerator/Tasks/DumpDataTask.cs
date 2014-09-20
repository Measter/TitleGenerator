using System.Collections.Generic;
using System.IO;
using Parsers.Province;

namespace TitleGenerator.Tasks
{
	class DumpDataTask : SharedTask
	{
		public DumpDataTask( Options options, Logger log ) : base( options, log )
		{
			
		}

		protected override bool Execute()
		{
			StreamWriter sw = new StreamWriter( "dump.txt" );

			foreach ( var pair in m_options.Data.Provinces )
			{
				sw.WriteLine( "Province {0}, ID {1}", pair.Key, pair.Value.Title );
				sw.WriteLine( "  --{0} {1}", pair.Value.Culture, pair.Value.Religion );

				if ( pair.Value.CustomFlags.Count == 0 )
					sw.WriteLine( "  --Flags {0}", pair.Value.CustomFlags.Count );
				else
				{
					sw.WriteLine("  --Flags");
					foreach ( var flag in pair.Value.CustomFlags )
						sw.WriteLine( "    --{0}", flag.Key );
				}

				sw.WriteLine( "  --Settlements" );
				foreach( Settlement set in pair.Value.Settlements )
				{
					sw.WriteLine( "    --{0} {1}", set.Title, set.Type );
					if ( set.CustomFlags.Count == 0 )
						sw.WriteLine( "      --Flags {0}", set.CustomFlags.Count );
					else
					{
						sw.WriteLine( "      --Flags" );
						foreach ( var flag in set.CustomFlags )
							sw.WriteLine( "        --{0}", flag.Key );
					}
				}
			}

			sw.Close();
			return true;
		}
	}
}