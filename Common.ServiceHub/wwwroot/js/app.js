const API_URL = '/api';

// Handle Login Page
const loginForm = document.getElementById('loginForm');
if (loginForm) {
    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const usernameInput = document.getElementById('username');
        const passwordInput = document.getElementById('password');
        const errorMsg = document.getElementById('errorMsg');
        const submitBtn = document.getElementById('submitBtn');

        const username = usernameInput.value.trim();
        const password = passwordInput.value.trim();

        if (errorMsg) errorMsg.classList.add('hidden');
        if (submitBtn) submitBtn.disabled = true;

        try {
            const res = await fetch(`${API_URL}/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });

            if (res.ok) {
                const data = await res.json();
                localStorage.setItem('token', data.token);
                localStorage.setItem('role', data.role);
                localStorage.setItem('username', data.username);
                window.location.href = '/dashboard.html';
            } else {
                if (errorMsg) {
                    errorMsg.innerText = 'Invalid username or password';
                    errorMsg.classList.remove('hidden');
                }
            }
        } catch (err) {
            if (errorMsg) {
                errorMsg.innerText = 'Server connection error';
                errorMsg.classList.remove('hidden');
            }
        } finally {
            if (submitBtn) submitBtn.disabled = false;
        }
    });
}

// Password Visibility Toggle
window.togglePassword = () => {
    const p = document.getElementById('password');
    if (p) {
        p.type = p.type === 'password' ? 'text' : 'password';
    }
};

// Handle Dashboard Page
async function loadDashboard() {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const username = localStorage.getItem('username');

    if (!token) {
        window.location.href = '/index.html';
        return;
    }

    const badge = document.getElementById('userRoleBadge');
    if (badge) badge.innerText = role;

    const nameDisplay = document.getElementById('userNameDisplay');
    if (nameDisplay && username) {
        nameDisplay.innerText = `Welcome, ${username}`;
    }

    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', () => {
            localStorage.clear();
            window.location.href = '/index.html';
        });
    }

    const isAdmin = (role || '').toLowerCase() === 'admin';
    const usersTabBtn = document.getElementById('usersTabBtn');
    const addItemBtn = document.getElementById('addItemBtn');

    if (!isAdmin && usersTabBtn) {
        usersTabBtn.classList.add('hidden');
    }
    if (isAdmin && addItemBtn) {
        addItemBtn.classList.remove('hidden');
    }

    await fetchItems(token, isAdmin);
}

window.switchTab = async (tab) => {
    const itemsTabBtn = document.getElementById('itemsTabBtn');
    const usersTabBtn = document.getElementById('usersTabBtn');
    const itemsSection = document.getElementById('itemsSection');
    const usersSection = document.getElementById('usersSection');
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const isAdmin = (role || '').toLowerCase() === 'admin';

    if (tab === 'items') {
        if (itemsTabBtn) itemsTabBtn.classList.add('active');
        if (usersTabBtn) usersTabBtn.classList.remove('active');
        if (itemsSection) itemsSection.classList.remove('hidden');
        if (usersSection) usersSection.classList.add('hidden');
        await fetchItems(token, isAdmin);
    } else {
        if (usersTabBtn) usersTabBtn.classList.add('active');
        if (itemsTabBtn) itemsTabBtn.classList.remove('active');
        if (usersSection) usersSection.classList.remove('hidden');
        if (itemsSection) itemsSection.classList.add('hidden');
        await fetchUsers(token);
    }
};

async function fetchItems(token, isAdmin) {
    try {
        const res = await fetch(`${API_URL}/common/items`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (res.status === 401) {
            window.location.href = '/index.html';
            return;
        }

        const items = await res.json();

        // Dynamically adjust table headers based on Admin status
        const theadTr = document.querySelector('#itemsSection table thead tr');
        if (theadTr) {
            theadTr.innerHTML = `
                <th>Item Name</th>
                <th>Category</th>
                <th>Source Service</th>
                <th>Price</th>
                <th>Stock</th>
                ${isAdmin ? '<th>Actions</th>' : ''}
            `;
        }

        const tbody = document.getElementById('itemsBody');
        if (!tbody) return;
        tbody.innerHTML = '';

        items.forEach(item => {
            const tr = document.createElement('tr');
            const itemId = item.itemId || item.ItemId || item.id || item.Id;
            const name = item.name || item.Name || 'Unnamed Item';
            const category = item.category || item.Category || 'General';
            const source = item.sourceService || item.SourceService || 'Common';
            const price = item.price !== undefined ? item.price : item.Price;
            const stock = item.stockQuantity !== undefined ? item.stockQuantity : (item.StockQuantity !== undefined ? item.StockQuantity : '-');

            let badgeBg = 'var(--gradient-btn)';
            if (source === 'Dairy') badgeBg = '#3b82f6';
            else if (source === 'Grocery') badgeBg = '#10b981';
            else if (source === 'Stationary') badgeBg = '#f59e0b';

            tr.innerHTML = `
                <td><strong>${name}</strong></td>
                <td>${category}</td>
                <td><span class="badge" style="background: ${badgeBg}">${source}</span></td>
                <td>$${price}</td>
                <td>${stock}</td>
            `;

            if (isAdmin) {
                const btnEdit = document.createElement('button');
                btnEdit.className = 'submit-btn btn-sm';
                btnEdit.style.marginRight = '5px';
                btnEdit.style.padding = '4px 10px';
                btnEdit.innerText = 'Edit';
                btnEdit.onclick = () => window.openEditItemModal(itemId, name, category, price, stock);

                const btnDelete = document.createElement('button');
                btnDelete.className = 'btn-danger';
                btnDelete.innerText = 'Delete';
                btnDelete.onclick = () => window.deleteItem(itemId);

                const tdActions = document.createElement('td');
                tdActions.appendChild(btnEdit);
                tdActions.appendChild(btnDelete);
                tr.appendChild(tdActions);
            }
            tbody.appendChild(tr);
        });
    } catch (err) {
        console.error(err);
    }
}

async function fetchUsers(token) {
    try {
        const res = await fetch(`${API_URL}/users`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (res.status === 401) {
            window.location.href = '/index.html';
            return;
        }

        const users = await res.json();
        const tbody = document.getElementById('usersBody');
        if (!tbody) return;
        tbody.innerHTML = '';

        users.forEach(u => {
            const id = u.id || u.Id || '';
            const username = u.username || u.Username || 'Unknown';
            const role = u.role || u.Role || 'User';

            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td>${username}</td>
                <td><span class="badge">${role}</span></td>
                <td><button class="btn-danger" onclick="deleteUser('${id}')">Delete</button></td>
            `;
            tbody.appendChild(tr);
        });
    } catch (err) {
        console.error(err);
    }
}

