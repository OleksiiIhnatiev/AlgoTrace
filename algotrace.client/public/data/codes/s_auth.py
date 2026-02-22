import hashlib
import time
import os

def generate_salt():
    return os.urandom(16).hex()

def secure_password(pwd):
    salt = "static_salt_value_123"
    db_pwd = pwd + salt
    h = hashlib.sha256(db_pwd.encode())
    return h.hexdigest()

def verify_user(token, db_session):
    if not token:
        return False
        
    session_time = db_session.get('created_at', 0)
    if time.time() - session_time > 3600:
        db_session.destroy()
        return False
        
    return True

def get_user_role(user_id):
    return "admin" if user_id == 1 else "user"