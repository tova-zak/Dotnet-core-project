const uri = '/user';
let users = [];

function getUsers() {
    const token = localStorage.getItem('token'); // מסביר: לוקח את הטוקן מה-localStorage
    fetch(uri, {
            headers: token ? { 'Authorization': `Bearer ${token}` } : {}
        })
        .then(response => response.json())
        .then(data => _displayUsers(data))
        .catch(error => console.error('Unable to get items.', error));
}

function addUser() {
    const addNameTextbox = document.getElementById('add-name');

    const user = {
        firstName: addNameTextbox.value.trim(),
        lastName: addNameTextbox.value.trim()
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
        .then(response => response.json())
        .then(() => {
            getUsers();
            addNameTextbox.value = '';
        })
        .catch(error => console.error('Unable to add item.', error));
}

function deleteUser(id) {
    const token = localStorage.getItem('token'); // מסביר: מוסיף טוקן למחיקת משתמש
    fetch(`${uri}/${id}`, {
            method: 'DELETE',
            headers: token ? { 'Authorization': `Bearer ${token}` } : {}
        })
        .then(() => getUsers())
        .catch(error => console.error('Unable to delete item.', error));
}

function displayEditForm(id) {
    const user = users.find(user => user.id === id);

    document.getElementById('edit-firstName').value = user.firstName;
    document.getElementById('edit-id').value = user.id;
    document.getElementById('edit-lastName').checked = user.lastName;
    document.getElementById('editForm').style.display = 'block';
}

function updateUser() {
    const userId = document.getElementById('edit-id').value;
    const user = {
        id: parseInt(userId, 10),
        lastName: document.getElementById('edit-lastName').checked,
        firstName: document.getElementById('edit-firstName').value.trim()
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
        .then(() => getUsers())
        .catch(error => console.error('Unable to update item.', error));

    closeInput();

    return false;
}

function closeInput() {
    document.getElementById('editForm').style.display = 'none';
}

function _displayCount(userCount) {
    const name = (userCount === 1) ? 'user' : 'user kinds';

    document.getElementById('counter').innerText = `${userCount} ${name}`;
}

function _displayUsers(data) {
    const tBody = document.getElementById('users');
    tBody.innerHTML = '';

    _displayCount(data.length);

    const button = document.createElement('button');

    data.forEach(user => {
        console.log(user);
        let editButton = button.cloneNode(false);
        editButton.innerText = 'Edit';
        editButton.setAttribute('onclick', `displayEditForm(${user.id})`);

        let deleteButton = button.cloneNode(false);
        deleteButton.innerText = 'Delete';
        deleteButton.setAttribute('onclick', `deleteUser(${user.id})`);

        let tr = tBody.insertRow();

        let td1 = tr.insertCell(0);
        let textNode1 = document.createTextNode(`${user.firstName}`);
        td1.appendChild(textNode1); // fixed variable name
    
        let td2 = tr.insertCell(1);
        let textNode2 = document.createTextNode(`${user.lastName}`);
        td2.appendChild(textNode2);

        let td3 = tr.insertCell(2);
        td3.appendChild(editButton);

        let td4 = tr.insertCell(3);
        td4.appendChild(deleteButton);
    });

    users = data;
}