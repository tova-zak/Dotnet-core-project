// register.js
// הסבר בעברית: סקריפט זה שולח בקשה לשרת ליצירת משתמש/חנות חדש.
// השדות הנשלחים: ShopName, Password, Email, Address

function registerUser() {
    const shopName = document.getElementById('shopName').value.trim();
    const password = document.getElementById('password').value;
    const email = document.getElementById('email').value.trim();
    const address = document.getElementById('address').value.trim();

    const user = {
        ShopName: shopName,
        Password: password,
        Email: email,
        Address: address
    };

    // CORRECTED: send registration to the anonymous "register" endpoint
    fetch('/user/register', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(user)
    })
    .then(response => {
        if (!response.ok) {
            return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        }
        return response.json();
    })
    .then(createdUser => {
        // הרשמה הצליחה, מבצע התחברות אוטומטית
        fetch('/user/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                ShopName: shopName,
                Password: password
            })
        })
        .then(response => {
            if (!response.ok) {
                alert('ההרשמה הצליחה, אך ההתחברות נכשלה. נא להתחבר ידנית.');
                window.location.href = './login.html';
                throw new Error('Login failed');
            }
            return response.text();
        })
        .then(token => {
            if (token) {
                localStorage.setItem('token', token);
                window.location.href = './myicecreams.html';
            }
        });
    })
    .catch(err => {
        console.error('Registration failed', err);
        alert('Registration failed: ' + (err.message || 'error'));
    });
}
