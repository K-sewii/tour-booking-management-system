// Add an itinerary day using local calendar arithmetic, avoiding UTC date shifts.
export function dateOnDay(startDate: string, day: number): string {
  const date = new Date(startDate + 'T12:00:00');
  date.setDate(date.getDate() + day - 1);
  return date.toLocaleDateString('sv-SE');
}
