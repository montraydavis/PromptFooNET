# PromptFooNET 🚀

A C# NUnit testing framework that bridges the gap between .prompty files and Promptfoo CLI, enabling automated evaluation of AI prompts in your solution.

## Key Features ✨

- **Automatic Prompt Testing**: Discovers and evaluates all `.prompty` files in your solution
- **Format Conversion**: Seamlessly converts `.prompty` files to Promptfoo-compatible YAML format
- **NUnit Integration**: Use familiar `[AITest]` attributes to mark test methods
- **Azure OpenAI Support**: Built-in support for Azure OpenAI endpoints
- **Parallel Execution**: Run multiple prompt evaluations concurrently
- **CI/CD Ready**: Easy integration with your existing test pipeline

## Why PromptFooNET? 🤔

Traditional `.prompty` files are great for defining conversations but lack evaluation capabilities. Promptfoo is excellent for evaluating prompts but expects a different format. PromptFooNET bridges this gap by:

1. **Converting**: Transforms `.prompty` conversation format to Promptfoo YAML
2. **Evaluating**: Runs converted files through Promptfoo CLI
3. **Reporting**: Integrates results into NUnit test reports

## Prerequisites

- .NET 8.0+
- Node.js 16+
- Promptfoo CLI: `npm install -g promptfoo`
- Azure OpenAI subscription (for default provider)

## Installation

```powershell
dotnet add package PromptFooNET
```

## Usage

### 1. Create a Prompt File (example.prompty)
```yaml
---
description: Product classification prompt
sample:
  product_name: "Wireless Headphones"
  product_description: "Over-ear bluetooth headphones with noise cancellation"
---
system: You are a product classifier.

user: Please classify the following product:
Name: {{product_name}}
Description: {{product_description}}

assistant: Here's the classification in JSON format.
```

### 2. Write a Test
```csharp
[Test]
[AITest("Prompts/example.prompty")]
public async Task ProductClassification_ShouldPass()
{
    Assert.Pass("Prompt evaluation completed");
}
```

### 3. Auto-Discovery
```csharp
var discoveryService = new TestDiscoveryService();
var promptFiles = discoveryService.DiscoverPromptFiles("./Solution");
```

## YAML Conversion

PromptFooNET converts `.prompty` files to Promptfoo format:

```yaml
---
description: Test prompt
sample:
  text: "example"
---
system: You are helpful.
user: Process {{text}}

# Output (Promptfoo YAML)
description: Test prompt
prompts:
  - "system: You are helpful.\nuser: Process {{text}}"
providers:
  - id: azureopenai:chat:gpt-4o-mini
    config:
      apiKey: your_key
      apiHost: your_endpoint
tests:
  - vars:
      text: "example"
    assert:
      - type: is-json
```

## Configuration 🔧

```csharp
var config = new PromptfooConfig
{
    BinaryPath = "path/to/promptfoo",
    Provider = "azureopenai:chat:gpt-4",
    Timeout = TimeSpan.FromMinutes(5)
};

var wrapper = new PromptfooWrapper(config);
```

## Contributing 🤝

I appreciate contributions.

## License

MIT License - see [LICENSE](LICENSE) file for details

## Related Projects

- [Promptfoo](https://github.com/promptfoo/promptfoo)
- [NUnit](https://github.com/nunit/nunit)