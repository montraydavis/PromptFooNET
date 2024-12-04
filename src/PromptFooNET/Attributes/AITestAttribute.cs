using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using PromptFooNET.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PromptFooNET.Attributes
{

    public class YamlConverterService
    {
        public async Task<string> ConvertPromptyFile(string filePath)
        {
            var input = await File.ReadAllTextAsync(filePath);
            var pattern = new Regex(@"---(.*?)---", RegexOptions.Singleline);
            var matches = pattern.Matches(input);

            if (!matches.Any())
                throw new InvalidDataException("No YAML content found between --- markers");

            var yamlContent = matches[0].Groups[1].Value;
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var docs = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);

            var systemPattern = new Regex(@"system:(.*)", RegexOptions.Singleline);
            var systemMatch = systemPattern.Match(input);
            var blocks = ExtractBlocks(systemMatch.Groups[1].Value);

            var config = new
            {
                description = docs["description"],
                prompts = new[] { string.Join("\n\n", blocks.Select(b => b.Text)) },
                providers = new[]
                {
                    new
                    {
                        id = "azureopenai:chat:gpt-4o-mini",
                        config = new
                        {
                            apiKey = "3e1720f2f83040e4afab1f0948ecad54",
                            apiHost = "https://hpkbdemo6081598253.openai.azure.com"
                        }
                    }
                },
                tests = new[]
                {
                    new
                    {
                        vars = (Dictionary<object, object>)docs["sample"],
                        assert = new object[]
                        {
                            new { type = "is-json" },
                            new
                            {
                                type = "javascript",
                                value = "JSON.parse(output).length >= 1"
                            }
                        }
                    }
                }
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = "# yaml-language-server: $schema=https://promptfoo.dev/config-schema.json\n\n" +
                      serializer.Serialize(config);

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(filePath)}.yaml");
            await File.WriteAllTextAsync(tempPath, yaml);
            return tempPath;
        }

        private List<Block> ExtractBlocks(string input)
        {
            var blocks = new List<Block>();
            var splits = Regex.Split(input, @"(system:|user:|assistant:)")
                .Where(s => !string.IsNullOrWhiteSpace(s));

            string currentType = null;
            foreach (var part in splits)
            {
                switch (part.ToLower())
                {
                    case "system:": currentType = "System"; break;
                    case "user:": currentType = "User"; break;
                    case "assistant:": currentType = "Assistant"; break;
                    default:
                        if (currentType != null)
                            blocks.Add(new Block { Type = currentType, Text = part.Trim() });
                        break;
                }
            }
            return blocks;
        }

        private class Block
        {
            public string Type { get; set; }
            public string Text { get; set; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AITestAttribute : TestAttribute, IWrapTestMethod
    {
        public string PromptFile { get; }
        private readonly PromptfooWrapper _wrapper;

        public AITestAttribute(string promptFile)
        {
            PromptFile = promptFile ?? throw new ArgumentNullException(nameof(promptFile));
            _wrapper = new PromptfooWrapper();
        }

        public TestCommand Wrap(TestCommand command)
        {
            return new AITestCommand(command, PromptFile, _wrapper);
        }
    }

    internal class AITestCommand : DelegatingTestCommand
    {
        private readonly string _promptFile;
        private readonly PromptfooWrapper _wrapper;

        public AITestCommand(TestCommand innerCommand, string promptFile, PromptfooWrapper wrapper)
            : base(innerCommand)
        {
            _promptFile = promptFile;
            _wrapper = wrapper;
        }

        public override TestResult Execute(TestExecutionContext context)
        {
            try
            {
                var result = _wrapper.EvaluatePrompt(_promptFile).GetAwaiter().GetResult();

                if (!result.Success)
                {
                    context.CurrentResult.SetResult(ResultState.Failure,
                        $"Prompt evaluation failed: {result.ErrorMessage}");
                    return context.CurrentResult;
                }

                return innerCommand.Execute(context);
            }
            catch (Exception ex)
            {
                context.CurrentResult.SetResult(ResultState.Error, ex.Message);
                return context.CurrentResult;
            }
        }
    }
}