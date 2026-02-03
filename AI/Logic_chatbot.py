import logging
import os
import json
from enum import Enum
from typing import Dict
from dotenv import load_dotenv
import google.generativeai as genai

# Load environment variables
load_dotenv()

# Disable logging
# Bật logging để debug
logging.basicConfig(level=logging.INFO, format='%(asctime)s [%(levelname)s] %(message)s')
logger = logging.getLogger(__name__)
# logger.disabled = True # Enable logger for debugging

try:
    from Config_manager import api_key_manager
except ImportError:
    import sys
    sys.path.append(os.path.dirname(os.path.abspath(__file__)))
    from Config_manager import api_key_manager


# ==================== USER STATE ====================
class UserState(Enum):
    NEW_VISITOR = "new_visitor"
    WAITING_FIRST_CHOICE = "waiting_first_choice"
    IN_FLOW_TU_VAN = "in_flow_tu_van"
    IN_FLOW_TRIAL_PARENT = "in_flow_trial_parent"
    IN_FLOW_TRIAL_STUDENT = "in_flow_trial_student"
    LEAD_COLLECTED = "lead_collected"
    HANDOVER_TO_HUMAN = "handover_to_human"
    IDLE = "idle"

class User:
    def __init__(self, user_id: str):
        self.id = user_id
        self.state = UserState.NEW_VISITOR
        self.has_interacted = False
        self.lead_submitted = False

