using NCalc;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace TalkingBot.Helpers;

public class MathPlugin
    {
        public string SkillName { get; set; } = "CalculatorSkill";
        public string FunctionName { set; get; } = "Calculator";
        int MaxTokens { set; get; }
        double Temperature { set; get; }
        double TopP { set; get; }

        public bool IsConfigured { set; get; } = false;

        Dictionary<string, KernelFunction> ListFunctions = new Dictionary<string, KernelFunction>();

        Kernel kernel { set; get; }

    public MathPlugin()
    {
        var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        kernel = kernelBuilder
                      .AddOpenAIChatCompletion(modelId: "gpt-4o-mini", apiKey: AppConstants.OpenAIKey , serviceId: "gpt4o", endpoint: new Uri(AppConstants.OpenAIEndpoint))//
                      .Build();
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        SetupSkill();
    }

    public void SetupSkill(int MaxTokens = 2000, double Temperature = 0.0, double TopP = 1.0)
    {
        try
        {

            //this.kernel = kernel;
            this.MaxTokens = MaxTokens;
            this.Temperature = Temperature;
            this.TopP = TopP;

            string skPrompt = @"Translate a math problem into a expression that can be executed using .net NCalc library. Use the output of running this code to answer the question.
Available functions: Abs, Acos, Asin, Atan, Ceiling, Cos, Exp, Floor, IEEERemainder, Log, Log10, Max, Min, Pow, Round, Sign, Sin, Sqrt, Tan, and Truncate. in and if are also supported.

Question: $((Question with math problem.))
expression:``` $((single line mathematical expression that solves the problem))```

[Examples]
Question: What is 37593 * 67?
expression:```37593 * 67```

Question: what is 3 to the 2nd power?
expression:```Pow(3, 2)```

Question: what is sine of 0 radians?
expression:```Sin(0)```

Question: what is sine of 45 degrees?
expression:```Sin(45 * Pi /180 )```

Question: how many radians is 45 degrees?
expression:``` 45 * Pi / 180 ```

Question: what is the square root of 81?
expression:```Sqrt(81)```

Question: what is the angle whose sine is the number 1?
expression:```Asin(1)```

[End of Examples]

Question: {{ $input }}
";

            PromptExecutionSettings setting = null;// new OpenAIPromptExecutionSettings() { MaxTokens = MaxTokens, Temperature = Temperature, TopP = TopP };

            setting = new OpenAIPromptExecutionSettings()
            {
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                TopP = TopP
            };

            var CalculatorFunction = this.kernel.CreateFunctionFromPrompt(skPrompt, executionSettings: setting, functionName: FunctionName);

            ListFunctions.Add(FunctionName, CalculatorFunction);

            IsConfigured = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }

        [KernelFunction, Description("Translate a math problem into a expression that can be executed using .net NCalc library")]
        public async Task<string> Calculate([Description("Question with math problem")] string MathQuestion)
        {
            string Result = string.Empty;
            try
            {
               
             
                var answer = await this.kernel.InvokeAsync(ListFunctions[FunctionName], new() { ["input"] = MathQuestion });

                string pattern = @"```\s*(.*?)\s*```";

                Match match = Regex.Match(answer.GetValue<string>(), pattern, RegexOptions.Singleline);
                if (match.Success)
                {
                    Result = EvaluateMathExpression(match);
                }
                else
                {
                    Result = $"Input value [{MathQuestion}] could not be understood, received following {answer.GetValue<string>()}";
                }
                Console.WriteLine(Result);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Result = "error in calculator for input " + MathQuestion + " " + ex;
                return Result;
            }
            finally
            {
           
            }
            return Result;
        }

        static string EvaluateMathExpression(Match match)
        {
            var textExpressions = match.Groups[1].Value;
            var expr = new Expression(textExpressions, EvaluateOptions.IgnoreCase);
            expr.EvaluateParameter += delegate (string name, ParameterArgs args)
            {
                args.Result = name.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
                {
                    "pi" => Math.PI,
                    "e" => Math.E,
                    _ => args.Result
                };
            };

            try
            {
                if (expr.HasErrors())
                {
                    return "Error:" + expr.Error + " could not evaluate " + textExpressions;
                }

                var result = expr.Evaluate();
                return result.ToString();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("could not evaluate " + textExpressions, e);
            }
        }
    }
