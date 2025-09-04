document.addEventListener('DOMContentLoaded', () => {
    const logoutButton = document.getElementById('logoutButton');
    const token = localStorage.getItem('jwtToken');
    if (!token) { window.location.href = '/login.html'; return; }

    const params = new URLSearchParams(window.location.search);
    const id = params.get('id');
    const nameEl = document.getElementById('personName');
    const detailContent = document.getElementById('detailContent');

    const imageModal = document.getElementById('imageModal');
    const modalImg = document.getElementById('modalFullImage');
    const closeModalBtn = document.getElementById('closeImageModal');

    if (!id) { detailContent.textContent = 'No report id provided.'; return; }

    async function loadDetail() {
        detailContent.innerHTML = 'Loading...';
        try {
            const resp = await fetch(`/api/MissingPersonReports/${id}`, { headers: { 'Authorization': 'Bearer ' + token }});
            if(!resp.ok){ detailContent.textContent = 'Failed to load report.'; return; }
            const r = await resp.json();

            nameEl.textContent = r.name || r.Name || 'Unknown';
            const name = nameEl.textContent;

            const photoUrl = r.photoUrl || r.PhotoUrl || '/images/default-avatar.png';
            const status = (r.status || r.Status || 'Unknown');
            const statusClass = 'badge-status ' + status.toLowerCase();

            const heightDisplay = r.height ? `${r.height} cm` : '—';
            const weightDisplay = r.weight ? `${r.weight} kg` : '—';
            const lastSeenDate = r.lastSeenDate || r.LastSeenDate;            
            const lastSeenStr = lastSeenDate ? new Date(lastSeenDate).toLocaleDateString() : '—';

            const clothing = (r.clothing || r.Clothing) || 'Not specified';
            const scarsMarks = (r.scarsMarks || r.ScarsMarks) || null;
            const uniqueChars = (r.uniqueCharacteristics || r.UniqueCharacteristics) || null;
            const identifying = [scarsMarks, uniqueChars].filter(Boolean).join('\n');

            const eyeColor = r.eyeColor || r.EyeColor || '—';
            const hairColor = r.hairColor || r.HairColor || '—';
            const hasGlasses = (r.hasGlasses ?? r.HasGlasses) ? 'Yes' : 'No';

            // Inject layout: two existing columns + new timeline column container
            detailContent.innerHTML = `
              <div class="detail-grid">
                <div>
                  <div class="facts-list">
                    <p><strong>Age:</strong> ${r.age ?? r.Age ?? '—'}</p>
                    <p><strong>Sex:</strong> ${r.gender || r.Gender || '—'}</p>
                    <p><strong>Eyes:</strong> ${eyeColor}</p>
                    <p><strong>Hair:</strong> ${hairColor}</p>
                    <p><strong>Glasses:</strong> ${hasGlasses}</p>
                    <p><strong>Height:</strong> ${heightDisplay}</p>
                    <p><strong>Weight:</strong> ${weightDisplay}</p>
                    <p><strong>Wearing:</strong> ${clothing}</p>
                    ${identifying ? `<p><strong>Identifying Characteristics:</strong><br>${identifying.replace(/\n/g,'<br>')}</p>` : ''}
                    ${r.medicalCondition ? `<p><strong>Medical:</strong> ${r.medicalCondition}</p>` : ''}
                    <p class="last-seen-line"><strong>LAST SEEN:</strong> ${lastSeenStr}</p>
                  </div>
                  <div class="meta-row">
                    <span class="${statusClass}">${status}</span>
                    ${r.reportingDate ? `<span>Reported: ${new Date(r.reportingDate).toLocaleDateString()}</span>` : ''}
                  </div>
                  <div class="highlight-box" id="contactBox">If you have any information about ${name}, please contact local authorities or emergency services.</div>
                  <div class="actions-bar no-print">
                    <a class="btn-secondary" href="dashboard.html">Back to Dashboard</a>
                    <button class="btn-secondary" id="printBtn" type="button">Print</button>
                    <button class="btn-secondary" id="openImageBtn" type="button">View Photo</button>
                  </div>
                </div>
                <div>
                  <img id="inlinePhoto" class="person-photo print-photo" src="${photoUrl}" alt="Photo of ${name}" onerror="this.onerror=null;this.src='/images/default-avatar.png';" />
                </div>
                <div id="timelinePanel">
                  <div id="timelineHeader">
                    <h3 style="margin:0;font-size:1rem;">Timeline</h3>
                    <span class="tl-badge" id="tlCount">0</span>
                  </div>
                  <div class="scroll-hint">Newest at bottom</div>
                  <div id="timelineItems"></div>
                  <form id="timelineForm">
                    <textarea id="timelineMessage" maxlength="2000" placeholder="Share info or sightings..."></textarea>
                    <button type="submit" class="btn-primary" style="width:auto;min-width:90px;">Post</button>
                  </form>
                  <div id="tlStatus"></div>
                </div>
              </div>`;

            const preload = new Image();
            preload.src = photoUrl;

            document.getElementById('printBtn').addEventListener('click', ()=> {
                if (preload.complete) { window.print(); }
                else { preload.onload = () => window.print(); }
            });
            document.getElementById('openImageBtn').addEventListener('click', ()=> openModal(photoUrl, name));
            document.getElementById('inlinePhoto').addEventListener('click', ()=> openModal(photoUrl, name));

            // Initialize timeline after layout ready
            initTimeline(parseInt(id,10), token);
        } catch(err){
            console.error(err);
            detailContent.textContent = 'An unexpected error occurred.';
        }
    }

    function openModal(src, alt){ modalImg.src = src; modalImg.alt = alt; imageModal.classList.add('open'); imageModal.setAttribute('aria-hidden','false'); }
    function closeModal(){ imageModal.classList.remove('open'); imageModal.setAttribute('aria-hidden','true'); modalImg.src=''; }

    closeModalBtn.addEventListener('click', closeModal);
    imageModal.addEventListener('click', (e)=>{ if(e.target===imageModal) closeModal(); });
    document.addEventListener('keydown', (e)=>{ if(e.key==='Escape' && imageModal.classList.contains('open')) closeModal(); });

    logoutButton.addEventListener('click', e => { e.preventDefault(); localStorage.removeItem('jwtToken'); window.location.href='/login.html'; });

    loadDetail();

    window.addEventListener('beforeprint', () => {
        document.querySelectorAll('img.print-photo').forEach(img => { if(!img.complete){ const clone = new Image(); clone.src = img.src; } img.style.visibility='visible'; });
    });
});

