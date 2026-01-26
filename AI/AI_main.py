<pre>{"status": "healthy", "message": "..."}</pre>
            </div>

            <div class="endpoint">
                <div><span class="method">GET</span> <span class="path">/api/status</span></div>
                <p>Get API status and available endpoints</p>
            </div>

            <div class="endpoint">
                <div><span class="method">POST</span> <span class="path">/api/hint</span></div>
                <p>Generate AI hint for a question</p>
                <strong>Required fields:</strong> question_text, question_type, difficulty_level, student_answer
                <pre>POST http://127.0.0.1:5000/api/hint
Content-Type: application/json

{
  "question_text": "What is 2 + 2?",
  "question_type": "MultipleChoice",
  "difficulty_level": "Easy",
  "student_answer": "5",
  "hint_level": 1
}</pre>
            </div>

            <div class="endpoint">
                <div><span class="method">POST</span> <span class="path">/api/hint/batch</span></div>
                <p>Generate multiple hints at once</p>
                <strong>Request body:</strong> Object with "hints" array
            </div>

            <div class="endpoint">
                <div><span class="method">POST</span> <span class="path">/api/feedback</span></div>
                <p>Generate AI feedback for a question</p>
                <strong>Required fields:</strong> question_text, question_type, student_answer, correct_answer, is_correct
            </div>

            <div class="endpoint">
                <div><span class="method">POST</span> <span class="path">/api/feedback/batch</span></div>
                <p>Generate multiple feedbacks at once</p>
                <strong>Request body:</strong> Object with "feedbacks" array
            </div>

            <div class="links">
                <a href="/api/health"> Health Check</a>
                <a href="/api/status"> Status</a>
                <a href="https://ai.google.dev/"> Gemini API Docs</a>
            </div>
        </div>
    </body>
    </html>
    """
    return render_template_string(swagger_html)


# ==================== MAIN ====================
if __name__ == '__main__':
    port = os.getenv('FLASK_PORT', 5000)
    debug = os.getenv('FLASK_DEBUG', False)
    
    logger.info(f"Starting Gemini AI Flask server on port {port}")
    app.run(
        host='0.0.0.0',
        port=int(port),
        debug=debug
    )