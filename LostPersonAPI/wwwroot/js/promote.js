// Admin-only user promotion script
(function(){
  const token = localStorage.getItem('jwtToken');
  const isAdmin = localStorage.getItem('isAdmin') === 'true';
  const form = document.getElementById('promoteForm');
  const msg = document.getElementById('promoteMessage');
  const box = document.getElementById('promotionBox');
  if(!form || !isAdmin){ if(box) box.style.display='none'; return; }

  form.addEventListener('submit', async (e)=>{
    e.preventDefault();
    msg.textContent='Promoting...';
    const username = document.getElementById('promoteUsername').value.trim();
    if(!username){ msg.textContent='Enter a username.'; return; }
    try {
      const resp = await fetch(`/api/admin/promote/${encodeURIComponent(username)}`, { method:'POST', headers:{'Authorization':'Bearer '+token }});
      if(resp.ok){ msg.textContent = 'User promoted (if existed).'; }
      else if(resp.status===404){ msg.textContent='User not found.'; }
      else if(resp.status===403){ msg.textContent='Not authorized.'; }
      else { msg.textContent='Promotion failed.'; }
    } catch(err){ console.error(err); msg.textContent='Network error.'; }
  });
})();
