using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSExtension
{
	internal static class Utils
	{
		private static IAssemblyReference GetAssemblyReference(EnvDTE.ProjectItem projectItem)
		{
			var name = projectItem.ContainingProject.Properties.Item("AssemblyName").Value as string;
			var result = new AssemblyReference(name);
			return result;
		}

		private static IAssemblyReference GetAssemblyReference(EnvDTE.CodeType type)
		{
			IAssemblyReference result = null;

			if (type.InfoLocation == EnvDTE.vsCMInfoLocation.vsCMInfoLocationExternal)
			{
				// ??
				//dynamic value = type.Extender["ExternalLocation"];
				//var obj = value.ExternalLocation;

				//var proj = type.ProjectItem.ContainingProject.Object as VSLangProj.VSProject;
			}
			else
			{
				result = GetAssemblyReference(type.ProjectItem);
			}

			return result;
		}

		public static IBasicType ToReference(object type)
		{
			BasicType result = null;

			if (type is EnvDTE.CodeClass parentClass)
			{
				result = new BasicType(parentClass.Name, TypeKind.ReferenceType)
				{
					ContainingAssembly = GetAssemblyReference(parentClass.ProjectItem),
					ContainingNamespace = parentClass.Namespace.FullName,
					ContainingType = ToReference(parentClass.Parent)
				};
			}
			else if (type is EnvDTE.CodeStruct parentStruct)
			{
				result = new BasicType(parentStruct.Name, TypeKind.ValueType)
				{
					ContainingAssembly = GetAssemblyReference(parentStruct.ProjectItem),
					ContainingNamespace = parentStruct.Namespace.FullName,
					ContainingType = ToReference(parentStruct.Parent)
				};
			}

			return result;
		}

		public static IMethodReference ToReference(this EnvDTE.CodeFunction method)
		{
			var result = new MethodReference(method.Name, method.Type.ToReference())
			{
				IsStatic = method.IsShared,
				ContainingType = ToReference(method.Parent)
			};

			ushort index = 0;

			foreach (EnvDTE.CodeParameter parameter in method.Parameters)
			{
				var p = parameter.ToReference() as MethodParameterReference;
				p.Index = index++;
				result.Parameters.Add(p);
			}

			return result;
		}

		public static IMethodParameterReference ToReference(this EnvDTE.CodeParameter parameter)
		{
			var result = new MethodParameterReference(0, parameter.Type.ToReference());
			return result;
		}

		public static IType ToReference(this EnvDTE.CodeTypeRef type)
		{
			IType result = null;

			switch (type.TypeKind)
			{
				case EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType:
					result = new BasicType(type.CodeType.Name)
					{
						ContainingAssembly = GetAssemblyReference(type.CodeType),
						ContainingNamespace = type.CodeType.Namespace.FullName,
						ContainingType = ToReference(type.CodeType.Parent)
					};
					break;

				case EnvDTE.vsCMTypeRef.vsCMTypeRefArray:
					result = new ArrayType(type.ElementType.ToReference(), (uint)type.Rank);
					break;

				case EnvDTE.vsCMTypeRef.vsCMTypeRefPointer:
					result = new PointerType(type.ElementType.ToReference());
					break;

				case EnvDTE.vsCMTypeRef.vsCMTypeRefVoid: result = PlatformTypes.Void; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefString: result = PlatformTypes.String; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefObject: result = PlatformTypes.Object; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefByte: result = PlatformTypes.Byte; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefChar: result = PlatformTypes.Char; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefShort: result = PlatformTypes.Int16; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefInt: result = PlatformTypes.Int32; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefLong: result = PlatformTypes.Int64; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefFloat: result = PlatformTypes.Single; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefDouble: result = PlatformTypes.Double; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefDecimal: result = PlatformTypes.Decimal; break;
				case EnvDTE.vsCMTypeRef.vsCMTypeRefBool: result = PlatformTypes.Boolean; break;

				default: throw new Exception("Unknown type kind");
			}

			return result;
		}

		public static IBasicType GetTypeReference(object type)
		{
			BasicType result = null;

			if (type is EnvDTE.CodeClass parentClass)
			{
				result = new BasicType(parentClass.Name, TypeKind.ReferenceType)
				{
					ContainingAssembly = GetAssemblyReference(parentClass.ProjectItem),
					ContainingNamespace = parentClass.Namespace.FullName,
					ContainingType = GetTypeReference(parentClass.Parent)
				};
			}
			else if (type is EnvDTE.CodeStruct parentStruct)
			{
				result = new BasicType(parentStruct.Name, TypeKind.ValueType)
				{
					ContainingAssembly = GetAssemblyReference(parentStruct.ProjectItem),
					ContainingNamespace = parentStruct.Namespace.FullName,
					ContainingType = GetTypeReference(parentStruct.Parent)
				};
			}

			return result;
		}

		public static string GetSignature(IMethodReference method)
		{
			var modifier = string.Empty;
			var parameters = GetParameters(method.Parameters);

			if (method.IsStatic)
			{
				modifier = "static ";
			}

			return string.Format("{0}{1}({2})", modifier, method.Name, parameters);
		}

		private static object GetParameters(IEnumerable<IMethodParameterReference> parameters)
		{
			var result = new StringBuilder();

			foreach (var parameter in parameters)
			{
				var type = parameter.Type.GetFullName();
				result.AppendFormat(", {0}", type);
			}

			if (result.Length > 0)
			{
				result.Remove(0, 2);
			}

			return result.ToString();
		}

		public static string GetSignature(EnvDTE.CodeFunction method)
		{
			var modifier = string.Empty;
			var parameters = GetParameters(method.Parameters);

			if (method.IsShared)
			{
				modifier = "static ";
			}

			return string.Format("{0}{1}({2})", modifier, method.Name, parameters);
		}

		private static object GetParameters(EnvDTE.CodeElements parameters)
		{
			var result = new StringBuilder();

			foreach (EnvDTE.CodeParameter parameter in parameters)
			{
				result.AppendFormat(", {0}", parameter.Type.AsFullName);
			}

			if (result.Length > 0)
			{
				result.Remove(0, 2);
			}

			return result.ToString();
		}
	}
}
