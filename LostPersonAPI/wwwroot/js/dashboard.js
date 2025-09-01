document.addEventListener('DOMContentLoaded', () => {
    // --- Element References ---
    const reportsGrid = document.getElementById('reportsGrid');
    const logoutButton = document.getElementById('logoutButton');
    const filterTabs = document.querySelector('.filter-tabs');

    // --- Authentication ---
    const token = localStorage.getItem('jwtToken');
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    // --- Core Functions ---

    /**
     * Fetches reports from the API based on the selected status.
     * @param {string} status - The status to filter by (e.g., 'Active').
     */
    async function fetchReports(status = 'Active') {
        reportsGrid.innerHTML = '<p>Loading reports...</p>';
        try {
            // **THE FIX - PART 1:**
            // We add a large pageSize to get all reports for the dashboard view.
            const response = await fetch(`/api/MissingPersonReports?status=${status}&pageSize=1000`, {
                method: 'GET',
                headers: { 'Authorization': 'Bearer ' + token }
            });

            if (response.ok) {
                // **THE FIX - PART 2:**
                // We now expect a data object and need to get the .reports property from it.
                const data = await response.json();
                displayReports(data.reports, status); // Pass the inner array to the display function
            } else {
                reportsGrid.innerHTML = `<p class="error-message">Could not load '${status}' reports.</p>`;
            }
        } catch (error) {
            console.error(`Error fetching ${status} reports:`, error);
        }
    }

    /**
     * Renders the report cards into the grid.
     * @param {Array} reports - The array of report objects to display.
     */
    function displayReports(reports, status) {
        reportsGrid.innerHTML = '';
        if (reports.length === 0) {
            reportsGrid.innerHTML = `<p>No reports with status '${status}'.</p>`;
            return;
        }

        reports.forEach(report => {
            const card = document.createElement('div');
            card.className = 'report-card';
            const photoUrl = report.photoUrl || '/images/default-avatar.png';

            card.innerHTML = `
                <div class="card-photo">
                    <img src="${photoUrl}" alt="Photo of ${report.name}" onerror="this.onerror=null;this.src='/images/default-avatar.png';">
                </div>
                <div class="card-details">
                    <h3>${report.name}</h3>
                    <p><strong>Age:</strong> ${report.age}</p>
                    <p><strong>Last Seen:</strong> ${new Date(report.lastSeenDate).toLocaleDateString()}</p>
                    <span class="status-badge status-${report.status.toLowerCase()}">${report.status}</span>
                </div>
            `;
            reportsGrid.appendChild(card);
        });
    }

    // --- Event Listeners ---
    filterTabs.addEventListener('click', (e) => {
        if (e.target.classList.contains('tab-button')) {
            document.querySelectorAll('.tab-button').forEach(btn => btn.classList.remove('active'));
            e.target.classList.add('active');
            fetchReports(e.target.dataset.status);
        }
    });

    logoutButton.addEventListener('click', (e) => {
        e.preventDefault();
        localStorage.removeItem('jwtToken');
        window.location.href = '/login.html';
    });

    // --- Initial Page Load ---
    fetchReports('Active');
});
