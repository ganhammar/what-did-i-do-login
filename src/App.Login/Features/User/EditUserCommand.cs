using System.Data;
using App.Login.Infrastructure;
using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace App.Login.Features.User;

public class EditUserCommand
{
  public class Command : IRequest<IResponse<UserDto>>
  {
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
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

      When(x => x.Email != default, () =>
      {
        RuleFor(x => x.Email)
          .EmailAddress()
          .MustAsync(async (email, cancellationToken) =>
          {
            var currentUser = await userManager.GetUserAsync(httpContextAccessor.HttpContext!.User);
            var user = await userManager.FindByEmailAsync(email);
            return email == currentUser?.Email || user == default;
          })
          .WithErrorCode(Errors.InvalidRequest)
          .WithMessage("The specified email address is already registered");
      });
    }
  }

  public class CommandHandler : Handler<Command, IResponse<UserDto>>
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

    public override async Task<IResponse<UserDto>> Handle(
      Command request, CancellationToken cancellationToken)
    {
      var user = (await _userManager.GetUserAsync(_httpContextAccessor.HttpContext!.User))!;

      if (string.IsNullOrEmpty(request.Password) == false)
      {
        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);
      }

      if (string.IsNullOrEmpty(request.Email) == false)
      {
        user.Email = request.Email;
        user.UserName = request.Email;
      }

      if (string.IsNullOrEmpty(request.UserName) == false)
      {
        user.UserName = request.UserName;
      }

      var result = await _userManager.UpdateAsync(user);

      if (!result.Succeeded)
      {
        return Response<UserDto>(new(), result.Errors.Select(x => new ValidationFailure
        {
          ErrorCode = x.Code,
          ErrorMessage = x.Description
        }));
      }

      return Response(UserMapper.ToDto(user));
    }
  }
}
