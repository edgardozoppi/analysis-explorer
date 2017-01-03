using Model;
using System.Collections.ObjectModel;
using System;
using Model.Types;
using System.Text;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using Backend.Transformations;
using Backend.Utils;
using Backend.Analyses;
using Backend.Model;
using Backend.Serialization;
using System.Collections.Generic;
using GraphX.PCL.Common.Enums;

namespace Explorer
{
	abstract class ItemViewModelBase
	{
		public MainViewModel Main { get; private set; }
		public abstract string Name { get; }
		public abstract string Icon { get; }

		public ObservableCollection<ItemViewModelBase> Childs { get; private set; }
		public ObservableCollection<UIDelegateCommand> Commands { get; private set; }

		public ItemViewModelBase(MainViewModel main)
		{
			this.Main = main;
			this.Childs = new ObservableCollection<ItemViewModelBase>();
			this.Commands = new ObservableCollection<UIDelegateCommand>();
		}

		public bool HasCommands
		{
			get { return this.Commands.Count > 0; }
		}

		protected void AddSeparator()
		{
			this.Commands.Add(null);
		}

		protected UIDelegateCommand AddCommand(string name, ModifierKeys modifiers, Key key, Action<object> action, Func<object, bool> enabled = null)
		{
			var shortcut = new KeyGesture(key, modifiers);
			var command = new UIDelegateCommand(name, shortcut, action, enabled);
			this.Commands.Add(command);
			return command;
		}
	}

	class ItemViewModel : ItemViewModelBase
	{
		public string name;
		private string icon;

		public ItemViewModel(MainViewModel main, string name, string icon = null)
			: base(main)
		{
			this.name = name;
			this.icon = icon ?? "none";
		}

		public override string Icon
		{
			get { return string.Format(@"Images\{0}.png", icon); }
		}

		public override string Name
		{
			get { return name; }
		}
	}

	class AssemblyViewModel : ItemViewModelBase
	{
		private Assembly assembly;
		private IDictionary<string, object> info;

		public AssemblyViewModel(MainViewModel main, Assembly assembly)
			: base(main)
		{
			this.assembly = assembly;
			this.info = new Dictionary<string, object>();

			var references = new ItemViewModel(main, "References", "reference");
			this.Childs.Add(references);

			AddCommand("Show _CG", ModifierKeys.Control, Key.C, OnShowCG);
			AddCommand("Show _CH", ModifierKeys.Control, Key.H, OnShowCH);

			foreach (var reference in assembly.References)
			{
				var vm = new ReferenceViewModel(main, reference);
				references.Childs.Add(vm);
			}

			foreach (var @namespace in assembly.RootNamespace.Namespaces)
			{
				var vm = new NamespaceViewModel(main, @namespace);
				this.Childs.Add(vm);
			}

			foreach (var type in assembly.RootNamespace.Types)
			{
				var vm = new TypeViewModel(main, type);
				this.Childs.Add(vm);
			}
		}

		public override string Name
		{
			get { return string.Format("{0}.dll", assembly.Name); }
		}

		public override string Icon
		{
			get { return @"Images\assembly.png"; }
		}

		private void OnShowCH(object obj)
		{
			GenerateCH();

			var text = GetInfo<string>("CH_TEXT");
			var document = new GraphDocumentViewModel(this.Main, "CH", assembly.Name, text, LayoutAlgorithmTypeEnum.LinLog);
			this.Main.AddDocument(document);
		}

		private void OnShowCG(object obj)
		{
			GenerateCG();

			var text = GetInfo<string>("CG_TEXT");
			var document = new GraphDocumentViewModel(this.Main, "CG", assembly.Name, text, LayoutAlgorithmTypeEnum.LinLog);
			this.Main.AddDocument(document);
		}

		private void GenerateCH()
		{
			if (!info.ContainsKey("CH_TEXT"))
			{
				var ch = new ClassHierarchy();
				ch.Analyze(assembly);

				var text = DGMLSerializer.Serialize(ch);

				info.Add("CH", ch);
				info.Add("CH_TEXT", text);
			}
		}

		private void GenerateCG()
		{
			GenerateCH();

			if (!info.ContainsKey("CG_TEXT"))
			{
				var ch = GetInfo<ClassHierarchy>("CH");
				var cga = new ClassHierarchyAnalysis(ch);

				cga.OnReachableMethodFound = method =>
				{
					//this.Main.GenerateIL(method);
					this.Main.GenerateTAC(method);
				};

				var roots = assembly.GetRootMethods();
				var cg = cga.Analyze(this.Main.Host, roots);
				var text = DGMLSerializer.Serialize(cg);

				info.Add("CG_TEXT", text);
			}
		}

		private T GetInfo<T>(string key)
		{
			return (T)info[key];
		}
	}

	class ReferenceViewModel : ItemViewModelBase
	{
		private IAssemblyReference reference;

		public ReferenceViewModel(MainViewModel main, IAssemblyReference reference)
			: base(main)
		{
			this.reference = reference;
		}

		public override string Name
		{
			get { return string.Format("{0}.dll", reference.Name); }
		}

		public override string Icon
		{
			get { return @"Images\reference.png"; }
		}
	}

	class NamespaceViewModel : ItemViewModelBase
	{
		private Namespace @namespace;

