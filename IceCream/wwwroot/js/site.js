const uri = '/IceCream';
let iceCreams = [];

function parseJwt (token) {
    if (!token) return null;
    try {
        const payload = token.split('.')[1];
        if (!payload) return null;
        // atob should work for typical tokens (no heavy unicode in claims)
        return JSON.parse(decodeURIComponent(Array.prototype.map.call(atob(payload.replace(/-/g, '+').replace(/_/g, '/')), function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join('')));
    } catch (e) {
        return null;
    }
}

function isAdmin() {
    const token = localStorage.getItem('token');
    const claims = parseJwt(token);
    return claims && (claims.type === 'Admin');
}

function getItems() {
    const token = localStorage.getItem('token');
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
        .then(data => _displayItems(data))
        .catch(error => console.error('Unable to get items.', error));
}

function addItem() {
    const addNameTextbox = document.getElementById('add-name');
    const addIsDiary = document.getElementById('add-IsDiary'); // מסביר: קורא את ה-checkbox

    const item = {
        IsDiary: addIsDiary ? addIsDiary.checked : false,
        name: addNameTextbox.value.trim()
    };

    const token = localStorage.getItem('token'); // מסביר: לוקח טוקן מה-localStorage

    fetch(uri, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                ...(token ? { 'Authorization': `Bearer ${token}` } : {})
            },
            body: JSON.stringify(item)
        })
        .then(response => {
            if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
            // successful POST may return JSON or empty; try to parse if content-type is json
            const ct = response.headers.get('content-type') || '';
            return ct.includes('application/json') ? response.json() : Promise.resolve();
        })
        .then(() => {
            getItems();
            addNameTextbox.value = '';
            if (addIsDiary) addIsDiary.checked = false; // איפוס תיבת הסימון
        })
        .catch(error => console.error('Unable to add item.', error));
}

function deleteItem(id) {
    const token = localStorage.getItem('token');
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
        .then(() => getItems())
        .catch(error => console.error('Unable to delete item.', error));
}

function displayEditForm(id) {
    const item = iceCreams.find(item => item.id === id);

    document.getElementById('edit-name').value = item.name;
    document.getElementById('edit-id').value = item.id;
    document.getElementById('edit-IsDiary').checked = item.isDiary; // מסביר: מסמן את ה-checkbox לפי הערך
    document.getElementById('editForm').style.display = 'block';
}

function updateItem() {
    const itemId = document.getElementById('edit-id').value;
    const item = {
        id: parseInt(itemId, 10),
        IsDiary: document.getElementById('edit-IsDiary').checked,
        name: document.getElementById('edit-name').value.trim()
    };

    const token = localStorage.getItem('token'); // מסביר: מוסיף טוקן לכותרות אם קיים

    fetch(`${uri}/${itemId}`, {
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
        .then(() => getItems())
        .catch(error => console.error('Unable to update item.', error));

    closeInput();

    return false;
}

function closeInput() {
    document.getElementById('editForm').style.display = 'none';
}

function _displayCount(itemCount) {
    const name = (itemCount === 1) ? 'iceCream' : 'iceCream kinds';

    document.getElementById('counter').innerText = `${itemCount} ${name}`;
}

function _displayItems(data) {
    const tBody = document.getElementById('iceCreams');
    tBody.innerHTML = '';

    _displayCount(data.length);

    const button = document.createElement('button');

    data.forEach(item => {
        let IsDiaryCheckbox = document.createElement('input');
        IsDiaryCheckbox.type = 'checkbox';
        IsDiaryCheckbox.disabled = true;
        IsDiaryCheckbox.checked = item.isDiary;

        let editButton = button.cloneNode(false);
        editButton.innerText = 'Edit';
        editButton.setAttribute('onclick', `displayEditForm(${item.id})`);

        let deleteButton = button.cloneNode(false);
        deleteButton.innerText = 'Delete';
        deleteButton.setAttribute('onclick', `deleteItem(${item.id})`);

        let tr = tBody.insertRow();

        let td1 = tr.insertCell(0);
        td1.appendChild(IsDiaryCheckbox);

        let td2 = tr.insertCell(1);
        let textNode = document.createTextNode(item.name);
        td2.appendChild(textNode);

        let td3 = tr.insertCell(2);
        td3.appendChild(editButton);

        let td4 = tr.insertCell(3);
        // only show delete button to Admin users
        if (isAdmin()) td4.appendChild(deleteButton);
    });

    iceCreams = data;
}