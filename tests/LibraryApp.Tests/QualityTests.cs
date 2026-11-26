using LibraryApp;
using Xunit;

namespace LibraryApp.Tests;

public class SecurityTests
{
    [Fact]
    public void HashAndVerify_AcceptsCorrectPassword()
    {
        var salt = Security.NewSalt();
        var hash = Security.Hash("StrongPass9", salt);
        Assert.True(Security.Verify("StrongPass9", salt, hash));
    }

    [Fact]
    public void Verify_RejectsWrongPassword()
    {
        var salt = Security.NewSalt();
        var hash = Security.Hash("StrongPass9", salt);
        Assert.False(Security.Verify("WrongPass9", salt, hash));
    }

    [Fact]
    public void NewSalt_GeneratesDifferentValues()
    {
        Assert.NotEqual(Security.NewSalt(), Security.NewSalt());
    }

    [Theory]
    [InlineData("abc", false)]
    [InlineData("alllowercase9", false)]
    [InlineData("ALLUPPERCASE9", false)]
    [InlineData("NoDigitsHere", false)]
    [InlineData("StrongPass9", true)]
    public void PasswordPolicy_ChecksComplexity(string password, bool expectedValid)
    {
        Assert.Equal(expectedValid, Security.CheckPasswordPolicy(password) == null);
    }
}

public class SessionTests
{
    [Fact]
    public void Title_MapsDatabaseRolesToUiTitles()
    {
        Assert.Equal("Ученик", Session.Title(Session.Reader));
        Assert.Equal("Сотрудник", Session.Title(Session.Librarian));
        Assert.Equal("Администратор", Session.Title(Session.Admin));
    }

    [Fact]
    public void Clear_RemovesAuthenticatedSession()
    {
        Session.Set(10, "test", Session.Admin, null, null);
        Assert.True(Session.IsLoggedIn);
        Session.Clear();
        Assert.False(Session.IsLoggedIn);
        Assert.Null(Session.Login);
        Assert.Null(Session.RoleName);
    }
}
