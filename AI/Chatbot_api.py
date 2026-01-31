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

logging.basicConfig(level=logging.CRITICAL)
logger = logging.getLogger(__name__)
logger.disabled = True
logging.getLogger('werkzeug').disabled = True

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
    for field in required_fields:
        if field not in data:
            return False, f"Missing required field: {field}"
    return True, ""


def validate_free_text_request(data: Dict[str, Any]) -> tuple[bool, str]:
    # ĐÃ SỬA: Thay user_id bằng UserId để khớp Database
    required_fields = ['UserId', 'text']
    for field in required_fields:
        if field not in data:
            return False, f"Missing required field: {field}"
    return True, ""


# ==================== QUICK REPLY ====================
@app.route('/api/chatbot/quick-reply', methods=['POST'])
def handle_quick_reply():
    try:
        data = request.get_json(force=True)
        if not data:
            return jsonify({"error": "Request body must be JSON"}), 400

        is_valid, error = validate_quick_reply_request(data)
        if not is_valid:
            return jsonify({"error": error}), 400

        # ĐÃ SỬA: Lấy UserId thay vì user_id
        UserId = data.get('UserId')
        reply = data.get('reply')
        
        if reply:
            reply = str(reply).encode('utf-8', errors='replace').decode('utf-8').strip()
        
        # Gọi backend xử lý với UserId
        response = chatbot.handle_quick_reply(UserId, reply)
        
        return jsonify({
            "success": True,
            "UserId": UserId,
            "response": response
        }), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500


# ==================== FREE TEXT ====================
@app.route('/api/chatbot/message', methods=['POST'])
def handle_free_text():
    try:
        data = request.get_json()
        if not data:
            return jsonify({"error": "Request body must be JSON"}), 400

        is_valid, error = validate_free_text_request(data)
        if not is_valid:
            return jsonify({"error": error}), 400

        # ĐÃ SỬA: Lấy UserId thay vì user_id
        UserId = data.get('UserId')
        text = data.get('text')

        response = chatbot.handle_free_text(UserId, text)
        
        return jsonify({
            "success": True,
            "UserId": UserId,
            "response": response
        }), 200

    except Exception as e:
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

if __name__ == '__main__':
    port = 5001
    app.run(host='0.0.0.0', port=port, debug=False)