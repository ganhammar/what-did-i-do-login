using App.Login.Infrastructure;
using App.Login.Infrastructure.Validators;
using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace App.Login.Features.User;

public class GetTwoFactorProvidersQuery
{
  public class Query : IRequest<IResponse<List<string>>>
  {
  }

  public class QueryValidator : AbstractValidator<Query>
  {
    public QueryValidator(
      SignInManager<DynamoDbUser> signInManager,
      UserManager<DynamoDbUser> userManager,
      IHttpContextAccessor httpContextAccessor)
    {
      RuleFor(x => x)
        .MustAsync(async (query, cancellationToken) =>
        {
          DynamoDbUser? user;
          if (httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true)
          {
            user = await signInManager.GetTwoFactorAuthenticationUserAsync();
          }
          else
          {
            user = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
          }

          return user != default;
        })
        .WithErrorCode(nameof(ErrorCodes.NoLoginAttemptInProgress))
        .WithMessage(ErrorCodes.NoLoginAttemptInProgress);
    }
  }

  public class QueryHandler : Handler<Query, IResponse<List<string>>>
  {
    private readonly UserManager<DynamoDbUser> _userManager;
    private readonly SignInManager<DynamoDbUser> _signInManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryHandler(
      UserManager<DynamoDbUser> userManager,
      SignInManager<DynamoDbUser> signInManager,
      IHttpContextAccessor httpContextAccessor)
    {
      _userManager = userManager;
      _signInManager = signInManager;
      _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IResponse<List<string>>> Handle(
      Query request, CancellationToken cancellationToken)
    {
      DynamoDbUser? user;
      if (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true)
      {
        user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
      }
      else
      {
        user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext!.User);
      }

      var providers = await _userManager.GetValidTwoFactorProvidersAsync(user!);

      return Response(providers.ToList());
    }
  }
}
