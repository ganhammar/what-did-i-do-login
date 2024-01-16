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
public class GenerateAuthenticatorKeyCommandTests : TestBase
{
  [Fact]
  public async Task Should_GenerateKey_When_CommandIsValid() =>
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
      var command = new GenerateAuthenticatorKeyCommand.Command();

      // Act
      var response = await mediator.Send(command);

      // Assert
      Assert.True(response.IsValid);
      Assert.False(string.IsNullOrEmpty(response.Result!.Key));
    });

  [Fact]
  public async Task Should_GenerateKey_When_AuthenticatorAlreadySet() =>
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
      await userManager.ResetAuthenticatorKeyAsync(user);
      var command = new GenerateAuthenticatorKeyCommand.Command();

      // Act
      var response = await mediator.Send(command);

      // Assert
      Assert.True(response.IsValid);
      Assert.False(string.IsNullOrEmpty(response.Result!.Key));
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
      var command = new GenerateAuthenticatorKeyCommand.Command();

      // Act
      var response = await mediator.Send(command);

      // Assert
      Assert.False(response.IsValid);
      Assert.Contains(response.Errors, error =>
        error.ErrorCode == Errors.InvalidToken);
    });
}
