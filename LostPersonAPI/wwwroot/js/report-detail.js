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
              </div>`;

            const preload = new Image();
            preload.src = photoUrl;

            document.getElementById('printBtn').addEventListener('click', ()=> {
                if (preload.complete) { window.print(); }
                else { preload.onload = () => window.print(); }
            });
            document.getElementById('openImageBtn').addEventListener('click', ()=> openModal(photoUrl, name));
            document.getElementById('inlinePhoto').addEventListener('click', ()=> openModal(photoUrl, name));
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
