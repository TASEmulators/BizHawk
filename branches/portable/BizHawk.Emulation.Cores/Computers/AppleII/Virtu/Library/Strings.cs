using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jellyfish.Virtu.Properties
{
	public static class Strings // Hack because we don't want resources in the core
	{
		public static string InvalidAddressRange
		{
			get
			{
				return "Invalid address range ${0:X04}-${1:X04}.";
			}
		}

		public static string MarkerNotFound
		{
			get
			{
				return "Marker ${0:X04} not found.";
			}
		}

		public static string ResourceNotFound
		{
			get
			{
				return "Resource '{0}' not found.";
			}
		}

		public static string ServiceAlreadyPresent
		{
			get
			{
				return "Service type '{0}' already present.";
			}
		}

		public static string ServiceMustBeAssignable
		{
			get
			{
				return "Service type '{0}' must be assignable from service provider '{1}'.";
			}
		}
	}
}
