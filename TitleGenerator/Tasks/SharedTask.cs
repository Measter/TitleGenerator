using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Parsers.Province;

namespace TitleGenerator.Tasks
{
	class SharedTask : ITask
	{
		public event MessageUpdate Message;
		public List<string> Errors
		{
			get;
			set;
		}

		protected Options m_options;
		protected Logger m_log;

		public SharedTask( Options options, Logger log )
		{
			m_options = options;
			m_log = log;
			Errors = new List<string>();
		}

		public bool Run()
		{
			try
			{
				return Execute();
			} catch( Exception ex )
			{
				Errors.Add( ex.ToString() );
				return false;
			}
		}

		protected virtual bool Execute()
		{
			return true;
		}


		protected void Log( string message )
		{
			m_log.Log( message, Logger.LogType.Generate );
		}

		protected void SendMessage( string message )
		{
			if( Message != null )
				Message( message );
		}

		protected Color HSVtoRGB( float hue, float sat, float val )
		{
			Color temp = Color.Empty;

			double C = 0, H = 0, X = 0, R1 = 0, G1 = 0, B1 = 0, m = 0;

			//Convert hue to 0-360 range
			hue *= 360;

			//Get Chroma
			C = val * sat;

			//Find Bottom RGB
			H = hue / 60;
			X = C * ( 1 - Math.Abs( ( H % 2 ) - 1 ) );

			if( H < 1 )
			{
				R1 = C;
				G1 = X;
				B1 = 0;
			} else if( H < 2 )
			{
				R1 = X;
				G1 = C;
				B1 = 0;
			} else if( H < 3 )
			{
				R1 = 0;
				G1 = C;
				B1 = X;
			} else if( H < 4 )
			{
				R1 = 0;
				G1 = X;
				B1 = C;
			} else if( H < 5 )
			{
				R1 = X;
				G1 = 0;
				B1 = C;
			} else if( H <= 6 )
			{
				R1 = C;
				G1 = 0;
				B1 = X;
			}

			m = val - C;

			temp = Color.FromArgb( (int)Math.Round( 255 * ( R1 + m ) ), (int)Math.Round( 255 * ( G1 + m ) ),
								   (int)Math.Round( 255 * ( B1 + m ) ) );

			return temp;
		}

		protected void RGBtoHSV( Color col, out float hue, out float sat, out float val )
		{
			int max = Math.Max( col.R, Math.Max( col.G, col.B ) );
			int min = Math.Min( col.R, Math.Min( col.G, col.B ) );

			hue = col.GetHue() / 360f;
			sat = ( max == 0 ) ? 0 : 1f - ( 1f * min / max );
			val = max / 255f;
		}

		protected List<Province> FilterIgnoredProvinces()
		{
			Log( " --Filtering ignored Provinces" );
			List<Province> provList = m_options.Data.Provinces.Values.ToList();

			for( int i = 0; i < provList.Count; i++ )
			{
				Province p = provList[i];
				if( m_options.RuleSet.IgnoredTitles.Contains( p.Title ) )
				{
					provList.Remove( p );
					--i;
				}
			}

			return provList;
		}

		protected void FilterSingleProvinces( List<Province> provs, List<Province> unownedProvs )
		{
			for( int i = 0; i < provs.Count; i++ )
			{
				Province p = provs[i];
				if( p.Adjacencies.Count == 0 )
				{
					provs.Remove( p );
					unownedProvs.Add( p );
					--i;
				}
			}
		}
	}
}
