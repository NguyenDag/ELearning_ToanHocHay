import google.generativeai as genai
import json
from typing import Dict, List, Optional
import os
from dotenv import load_dotenv
import sys
import requests
from PIL import Image
from io import BytesIO
import logging

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Add parent directory to path for imports
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from Prompts import hint_prompt, feedback_prompt

# Load environment variables
load_dotenv()

# ==================== API KEY ROTATION ====================
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
            raise ValueError("No Gemini API keys found in environment variables")
        
        logger.info(f"Loaded {len(self.api_keys)} Gemini API key(s)")
    
    def get_current_key(self) -> str:
        """Get current API key"""
        return self.api_keys[self.current_index]
    
    def rotate_key(self) -> str:
        """Rotate to next API key on failure"""
        self.current_index = (self.current_index + 1) % len(self.api_keys)
        logger.warning(f"Rotating to API key #{self.current_index + 1}")
        return self.get_current_key()
    
    def configure(self):
        """Configure genai with current API key"""
        genai.configure(api_key=self.get_current_key())


# Initialize API key manager
api_key_manager = GeminiAPIKeyManager()
api_key_manager.configure()

class GeminiAIService:
    """Service to interact with Google Gemini AI for educational hints and feedback"""
    
    def __init__(self, model_name: str = "gemini-2.5-flash"):
        # Initialize model with JSON mode for structured responses
        self.model_name = model_name
        self.model = genai.GenerativeModel(
            model_name,
            generation_config=genai.types.GenerationConfig(
                response_mime_type="application/json"
            )
        )
    
    # ==================== HINT GENERATION ====================
    def generate_hint(
        self,
        question_text: str,
        question_type: str,
        difficulty_level: str,
        student_answer: str,
        hint_level: int = 1,
        options: Optional[List[Dict]] = None,
        question_id: int = None,
        question_image_url: Optional[str] = None
    ) -> Dict[str, str]:
        """
        Generate a hint for a specific question without revealing the answer.
        
        Args:
            question_text: The question text
            question_type: Type of question (MultipleChoice, TrueFalse, FillBlank, Essay)
            difficulty_level: Difficulty level (Easy, Medium, Hard)
            student_answer: Student's current answer or "Chưa trả lời"
            hint_level: Hint level 1-3 (1=general, 2=specific, 3=step-by-step)
            options: List of options for multiple choice (without IsCorrect field)
            question_id: Question ID for tracking
            question_image_url: URL to question image (optional)
        
        Returns:
            Dictionary with HintText and HintLevel
        """
        try:
            # Format options text
            options_text = self._format_options(options, include_correct=False)
            
            # Format the prompt
            formatted_prompt = hint_prompt.format(
                hint_level=hint_level,
                question_text=question_text,
                question_type=question_type,
                difficulty_level=difficulty_level,
                student_answer=student_answer,
                options_text=options_text
            )
            
            # Add JSON format instruction
            formatted_prompt += "\n\nCung cấp phản hồi dưới dạng JSON với cấu trúc: {\"hint_text\": \"...\", \"hint_level\": 1}"
            
            # Prepare content with image if provided
            content = self._prepare_content_with_image(formatted_prompt, question_image_url)
            
            # Call Gemini API with retry logic
            response = self._call_api_with_retry(content)
            
            if response.get("Status") == "error":
                return response
            
            # Parse JSON response
            response_text = response.get("text", "")
            hint_data = json.loads(response_text)
            
            return {
                "HintText": hint_data.get("hint_text", response_text),
                "HintLevel": hint_level,
                "QuestionId": question_id,
                "Status": "success"
            }
        
        except json.JSONDecodeError as e:
            logger.error(f"JSON decode error: {str(e)}")
            return {
                "HintText": f"Lỗi xử lý phản hồi AI: {str(e)}",
                "HintLevel": hint_level,
                "QuestionId": question_id,
                "Status": "error",
                "Error": str(e)
            }
        
        except Exception as e:
            logger.error(f"Error generating hint: {str(e)}")
            return {
                "HintText": f"Lỗi tạo gợi ý: {str(e)}",
                "HintLevel": hint_level,
                "QuestionId": question_id,
                "Status": "error",
                "Error": str(e)
            }
    
    # ==================== FEEDBACK GENERATION ====================
    def generate_feedback(
        self,
        question_text: str,
        question_type: str,
        student_answer: str,
        correct_answer: str,
        is_correct: bool,
        explanation: Optional[str] = None,
        options: Optional[List[Dict]] = None,
        attempt_id: int = None,
        question_image_url: Optional[str] = None
    ) -> Dict[str, str]:
        """
        Generate comprehensive feedback after exercise completion.
        
        Args:
            question_text: The question text
            question_type: Type of question
            student_answer: Student's answer
            correct_answer: Correct answer
            is_correct: Whether student answer is correct
            explanation: Teacher's explanation (optional)
            options: List of options for multiple choice (WITH IsCorrect field)
            attempt_id: Attempt ID for tracking
            question_image_url: URL to question image (optional)
        
        Returns:
            Dictionary with FullSolution, MistakeAnalysis, ImprovementAdvice
        """
        try:
            # Format options text (including correct answers for feedback)
            options_text = self._format_options(options, include_correct=True)
            
            from Prompts import feedback_prompt
            
            # Format the prompt
            formatted_prompt = feedback_prompt.format(
                question_text=question_text,
                question_type=question_type,
                correct_answer=correct_answer,
                explanation=explanation or "Không có giải thích thêm",
                student_answer=student_answer,
                is_correct="✓ Đúng" if is_correct else "✗ Sai",
                options_text=options_text
            )
            
            # Add JSON format instruction
            formatted_prompt += """\n\nCung cấp phản hồi dưới dạng JSON với cấu trúc:
{
    "full_solution": "Lời giải hoàn chỉnh...",
    "mistake_analysis": "Phân tích lỗi...",
    "improvement_advice": "Lời khuyên cải thiện..."
}"""
            
            # Prepare content with image if provided
            content = self._prepare_content_with_image(formatted_prompt, question_image_url)
            
            # Call Gemini API with retry logic
            response = self._call_api_with_retry(content)
            
            if response.get("Status") == "error":
                return {
                    "FullSolution": response.get("text", "Lỗi tạo feedback"),
                    "MistakeAnalysis": "",
                    "ImprovementAdvice": "",
                    "AttemptId": attempt_id,
                    "Status": "error",
                    "Error": response.get("Error")
                }
            
            # Parse JSON response
            response_text = response.get("text", "")
            feedback_data = json.loads(response_text)
            
            return {
                "FullSolution": feedback_data.get("full_solution", ""),
                "MistakeAnalysis": feedback_data.get("mistake_analysis", ""),
                "ImprovementAdvice": feedback_data.get("improvement_advice", ""),
                "AttemptId": attempt_id,
                "Status": "success"
            }
        
        except json.JSONDecodeError as e:
            logger.error(f"JSON decode error: {str(e)}")
            return {
                "FullSolution": f"Lỗi xử lý phản hồi AI: {str(e)}",
                "MistakeAnalysis": "",
                "ImprovementAdvice": "",
                "AttemptId": attempt_id,
                "Status": "error",
                "Error": str(e)
            }
        
        except Exception as e:
            logger.error(f"Error generating feedback: {str(e)}")
            return {
                "FullSolution": f"Lỗi tạo feedback: {str(e)}",
                "MistakeAnalysis": "",
                "ImprovementAdvice": "",
                "AttemptId": attempt_id,
                "Status": "error",
                "Error": str(e)
            }
    
    # ==================== HELPER METHODS ====================
    def _download_image(self, image_url: str) -> Optional[Image.Image]:
        """Download image from URL and convert to PIL Image"""
        try:
            if not image_url or not image_url.startswith(('http://', 'https://')):
                return None
            
            response = requests.get(image_url, timeout=10)
            response.raise_for_status()
            
            image = Image.open(BytesIO(response.content))
            return image
        
        except Exception as e:
            logger.warning(f"Failed to download image from {image_url}: {str(e)}")
            return None
    
    def _prepare_content_with_image(
        self,
        prompt_text: str,
        image_url: Optional[str] = None
    ) -> List:
        """Prepare content for Gemini with optional image"""
        content = [prompt_text]
        
        if image_url:
            image = self._download_image(image_url)
            if image:
                content.append(image)
                logger.info(f"Added image to prompt: {image_url}")
        
        return content
    
    def _call_api_with_retry(self, content) -> Dict:
        """Call Gemini API with retry logic on key rotation"""
        max_retries = len(api_key_manager.api_keys)
        
        for attempt in range(max_retries):
            try:
                response = self.model.generate_content(content)
                return {
                    "text": response.text.strip(),
                    "Status": "success"
                }
            
            except Exception as e:
                logger.error(f"API call attempt {attempt + 1} failed: {str(e)}")
                
                if attempt < max_retries - 1:
                    # Rotate API key and reconfigure
                    new_key = api_key_manager.rotate_key()
                    api_key_manager.configure()
                    # Recreate model with new API key
                    self.model = genai.GenerativeModel(
                        self.model_name,
                        generation_config=genai.types.GenerationConfig(
                            response_mime_type="application/json"
                        )
                    )
                else:
                    return {
                        "text": f"API Error after {max_retries} attempts: {str(e)}",
                        "Status": "error",
                        "Error": str(e)
                    }
        
        return {
            "text": "Unknown error",
            "Status": "error",
            "Error": "Max retries exceeded"
        }
    
    def _format_options(
        self,
        options: Optional[List[Dict]],
        include_correct: bool = False
    ) -> str:
        """Format options for display in prompts"""
        if not options:
            return "Không có lựa chọn"
        
        options_text = ""
        for idx, opt in enumerate(options, 1):
            option_line = f"{idx}. {opt.get('OptionText', '')}"
            
            if include_correct:
                is_correct = opt.get('IsCorrect', False)
                option_line += " ✓" if is_correct else ""
            
            if opt.get('ImageUrl'):
                option_line += f" [Hình ảnh: {opt['ImageUrl']}]"
            
            options_text += option_line + "\n"
        
        return options_text


