from flask import Flask, request, jsonify, render_template_string
from flask_cors import CORS
import logging
import sys
import os
from typing import Dict, Any

# ==================== SETUP PATHS ====================
# Đảm bảo import được các module trong thư mục AI và AI_model
current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.append(current_dir)
if os.path.join(current_dir, 'AI_model') not in sys.path:
    sys.path.append(os.path.join(current_dir, 'AI_model'))

# ==================== CONFIGURATION ====================
# Ép log hiện ra ngay (Unbuffered)
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

logging.basicConfig(
    level=logging.DEBUG, # Chuyển sang DEBUG để xem chi tiết hơn
    format='%(asctime)s [%(levelname)s] %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout)
    ],
    force=True # Ghi đè cấu hình cũ nếu có
)
logger = logging.getLogger(__name__)

# Đảm bảo log của Flask (Werkzeug) cũng hiện ra
logging.getLogger('werkzeug').setLevel(logging.INFO)

# ==================== IMPORTS ====================
try:
    from Logic_chatbot import ChatbotLogicBackend, UserState
    from AI_model.Gemini_api import GeminiAIService
    logger_msg = "Successfully imported all AI modules"
except ImportError as e:
    logger_msg = f"Import error: {str(e)}"

logger.info(logger_msg)

app = Flask(__name__)
CORS(app, resources={r"/api/*": {"origins": "*"}})

app.config['JSON_AS_ASCII'] = False
app.config['JSON_SORT_KEYS'] = False

# Khởi tạo các service
chatbot = ChatbotLogicBackend()
gemini_ai = GeminiAIService()

@app.before_request
def log_request_info():
    print(f">>> [HTTP] Incoming {request.method} request to: {request.path}")
    logger.info(f"Incoming {request.method} request to: {request.path}")

# ==================== VALIDATION HELPERS ====================
def validate_fields(data, required_fields):
    for field in required_fields:
        if field not in data:
            return False, f"Missing field: {field}"
    return True, ""

# ==================== CHATBOT ROUTES ====================
@app.route('/api/chatbot/message', methods=['POST'])
def handle_chatbot_message():
    try:
        data = request.get_json(force=True)
        is_valid, error = validate_fields(data, ['UserId', 'text'])
        if not is_valid: return jsonify({"error": error}), 400

        UserId = data.get('UserId')
        text = data.get('text')
        logger.info(f"[Chatbot] Message from {UserId}: {text}")
        
        response = chatbot.handle_free_text(UserId, text)
        return jsonify({"success": True, "UserId": UserId, "response": response}), 200
    except Exception as e:
        logger.exception("Error in chatbot message")
        return jsonify({"error": str(e)}), 500

@app.route('/api/chatbot/quick-reply', methods=['POST'])
def handle_chatbot_quick_reply():
    try:
        data = request.get_json(force=True)
        is_valid, error = validate_fields(data, ['UserId', 'reply'])
        if not is_valid: return jsonify({"error": error}), 400

        UserId = data.get('UserId')
        reply = data.get('reply')
        logger.info(f"[Chatbot] QuickReply from {UserId}: {reply}")
        
        response = chatbot.handle_quick_reply(UserId, reply)
        return jsonify({"success": True, "UserId": UserId, "response": response}), 200
    except Exception as e:
        logger.exception("Error in chatbot quick-reply")
        return jsonify({"error": str(e)}), 500

