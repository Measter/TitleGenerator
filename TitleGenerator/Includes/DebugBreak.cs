using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Parsers.Title;

namespace TitleGenerator.Includes
{
	public class DebugBreak
	{
		[Conditional("DEBUG")]
		public static void BreakOnValue<T>( T checker, T value )
		{
			if ( checker.Equals( value ) )
				System.Diagnostics.Debugger.Break();
		}
	}
}
