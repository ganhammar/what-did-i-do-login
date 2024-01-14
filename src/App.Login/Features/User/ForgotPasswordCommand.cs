using System.Web;
using App.Login.Features.Email;
using App.Login.Infrastructure;
using AspNetCore.Identity.AmazonDynamoDB;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace App.Login.Features.User;

public class ForgotPasswordCommand
{
  public class Command : IRequest<IResponse>
  {
    public string? Email { get; set; }
    public string? ResetUrl { get; set; }
  }

  public class CommandValidator : AbstractValidator<Command>
  {
    public CommandValidator()
    {
      RuleFor(x => x.Email)
        .NotEmpty()
        .EmailAddress();

      RuleFor(x => x.ResetUrl)
        .NotEmpty();
    }
  }

  public class CommandHandler : Handler<Command, IResponse>
  {
    private readonly UserManager<DynamoDbUser> _userManager;
    private readonly IEmailSender _emailSender;

    public CommandHandler(
      UserManager<DynamoDbUser> userManager,
      IEmailSender emailSender)
    {
      _userManager = userManager;
      _emailSender = emailSender;
    }

    public override async Task<IResponse> Handle(
      Command request, CancellationToken cancellationToken)
    {
      var user = await _userManager.FindByEmailAsync(request.Email!);

      if (user != default)
      {
        await SendResetEmail(user, request.ResetUrl);
      }

      return Response();
    }

    private async Task SendResetEmail(DynamoDbUser user, string? resetUrl)
    {
      var token = await _userManager.GeneratePasswordResetTokenAsync(user);
      var url = $"{resetUrl}?UserId={user.Id}&Token={HttpUtility.UrlEncode(token)}";

      var body = $"Follow the link below to reset your WDID account password:<br /><a href=\"{url}\">{url}</a>";

      await _emailSender.Send(user.Email!, "Reset Password", body);
    }
  }
}
