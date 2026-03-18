// SignalR Connection for Real-time Notifications
let connection = null;

// Helper function to add a new user to the users list in the DOM
function addNewUserToList(newUser) {
    console.log('addNewUserToList called with:', newUser);
    
    const container = document.getElementById('usersList');
    if (!container) {
        console.log('usersList container NOT found');
        return;
    }
    
    console.log('usersList container found, adding user');

    // Update global users array if it exists
    if (typeof users !== 'undefined' && Array.isArray(users)) {
        // Check if user already exists
        const existingIndex = users.findIndex(u => u.id === newUser.id);
        if (existingIndex < 0) {
            users.push(newUser);
            console.log('Added to users array');
        }
    }

    // Clear "no users" message if present
    const paragraphs = container.querySelectorAll('p');
    paragraphs.forEach(p => {
        if (p.textContent === 'אין משתמשים להצגה') {
            p.remove();
        }
    });

    // Create user card - SAME STRUCTURE AS _displayUsers
    const card = document.createElement('div');
    card.className = 'user-card';
    card.id = `user-card-${newUser.id}`;

    const info = document.createElement('div');
    const shopName = newUser.ShopName || newUser.shopName || newUser.Email || '(אין שם חנות)';
    const email = newUser.Email || newUser.email || '';
    const address = newUser.Address || newUser.address || '';
    info.innerHTML = `<strong>${shopName}</strong><br/>${email}<br/>${address}`;

    const actions = document.createElement('div');
    actions.className = 'actions';

    const detailsBtn = document.createElement('button');
    detailsBtn.innerText = 'פרטים';
    detailsBtn.onclick = () => {
        console.log('Details clicked for user:', newUser.id);
        if (typeof showProfile === 'function') {
            showProfile(newUser.id, { hideList: true });
        }
    };

    actions.appendChild(detailsBtn);
    card.appendChild(info);
    card.appendChild(actions);
    container.appendChild(card);

    console.log('User card added to DOM:', shopName);
}

// Helper function to fetch a single user from server and add to list
function fetchAndAddUserToList(userId) {
    console.log('fetchAndAddUserToList called for userId:', userId, 'type:', typeof userId);
    
    if (!userId && userId !== 0) {
        console.error('Invalid userId provided');
        return;
    }
    
    const token = localStorage.getItem('token');
    const uri = '/user';
    const userIdStr = String(userId);
    
    console.log('Fetching from:', `${uri}/${userIdStr}`);
    
    fetch(`${uri}/${encodeURIComponent(userIdStr)}`, {
        headers: {
            'Accept': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {})
        }
    })
    .then(response => {
        console.log('Fetch response status:', response.status, response.statusText);
        if (!response.ok) return response.text().then(t => { throw new Error(`${response.status} ${response.statusText}: ${t}`); });
        return response.json();
    })
    .then(user => {
        console.log('User fetched from server:', user);
        
        // Update users array if it exists
        if (typeof users !== 'undefined' && Array.isArray(users)) {
            // Check if user already exists
            const existingIndex = users.findIndex(u => u.id === user.id);
            if (existingIndex < 0) {
                users.push(user);
                console.log('Added to users array');
            } else {
                console.log('User already exists in array at index:', existingIndex);
            }
        } else {
            console.log('users array not available (not admin page?)');
        }
        
        // Add to DOM if container exists
        addNewUserToList(user);
    })
    .catch(error => {
        console.error('Error fetching user:', error);
        // Fallback: reload all users
        console.log('Fallback: calling getUsers()');
        if (typeof getUsers === 'function') {
            setTimeout(() => getUsers(), 500);
        }
    });
}

// Helper function to remove a user from the users list in the DOM
function removeUserFromList(userId) {
    const card = document.getElementById(`user-card-${userId}`);
    if (card) {
        card.remove();
        console.log('Removed user from list:', userId);
    }
    
    // Refresh the list if no users remain
    const container = document.getElementById('usersList');
    if (container && container.children.length === 0) {
        container.innerHTML = '<p>אין משתמשים להצגה</p>';
    }
}

