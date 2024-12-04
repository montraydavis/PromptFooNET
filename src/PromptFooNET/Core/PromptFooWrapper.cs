using PromptFooNET.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PromptFooNET.Core
{
    public class TestDiscoveryService
    {
        public IEnumerable<string> DiscoverPromptFiles(string solutionPath)
        {
            return Directory.GetFiles(solutionPath, "*.prompty", SearchOption.AllDirectories);
        }

        public IEnumerable<TestCaseInfo> DiscoverTests(Assembly testAssembly)
        {
            return testAssembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes<AITestAttribute>().Any())
                .Select(m => new TestCaseInfo
                {
                    Method = m,
                    PromptFile = m.GetCustomAttribute<AITestAttribute>()?.PromptFile
                });
        }
    }

    public class TestCaseInfo
    {
        public MethodInfo Method { get; set; }
        public string PromptFile { get; set; }
    }

    public class PromptfooWrapper
    {
        private readonly string _promptfooBinaryPath;
        private readonly YamlConverterService _converter;

        public PromptfooWrapper(string? promptfooBinaryPath = null)
        {
            _promptfooBinaryPath = promptfooBinaryPath ?? "npx";
            _converter = new YamlConverterService();
        }

        public async Task<PromptEvaluationResult> EvaluatePrompt(string promptFile)
        {
            try
            {
                var tempYamlPath = await _converter.ConvertPromptyFile(promptFile);
                var startInfo = new ProcessStartInfo
                {
                    FileName = _promptfooBinaryPath,
                    Arguments = $"promptfoo eval -c {tempYamlPath} --json --no-progress-bar",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return new PromptEvaluationResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to start promptfoo process"
                    };
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                File.Delete(tempYamlPath); // Cleanup temp file

                if (process.ExitCode != 0)
                {
                    return new PromptEvaluationResult
                    {
                        Success = false,
                        ErrorMessage = error
                    };
                }

                var evaluationResult = JsonSerializer.Deserialize<PromptfooEvaluation>(output);
                return new PromptEvaluationResult
                {
                    Success = evaluationResult?.Failed == 0,
                    ErrorMessage = evaluationResult?.Failed > 0 ? "Some prompt tests failed" : null,
                    RawOutput = output,
                    Evaluation = evaluationResult
                };
            }
            catch (Exception ex)
            {
                return new PromptEvaluationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public class PromptEvaluationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawOutput { get; set; }
        public PromptfooEvaluation? Evaluation { get; set; }
    }

    public class PromptfooEvaluation
    {
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public List<TestCase>? Tests { get; set; }
    }

    public class TestCase
    {
        public string? Prompt { get; set; }
        public string? Expected { get; set; }
        public string? Actual { get; set; }
        public bool Passed { get; set; }
    }
}
