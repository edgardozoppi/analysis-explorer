using Backend.Analyses;
using Backend.Model;
using Backend.Serialization;
using Backend.Transformations;
using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSExtension
{
	internal class AnalysisHelper : IDisposable
	{
		private Host _host;
		private ILoader _loader;

		public AnalysisHelper()
		{
			_host = new Host();
			_loader = new CCIProvider.Loader(_host);

			PlatformTypes.Resolve(_host);
		}

		public void Dispose()
		{
			if (_loader != null)
			{
				_loader.Dispose();
				_loader = null;
			}

			GC.SuppressFinalize(this);
		}

		public void LoadAssembly(string fileName)
		{
			_loader.LoadAssembly(fileName);
		}

		public IMethodReference FindMethod(IBasicType containingType, string methodSignature)
		{
			IMethodReference result = null;
			var typedef = _host.ResolveReference(containingType);
			var methods = typedef.Members.OfType<MethodDefinition>();

			foreach (var method in methods)
			{
				var signature = Utils.GetSignature(method);

				if (signature == methodSignature)
				{
					result = method;
					break;
				}
			}

			return result;
		}

        public MethodDefinition Resolve(IMethodReference method)
        {
            var result = _host.ResolveReference(method) as MethodDefinition;
            return result;
        }

		public string GenerateIL(MethodDefinition method)
		{
			var result = method.Body.ToString();
			return result;
		}

		public string GenerateTAC(MethodDefinition method)
		{
			var body = GetTAC(method);
			var result = body.ToString();
			return result;
		}

		public string GenerateWebs(MethodDefinition method)
		{
			GetWebs(method, out MethodBody body);
			var result = body.ToString();
			return result;
		}

		public string GenerateSSA(MethodDefinition method)
		{
			GetSSA(method, out MethodBody body);
			var result = body.ToString();
			return result;
		}

        public string GenerateCFG(MethodDefinition method)
        {
            var cfg = GetCFG(method, out MethodBody body);
            var result = DGMLSerializer.Serialize(cfg);
            return result;
        }

        public string GeneratePTG(MethodDefinition method)
        {
            var ptg = GetPTG(method);
            var result = DGMLSerializer.Serialize(ptg);
            return result;
        }

        private MethodBody GetTAC(MethodDefinition method)
		{
			var dissasembler = new Disassembler(method);
			var body = dissasembler.Execute();
			return body;
		}

		private ControlFlowGraph GetCFG(MethodDefinition method, out MethodBody body)
		{
			body = GetTAC(method);

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

			return cfg;
		}

		private ControlFlowGraph GetWebs(MethodDefinition method, out MethodBody body)
		{
			var cfg = GetCFG(method, out body);

			// Webs
			var splitter = new WebAnalysis(cfg);
			splitter.Analyze();
			splitter.Transform();

			body.UpdateVariables();

			var typeAnalysis = new TypeInferenceAnalysis(cfg);
			typeAnalysis.Analyze();

			return cfg;
		}

		private ControlFlowGraph GetSSA(MethodDefinition method, out MethodBody body)
		{
			var cfg = GetWebs(method, out body);

			// Live Variables
			var liveVariables = new LiveVariablesAnalysis(cfg);
			var livenessInfo = liveVariables.Analyze();

			// SSA
			var ssa = new StaticSingleAssignment(body, cfg);
			ssa.Transform();
			ssa.Prune(livenessInfo);

			body.UpdateVariables();
			return cfg;
		}

        private PointsToGraph GetPTG(MethodDefinition method)
        {
            var cfg = GetSSA(method, out MethodBody body);

            // Points-to
            var pointsTo = new PointsToAnalysis(cfg, method);
            var result = pointsTo.Analyze();

            var ptg = result[cfg.Exit.Id].Output;
            //ptg.RemoveVariablesExceptParameters();
            //ptg.RemoveTemporalVariables();

            return ptg;
        }
    }
}
