document.addEventListener('DOMContentLoaded', () => {
    const signupForm = document.getElementById('signupForm');
    const messageDiv = document.getElementById('message');

    signupForm.addEventListener('submit', async (event) => {
        event.preventDefault();

        messageDiv.style.display = 'none';
        messageDiv.textContent = '';
        messageDiv.className = 'message-display';

        const username = document.getElementById('username').value;
        const email = document.getElementById('email').value;
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;

        if (password !== confirmPassword) {
            displayMessage('Passwords do not match.', 'error');
            return;
        }

        try {
            const response = await fetch('/api/auth/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ username, email, password }),
            });

            const data = await response.json();

            if (response.ok) {
                displayMessage(data.message, 'success');
                setTimeout(() => {
                    window.location.href = '/login.html';
                }, 2000);
            } else {
                displayMessage(data.message || 'Registration failed.', 'error');
            }
        } catch (error) {
            displayMessage('An unexpected error occurred.', 'error');
        }
    });

    function displayMessage(message, type) {
        messageDiv.textContent = message;
        messageDiv.className = 'message-display ' + (type === 'success' ? 'success-message' : 'error-message');
        messageDiv.style.display = 'block';
    }
});
