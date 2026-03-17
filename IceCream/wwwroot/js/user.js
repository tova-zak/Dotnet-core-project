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
        // Non-admin: fetch only own user by id - showProfile directly to avoid using removed table
        const myId = getCurrentUserId();
        if (!myId) {
            console.error('No authenticated user found.');
            return;
        }

        // במקום לעטוף במערך ולקרוא ל-_displayUsers, פשוט הצג את הפרופיל של המשתמש הנוכחי
        showProfile(myId);
    }
}

// הסרתי פונקציית addUser (עמוד ה-HTML כבר הוסר), לא נדרשת

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
    // בטיחות בצד לקוח: אפשר לערוך רק את הפרופיל של המשתמש המחובר
    const currentId = getCurrentUserId();
    if (!currentId) { window.location.href = 'login.html'; return; }

    // אם הבקשה היא לערוך משתמש שאינו המשתמש המחובר, אין הרשאה
    if (parseInt(currentId, 10) !== parseInt(id, 10)) {
        alert('אין הרשאה לערוך משתמש זה. ניתן לערוך רק את הפרופיל האישי שלך.');
        return;
    }

    // במצב זה — המשתמש עורך את הפרופיל שלו: הצג את ה־profileSection למילוי ועריכה
    const user = users.find(u => u.id === id) || users.find(u => u.id === parseInt(id, 10));
    if (user) {
        // מלא את שדות הפרופיל עם הנתונים הקיימים (תומך בשמות שדות שונים)
        const shop = user.ShopName || user.shopName || '';
        const email = user.Email || user.email || '';
        const address = user.Address || user.address || '';

        if (document.getElementById('profile-id')) document.getElementById('profile-id').value = user.id;
        if (document.getElementById('pwd-id')) document.getElementById('pwd-id').value = user.id;
        if (document.getElementById('profile-shopName')) document.getElementById('profile-shopName').value = shop;
        if (document.getElementById('profile-email')) document.getElementById('profile-email').value = email;
        if (document.getElementById('profile-address')) document.getElementById('profile-address').value = address;

        // הצג אזור עריכת הפרופיל והסתר אזורים אחרים
        if (document.getElementById('adminProfileSection')) document.getElementById('adminProfileSection').style.display = 'none';
        if (document.getElementById('editForm')) document.getElementById('editForm').style.display = 'none';
        if (document.getElementById('profileSection')) document.getElementById('profileSection').style.display = 'block';
        return;
    }

    // אם לא נמצא במטמון, נטען מהשרת ואז נמלא
    const token = localStorage.getItem('token');
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
        const shop = user.ShopName || user.shopName || '';
        const email = user.Email || user.email || '';
        const address = user.Address || user.address || '';

        if (document.getElementById('profile-id')) document.getElementById('profile-id').value = user.id;
        if (document.getElementById('pwd-id')) document.getElementById('pwd-id').value = user.id;
        if (document.getElementById('profile-shopName')) document.getElementById('profile-shopName').value = shop;
        if (document.getElementById('profile-email')) document.getElementById('profile-email').value = email;
        if (document.getElementById('profile-address')) document.getElementById('profile-address').value = address;

        if (document.getElementById('adminProfileSection')) document.getElementById('adminProfileSection').style.display = 'none';
        if (document.getElementById('editForm')) document.getElementById('editForm').style.display = 'none';
        if (document.getElementById('profileSection')) document.getElementById('profileSection').style.display = 'block';
    })
    .catch(error => {
        console.error('Unable to load user for edit.', error);
        alert('שגיאה בטעינת פרטי המשתמש: ' + (error.message || error));
    });
}

