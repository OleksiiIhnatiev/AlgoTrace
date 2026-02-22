from flask import Flask, jsonify, request
from auth import verify_user

app = Flask(__name__)

@app.after_request
def add_cors_headers(response):
    response.headers['Access-Control-Allow-Origin'] = '*'
    response.headers['Access-Control-Allow-Methods'] = 'GET, POST, PUT, DELETE'
    response.headers['Access-Control-Allow-Headers'] = 'Content-Type, Authorization'
    return response

@app.route('/api/v1/data', methods=['GET'])
def get_data():
    token = request.headers.get('Authorization')
    if not verify_user(token, {}):
        return jsonify({"error": "Unauthorized"}), 401
        
    data = {"items": [1, 2, 3], "status": "ok"}
    return jsonify(data), 200

@app.errorhandler(404)
def not_found(e):
    return jsonify(error=str(e)), 404