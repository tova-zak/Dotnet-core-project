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
        .then(data => _displayAllShops(data, token))
        .catch(error => console.error('Unable to get shops.', error));
    } else {
        // משתמש רגיל - הצג רק את האוסף שלו
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
        .then(data => _displayIceCreams(data))
        .catch(error => console.error('Unable to get ice creams.', error));
    }
}

function addIceCream() {
    const name = document.getElementById('add-icecream-name').value.trim();
    const isDiary = document.getElementById('add-icecream-isdiary').checked;
    const myId = getCurrentUserId();
    const token = localStorage.getItem('token');
    fetch(`${uri}/${myId}/icecreams`, {
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
        getMyIceCreams();
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
    const myId = getCurrentUserId();
    const token = localStorage.getItem('token');
    fetch(`${uri}/${myId}/icecreams`, {
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
    const myId = getCurrentUserId();
    const token = localStorage.getItem('token');
    const itemId = document.getElementById('edit-id').value;
    const item = {
        id: parseInt(itemId, 10),
        Name: document.getElementById('edit-name').value.trim(),
        IsDiary: document.getElementById('edit-isdiary').checked
    };
    fetch(`${uri}/${myId}/icecreams/${itemId}`, {
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
        getMyIceCreams();
        closeInput();
    })
    .catch(error => console.error('Unable to update ice cream.', error));
    return false;
}

function deleteIceCream(id) {
    const myId = getCurrentUserId();
    const token = localStorage.getItem('token');
    fetch(`${uri}/${myId}/icecreams/${id}`, {
        method: 'DELETE',
        headers: {
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        }
    })
    .then(response => {
        if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        return response.text();
    })
    .then(() => getMyIceCreams())
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
        
        // עמודה 3: כפתור לראות את הגלידות
        let td3 = tr.insertCell(2);
        let viewButton = document.createElement('button');
        viewButton.innerText = 'ראה גלידות';
        viewButton.setAttribute('onclick', `viewShopIceCreams(${shop.id})`);
        td3.appendChild(viewButton);
    });
}

function viewShopIceCreams(shopId) {
    const token = localStorage.getItem('token');
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
        alert(`גלידות בחנות זו: ${data.map(i => i.name || i.Name).join(', ') || 'אוסף ריק'}`);
    })
    .catch(error => console.error('Unable to get ice creams.', error));
}
