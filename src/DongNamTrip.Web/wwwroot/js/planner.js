"use strict";
(() => {
  const root = document.getElementById("planner"); if (!root) return;
  const form = document.getElementById("itinerary-form"), list = document.getElementById("selected-places");
  const rows = [...document.querySelectorAll(".place-row")];
  const catalog = new Map(rows.map(row => [Number(row.dataset.id), row.dataset]));
  const activities = [...document.querySelectorAll(".planner-activity")].map(row => row.dataset);
  const slots = [...document.querySelectorAll(".planner-slot")].map(row => row.dataset);
  const key = "dongnamtrip.planner.v2";
  let cached = {}; try { cached = JSON.parse(sessionStorage.getItem(key) || "{}") || {}; } catch {}
  let initial = []; try { initial = JSON.parse(root.dataset.initial || "[]"); } catch {}
  const normalize = value => ({ DestinationId: Number(value.DestinationId), Day: Number(value.Day || 1), Order: Number(value.Order || 1), Note: value.Note || "", SlotIds: Array.isArray(value.SlotIds) ? value.SlotIds.map(Number) : [] });
  let selected = (initial.length || root.dataset.existingId ? initial : cached.stops || []).map(normalize).filter(stop => catalog.has(stop.DestinationId)).slice(0, 12);
  if (!initial.length && !root.dataset.existingId && cached.fields)
    for (const [name, value] of Object.entries(cached.fields)) if (form.elements[name]) form.elements[name].value = value;

  function persist() {
    try { const fields = Object.fromEntries(["Name", "StartDate", "Guests", "DeparturePoint", "Transport", "Budget"].map(name => [name, form.elements[name].value])); sessionStorage.setItem(key, JSON.stringify({ stops: selected, fields })); } catch {}
  }
  function button(text, label, action, disabled = false) {
    const b = document.createElement("button"); b.type = "button"; b.textContent = text; b.setAttribute("aria-label", label); b.disabled = disabled; b.addEventListener("click", action); return b;
  }
  function field(type, value, label, onChange, attrs = {}) {
    const wrap = document.createElement("label"), input = document.createElement("input"); wrap.textContent = label; input.type = type; input.value = value;
    for (const [name, setting] of Object.entries(attrs)) input.setAttribute(name, setting);
    input.addEventListener("change", () => onChange(input.value)); wrap.append(input); return wrap;
  }
  function dateForDay(day) {
    const date = new Date(`${form.elements.StartDate.value}T00:00:00`); if (Number.isNaN(date.getTime())) return "";
    date.setDate(date.getDate() + day - 1);
    return [date.getFullYear(), String(date.getMonth() + 1).padStart(2, "0"), String(date.getDate()).padStart(2, "0")].join("-");
  }
  function render() {
    list.replaceChildren();
    selected.forEach((stop, index) => {
      stop.Order = index + 1;
      const place = catalog.get(stop.DestinationId), li = document.createElement("li"), heading = document.createElement("span"), small = document.createElement("small");
      heading.textContent = `${index + 1}. ${place.name}`; small.textContent = `${place.minutes} phút · ${place.region}`; heading.append(small); li.append(heading);
      li.append(button("↑", `Đưa ${place.name} lên`, () => { [selected[index - 1], selected[index]] = [selected[index], selected[index - 1]]; render(); }, index === 0));
      li.append(button("↓", `Đưa ${place.name} xuống`, () => { [selected[index + 1], selected[index]] = [selected[index], selected[index + 1]]; render(); }, index === selected.length - 1));
      li.append(button("×", `Xóa ${place.name}`, () => { selected = selected.filter(item => item !== stop); render(); }));
      const details = document.createElement("div"); details.className = "planner-stop-details";
      details.append(field("number", stop.Day, "Ngày thứ", value => { stop.Day = Math.max(1, Math.min(30, Number(value) || 1)); render(); }, { min: "1", max: "30" }));
      details.append(field("text", stop.Note, "Ghi chú", value => { stop.Note = value.slice(0, 500); persist(); }, { maxlength: "500" }));
      for (const activity of activities.filter(item => Number(item.destination) === stop.DestinationId)) {
        const caption = document.createElement("small"); caption.textContent = `${activity.name} · ${activity.duration} phút${activity.fee ? ` · tham khảo ${Number(activity.fee).toLocaleString("vi-VN")} ₫` : ""}`; details.append(caption);
        const choices = slots.filter(slot => Number(slot.activity) === Number(activity.id) && slot.date === dateForDay(stop.Day) && slot.status === "Published");
        for (const slot of choices) {
          const label = document.createElement("label"), checkbox = document.createElement("input"); label.className = "planner-slot-choice"; checkbox.type = "checkbox"; checkbox.checked = stop.SlotIds.includes(Number(slot.id));
          checkbox.addEventListener("change", () => { stop.SlotIds = checkbox.checked ? [...stop.SlotIds, Number(slot.id)] : stop.SlotIds.filter(id => id !== Number(slot.id)); persist(); });
          label.append(checkbox, document.createTextNode(` ${slot.date} ${slot.start}–${slot.end} · mong muốn, chưa giữ chỗ`)); details.append(label);
        }
        if (!choices.length) { const hint = document.createElement("small"); hint.textContent = "Chưa có suất công bố cho ngày này."; details.append(hint); }
      }
      li.append(details); list.append(li);
    });
    for (const row of rows) { const added = selected.some(stop => stop.DestinationId === Number(row.dataset.id)), add = row.querySelector("button"); add.disabled = added || selected.length >= 12; add.textContent = added ? "✓" : "+"; }
    document.getElementById("planner-empty").hidden = selected.length > 0;
    document.getElementById("selected-count").textContent = `${selected.length} điểm`;
    document.getElementById("duration-note").textContent = `Tổng tham quan: ${selected.reduce((sum, stop) => sum + Number(catalog.get(stop.DestinationId).minutes), 0)} phút. Chưa bao gồm di chuyển.`;
    persist();
  }
  for (const row of rows) row.querySelector("button").addEventListener("click", () => { const id = Number(row.dataset.id); if (!selected.some(stop => stop.DestinationId === id) && selected.length < 12) selected.push({ DestinationId: id, Day: 1, Order: selected.length + 1, Note: "", SlotIds: [] }); render(); });
  form.addEventListener("input", persist); form.elements.StartDate.addEventListener("change", render);
  document.getElementById("place-search").addEventListener("input", event => { const query = event.target.value.toLocaleLowerCase("vi-VN"); for (const row of rows) row.hidden = !(row.dataset.name + " " + row.dataset.region).toLocaleLowerCase("vi-VN").includes(query); document.getElementById("no-places").hidden = rows.some(row => !row.hidden); });
  document.getElementById("clear-itinerary").addEventListener("click", () => { selected = []; form.reset(); render(); });
  form.addEventListener("submit", async event => {
    event.preventDefault(); const message = document.getElementById("planner-message"), submit = document.getElementById("save-itinerary"), login = document.getElementById("planner-login"); login.hidden = true;
    if (!selected.length) { message.textContent = "Bạn cần chọn ít nhất một địa điểm."; return; }
    if (root.dataset.authenticated !== "true") { message.textContent = "Đăng nhập để lưu lịch trình. Bản nháp được giữ trong tab này."; login.hidden = false; return; }
    submit.disabled = true; message.textContent = "Đang lưu lịch trình…";
    try {
      const body = new FormData(form);
      selected.forEach((stop, index) => { body.append("DestinationIds", String(stop.DestinationId)); body.append(`Stops[${index}].DestinationId`, String(stop.DestinationId)); body.append(`Stops[${index}].Day`, String(stop.Day)); body.append(`Stops[${index}].Order`, String(index + 1)); body.append(`Stops[${index}].Note`, stop.Note); stop.SlotIds.forEach(id => body.append(`Stops[${index}].SlotIds`, String(id))); });
      const response = await fetch(form.action, { method: "POST", body });
      if (response.redirected || response.status === 401) { login.hidden = false; throw new Error("Phiên đăng nhập đã hết hạn. Đăng nhập lại để lưu."); }
      const data = await response.json().catch(() => ({ message: "Chưa lưu được lịch trình. Vui lòng thử lại." }));
      if (!response.ok) throw new Error(data.message || "Chưa lưu được lịch trình.");
      try { sessionStorage.removeItem(key); } catch {} window.location.assign(data.url);
    } catch (error) { message.textContent = error.message || "Chưa kết nối được dịch vụ."; submit.disabled = false; }
  });
  render();
})();