function updateUser() {
    // עדכון פרופיל: רק המוכר (המשתמש המחובר) יכול לקרוא לעדכון זה
    const userId = document.getElementById('edit-id').value;
    const currentId = getCurrentUserId();
    if (!currentId || parseInt(currentId,10) !== parseInt(userId,10)) {
        alert('אין הרשאה לעדכן משתמש זה.');
        return false;
    }

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
        if (!response.ok) {
            // אם הסטטוס הוא 401 או 400 (למשל סיסמה שגויה), נזרוק שגיאה
            return response.text().then(t => { 
                throw new Error("InvalidPassword"); 
            });
        }
        const ct = response.headers.get('content-type') || '';
        return ct.includes('application/json') || ct.includes('text/plain') ? response.text() : Promise.resolve();
    })
    .then(tokenString => {
        // אם הגענו לכאן, סימן שהעדכון הצליח
        alert("עדכון הסיסמא עבר בהצלחה");

        if (tokenString && tokenString.length > 10) {
            localStorage.setItem('token', tokenString);
        }
        document.getElementById('current-password').value = '';
        document.getElementById('new-password').value = '';
    })
    .catch(error => {
        // כאן אנחנו תופסים גם שגיאות רשת וגם את השגיאה שזרקנו למעלה
        console.error('Unable to change password.', error);
        alert("אחת מהסיסמאות שהזנת שגויות, נסה שנית");
    });
}



function closeInput() {
    document.getElementById('editForm').style.display = 'none';
}

function _displayCount(userCount) {
    const name = (userCount === 1) ? 'user' : 'user kinds';

    document.getElementById('counter').innerText = `${userCount} ${name}`;
}

// new: show profile for current user (or admin viewing a specific user id)
// accepts optional second parameter { hideList: boolean }
function showProfile(userId, options) {
    const token = localStorage.getItem('token');
    // use provided userId if explicitly passed (even 0), otherwise fallback to current user
    let id;
    if (typeof userId !== 'undefined' && userId !== null) id = userId;
    else id = getCurrentUserId();

    if (!id) { window.location.href = 'login.html'; return; }

    console.log('showProfile requested id=', id);

    fetch(`${uri}/${encodeURIComponent(id)}`, {
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
        console.log('showProfile got user.id=', user && user.id);
        const claims = parseJwt(token);
        const isAdminUser = claims && claims.type === 'Admin';

        if (isAdminUser) {
            // Admin: show admin profile section (read-only) and hide editing UI
            const profileEl = document.getElementById('profileSection'); if (profileEl) profileEl.style.display = 'none';
            const editEl = document.getElementById('editForm'); if (editEl) editEl.style.display = 'none';
            const adminEl = document.getElementById('adminProfileSection'); if (adminEl) adminEl.style.display = 'block';

            // fill with the fetched user's data (not admin claims)
            const adminId = document.getElementById('admin-id'); if (adminId) adminId.innerText = user.id;
            const adminShop = document.getElementById('admin-shopName'); if (adminShop) adminShop.innerText = user.ShopName || user.shopName || '';
            const adminEmail = document.getElementById('admin-email'); if (adminEmail) adminEmail.innerText = user.Email || user.email || '';
            const adminAddress = document.getElementById('admin-address'); if (adminAddress) adminAddress.innerText = user.Address || user.address || '';
            const adminIce = document.getElementById('admin-icecount'); if (adminIce) adminIce.innerText = (user.IceCreams && user.IceCreams.length) || (user.iceCreams && user.iceCreams.length) || 0;

            // hide users list only after successfully loaded the selected user's data
            if (options && options.hideList) {
                const usersListEl = document.getElementById('usersList'); if (usersListEl) usersListEl.style.display = 'none';
            }
        } else {
            // Non-admin: show editable profile
            const adminEl = document.getElementById('adminProfileSection'); if (adminEl) adminEl.style.display = 'none';
            const profileEl = document.getElementById('profileSection'); if (profileEl) profileEl.style.display = 'block';
            const editEl = document.getElementById('editForm'); if (editEl) editEl.style.display = 'none';

            const profileIdEl = document.getElementById('profile-id'); if (profileIdEl) profileIdEl.value = user.id;
            const pwdEl = document.getElementById('pwd-id'); if (pwdEl) pwdEl.value = user.id;

            const shopEl = document.getElementById('profile-shopName'); if (shopEl) shopEl.value = user.ShopName || user.shopName || '';
            const emailEl = document.getElementById('profile-email'); if (emailEl) emailEl.value = user.Email || user.email || '';
            const addressEl = document.getElementById('profile-address'); if (addressEl) addressEl.value = user.Address || user.address || '';

            // ensure users list is hidden for non-admin view (only if asked)
            if (options && options.hideList) {
                const usersListEl = document.getElementById('usersList'); if (usersListEl) usersListEl.style.display = 'none';
            }
        }
    })
    .catch(error => {
        console.error('Unable to get user.', error);
        alert('שגיאה בטעינת פרטי המשתמש: ' + (error.message || error));
    });
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
            // עדכון המטמון בצד לקוח כדי שהשינויים ייראו מיד
            const idx = users.findIndex(u => parseInt(u.id,10) === parseInt(id,10));
            if (idx !== -1) {
                users[idx].ShopName = user.ShopName;
                users[idx].shopName = user.ShopName;
                users[idx].Email = user.Email;
                users[idx].email = user.Email;
                users[idx].Address = user.Address;
                users[idx].address = user.Address;
            }

            // עדכון תצוגה נוכחית
            alert('הפרטים עודכנו בהצלחה');
            showProfile(id);
        })
        .catch(error => {
            console.error('Unable to update profile.', error);
            alert('עדכון נכשל: ' + (error.message || error));
        });

    return false;
}

