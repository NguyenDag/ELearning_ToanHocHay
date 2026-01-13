from flask import Flask, request, jsonify
from flask_cors import CORS
import logging
import sys
import os
from typing import Dict, Any

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Add AI_model to path
sys.path.append(os.path.join(os.path.dirname(__file__), 'AI_model'))

from AI_model.Gemini_api import GeminiAIService

# Initialize Flask app
app = Flask(__name__)
CORS(app)  # Enable CORS for cross-origin requests

# Initialize Gemini service
gemini_service = GeminiAIService()

# ==================== UTILITY FUNCTIONS ====================
def validate_hint_request(data: Dict[str, Any]) -> tuple[bool, str]:
    """Validate hint request data"""
    required_fields = ['question_text', 'question_type', 'difficulty_level', 'student_answer']
    
    for field in required_fields:
        if field not in data:
            return False, f"Missing required field: {field}"
    
    if not isinstance(data.get('student_answer'), str):
        return False, "student_answer must be a string"
    
    return True, ""


def validate_feedback_request(data: Dict[str, Any]) -> tuple[bool, str]:
    """Validate feedback request data"""
    required_fields = ['question_text', 'question_type', 'student_answer', 'correct_answer', 'is_correct']
    
    for field in required_fields:
        if field not in data:
            return False, f"Missing required field: {field}"
    
    if not isinstance(data.get('is_correct'), bool):
        return False, "is_correct must be a boolean"
    
    return True, ""


# ==================== HINT ENDPOINTS ====================
@app.route('/api/hint', methods=['POST'])
def generate_hint():
    """
    Generate AI hint for a question
    
    Request body:
    {
        "question_text": "string",
        "question_type": "string (MultipleChoice, TrueFalse, FillBlank, Essay)",
        "difficulty_level": "string (Easy, Medium, Hard)",
        "student_answer": "string",
        "hint_level": "int (1-3, default: 1)",
        "question_id": "int (optional)",
        "question_image_url": "string (optional)",
        "options": "array (optional)"
    }
    
    Response:
    {
        "hint_text": "string",
        "hint_level": "int",
        "question_id": "int",
        "status": "success|error"
    }
    """
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({
                "error": "Request body must be JSON",
                "status": "error"
            }), 400
        
        # Validate required fields
        is_valid, error_msg = validate_hint_request(data)
        if not is_valid:
            return jsonify({
                "error": error_msg,
                "status": "error"
            }), 400
        
        # Extract parameters
        question_text = data.get('question_text')
        question_type = data.get('question_type')
        difficulty_level = data.get('difficulty_level')
        student_answer = data.get('student_answer')
        hint_level = data.get('hint_level', 1)
        question_id = data.get('question_id')
        question_image_url = data.get('question_image_url')
        options = data.get('options', [])
        
        # Validate hint_level
        if not isinstance(hint_level, int) or hint_level < 1 or hint_level > 3:
            return jsonify({
                "error": "hint_level must be an integer between 1 and 3",
                "status": "error"
            }), 400
        
        logger.info(f"Generating hint for question {question_id}, level {hint_level}")
        
        # Generate hint
        result = gemini_service.generate_hint(
            question_text=question_text,
            question_type=question_type,
            difficulty_level=difficulty_level,
            student_answer=student_answer,
            hint_level=hint_level,
            options=options if options else None,
            question_id=question_id,
            question_image_url=question_image_url
        )
        
        logger.info(f"Hint generated successfully: {result.get('Status')}")
        
        return jsonify(result), 200 if result.get('Status') == 'success' else 500
    
    except Exception as e:
        logger.error(f"Error in /api/hint: {str(e)}")
        return jsonify({
            "error": f"Server error: {str(e)}",
            "status": "error"
        }), 500