# ==================== CHATBOT LOGIC BACKEND ====================
class ChatbotLogicBackend:
    """
    Backend chatbot với LLM integration.
    Quick Reply → rule-based mapping
    Free Text → Gemini LLM (with API key rotation)
    """
    def __init__(self):
        self.users: Dict[str, User] = {}
        self.api_manager = api_key_manager
        self.api_manager.configure()
        self.model_name = "gemini-1.5-flash"
        self._init_model()
    
    def _init_model(self):
        """Initialize Gemini model"""
        self.model = genai.GenerativeModel(self.model_name)

    def get_user(self, user_id: str) -> User:
        if user_id not in self.users:
            self.users[user_id] = User(user_id)
        return self.users[user_id]

    # ---------- Handle Quick Reply ----------
    def handle_quick_reply(self, user_id: str, reply: str) -> Dict:
        user = self.get_user(user_id)
        user.has_interacted = True

        if not reply:
            return self._flow_handover(user)
            
        # Chuyển về chữ thường và xóa khoảng trắng để so sánh cho dễ
        r = reply.lower().strip()
        logger.info(f"Processing Quick Reply: '{r}'")

        # Dùng 'in' để so sánh từ khóa, tránh lỗi Encoding hoặc sai lệch 1-2 chữ
        
        # ===== Main flows =====
        if "tư vấn" in r and "lớp 6" in r:
            return self._flow_tu_van(user)
        
        elif "học thử" in r and ("miễn phí" in r or "cho con" in r):
            return self._flow_hoc_thu_student(user)
            
        elif "báo cáo" in r and "mẫu" in r:
            return self._flow_bao_cao(user)
            
        elif "hay làm sai" in r or ("không hiểu" in r and "sai" in r):
            return self._flow_con_hay_lam_sai(user)
            
        elif "học chậm" in r or "dễ quên" in r:
            return self._flow_con_hoc_cham(user)
            
        elif "ngại học" in r:
            return self._flow_con_ngai_hoc(user)
            
        elif "theo sát" in r:
            return self._flow_theo_sat(user)
            
        elif "học phí" in r or "lộ trình" in r:
            return self._flow_hoc_phi(user)
        
        # ===== Sub-option flows =====
        # Từ _flow_con_hay_lam_sai options
        elif "có" in r and "cho con học thử" in r:
            return self._flow_hoc_thu_parent(user)
        
        elif "tìm hiểu thêm" in r:
            return self._flow_tu_van_more(user)
        
        # Từ _flow_hoc_thu_student options
        elif "nhờ" in r and "bố" in r and "mẹ" in r:
            return self._flow_hoc_thu_parent_help(user)
        
        elif "học thử" in r and "nhờ" not in r:
            return self._flow_hoc_thu_parent(user)
        
        # Từ _flow_bao_cao options
        elif "nhận" in r and "báo cáo" in r:
            return self._flow_bao_cao(user)
        
        elif "tư vấn thêm" in r and "báo" not in r:
            return self._flow_handover(user)
        
        # Từ _flow_hoc_phi options
        elif "xem" in r and "lộ trình" in r:
            return self._flow_tu_van_more(user)
        
        elif "được tư vấn chi tiết" in r:
            return self._flow_handover(user)
        
        elif "cho con học thử trước" in r:
            return self._flow_hoc_thu_parent(user)
            
        elif "liên hệ" in r or "nhân viên" in r or "gọi điện" in r:
            return self._flow_handover(user)
            
        # Nếu không khớp cái nào bên trên thì mới fallback
        logger.warning(f"✗ Vẫn không khớp: '{r}'")
        return self._flow_handover(user)

    # ---------- Handle Free Text ----------
    def handle_free_text(self, user_id: str, text: str) -> Dict:
        """
        Xử lý text tự do từ người dùng bằng Gemini LLM.
        LLM sẽ:
        1. Chào lại nếu là lời chào
        2. Xác định flow phù hợp
        3. Gọi flow đó hoặc fallback nếu không match
        """
        user = self.get_user(user_id)
        user.has_interacted = True
        
        # Nếu không có API key, dùng fallback
        if not self.api_manager.api_keys:
            logger.warning("No API keys available, using fallback")
            return self._flow_fallback(user)
        
        return self._call_llm_with_retry(user, text)
    
    def _call_llm_with_retry(self, user: User, text: str) -> Dict:
        """Call LLM with retry logic on key rotation"""
        max_retries = len(self.api_manager.api_keys)
        
        for attempt in range(max_retries):
            try:
                # Prompt cho LLM
                system_prompt = (
                    "Bạn là trợ lý ảo thông minh của web toán học hay, khi người dùng hỏi hãy trả lời lịch sự. "
                    "Mục tiêu của bạn là khi người dùng chào bạn bạn hãy chào lại, với câu hỏi người dùng thì quyết định xem nó thuộc flow nào và gọi tới flow đó. "
                    "Khi cảm thấy không thuộc flow nào thì hãy điều hướng tới flow fall back."
                    "\n\nCác flow có sẵn:\n"
                    "- tu_van: Tư vấn cho con lớp 6\n"
                    "- con_hay_lam_sai: Con hay làm sai, không hiểu vì sao\n"
                    "- con_hoc_cham: Con học chậm, dễ quên bài\n"
                    "- con_ngai_hoc: Con ngại học Toán\n"
                    "- theo_sat: Tôi muốn theo sát việc học của con\n"
                    "- hoc_thu: Học thử miễn phí\n"
                    "- bao_cao: Xem báo cáo tiến độ\n"
                    "- hoc_phi: Học phí & lộ trình\n"
                    "- handover: Cần tư vấn chi tiết / liên hệ nhân viên\n"
                    "- fallback: Không rõ / không thuộc flow nào\n"
                    "- greeting: Chào hỏi (trả lời lịch sự)\n"
                    "\n"
                    "Hãy trả lời với format JSON: {\"flow\": \"<flow_name>\", \"message\": \"<your_response>\"}"
                )
                
                prompt = f"{system_prompt}\n\nUser message: {text}"
                
                # Gọi Gemini API
                response = self.model.generate_content(prompt)
                response_text = response.text.strip()
                logger.info(f"LLM response: {response_text}")
                
                # Thử parse JSON response (remove markdown backticks nếu có)
                try:
                    # Remove markdown code block markers
                    if response_text.startswith("```"):
                        response_text = response_text.split("```")[1]
                        if response_text.startswith("json"):
                            response_text = response_text[4:]
                        response_text = response_text.strip()
                    
                    result = json.loads(response_text)
                    flow_name = result.get("flow", "fallback")
                    message = result.get("message", "")
                except json.JSONDecodeError as e:
                    logger.warning(f"Failed to parse LLM response: {response_text}, error: {str(e)}")
                    return self._flow_fallback(user)
                
                # Mapping flow name to flow handler
                flow_handlers = {
                    "tu_van": self._flow_tu_van,
                    "con_hay_lam_sai": self._flow_con_hay_lam_sai,
                    "con_hoc_cham": self._flow_con_hoc_cham,
                    "con_ngai_hoc": self._flow_con_ngai_hoc,
                    "theo_sat": self._flow_theo_sat,
                    "hoc_thu": self._flow_hoc_thu_student,
                    "bao_cao": self._flow_bao_cao,
                    "hoc_phi": self._flow_hoc_phi,
                    "handover": self._flow_handover,
                    "greeting": self._flow_greeting,
                    "fallback": self._flow_fallback,
                }
                
                handler = flow_handlers.get(flow_name, self._flow_fallback)
                result = handler(user)
                
                # Thêm LLM message vào response nếu có (chỉ khi không phải fallback)
                if message and flow_name != "fallback":
                    if result.get("type") == "quick_reply":
                        result["message"] = message + "\n" + result.get("message", "")
                    else:
                        result["message"] = message
                
                return result
                
            except Exception as e:
                logger.error(f"API call attempt {attempt + 1} failed: {str(e)}")
                
                if attempt < max_retries - 1:
                    # Rotate API key and reconfigure
                    new_key = self.api_manager.rotate_key()
                    self.api_manager.configure()
                    # Recreate model with new API key
                    self._init_model()
                    logger.info(f"Retrying with next API key...")
                else:
                    logger.error(f"Max retries ({max_retries}) exceeded")
                    return self._flow_fallback(user)
        
        return self._flow_fallback(user)

    # ---------- Flow handlers với message đầy đủ ----------
    def _flow_tu_van(self, user: User) -> Dict:
        user.state = UserState.IN_FLOW_TU_VAN
        return {
            "type": "quick_reply",
            "message": (
                "Lớp 6 là giai đoạn nhiều bạn bị “sốc” vì kiến thức Toán tăng nhanh "
                "và cách học thay đổi.\n"
                "Anh/chị thấy con đang gặp vấn đề nào nhiều nhất?"
            ),
            "options": [
                "Con hay làm sai, không hiểu vì sao",
                "Con học chậm, dễ quên bài",
                "Con ngại học Toán",
                "Tôi muốn theo sát việc học của con"
            ]
        }

    def _flow_con_hay_lam_sai(self, user: User) -> Dict:
        return {
            "type": "quick_reply",
            "message": (
                "Đây là tình trạng rất phổ biến ở học sinh lớp 6.\n"
                "ToánHọcHay giúp con hiểu từng bước khi làm bài, "
                "để biết mình sai ở đâu và sửa lại cho đúng.\n"
                "Anh/chị có muốn cho con học thử miễn phí để xem con có phù hợp không?"
            ),
            "options": ["Có, cho con học thử", "Tìm hiểu thêm"]
        }

    def _flow_con_hoc_cham(self, user: User) -> Dict:
        return {
            "type": "message",
            "message": "ToánHọcHay giúp con học theo từng bước, nhớ bài lâu hơn và củng cố kiến thức nền."
        }

    def _flow_con_ngai_hoc(self, user: User) -> Dict:
        return {
            "type": "message",
            "message": "ToánHọcHay tạo hứng thú học tập bằng bài giảng trực quan, dễ hiểu, giúp con tự tin học Toán."
        }

    def _flow_theo_sat(self, user: User) -> Dict:
        return {
            "type": "message",
            "message": "ToánHọcHay cung cấp báo cáo tiến độ và gợi ý ôn tập, giúp phụ huynh theo sát việc học của con mà không cần kèm cặp từng bài."
        }

    def _flow_hoc_thu_parent(self, user: User) -> Dict:
        user.state = UserState.IN_FLOW_TRIAL_PARENT 
        return {
            "type": "form",
            "message": (
                "ToánHọcHay hiện có chương trình học thử miễn phí cho học sinh lớp 6.\n"
                "Anh/chị để lại thông tin để bên mình gửi hướng dẫn cho con nhé."
            ),
            "form_fields": [
                "Họ tên phụ huynh (bắt buộc)",
                "Số điện thoại (bắt buộc)",
                "Email (không bắt buộc)"
            ]
        }

    def _flow_tu_van_more(self, user: User) -> Dict:
        return {
            "type": "message",
            "message": "Anh/chị có thể xem thêm thông tin chi tiết tại website hoặc liên hệ đội ngũ tư vấn để được hỗ trợ đầy đủ."
        }

    def _flow_hoc_thu_student(self, user: User) -> Dict:
        user.state = UserState.IN_FLOW_TRIAL_STUDENT
        return {
            "type": "quick_reply", # Đổi từ form thành quick_reply
            "message": (
                "Chào em, ToánHọcHay giúp học sinh lớp 6 học Toán từng bước, dễ hiểu hơn.\n"
                "Em muốn làm gì tiếp?"
            ),
            "options": ["Học thử", "Nhờ bố/mẹ xem giúp"]
        }

    def _flow_hoc_thu_parent_help(self, user: User) -> Dict:
        return {
            "type": "message",
            "message": "Em có thể nhờ bố/mẹ hoặc người thân điền form để nhận hướng dẫn học thử."
        }

    def _flow_bao_cao(self, user: User) -> Dict:
        return {
            "type": "quick_reply", # Đổi từ form thành quick_reply
            "message": (
                "Báo cáo giúp phụ huynh nắm được:\n"
                "- Con đang học đến đâu\n"
                "- Những phần con còn yếu\n"
                "- Gợi ý nội dung cần ôn lại\n"
                "Anh/chị muốn thực hiện thao tác nào?"
            ),
            "options": ["Nhận báo cáo mẫu", "Tư vấn thêm"]
        }

    def _flow_hoc_phi(self, user: User) -> Dict:
        return {
            "type": "quick_reply",
            "message": (
                "ToánHọcHay xây dựng lộ trình học Toán lớp 6 theo từng giai đoạn, "
                "phù hợp với khả năng của mỗi học sinh.\n"
                "Học phí được thiết kế ở mức phù hợp để phụ huynh có thể cho con học đều đặn.\n"
                "Anh/chị muốn:"
            ),
            "options": ["Xem lộ trình học", "Được tư vấn chi tiết", "Cho con học thử trước"]
        }

    def _flow_handover(self, user: User) -> Dict:
        user.state = UserState.HANDOVER_TO_HUMAN
        return {
            "type": "message",
            "message": "Mình đã ghi nhận yêu cầu. Đội ngũ ToánHọcHay sẽ liên hệ lại để tư vấn cụ thể hơn."
        }

    def _flow_greeting(self, user: User) -> Dict:
        """Xử lý lời chào hỏi - chỉ trả lời message từ LLM"""
        return {
            "type": "message",
            "message": ""  # Message sẽ được thêm từ LLM
        }

    def _flow_fallback(self, user: User) -> Dict:
        return {
            "type": "quick_reply",
            "message": "Mình chưa hiểu câu hỏi này. Anh/chị có thể chọn nội dung bên dưới để mình hỗ trợ nhanh hơn nhé.",
            "options": ["Tư vấn cho con lớp 6", "Học thử miễn phí", "Báo cáo tiến độ mẫu"]
        }

# ==================== EXAMPLE ====================
if __name__ == "__main__":
    chatbot = ChatbotLogicBackend()

    user_id = "user_1"

    # 1. User click Quick Reply
    response = chatbot.handle_quick_reply(user_id, "Xem báo cáo tiến độ mẫu")
    print("Quick Reply response:", response)

    # 2. User gõ text tự do
    free_text = "Bạn sinh năm bao nhiêu?"
    response2 = chatbot.handle_free_text(user_id, free_text)
    print("Free Text response (fallback):", response2)
