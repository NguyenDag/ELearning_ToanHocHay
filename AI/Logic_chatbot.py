import logging
from enum import Enum
from typing import Dict

# Disable logging
logging.basicConfig(level=logging.CRITICAL)
logger = logging.getLogger(__name__)
logger.disabled = True

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
    Backend chatbot rule-based, không dùng LLM.
    Quick Reply → rule-based mapping
    Free Text → fallback
    """
    def __init__(self):
        self.users: Dict[str, User] = {}

    def get_user(self, user_id: str) -> User:
        if user_id not in self.users:
            self.users[user_id] = User(user_id)
        return self.users[user_id]

    # ---------- Handle Quick Reply ----------
    def handle_quick_reply(self, user_id: str, reply: str) -> Dict:
        user = self.get_user(user_id)
        user.has_interacted = True

        # Normalize reply
        if reply:
            reply = reply.strip()

        flow_map = {
            "Tư vấn cho con lớp 6": self._flow_tu_van,
            "Con hay làm sai, không hiểu vì sao": self._flow_con_hay_lam_sai,
            "Con học chậm, dễ quên bài": self._flow_con_hoc_cham,
            "Con ngại học Toán": self._flow_con_ngai_hoc,
            "Tôi muốn theo sát việc học của con": self._flow_theo_sat,
            "Có, cho con học thử": self._flow_hoc_thu_parent,
            "Tìm hiểu thêm": self._flow_tu_van_more,
            "Học thử": self._flow_hoc_thu_student,
            "Nhờ bố/mẹ xem giúp": self._flow_hoc_thu_parent_help,
            "Xem báo cáo tiến độ mẫu": self._flow_bao_cao,
            "Nhận báo cáo mẫu": self._flow_bao_cao,
            "Học phí & lộ trình": self._flow_hoc_phi,
            "Xem lộ trình học": self._flow_hoc_phi,
            "Được tư vấn chi tiết": self._flow_hoc_phi,
            "Cho con học thử trước": self._flow_hoc_phi,
            "Tư vấn thêm": self._flow_handover,
            "Liên hệ": self._flow_handover,
            "Gọi điện": self._flow_handover,
            "Nhân viên": self._flow_handover,
        }

        logger.info(f"Quick reply '{reply}' from user {user_id}")
        
        if reply in flow_map:
            logger.info(f"✓ Match found: {flow_map[reply].__name__}")
            return flow_map[reply](user)
        else:
            logger.warning(f"✗ No match for reply: '{reply}' - Fallback triggered")
            return self._flow_fallback(user)

    # ---------- Handle Free Text ----------
    def handle_free_text(self, user_id: str, text: str) -> Dict:
        # Nếu người dùng gõ text tự do, backend **không phân tích**, dùng fallback
        user = self.get_user(user_id)
        user.has_interacted = True
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
            "type": "form",
            "message": (
                "Chào em, ToánHọcHay giúp học sinh lớp 6 học Toán từng bước, dễ hiểu hơn.\n"
                "Em muốn làm gì tiếp?"
            ),
            "form_fields": [
                "Họ tên (bắt buộc)",
                "Lớp (bắt buộc)",
                "Email (không bắt buộc)"
            ],
            "options": ["Học thử", "Hỏi bài", "Nhờ bố/mẹ xem giúp"]
        }

    def _flow_hoc_thu_parent_help(self, user: User) -> Dict:
        return {
            "type": "message",
            "message": "Anh/chị có thể nhờ bố/mẹ hoặc người thân điền form để nhận hướng dẫn học thử."
        }

    def _flow_bao_cao(self, user: User) -> Dict:
        return {
            "type": "form",
            "message": (
                "Báo cáo giúp phụ huynh nắm được:\n"
                "- Con đang học đến đâu\n"
                "- Những phần con còn yếu\n"
                "- Gợi ý nội dung cần ôn lại\n"
                "Anh/chị có muốn nhận bản báo cáo mẫu qua email không?"
            ),
            "form_fields": ["Email (bắt buộc)"],
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
    response = chatbot.handle_quick_reply(user_id, "Tư vấn cho con lớp 6")
    print("Quick Reply response:", response)

    # 2. User gõ text tự do
    free_text = "Tôi muốn cho con học thử vài buổi đầu"
    response2 = chatbot.handle_free_text(user_id, free_text)
    print("Free Text response (fallback):", response2)