function _displayUsers(data) {
    // הצג רשימת משתמשים בתוך ה-div #usersList — מיועד למנהל בלבד
    const container = document.getElementById('usersList');
    if (!container) {
        // אם אין אלמנט כזה - אין מה לעשות (ייתכן שמדובר בעמוד משתמש בלבד)
        return;
    }

    container.style.display = 'block'; // ensure visible when populated
    container.innerHTML = '';

    if (!Array.isArray(data) || data.length === 0) {
        container.innerHTML = '<p>אין משתמשים להצגה</p>';
        return;
    }

    data.forEach(u => {
        const user = u; // keep original
        const card = document.createElement('div');
        card.className = 'user-card';

        const info = document.createElement('div');
        // show shop name when available; fall back to email or a localized placeholder instead of a dash
        info.innerHTML = `<strong>${user.ShopName || user.shopName || user.Email || '(אין שם חנות)'}</strong><br/>${user.Email || ''}<br/>${user.Address || ''}`;

        const actions = document.createElement('div');
        actions.className = 'actions';

        const detailsBtn = document.createElement('button');
        detailsBtn.innerText = 'פרטים';
        detailsBtn.onclick = () => {
            // עבור מנהל: הבאת פרטי המשתמש הספציפי ישירות מהשרת והצגתם
            const idNum = Number(user.id);
            if (Number.isNaN(idNum)) return;
            const token = localStorage.getItem('token');
            fetch(`${uri}/${encodeURIComponent(idNum)}`, {
                headers: {
                    'Accept': 'application/json',
                    ...(token ? { 'Authorization': `Bearer ${token}` } : {})
                }
            })
            .then(r => {
                if (!r.ok) return r.text().then(t => { throw new Error(`${r.status} ${r.statusText}: ${t}`); });
                return r.json();
            })
            .then(selectedUser => {
                // הסתר את הרשימה והצג את כרטיס הפרטים של המשתמש שנבחר
                const usersListEl = document.getElementById('usersList'); if (usersListEl) usersListEl.style.display = 'none';
                const adminEl = document.getElementById('adminProfileSection'); if (adminEl) adminEl.style.display = 'block';
                const profileEl = document.getElementById('profileSection'); if (profileEl) profileEl.style.display = 'none';
                const editEl = document.getElementById('editForm'); if (editEl) editEl.style.display = 'none';

                const adminId = document.getElementById('admin-id'); if (adminId) adminId.innerText = selectedUser.id;
                const adminShop = document.getElementById('admin-shopName'); if (adminShop) adminShop.innerText = selectedUser.ShopName || selectedUser.shopName || '';
                const adminEmail = document.getElementById('admin-email'); if (adminEmail) adminEmail.innerText = selectedUser.Email || selectedUser.email || '';
                const adminAddress = document.getElementById('admin-address'); if (adminAddress) adminAddress.innerText = selectedUser.Address || selectedUser.address || '';
                const adminIce = document.getElementById('admin-icecount'); if (adminIce) adminIce.innerText = (selectedUser.IceCreams && selectedUser.IceCreams.length) || (selectedUser.iceCreams && selectedUser.iceCreams.length) || 0;

                // וודא שהכפתור מחיקה פועל על המשתמש הנבחר
                const delBtn = document.getElementById('admin-delete-btn');
                if (delBtn) {
                    // נתק את המאזין הקודם
                    delBtn.onclick = null;
                    delBtn.onclick = function () {
                        if (confirm('אתה בטוח שברצונך למחוק את המשתמש?')) {
                            deleteUser(selectedUser.id);
                            // אחרי מחיקה הצג את הרשימה מחדש
                            const adminEl2 = document.getElementById('adminProfileSection'); if (adminEl2) adminEl2.style.display = 'none';
                            const usersListEl2 = document.getElementById('usersList'); if (usersListEl2) usersListEl2.style.display = 'block';
                        }
                    };
                }
            })
            .catch(err => {
                console.error('Unable to load selected user', err);
                alert('שגיאה בטעינת פרטי המשתמש שנבחר: ' + (err.message || err));
            });
        };

        // מנהל יכול למחוק
        const deleteBtn = document.createElement('button');
        deleteBtn.innerText = 'מחק';
        deleteBtn.onclick = () => {
            if (confirm('האם למחוק את המשתמש?')) {
                deleteUser(Number(user.id));
            }
        };

        actions.appendChild(detailsBtn);
        if (isAdmin()) actions.appendChild(deleteBtn);

        card.appendChild(info);
        card.appendChild(actions);

        container.appendChild(card);
    });

    // שמירת מטמון
    users = data;
}

