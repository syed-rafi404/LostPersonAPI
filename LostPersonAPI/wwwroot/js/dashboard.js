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
            const response = await fetch(`/api/MissingPersonReports?status=${status}&pageSize=1000`, {
                method: 'GET',
                headers: { 'Authorization': 'Bearer ' + token }
            });

            if (response.ok) {
                const data = await response.json();
                // Handle both PascalCase (API default) and camelCase (if serializer changed) plus raw array fallback
                const reports = data.reports || data.Reports || (Array.isArray(data) ? data : []);
                displayReports(reports, status); 
            } else if (response.status === 401) {
                reportsGrid.innerHTML = `<p class="error-message">Unauthorized. Please log in again.</p>`;
                localStorage.removeItem('jwtToken');
                setTimeout(()=>window.location.href='/login.html',1500);
            } else {
                reportsGrid.innerHTML = `<p class="error-message">Could not load '${status}' reports.</p>`;
            }
        } catch (error) {
            console.error(`Error fetching ${status} reports:`, error);
            reportsGrid.innerHTML = `<p class="error-message">Network error loading reports.</p>`;
        }
    }

    /**
     * Renders the report cards into the grid.
     * @param {Array} reports - The array of report objects to display.
     */
    function displayReports(reports, status) {
        reportsGrid.innerHTML = '';
        if (!Array.isArray(reports) || reports.length === 0) {
            reportsGrid.innerHTML = `<p>No reports with status '${status}'.</p>`;
            return;
        }

        reports.forEach(report => {
            const id = report.reportID || report.ReportID;
            const card = document.createElement('a');
            card.href = `report-detail.html?id=${encodeURIComponent(id)}`;
            card.className = 'report-card-link';

            const inner = document.createElement('div');
            inner.className = 'report-card';
            const photoUrl = report.photoUrl || report.PhotoUrl || '/images/default-avatar.png';
            const reportStatus = report.status || report.Status || 'Unknown';
            const lastSeenDate = report.lastSeenDate || report.LastSeenDate;

            inner.innerHTML = `
                <div class="card-photo">
                    <img src="${photoUrl}" alt="Photo of ${report.name || report.Name}" onerror="this.onerror=null;this.src='/images/default-avatar.png';">
                </div>
                <div class="card-details">
                    <h3>${report.name || report.Name}</h3>
                    <p><strong>Age:</strong> ${report.age || report.Age || 'N/A'}</p>
                    <p><strong>Last Seen:</strong> ${lastSeenDate ? new Date(lastSeenDate).toLocaleDateString() : 'N/A'}</p>
                    <span class="status-badge status-${reportStatus.toLowerCase()}">${reportStatus}</span>
                </div>`;
            card.appendChild(inner);
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
