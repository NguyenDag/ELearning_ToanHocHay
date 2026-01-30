from flask import Flask, request, jsonify, render_template_string
from flask_cors import CORS
import logging
import sys
import os
from typing import Dict, Any

# ==================== SETUP ====================
# Force UTF-8 encoding
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')

# Disable logging
logging.basicConfig(level=logging.CRITICAL)
logger = logging.getLogger(__name__)
logger.disabled = True

# Disable werkzeug logs
logging.getLogger('werkzeug').disabled = True

sys.path.append(os.path.join(os.path.dirname(__file__)))
from Logic_chatbot import ChatbotLogicBackend, UserState

app = Flask(__name__)
CORS(app)

# Force UTF-8
app.config['JSON_AS_ASCII'] = False
app.config['JSON_SORT_KEYS'] = False

# Initialize chatbot backend
chatbot = ChatbotLogicBackend()

# ==================== VALIDATION ====================
def validate_quick_reply_request(data: Dict[str, Any]) -> tuple[bool, str]:
    required_fields = ['user_id', 'reply']
    for field in required_fields:
        if field not in data:
            return False, f"Missing required field: {field}"
    return True, ""


def validate_free_text_request(data: Dict[str, Any]) -> tuple[bool, str]:
    required_fields = ['user_id', 'text']
    for field in required_fields:
        if field not in data:
            return False, f"Missing required field: {field}"
    return True, ""


# ==================== QUICK REPLY ====================
@app.route('/api/chatbot/quick-reply', methods=['POST'])
def handle_quick_reply():
    """
    Handle quick reply button click
    
    Request:
    {
        "user_id": "user_123",
        "reply": "Tư vấn cho con lớp 6"
    }
    """
    try:
        data = request.get_json(force=True)
        if not data:
            return jsonify({"error": "Request body must be JSON"}), 400

        is_valid, error = validate_quick_reply_request(data)
        if not is_valid:
            return jsonify({"error": error}), 400

        user_id = data.get('user_id')
        reply = data.get('reply')
        
        # Ensure UTF-8 encoding
        if reply:
            reply = str(reply).encode('utf-8', errors='replace').decode('utf-8')
            reply = reply.strip()
        
        # Debug log
        logger.info(f"Quick Reply received - user_id: {user_id}, reply: '{reply}'")

        response = chatbot.handle_quick_reply(user_id, reply)
        
        return jsonify({
            "success": True,
            "user_id": user_id,
            "response": response
        }), 200

    except Exception as e:
        logger.error(f"Error in quick reply: {str(e)}")
        return jsonify({"error": str(e)}), 500


# ==================== FREE TEXT ====================
@app.route('/api/chatbot/message', methods=['POST'])
def handle_free_text():
    """
    Handle free text message
    
    Request:
    {
        "user_id": "user_123",
        "text": "Tôi muốn cho con học thử"
    }
    """
    try:
        data = request.get_json()
        if not data:
            return jsonify({"error": "Request body must be JSON"}), 400

        is_valid, error = validate_free_text_request(data)
        if not is_valid:
            return jsonify({"error": error}), 400

        user_id = data.get('user_id')
        text = data.get('text')

        response = chatbot.handle_free_text(user_id, text)
        
        return jsonify({
            "success": True,
            "user_id": user_id,
            "response": response
        }), 200

    except Exception as e:
        logger.error(f"Error in free text: {str(e)}")
        return jsonify({"error": str(e)}), 500


# ==================== GET USER STATE ====================
@app.route('/api/chatbot/user/<user_id>', methods=['GET'])
def get_user_state(user_id: str):
    """
    Get user state and interaction history
    
    Response:
    {
        "user_id": "user_123",
        "state": "in_flow_tu_van",
        "has_interacted": true,
        "lead_submitted": false
    }
    """
    try:
        user = chatbot.get_user(user_id)
        
        return jsonify({
            "success": True,
            "user_id": user.id,
            "state": user.state.value,
            "has_interacted": user.has_interacted,
            "lead_submitted": user.lead_submitted
        }), 200

    except Exception as e:
        logger.error(f"Error getting user state: {str(e)}")
        return jsonify({"error": str(e)}), 500


