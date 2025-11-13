# Bricky Chat - Interactive GUI Assistant

A modern, Clippy-inspired chat interface for BuilderTrend documentation assistance!

## 🧱 About Bricky

Bricky is your friendly BuilderTrend documentation assistant - like Clippy, but for BuilderTrend! The GUI features:

- **Friendly Mascot**: Bricky appears at the top with a dynamic speech bubble
- **Modern Chat Interface**: Clean, ChatGPT-style conversation view
- **Semantic Search**: Powered by AI-driven semantic search of BuilderTrend docs
- **Brief Responses**: Clippy-style helpful answers (200-500 characters)
- **Warm Personality**: Encouraging, supportive, and approachable

## 🚀 Quick Start

### Option 1: Using the batch file
```batch
bricky.bat
```

### Option 2: Using PowerShell
```powershell
.\bricky.ps1
```

### Option 3: Direct dotnet command
```bash
cd BrickyChat
dotnet run
```

## 💬 How to Use

1. **Launch the application** using one of the methods above
2. **Type your question** in the input box at the bottom
3. **Press Enter or click Send** to ask Bricky
4. **Watch the speech bubble** update with Bricky's response
5. **Chat history** appears in the middle section

## ✨ Features

### Personality & Tone
- **Friendly & Encouraging**: Like talking to a helpful friend
- **Brief & Conversational**: Gets to the point quickly (max 400-500 chars)
- **Uses Emojis**: 👋 🎉 💡 ✨ 🔍 📚 for a warm feel
- **Helpful Offers**: Always ends with follow-up questions or offers to help more

### Technical Features
- **Semantic Search**: Automatically searches BuilderTrend docs for relevant questions
- **Smart Detection**: Recognizes documentation vs. general conversation queries
- **Full Content Access**: Uses the complete indexed article content for accurate answers
- **Typing Indicator**: Shows when Bricky is thinking
- **Smooth Animations**: Speech bubble fades in with responses
- **🔊 Text-to-Speech (NEW)**: Play button on each response for accessibility
  - Click the 🔊 button next to any Bricky response to hear it read aloud
  - Great for accessibility and hands-free operation
  - Click again to stop playback

## 🎨 UI Layout

```
┌─────────────────────────────────────────┐
│  [Speech Bubble]       [Bricky Image]   │  ← Top Section
│  "Hi! I'm Bricky..."   🧱               │
├─────────────────────────────────────────┤
│  Chat History                           │  ← Scrollable
│  User: How do I...                      │     Middle
│  [Bricky: Here's what I found!] [🔊]    │     Section
│                                         │     (Play button)
├─────────────────────────────────────────┤
│  [Type your message...] [Send]          │  ← Input Area
└─────────────────────────────────────────┘
```

## 🧠 How It Works

1. **User asks a question**
2. **Bricky detects if it's about BuilderTrend**
   - Yes → Searches documentation with semantic search
   - No → General conversation
3. **LLM synthesizes answer** from full article content
4. **Response is brief** (200-500 chars) and friendly
5. **Speech bubble updates** at top + added to chat history

## 🔧 Configuration

Bricky uses the same configuration as the console chat mode:

- **LLM**: Ollama (llama3.2 on localhost:11434)
- **System Prompt**: Defined in `MainWindow.xaml.cs` as `BrickySystemPrompt`
- **Response Limits**: 
  - Brief questions: 200 chars max
  - Complex questions: 400 chars max
  - Hard limit: 500 chars (safety net)

## 📝 System Prompt

Bricky's personality is defined by a Clippy-inspired system prompt:

```
You are Bricky, a helpful and friendly BuilderTrend documentation assistant.

PERSONALITY:
- Warm, encouraging, and supportive like Clippy
- Use occasional emojis (👋 🎉 💡 ✨ 🔍 📚) to be friendly
- Keep responses SHORT and conversational (2-4 sentences max)
- Be helpful without being overwhelming

RESPONSE RULES:
- Maximum 200 characters for brief questions
- Maximum 400 characters for complex questions
- If the answer is longer, summarize key points
- Always end with a helpful follow-up question or offer

TONE:
- Casual and approachable
- Encouraging
- Brief: Get to the point quickly
```

## 🏗️ Architecture

**Project**: `BrickyChat.csproj`
- WPF application (.NET 8.0-windows)
- References shared services from `../dotnet-mcp-server`:
  - `LLMService` - Ollama integration
  - `BTDocumentationService` - BuilderTrend doc crawler
  - `SemanticSearchService` - AI-powered semantic search
  - `EmbeddingCache` - Persistent embedding storage

**Key Files**:
- `MainWindow.xaml` - UI layout (speech bubble, chat area, input)
- `MainWindow.xaml.cs` - Chat logic and LLM integration
- `App.xaml` - WPF application styles and resources
- `BrickyChatApp.cs` - Application entry point
- `assets/BrickyV1.png` - Bricky mascot image
- `appsettings.example.json` - Configuration template

## 🎯 Example Interactions

**Documentation Question**:
```
User: How do I add a new user?
Bricky: 🎉 Great question! Go to Settings > Users > Add New User. 
        Enter their email and role, then send the invite! Need help 
        with permissions?
```

**General Chat**:
```
User: Hello!
Bricky: 👋 Hi there! I'm Bricky, your BuilderTrend buddy! Ask me 
        anything about BuilderTrend features, and I'll help you out!
```

**No Results**:
```
User: How do I fly to the moon?
Bricky: 🤔 Hmm, I couldn't find anything about that in the BuilderTrend 
        docs. Could you rephrase or ask about something else?
```

## 🚧 Development Notes

- Built on top of existing MCP server infrastructure
- Shares semantic search cache with console mode
- First search takes ~50-90 seconds (indexing), cached searches ~1-2 seconds
- Response brevity enforced at multiple levels (prompt + hard limit)
- Uses same LLM configuration as console chat mode

## 📦 Dependencies

- .NET 8.0 Windows SDK
- HtmlAgilityPack 1.11.61
- System.Speech 8.0.0 (for text-to-speech accessibility)
- Ollama running locally (llama3.2 model)
- Existing service files from MCP server project

## ♿ Accessibility Features

Bricky Chat includes built-in accessibility support:

- **Natural Text-to-Speech** 🎙️: Every Bricky response includes a 🔊 play button
- **Free Neural Voices**: Uses Microsoft Edge TTS (no API key required!)
- **Keyboard Navigation**: Full keyboard support for all interactions
- **Screen Reader Friendly**: Properly labeled controls and tooltips
- **High Contrast**: Clear visual separation between user and assistant messages
- **Hover Effects**: Visual feedback for interactive elements

### Text-to-Speech Features

**🆓 FREE Natural Voice (Default)**

BrickyChat uses **Microsoft Edge TTS** - completely free with human-like neural voices!

- **Voice**: Jenny Neural (en-US) - friendly, natural female voice
- **Quality**: Neural TTS - sounds like a real person, not robotic
- **Cost**: FREE - no API key or account required
- **Works Offline**: No (requires internet connection)

**Automatic Fallback**

If Edge TTS fails (no internet), automatically falls back to Windows Speech Synthesis.

### Using Text-to-Speech

1. Look for the 🔊 button next to any Bricky response
2. Click to hear the message read aloud with a natural voice
3. Click again to stop playback if needed

**No setup required** - just works out of the box! 🎉

---

**Enjoy chatting with Bricky!** 🧱✨
