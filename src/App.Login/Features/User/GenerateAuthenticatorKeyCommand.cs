using App.Login.Infrastructure;
using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace App.Login.Features.User;

public class GenerateAuthenticatorKeyCommand
{
  public class Command : IRequest<IResponse<Result>>
  {
  }

  public class Result
  {
    public string? Key { get; set; }
  }

  public class CommandValidator : AbstractValidator<Command>
  {
    public CommandValidator(
      UserManager<DynamoDbUser> userManager,
      IHttpContextAccessor httpContextAccessor)
    {
      RuleFor(x => x)
        .Cascade(CascadeMode.Stop)
        .Must((query) =>
          httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true)
        .WithErrorCode(Errors.InvalidToken)
        .WithMessage("User is not authenticated")
        .MustAsync(async (query, cancellationToken) =>
        {
          var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
          return user != default;
        })
        .WithErrorCode(Errors.InvalidToken)
        .WithMessage("User is not authenticated");
    }
  }

  public class CommandHandler : Handler<Command, IResponse<Result>>
  {
    private readonly UserManager<DynamoDbUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CommandHandler(
      UserManager<DynamoDbUser> userManager,
      IHttpContextAccessor httpContextAccessor)
    {
      _userManager = userManager;
      _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IResponse<Result>> Handle(Command request, CancellationToken cancellationToken)
    {
      var user = (await _userManager.GetUserAsync(_httpContextAccessor.HttpContext!.User))!;
      var key = await _userManager.GetAuthenticatorKeyAsync(user);

      if (string.IsNullOrEmpty(key))
      {
        await _userManager.ResetAuthenticatorKeyAsync(user);
        key = await _userManager.GetAuthenticatorKeyAsync(user);
      }

      return Response(new Result()
      {
        Key = key
      });
    }
  }
}
