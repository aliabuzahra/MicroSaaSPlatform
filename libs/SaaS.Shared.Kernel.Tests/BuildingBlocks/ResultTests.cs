using FluentAssertions;
using SaaS.Shared.Kernel.BuildingBlocks;
using Xunit;

namespace SaaS.Shared.Kernel.Tests.BuildingBlocks;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        var error = "Something went wrong";
        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
    
    [Fact]
    public void SuccessGeneric_ShouldCreateSuccessResultWithValue()
    {
        var value = "Test Value";
        var result = Result<string>.Success(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Error.Should().BeEmpty();
    }

    [Fact]
    public void FailureGeneric_ShouldCreateFailureResultAndThrowOnValueAccess()
    {
        var error = "Error";
        var result = Result<string>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
        
        Action act = () => { var v = result.Value; };
        act.Should().Throw<InvalidOperationException>();
    }
}