# ==================== RESET USER ====================
@app.route('/api/chatbot/user/<user_id>/reset', methods=['POST'])
def reset_user(user_id: str):
    """
    Reset user state to NEW_VISITOR
    """
    try:
        from Logic_chatbot import User
        chatbot.users[user_id] = User(user_id)
        
        return jsonify({
            "success": True,
            "message": f"User {user_id} reset to NEW_VISITOR state"
        }), 200

    except Exception as e:
        logger.error(f"Error resetting user: {str(e)}")
        return jsonify({"error": str(e)}), 500


# ==================== HEALTH ====================
@app.route('/api/chatbot/health', methods=['GET'])
def health():
    """Health check"""
    return jsonify({"status": "healthy", "service": "Chatbot API"}), 200


@app.route('/api/chatbot/status', methods=['GET'])
def status():
    """Get API status"""
    return jsonify({
        "service": "Chatbot API",
        "status": "running",
        "active_users": len(chatbot.users)
    }), 200


# ==================== DOCUMENTATION ====================
@app.route('/api/chatbot/docs', methods=['GET'])
def swagger_ui():
    """Chatbot API Documentation"""
    swagger_html = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Chatbot API Docs</title>
        <meta charset="utf-8"/>
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <style>
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; }
            .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; text-align: center; }
            .header h1 { margin-bottom: 10px; }
            .header p { opacity: 0.9; }
            .container { max-width: 1000px; margin: 0 auto; padding: 20px; }
            .endpoint { background: white; margin-bottom: 20px; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
            .endpoint-header { background: #f9f9f9; padding: 15px 20px; border-left: 4px solid #667eea; display: flex; align-items: center; gap: 15px; }
            .method { font-weight: bold; padding: 4px 10px; border-radius: 4px; font-size: 12px; }
            .method.post { background: #4CAF50; color: white; }
            .method.get { background: #2196F3; color: white; }
            .path { font-family: 'Courier New', monospace; background: #e3f2fd; padding: 4px 8px; border-radius: 4px; flex: 1; }
            .endpoint-body { padding: 20px; }
            .endpoint-body h4 { margin-bottom: 10px; color: #333; }
            pre { background: #f5f5f5; padding: 12px; border-radius: 4px; overflow-x: auto; font-size: 12px; border: 1px solid #ddd; }
            .example-label { font-weight: bold; color: #666; margin-top: 15px; margin-bottom: 8px; }
            .footer { background: #f9f9f9; padding: 20px; text-align: center; color: #666; border-top: 1px solid #ddd; margin-top: 40px; }
        </style>
    </head>
    <body>
        <div class="header">
            <h1>🤖 Chatbot API Documentation</h1>
            <p>ToánHọcHay Chatbot Backend API v1.0</p>
        </div>
        
        <div class="container">
            
            <!-- Health Check -->
            <div class="endpoint">
                <div class="endpoint-header">
                    <span class="method get">GET</span>
                    <span class="path">/api/chatbot/health</span>
                </div>
                <div class="endpoint-body">
                    <h4>Health Check</h4>
                    <p>Check if server is running</p>
                    <div class="example-label">Response (200):</div>
                    <pre>{"status": "healthy", "service": "Chatbot API"}</pre>
                </div>
            </div>

            <!-- Status -->
            <div class="endpoint">
                <div class="endpoint-header">
                    <span class="method get">GET</span>
                    <span class="path">/api/chatbot/status</span>
                </div>
                <div class="endpoint-body">
                    <h4>Get API Status</h4>
                    <p>Get current API status and active users count</p>
                    <div class="example-label">Response (200):</div>
                    <pre>{"service": "Chatbot API", "status": "running", "active_users": 5}</pre>
                </div>
            </div>

            <!-- Quick Reply -->
            <div class="endpoint">
                <div class="endpoint-header">
                    <span class="method post">POST</span>
                    <span class="path">/api/chatbot/quick-reply</span>
                </div>
                <div class="endpoint-body">
                    <h4>Handle Quick Reply</h4>
                    <p>Process user's quick reply button click</p>
                    <div class="example-label">Request (JSON):</div>
                    <pre>{
  "user_id": "user_123",
  "reply": "Tư vấn cho con lớp 6"
}</pre>
                    <div class="example-label">Response (200):</div>
                    <pre>{
  "success": true,
  "user_id": "user_123",
  "response": {
    "type": "quick_reply",
    "message": "...",
    "options": ["option1", "option2"]
  }
}</pre>
                </div>
            </div>

            <!-- Free Text Message -->
            <div class="endpoint">
                <div class="endpoint-header">
                    <span class="method post">POST</span>
                    <span class="path">/api/chatbot/message</span>
                </div>
                <div class="endpoint-body">
                    <h4>Handle Free Text Message</h4>
                    <p>Process user's free text message (returns fallback response)</p>
                    <div class="example-label">Request (JSON):</div>
                    <pre>{
  "user_id": "user_123",
  "text": "Tôi muốn cho con học thử"
}</pre>
                    <div class="example-label">Response (200):</div>
                    <pre>{
  "success": true,
  "user_id": "user_123",
  "response": {
    "type": "quick_reply",
    "message": "Mình chưa hiểu câu hỏi này...",
    "options": ["Tư vấn cho con lớp 6", "Học thử miễn phí"]
  }
}</pre>
                </div>
            </div>

            <!-- Get User State -->
            <div class="endpoint">
                <div class="endpoint-header">
                    <span class="method get">GET</span>
                    <span class="path">/api/chatbot/user/:user_id</span>
                </div>
                <div class="endpoint-body">
                    <h4>Get User State</h4>
                    <p>Get current state and interaction history of a user</p>
                    <div class="example-label">Response (200):</div>
                    <pre>{
  "success": true,
  "user_id": "user_123",
  "state": "in_flow_tu_van",
  "has_interacted": true,
  "lead_submitted": false
}</pre>
                    <div class="example-label">User States:</div>
                    <pre>- new_visitor
- waiting_first_choice
- in_flow_tu_van
- in_flow_trial_parent
- in_flow_trial_student
- lead_collected
- handover_to_human
- idle</pre>
                </div>
            </div>

            <!-- Reset User -->
            <div class="endpoint">
                <div class="endpoint-header">
                    <span class="method post">POST</span>
                    <span class="path">/api/chatbot/user/:user_id/reset</span>
                </div>
                <div class="endpoint-body">
                    <h4>Reset User State</h4>
                    <p>Reset user state back to NEW_VISITOR (clear interaction history)</p>
                    <div class="example-label">Response (200):</div>
                    <pre>{"success": true, "message": "User user_123 reset to NEW_VISITOR state"}</pre>
                </div>
            </div>

        </div>

        <div class="footer">
            <p>🎓 ToánHọcHay Chatbot API | Version 1.0</p>
            <p style="margin-top: 10px; font-size: 12px;">Base URL: http://localhost:5000</p>
        </div>
    </body>
    </html>
    """
    return render_template_string(swagger_html)


# ==================== ROOT ====================
@app.route('/', methods=['GET'])
def root():
    """Redirect to docs"""
    return jsonify({
        "message": "Welcome to Chatbot API",
        "docs": "/api/chatbot/docs",
        "health": "/api/chatbot/health"
    }), 200


# ==================== MAIN ====================
if __name__ == '__main__':
    port = int(os.getenv('FLASK_PORT', 5001))  # Default port 5001 for Chatbot API
    print(f"\n{'='*60}")
    print(f"🚀 Chatbot API Server Starting")
    print(f"{'='*60}")
    print(f"📍 URL: http://localhost:{port}")
    print(f"📚 Docs: http://localhost:{port}/api/chatbot/docs")
    print(f"❤️  Health: http://localhost:{port}/api/chatbot/health")
    print(f"{'='*60}\n")
    app.run(host='0.0.0.0', port=port, debug=False)
