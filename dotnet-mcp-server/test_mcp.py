"""Simple MCP test client"""
import json
import subprocess
import sys

def main():
    query = " ".join(sys.argv[1:]) if len(sys.argv) > 1 else "how to create an invoice"
    
    print("╔══════════════════════════════════════════╗")
    print("║    MCP Server Test (Python Client)       ║")
    print("╚══════════════════════════════════════════╝")
    print()
    print(f"Query: '{query}'")
    print()
    
    # Start MCP server
    proc = subprocess.Popen(
        ["dotnet", "run", "--project", "McpServer.csproj"],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        bufsize=1,
        encoding='utf-8',
        errors='replace'
    )
    
    print("✅ Server started")
    print()
    
    # Send initialize
    init_msg = {
        "jsonrpc": "2.0",
        "id": 1,
        "method": "initialize",
        "params": {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {"name": "test", "version": "1.0"}
        }
    }
    
    proc.stdin.write(json.dumps(init_msg) + "\n")
    proc.stdin.flush()
    
    response = proc.stdout.readline()
    print("✅ Initialized")
    print()
    
    # Send bt_documentation call
    print("📞 Calling bt_documentation tool...")
    print("(First run: ~90s, Cached: ~2s)")
    print()
    
    tool_msg = {
        "jsonrpc": "2.0",
        "id": 2,
        "method": "tools/call",
        "params": {
            "name": "bt_documentation",
            "arguments": {"query": query}
        }
    }
    
    proc.stdin.write(json.dumps(tool_msg) + "\n")
    proc.stdin.flush()
    
    import time
    start_time = time.time()
    print("⏳ Waiting for response (timeout: 120s)...")
    
    # Read stderr in background
    import threading
    def read_stderr():
        for line in proc.stderr:
            if "Fetching" in line or "Loading" in line or "Indexing" in line:
                print(f"  {line.strip()}")
    
    stderr_thread = threading.Thread(target=read_stderr, daemon=True)
    stderr_thread.start()
    
    response = None
    try:
        # Wait up to 120 seconds
        proc.stdout.flush()
        response = proc.stdout.readline()
        elapsed = time.time() - start_time
    except Exception as e:
        print(f"❌ Timeout or error: {e}")
        proc.kill()
        return
    
    print()
    print("═══════════════════════════════════════════════")
    print("✅ RESPONSE:")
    print("═══════════════════════════════════════════════")
    print()
    
    if not response or response.strip() == "":
        print("❌ No response received")
    else:
        try:
            result = json.loads(response)
            if "result" in result:
                for item in result["result"]["content"]:
                    print(item["text"])
            elif "error" in result:
                print(f"❌ Error: {result['error']}")
            
            print()
            print(f"⏱️  Completed in {elapsed:.1f} seconds")
        except json.JSONDecodeError as e:
            print(f"❌ Invalid JSON response: {e}")
            print(f"Raw response: {response}")
    
    proc.kill()
    proc.wait()

if __name__ == "__main__":
    main()
