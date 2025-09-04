document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');
    const errorMessageDiv = document.getElementById('errorMessage');

    loginForm.addEventListener('submit', async (event) => {
        event.preventDefault();

        const username = document.getElementById('username').value;
        const password = document.getElementById('password').value;

        errorMessageDiv.style.display = 'none';
        errorMessageDiv.textContent = '';

        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password }),
            });

            if (response.ok) {
                const data = await response.json();
                localStorage.setItem('jwtToken', data.token);
                if (data.roles && data.roles.includes('Admin')) {
                    localStorage.setItem('isAdmin', 'true');
                } else {
                    localStorage.removeItem('isAdmin');
                }
                window.location.href = '/dashboard.html';
            } else {
                let message = 'Login failed. Please check your credentials.';
                if (response.status === 401) message = 'Invalid username or password.';
                const txt = await response.text();
                errorMessageDiv.textContent = message + (txt ? ' ' + txt : '');
                errorMessageDiv.style.display = 'block';
            }
        } catch (error) {
            errorMessageDiv.textContent = 'An unexpected error occurred. Please try again later.';
            errorMessageDiv.style.display = 'block';
        }
    });
});