@app.route('/api/chatbot/trigger', methods=['POST'])
def handle_chatbot_trigger():
    print(">>> [CHATBOT] TRIGGER REQUEST RECEIVED!")
    try:
        data = request.get_json(force=True)
        UserId = data.get('UserId')
        trigger = data.get('trigger')
        
        user = chatbot.get_user(UserId)
        logger.info(f"[Chatbot] Trigger '{trigger}' for user {UserId}")
        
        # Logic trigger từ Chatbot_api.py cũ
        response = {"message": ""}
        if trigger == "page_load":
            response = {
                "type": "quick_reply",
                "message": "Chào anh/chị,\nToánHọcHay là nền tảng học Toán dành riêng cho học sinh lớp 6, giúp con học dễ hiểu hơn trong giai đoạn chuyển cấp.\nAnh/chị muốn tìm hiểu nội dung nào cho con?",
                "options": ["Tư vấn cho con lớp 6", "Cho con học thử miễn phí", "Xem báo cáo tiến độ mẫu", "Học phí & lộ trình"]
            }
        elif trigger == "wait_15s" and not user.has_interacted:
            response = {
                "type": "quick_reply",
                "message": "Nhiều học sinh lớp 6 gặp khó khăn vì Toán khó hơn tiểu học và dễ bị hổng kiến thức.\nCon anh/chị có đang gặp tình trạng này không?",
                "options": ["Có, con đang gặp khó", "Con học bình thường", "Tôi chỉ đang tìm hiểu"]
            }
        elif trigger == "scroll_70":
            response = {
                "type": "quick_reply",
                "message": "ToánHọcHay giúp phụ huynh theo sát việc học Toán lớp 6 của con mà không cần kèm từng bài.\nAnh/chị muốn xem thử nội dung nào?",
                "options": ["Cho con học thử miễn phí", "Xem báo cáo tiến độ mẫu", "Nhận tư vấn nhanh"]
            }
        
        return jsonify({"success": True, "UserId": UserId, "response": response}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/chatbot/user/<UserId>', methods=['GET'])
def get_user_state(UserId: str):
    try:
        user = chatbot.get_user(UserId)
        return jsonify({
            "success": True, "UserId": user.id, "state": user.state.value,
            "has_interacted": user.has_interacted, "lead_submitted": user.lead_submitted
        }), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/api/chatbot/user/<UserId>/reset', methods=['POST'])
def reset_user(UserId: str):
    try:
        from Logic_chatbot import User
        chatbot.users[UserId] = User(UserId)
        return jsonify({"success": True, "message": f"User {UserId} reset"}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

# ==================== AI HINT & FEEDBACK ROUTES ====================
@app.route('/api/hint', methods=['POST'])
def generate_hint():
    print(">>> [DEBUG] generate_hint called!")
    try:
        data = request.get_json(force=True)
        required = ['question_text', 'question_type', 'difficulty_level', 'student_answer']
        is_valid, error = validate_fields(data, required)
        if not is_valid: return jsonify({"error": error}), 400

        logger.info(f"[AI] Generating hint for: {data.get('question_text')[:50]}...")
        result = gemini_ai.generate_hint(
            question_text=data.get('question_text'),
            question_type=data.get('question_type'),
            difficulty_level=data.get('difficulty_level'),
            student_answer=data.get('student_answer'),
            hint_level=data.get('hint_level', 1),
            options=data.get('options'),
            question_id=data.get('question_id'),
            question_image_url=data.get('question_image_url')
        )
        return jsonify(result), 200
    except Exception as e:
        logger.exception("Error in generating hint")
        return jsonify({"error": str(e)}), 500

@app.route('/api/hint/batch', methods=['POST'])
def generate_hints_batch():
    print(">>> [AI] BATCH HINT REQUEST RECEIVED!")
    data = request.get_json(force=True)
    if not data or 'hints' not in data:
        return jsonify({"error": "Missing hints array"}), 400

    results = []
    for idx, hint in enumerate(data['hints']):
        try:
            result = gemini_ai.generate_hint(
                question_text=hint.get('question_text'),
                question_type=hint.get('question_type'),
                difficulty_level=hint.get('difficulty_level'),
                student_answer=hint.get('student_answer'),
                hint_level=hint.get('hint_level', 1),
                options=hint.get('options'),
                question_id=hint.get('question_id'),
                question_image_url=hint.get('question_image_url')
            )
            result['index'] = idx
            results.append(result)
        except Exception as e:
            results.append({"index": idx, "error": str(e)})

    return jsonify({"results": results}), 200

@app.route('/api/feedback', methods=['POST'])
def generate_feedback():
    try:
        data = request.get_json(force=True)
        required = ['question_text', 'question_type', 'student_answer', 'correct_answer', 'is_correct']
        is_valid, error = validate_fields(data, required)
        if not is_valid: return jsonify({"error": error}), 400

        logger.info(f"[AI] Generating feedback for attempt: {data.get('attempt_id')}")
        result = gemini_ai.generate_feedback(
            question_text=data.get('question_text'),
            question_type=data.get('question_type'),
            student_answer=data.get('student_answer'),
            correct_answer=data.get('correct_answer'),
            is_correct=data.get('is_correct'),
            explanation=data.get('explanation'),
            options=data.get('options'),
            attempt_id=data.get('attempt_id')
        )
        return jsonify(result), 200
    except Exception as e:
        logger.exception("Error in generating feedback")
        return jsonify({"error": str(e)}), 500

@app.route('/api/feedback/batch', methods=['POST'])
def generate_feedback_batch():
    print(">>> [AI] BATCH FEEDBACK REQUEST RECEIVED!")
    data = request.get_json(force=True)
    if not data or 'feedbacks' not in data:
        return jsonify({"error": "Missing feedbacks array"}), 400

    results = []
    for idx, fb in enumerate(data['feedbacks']):
        try:
            result = gemini_ai.generate_feedback(
                question_text=fb.get('question_text'),
                question_type=fb.get('question_type'),
                student_answer=fb.get('student_answer'),
                correct_answer=fb.get('correct_answer'),
                is_correct=fb.get('is_correct'),
                explanation=fb.get('explanation'),
                options=fb.get('options'),
                attempt_id=fb.get('attempt_id')
            )
            result['index'] = idx
            results.append(result)
        except Exception as e:
            results.append({"index": idx, "error": str(e)})

    return jsonify({"results": results}), 200

# ==================== SYSTEM ROUTES ====================
@app.route('/api/health', methods=['GET'])
@app.route('/api/chatbot/health', methods=['GET'])
def health():
    return jsonify({"status": "healthy", "services": ["Chatbot", "AI Hint", "AI Feedback"]}), 200

@app.route('/api/status', methods=['GET'])
@app.route('/api/chatbot/status', methods=['GET'])
def status():
    return jsonify({
        "status": "running",
        "chatbot_users": len(chatbot.users),
        "ai_model": gemini_ai.model_name
    }), 200

@app.route('/', methods=['GET'])
@app.route('/docs', methods=['GET'])
@app.route('/api/docs', methods=['GET'])
@app.route('/api/chatbot/docs', methods=['GET'])
def documentation():
    swagger_html = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Unified AI Service Docs</title>
        <meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1">
        <style>
            body { font-family: sans-serif; padding: 20px; background: #f4f7f9; }
            .card { background: white; padding: 15px; border-radius: 8px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
            .method { font-weight: bold; color: #007bff; margin-right: 10px; }
            pre { background: #2d2d2d; color: #ccc; padding: 10px; border-radius: 5px; overflow-x: auto; }
            h1 { color: #333; } h2 { color: #555; border-bottom: 2px solid #ddd; padding-bottom: 5px; }
        </style>
    </head>
    <body>
        <h1>🤖 Unified AI Service (v1.0)</h1>
        
        <h2>💬 Chatbot Endpoints</h2>
        <div class="card">
            <p><span class="method">POST</span> /api/chatbot/message</p>
            <pre>{"UserId": "123", "text": "Chào bạn"}</pre>
        </div>
        <div class="card">
            <p><span class="method">POST</span> /api/chatbot/quick-reply</p>
            <pre>{"UserId": "123", "reply": "Tư vấn cho con lớp 6"}</pre>
        </div>
        <div class="card">
            <p><span class="method">POST</span> /api/chatbot/trigger</p>
            <pre>{"UserId": "123", "trigger": "page_load"}</pre>
        </div>

        <h2>🧠 Educational AI Endpoints</h2>
        <div class="card">
            <p><span class="method">POST</span> /api/hint</p>
            <pre>{"question_text": "2+2=?", "question_type": "MultipleChoice", "difficulty_level": "Easy", "student_answer": "5"}</pre>
        </div>
        <div class="card">
            <p><span class="method">POST</span> /api/hint/batch</p>
            <pre>{"hints": [{"question_text": "2+2=?", ...}]}</pre>
        </div>
        <div class="card">
            <p><span class="method">POST</span> /api/feedback</p>
            <pre>{"question_text": "2+2=?", "question_type": "MultipleChoice", "student_answer": "5", "correct_answer": "4", "is_correct": false}</pre>
        </div>
        <div class="card">
            <p><span class="method">POST</span> /api/feedback/batch</p>
            <pre>{"feedbacks": [{"question_text": "2+2=?", ...}]}</pre>
        </div>

        <h2>🛠 System</h2>
        <div class="card">
            <p><span class="method">GET</span> /api/health | /api/status</p>
        </div>
    </body>
    </html>
    """
    return render_template_string(swagger_html)

# ==================== MAIN ====================
if __name__ == '__main__':
    # Ưu tiên cổng 5001 như bạn đã cấu hình ở Backend C#
    port = 5001
    logger.info(f"Starting Unified AI Service on port {port}...")
    app.run(host='0.0.0.0', port=port, debug=True)
