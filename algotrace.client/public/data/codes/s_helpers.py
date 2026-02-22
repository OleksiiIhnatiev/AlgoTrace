import re
from datetime import datetime

def format_currency(amount, currency="USD"):
    return f"{amount:,.2f} {currency}"

def validate_email_strict(email):
    pattern = r"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$"
    if re.match(pattern, email):
        return True
    return False

def parse_iso_date(date_str):
    try:
        dt = datetime.strptime(date_str, "%Y-%m-%dT%H:%M:%S.%fZ")
        return dt.strftime("%Y-%m-%d %H:%M")
    except ValueError:
        return "Invalid Date"

def slugify(text):
    text = text.lower()
    return re.sub(r'[\W_]+', '-', text)
    import re
from datetime import datetime

def format_currency(amount, currency="USD"):
    return f"{amount:,.2f} {currency}"

def validate_email_strict(email):
    pattern = r"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$"
    if re.match(pattern, email):
        return True
    return False

def parse_iso_date(date_str):
    try:
        dt = datetime.strptime(date_str, "%Y-%m-%dT%H:%M:%S.%fZ")
        return dt.strftime("%Y-%m-%d %H:%M")
    except ValueError:
        return "Invalid Date"

def slugify(text):
    text = text.lower()
    return re.sub(r'[\W_]+', '-', text)
    import re
from datetime import datetime

def format_currency(amount, currency="USD"):
    return f"{amount:,.2f} {currency}"

def validate_email_strict(email):
    pattern = r"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$"
    if re.match(pattern, email):
        return True
    return False

def parse_iso_date(date_str):
    try:
        dt = datetime.strptime(date_str, "%Y-%m-%dT%H:%M:%S.%fZ")
        return dt.strftime("%Y-%m-%d %H:%M")
    except ValueError:
        return "Invalid Date"

def slugify(text):
    text = text.lower()
    return re.sub(r'[\W_]+', '-', text)
    import re
from datetime import datetime

def format_currency(amount, currency="USD"):
    return f"{amount:,.2f} {currency}"

def validate_email_strict(email):
    pattern = r"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$"
    if re.match(pattern, email):
        return True
    return False

def parse_iso_date(date_str):
    try:
        dt = datetime.strptime(date_str, "%Y-%m-%dT%H:%M:%S.%fZ")
        return dt.strftime("%Y-%m-%d %H:%M")
    except ValueError:
        return "Invalid Date"

def slugify(text):
    text = text.lower()
    return re.sub(r'[\W_]+', '-', text)