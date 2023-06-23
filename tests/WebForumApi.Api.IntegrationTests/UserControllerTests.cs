﻿using Bogus;
using FluentAssertions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebForumApi.Api.IntegrationTests.Common;
using WebForumApi.Application.Common.Responses;
using WebForumApi.Application.Features.Auth;
using WebForumApi.Application.Features.Auth.Authenticate;
using WebForumApi.Application.Features.Users;
using WebForumApi.Application.Features.Users.CreateUser;
using WebForumApi.Application.Features.Users.GetUsers;
using WebForumApi.Application.Features.Users.UpdateUser;
using WebForumApi.Domain.Entities.Common;

namespace WebForumApi.Api.IntegrationTests;

public class UserControllerTests : BaseTest
{
    private static string? _adminToken;

    private static string? _userToken;

    public UserControllerTests(CustomWebApplicationFactory apiFactory)
        : base(apiFactory) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _adminToken ??= await GetAdminToken();
        _userToken ??= await GetUserToken();
        LoginAsAdmin();
    }

    protected void UpdateBearerToken(string? token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void LoginAsAdmin()
    {
        UpdateBearerToken(_adminToken);
    }

    private void LoginAsUser()
    {
        UpdateBearerToken(_userToken);
    }

    #region PATCH

    [Fact]
    public async Task Patch_ValidUser_UpdatePassword_NoContent()
    {
        // Arrange
        Faker<CreateUserRequest> userFaker = new();

        CreateUserRequest? newUser = userFaker
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.Password, f => f.Internet.Password())
            .Generate();
        HttpResponseMessage response = await PostAsync("/api/User", newUser);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        response = await PostAsync("/api/User/authenticate", newUser);
        Jwt? newUserToken = await response.Content.ReadFromJsonAsync<Jwt>();
        UpdateBearerToken(newUserToken!.AccessToken);

        // Act
        response = await PatchAsync(
            "/api/User/password",
            new UpdateUserRequest { Password = "mypasswordisverynice" }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET

    [Fact]
    public async Task Get_AllUsers_ReturnsOk()
    {
        // Act
        PaginatedList<GetUserResponse>? response = await GetAsync<PaginatedList<GetUserResponse>>(
            "/api/User"
        );

        // Assert
        response.Should().NotBeNull();
        response!.Result.Should().OnlyHaveUniqueItems();
        response.Result.Should().HaveCount(2);
        response.CurrentPage.Should().Be(1);
        response.TotalItems.Should().Be(2);
        response.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Get_AllUsersWithPaginationFilter_ReturnsOk()
    {
        // Act
        PaginatedList<GetUserResponse>? response = await GetAsync<PaginatedList<GetUserResponse>>(
            "/api/User",
            new GetUsersRequest { CurrentPage = 1, PageSize = 1 }
        );

        // Assert
        response.Should().NotBeNull();
        response!.Result.Should().OnlyHaveUniqueItems();
        response.Result.Should().HaveCount(1);
        response.CurrentPage.Should().Be(1);
        response.TotalItems.Should().Be(2);
        response.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Get_ExistingUsersWithFilter_ReturnsOk()
    {
        // Act
        PaginatedList<GetUserResponse>? response = await GetAsync<PaginatedList<GetUserResponse>>(
            "/api/User",
            new GetUsersRequest { Username = "admin@boilerplate.com" }
        );

        // Assert
        response.Should().NotBeNull();
        response!.Result.Should().OnlyHaveUniqueItems();
        response.Result.Should().HaveCount(1);
        response.CurrentPage.Should().Be(1);
        response.TotalItems.Should().Be(1);
        response.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Get_NonExistingUsersWithFilter_ReturnsOk()
    {
        // Act
        PaginatedList<GetUserResponse>? response = await GetAsync<PaginatedList<GetUserResponse>>(
            "/api/User",
            new GetUsersRequest { Username = "admifsdfsdfsdjma" }
        );

        // Assert
        response.Should().NotBeNull();
        response!.Result.Should().BeEmpty();
        response.CurrentPage.Should().Be(1);
        response.TotalItems.Should().Be(0);
        response.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsOk()
    {
        // Act
        GetUserResponse? response = await GetAsync<GetUserResponse>(
            "/api/User/2e3b7a21-f06e-4c47-b28a-89bdaa3d2a37"
        );

        // Assert
        response.Should().NotBeNull();
        response!.Id.Should().NotBe(UserId.Empty);
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await GetAsync($"/api/User/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST

    [Fact]
    public async Task<string> GetAdminToken()
    {
        // Act
        AuthenticateRequest loginData =
            new() { Username = "admin@boilerplate.com", Password = "testpassword123" };

        Jwt? response = await PostAsync<Jwt>("/api/User/authenticate", loginData);
        response.Should().NotBeNull();
        response!.Expire.Should().NotBe(DateTime.MinValue);
        response.AccessToken.Should().NotBeNullOrWhiteSpace();

        return response.AccessToken;
    }

    [Fact]
    public async Task<string> GetUserToken()
    {
        // Act
        AuthenticateRequest loginData =
            new() { Username = "user@boilerplate.com", Password = "testpassword123" };

        Jwt? response = await PostAsync<Jwt>("/api/User/authenticate", loginData);
        response.Should().NotBeNull();
        response!.Expire.Should().NotBe(DateTime.MinValue);
        response.AccessToken.Should().NotBeNullOrWhiteSpace();

        return response.AccessToken;
    }

    [Theory]
    [InlineData("admin@boilerplate.com", "incorrect")]
    [InlineData("admin@incorrect.com", "testpassword123")]
    public async Task Authenticate_IncorretUserOrPassword(string email, string password)
    {
        // Act
        AuthenticateRequest loginData = new() { Username = email, Password = password };
        HttpResponseMessage response = await PostAsync("/api/User/authenticate", loginData);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ValidUser_ReturnsCreated()
    {
        // Arrange
        Faker<CreateUserRequest> userFaker = new();

        // Act
        CreateUserRequest? newUser = userFaker
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.Password, f => f.Internet.Password())
            .Generate();
        GetUserResponse? response = await PostAsync<GetUserResponse>("/api/User", newUser);

        // Assert
        response.Should().NotBeNull();
        response!.Id.Should().NotBe(UserId.Empty);
    }

    [Fact]
    public async Task Post_EmaillessUser_ReturnsBadRequest()
    {
        // Act
        CreateUserRequest newUser = new() { Password = "mypasswordisnice" };
        HttpResponseMessage response = await PostAsync("/api/User", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_PasswordlessUser_ReturnsBadRequest()
    {
        // Arrange
        Faker<CreateUserRequest> userFaker = new();

        // Act
        CreateUserRequest? newUser = userFaker
            .RuleFor(x => x.Email, f => f.Internet.Email())
            .RuleFor(x => x.Password, _ => null!)
            .Generate();
        HttpResponseMessage response = await PostAsync("/api/User", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_EmptyUser_ReturnsBadRequest()
    {
        // Act
        CreateUserRequest newUser = new();
        HttpResponseMessage response = await PostAsync("/api/User", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE

    [Fact]
    public async Task Delete_ValidUser_ReturnsNoContent()
    {
        // Act
        HttpResponseMessage response = await DeleteAsync(
            "/api/User/c68acd7b-9054-4dc3-b536-17a1b81fa7a3"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_InvalidUser_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await DeleteAsync($"/api/User/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AsUserRole_ReturnsForbidden()
    {
        // Arrange
        LoginAsUser();

        // act
        HttpResponseMessage response = await DeleteAsync($"/api/User/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
