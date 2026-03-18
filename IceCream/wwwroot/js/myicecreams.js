const uri = '/user';

function parseJwt(token) {
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

let viewingShopId = null; // מזהה החנות שמוצגת ברגע זה (יכול להיות של המשתמש או של חנות אחרת אם Admin)
let viewingShopName = null; // שם החנות שמוצגת כעת (לממשק מידע)

// helper to update the 'פרטי משתמש' link based on context
function updateUserLink(targetId, shopName) {
    try {
        const link = document.getElementById('userLink');
        if (!link) return;
        if (targetId) {
            // use a relative path to the same folder and attach id
            link.href = `user.html?id=${encodeURIComponent(targetId)}`;
            // set human friendly text: shopName if available, otherwise a default
            link.innerText = shopName && String(shopName).trim() ? `פרטי ${String(shopName).trim()}` : 'פרטי משתמש';
            link.dataset.userId = targetId;
        } else {
            const token = localStorage.getItem('token');
            const claims = parseJwt(token);
            if (claims && claims.type === 'Admin') {
                link.href = 'user.html';
                link.innerText = 'רשימת משתמשים';
                link.dataset.userId = '';
            } else {
                const myId = getCurrentUserId();
                link.href = myId ? `user.html?id=${encodeURIComponent(myId)}` : 'user.html';
                link.innerText = 'פרטי משתמש';
                link.dataset.userId = myId || '';
            }
        }
    } catch (e) {
        console.error('updateUserLink failed', e);
    }
}

// NEW: control add form visibility depending on role and viewing context
function updateAddFormVisibility() {
    const addForm = document.getElementById('addForm');
    const info = document.getElementById('addTargetInfo');
    if (!addForm) return;
    if (isAdmin()) {
        // admin can add only when viewing a specific shop
        addForm.style.display = viewingShopId ? 'block' : 'none';
        if (info) info.innerText = viewingShopId ? `מוסיף גלידה לחנות: ${viewingShopName || viewingShopId}` : '';
    } else {
        // regular users always see the add form for their own collection
        addForm.style.display = 'block';
        if (info) info.innerText = 'הוספת גלידה לאוסף שלי';
    }
}

function getMyIceCreams() {
    const token = localStorage.getItem('token');
    const myId = getCurrentUserId();
    if (!myId) {
        window.location.href = 'login.html';
        return;
    }
    
    // אם אדמין - הצג את כל החנויות עם האוספים שלהן
    if (isAdmin()) {
        fetch(`${uri}`, {
            headers: {
                'Accept': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            }
        })
        .then(response => {
            if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
            return response.json();
        })
        .then(data => {
            viewingShopId = null; viewingShopName = null; // עדיין לא מציגים אוסף ספציפי
            _displayAllShops(data, token);
            updateUserLink(null); // show users list link for admin
            // hide add form on the admin overview
            updateAddFormVisibility();
            // set page title
            const titleEl = document.getElementById('page-title'); if (titleEl) titleEl.innerText = 'כל החנויות והאוספים שלהן';
        })
        .catch(error => console.error('Unable to get shops.', error));
    } else {
        // משתמש רגיל - הצג רק את האוסף שלו
        viewingShopId = myId; viewingShopName = 'שלי';
        fetch(`${uri}/${myId}/icecreams`, {
            headers: {
                'Accept': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            }
        })
        .then(response => {
            if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
            return response.json();
        })
        .then(data => {
            _displayIceCreams(data);
            updateUserLink(myId); // link to own profile
            updateAddFormVisibility();
            const titleEl = document.getElementById('page-title'); if (titleEl) titleEl.innerText = 'האוספים וההזמנות שלי';
        })
        .catch(error => console.error('Unable to get ice creams.', error));
    }
}

function addIceCream() {
    const name = document.getElementById('add-icecream-name').value.trim();
    const isDiary = document.getElementById('add-icecream-isdiary').checked;
    const token = localStorage.getItem('token');
    const targetId = viewingShopId || getCurrentUserId();
    if (!targetId) { window.location.href = 'login.html'; return; }
    fetch(`${uri}/${targetId}/icecreams`, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify({ Name: name, IsDiary: isDiary })
    })
    .then(response => {
        if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        return response.json();
    })
    .then(() => {
        // רענון התצוגה של האוסף שמוצג כעת
        if (viewingShopId) viewShopIceCreams(viewingShopId); else getMyIceCreams();
        document.getElementById('add-icecream-name').value = '';
        document.getElementById('add-icecream-isdiary').checked = false;
    })
    .catch(error => console.error('Unable to add ice cream.', error));
}

