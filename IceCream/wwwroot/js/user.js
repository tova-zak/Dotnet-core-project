const uri = '/user';
let users = [];

function parseJwt (token) {
    if (!token) return null;
    try {
        const payload = token.split('.')[1];
        if (!payload) return null;
        return JSON.parse(decodeURIComponent(Array.prototype.map.call(atob(payload.replace(/-/g, '+').replace(/_/g, '/')), function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join('')));
    } catch (e) {
        return null;
    }
}

// helper: return userId from token (string) or null
function getCurrentUserId() {
    const token = localStorage.getItem('token');
    const claims = parseJwt(token);
    return claims ? (claims.userId || claims.userId?.toString()) : null;
}

function isAdmin() {
    const token = localStorage.getItem('token');
    const claims = parseJwt(token);
    return claims && (claims.type === 'Admin');
}

function getUsers() {
    const token = localStorage.getItem('token'); // מסביר: לוקח את הטוקן מה-localStorage

    if (isAdmin()) {
        // Admin: fetch all users
        fetch(uri, {
                headers: {
                    'Accept': 'application/json',
                    ...(token ? { 'Authorization': `Bearer ${token}` } : {})
                }
            })
            .then(response => {
                if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
                return response.json();
            })
            .then(data => _displayUsers(data))
            .catch(error => console.error('Unable to get items.', error));
    } else {
        // Non-admin: fetch only own user by id
        const myId = getCurrentUserId();
        if (!myId) {
            console.error('No authenticated user found.');
            return;
        }

        fetch(`${uri}/${myId}`, {
                headers: {
                    'Accept': 'application/json',
                    ...(token ? { 'Authorization': `Bearer ${token}` } : {})
                }
            })
            .then(response => {
                if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
                return response.json();
            })
            .then(user => {
                // wrap single user into array so _displayUsers can reuse same renderer
                _displayUsers([user]);
            })
            .catch(error => console.error('Unable to get user.', error));
    }
}

function addUser() {
    const addFirst = document.getElementById('add-firstName');
    const addLast = document.getElementById('add-lastName');

    // שים לב: עמוד הניהול משתמש בשמות ישנים של שדות; כאן אנו ממפים אותם ל-ShopName ו-Email
    const user = {
        ShopName: addFirst.value.trim(),
        Email: addLast.value.trim()
    };

    const token = localStorage.getItem('token'); // מסביר: מוסיף טוקן לכותרות אם קיים

    fetch(uri, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            },
            body: JSON.stringify(user)
        })
        .then(response => {
            if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
            const ct = response.headers.get('content-type') || '';
            return ct.includes('application/json') ? response.json() : Promise.resolve();
        })
        .then(() => {
            getUsers();
            addFirst.value = '';
            addLast.value = '';
        })
        .catch(error => console.error('Unable to add item.', error));
}

function deleteUser(id) {
    const token = localStorage.getItem('token'); // מסביר: מוסיף טוקן למחיקת משתמש
    fetch(`${uri}/${id}`, {
            method: 'DELETE',
            headers: {
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            }
        })
        .then(response => {
            if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
            return response.text();
        })
        .then(() => getUsers())
        .catch(error => console.error('Unable to delete item.', error));
}

function displayEditForm(id) {
    const user = users.find(user => user.id === id);

    document.getElementById('edit-firstName').value = user.ShopName || user.firstName || '';
    document.getElementById('edit-id').value = user.id;
    document.getElementById('edit-lastName').value = user.Email || user.lastName || '';
    document.getElementById('edit-address').value = user.Address || '';
    document.getElementById('pwd-id').value = user.id;
    document.getElementById('editForm').style.display = 'block';
}

function updateUser() {
    const userId = document.getElementById('edit-id').value;
    const user = {
        id: parseInt(userId, 10),
        Email: document.getElementById('edit-lastName').value.trim(),
        ShopName: document.getElementById('edit-firstName').value.trim(),
        Address: document.getElementById('edit-address').value.trim(),
        Password: "" // ריק -> UserService.Update ישמור את הסיסמה הקיימת
    };

    const token = localStorage.getItem('token'); // מסביר: מוסיף טוקן לעדכון

    fetch(`${uri}/${userId}`, {
            method: 'PUT',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            },
            body: JSON.stringify(user)
        })
        .then(response => {
            if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
            return response.text();
        })
        .then(() => getUsers())
        .catch(error => console.error('Unable to update item.', error));

    closeInput();

    return false;
}

function changePassword() {
    const id = document.getElementById('pwd-id').value || getCurrentUserId();
    const current = document.getElementById('current-password').value;
    const neu = document.getElementById('new-password').value;
    const token = localStorage.getItem('token');

    fetch(`${uri}/${id}/changepassword`, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify({ CurrentPassword: current, NewPassword: neu })
    })
    .then(response => {
        if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        const ct = response.headers.get('content-type') || '';
        return ct.includes('application/json') || ct.includes('text/plain') ? response.text() : Promise.resolve();
    })
    .then(tokenString => {
        // אם השרת החזיר טוקן — נשמור אותו ב-localStorage
        if (tokenString && tokenString.length > 10) {
            localStorage.setItem('token', tokenString);
        }
        // ננקה שדות הסיסמה
        document.getElementById('current-password').value = '';
        document.getElementById('new-password').value = '';
    })
    .catch(error => console.error('Unable to change password.', error));

    return false;
}

