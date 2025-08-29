document.addEventListener('DOMContentLoaded', () => {
    const reportForm = document.getElementById('reportForm');
    const messageDiv = document.getElementById('message');
    const logoutButton = document.getElementById('logoutButton');
    const latInput = document.getElementById('lastSeenLatitude');
    const lonInput = document.getElementById('lastSeenLongitude');

    // --- MAP INITIALIZATION ---
    const map = L.map('map').setView([51.505, -0.09], 13); // Default view (London)
    let marker = null;

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    // Function to update marker and form fields
    function updateMarker(lat, lng) {
        latInput.value = lat.toFixed(6);
        lonInput.value = lng.toFixed(6);

        if (marker) {
            marker.setLatLng([lat, lng]);
        } else {
            marker = L.marker([lat, lng], { draggable: true }).addTo(map);

            // Add drag event listener to the marker
            marker.on('dragend', function (event) {
                const position = marker.getLatLng();
                updateMarker(position.lat, position.lng);
            });
        }
        map.panTo([lat, lng]);
    }

    // Listen for clicks on the map
    map.on('click', function (e) {
        updateMarker(e.latlng.lat, e.latlng.lng);
    });
    // --- END OF MAP LOGIC ---


    // --- Authentication and Form Submission Logic (mostly unchanged) ---
    const token = localStorage.getItem('jwtToken');
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    reportForm.addEventListener('submit', async (event) => {
        event.preventDefault();

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
        };

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
                displayMessage('Report submitted successfully!', 'success');
                reportForm.reset();
                if (marker) {
                    map.removeLayer(marker);
                    marker = null;
                }
            } else {
                const errorData = await response.json();
                const errorMessage = errorData.title || 'Submission failed.';
                displayMessage(errorMessage, 'error');
            }
        } catch (error) {
            console.error('Error submitting report:', error);
            displayMessage('An unexpected network error occurred.', 'error');
        }
    });

    logoutButton.addEventListener('click', () => {
        localStorage.removeItem('jwtToken');
        window.location.href = '/login.html';
    });

    function displayMessage(message, type) {
        messageDiv.textContent = message;
        messageDiv.className = 'message-display ' + (type === 'success' ? 'success-message' : 'error-message');
        messageDiv.style.display = 'block';
    }
});
