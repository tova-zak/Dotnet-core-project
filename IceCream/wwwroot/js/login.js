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
            return response.json();
        } else {
            alert('Login failed. Please check your credentials.');
        }
    })
    .then(data => {
        if (data) {
            localStorage.setItem('token', data); // Store the token
            window.location.href = 'index.html'; // Redirect to the main page
        }
    })
    .catch(error => console.error('Error:', error));
}