// expose showProfile on load for the current user
// if this page is for profile, call getUsers() for admin, or showProfile() for non-admin
if (window.location.pathname.endsWith('/user.html')) {
    const params = new URLSearchParams(window.location.search);
    const idParam = params.get('id');
    if (idParam) {
        // show the specific user's profile and hide the users list (if admin)
        showProfile(idParam, { hideList: true });
    } else {
        if (isAdmin()) {
            // admin should see the users list first
            getUsers();
        } else {
            // non-admin see own profile
            showProfile();
        }
    }
}

// call getUsers when relevant DOM is ready (protect against pages without these elements)
document.addEventListener('DOMContentLoaded', function() {
    if (!(document.getElementById('profileSection') || document.getElementById('adminProfileSection') || document.getElementById('usersList'))) return;
    const params = new URLSearchParams(window.location.search);
    const idParam = params.get('id');
    if (idParam) {
        // already handled above; ensure profile is shown for that id
        showProfile(idParam, { hideList: true });
        return;
    }
    getUsers();
});

// admin profile UI actions
document.addEventListener('click', function (e) {
    if (e.target && e.target.id === 'admin-delete-btn') {
        const idEl = document.getElementById('admin-id');
        const id = idEl ? idEl.innerText : null;
        if (id && confirm('האם למחוק את המשתמש?')) {
            deleteUser(id);
            const adminEl = document.getElementById('adminProfileSection'); if (adminEl) adminEl.style.display = 'none';
            // after deletion, show the list again
            const usersListEl = document.getElementById('usersList'); if (usersListEl) usersListEl.style.display = 'block';
        }
    }
    if (e.target && e.target.id === 'admin-back-btn') {
        // hide admin card and show users list
        const adminEl = document.getElementById('adminProfileSection'); if (adminEl) adminEl.style.display = 'none';
        const usersListEl = document.getElementById('usersList'); if (usersListEl) usersListEl.style.display = 'block';
        // refresh list
        getUsers();
    }
});