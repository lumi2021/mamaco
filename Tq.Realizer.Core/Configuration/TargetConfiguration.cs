using Tq.Realizer.Core.Program;
using Tq.Realizer.Core.Program.Member;
using Tq.Realizer.Core.Configuration.LangOutput;

namespace Tq.Realizer.Core.Configuration;

public class TargetConfiguration
{
    public delegate void CompilerDelegate(RealizerProgram program, IOutputConfiguration config);
    
    public string TargetName { get; init; }
    public string TargetDescription { get; init; }
    public string TargetIdentifier { get; init; }
    
    public IOutputConfiguration LanguageOutput { get; init; }
    public CompilerDelegate CompilerInvoke { get; init; }
}