function closeInput() {
    document.getElementById('editForm').style.display = 'none';
}

function _displayCount(userCount) {
    const name = (userCount === 1) ? 'user' : 'user kinds';

    document.getElementById('counter').innerText = `${userCount} ${name}`;
}

// new: show profile for current user (or admin viewing a specific user id)
function showProfile(userId) {
    const token = localStorage.getItem('token');
    const id = userId || getCurrentUserId();
    if (!id) { window.location.href = 'login.html'; return; }

    fetch(`${uri}/${id}`, {
        headers: {
            'Accept': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        }
    })
    .then(response => {
        if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        return response.json();
    })
    .then(user => {
        const claims = parseJwt(token);
        const isAdminUser = claims && claims.type === 'Admin';

        if (isAdminUser) {
            // Admin: show admin profile section (read-only) and hide editing UI
            document.getElementById('profileSection').style.display = 'none';
            document.getElementById('editForm').style.display = 'none';
            document.getElementById('adminProfileSection').style.display = 'block';
            document.getElementById('admin-id').innerText = user.id;
            document.getElementById('admin-shopName').innerText = user.ShopName || '';
            document.getElementById('admin-email').innerText = user.Email || '';
            document.getElementById('admin-address').innerText = user.Address || '';
            document.getElementById('admin-icecount').innerText = (user.IceCreams && user.IceCreams.length) || 0;
        } else {
            // Non-admin: show editable profile
            document.getElementById('adminProfileSection').style.display = 'none';
            document.getElementById('profileSection').style.display = 'block';
            document.getElementById('editForm').style.display = 'none';
            document.getElementById('profile-id').value = user.id;
            document.getElementById('profile-shopName').value = user.ShopName || '';
            document.getElementById('profile-email').value = user.Email || '';
            document.getElementById('profile-address').value = user.Address || '';
        }
    })
    .catch(error => console.error('Unable to get user.', error));
}

function updateProfile() {
    const id = document.getElementById('profile-id').value;
    const user = {
        id: parseInt(id, 10),
        Email: document.getElementById('profile-email').value.trim(),
        ShopName: document.getElementById('profile-shopName').value.trim(),
        Address: document.getElementById('profile-address').value.trim(),
        Password: "" // keep existing password
    };

    const token = localStorage.getItem('token');

    fetch(`${uri}/${id}`, {
            method: 'PUT',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            },
            body: JSON.stringify(user)
        })
        .then(response => {
            if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
            return response.text();
        })
        .then(() => {
            alert('הפרטים עודכנו');
            showProfile(id);
        })
        .catch(error => console.error('Unable to update profile.', error));

    return false;
}

function _displayUsers(data) {
    const tBody = document.getElementById('users');
    tBody.innerHTML = '';

    _displayCount(data.length);

    const button = document.createElement('button');

    data.forEach(user => {
        let detailsButton = button.cloneNode(false);
        detailsButton.innerText = 'פרטים';
        detailsButton.setAttribute('onclick', `showProfile(${user.id})`);

        let editButton = button.cloneNode(false);
        editButton.innerText = 'Edit';
        editButton.setAttribute('onclick', `displayEditForm(${user.id})`);

        let deleteButton = button.cloneNode(false);
        deleteButton.innerText = 'Delete';
        deleteButton.setAttribute('onclick', `deleteUser(${user.id})`);

        let tr = tBody.insertRow();

        let td0 = tr.insertCell(0);
        td0.appendChild(detailsButton);

        let td1 = tr.insertCell(1);
        let textNode1 = document.createTextNode(`${user.ShopName || user.FirstName || user.firstName}`);
        td1.appendChild(textNode1);

        let td2 = tr.insertCell(2);
        let textNode2 = document.createTextNode(`${user.Email || user.LastName || user.lastName}`);
        td2.appendChild(textNode2);

        let td3 = tr.insertCell(3);
        td3.appendChild(editButton);

        let td4 = tr.insertCell(4);
        // only show delete button to Admin users
        if (isAdmin()) td4.appendChild(deleteButton);

        // הודעה אם האוסף ריק
        let td5 = tr.insertCell(5);
        if (user.IceCreams && user.IceCreams.length === 0) {
            td5.innerText = 'האוסף שלך ריק';
        }
    });

    users = data;
}

// expose showProfile on load for the current user
// if this page is for profile, call showProfile() on load
if (window.location.pathname.endsWith('/user.html')) {
    showProfile();
}

// admin profile UI actions
document.addEventListener('click', function (e) {
    if (e.target && e.target.id === 'admin-delete-btn') {
        const id = document.getElementById('admin-id').innerText;
        if (confirm('האם למחוק את המשתמש?')) {
            deleteUser(id);
            document.getElementById('adminProfileSection').style.display = 'none';
        }
    }
    if (e.target && e.target.id === 'admin-back-btn') {
        history.back();
    }
});