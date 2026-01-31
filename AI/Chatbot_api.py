from flask import Flask, request, jsonify, render_template_string
from flask_cors import CORS
import logging
import sys
import os
from typing import Dict, Any

# ==================== SETUP ====================
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

logging.basicConfig(level=logging.DEBUG)
logger = logging.getLogger(__name__)
logger.disabled = False
logging.getLogger('werkzeug').disabled = False

sys.path.append(os.path.join(os.path.dirname(__file__)))
from Logic_chatbot import ChatbotLogicBackend, UserState

app = Flask(__name__)
CORS(app, resources={r"/api/*": {"origins": "*"}})

app.config['JSON_AS_ASCII'] = False
app.config['JSON_SORT_KEYS'] = False

chatbot = ChatbotLogicBackend()

# ==================== VALIDATION ====================
def validate_quick_reply_request(data: Dict[str, Any]) -> tuple[bool, str]:
    # ĐÃ SỬA: Thay user_id bằng UserId để khớp Database
    required_fields = ['UserId', 'reply']
    logger.debug(f"[VALIDATE_QUICK_REPLY] Checking fields: {required_fields}")
    logger.debug(f"[VALIDATE_QUICK_REPLY] Data keys: {list(data.keys())}")
    for field in required_fields:
        if field not in data:
            logger.warning(f"[VALIDATE_QUICK_REPLY] Missing field '{field}'")
            return False, f"Missing required field: {field}"
    logger.debug(f"[VALIDATE_QUICK_REPLY] ✓ All fields valid")
    return True, ""


def validate_free_text_request(data: Dict[str, Any]) -> tuple[bool, str]:
    # ĐÃ SỬA: Thay user_id bằng UserId để khớp Database
    required_fields = ['UserId', 'text']
    logger.debug(f"[VALIDATE_FREE_TEXT] Checking fields: {required_fields}")
    logger.debug(f"[VALIDATE_FREE_TEXT] Data keys: {list(data.keys())}")
    for field in required_fields:
        if field not in data:
            logger.warning(f"[VALIDATE_FREE_TEXT] Missing field '{field}'")
            return False, f"Missing required field: {field}"
    logger.debug(f"[VALIDATE_FREE_TEXT] ✓ All fields valid")
    return True, ""


# ==================== QUICK REPLY ====================
@app.route('/api/chatbot/quick-reply', methods=['POST'])
def handle_quick_reply():
    try:
        logger.info("=" * 60)
        logger.info("[QUICK_REPLY] Received request")
        logger.info(f"Content-Type: {request.content_type}")
        logger.info(f"Raw data: {request.get_data(as_text=True)}")
        
        data = request.get_json(force=True)
        logger.info(f"Parsed JSON: {data}")
        
        if not data:
            logger.warning("[QUICK_REPLY] Empty request body")
            return jsonify({"error": "Request body must be JSON"}), 400

        is_valid, error = validate_quick_reply_request(data)
        if not is_valid:
            logger.error(f"[QUICK_REPLY] Validation failed: {error}")
            logger.error(f"[QUICK_REPLY] Available fields: {list(data.keys())}")
            return jsonify({"error": error}), 400

        # ĐÃ SỬA: Lấy UserId thay vì user_id
        UserId = data.get('UserId')
        reply = data.get('reply')
        logger.info(f"[QUICK_REPLY] Processing - UserId: {UserId}, Reply: {reply}")
        
        if reply:
            reply = str(reply).encode('utf-8', errors='replace').decode('utf-8').strip()
        
        # Gọi backend xử lý với UserId
        logger.info(f"[QUICK_REPLY] Calling chatbot backend...")
        response = chatbot.handle_quick_reply(UserId, reply)
        logger.info(f"[QUICK_REPLY] Backend response: {response}")
        
        return jsonify({
            "success": True,
            "UserId": UserId,
            "response": response
        }), 200

    except Exception as e:
        logger.exception(f"[QUICK_REPLY] Exception occurred: {str(e)}")
        return jsonify({"error": str(e)}), 500


# ==================== FREE TEXT ====================
@app.route('/api/chatbot/message', methods=['POST'])
def handle_free_text():
    try:
        logger.info("=" * 60)
        logger.info("[FREE_TEXT] Received request")
        logger.info(f"Content-Type: {request.content_type}")
        logger.info(f"Raw data: {request.get_data(as_text=True)}")
        
        data = request.get_json()
        logger.info(f"Parsed JSON: {data}")
        
        if not data:
            logger.warning("[FREE_TEXT] Empty request body")
            return jsonify({"error": "Request body must be JSON"}), 400

        is_valid, error = validate_free_text_request(data)
        if not is_valid:
            logger.error(f"[FREE_TEXT] Validation failed: {error}")
            logger.error(f"[FREE_TEXT] Available fields: {list(data.keys())}")
            return jsonify({"error": error}), 400

        # ĐÃ SỬA: Lấy UserId thay vì user_id
        UserId = data.get('UserId')
        text = data.get('text')
        logger.info(f"[FREE_TEXT] Processing - UserId: {UserId}, Text: {text}")

        logger.info(f"[FREE_TEXT] Calling chatbot backend...")
        response = chatbot.handle_free_text(UserId, text)
        logger.info(f"[FREE_TEXT] Backend response: {response}")
        
        return jsonify({
            "success": True,
            "UserId": UserId,
            "response": response
        }), 200

    except Exception as e:
        logger.exception(f"[FREE_TEXT] Exception occurred: {str(e)}")
        return jsonify({"error": str(e)}), 500


