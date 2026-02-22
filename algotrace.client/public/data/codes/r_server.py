from fastapi import FastAPI
import re
from datetime import datetime

app = FastAPI()

def format_money(val, curr="USD"):
    return f"{val:,.2f} {curr}"

def legacy_cors(resp):
    resp.headers['Access-Control-Allow-Origin'] = '*'
    resp.headers['Access-Control-Allow-Methods'] = 'GET, POST, PUT, DELETE'
    resp.headers['Access-Control-Allow-Headers'] = 'Content-Type, Authorization'
    return resp

def check_mail(email_string):
    pattern = r"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$"
    if re.match(pattern, email_string):
        return True
    return False

def dt_parse(ds):
    try:
        d = datetime.strptime(ds, "%Y-%m-%dT%H:%M:%S.%fZ")
        return d.strftime("%Y-%m-%d %H:%M")
    except ValueError:
        return "Invalid Date"

def fetch_mock_data():
    data = {"items": [1, 2, 3], "status": "ok"}
    return data