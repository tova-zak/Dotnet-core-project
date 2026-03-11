function loginUser() {
    const shopName = document.getElementById('shopName').value;
    const password = document.getElementById('password').value;

    fetch('/user/login', { // Update the URL accordingly
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
        if (response.ok) {
            // השרת מחזיר מחרוזת עם הטוקן, לכן נקרא את התוכן כ-text ולא כ-json
            return response.text(); // מסביר: קורא את גוף התשובה כמחרוזת
        } else {
            alert('Login failed. Please check your credentials.');
            throw new Error('Login failed');
        }
    })
    .then(token => {
        if (token) {
            // שומר את הטוקן ב-localStorage
            localStorage.setItem('token', token); // מסביר: מאחסן את הטוקן
            // הפניה לעמוד הראשי של האתר
            window.location.href = '../index.html';
        }
    })
    .catch(error => console.error('Error:', error));
}
