import os
import logging
import google.generativeai as genai
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Setup logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s [%(levelname)s] %(message)s')
logger = logging.getLogger(__name__)

class GeminiAPIKeyManager:
    """Manage multiple Gemini API keys with rotation on failure"""
    
    def __init__(self):
        # Get API keys from env (support multiple keys: GEMINI_API_KEY_1, GEMINI_API_KEY_2, etc.)
        self.api_keys = []
        self.current_index = 0
        
        # Try to get multiple API keys
        i = 1
        while True:
            key = os.getenv(f"GEMINI_API_KEY_{i}")
            if not key:
                break
            self.api_keys.append(key)
            i += 1
        
        # If no numbered keys, try single key
        if not self.api_keys:
            key = os.getenv("GEMINI_API_KEY")
            if key:
                self.api_keys.append(key)
        
        if not self.api_keys:
            logger.warning("⚠️ No Gemini API keys found in environment variables. AI features may not work.")
        else:
            logger.info(f"✅ Loaded {len(self.api_keys)} Gemini API key(s)")
    
    def get_current_key(self):
        """Get current API key"""
        if not self.api_keys:
            return None
        return self.api_keys[self.current_index]
    
    def rotate_key(self):
        """Rotate to next API key on failure"""
        if not self.api_keys:
            return None
            
        self.current_index = (self.current_index + 1) % len(self.api_keys)
        logger.warning(f"🔄 Rotating to API key #{self.current_index + 1}")
        return self.get_current_key()
    
    def configure(self):
        """Configure genai with current API key"""
        current_key = self.get_current_key()
        if current_key:
            genai.configure(api_key=current_key)
            return True
        return False

# Initialize global instance
api_key_manager = GeminiAPIKeyManager()
