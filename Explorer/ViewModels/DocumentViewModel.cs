using Backend.Analyses;
using Backend.Model;
using Backend.Serialization;
using Backend.Transformations;
using Backend.Utils;
using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Explorer
{
	abstract class DocumentViewModelBase : ViewModelBase
	{
		public MainViewModel Main { get; private set; }
		public abstract string Name { get; }

		public ICommand CloseCommand { get; private set; }

		public DocumentViewModelBase(MainViewModel main)
		{
			this.Main = main;
			this.CloseCommand = new DelegateCommand(OnClose);
		}

		private void OnClose(object obj)
		{
			this.Main.Documents.Remove(this);
		}
	}

	class TextDocumentViewModel : DocumentViewModelBase
	{
		private string name;

		public string Kind { get; private set; }
		public string Text { get; private set; }

		public TextDocumentViewModel(MainViewModel main, string kind, string name, string text)
			: base(main)
		{
			this.Kind = kind;
			this.name = name;
			this.Text = text;
		}

		public override string Name
		{
			get { return name; }
		}
	}

	class MethodDocumentViewModel : DocumentViewModelBase
	{
		private MethodDefinition method;
		private IList<MethodBodyViewModel> bodies;

		public MethodDocumentViewModel(MainViewModel main, MethodDefinition method)
			: base(main)
		{
			this.method = method;
			this.bodies = new List<MethodBodyViewModel>();

			Analyze();
		}

		public override string Name
		{
			get { return method.ToFullDisplayName(); }
		}

		public string Kind
		{
			get { return "All"; }
		}

		public IEnumerable<MethodBodyViewModel> Bodies
		{
			get { return bodies; }
		}

		public IEnumerable<object> VisibleBodies
		{
			get { return bodies.Where(vm => vm.IsVisible); }
		}

		private void Analyze()
		{
			//GenerateIL(methodInfo);
			//GenerateTAC(methodInfo);
			//GenerateCFG(methodInfo);
			//GenerateWebs(methodInfo);
			//GenerateSSA(methodInfo);
			this.Main.GeneratePTG(method);

			var text = this.Main.GetMethodInfo<string>(method, "IL_TEXT");
			var vm = new MethodBodyViewModel(this, "IL Bytecode", text);
			bodies.Add(vm);

			text = this.Main.GetMethodInfo<string>(method, "TAC_TEXT");
			vm = new MethodBodyViewModel(this, "Three Address Code", text);
			bodies.Add(vm);

			text = this.Main.GetMethodInfo<string>(method, "WEBS_TEXT");
			vm = new MethodBodyViewModel(this, "Webbed Three Address Code", text);
			bodies.Add(vm);

			text = this.Main.GetMethodInfo<string>(method, "SSA_TEXT");
			vm = new MethodBodyViewModel(this, "Static Single Assignment", text);
			bodies.Add(vm);

			text = this.Main.GetMethodInfo<string>(method, "CFG_TEXT");
			vm = new MethodGraphViewModel(this, "Control-Flow Graph", text, "EfficientSugiyama");
			bodies.Add(vm);

			text = this.Main.GetMethodInfo<string>(method, "PTG_TEXT");
			vm = new MethodGraphViewModel(this, "Points-To Graph", text, "LinLog");
			bodies.Add(vm);
		}
	}

	class MethodBodyViewModel : ViewModelBase
	{
		private MethodDocumentViewModel parent;
		private bool isVisible;

		public string Name { get; private set; }
		public string Text { get; private set; }

		public MethodBodyViewModel(MethodDocumentViewModel parent, string name, string text)
		{
			this.parent = parent;
			this.Name = name;
			this.Text = text;
			this.isVisible = true;
		}

		public bool IsVisible
		{
			get { return isVisible; }
			set
			{
				SetProperty(ref isVisible, value);
				parent.OnPropertyChanged(nameof(parent.VisibleBodies));
			}
		}
	}

	class MethodGraphViewModel : MethodBodyViewModel
	{
		private string layoutType;

		public Graph Graph { get; private set; }

		public MethodGraphViewModel(MethodDocumentViewModel parent, string name, string text, string layoutType)
			: base(parent, name, text)
		{
			this.layoutType = layoutType;

			this.Graph = Extensions.CreateGraphFromDGML(text);
		}

		public string LayoutType
		{
			get { return layoutType; }
			set { SetProperty(ref layoutType, value); }
		}
	}
}