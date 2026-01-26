import os
import logging
from flask import Flask, jsonify, request, render_template_string

app = Flask(__name__)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# ==================== HEALTH ====================
@app.route("/api/health", methods=["GET"])
def health():
    return jsonify({
        "status": "healthy",
        "message": "API is running"
    })


# ==================== STATUS ====================
@app.route("/api/status", methods=["GET"])
def status():
    return jsonify({
        "service": "Gemini AI Hint API",
        "endpoints": [
            "/api/health",
            "/api/status",
            "/api/hint",
            "/api/hint/batch",
            "/api/feedback",
            "/api/feedback/batch"
        ]
    })


# ==================== SWAGGER UI ====================
@app.route("/", methods=["GET"])
def swagger_ui():
    swagger_html = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Gemini AI API</title>
        <style>
            body { font-family: Arial; margin: 40px; }
            .endpoint { margin-bottom: 20px; }
            .method { font-weight: bold; color: green; }
            .path { color: blue; }
            pre { background: #f4f4f4; padding: 10px; }
        </style>
    </head>
    <body>
        <h1>Gemini AI Flask API</h1>

        <pre>{"status": "healthy", "message": "..."}</pre>

        <div class="endpoint">
            <div><span class="method">GET</span> <span class="path">/api/status</span></div>
            <p>Get API status and available endpoints</p>
        </div>

        <div class="endpoint">
            <div><span class="method">POST</span> <span class="path">/api/hint</span></div>
            <p>Generate AI hint for a question</p>
            <strong>Required fields:</strong>
            question_text, question_type, difficulty_level, student_answer
            <pre>
POST http://127.0.0.1:5000/api/hint
Content-Type: application/json

{
  "question_text": "What is 2 + 2?",
  "question_type": "MultipleChoice",
  "difficulty_level": "Easy",
  "student_answer": "5",
  "hint_level": 1
}
            </pre>
        </div>

        <div class="endpoint">
            <div><span class="method">POST</span> <span class="path">/api/hint/batch</span></div>
            <p>Generate multiple hints at once</p>
        </div>

        <div class="endpoint">
            <div><span class="method">POST</span> <span class="path">/api/feedback</span></div>
            <p>Generate AI feedback for a question</p>
        </div>

        <div class="endpoint">
            <div><span class="method">POST</span> <span class="path">/api/feedback/batch</span></div>
            <p>Generate multiple feedbacks at once</p>
        </div>

        <hr>
        <a href="/api/health">Health</a> |
        <a href="/api/status">Status</a> |
        <a href="https://ai.google.dev/">Gemini API Docs</a>

    </body>
    </html>
    """
    return render_template_string(swagger_html)


# ==================== MAIN ====================
if __name__ == "__main__":
    port = int(os.getenv("FLASK_PORT", 5000))
    debug = os.getenv("FLASK_DEBUG", "false").lower() == "true"

    logger.info(f"Starting Gemini AI Flask server on port {port}")
    app.run(host="0.0.0.0", port=port, debug=debug)
