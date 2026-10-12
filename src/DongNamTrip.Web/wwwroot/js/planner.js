"use strict";
(() => {
  const root = document.getElementById('planner'); if (!root) return;
  const form = document.getElementById('itinerary-form'), list = document.getElementById('selected-places');
  const rows = [...document.querySelectorAll('.place-row')], catalog = new Map(rows.map(r => [Number(r.dataset.id), r.dataset]));
  const key = 'dongnamtrip.planner.v1'; let draft = {}; try { draft = JSON.parse(sessionStorage.getItem(key) || '{}') || {}; } catch {}
  const initial = root.dataset.initial.split(',').map(Number).filter(id => catalog.has(id));
  let selected = [...new Set(initial.length ? initial : (Array.isArray(draft.ids) ? draft.ids.filter(id => catalog.has(id)) : []))].slice(0,12);
  if (!initial.length) for (const name of ['Name','StartDate','Guests']) if (draft[name]) form.elements[name].value = draft[name];
  function persist() { try { sessionStorage.setItem(key, JSON.stringify({ ids:selected, Name:form.elements.Name.value, StartDate:form.elements.StartDate.value, Guests:form.elements.Guests.value })); } catch {} }
  function button(text, label, action, disabled) { const b = document.createElement('button'); b.type='button'; b.textContent=text; b.setAttribute('aria-label',label); b.disabled=disabled; b.addEventListener('click',action); return b; }
  function render() {
    list.replaceChildren();
    selected.forEach((id,index) => {
      const place = catalog.get(id), li = document.createElement('li'), text = document.createElement('span'), small = document.createElement('small');
      text.textContent = `${index+1}. ${place.name}`; small.textContent = `${place.minutes} phút · ${place.region}`; text.append(small); li.append(text);
      li.append(button('↑', `Đưa ${place.name} lên`, () => { [selected[index-1],selected[index]]=[selected[index],selected[index-1]]; render(); }, index===0));
      li.append(button('↓', `Đưa ${place.name} xuống`, () => { [selected[index+1],selected[index]]=[selected[index],selected[index+1]]; render(); }, index===selected.length-1));
      li.append(button('×', `Xóa ${place.name}`, () => { selected=selected.filter(x=>x!==id); render(); }, false)); list.append(li);
    });
    for (const row of rows) { const added = selected.includes(Number(row.dataset.id)); const btn = row.querySelector('button'); btn.disabled = added || selected.length >= 12; btn.textContent = added ? '✓' : '+'; }
    document.getElementById('planner-empty').hidden = selected.length > 0;
    document.getElementById('selected-count').textContent = `${selected.length} điểm`;
    document.getElementById('duration-note').textContent = `Tổng tham quan: ${selected.reduce((sum,id)=>sum+Number(catalog.get(id).minutes),0)} phút. Chưa bao gồm di chuyển.`;
    persist();
  }
  for (const row of rows) row.querySelector('button').addEventListener('click',()=>{ const id=Number(row.dataset.id); if(!selected.includes(id) && selected.length<12) selected.push(id); render(); });
  form.addEventListener('input',persist);
  document.getElementById('place-search').addEventListener('input',e=>{ const q=e.target.value.toLocaleLowerCase('vi-VN'); for(const r of rows) r.hidden=!(r.dataset.name+' '+r.dataset.region).toLocaleLowerCase('vi-VN').includes(q); document.getElementById('no-places').hidden=rows.some(r=>!r.hidden); });
  document.getElementById('clear-itinerary').addEventListener('click',()=>{ selected=[]; form.reset(); render(); });
  form.addEventListener('submit',async event=>{
    event.preventDefault(); const message=document.getElementById('planner-message'), submit=document.getElementById('save-itinerary'), login=document.getElementById('planner-login');
    login.hidden=true;
    if(!selected.length){message.textContent='Bạn cần chọn ít nhất một địa điểm.';return;}
    if(root.dataset.authenticated!=='true'){message.textContent='Đăng nhập để lưu lịch trình. Bản nháp được giữ trong tab này.';login.hidden=false;return;}
    submit.disabled=true;message.textContent='Đang lưu lịch trình…';
    try {
      const body=new FormData(form); selected.forEach(id=>body.append('DestinationIds',String(id)));
      const response=await fetch(form.action,{method:'POST',body});
      if(response.redirected || response.status===401){login.hidden=false;throw new Error('Phiên đăng nhập đã hết hạn. Đăng nhập lại để lưu.');}
      const data=await response.json().catch(()=>({message:'Chưa lưu được lịch trình. Vui lòng thử lại.'}));
      if(!response.ok) throw new Error(data.message || 'Chưa lưu được lịch trình.');
      try{sessionStorage.removeItem(key);}catch{} window.location.assign(data.url);
    } catch(error){message.textContent=error.message || 'Chưa kết nối được dịch vụ.';submit.disabled=false;}
  });
  render();
})();