# ==================== STANDALONE FUNCTIONS ====================
async def get_ai_hint(
    question_data: Dict,
    student_answer: str,
    hint_level: int = 1
) -> Dict[str, str]:
    """
    Standalone function to get AI hint.
    
    Expected question_data format:
    {
        "QuestionId": 1,
        "QuestionText": "...",
        "QuestionType": "MultipleChoice",
        "DifficultyLevel": "Medium",
        "QuestionImageUrl": "https://...", (optional)
        "QuestionOptions": [
            {"OptionId": 1, "OptionText": "..."},
            ...
        ]
    }
    """
    service = GeminiAIService()
    
    return service.generate_hint(
        question_text=question_data.get("QuestionText"),
        question_type=question_data.get("QuestionType"),
        difficulty_level=question_data.get("DifficultyLevel"),
        student_answer=student_answer,
        hint_level=hint_level,
        options=question_data.get("QuestionOptions"),
        question_id=question_data.get("QuestionId"),
        question_image_url=question_data.get("QuestionImageUrl")
    )


async def get_ai_feedback(
    question_data: Dict,
    student_answer_data: Dict,
    correct_answer: str,
    is_correct: bool
) -> Dict[str, str]:
    """
    Standalone function to get AI feedback.
    
    Expected question_data format:
    {
        "QuestionId": 1,
        "QuestionText": "...",
        "QuestionType": "MultipleChoice",
        "Explanation": "...",
        "QuestionImageUrl": "https://...", (optional)
        "QuestionOptions": [
            {"OptionId": 1, "OptionText": "...", "IsCorrect": true/false},
            ...
        ]
    }
    
    Expected student_answer_data format:
    {
        "AttemptId": 1,
        "AnswerText": "...",
        "SelectedOptionId": 1
    }
    """
    service = GeminiAIService()
    
    return service.generate_feedback(
        question_text=question_data.get("QuestionText"),
        question_type=question_data.get("QuestionType"),
        student_answer=student_answer_data.get("AnswerText"),
        correct_answer=correct_answer,
        is_correct=is_correct,
        explanation=question_data.get("Explanation"),
        options=question_data.get("QuestionOptions"),
        attempt_id=student_answer_data.get("AttemptId"),
        question_image_url=question_data.get("QuestionImageUrl")
    )


if __name__ == "__main__":
    # Test example
    service = GeminiAIService()
    
    # Test hint
    print("Testing hint generation...")
    hint_result = service.generate_hint(
        question_text="Tính đạo hàm của f(x) = 2x³ + 3x²",
        question_type="FillBlank",
        difficulty_level="Medium",
        student_answer="f'(x) = 6x² + 6x",
        hint_level=1
    )
    print(f"Hint: {hint_result}")
    
    print("\n" + "="*50 + "\n")
    
    # Test feedback
    print("Testing feedback generation...")
    feedback_result = service.generate_feedback(
        question_text="Tính đạo hàm của f(x) = 2x³ + 3x²",
        question_type="FillBlank",
        student_answer="f'(x) = 6x² + 6x",
        correct_answer="f'(x) = 6x² + 6x",
        is_correct=True,
        explanation="Sử dụng quy tắc lũy thừa để tính đạo hàm"
    )
    print(f"Feedback: {json.dumps(feedback_result, ensure_ascii=False, indent=2)}")
