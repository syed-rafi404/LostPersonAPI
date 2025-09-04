document.addEventListener('DOMContentLoaded', () => {
  const token = localStorage.getItem('jwtToken');
  const isAdmin = localStorage.getItem('isAdmin') === 'true';
  if(!token){ window.location.href='/login.html'; return; }
  if(!isAdmin){ window.location.href='/dashboard.html'; return; }

  const tbody = document.getElementById('moderationBody');
  const logoutButton = document.getElementById('logoutButton');
  const refreshBtn = document.getElementById('refreshBtn');
  const msg = document.getElementById('modMessage');

  async function loadReports(){
    tbody.innerHTML = '<tr><td colspan="9">Loading...</td></tr>';
    try {
      const resp = await fetch('/api/MissingPersonReports?pageSize=1000', { headers:{'Authorization':'Bearer '+token}});
      if(!resp.ok){ tbody.innerHTML = '<tr><td colspan="9">Failed to load reports.</td></tr>'; return; }
      const data = await resp.json();
      const reports = data.reports || data.Reports || [];
      tbody.innerHTML='';
      if(reports.length===0){ tbody.innerHTML = '<tr><td colspan="9">No reports.</td></tr>'; return; }
      reports.forEach(r => addRow(r));
    } catch(err){
      console.error(err); tbody.innerHTML = '<tr><td colspan="9">Error.</td></tr>';
    }
  }

  function addRow(r){
    const tr = document.createElement('tr');
    const status = (r.status || r.Status || '').toLowerCase();
    const photo = r.photoUrl || r.PhotoUrl || '/images/default-avatar.png';
    tr.innerHTML = `
      <td>${r.reportID || r.ReportID}</td>
      <td><img class="inline-photo" src="${photo}" onerror="this.onerror=null;this.src='/images/default-avatar.png';"/></td>
      <td><a href="report-detail.html?id=${r.reportID || r.ReportID}" target="_blank">${r.name || r.Name}</a></td>
      <td>${r.age || r.Age || ''}</td>
      <td>${r.gender || r.Gender || ''}</td>
      <td><span class="status-pill ${status}">${r.status || r.Status}</span></td>
      <td>${r.reportingDate ? new Date(r.reportingDate).toLocaleDateString() : ''}</td>
      <td>
        <button class="action-btn approve-btn" data-action="approve" ${status!=='pending' ? 'disabled' : ''}>Approve</button>
        <button class="action-btn decline-btn" data-action="decline" ${status!=='pending' ? 'disabled' : ''}>Decline</button>
        <button class="action-btn" data-action="found" ${status!=='active' ? 'disabled' : ''}>Found</button>
        <button class="action-btn" data-action="close" ${!(status==='active'||status==='found') ? 'disabled' : ''}>Close</button>
        <button class="action-btn decline-btn" data-action="delete" ${status==='pending' ? 'disabled' : ''}>Delete</button>
      </td>`;

    tr.querySelectorAll('button').forEach(btn => btn.addEventListener('click', () => handleAction(btn, r)));
    tbody.appendChild(tr);
  }

  async function handleAction(btn, r){
    const id = r.reportID || r.ReportID;
    const action = btn.getAttribute('data-action');
    btn.disabled = true;
    msg.textContent = action.charAt(0).toUpperCase()+action.slice(1)+ '...';
    try {
      let resp;
      if(action==='delete'){
        resp = await fetch(`/api/MissingPersonReports/${id}`, { method:'DELETE', headers:{'Authorization':'Bearer '+token }});
      } else {
        resp = await fetch(`/api/MissingPersonReports/${id}/${action}`, { method:'POST', headers:{'Authorization':'Bearer '+token }});
      }

      const text = await resp.text(); // read body for diagnostics (may be JSON or plain)
      let payload;
      try { payload = text ? JSON.parse(text) : null; } catch { payload = text; }

      if(resp.ok){
        msg.textContent = payload?.message ? `${payload.message}` : `Action '${action}' completed.`;
        loadReports();
      } else {
        msg.textContent = `Action '${action}' failed (status ${resp.status}) ${payload && payload.detail ? '- '+payload.detail : ''}`;
        console.error('Moderation action error', {status: resp.status, body: payload});
        btn.disabled = false;
      }
    } catch(err){ console.error(err); msg.textContent='Network error.'; btn.disabled=false; }
  }

  refreshBtn.addEventListener('click', loadReports);
  logoutButton.addEventListener('click', e => { e.preventDefault(); localStorage.removeItem('jwtToken'); localStorage.removeItem('isAdmin'); window.location.href='/login.html'; });

  loadReports();
});
