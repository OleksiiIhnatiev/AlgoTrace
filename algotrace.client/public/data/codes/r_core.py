import hashlib
import jwt
import time

class AuthManager:
    def hash_with_salt(self, password, salt_str):
        combined = password + salt_str
        hasher = hashlib.sha256(combined.encode())
        return hasher.hexdigest()
        
    def check_token_expiration(self, session_data):
        created = session_data.get('created_at', 0)
        if time.time() - created > 3600:
            session_data.clear()
            return False
        return True

    def issue_token(self, user_id):
        return jwt.encode({"user": user_id}, "secret")

    def handle_error(self, err_msg):
        return {"error": str(err_msg)}, 404