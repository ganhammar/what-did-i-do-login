using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SSM;
using AppStack.Constructs;
using Constructs;
using Microsoft.Extensions.Configuration;

namespace AppStack;

public class AppStack : Stack
{
  private readonly IConfiguration _configuration;

  internal AppStack(Construct scope, string id, IStackProps props, IConfiguration configuration)
    : base(scope, id, props)
  {
    _configuration = configuration;

    var apiGateway = new RestApi(this, "what-did-i-do-login", new RestApiProps
    {
      RestApiName = "what-did-i-do-login",
      DefaultCorsPreflightOptions = new CorsOptions
      {
        AllowOrigins = new[]
        {
          "http://localhost:3000",
        },
      },
    });

    var loginConfiguration = _configuration.GetSection("Login");
    _ = new StringParameter(this, "LoginSigningCertificateParameter", new StringParameterProps
    {
      ParameterName = "/WDID/Login/SigningCertificate",
      StringValue = loginConfiguration.GetValue<string>("SigningCertificate")!,
      Tier = ParameterTier.STANDARD,
    });
    _ = new StringParameter(this, "LoginEncryptionCertificateParameter", new StringParameterProps
    {
      ParameterName = "/WDID/Login/EncryptionCertificate",
      StringValue = loginConfiguration.GetValue<string>("EncryptionCertificate")!,
      Tier = ParameterTier.STANDARD,
    });

    var loginFunction = new AppFunction(this, "App.Login", new AppFunction.Props(
      "App.Login::App.Login.LambdaEntryPoint::FunctionHandlerAsync",
      2048
    ));

    var identityTable = Table.FromTableAttributes(this, "IdentityTable", new TableAttributes
    {
      TableArn = $"arn:aws:dynamodb:{Region}:{Account}:table/what-did-i-do.identity",
      GrantIndexPermissions = true,
    });
    identityTable.GrantReadWriteData(loginFunction);

    var openiddictTable = Table.FromTableAttributes(this, "OpenIddictTable", new TableAttributes
    {
      TableArn = $"arn:aws:dynamodb:{Region}:{Account}:table/what-did-i-do.openiddict",
      GrantIndexPermissions = true,
    });
    openiddictTable.GrantReadWriteData(loginFunction);

    AllowSes(loginFunction);
    AllowSsm(loginFunction, "/WDID/DataProtection*", true);
    AllowSsm(loginFunction, "/WDID/Login*", false);

    apiGateway.Root.AddProxy(new ProxyResourceOptions
    {
      AnyMethod = true,
      DefaultIntegration = new LambdaIntegration(loginFunction),
    });
  }

  private void AllowSsm(AppFunction function, string resource, bool allowPut)
  {
    var actions = new List<string>
    {
      "ssm:GetParametersByPath",
    };

    if (allowPut)
    {
      actions.Add("ssm:PutParameter");
    }

    var ssmPolicy = new PolicyStatement(new PolicyStatementProps
    {
      Effect = Effect.ALLOW,
      Actions = actions.ToArray(),
      Resources = new[]
      {
        $"arn:aws:ssm:{Region}:{Account}:parameter{resource}",
      },
    });

    function.AddToRolePolicy(ssmPolicy);
  }

  private void AllowSes(AppFunction function)
  {
    var sesPolicy = new PolicyStatement(new PolicyStatementProps
    {
      Effect = Effect.ALLOW,
      Actions = new[]
      {
        "ses:SendEmail",
        "ses:SendRawEmail",
        "ses:SendTemplatedEmail",
      },
      Resources = new[]
      {
        "*",
      },
    });
    function.AddToRolePolicy(sesPolicy);
  }
}
