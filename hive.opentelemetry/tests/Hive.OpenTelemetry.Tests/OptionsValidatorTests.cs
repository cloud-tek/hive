using FluentAssertions;
using FluentValidation.TestHelper;
using Hive.Testing;
using Xunit;

namespace Hive.OpenTelemetry.Tests;

public class OptionsValidatorTests
{
  private readonly OptionsValidator _validator;

  public OptionsValidatorTests()
  {
    _validator = new OptionsValidator();
  }

  #region OTLP Endpoint Validation

  [Fact]
  [UnitTest]
  public void GivenValidHttpEndpoint_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = "http://localhost:4317"
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Otlp.Endpoint);
  }

  [Fact]
  [UnitTest]
  public void GivenValidHttpsEndpoint_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = "https://otel-collector.example.com:4318"
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Otlp.Endpoint);
  }

  [Fact]
  [UnitTest]
  public void GivenNullEndpoint_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = null
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Otlp.Endpoint);
  }

  [Fact]
  [UnitTest]
  public void GivenEmptyEndpoint_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = string.Empty
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Otlp.Endpoint);
  }

  [Fact]
  [UnitTest]
  public void GivenInvalidUri_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = "not-a-valid-uri"
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Endpoint)
      .WithErrorMessage("Invalid OTLP endpoint URL 'not-a-valid-uri'. Must be a valid absolute URI (e.g., 'http://localhost:4317')");
  }

  [Fact]
  [UnitTest]
  public void GivenRelativeUri_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = "relative/path"
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Endpoint)
      .WithErrorMessage("Invalid OTLP endpoint URL 'relative/path'. Must be a valid absolute URI (e.g., 'http://localhost:4317')");
  }

  [Fact]
  [UnitTest]
  public void GivenFtpScheme_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = "ftp://localhost:4317"
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Endpoint)
      .WithErrorMessage("OTLP endpoint must use http or https scheme");
  }

  [Fact]
  [UnitTest]
  public void GivenWsScheme_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = "ws://localhost:4317"
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Endpoint)
      .WithErrorMessage("OTLP endpoint must use http or https scheme");
  }

  #endregion

  #region Timeout Validation

  [Fact]
  [UnitTest]
  public void GivenValidTimeout_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        TimeoutMilliseconds = 10000
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Otlp.TimeoutMilliseconds);
  }

  [Fact]
  [UnitTest]
  public void GivenMinimumTimeout_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        TimeoutMilliseconds = 1000
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Otlp.TimeoutMilliseconds);
  }

  [Fact]
  [UnitTest]
  public void GivenMaximumTimeout_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        TimeoutMilliseconds = 60000
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Otlp.TimeoutMilliseconds);
  }

  [Fact]
  [UnitTest]
  public void GivenTimeoutBelowMinimum_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        TimeoutMilliseconds = 999
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.TimeoutMilliseconds)
      .WithErrorMessage("Timeout must be between 1000 and 60000 milliseconds");
  }

  [Fact]
  [UnitTest]
  public void GivenTimeoutAboveMaximum_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        TimeoutMilliseconds = 60001
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.TimeoutMilliseconds)
      .WithErrorMessage("Timeout must be between 1000 and 60000 milliseconds");
  }

  [Fact]
  [UnitTest]
  public void GivenNegativeTimeout_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        TimeoutMilliseconds = -1
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.TimeoutMilliseconds)
      .WithErrorMessage("Timeout must be between 1000 and 60000 milliseconds");
  }

  [Fact]
  [UnitTest]
  public void GivenZeroTimeout_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        TimeoutMilliseconds = 0
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.TimeoutMilliseconds)
      .WithErrorMessage("Timeout must be between 1000 and 60000 milliseconds");
  }

  #endregion

  #region Header Validation

  [Fact]
  [UnitTest]
  public void GivenValidHeaders_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          ["x-api-key"] = "secret",
          ["x-custom-header"] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }

  [Fact]
  [UnitTest]
  public void GivenEmptyHeaders_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>()
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }

  [Fact]
  [UnitTest]
  public void GivenEmptyHeaderKey_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          [string.Empty] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Headers)
      .WithErrorMessage("Header keys cannot be null or whitespace");
  }

  [Fact]
  [UnitTest]
  public void GivenHeaderKeyWithComma_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          ["x-header,invalid"] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Headers)
      .WithErrorMessage("Header key 'x-header,invalid' contains invalid characters (control characters, comma, or equals sign)");
  }

  [Fact]
  [UnitTest]
  public void GivenHeaderKeyWithEquals_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          ["x-header=invalid"] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Headers)
      .WithErrorMessage("Header key 'x-header=invalid' contains invalid characters (control characters, comma, or equals sign)");
  }

  [Fact]
  [UnitTest]
  public void GivenHeaderKeyWithControlCharacter_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          ["x-header\ninvalid"] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Headers)
      .WithErrorMessage("Header key 'x-header\ninvalid' contains invalid characters (control characters, comma, or equals sign)");
  }

  [Fact]
  [UnitTest]
  public void GivenHeaderValueWithComma_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          ["x-header"] = "value,invalid"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Headers)
      .WithErrorMessage("Header value for 'x-header' contains invalid characters (control characters, comma, or equals sign). Per W3C Baggage spec, use percent-encoding for special characters.");
  }

  [Fact]
  [UnitTest]
  public void GivenHeaderValueWithEquals_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          ["Authorization"] = "Bearer=token123"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Headers)
      .WithErrorMessage("Header value for 'Authorization' contains invalid characters (control characters, comma, or equals sign). Per W3C Baggage spec, use percent-encoding for special characters.");
  }

  [Fact]
  [UnitTest]
  public void GivenHeaderValueWithControlCharacter_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          ["x-header"] = "value\ninvalid"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Headers)
      .WithErrorMessage("Header value for 'x-header' contains invalid characters (control characters, comma, or equals sign). Per W3C Baggage spec, use percent-encoding for special characters.");
  }

  [Fact]
  [UnitTest]
  public void GivenNullHeaderValue_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Headers = new Dictionary<string, string>
        {
          ["x-header"] = null!
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Otlp.Headers);
  }

  #endregion

  #region Resource Attribute Validation

  [Fact]
  [UnitTest]
  public void GivenValidResourceAttributes_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Resource = new ResourceOptions
      {
        Attributes = new Dictionary<string, string>
        {
          ["environment"] = "production",
          ["region"] = "us-east-1"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }

  [Fact]
  [UnitTest]
  public void GivenEmptyResourceAttributes_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Resource = new ResourceOptions
      {
        Attributes = new Dictionary<string, string>()
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }

  [Fact]
  [UnitTest]
  public void GivenEmptyAttributeKey_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Resource = new ResourceOptions
      {
        Attributes = new Dictionary<string, string>
        {
          [string.Empty] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Resource.Attributes)
      .WithErrorMessage("Resource attribute keys cannot be null or whitespace");
  }

  [Fact]
  [UnitTest]
  public void GivenWhitespaceAttributeKey_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Resource = new ResourceOptions
      {
        Attributes = new Dictionary<string, string>
        {
          ["   "] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Resource.Attributes)
      .WithErrorMessage("Resource attribute keys cannot be null or whitespace");
  }

  [Fact]
  [UnitTest]
  public void GivenAttributeKeyWithControlCharacter_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Resource = new ResourceOptions
      {
        Attributes = new Dictionary<string, string>
        {
          ["attribute\nkey"] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Resource.Attributes)
      .WithErrorMessage("Resource attribute key 'attribute\nkey' contains control characters");
  }

  [Fact]
  [UnitTest]
  public void GivenAttributeKeyWithTab_WhenValidating_ThenValidationFails()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Resource = new ResourceOptions
      {
        Attributes = new Dictionary<string, string>
        {
          ["attribute\tkey"] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Resource.Attributes)
      .WithErrorMessage("Resource attribute key 'attribute\tkey' contains control characters");
  }

  [Fact]
  [UnitTest]
  public void GivenNullAttributeValue_WhenValidating_ThenValidationSucceeds()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Resource = new ResourceOptions
      {
        Attributes = new Dictionary<string, string>
        {
          ["key"] = null!
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveValidationErrorFor(x => x.Resource.Attributes);
  }

  #endregion

  #region Combined Validation Scenarios

  [Fact]
  [UnitTest]
  public void GivenMultipleValidationErrors_WhenValidating_ThenAllErrorsAreReported()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = "ftp://invalid:4317",
        TimeoutMilliseconds = 0,
        Headers = new Dictionary<string, string>
        {
          ["x-header,invalid"] = "value"
        }
      },
      Resource = new ResourceOptions
      {
        Attributes = new Dictionary<string, string>
        {
          ["attr\nkey"] = "value"
        }
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Endpoint);
    result.ShouldHaveValidationErrorFor(x => x.Otlp.TimeoutMilliseconds);
    result.ShouldHaveValidationErrorFor(x => x.Otlp.Headers);
    result.ShouldHaveValidationErrorFor(x => x.Resource.Attributes);
  }

  [Fact]
  [UnitTest]
  public void GivenCompletelyValidOptions_WhenValidating_ThenNoValidationErrors()
  {
    // Arrange
    var options = new OpenTelemetryOptions
    {
      Otlp = new OtlpOptions
      {
        Endpoint = "https://otel-collector.example.com:4318",
        TimeoutMilliseconds = 15000,
        Headers = new Dictionary<string, string>
        {
          ["x-api-key"] = "secret-key",
          ["x-tenant-id"] = "tenant-123"
        }
      },
      Resource = new ResourceOptions
      {
        ServiceNamespace = "production",
        ServiceVersion = "1.0.0",
        Attributes = new Dictionary<string, string>
        {
          ["environment"] = "production",
          ["region"] = "us-east-1",
          ["team"] = "platform"
        }
      },
      Logging = new LoggingOptions
      {
        EnableConsoleExporter = true
      },
      Tracing = new TracingOptions
      {
        EnableAspNetCoreInstrumentation = true,
        EnableHttpClientInstrumentation = true
      },
      Metrics = new MetricsOptions
      {
        EnableRuntimeInstrumentation = true
      }
    };

    // Act
    var result = _validator.TestValidate(options);

    // Assert
    result.ShouldNotHaveAnyValidationErrors();
  }

  #endregion
}
