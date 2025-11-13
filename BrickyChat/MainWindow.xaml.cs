using System.Speech.Synthesis;
using System.Globalization;
using NAudio.Wave;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using McpServer;

namespace BrickyChat;

public partial class MainWindow : Window
{
    private readonly LLMService _llmService;
    private readonly BTDocumentationService _btDocService;
    private readonly SpeechSynthesizer _speechSynthesizer;
    private readonly HttpClient _httpClient = new HttpClient();
    private bool _isProcessing = false;
    private bool _isSpeaking = false;
    private IWavePlayer? _wavePlayer;
    private AudioFileReader? _audioFileReader;
    
    // Clippy-style system prompt for Bricky
    private const string BrickySystemPrompt = @"You are Bricky, a helpful and friendly BuilderTrend documentation assistant. 

PERSONALITY:
- Warm, encouraging, and supportive like Clippy
- Use occasional emojis (👋 🎉 💡 ✨ 🔍 📚) to be friendly
- Keep responses SHORT and conversational (2-4 sentences max)
- Be helpful without being overwhelming

RESPONSE RULES:
- Maximum 200 characters for brief questions
- Maximum 400 characters for complex questions
- If the answer is longer, summarize key points and offer to explain more
- Always end with a helpful follow-up question or offer

TONE:
- Casual and approachable: ""Here's what I found!"" not ""According to the documentation...""
- Encouraging: ""Great question!"" ""Let me help with that!""
- Brief: Get to the point quickly

Remember: You're a friendly mascot assistant, not a formal documentation bot!";

    private bool _isHistoryExpanded = false;

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize services
        _llmService = new LLMService();
        _btDocService = new BTDocumentationService(_llmService);
        
        // Initialize text-to-speech with natural voice settings
        _speechSynthesizer = new SpeechSynthesizer();
        _speechSynthesizer.SetOutputToDefaultAudioDevice();
        _speechSynthesizer.SpeakCompleted += (s, e) => _isSpeaking = false;
        
        // Try to select a more natural voice (prefer Microsoft voices or neural voices)
        var availableVoices = _speechSynthesizer.GetInstalledVoices();
        VoiceInfo? selectedVoice = null;
        
        // Prioritize these voice names for better quality
        string[] preferredVoices = { 
            "Microsoft David", "Microsoft Zira", "Microsoft Mark",  // Windows 10/11 voices
            "Microsoft Eva", "Microsoft Aria",                       // Newer voices
            "David", "Zira", "Mark"                                 // Fallback names
        };
        
        foreach (var voiceName in preferredVoices)
        {
            selectedVoice = availableVoices
                .Select(v => v.VoiceInfo)
                .FirstOrDefault(v => v.Name.Contains(voiceName, StringComparison.OrdinalIgnoreCase));
            
            if (selectedVoice != null)
                break;
        }
        
        // If we found a preferred voice, use it
        if (selectedVoice != null)
        {
            _speechSynthesizer.SelectVoice(selectedVoice.Name);
        }
        
        // Configure speech rate and volume for more natural sound
        _speechSynthesizer.Rate = 1;      // Normal speed (-10 to 10, 0 is default)
        _speechSynthesizer.Volume = 85;    // Slightly softer (0 to 100)
        
