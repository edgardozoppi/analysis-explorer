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

			//AddCommand("Show _CG", ModifierKeys.Control, Key.C, OnShowCG);
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
			var document = new GraphDocumentViewModel(this.Main, "CH", assembly.Name, text, "LinLog");
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
			var methodInfo = this.Main.ProgramAnalysisInfo.GetOrAdd(method);
			GenerateIL(methodInfo);

			var text = methodInfo.Get<string>("IL_TEXT");
			var document = new TextDocumentViewModel(this.Main, "IL", this.FullName, text);
			this.Main.AddDocument(document);
		}

		private void OnShowTAC(object obj)
		{
			var methodInfo = this.Main.ProgramAnalysisInfo.GetOrAdd(method);
			GenerateTAC(methodInfo);

			var text = methodInfo.Get<string>("TAC_TEXT");
			var document = new TextDocumentViewModel(this.Main, "TAC", this.FullName, text);
			this.Main.AddDocument(document);
		}

		private void OnShowWebs(object obj)
		{
			var methodInfo = this.Main.ProgramAnalysisInfo.GetOrAdd(method);
			GenerateWebs(methodInfo);

			var text = methodInfo.Get<string>("WEBS_TEXT");
			var document = new TextDocumentViewModel(this.Main, "Webs", this.FullName, text);
			this.Main.AddDocument(document);
		}

		private void OnShowSSA(object obj)
		{
			var methodInfo = this.Main.ProgramAnalysisInfo.GetOrAdd(method);
			GenerateSSA(methodInfo);

			var text = methodInfo.Get<string>("SSA_TEXT");
			var document = new TextDocumentViewModel(this.Main, "SSA", this.FullName, text);
			this.Main.AddDocument(document);
		}

		private void OnShowCFG(object obj)
		{
			var methodInfo = this.Main.ProgramAnalysisInfo.GetOrAdd(method);
			GenerateCFG(methodInfo);

			var text = methodInfo.Get<string>("CFG_TEXT");
			var document = new GraphDocumentViewModel(this.Main, "CFG", this.FullName, text, "EfficientSugiyama");
			this.Main.AddDocument(document);
		}

		private void OnShowPTG(object obj)
		{
			var methodInfo = this.Main.ProgramAnalysisInfo.GetOrAdd(method);
			GeneratePTG(methodInfo);

			var text = methodInfo.Get<string>("PTG_TEXT");
			var document = new GraphDocumentViewModel(this.Main, "PTG", this.FullName, text, "LinLog");
			this.Main.AddDocument(document);
		}

		private void GenerateIL(MethodAnalysisInfo methodInfo)
		{
			if (!methodInfo.Contains("IL_TEXT"))
			{
				var text = method.Body.ToString();

				methodInfo.Add("IL_TEXT", text);
			}
		}

		private void GenerateTAC(MethodAnalysisInfo methodInfo)
		{
			GenerateIL(methodInfo);

			if (!methodInfo.Contains("TAC"))
			{
				var dissasembler = new Disassembler(method);
				var body = dissasembler.Execute();
				var text = body.ToString();

				methodInfo.Add("TAC", body);
				methodInfo.Add("TAC_TEXT", text);
			}
		}

		private void GenerateCFG(MethodAnalysisInfo methodInfo)
		{
			//GenerateIL(methodInfo);
			GenerateTAC(methodInfo);

			if (!methodInfo.Contains("CFG"))
			{
				var body = methodInfo.Get<MethodBody>("TAC");

				// Control-flow
				var cfAnalysis = new ControlFlowAnalysis(body);
				var cfg = cfAnalysis.GenerateNormalControlFlow();
				//var cfg = cfAnalysis.GenerateExceptionalControlFlow();

				var domAnalysis = new DominanceAnalysis(cfg);
				domAnalysis.Analyze();
				domAnalysis.GenerateDominanceTree();

				var loopAnalysis = new NaturalLoopAnalysis(cfg);
				loopAnalysis.Analyze();

				var domFrontierAnalysis = new DominanceFrontierAnalysis(cfg);
				domFrontierAnalysis.Analyze();

				var text = DGMLSerializer.Serialize(cfg);

				methodInfo.Add("CFG", cfg);
				methodInfo.Add("CFG_TEXT", text);
			}
		}

		private void GenerateWebs(MethodAnalysisInfo methodInfo)
		{
			//GenerateIL(methodInfo);
			//GenerateTAC(methodInfo);
			GenerateCFG(methodInfo);

			if (!methodInfo.Contains("WEBS_TEXT"))
			{
				var body = methodInfo.Get<MethodBody>("TAC");
				var cfg = methodInfo.Get<ControlFlowGraph>("CFG");

				// Webs
				var splitter = new WebAnalysis(cfg);
				splitter.Analyze();
				splitter.Transform();

				body.UpdateVariables();

				var typeAnalysis = new TypeInferenceAnalysis(cfg);
				typeAnalysis.Analyze();

				var text = body.ToString();
				methodInfo.Add("WEBS_TEXT", text);

				text = DGMLSerializer.Serialize(cfg);
				methodInfo.Set("CFG_TEXT", text);
			}
		}

		private void GenerateSSA(MethodAnalysisInfo methodInfo)
		{
			//GenerateIL(methodInfo);
			//GenerateTAC(methodInfo);
			//GenerateCFG(methodInfo);
			GenerateWebs(methodInfo);

			if (!methodInfo.Contains("SSA_TEXT"))
			{
				var body = methodInfo.Get<MethodBody>("TAC");
				var cfg = methodInfo.Get<ControlFlowGraph>("CFG");

				// Live Variables
				var liveVariables = new LiveVariablesAnalysis(cfg);
				liveVariables.Analyze();

				// SSA
				var ssa = new StaticSingleAssignment(body, cfg);
				ssa.Transform();
				ssa.Prune(liveVariables);

				body.UpdateVariables();

				var text = body.ToString();
				methodInfo.Add("SSA_TEXT", text);

				text = DGMLSerializer.Serialize(cfg);
				methodInfo.Set("CFG_TEXT", text);
			}
		}

		private void GeneratePTG(MethodAnalysisInfo methodInfo)
		{
			//GenerateIL(methodInfo);
			//GenerateTAC(methodInfo);
			//GenerateCFG(methodInfo);
			//GenerateWebs(methodInfo);
			GenerateSSA(methodInfo);

			if (!methodInfo.Contains("PTG_TEXT"))
			{
				var cfg = methodInfo.Get<ControlFlowGraph>("CFG");

				// Points-to
				var pointsTo = new PointsToAnalysis(cfg, method);
				var result = pointsTo.Analyze();

				var ptg = result[cfg.Exit.Id].Output;
				//ptg.RemoveVariablesExceptParameters();
				//ptg.RemoveTemporalVariables();

				var text = DGMLSerializer.Serialize(ptg);

				methodInfo.Add("PTG", ptg);
				methodInfo.Set("PTG_TEXT", text);
			}
		}
	}
}