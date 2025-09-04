document.addEventListener('DOMContentLoaded', () => {
    // --- Element References ---
    const searchForm = document.getElementById('searchForm');
    const reportsGrid = document.getElementById('reportsGrid');
    const resultsCount = document.getElementById('resultsCount');
    const logoutButton = document.getElementById('logoutButton');
    const clearButton = document.getElementById('clearButton');
    const prevButton = document.getElementById('prevButton');
    const nextButton = document.getElementById('nextButton');
    const pageInfo = document.getElementById('pageInfo');

    // --- State Management ---
    let currentPage = 1;
    const pageSize = 8; // Show 8 reports per page

    // --- Authentication ---
    const token = localStorage.getItem('jwtToken');
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    // --- Core Functions ---
    async function performSearch(page = 1) {
        currentPage = page;
        const name = document.getElementById('searchName').value;
        const status = document.getElementById('searchStatus').value;
        const minAge = document.getElementById('searchMinAge').value;
        const maxAge = document.getElementById('searchMaxAge').value;
        const gender = document.getElementById('searchGender').value;

        const params = new URLSearchParams({
            pageNumber: currentPage,
            pageSize: pageSize
        });
        if (name) params.append('name', name);
        if (status) params.append('status', status);
        if (minAge) params.append('minAge', minAge);
        if (maxAge) params.append('maxAge', maxAge);
        if (gender) params.append('gender', gender);

        reportsGrid.innerHTML = '<p>Searching...</p>';
        resultsCount.textContent = '';
        pageInfo.textContent = '';

        try {
            const response = await fetch(`/api/MissingPersonReports?${params.toString()}`, {
                method: 'GET',
                headers: { 'Authorization': 'Bearer ' + token }
            });

            if (response.ok) {
                const data = await response.json();
                displayResults(data);
            } else {
                reportsGrid.innerHTML = `<p class="error-message">Search failed. Please try again.</p>`;
            }
        } catch (error) {
            console.error('Search error:', error);
        }
    }

    function displayResults(data) {
        reportsGrid.innerHTML = '';
        const { reports, totalRecords, totalPages, currentPage } = data;
        resultsCount.textContent = `${totalRecords} report(s) found.`;

        if (!reports || reports.length === 0) {
            reportsGrid.innerHTML = `<p>No reports match your search criteria.</p>`;
            return;
        }

        reports.forEach(report => {
            const id = report.reportID || report.ReportID;
            const link = document.createElement('a');
            link.href = `report-detail.html?id=${encodeURIComponent(id)}`;
            link.className = 'report-card-link';

            const card = document.createElement('div');
            card.className = 'report-card';
            const photoUrl = report.photoUrl || report.PhotoUrl || '/images/default-avatar.png';
            const lastSeenDate = report.lastSeenDate || report.LastSeenDate;
            const status = report.status || report.Status || 'Unknown';

            card.innerHTML = `
                <div class="card-photo">
                    <img src="${photoUrl}" alt="Photo of ${report.name || report.Name}" onerror="this.onerror=null;this.src='/images/default-avatar.png';">
                </div>
                <div class="card-details">
                    <h3>${report.name || report.Name}</h3>
                    <p><strong>Age:</strong> ${report.age || report.Age || 'N/A'} | <strong>Gender:</strong> ${report.gender || report.Gender || 'N/A'}</p>
                    <p><strong>Last Seen:</strong> ${lastSeenDate ? new Date(lastSeenDate).toLocaleDateString() : 'N/A'}</p>
                    <span class="status-badge status-${status.toLowerCase()}">${status}</span>
                </div>
            `;
            link.appendChild(card);
            reportsGrid.appendChild(link);
        });

        // Update pagination controls
        pageInfo.textContent = `Page ${currentPage} of ${totalPages}`;
        prevButton.disabled = currentPage <= 1;
        nextButton.disabled = currentPage >= totalPages;
    }

    // --- Event Listeners ---
    searchForm.addEventListener('submit', (e) => {
        e.preventDefault();
        performSearch(1); // Always go to page 1 for a new search
    });

    clearButton.addEventListener('click', () => {
        searchForm.reset();
        performSearch(1);
    });

    prevButton.addEventListener('click', () => {
        if (currentPage > 1) {
            performSearch(currentPage - 1);
        }
    });

    nextButton.addEventListener('click', () => {
        performSearch(currentPage + 1);
    });

    logoutButton.addEventListener('click', (e) => {
        e.preventDefault();
        localStorage.removeItem('jwtToken');
        window.location.href = '/login.html';
    });

    // --- Initial Page Load ---
    performSearch(1);
});
