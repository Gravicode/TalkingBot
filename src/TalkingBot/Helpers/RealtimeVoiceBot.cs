using Azure.AI.OpenAI;
using OpenAI.RealtimeConversation;
using OpenAI;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Azure;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Nodes;
#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace TalkingBot.Helpers
{

    public class MathParamInput
    {
        public string math_question { get; set; }
    }

    public class LogMessage : EventArgs
    {
        public DateTime Created { get; set; } = DateTime.Now;
        public string Message { get; set; }

        public bool NewLine { get; set; } = true;
    }
    public class RealtimeVoiceBot
    {
        MathPlugin mathPlugin { set; get; }
        Thread conversationThread { get; set; }
        public EventHandler<LogMessage> LogMessageReceived;
        CancellationTokenSource cancellationTokenSource;
        public bool IsRunning { get; set; } = false;
        public RealtimeVoiceBot()
        {
            mathPlugin = new();
        }
        public async Task Stop()
        {
            if (!IsRunning)
            {
                WriteLog("Bot is not running.. cannot stop");
                return;
            }
            WriteLog("Trying to stop bot..");
            cancellationTokenSource.Cancel();
        }
        public async Task Start()
        {
            if (IsRunning)
            {
                WriteLog("Bot is already running..");
                return;
            }
            cancellationTokenSource = new();
            var token = cancellationTokenSource.Token;
            conversationThread = new Thread(async () =>
            {
                // First, we create a client according to configured environment variables (see end of file) and then start
                // a new conversation session.
                RealtimeConversationClient client = GetConfiguredClient();
                using RealtimeConversationSession session = await client.StartConversationSessionAsync();

                // We'll add a simple function tool that enables the model to interpret user input to figure out when it
                // might be a good time to stop the interaction.
                ConversationFunctionTool finishConversationTool = new()
                {
                    Name = "user_wants_to_finish_conversation",
                    Description = "Invoked when the user says goodbye, expresses being finished, or otherwise seems to want to stop the interaction.",
                    Parameters = BinaryData.FromString("{}"),
                };

                ConversationFunctionTool GetCurrentUtcTimeTool = new()
                {
                    Name = "get_current_utc_time",
                    Description = "Retrieves the current time in UTC.",
                    Parameters = BinaryData.FromString("{}"),
                };

                ConversationFunctionTool MathTool = new()
                {
                    Name = "calculate_math",
                    Description = "Translate a math problem into a expression that can be executed using .net NCalc library",
                    Parameters = BinaryData.FromString(
                        """
                        {
                          "type": "object",
                          "properties": {
                            "math_question": {
                              "type": "string",
                              "description": "Question with math problem"
                            }
                          },
                          "required": ["math_question"]
                        }
                        """),
                };


                // Now we configure the session using the tool we created along with transcription options that enable input
                // audio transcription with whisper.
                await session.ConfigureSessionAsync(new ConversationSessionOptions()
                {
                    Tools = { finishConversationTool, GetCurrentUtcTimeTool,/* MathTool*/ },
                    InputTranscriptionOptions = new()
                    {
                        Model = "whisper-1",
                    },
                });

                // For convenience, we'll proactively start playback to the speakers now. Nothing will play until it's enqueued.
                SpeakerOutput speakerOutput = new();
                // With the session configured, we start processing commands received from the service.
                await foreach (ConversationUpdate update in session.ReceiveUpdatesAsync())
                {
                    // session.created is the very first command on a session and lets us know that connection was successful.
                    if (update is ConversationSessionStartedUpdate)
                    {
                        WriteLog($" <<< Connected: session started");
                        // This is a good time to start capturing microphone input and sending audio to the service. The
                        // input stream will be chunked and sent asynchronously, so we don't need to await anything in the
                        // processing loop.
                        _ = Task.Run(async () =>
                        {
                            using MicrophoneAudioStream microphoneInput = MicrophoneAudioStream.Start();
                            IsRunning = true;
                            WriteLog($" >>> Listening to microphone input");
                            WriteLog($" >>> (Just tell the app you're done to finish)");
                            WriteLog();
                            await session.SendAudioAsync(microphoneInput);

                        });
                    }

                    if (token.IsCancellationRequested)
                    {
                        WriteLog($" <<< Request to stop!");
                        break;
                    }

                    // input_audio_buffer.speech_started tells us that the beginning of speech was detected in the input audio
                    // we're sending from the microphone.
                    if (update is ConversationInputSpeechStartedUpdate)
                    {
                        WriteLog($" <<< Start of speech detected");
                        // Like any good listener, we can use the cue that the user started speaking as a hint that the app
                        // should stop talking. Note that we could also track the playback position and truncate the response
                        // item so that the model doesn't "remember things it didn't say" -- that's not demonstrated here.
                        speakerOutput.ClearPlayback();
                    }

                    // input_audio_buffer.speech_stopped tells us that the end of speech was detected in the input audio sent
                    // from the microphone. It'll automatically tell the model to start generating a response to reply back.
                    if (update is ConversationInputSpeechFinishedUpdate)
                    {
                        WriteLog($" <<< End of speech detected");
                    }

                    // conversation.item.input_audio_transcription.completed will only arrive if input transcription was
                    // configured for the session. It provides a written representation of what the user said, which can
                    // provide good feedback about what the model will use to respond.
                    if (update is ConversationInputTranscriptionFinishedUpdate transcriptionFinishedUpdate)
                    {
                        WriteLog($" >>> USER: {transcriptionFinishedUpdate.Transcript}");

                    }

                    // response.audio.delta provides incremental output audio generated by the model talking. Here, we
                    // immediately enqueue it for playback on the active speaker output.
                    if (update is ConversationAudioDeltaUpdate audioDeltaUpdate)
                    {

                        speakerOutput.EnqueueForPlayback(audioDeltaUpdate.Delta);
                    }

                    // response.audio_transcript.delta provides the incremental transcription of the emitted audio. The model
                    // typically produces output much faster than it should be played back, so the transcript may move very
                    // quickly relative to what's heard.
                    if (update is ConversationOutputTranscriptionDeltaUpdate outputTranscriptionDeltaUpdate)
                    {
                        WriteLog(outputTranscriptionDeltaUpdate.Delta);
                    }
                    // Item finished updates arrive when all streamed data for an item has arrived and the
                    // accumulated results are available. In the case of function calls, this is the point
                    // where all arguments are expected to be present.
                    if (update is ConversationItemFinishedUpdate itemFinishedUpdate)
                    {

                        WriteLog($"  -- Item streaming finished, response_id={itemFinishedUpdate.ResponseId}");

                        if (itemFinishedUpdate.FunctionCallId is not null)
                        {
                            if (itemFinishedUpdate.FunctionName == finishConversationTool.Name)
                            {
                                WriteLog($" <<< Finish tool invoked -- ending conversation!");
                                break;
                            }
                            else
                            if (itemFinishedUpdate.FunctionName == GetCurrentUtcTimeTool.Name)
                            {
                                var functionOutput = DateTime.UtcNow.ToString("R");
                                WriteLog($"{itemFinishedUpdate.FunctionName}[{itemFinishedUpdate.FunctionCallId}] => {functionOutput}");
                                await session.AddItemAsync(ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, functionOutput));
                                await session.StartResponseTurnAsync();
                            }
                            else if (itemFinishedUpdate.FunctionName == MathTool.Name)
                            {
                                var json = itemFinishedUpdate.FunctionCallArguments;
                                var obj = JsonSerializer.Deserialize<MathParamInput>(json);
                                var functionOutput = await mathPlugin.Calculate(obj.math_question);
                                WriteLog($"{itemFinishedUpdate.FunctionName}[{itemFinishedUpdate.FunctionCallId}] => {functionOutput}");
                                await session.AddItemAsync(ConversationItem.CreateFunctionCallOutput(itemFinishedUpdate.FunctionCallId, functionOutput));
                                await session.StartResponseTurnAsync();


                            }
                            /*
                            WriteLog($"    + Responding to tool invoked by item: {itemFinishedUpdate.FunctionName}");
                            ConversationItem functionOutputItem = ConversationItem.CreateFunctionCallOutput(
                                callId: itemFinishedUpdate.FunctionCallId,
                                output: "70 degrees Fahrenheit and sunny");
                            await session.AddItemAsync(functionOutputItem);
                            */
                        }
                        else if (itemFinishedUpdate.MessageContentParts?.Count > 0)
                        {
                            Console.Write($"    + [{itemFinishedUpdate.MessageRole}]: ");
                            foreach (ConversationContentPart contentPart in itemFinishedUpdate.MessageContentParts)
                            {
                                Console.Write(contentPart.AudioTranscriptValue);
                            }
                            Console.WriteLine();
                        }
                    }
                    /*
                    if (update is ConversationItemStartedUpdate itemStartedUpdate)
                    {

                        if (itemStartedUpdate.FunctionName == GetCurrentUtcTimeTool.Name)
                        {
                            var functionOutput = DateTime.UtcNow.ToString("R");
                            WriteLog($"{itemStartedUpdate.FunctionName}[{itemStartedUpdate.FunctionCallId}] => {functionOutput}");
                            await session.AddItemAsync(ConversationItem.CreateFunctionCallOutput(itemStartedUpdate.FunctionCallId, functionOutput));
                            await session.StartResponseTurnAsync();
                        }
                        else if (itemStartedUpdate.FunctionName == MathTool.Name)
                        {
                            var json = itemStartedUpdate.FunctionCallArguments;
                            var obj = JsonSerializer.Deserialize<JsonObject>(json);
                            var functionOutput = await mathPlugin.Calculate(obj["math_question"].GetValue<string>());
                            WriteLog($"{itemStartedUpdate.FunctionName}[{itemStartedUpdate.FunctionCallId}] => {functionOutput}");
                            await session.AddItemAsync(ConversationItem.CreateFunctionCallOutput(itemStartedUpdate.FunctionCallId, functionOutput));
                            await session.StartResponseTurnAsync();


                        }
                    }*/
                    /*
                    // response.output_item.done tells us that a model-generated item with streaming content is completed.
                    // That's a good signal to provide a visual break and perform final evaluation of tool calls.
                    if (update is ConversationItemFinishedUpdate itemFinishedUpdate)
                    {

                        if (itemFinishedUpdate.FunctionName == finishConversationTool.Name)
                        {
                            WriteLog($" <<< Finish tool invoked -- ending conversation!");
                            break;
                        }
                        else if (itemFinishedUpdate.FunctionName == GetCurrentUtcTimeTool.Name)
                        {
                            WriteLog($"{itemFinishedUpdate.FunctionName} => {itemFinishedUpdate.FunctionCallOutput}");

                        }
                    }*/

                    if (update is ConversationFunctionCallArgumentsDeltaUpdate functionCallArgumentsDeltaUpdate)
                    {
                        //WriteLog($"{functionCallArgumentsDeltaUpdate.Delta} : {functionCallArgumentsDeltaUpdate.CallId}");
                    }

                    if (update is ConversationFunctionCallArgumentsDoneUpdate functionCallArgumentsDoneUpdate)
                    {

                        if (functionCallArgumentsDoneUpdate.Name == GetCurrentUtcTimeTool.Name)
                        {
                            WriteLog($"function call: {functionCallArgumentsDoneUpdate.Name} [{functionCallArgumentsDoneUpdate.CallId}]");

                        }
                    }

                    // error commands, as the name implies, are raised when something goes wrong.
                    if (update is ConversationErrorUpdate errorUpdate)
                    {

                        WriteLog($" <<< ERROR: {errorUpdate.ErrorMessage}");
                        WriteLog(errorUpdate.GetRawContent().ToString());
                        break;
                    }



                }
                IsRunning = false;
                WriteLog("Conversation is finished.");
            });
            conversationThread.Start();

        }

        void WriteLog(string message = "", bool Newline = true)
        {
            var additionalEnd = string.Empty;
            var Msg = message;
            if (Newline)
            {
                additionalEnd = "\n";
                Msg = string.IsNullOrEmpty(message) ? $"---------------{additionalEnd}" : $"{DateTime.Now.ToString("dd-MMM-yy HH:mm:ss")} => {message}{additionalEnd}";
            }

            Debug.WriteLine(Msg);
            LogMessageReceived?.Invoke(this, new() { Message = Msg, NewLine = Newline });
        }

        private RealtimeConversationClient GetConfiguredClient()
        {
            return GetConfiguredClientForOpenAIWithKey(AppConstants.OpenAIKey);
        }

        private RealtimeConversationClient GetConfiguredClientForOpenAIWithKey(string oaiApiKey)
        {
            string oaiEndpoint = "https://api.openai.com/v1";
            WriteLog($" * Connecting to OpenAI endpoint (OPENAI_ENDPOINT): {oaiEndpoint}");
            WriteLog($" * Using API key (OPENAI_API_KEY): {oaiApiKey[..5]}**");

            OpenAIClient aoaiClient = new(new ApiKeyCredential(oaiApiKey));
            return aoaiClient.GetRealtimeConversationClient(AppConstants.ModelId);
        }
    }
}
#pragma warning restore OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
