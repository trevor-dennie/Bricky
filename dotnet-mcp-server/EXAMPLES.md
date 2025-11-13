# Example: Using the LLM Tool

This document shows examples of using the `ask_llm` tool in your MCP server.

## Basic Usage

### Simple Question
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "ask_llm",
    "arguments": {
      "prompt": "What is the capital of France?"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "The capital of France is Paris."
      }
    ]
  }
}
```

## Using System Prompts

System prompts help set the context and behavior for the LLM:

### Code Helper Example
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "ask_llm",
    "arguments": {
      "prompt": "How do I read a file in C#?",
      "systemPrompt": "You are an expert C# developer. Provide concise, working code examples."
    }
  }
}
```

### Technical Writer Example
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "ask_llm",
    "arguments": {
      "prompt": "Explain async/await in simple terms",
      "systemPrompt": "You are a technical writer who explains complex concepts in simple, beginner-friendly language."
    }
  }
}
```

## Practical Examples

### 1. Code Generation
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "ask_llm",
    "arguments": {
      "prompt": "Write a C# function that calculates the factorial of a number"
    }
  }
}
```

### 2. Code Review
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "ask_llm",
    "arguments": {
      "prompt": "Review this code and suggest improvements:\npublic void ProcessData(string data) { var result = data.Split(','); foreach(var item in result) { Console.WriteLine(item); } }",
      "systemPrompt": "You are a senior software engineer conducting a code review. Focus on best practices, readability, and potential issues."
    }
  }
}
```

### 3. Documentation Help
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "tools/call",
  "params": {
    "name": "ask_llm",
    "arguments": {
      "prompt": "Explain what the Model Context Protocol (MCP) is and how it works",
      "systemPrompt": "You are a technical documentation expert. Provide clear, structured explanations."
    }
  }
}
```

### 4. Problem Solving
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "ask_llm",
    "arguments": {
      "prompt": "I'm getting a NullReferenceException in my C# code. What are the common causes and how can I debug it?",
      "systemPrompt": "You are a debugging expert. Provide practical troubleshooting steps."
    }
  }
}
```

## Combining with Other Tools

You can combine the LLM tool with other tools in your MCP server:

### Example Workflow
1. Search documentation: `bt_documentation`
2. Ask LLM to summarize: `ask_llm`

```json
// First, search documentation
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "tools/call",
  "params": {
    "name": "bt_documentation",
    "arguments": {
      "query": "schedule creation"
    }
  }
}

// Then, ask LLM to summarize the results
{
  "jsonrpc": "2.0",
  "id": 9,
  "method": "tools/call",
  "params": {
    "name": "ask_llm",
    "arguments": {
      "prompt": "Based on these BuilderTrend articles: [paste results], give me a step-by-step guide to create a schedule",
      "systemPrompt": "You are a BuilderTrend expert. Create clear, actionable instructions."
    }
  }
}
```

## Tips for Better Results

1. **Be Specific**: The more specific your prompt, the better the response
   - ❌ "Tell me about code"
   - ✅ "Explain the SOLID principles in C# with examples"

2. **Use System Prompts**: Set the right context for your use case
   - For code: "You are an expert C# developer"
   - For explanations: "You are a patient teacher"
   - For reviews: "You are a senior engineer conducting a code review"

3. **Iterate**: If the first response isn't perfect, refine your prompt
   
4. **Model Selection**: Choose the right model for your needs
   - Fast responses: Groq with `llama-3.1-8b-instant`
   - Better quality: Ollama with `llama3.2`
   - Privacy: Always use Ollama for sensitive data

## Error Handling

If you get errors, check:

1. **"LLM service is not configured"**: 
   - Verify `appsettings.json` exists and is properly formatted
   - Run `./test-llm.ps1` to verify configuration

2. **"Connection refused"** (Ollama):
   - Make sure Ollama is running
   - Check if the model is pulled: `ollama list`

3. **"API key invalid"** (OpenRouter/Groq):
   - Verify your API key in `appsettings.json`
   - Check if the key is active in your provider's dashboard

4. **"Model not found"**:
   - Check model name spelling in `appsettings.json`
   - For Ollama: `ollama list` to see available models
   - For cloud providers: Check their documentation for available models
