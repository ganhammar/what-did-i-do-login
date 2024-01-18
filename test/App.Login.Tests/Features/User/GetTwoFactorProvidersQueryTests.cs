using App.Login.Features.User;
using App.Login.Infrastructure.Validators;
using App.Login.Tests.Infrastructure;
using AspNetCore.Identity.AmazonDynamoDB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace App.Login.Tests.Features.User;

[Collection(Constants.DatabaseCollection)]
[Cleanup]
public class GetTwoFactorProvidersQueryTests : TestBase
{
  [Fact]
  public async Task Should_ReturnListOfTwoFactorProviders_When_CommandIsValid() =>
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
        LockoutEnabled = false,
      };
      await userManager.CreateAsync(user, password);

      await mediator.Send(new LoginCommand.Command
      {
        Email = email,
        Password = password,
        RememberMe = false,
      });
      var query = new GetTwoFactorProvidersQuery.Query();

      // Act
      var providers = await mediator.Send(query);

      // Assert
      Assert.True(providers.IsValid);
      Assert.True(providers.Result!.Any());
    });

  [Fact]
  public async Task Should_ReturnListOfTwoFactorProviders_When_UserIsLoggedIn() =>
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
        TwoFactorEnabled = false,
        LockoutEnabled = false,
      };
      await userManager.CreateAsync(user, password);
      await mediator.Send(new LoginCommand.Command
      {
        Email = email,
        Password = password,
        RememberMe = false,
      });
      var query = new GetTwoFactorProvidersQuery.Query();

      // Act
      var providers = await mediator.Send(query);

      // Assert
      Assert.True(providers.IsValid);
      Assert.True(providers.Result!.Any());
    });

  [Fact]
  public async Task Should_NotBeValid_When_ThereIsNoLoginInProgress() =>
    await MediatorTest(async (mediator, services) =>
    {
      // Arrange
      var userManager = services.GetRequiredService<UserManager<DynamoDbUser>>();
      var signInManager = services.GetRequiredService<SignInManager<DynamoDbUser>>();
      var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
      var validator = new GetTwoFactorProvidersQuery.QueryValidator(
        signInManager, userManager, httpContextAccessor);
      var query = new GetTwoFactorProvidersQuery.Query();

      // Act
      var response = await validator.ValidateAsync(query);

      // Assert
      Assert.False(response.IsValid);
      Assert.Contains(response.Errors, error =>
        error.ErrorCode == nameof(ErrorCodes.NoLoginAttemptInProgress));
    });
}
