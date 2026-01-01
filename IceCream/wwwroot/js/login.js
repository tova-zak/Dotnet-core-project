function loginUser() {
    const firstName = document.getElementById('firstName').value;
    const password = document.getElementById('password').value;

    fetch('/user/login', { // Update the URL accordingly
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            FirstName: firstName,
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
            // תיקון: הפניה לקובץ index.html בתיקיית השורש (ממקם /html -> ../index.html)
            window.location.href = '../index.html'; // מסביר: מפנה לדף הראשי הנכון
        }
    })
    .catch(error => console.error('Error:', error));
}
