# Key Architecture Decisions: Cloud-Agnostic Migration
*Created: 2025-09-14*

## Decision 1: Service Abstraction Pattern

### Context
The application currently uses Azure-specific services directly in the infrastructure layer. To achieve cloud agnosticism, we need to decouple business logic from cloud provider implementations while maintaining performance and functionality.

### Options Considered

#### Option A: Factory Pattern with Provider Selection
- Create factory classes that instantiate provider-specific implementations
- Runtime provider selection based on configuration
- Minimal changes to existing dependency injection

#### Option B: Strategy Pattern with Unified Interfaces
- Define common interfaces for all cloud services (AI, Storage, Document Processing)
- Implement provider-specific strategies for each interface
- Configuration-driven strategy selection at startup

#### Option C: Adapter Pattern with Provider Wrappers
- Wrap existing Azure services with generic adapters
- Create equivalent adapters for other cloud providers
- Translate between generic calls and provider-specific APIs

### Chosen Solution: Strategy Pattern with Unified Interfaces (Option B)

### Rationale
- **Clean Architecture**: Provides the clearest separation between business logic and infrastructure concerns
- **Testability**: Easy to mock and test individual provider implementations
- **Extensibility**: New providers can be added without modifying existing code
- **Performance**: Direct provider implementations without wrapper overhead
- **Type Safety**: Strong typing through interfaces ensures compile-time validation

### Implementation Details
```csharp
// Core interfaces
public interface IAIService { ... }
public interface IStorageService { ... }
public interface IDocumentProcessingService { ... }

// Provider implementations
public class AzureOpenAIService : IAIService { ... }
public class AWSBedrockService : IAIService { ... }
public class GoogleVertexAIService : IAIService { ... }

// Configuration-driven selection
services.AddScoped<IAIService>(provider =>
    CloudProvider switch {
        "Azure" => new AzureOpenAIService(...),
        "AWS" => new AWSBedrockService(...),
        _ => throw new NotSupportedException()
    });
```

## Decision 2: Configuration Management Architecture

### Context
Different cloud providers require different configuration parameters, authentication methods, and service endpoints. The system needs to manage these configurations while maintaining security and deployment flexibility.

### Options Considered

#### Option A: Single Configuration File per Provider
- Separate appsettings files for each cloud provider
- Provider selection determines which config file to load
- Simple but requires deployment-time provider selection

#### Option B: Unified Configuration with Provider Sections
- Single configuration file with provider-specific sections
- Runtime selection of active provider configuration
- More complex but allows runtime switching

#### Option C: Environment Variable-Driven Configuration
- All configuration through environment variables
- Provider-agnostic variable naming with provider-specific values
- Maximum deployment flexibility but potentially verbose

### Chosen Solution: Hybrid Approach (Options B + C)

### Rationale
- **Flexibility**: Environment variables override config file values for deployment-specific needs
- **Security**: Sensitive values (API keys) come from environment/secrets only
- **Maintainability**: Config files provide structure and defaults
- **DevOps Friendly**: CI/CD pipelines can easily set provider-specific variables

### Implementation Details
```json
{
  "CloudProvider": {
    "Active": "Azure", // Can be overridden by CLOUD_PROVIDER env var
    "Azure": {
      "OpenAI": { "Endpoint": "...", "DeploymentName": "..." },
      "BlobStorage": { "ConnectionString": "...", "ContainerName": "..." }
    },
    "AWS": {
      "Bedrock": { "Region": "us-east-1", "Model": "anthropic.claude-v2" },
      "S3": { "BucketName": "...", "Region": "..." }
    }
  }
}
```

## Decision 3: Data Format Standardization Strategy

### Context
Different cloud providers return data in different formats with varying levels of detail. We need to normalize responses while preserving provider-specific capabilities and maintaining existing API contracts.

### Options Considered

#### Option A: Lowest Common Denominator
- Standardize on the minimal feature set available across all providers
- Simple implementation but limits functionality
- May reduce application capabilities

#### Option B: Feature-Rich Union Type
- Create comprehensive response types that support all provider features
- Complex implementation with many optional fields
- Preserves all capabilities but increases complexity

#### Option C: Core Standard + Provider Extensions
- Define core response format for common functionality
- Allow provider-specific extensions in metadata
- Balanced approach between simplicity and capability preservation

### Chosen Solution: Core Standard + Provider Extensions (Option C)

### Rationale
- **API Stability**: Existing clients continue to work with core response format
- **Provider Optimization**: Applications can access provider-specific features when needed
- **Future Proof**: New provider capabilities can be added without breaking changes
- **Performance**: Core responses remain lightweight while extensions are optional

### Implementation Details
```csharp
public class AIResponse
{
    // Core standardized properties
    public bool Success { get; set; }
    public string GeneratedText { get; set; }
    public double ConfidenceScore { get; set; } // Normalized 0-1
    public TimeSpan ProcessingTime { get; set; }

    // Provider-specific extensions
    public Dictionary<string, object> Metadata { get; set; }
    public string Provider { get; set; }
}

// Provider-specific metadata examples:
// Azure: { "token_count": 150, "model": "gpt-4" }
// AWS: { "input_tokens": 100, "output_tokens": 50, "model_id": "claude-v2" }
```

## Technical Stack Decisions

### Cloud Provider SDKs
- **Azure**: Existing Azure SDK packages (minimal changes)
- **AWS**: AWS SDK for .NET v3.x (latest stable)
- **Google Cloud**: Google Cloud Client Libraries for .NET
- **OpenAI**: Official OpenAI .NET SDK for direct API access

### Service Mapping Strategy

#### AI Services
| Provider | Service | .NET SDK | Notes |
|----------|---------|----------|-------|
| Azure | OpenAI Service | Azure.AI.OpenAI | Current implementation |
| AWS | Amazon Bedrock | AWS.BedrockRuntime | Claude, Jurassic models |
| Google | Vertex AI | Google.Cloud.AIPlatform.V1 | PaLM, Gemini models |
| OpenAI | Direct API | OpenAI.NET | Fallback option |

#### Storage Services
| Provider | Service | .NET SDK | Notes |
|----------|---------|----------|-------|
| Azure | Blob Storage | Azure.Storage.Blobs | Current implementation |
| AWS | Amazon S3 | AWS.S3 | Standard object storage |
| Google | Cloud Storage | Google.Cloud.Storage.V1 | Standard object storage |
| Local | File System | System.IO | Development/testing |

#### Document Processing
| Provider | Service | .NET SDK | Notes |
|----------|---------|----------|-------|
| Azure | Form Recognizer | Azure.AI.FormRecognizer | Current implementation |
| AWS | Amazon Textract | AWS.Textract | Advanced OCR capabilities |
| Google | Document AI | Google.Cloud.DocumentAI.V1 | ML-powered extraction |
| Local | Mock/Tesseract | Custom | Development/testing |

### Dependency Injection Strategy
- Use factory pattern for provider selection
- Implement health checks for all providers
- Support graceful degradation when services unavailable
- Enable hot-swapping of providers for disaster recovery

### Error Handling Standardization
- Common exception types across providers
- Consistent retry policies with exponential backoff
- Standardized error codes and messages
- Provider-specific error details in metadata

## Next Steps
1. Design detailed interface contracts for each service type
2. Implement provider factories with configuration validation
3. Create comprehensive testing framework for multi-provider scenarios
4. Establish monitoring and alerting for provider-specific metrics
5. Plan deployment templates for each supported cloud platform