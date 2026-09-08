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
            return response.text(); 
        } else {
            alert('Login failed. Please check your credentials.');
            throw new Error('Login failed');
        }
    })
    .then(token => {
        if (token) {
            localStorage.setItem('token', token); 
            window.location.href = '../html/myicecreams.html';
        }
        
    })
    .catch(error => console.error('Error:', error));
}
