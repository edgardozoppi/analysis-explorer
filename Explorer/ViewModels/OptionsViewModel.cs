using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer
{
	class OptionsViewModel : ViewModelBase
	{
		public MainViewModel Main { get; private set; }
		public bool RemoveUnusedLabels { get; set; }
		public bool RunForwardCopyPropagation { get; set; }
		public bool RunBackwardCopyPropagation { get; set; }

		public OptionsViewModel(MainViewModel main)
		{
			this.Main = main;
		}

		public void LoadOptions()
		{
			this.RemoveUnusedLabels = this.Main.RemoveUnusedLabels;
			this.RunForwardCopyPropagation = this.Main.RunForwardCopyPropagation;
			this.RunBackwardCopyPropagation = this.Main.RunBackwardCopyPropagation;
		}
	}
}