function _displayIceCreams(data) {
    const tBody = document.getElementById('iceCreams');
    tBody.innerHTML = '';
    document.getElementById('counter').innerText = `${data.length} גלידות`;
    const button = document.createElement('button');
    data.forEach(item => {
        let IsDiaryCheckbox = document.createElement('input');
        IsDiaryCheckbox.type = 'checkbox';
        IsDiaryCheckbox.disabled = true;
        IsDiaryCheckbox.checked = item.isDiary || item.IsDiary;
        let editButton = button.cloneNode(false);
        editButton.innerText = 'Edit';
        editButton.setAttribute('onclick', `displayEditForm(${item.id})`);
        let deleteButton = button.cloneNode(false);
        deleteButton.innerText = 'Delete';
        deleteButton.setAttribute('onclick', `deleteIceCream(${item.id})`);
        let tr = tBody.insertRow();
        let td1 = tr.insertCell(0);
        td1.appendChild(IsDiaryCheckbox);
        let td2 = tr.insertCell(1);
        let textNode = document.createTextNode(item.name || item.Name);
        td2.appendChild(textNode);
        let td3 = tr.insertCell(2);
        td3.appendChild(editButton);
        let td4 = tr.insertCell(3);
        td4.appendChild(deleteButton);
    });
}

function displayEditForm(id) {
    const token = localStorage.getItem('token');
    const targetId = viewingShopId || getCurrentUserId();
    fetch(`${uri}/${targetId}/icecreams`, {
        headers: {
            'Accept': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        }
    })
    .then(response => response.json())
    .then(data => {
        const item = data.find(i => i.id === id);
        document.getElementById('edit-name').value = item.name || item.Name;
        document.getElementById('edit-id').value = item.id;
        document.getElementById('edit-isdiary').checked = item.isDiary || item.IsDiary;
        document.getElementById('editForm').style.display = 'block';
    });
}

function updateIceCream() {
    const token = localStorage.getItem('token');
    const itemId = document.getElementById('edit-id').value;
    const item = {
        id: parseInt(itemId, 10),
        Name: document.getElementById('edit-name').value.trim(),
        IsDiary: document.getElementById('edit-isdiary').checked
    };
    const targetId = viewingShopId || getCurrentUserId();
    fetch(`${uri}/${targetId}/icecreams/${itemId}`, {
        method: 'PUT',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        },
        body: JSON.stringify(item)
    })
    .then(response => {
        if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        return response.text();
    })
    .then(() => {
        if (viewingShopId) viewShopIceCreams(viewingShopId); else getMyIceCreams();
        closeInput();
    })
    .catch(error => console.error('Unable to update ice cream.', error));
    return false;
}

function deleteIceCream(id) {
    const token = localStorage.getItem('token');
    const targetId = viewingShopId || getCurrentUserId();
    fetch(`${uri}/${targetId}/icecreams/${id}`, {
        method: 'DELETE',
        headers: {
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        }
    })
    .then(response => {
        if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        return response.text();
    })
    .then(() => {
        if (viewingShopId) viewShopIceCreams(viewingShopId); else getMyIceCreams();
    })
    .catch(error => console.error('Unable to delete ice cream.', error));
}

function closeInput() {
    document.getElementById('editForm').style.display = 'none';
}

function _displayAllShops(shops, token) {
    const tBody = document.getElementById('iceCreams');
    tBody.innerHTML = '';
    document.getElementById('counter').innerText = `${shops.length} חנויות`;
    
    shops.forEach(shop => {
        let tr = tBody.insertRow();
        
        // עמודה 1: שם החנות
        let td1 = tr.insertCell(0);
        let shopNameNode = document.createTextNode(shop.shopName || shop.ShopName);
        td1.appendChild(shopNameNode);
        
        // עמודה 2: מספר גלידות באוסף
        let td2 = tr.insertCell(1);
        let iceCreamCount = (shop.iceCreams && shop.iceCreams.length) || (shop.IceCreams && shop.IceCreams.length) || 0;
        if (iceCreamCount === 0) {
            td2.innerText = 'אוסף ריק';
        } else {
            td2.innerText = `${iceCreamCount} גלידות`;
        }
        
        // עמודה 3: כפתור לנהל את הגלידות של החנות
        let td3 = tr.insertCell(2);
        let manageButton = document.createElement('button');
        manageButton.innerText = 'נהל אוסף';
        // determine the user id for this shop object (support multiple possible property names)
        const shopUserId = shop.id || shop.userId || shop.UserId || shop.shopId;
        const shopNameForBtn = shop.shopName || shop.ShopName || '';
        manageButton.addEventListener('click', function () { viewShopIceCreams(shopUserId, shopNameForBtn); });
        td3.appendChild(manageButton);
    });
}

