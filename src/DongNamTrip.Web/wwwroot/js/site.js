"use strict";
const toggle = document.querySelector('.menu-toggle');
toggle?.addEventListener('click', () => { const open = toggle.getAttribute('aria-expanded') !== 'true'; toggle.setAttribute('aria-expanded', String(open)); document.getElementById('navigation').classList.toggle('open', open); });
const guests = document.getElementById('guest-count');
function estimate() { const panel = document.querySelector('[data-unit-price]'); if (!panel) return; const n = Number(guests.value); document.getElementById('estimate-value').textContent = Number.isInteger(n) && n >= 1 && n <= 40 ? new Intl.NumberFormat('vi-VN', {style:'currency', currency:'VND'}).format(n * Number(panel.dataset.unitPrice)) : 'Chọn từ 1–40 khách'; }
guests?.addEventListener('input', estimate); estimate();
const tabs = [...document.querySelectorAll('[data-tab]')];
function selectTab(tab) { for (const button of tabs) { const active = button === tab; button.classList.toggle('current', active); button.setAttribute('aria-selected', String(active)); button.tabIndex = active ? 0 : -1; document.getElementById(button.dataset.tab).hidden = !active; } }
for (const tab of tabs) { tab.addEventListener('click', () => selectTab(tab)); tab.addEventListener('keydown', event => { if (!['ArrowLeft','ArrowRight','Home','End'].includes(event.key)) return; event.preventDefault(); let i = tabs.indexOf(tab); i = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (i + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length; selectTab(tabs[i]); tabs[i].focus(); }); }
if (tabs.length) selectTab(tabs[0]);