# ==================== GET USER STATE ====================
@app.route('/api/chatbot/user/<UserId>', methods=['GET'])
def get_user_state(UserId: str):
    try:
        user = chatbot.get_user(UserId)
        
        return jsonify({
            "success": True,
            "UserId": user.id,
            "state": user.state.value,
            "has_interacted": user.has_interacted,
            "lead_submitted": user.lead_submitted
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# ==================== RESET USER ====================
@app.route('/api/chatbot/user/<UserId>/reset', methods=['POST'])
def reset_user(UserId: str):
    try:
        from Logic_chatbot import User
        chatbot.users[UserId] = User(UserId)
        
        return jsonify({
            "success": True,
            "message": f"User {UserId} reset to NEW_VISITOR state"
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# ==================== HEALTH & STATUS ====================
@app.route('/api/chatbot/health', methods=['GET'])
def health():
    return jsonify({"status": "healthy", "service": "Chatbot API"}), 200

@app.route('/api/chatbot/status', methods=['GET'])
def status():
    return jsonify({
        "service": "Chatbot API",
        "status": "running",
        "active_users": len(chatbot.users)
    }), 200

# ==================== DOCUMENTATION ====================
@app.route('/api/chatbot/docs', methods=['GET'])
def swagger_ui():
    # Phần HTML này mình đã cập nhật hiển thị "UserId" để bạn dễ test
    swagger_html = """
    <!DOCTYPE html>
    <html>
    <head><title>Chatbot API Docs</title><meta charset="utf-8"/><style>
        body { font-family: sans-serif; padding: 20px; background: #f0f2f5; }
        .card { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); margin-bottom: 20px; }
        code { background: #eee; padding: 2px 5px; }
        .post { color: green; font-weight: bold; }
        pre { background: #272822; color: #f8f8f2; padding: 15px; border-radius: 5px; }
    </style></head>
    <body>
        <h1>🤖 Chatbot API (Chuẩn UserId)</h1>
        <div class="card">
            <p><span class="post">POST</span> /api/chatbot/message</p>
            <pre>{
  "UserId": "12345",
  "text": "Chào bạn"
}</pre>
        </div>
        <div class="card">
            <p><span class="post">POST</span> /api/chatbot/quick-reply</p>
            <pre>{
  "UserId": "12345",
  "reply": "Tư vấn lớp 6"
}</pre>
        </div>
    </body></html>
    """
    return render_template_string(swagger_html)

@app.route('/', methods=['GET'])
def root():
    return jsonify({"message": "Welcome", "docs": "/api/chatbot/docs"}), 200

@app.route('/api/chatbot/trigger', methods=['POST'])
def handle_trigger():
    try:
        data = request.get_json()
        UserId = data.get('UserId')
        trigger = data.get('trigger')
        
        user = chatbot.get_user(UserId)
        
        if trigger == "page_load":
            # Trigger 1: Chào mừng + quick reply
            response = {
                "type": "quick_reply",
                "message": "Chào anh/chị,\nToánHọcHay là nền tảng học Toán dành riêng cho học sinh lớp 6, giúp con học dễ hiểu hơn trong giai đoạn chuyển cấp.\nAnh/chị muốn tìm hiểu nội dung nào cho con?",
                "options": [  # ⚠️ PHẢI LÀ LIST, KHÔNG PHẢI SET
                    "Tư vấn cho con lớp 6",
                    "Cho con học thử miễn phí",
                    "Xem báo cáo tiến độ mẫu",
                    "Học phí & lộ trình"
                ]
            }
        
        elif trigger == "wait_15s" and not user.has_interacted:
            # Trigger 2: Sau 15s chưa tương tác
            response = {
                "type": "quick_reply",
                "message": "Nhiều học sinh lớp 6 gặp khó khăn vì Toán khó hơn tiểu học và dễ bị hổng kiến thức.\nCon anh/chị có đang gặp tình trạng này không?",
                "options": [  # LIST, không set
                    "Có, con đang gặp khó",
                    "Con học bình thường",
                    "Tôi chỉ đang tìm hiểu"
                ]
            }
        
        elif trigger == "scroll_70":
            # Trigger 3: Scroll 70%
            response = {
                "type": "quick_reply",
                "message": "ToánHọcHay giúp phụ huynh theo sát việc học Toán lớp 6 của con mà không cần kèm từng bài.\nAnh/chị muốn xem thử nội dung nào?",
                "options": [
                    "Cho con học thử miễn phí",
                    "Xem báo cáo tiến độ mẫu",
                    "Nhận tư vấn nhanh"
                ]
            }
        else:
            response = {"message": ""}
        
        return jsonify({
            "success": True,
            "UserId": UserId,
            "response": response
        }), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    port = 5001
    app.run(host='0.0.0.0', port=port, debug=True)