        InputBox.Focus();
    }

    private void ToggleHistory_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isHistoryExpanded = !_isHistoryExpanded;
        
        if (_isHistoryExpanded)
        {
            // Expand history
            HistoryPanel.Width = 380;
            HistoryContent.Visibility = Visibility.Visible;
            HistoryToggleIcon.Text = "▶";
        }
        else
        {
            // Collapse history
            HistoryPanel.Width = 40;
            HistoryContent.Visibility = Visibility.Collapsed;
            HistoryToggleIcon.Text = "◀";
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !_isProcessing)
        {
            e.Handled = true;
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        var message = InputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(message) || _isProcessing)
            return;

        _isProcessing = true;
        SendButton.IsEnabled = false;
        InputBox.IsEnabled = false;

        // Show current prompt
        ShowCurrentPrompt(message);
        
        // Add user message to chat history
        AddUserMessage(message);
        
        // Clear input
        InputBox.Text = string.Empty;

        // Show thinking indicator
        ShowThinkingIndicator();

        try
        {
            // Check if this is a BuilderTrend documentation question
            string response;
            
            if (IsDocumentationQuery(message))
            {
                // Use semantic search for documentation queries
                var searchResults = await _btDocService.SearchDocumentationResultsAsync(message, useSemanticSearch: true);
                
                if (searchResults.Count > 0)
                {
                    // Build context from search results
                    var context = BuildSearchContext(searchResults);
                    var prompt = $@"User question: {message}

Relevant documentation:
{context}

Provide a brief, helpful answer (max 400 chars). Be friendly and conversational!";
                    
                    response = await _llmService.ChatAsync(prompt, BrickySystemPrompt);
                }
                else
                {
                    response = "🤔 Hmm, I couldn't find anything about that in the BuilderTrend docs. Could you rephrase your question or ask about something else?";
                }
            }
            else
            {
                // General conversation
                response = await _llmService.ChatAsync(message, BrickySystemPrompt);
            }

            // Enforce character limit (safety net)
            if (response.Length > 500)
            {
                response = response.Substring(0, 497) + "...";
            }

            // Update speech bubble
            UpdateSpeechBubble(response);
            
            // Add response to chat history
            AddBrickyMessage(response);
        }
        catch (Exception ex)
        {
            var errorMsg = $"😅 Oops! I ran into a problem: {ex.Message}";
            UpdateSpeechBubble(errorMsg);
            AddBrickyMessage(errorMsg);
        }
        finally
        {
            HideThinkingIndicator();
            _isProcessing = false;
            SendButton.IsEnabled = true;
            InputBox.IsEnabled = true;
            InputBox.Focus();
        }
    }

    private bool IsDocumentationQuery(string message)
    {
        var lowerMessage = message.ToLower();
        var docKeywords = new[] { "how", "what", "where", "when", "buildertrend", "feature", "setup", "configure", "use", "create", "add", "help", "explain" };
        return docKeywords.Any(keyword => lowerMessage.Contains(keyword));
    }

    private string BuildSearchContext(List<SearchResult> results)
    {
        var context = "";
        int count = 1;
        foreach (var result in results.Take(3)) // Top 3 results
        {
            var content = result.FullContent ?? result.Snippet;
            if (content.Length > 500)
                content = content.Substring(0, 500);
            
            context += $"{count}. {result.Title}: {content}\n\n";
            count++;
        }
        return context;
    }

    private void UpdateSpeechBubble(string text)
    {
        Dispatcher.Invoke(() =>
        {
            SpeechText.Text = text;
            
            // Animate the speech bubble
            var animation = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            SpeechText.BeginAnimation(OpacityProperty, animation);
        });
    }

    private void AddUserMessage(string message)
    {
        var messageContainer = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            CornerRadius = new CornerRadius(15, 15, 5, 15),
            Padding = new Thickness(15, 10, 15, 10),
            Margin = new Thickness(50, 5, 10, 5),
            HorizontalAlignment = HorizontalAlignment.Right,
            MaxWidth = 400
        };

        var textBlock = new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };

        messageContainer.Child = textBlock;
        ChatHistory.Children.Add(messageContainer);
        ScrollToBottom();
    }

    private void AddBrickyMessage(string message)
    {
        // Create a stack panel to hold the message and play button
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(5, 5, 5, 5)
        };

        var messageContainer = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(15, 15, 15, 5),
            Padding = new Thickness(12, 8, 12, 8),
            MaxWidth = 270
        };

        var textBlock = new TextBlock
        {
            Text = "🧱 " + message,
            Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            LineHeight = 20
        };

        messageContainer.Child = textBlock;

        // Create play button for accessibility (text-to-speech)
        var playButton = new Button
        {
            Content = "🔊",
            FontSize = 14,
            Width = 30,
            Height = 30,
            MinWidth = 30,
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(1),
            ToolTip = "Play audio (accessibility)",
            Cursor = Cursors.Hand,
            Style = null // Use default button style
        };

        // Add hover effect
        playButton.MouseEnter += (s, e) =>
        {
            playButton.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
        };
        playButton.MouseLeave += (s, e) =>
        {
            playButton.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
        };

        // Store the message text for the click handler
        var messageToSpeak = message;
        playButton.Click += (s, e) => PlayTextToSpeech(messageToSpeak);

        stackPanel.Children.Add(messageContainer);
        stackPanel.Children.Add(playButton);
        
        ChatHistory.Children.Add(stackPanel);
        ScrollToBottom();
    }

    private async void PlayTextToSpeech(string text)
    {
        if (_isSpeaking)
        {
            // Stop current speech
            _speechSynthesizer.SpeakAsyncCancelAll();
            _wavePlayer?.Stop();
            _isSpeaking = false;
            return;
        }

        try
        {
            // Remove emojis and other non-letter characters for cleaner speech
            var cleanText = System.Text.RegularExpressions.Regex.Replace(text, @"[^\w\s\.,!?'-]", "");
            
            // Also clean up multiple spaces
            cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, @"\s+", " ").Trim();
            
            if (string.IsNullOrWhiteSpace(cleanText))
                return;

            _isSpeaking = true;

            // Try to use Microsoft Edge TTS (free, natural-sounding)
            try
            {
                await PlayEdgeTTS(cleanText);
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Edge TTS failed: {ex.Message}, falling back to Windows TTS");
                // Fall back to Windows TTS
            }

            // Fallback to Windows Speech Synthesis
            var prompt = new PromptBuilder();
            prompt.StartStyle(new PromptStyle()
            {
                Emphasis = PromptEmphasis.Moderate,
                Rate = PromptRate.Medium,
                Volume = PromptVolume.Default
            });
            prompt.AppendText(cleanText);
            prompt.EndStyle();
            
            _speechSynthesizer.SpeakAsync(prompt);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Text-to-speech error: {ex.Message}", "Accessibility Feature", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            _isSpeaking = false;
        }
    }

    private async Task PlayEdgeTTS(string text)
    {
        // Use Google TTS (free, natural voices, actually works!)
        var tempFile = Path.Combine(Path.GetTempPath(), $"bricky_tts_{Guid.NewGuid()}.mp3");
        var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gtts_helper.py");

        // Check if Python and edge-tts are available
        var pythonPath = FindPython();
        if (pythonPath == null)
        {
            throw new Exception("Python not found. Install Python 3.7+ and gTTS: pip install gTTS");
        }

        // Call Google TTS helper script
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{scriptPath}\" \"{text}\" \"{tempFile}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
            throw new Exception("Failed to start Python process");
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Google TTS failed: {error}");
        }

        // Play the generated audio
        await Dispatcher.InvokeAsync(() =>
        {
            _wavePlayer?.Stop();
            _audioFileReader?.Dispose();
            
            _audioFileReader = new AudioFileReader(tempFile);
            _wavePlayer = new WaveOutEvent();
            _wavePlayer.Init(_audioFileReader);
            _wavePlayer.PlaybackStopped += (s, e) =>
            {
                _isSpeaking = false;
                _audioFileReader?.Dispose();
                _wavePlayer?.Dispose();
                
                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
            };
            _wavePlayer.Play();
        });
    }

    private string? FindPython()
    {
        // Try common Python locations
        var pythonPaths = new[]
        {
            "python",
            "python3",
            "py",
            @"C:\Python312\python.exe",
            @"C:\Python311\python.exe",
            @"C:\Python310\python.exe",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python312\python.exe",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python311\python.exe"
        };

        foreach (var path in pythonPaths)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit(1000);
                    if (process.ExitCode == 0)
                    {
                        return path;
                    }
                }
            }
            catch
            {
                continue;
            }
        }
        return null;
    }

    private void ShowCurrentPrompt(string prompt)
    {
        Dispatcher.Invoke(() =>
        {
            CurrentPromptText.Text = prompt;
            CurrentPromptContainer.Visibility = Visibility.Visible;
        });
    }

    private void ShowThinkingIndicator()
    {
        Dispatcher.Invoke(() =>
        {
            ThinkingIndicator.Visibility = Visibility.Visible;
            
            // Animate the thinking indicator
            var animation = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(600),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            ThinkingText.BeginAnimation(OpacityProperty, animation);
        });
    }

    private void HideThinkingIndicator()
    {
        Dispatcher.Invoke(() =>
        {
            ThinkingIndicator.Visibility = Visibility.Collapsed;
            ThinkingText.BeginAnimation(OpacityProperty, null);
        });
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToBottom();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        // Clean up speech synthesizer
        if (_isSpeaking)
        {
            _speechSynthesizer.SpeakAsyncCancelAll();
        }
        _speechSynthesizer.Dispose();
    }
}
