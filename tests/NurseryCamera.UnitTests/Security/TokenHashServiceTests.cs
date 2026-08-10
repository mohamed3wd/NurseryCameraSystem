using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NurseryCamera.Infrastructure.Security;

namespace NurseryCamera.UnitTests.Security;

public sealed class TokenHashServiceTests
{
    private readonly TokenHashService _sut = new();

    [Fact]
    public void Hash_ReturnsLowercaseSha256Hex()
    {
        const string raw = "stream-token-secret";
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

        var hash = _sut.Hash(raw);

        hash.Should().Be(expected);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Hash_IsDeterministic_AndDifferentForDifferentInputs()
    {
        var a = _sut.Hash("token-a");
        var b = _sut.Hash("token-b");

        _sut.Hash("token-a").Should().Be(a);
        b.Should().NotBe(a);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForMatchingHash()
    {
        const string raw = "verify-me";
        var hash = _sut.Hash(raw);

        _sut.Verify(raw, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMismatchedHash()
    {
        var hash = _sut.Hash("correct");

        _sut.Verify("wrong", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_Throws_WhenRawValueMissing(string? raw)
    {
        var act = () => _sut.Hash(raw!);

        act.Should().Throw<ArgumentException>();
    }
}