// Timeline logic (isolated function so we can call after detail loads)
function initTimeline(reportId, token){
  const itemsEl = document.getElementById('timelineItems');
  const form = document.getElementById('timelineForm');
  const textarea = document.getElementById('timelineMessage');
  const statusEl = document.getElementById('tlStatus');
  const countBadge = document.getElementById('tlCount');
  let lastId = 0;
  let loading = false;
  let pollHandle;

  async function load(initial=false){
    if(loading) return; loading = true;
    statusEl.textContent = initial ? 'Loading timeline...' : '';
    try {
      const resp = await fetch(`/api/Timeline/${reportId}?afterId=${initial?0:lastId}`, { headers:{'Authorization':'Bearer '+token} });
      if(!resp.ok){ statusEl.textContent='Failed to load timeline'; loading=false; return; }
      const data = await resp.json();
      const arr = data.items || [];
      if(initial){ itemsEl.innerHTML=''; lastId=0; }
      arr.forEach(item => {
        lastId = Math.max(lastId, item.id);
        const div = document.createElement('div');
        div.className='tl-item';
        div.innerHTML = `<div class="tl-meta"><span>${escapeHtml(item.username)}</span><span>${new Date(item.createdAt).toLocaleString()}</span></div><div class="tl-msg">${escapeHtml(item.message)}</div>`;
        itemsEl.appendChild(div);
      });
      countBadge.textContent = itemsEl.children.length;
      if(arr.length>0) itemsEl.scrollTop = itemsEl.scrollHeight;
      if(initial && arr.length===0){ itemsEl.innerHTML='<div style="font-size:.7rem;color:#777;padding:.25rem;">No updates yet. Be the first to add information.</div>'; }
      statusEl.textContent='';
    } catch(err){ console.error(err); statusEl.textContent='Timeline error'; }
    loading=false;
  }

  form.addEventListener('submit', async e => {
    e.preventDefault();
    const msg = textarea.value.trim();
    if(!msg){ return; }
    if(msg.length>2000){ statusEl.textContent='Message too long'; return; }
    statusEl.textContent='Posting...';
    try {
      const resp = await fetch('/api/Timeline', { method:'POST', headers:{'Content-Type':'application/json','Authorization':'Bearer '+token}, body:JSON.stringify({ reportId, message: msg }) });
      if(resp.ok){ textarea.value=''; await load(); statusEl.textContent='Posted'; setTimeout(()=>statusEl.textContent='',1200); }
      else { const t = await resp.text(); statusEl.textContent='Post failed '+t; }
    } catch(err){ console.error(err); statusEl.textContent='Network error'; }
  });

  function startPolling(){
    pollHandle = setInterval(()=> load(false), 5000);
  }
  function stopPolling(){ if(pollHandle) clearInterval(pollHandle); }
  document.addEventListener('visibilitychange', ()=>{ if(document.hidden) stopPolling(); else startPolling(); });

  load(true).then(startPolling);
}

function escapeHtml(str){ return str.replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;','\'':'&#39;'}[c])); }