@app.route('/api/hint/batch', methods=['POST'])
def generate_hints_batch():
    """
    Generate multiple hints at once (batch processing)
    
    Request body:
    {
        "hints": [
            {
                "question_text": "...",
                "question_type": "...",
                ...
            },
            ...
        ]
    }
    
    Response:
    {
        "results": [
            {...},
            ...
        ],
        "status": "success|partial|error"
    }
    """
    try:
        data = request.get_json()
        
        if not data or 'hints' not in data:
            return jsonify({
                "error": "Request must contain 'hints' array",
                "status": "error"
            }), 400
        
        hints = data.get('hints', [])
        
        if not isinstance(hints, list) or len(hints) == 0:
            return jsonify({
                "error": "hints must be a non-empty array",
                "status": "error"
            }), 400
        
        results = []
        errors = 0
        
        for idx, hint_req in enumerate(hints):
            is_valid, error_msg = validate_hint_request(hint_req)
            
            if not is_valid:
                results.append({
                    "index": idx,
                    "error": error_msg,
                    "status": "error"
                })
                errors += 1
                continue
            
            try:
                result = gemini_service.generate_hint(
                    question_text=hint_req.get('question_text'),
                    question_type=hint_req.get('question_type'),
                    difficulty_level=hint_req.get('difficulty_level'),
                    student_answer=hint_req.get('student_answer'),
                    hint_level=hint_req.get('hint_level', 1),
                    options=hint_req.get('options'),
                    question_id=hint_req.get('question_id'),
                    question_image_url=hint_req.get('question_image_url')
                )
                
                result['index'] = idx
                results.append(result)
                
                if result.get('Status') != 'success':
                    errors += 1
            
            except Exception as e:
                results.append({
                    "index": idx,
                    "error": str(e),
                    "status": "error"
                })
                errors += 1
        
        status = "success" if errors == 0 else ("partial" if errors < len(hints) else "error")
        
        return jsonify({
            "results": results,
            "total": len(hints),
            "successful": len(hints) - errors,
            "failed": errors,
            "status": status
        }), 200
    
    except Exception as e:
        logger.error(f"Error in /api/hint/batch: {str(e)}")
        return jsonify({
            "error": f"Server error: {str(e)}",
            "status": "error"
        }), 500


# ==================== FEEDBACK ENDPOINTS ====================
@app.route('/api/feedback', methods=['POST'])
def generate_feedback():
    """
    Generate AI feedback for a question
    
    Request body:
    {
        "question_text": "string",
        "question_type": "string",
        "student_answer": "string",
        "correct_answer": "string",
        "is_correct": "boolean",
        "explanation": "string (optional)",
        "attempt_id": "int (optional)",
        "question_id": "int (optional)",
        "question_image_url": "string (optional)",
        "options": "array (optional)"
    }
    
    Response:
    {
        "full_solution": "string",
        "mistake_analysis": "string",
        "improvement_advice": "string",
        "attempt_id": "int",
        "status": "success|error"
    }
    """
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({
                "error": "Request body must be JSON",
                "status": "error"
            }), 400
        
        # Validate required fields
        is_valid, error_msg = validate_feedback_request(data)
        if not is_valid:
            return jsonify({
                "error": error_msg,
                "status": "error"
            }), 400
        
        # Extract parameters
        question_text = data.get('question_text')
        question_type = data.get('question_type')
        student_answer = data.get('student_answer')
        correct_answer = data.get('correct_answer')
        is_correct = data.get('is_correct')
        explanation = data.get('explanation')
        attempt_id = data.get('attempt_id')
        question_id = data.get('question_id')
        question_image_url = data.get('question_image_url')
        options = data.get('options', [])
        
        logger.info(f"Generating feedback for attempt {attempt_id}, is_correct: {is_correct}")
        
        # Generate feedback
        result = gemini_service.generate_feedback(
            question_text=question_text,
            question_type=question_type,
            student_answer=student_answer,
            correct_answer=correct_answer,
            is_correct=is_correct,
            explanation=explanation,
            options=options if options else None,
            attempt_id=attempt_id,
            question_image_url=question_image_url
        )
        
        logger.info(f"Feedback generated successfully: {result.get('Status')}")
        
        return jsonify(result), 200 if result.get('Status') == 'success' else 500
    
    except Exception as e:
        logger.error(f"Error in /api/feedback: {str(e)}")
        return jsonify({
            "error": f"Server error: {str(e)}",
            "status": "error"
        }), 500