function viewShopIceCreams(shopId, shopName) {
    const token = localStorage.getItem('token');
    viewingShopId = shopId; viewingShopName = shopName || shopId; // נעדכן את ההקשר — עכשיו כל פעולות העריכה יתמקדו בחנות זו
    updateUserLink(shopId, shopName); // נקשר את כפתור "לפרטי משתמש" לפרטי המשתמש של החנות שנבחרה
    fetch(`${uri}/${shopId}/icecreams`, {
        headers: {
            'Accept': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        }
    })
    .then(response => {
        if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        return response.json();
    })
    .then(data => {
        _displayIceCreams(data);
        // show add form for admin when viewing a specific shop
        updateAddFormVisibility();
        // update page title to indicate which shop we view
        const titleEl = document.getElementById('page-title'); if (titleEl) titleEl.innerText = shopName ? `אוספים של ${shopName}` : 'אוספים';
    })
    .catch(error => console.error('Unable to get ice creams.', error));
}

// small toast helper
function showToast(message, ttl = 4000) {
    try {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.style.position = 'fixed';
            container.style.top = '16px';
            container.style.right = '16px';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }
        const el = document.createElement('div');
        el.className = 'toast-msg';
        el.style.background = 'rgba(0,0,0,0.8)';
        el.style.color = '#fff';
        el.style.padding = '10px 14px';
        el.style.marginTop = '8px';
        el.style.borderRadius = '6px';
        el.style.boxShadow = '0 2px 8px rgba(0,0,0,0.2)';
        el.innerText = message;
        container.appendChild(el);
        setTimeout(() => {
            el.style.transition = 'opacity 300ms';
            el.style.opacity = '0';
            setTimeout(() => el.remove(), 350);
        }, ttl);
    } catch (e) {
        console.warn('showToast failed', e);
    }
}

// SignalR client setup
(function () {
    try {
        const token = localStorage.getItem('token');
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notify', token ? { accessTokenFactory: () => token } : {})
            .configureLogging(signalR.LogLevel.Information)
            .build();

        connection.on('Notify', (payload) => {
            console.log('SignalR Notify received', payload);
            try {
                const data = typeof payload === 'string' ? JSON.parse(payload) : payload;

                // show readable toast messages for common actions
                if (data && data.action) {
                    if (data.action === 'ice_updated') {
                        showToast('עדכון בגלידה בוצע');
                    } else if (data.action === 'ice_added') {
                        showToast('נוספה גלידה חדשה');
                    } else if (data.action === 'ice_deleted') {
                        showToast('מחיקת גלידה בוצעה');
                    } else if (data.action === 'user_added') {
                        const name = data.shopName || data.userId || 'משתמש חדש';
                        showToast(`התווסף משתמש חדש: ${name}`);
                    } else {
                        showToast('קיבלנו הודעת עדכון');
                    }
                }

                if (data && data.action === 'ice_updated') {
                    // if the update affects the currently viewed shop, refresh
                    const currentTarget = viewingShopId || getCurrentUserId();
                    if (!currentTarget) return;
                    if (String(data.userId) === String(currentTarget) || data.action === 'ice_updated') {
                        if (viewingShopId) viewShopIceCreams(viewingShopId); else getMyIceCreams();
                    }
                } else if (data && data.action && data.action.startsWith('ice_')) {
                    // generic handler for other ice actions
                    if (viewingShopId) viewShopIceCreams(viewingShopId); else getMyIceCreams();
                } else {
                    // fallback: refresh
                    if (viewingShopId) viewShopIceCreams(viewingShopId); else getMyIceCreams();
                }
            } catch (e) {
                console.warn('Failed to parse SignalR payload', e);
            }
        });

        connection.start().then(() => console.log('SignalR connected')).catch(err => console.error('SignalR connection error', err));
        connection.onclose(err => console.warn('SignalR closed', err));

    } catch (e) {
        console.warn('SignalR setup failed', e);
    }
})();
