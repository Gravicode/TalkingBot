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
#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace TalkingBot.Helpers
{
    public class LogMessage:EventArgs
    {
        public DateTime Created { get; set; }=DateTime.Now;
        public string Message { get; set; }
    }
    public class RealtimeVoiceBot
    {
        Thread conversationThread { get; set; }
        public EventHandler<LogMessage> LogMessageReceived;
        CancellationTokenSource cancellationTokenSource;
        public bool IsRunning { get; set; } = false;
        public RealtimeVoiceBot()
        {
            
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
            conversationThread = new Thread(async() =>
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
                    Parameters = BinaryData.FromString("{}")
                };

                // Now we configure the session using the tool we created along with transcription options that enable input
                // audio transcription with whisper.
                await session.ConfigureSessionAsync(new ConversationSessionOptions()
                {
                    Tools = { finishConversationTool },
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
                            WriteLog("");
                            await session.SendAudioAsync(microphoneInput);
                        });
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
                        Console.Write(outputTranscriptionDeltaUpdate.Delta);
                    }

                    // response.output_item.done tells us that a model-generated item with streaming content is completed.
                    // That's a good signal to provide a visual break and perform final evaluation of tool calls.
                    if (update is ConversationItemFinishedUpdate itemFinishedUpdate)
                    {

                        if (itemFinishedUpdate.FunctionName == finishConversationTool.Name)
                        {
                            WriteLog($" <<< Finish tool invoked -- ending conversation!");
                            break;
                        }
                    }

                    // error commands, as the name implies, are raised when something goes wrong.
                    if (update is ConversationErrorUpdate errorUpdate)
                    {

                        WriteLog($" <<< ERROR: {errorUpdate.ErrorMessage}");
                        WriteLog(errorUpdate.GetRawContent().ToString());
                        break;
                    }

                    if (token.IsCancellationRequested)
                    {
                        WriteLog($" <<< Request to stop!");
                        break;
                    }
                
                }
                IsRunning = false;
                WriteLog("Conversation is finished.");
            });
            conversationThread.Start();
            
        }

        void WriteLog(string message)
        {
            var Msg = string.IsNullOrEmpty(message) ? "---------------\n" : $"{DateTime.Now.ToString("dd-MMM-yy HH:mm:ss")} => {message}\n";
            Debug.WriteLine(Msg);
            LogMessageReceived?.Invoke(this, new() { Message = Msg });
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
            return aoaiClient.GetRealtimeConversationClient("gpt-4o-realtime-preview-2024-10-01");
        }
    }
}
#pragma warning restore OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