@app.route('/api/feedback/batch', methods=['POST'])
def generate_feedbacks_batch():
    """
    Generate multiple feedbacks at once (batch processing)
    
    Request body:
    {
        "feedbacks": [
            {
                "question_text": "...",
                "question_type": "...",
                ...
            },
            ...
        ]
    }
    
    Response:
    {
        "results": [
            {...},
            ...
        ],
        "status": "success|partial|error"
    }
    """
    try:
        data = request.get_json()
        
        if not data or 'feedbacks' not in data:
            return jsonify({
                "error": "Request must contain 'feedbacks' array",
                "status": "error"
            }), 400
        
        feedbacks = data.get('feedbacks', [])
        
        if not isinstance(feedbacks, list) or len(feedbacks) == 0:
            return jsonify({
                "error": "feedbacks must be a non-empty array",
                "status": "error"
            }), 400
        
        results = []
        errors = 0
        
        for idx, feedback_req in enumerate(feedbacks):
            is_valid, error_msg = validate_feedback_request(feedback_req)
            
            if not is_valid:
                results.append({
                    "index": idx,
                    "error": error_msg,
                    "status": "error"
                })
                errors += 1
                continue
            
            try:
                result = gemini_service.generate_feedback(
                    question_text=feedback_req.get('question_text'),
                    question_type=feedback_req.get('question_type'),
                    student_answer=feedback_req.get('student_answer'),
                    correct_answer=feedback_req.get('correct_answer'),
                    is_correct=feedback_req.get('is_correct'),
                    explanation=feedback_req.get('explanation'),
                    options=feedback_req.get('options'),
                    attempt_id=feedback_req.get('attempt_id'),
                    question_image_url=feedback_req.get('question_image_url')
                )
                
                result['index'] = idx
                results.append(result)
                
                if result.get('Status') != 'success':
                    errors += 1
            
            except Exception as e:
                results.append({
                    "index": idx,
                    "error": str(e),
                    "status": "error"
                })
                errors += 1
        
        status = "success" if errors == 0 else ("partial" if errors < len(feedbacks) else "error")
        
        return jsonify({
            "results": results,
            "total": len(feedbacks),
            "successful": len(feedbacks) - errors,
            "failed": errors,
            "status": status
        }), 200
    
    except Exception as e:
        logger.error(f"Error in /api/feedback/batch: {str(e)}")
        return jsonify({
            "error": f"Server error: {str(e)}",
            "status": "error"
        }), 500


# ==================== HEALTH CHECK ====================
@app.route('/api/health', methods=['GET'])
def health_check():
    """
    Health check endpoint
    """
    return jsonify({
        "status": "healthy",
        "message": "Gemini AI API server is running"
    }), 200


@app.route('/api/status', methods=['GET'])
def status():
    """
    Status endpoint - returns server info
    """
    return jsonify({
        "service": "Gemini AI Educational API",
        "version": "1.0",
        "endpoints": {
            "hint": "/api/hint (POST)",
            "hint_batch": "/api/hint/batch (POST)",
            "feedback": "/api/feedback (POST)",
            "feedback_batch": "/api/feedback/batch (POST)",
            "health": "/api/health (GET)",
            "status": "/api/status (GET)"
        },
        "status": "operational"
    }), 200


# ==================== ERROR HANDLERS ====================
@app.errorhandler(404)
def not_found(error):
    """Handle 404 errors"""
    return jsonify({
        "error": "Endpoint not found",
        "status": "error"
    }), 404


@app.errorhandler(405)
def method_not_allowed(error):
    """Handle 405 errors"""
    return jsonify({
        "error": "Method not allowed",
        "status": "error"
    }), 405


@app.errorhandler(500)
def internal_error(error):
    """Handle 500 errors"""
    logger.error(f"Internal server error: {str(error)}")
    return jsonify({
        "error": "Internal server error",
        "status": "error"
    }), 500


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
