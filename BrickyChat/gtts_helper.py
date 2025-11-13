#!/usr/bin/env python3
"""
Google TTS helper - uses Google Translate's free TTS
This actually works without API keys!
"""
import sys
from gtts import gTTS

def generate_speech(text, output_file):
    """Generate speech using Google TTS"""
    try:
        tts = gTTS(text=text, lang='en', slow=False, tld='com')
        tts.save(output_file)
        print(f"Audio saved to {output_file}")
        return 0
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: gtts_helper.py <text> <output_file>", file=sys.stderr)
        sys.exit(1)
    
    text = sys.argv[1]
    output_file = sys.argv[2]
    
    sys.exit(generate_speech(text, output_file))
