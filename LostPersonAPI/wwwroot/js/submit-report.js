document.addEventListener('DOMContentLoaded', () => {
    const reportForm = document.getElementById('reportForm');
    const messageDiv = document.getElementById('message');
    const logoutButton = document.getElementById('logoutButton');
    const latInput = document.getElementById('lastSeenLatitude');
    const lonInput = document.getElementById('lastSeenLongitude');
    const photoInput = document.getElementById('photo');

    const token = localStorage.getItem('jwtToken');
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    const map = L.map('map').setView([51.505, -0.09], 13);
    let marker = null;
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    function updateMarker(lat, lng) {
        if (marker) {
            map.removeLayer(marker);
        }
        marker = L.marker([lat, lng]).addTo(map);
        latInput.value = lat.toFixed(6);
        lonInput.value = lng.toFixed(6);
    }

    map.on('click', function (e) {
        updateMarker(e.latlng.lat, e.latlng.lng);
    });

    reportForm.addEventListener('submit', async (event) => {
        event.preventDefault();

        let photoUrl = null;

        if (photoInput.files.length > 0) {
            displayMessage('Uploading photo, please wait...', 'info');
            const formData = new FormData();
            formData.append('file', photoInput.files[0]);

            try {
                const uploadResponse = await fetch('/api/FileUpload', {
                    method: 'POST',
                    headers: { 'Authorization': 'Bearer ' + token },
                    body: formData,
                });

                if (uploadResponse.ok) {
                    const result = await uploadResponse.json();
                    photoUrl = result.url;
                } else {
                    displayMessage('Photo upload failed. Please try again.', 'error');
                    return;
                }
            } catch (error) {
                displayMessage('A network error occurred during photo upload.', 'error');
                return;
            }
        }

        const reportData = {
            name: document.getElementById('name').value,
            age: parseInt(document.getElementById('age').value),
            gender: document.getElementById('gender').value,
            height: parseInt(document.getElementById('height').value),
            weight: parseInt(document.getElementById('weight').value),
            skinColor: document.getElementById('skinColor').value,
            clothing: document.getElementById('clothing').value,
            medicalCondition: document.getElementById('medicalCondition').value,
            lastSeenDate: document.getElementById('lastSeenDate').value,
            lastSeenLatitude: parseFloat(latInput.value),
            lastSeenLongitude: parseFloat(lonInput.value),
            photoUrl: photoUrl,
            eyeColor: document.getElementById('eyeColor').value,
            hairColor: document.getElementById('hairColor').value,
            hasGlasses: document.getElementById('hasGlasses').checked,
            scarsMarks: document.getElementById('scarsMarks').value,
            uniqueCharacteristics: document.getElementById('uniqueCharacteristics').value
        };

        displayMessage('Submitting report for moderation...', 'info');
        try {
            const response = await fetch('/api/MissingPersonReports', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + token
                },
                body: JSON.stringify(reportData),
            });

            if (response.ok) {
                displayMessage('Report submitted and waiting for approval.', 'success');
                reportForm.reset();
                if (marker) {
                    map.removeLayer(marker);
                    marker = null;
                }
            } else {
                const errorData = await response.json();
                displayMessage(errorData.title || 'Submission failed.', 'error');
            }
        } catch (error) {
            displayMessage('An unexpected network error occurred.', 'error');
        }
    });

    logoutButton.addEventListener('click', (e) => {
        e.preventDefault();
        localStorage.removeItem('jwtToken');
        window.location.href = '/login.html';
    });

    function displayMessage(message, type) {
        messageDiv.textContent = message;
        const typeClass = type === 'success' ? 'success-message' : (type === 'error' ? 'error-message' : 'info-message');
        messageDiv.className = 'message-display ' + typeClass;
        messageDiv.style.display = 'block';
    }
});
