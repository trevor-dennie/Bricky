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
    private bool _isProcessing = false;
    
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

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize services
        _llmService = new LLMService();
        _btDocService = new BTDocumentationService(_llmService);
        
        InputBox.Focus();
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

        // Add user message to chat
        AddUserMessage(message);
        
        // Clear input
        InputBox.Text = string.Empty;

        // Show typing indicator
        ShowTypingIndicator();

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
            HideTypingIndicator();
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
        var messageContainer = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(15, 15, 15, 5),
            Padding = new Thickness(15, 10, 15, 10),
            Margin = new Thickness(10, 5, 50, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            MaxWidth = 400
        };

        var textBlock = new TextBlock
        {
            Text = "🧱 " + message,
            Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        };

        messageContainer.Child = textBlock;
        ChatHistory.Children.Add(messageContainer);
        ScrollToBottom();
    }

    private TextBlock? _typingIndicator;

    private void ShowTypingIndicator()
    {
        _typingIndicator = new TextBlock
        {
            Text = "Bricky is thinking...",
            Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
            FontStyle = FontStyles.Italic,
            Margin = new Thickness(10, 5, 50, 5),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        ChatHistory.Children.Add(_typingIndicator);
        ScrollToBottom();

        // Animate dots
        var animation = new DoubleAnimation
        {
            From = 0.3,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(600),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        _typingIndicator.BeginAnimation(OpacityProperty, animation);
    }

    private void HideTypingIndicator()
    {
        if (_typingIndicator != null)
        {
            ChatHistory.Children.Remove(_typingIndicator);
            _typingIndicator = null;
        }
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToBottom();
    }
}