window.deleteItem = async (id) => {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const isAdmin = (role || '').toLowerCase() === 'admin';
    await fetch(`${API_URL}/common/items/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    await fetchItems(token, isAdmin);
};

window.deleteUser = async (id) => {
    const token = localStorage.getItem('token');
    await fetch(`${API_URL}/users/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    await fetchUsers(token);
};

// Modal Controls
window.openItemModal = () => document.getElementById('itemModal')?.classList.remove('hidden');
window.closeItemModal = () => document.getElementById('itemModal')?.classList.add('hidden');
window.openEditItemModal = (id, name, cat, price, stock) => {
    document.getElementById('editItemId').value = id;
    document.getElementById('editItemName').value = name;
    document.getElementById('editItemCategory').value = cat;
    document.getElementById('editItemPrice').value = price;
    document.getElementById('editItemStock').value = stock;
    document.getElementById('editItemModal')?.classList.remove('hidden');
};
window.closeEditItemModal = () => document.getElementById('editItemModal')?.classList.add('hidden');

window.openUserModal = () => document.getElementById('userModal')?.classList.remove('hidden');
window.closeUserModal = () => document.getElementById('userModal')?.classList.add('hidden');

window.saveItem = async () => {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const isAdmin = (role || '').toLowerCase() === 'admin';
    const payload = {
        name: document.getElementById('itemName').value,
        category: document.getElementById('itemCategory').value,
        price: parseFloat(document.getElementById('itemPrice').value),
        stockQuantity: parseInt(document.getElementById('itemStock').value)
    };

    await fetch(`${API_URL}/common/items`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload)
    });

    closeItemModal();
    await fetchItems(token, isAdmin);
};

window.updateItem = async () => {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const isAdmin = (role || '').toLowerCase() === 'admin';
    const id = document.getElementById('editItemId').value;
    const payload = {
        name: document.getElementById('editItemName').value,
        category: document.getElementById('editItemCategory').value,
        price: parseFloat(document.getElementById('editItemPrice').value),
        stockQuantity: parseInt(document.getElementById('editItemStock').value)
    };

    await fetch(`${API_URL}/common/items/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload)
    });

    closeEditItemModal();
    await fetchItems(token, isAdmin);
};

window.saveUser = async () => {
    const token = localStorage.getItem('token');
    const payload = {
        username: document.getElementById('newUsername').value,
        password: document.getElementById('newPassword').value,
        role: document.getElementById('newRole').value
    };

    await fetch(`${API_URL}/users`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload)
    });

    closeUserModal();
    await fetchUsers(token);
};

if (window.location.pathname.includes('dashboard.html')) {
    loadDashboard();
}
