(function(){
  const token = localStorage.getItem('jwtToken');
  if(!token) return;
  const nav = document.querySelector('nav');
  if(!nav) return;
  // Create bell container
  let bellWrap = document.getElementById('notifBellWrap');
  if(!bellWrap){
    bellWrap = document.createElement('div');
    bellWrap.id='notifBellWrap';
    bellWrap.innerHTML = `<button id="notifBell" aria-label="Notifications" type="button"><img src="/images/notification.png" alt="Notifications"/><span id="notifBadge" class="notif-badge" hidden>0</span></button><div id="notifDropdown" class="notif-dropdown" hidden><div class="notif-head"><span>Notifications</span><button id="notifMarkAll" type="button">Mark All Read</button></div><div id="notifList" class="notif-list"></div><div class="notif-foot"><button id="notifClose" type="button">Close</button></div></div>`;
    nav.appendChild(bellWrap);
  }
  const bellBtn = document.getElementById('notifBell');
  const badge = document.getElementById('notifBadge');
  const dropdown = document.getElementById('notifDropdown');
  const listEl = document.getElementById('notifList');
  const markAllBtn = document.getElementById('notifMarkAll');
  const closeBtn = document.getElementById('notifClose');

  let cache = [];
  let unreadIds = new Set();
  let open=false; let polling;

  function fmtDate(d){ return new Date(d).toLocaleString(); }
  function buildItem(n){
    const div = document.createElement('div');
    div.className='notif-item'+(n.isRead?'' :' unread');
    div.dataset.id = n.id;
    div.innerHTML = `<div class="notif-msg">${escapeHtml(n.message)}</div><div class="notif-meta">${escapeHtml(n.type)} • ${fmtDate(n.createdAt)}</div>`;
    div.addEventListener('click', ()=>{
      if(n.reportId){ window.location.href = `report-detail.html?id=${n.reportId}`; }
      markRead([n.id]);
    });
    return div;
  }

  async function fetchNotifications(){
    try {
      const resp = await fetch('/api/notifications?unreadOnly=false&limit=40',{ headers:{'Authorization':'Bearer '+token}});
      if(!resp.ok) return;
      const data = await resp.json();
      cache = data;
      unreadIds = new Set(data.filter(n=>!n.isRead).map(n=>n.id));
      if(unreadIds.size>0){ badge.textContent = unreadIds.size > 99 ? '99+' : unreadIds.size; badge.hidden=false; } else { badge.hidden=true; }
      if(open) renderList();
    } catch(e){ /* ignore */ }
  }

  function renderList(){
    listEl.innerHTML='';
    if(cache.length===0){ listEl.innerHTML='<div class="notif-empty">No notifications</div>'; return; }
    cache.forEach(n=> listEl.appendChild(buildItem(n)) );
  }

  async function markRead(ids){
    if(ids.length===0) return;
    try { await fetch('/api/notifications/mark-read',{method:'POST', headers:{'Content-Type':'application/json','Authorization':'Bearer '+token}, body:JSON.stringify({ids})}); }
    catch(e){}
    ids.forEach(id=> unreadIds.delete(id));
    if(unreadIds.size===0) badge.hidden=true; else badge.textContent = unreadIds.size > 99 ? '99+' : unreadIds.size;
    cache.forEach(n=>{ if(ids.includes(n.id)) n.isRead=true; });
    if(open) renderList();
  }

  bellBtn.addEventListener('click', (e)=>{ e.stopPropagation(); open=!open; dropdown.hidden=!open; if(open){ renderList(); }});
  markAllBtn.addEventListener('click', ()=> markRead(Array.from(unreadIds)));
  closeBtn.addEventListener('click', ()=> { open=false; dropdown.hidden=true; });
  document.addEventListener('click', ()=>{ if(open){ open=false; dropdown.hidden=true; }});
  dropdown.addEventListener('click', e=> e.stopPropagation());

  function startPoll(){ polling = setInterval(fetchNotifications, 7000); }
  function stopPoll(){ if(polling) clearInterval(polling); }
  document.addEventListener('visibilitychange', ()=>{ if(document.hidden) stopPoll(); else { fetchNotifications(); startPoll(); }});
  fetchNotifications(); startPoll();
})();

function escapeHtml(str){ return str.replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;','\'':'&#39;'}[c])); }