function initializeSignalR() {
    const token = localStorage.getItem('token');
    if (!token) {
        console.log('No token found, skipping SignalR connection');
        return;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(`/hubs/notify?access_token=${encodeURIComponent(token)}`)
        .withAutomaticReconnect()
        .build();

    connection.on("Notify", onNotificationReceived);

    connection.start().catch(err => console.error('SignalR connection error:', err));

    connection.onreconnected(() => {
        console.log('SignalR reconnected');
        showNotification('מחובר שוב לשרת', 'success');
    });

    connection.onreconnecting(() => {
        console.log('SignalR reconnecting...');
    });

    connection.onclose(() => {
        console.log('SignalR disconnected');
    });
}

function onNotificationReceived(message) {
    try {
        const notification = typeof message === 'string' ? JSON.parse(message) : message;
        const { action, ...details } = notification;

        let title = '';
        let description = '';

        switch (action) {
            // Notifications for Admin
            case 'ice_created':
                title = '🆕 גלידה חדשה';
                description = `הגלידה "${details.name}" נוצרה`;
                break;
            case 'ice_updated':
                title = '✏️ גלידה עודכנה';
                description = `הגלידה "${details.name}" עודכנה`;
                break;
            case 'ice_deleted':
                title = '🗑️ גלידה נמחקה';
                description = `גלידה נמחקה`;
                break;
            case 'user_added_ice':
                title = '➕ משתמש הוסיף גלידה';
                description = `${details.userName} הוסיף את "${details.iceName}"`;
                break;
            case 'user_deleted_ice':
                title = '➖ משתמש מחק גלידה';
                description = `${details.userName} מחק את "${details.iceName}"`;
                break;
            case 'user_updated_ice':
                title = '✏️ משתמש עדכן גלידה';
                description = `${details.userName} עדכן את "${details.iceName}"`;
                break;
            case 'user_updated':
                title = '👤 משתמש עודכן';
                description = `המשתמש ${details.userName} עודכן`;
                break;
            case 'user_deleted':
                title = '🗑️ משתמש נמחק';
                description = `${details.userName} נמחק`;
                // Remove user from list in DOM
                removeUserFromList(details.userId);
                break;
            case 'new_user_registered':
                title = '👥 משתמש חדש נרשם';
                description = `${details.userName} נרשם בהצלחה`;
                // Fetch and add the new user to the list immediately
                fetchAndAddUserToList(details.userId);
                break;
            case 'new_user_created':
                title = '👥 משתמש חדש נוצר';
                description = `${details.userName} (${details.role}) נוצר`;
                // Fetch and add the new user to the list immediately
                fetchAndAddUserToList(details.userId);
                break;
            case 'your_profile_updated':
                title = '✅ הפרופיל שלך עודכן';
                description = 'הנתונים שלך עודכנו בהצלחה';
                break;

            // Notifications for User
            case 'ice_added':
                title = '✨ נוסף לרשימתך';
                description = `"${details.iceName}" נוסף לרשימת הגלידות שלך`;
                break;
            case 'ice_deleted':
                title = '🗑️ הוסר מרשימתך';
                description = `"${details.iceName}" הוסר מרשימת הגלידות שלך`;
                break;

            default:
                title = '📢 הודעה';
                description = JSON.stringify(details);
        }

        showNotification(`${title}\n${description}`, 'info');
        console.log('Notification:', notification);

    } catch (error) {
        console.error('Error parsing notification:', error);
    }
}

function showNotification(message, type = 'info') {
    // Try to find existing notification area
    let notificationArea = document.getElementById('notification-area');

    if (!notificationArea) {
        // Create notification area if it doesn't exist
        notificationArea = document.createElement('div');
        notificationArea.id = 'notification-area';
        notificationArea.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            max-width: 400px;
            font-family: Arial, sans-serif;
        `;
        document.body.appendChild(notificationArea);
    }

    // Create notification element
    const notification = document.createElement('div');
    notification.style.cssText = `
        padding: 15px 20px;
        margin-bottom: 10px;
        border-radius: 5px;
        color: white;
        font-size: 14px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        animation: slideIn 0.3s ease-out;
        white-space: pre-wrap;
        word-wrap: break-word;
    `;

    // Set background color based on type
    if (type === 'success') {
        notification.style.backgroundColor = '#4CAF50';
    } else if (type === 'error') {
        notification.style.backgroundColor = '#f44336';
    } else {
        notification.style.backgroundColor = '#2196F3';
    }

    notification.textContent = message;
    notificationArea.appendChild(notification);

    // Remove after 5 seconds
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease-out';
        setTimeout(() => notification.remove(), 300);
    }, 5000);
}

// Add CSS animations if not already present
if (!document.getElementById('notification-styles')) {
    const style = document.createElement('style');
    style.id = 'notification-styles';
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(400px);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(400px);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);
}

// Initialize SignalR when page loads
document.addEventListener('DOMContentLoaded', initializeSignalR);

// Optional: Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (connection) {
        connection.stop();
    }
});
