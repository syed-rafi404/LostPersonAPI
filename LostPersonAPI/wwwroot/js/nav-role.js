// Dynamically hide/show Moderation link & add notifications
(function(){
  const token = localStorage.getItem('jwtToken');
  const isAdmin = localStorage.getItem('isAdmin') === 'true';
  const nav = document.querySelector('nav');
  if(!nav) return;
  const logoImg = document.querySelector('.logo img');
  if(logoImg && token){ logoImg.addEventListener('click', ()=>{ window.location.href='dashboard.html'; }); }
  let modLink = Array.from(nav.querySelectorAll('a')).find(a=>/moderation\.html$/i.test(a.getAttribute('href')||''));
  if(!isAdmin){ if(modLink) modLink.remove(); } else { if(!modLink){ const a=document.createElement('a'); a.href='moderation.html'; a.textContent='MODERATION'; nav.insertBefore(a, nav.querySelector('#logoutButton')); } }
  // Inject notifications script after nav ready
  if(token){
    const s = document.createElement('script');
    s.src='js/notifications.js';
    document.body.appendChild(s);
  }
})();
