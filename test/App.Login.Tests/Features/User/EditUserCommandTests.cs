using App.Login.Features.User;
using App.Login.Tests.Infrastructure;
using AspNetCore.Identity.AmazonDynamoDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace App.Login.Tests.Features.User;

[Collection(Constants.DatabaseCollection)]
[Cleanup]
public class EditUserCommandTests : TestBase
{
  [Fact]
  public async Task Should_EditUser_When_CommandIsValid() =>
    await MediatorTest(async (mediator, services) =>
    {
      // Arrange
      var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
      var email = "valid@wdid.fyi";
      var password = "itsaseasyas123";
      var user = new DynamoDbUser
      {
        Email = email,
        UserName = email,
        EmailConfirmed = true,
        TwoFactorEnabled = true,
      };
      await userManager.CreateAsync(user, password);
      await mediator.Send(new LoginCommand.Command
      {
        Email = email,
        Password = password,
        RememberMe = false,
      });
      var command = new EditUserCommand.Command
      {
        Password = "itsaseasyas1234",
        UserName = "valid",
        Email = email,
      };

      // Act
      var result = await mediator.Send(command);

      // Assert
      Assert.True(result.IsValid);
      Assert.Equal(email, result.Result!.Email);
    });

  [Fact]
  public async Task Should_NotBeValid_When_EmailIsNotAnEmailAddress() =>
    await MediatorTest(async (mediator, services) =>
    {
      // Arrange
      var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
      var email = "valid@wdid.fyi";
      var password = "itsaseasyas123";
      var user = new DynamoDbUser
      {
        Email = email,
        UserName = email,
        EmailConfirmed = true,
        TwoFactorEnabled = true,
      };
      await userManager.CreateAsync(user, password);
      await mediator.Send(new LoginCommand.Command
      {
        Email = email,
        Password = password,
        RememberMe = false,
      });
      var command = new EditUserCommand.Command
      {
        Password = "itsaseasyas1234",
        Email = "notanemailaddress",
      };

      // Act
      var result = await mediator.Send(command);

      // Assert
      Assert.False(result.IsValid);
    });

  [Fact]
  public async Task Should_RequireLogin_When_UserIsntAuthenticated() =>
    await MediatorTest(async (mediator, services) =>
    {
      // Arrange
      var httpContext = GetMock<HttpContext>();
      var featureCollection = new FeatureCollection();
      featureCollection.Set(new OpenIddictServerAspNetCoreFeature
      {
        Transaction = new OpenIddictServerTransaction
        {
          Request = new OpenIddictRequest
          {
            Prompt = Prompts.None,
          },
        },
      });
      httpContext!.Setup(x => x.Features).Returns(featureCollection);
      var command = new EditUserCommand.Command
      {
        Password = "itsaseasyas1234",
      };

      // Act
      var response = await mediator.Send(command);

      // Assert
      Assert.False(response.IsValid);
      Assert.Contains(response.Errors, error =>
        error.ErrorCode == Errors.InvalidToken);
    });
}