		public NamespaceViewModel(MainViewModel main, Namespace @namespace)
			: base(main)
		{
			this.@namespace = @namespace;

			foreach (var nestedNamespace in @namespace.Namespaces)
			{
				var vm = new NamespaceViewModel(main, nestedNamespace);
				this.Childs.Add(vm);
			}

			foreach (var type in @namespace.Types)
			{
				var vm = new TypeViewModel(main, type);
				this.Childs.Add(vm);
			}
		}

		public override string Name
		{
			get { return @namespace.Name; }
		}

		public override string Icon
		{
			get { return @"Images\namespace.png"; }
		}
	}

	class TypeViewModel : ItemViewModelBase
	{
		private ITypeDefinition type;

		public TypeViewModel(MainViewModel main, ITypeDefinition type)
			: base(main)
		{
			this.type = type;

			foreach (var member in type.Members)
			{
				var vm = CreateItemViewModel(member);
				this.Childs.Add(vm);
			}
		}

		public override string Name
		{
			get { return type.GenericName; }
		}

		public override string Icon
		{
			get { return @"Images\class.png"; }
		}

		private ItemViewModelBase CreateItemViewModel(ITypeMemberDefinition member)
		{
			ItemViewModelBase result = null;

			if (member is ITypeDefinition)
			{
				var type = member as ITypeDefinition;
				result = new TypeViewModel(this.Main, type);
			}
			else if (member is FieldDefinition)
			{
				var field = member as FieldDefinition;
				result = new FieldViewModel(this.Main, field);
			}
			else if (member is MethodDefinition)
			{
				var method = member as MethodDefinition;
				result = new MethodViewModel(this.Main, method);
			}

			return result;
		}
	}

	class FieldViewModel : ItemViewModelBase
	{
		private FieldDefinition field;

		public FieldViewModel(MainViewModel main, FieldDefinition field)
			: base(main)
		{
			this.field = field;
		}

		public override string Name
		{
			get { return field.Name; }
		}

		public override string Icon
		{
			get { return @"Images\field.png"; }
		}
	}

	class MethodViewModel : ItemViewModelBase
	{
		private MethodDefinition method;

		public MethodViewModel(MainViewModel main, MethodDefinition method)
			: base(main)
		{
			this.method = method;

			AddCommand("Show _All", ModifierKeys.Control, Key.A, OnShowAll, OnCanShowBody);
			AddSeparator();
			AddCommand("Show _IL", ModifierKeys.Control, Key.I, OnShowIL, OnCanShowBody);
			AddCommand("Show _TAC", ModifierKeys.Control, Key.T, OnShowTAC, OnCanShowBody);
			AddCommand("Show _Webs", ModifierKeys.Control, Key.W, OnShowWebs, OnCanShowBody);
			AddCommand("Show _SSA", ModifierKeys.Control, Key.S, OnShowSSA, OnCanShowBody);
			AddSeparator();
			AddCommand("Show _CFG", ModifierKeys.Control, Key.F, OnShowCFG, OnCanShowBody);
			AddCommand("Show _PTG", ModifierKeys.Control, Key.P, OnShowPTG, OnCanShowBody);
			//AddCommand("Show _ESC", ModifierKeys.Control, Key.E, OnShowESC, OnCanShowBody);
		}

		private bool OnCanShowBody(object obj)
		{
			return method.HasBody;
		}

		public override string Name
		{
			get { return method.ToDisplayName(); }
		}

		public string FullName
		{
			get { return method.ToFullDisplayName(); }
		}

		public override string Icon
		{
			get { return @"Images\method.png"; }
		}

		private void OnShowAll(object obj)
		{
			var document = new MethodDocumentViewModel(this.Main, method);
			this.Main.AddDocument(document);
		}

		private void OnShowIL(object obj)
		{
			this.Main.GenerateIL(method);

			var text = this.Main.GetMethodInfo<string>(method, "IL_TEXT");
			var document = new TextDocumentViewModel(this.Main, "IL", this.FullName, text);
			this.Main.AddDocument(document);
		}

		private void OnShowTAC(object obj)
		{
			this.Main.GenerateTAC(method);

			var text = this.Main.GetMethodInfo<string>(method, "TAC_TEXT");
			var document = new TextDocumentViewModel(this.Main, "TAC", this.FullName, text);
			this.Main.AddDocument(document);
		}

		private void OnShowWebs(object obj)
		{
			this.Main.GenerateWebs(method);

			var text = this.Main.GetMethodInfo<string>(method, "WEBS_TEXT");
			var document = new TextDocumentViewModel(this.Main, "Webs", this.FullName, text);
			this.Main.AddDocument(document);
		}

		private void OnShowSSA(object obj)
		{
			this.Main.GenerateSSA(method);

			var text = this.Main.GetMethodInfo<string>(method, "SSA_TEXT");
			var document = new TextDocumentViewModel(this.Main, "SSA", this.FullName, text);
			this.Main.AddDocument(document);
		}

		private void OnShowCFG(object obj)
		{
			this.Main.GenerateCFG(method);

			var text = this.Main.GetMethodInfo<string>(method, "CFG_TEXT");
			var document = new GraphDocumentViewModel(this.Main, "CFG", this.FullName, text, LayoutAlgorithmTypeEnum.EfficientSugiyama);
			this.Main.AddDocument(document);
		}

		private void OnShowPTG(object obj)
		{
			this.Main.GeneratePTG(method);

			var text = this.Main.GetMethodInfo<string>(method, "PTG_TEXT");
			var document = new GraphDocumentViewModel(this.Main, "PTG", this.FullName, text, LayoutAlgorithmTypeEnum.LinLog);
			this.Main.AddDocument(document);
		}
	}
}