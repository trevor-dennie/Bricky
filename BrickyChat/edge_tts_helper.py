#!/usr/bin/env python3
"""
Simple Edge TTS helper script
Generates natural speech using Microsoft Edge TTS (free)
"""
import sys
import edge_tts
import asyncio

async def generate_speech(text, output_file):
    """Generate speech from text using Edge TTS"""
    voice = "en-US-JennyNeural"  # Natural female voice
    communicate = edge_tts.Communicate(text, voice)
    await communicate.save(output_file)

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: edge_tts_helper.py <text> <output_file>")
        sys.exit(1)
    
    text = sys.argv[1]
    output_file = sys.argv[2]
    
    asyncio.run(generate_speech(text, output_file))
    print(f"Audio saved to {output_file}")
