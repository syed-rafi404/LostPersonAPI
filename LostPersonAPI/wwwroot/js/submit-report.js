document.addEventListener('DOMContentLoaded', () => {
    // --- Element References ---
    const reportForm = document.getElementById('reportForm');
    const messageDiv = document.getElementById('message');
    const logoutButton = document.getElementById('logoutButton');
    const latInput = document.getElementById('lastSeenLatitude');
    const lonInput = document.getElementById('lastSeenLongitude');
    const photoInput = document.getElementById('photo'); // The file input

    // --- Authentication Check ---
    const token = localStorage.getItem('jwtToken');
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    // --- Leaflet Map Initialization ---
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

    // --- Main Form Submission Logic ---
    reportForm.addEventListener('submit', async (event) => {
        event.preventDefault();

        let photoUrl = null;

        // Step 1: Upload the photo if one is selected
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
                    photoUrl = result.url; // Capture the URL from the server
                } else {
                    displayMessage('Photo upload failed. Please try again.', 'error');
                    return; // Stop if photo upload fails
                }
            } catch (error) {
                console.error('Error uploading photo:', error);
                displayMessage('A network error occurred during photo upload.', 'error');
                return;
            }
        }

        // Step 2: Gather all report data, including the new photo URL
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
            photoUrl: photoUrl, // Add the photo URL to the report object
            eyeColor: document.getElementById('eyeColor').value,
            hairColor: document.getElementById('hairColor').value,
            hasGlasses: document.getElementById('hasGlasses').checked,
            scarsMarks: document.getElementById('scarsMarks').value,
            uniqueCharacteristics: document.getElementById('uniqueCharacteristics').value
        };

        // Step 3: Submit the complete report data
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
            console.error('Error submitting report:', error);
            displayMessage('An unexpected network error occurred.', 'error');
        }
    });

    // --- Helper Functions ---
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
