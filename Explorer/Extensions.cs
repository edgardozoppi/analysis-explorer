using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer
{
	static class Extensions
	{
		public static string ToFullDisplayName(this IMethodReference method)
		{
			var result = new StringBuilder();

			result.AppendFormat("{0}::{1}", method.ContainingType.GetFullName(), method.GenericName);

			var parameters = string.Join(", ", method.Parameters.Select(p => p.ToParameterString()));
			result.AppendFormat("({0})", parameters);

			return result.ToString();
		}

		public static string ToDisplayName(this IMethodReference method)
		{
			var result = new StringBuilder();

			result.Append(method.GenericName);

			var parameters = string.Join(", ", method.Parameters.Select(p => p.ToParameterString()));
			result.AppendFormat("({0})", parameters);

			return result.ToString();
		}

		private static string ToParameterString(this IMethodParameterReference parameter)
		{
			var kind = string.Empty;

			switch (parameter.Kind)
			{
				case MethodParameterKind.Out:
					kind = "out ";
					break;

				case MethodParameterKind.Ref:
					kind = "ref ";
					break;
			}

			return string.Format("{0}{1}", kind, parameter.Type);
		}

		public static IEnumerable<T> Join<T>(this IEnumerable<T> src, Func<T> separator)
		{
			var srcArr = src.ToArray();

			for (int i = 0; i < srcArr.Length; i++)
			{
				yield return srcArr[i];

				if (i < srcArr.Length - 1)
				{
					yield return separator();
				}
			}
		}
	}
}
