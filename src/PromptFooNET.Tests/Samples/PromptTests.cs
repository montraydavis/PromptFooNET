using NUnit.Framework;
using PromptFooNET.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PromptFooNET.Tests.Samples
{
    public class PromptTests
    {
        [Test]
        [AITest("Prompts/DialogueTemplate.prompty")]
        public async Task ClassificationPrompt_ShouldPassEvaluation()
        {
            // Test execution is handled by AITestAttribute
            Assert.Pass("Prompt evaluation completed successfully");
        }

        [Test]
        [AITest("Prompts/sentiment.prompty")]
        [TestCase("en")]
        [TestCase("es")]
        public async Task SentimentPrompt_MultiLanguage_ShouldPassEvaluation(string language)
        {
            // Demonstrates combining AITest with TestCase
            Assert.Pass($"Sentiment analysis completed for language: {language}");
        }

        [Test]
        [AITest("Prompts/generation.prompty")]
        [NonParallelizable]
        public async Task GenerationPrompt_LongRunning_ShouldPassEvaluation()
        {
            // Example of a long-running test marked as non-parallelizable
            await Task.Delay(1000); // Simulate long operation
            Assert.Pass("Generation completed successfully");
        }
    }
}
