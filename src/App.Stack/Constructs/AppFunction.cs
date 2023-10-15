using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;

namespace AppStack.Constructs;

public class AppFunction : Function
{
  public AppFunction(Construct scope, string id, Props props)
    : base(scope, $"{id}Function", new FunctionProps
    {
      Runtime = Runtime.DOTNET_6,
      Architecture = Architecture.ARM_64,
      Handler = props.Handler,
      Code = Code.FromAsset($"./{id}.zip"),
      Timeout = Duration.Minutes(1),
      MemorySize = props.MemorySize,
      LogRetention = RetentionDays.ONE_DAY,
      Tracing = Tracing.ACTIVE,
    })
  { }

  public class Props
  {
    public Props(string handler, int memorySize = 1024)
    {
      Handler = handler;
      MemorySize = memorySize;
    }

    public string Handler { get; set; }
    public int MemorySize { get; set; }
  }
}
