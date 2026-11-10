export type ItemKind = 'tour' | 'destination';
export type User = { id: number; name: string; email: string; roles: string[] };
export type Destination = { id: number; name: string; region: string; category: string; address: string; description: string; visitMinutes: number; illustration: string; status?: string };
export type Tour = { id: number; name: string; region: string; category: string; duration: string; referencePrice: number; summary: string; illustration: string; destinationIds: number[] };
export type ItineraryStop = { destinationId: number; day: number; order: number; note: string | null; slotIds: number[] };
export type Itinerary = { id: number; userId: number; name: string; startDate: string; guests: number; destinationIds: number[]; destinationNames: string[]; totalVisitMinutes: number; departurePoint: string | null; transport: string | null; budget: number | null; updatedAt: string; stops: ItineraryStop[]; warnings: string[] };
export type PlannerCatalog = { destinations: Destination[]; activities: { id: number; destinationId: number; name: string; durationMinutes: number; conditions: string | null; referenceFee: number | null }[]; slots: { id: number; activityId: number; destinationId: number; date: string; startsAt: string; endsAt: string; status: string }[] };
export type PrivateTourQuote = { id: number; requestId: number; itineraryVersion: number; total: number; amountDue: number; cancellationTerms: string; expiresAt: string; status: string; lines: { id: number; description: string; unit: string; quantity: number; unitPrice: number }[]; orderId: number | null };
export type PrivateTourRequest = { id: number; itineraryId: number; version: number; customerId: number; customerName: string; contact: string; serviceNeeds: string | null; submittedAt: string; status: string; snapshot: Itinerary; verifications: { id: number; item: string; supplier: string; result: string; note: string | null; checkedAt: string }[]; quotes: PrivateTourQuote[] };
export type Paged<T> = { items: T[]; total: number; page: number; pageSize: number };
export type TourDetail = { tour: Tour; places: Destination[] };
export type TourEstimate = { guests: number; total: number };
export type ItineraryPreview = { places: Destination[]; totalVisitMinutes: number };
export type Meta = {
  regions: { name: string; illustration: string }[]; categories: string[];
  tourPageSize: number; destinationPageSize: number; plannerListSize: number;
  itineraryNameMax: number; guestsMin: number; guestsMax: number; maxPlaces: number;
  nameMin: number; nameMax: number; passwordMin: number; passwordMax: number; phoneMax: number;
  paymentMethods: string[];
};
export type Profile = { id: number; name: string; email: string; phoneNumber: string | null };
export type Session = { id: string; createdAt: string; expiresAt: string; isCurrent: boolean };
export type Money = { id: number; kind: string; amount: number; at: string; reference: string };
export type Payment = { id: number; placedAt: string; status: string; agreedPrice: number; guests: number; quoteId: number | null; departureId: number | null; transactions: Money[] };
export type PaymentChoice = { methodCode: string | null; updatedAt: string | null };
export type CollectionItem = { kind: ItemKind; itemId: number; name: string | null; at: string; missing: boolean };
export type Activity = { id: number; kind: 'search' | ItemKind; itemId: number | null; query: string | null; name: string | null; at: string; missing: boolean };
export type Collections = { favorites: CollectionItem[]; searches: Activity[]; viewed: Activity[] };
export type Notice = { id: number; title: string; body: string; at: string; readAt: string | null };
export type Preference = { securityNotices: boolean };
export type FieldErrors = Record<string, string[]>;
export type Slot = { id: number; activityId: number; date: string; start: string; end: string; status: string };
export type ActivityInfo = {
  id: number; destinationId: number; name: string; description: string | null; durationMinutes: number;
  conditions: string | null; referenceFee: number | null; status: string; slots: Slot[];
};
export type DestinationDetail = Destination & {
  openTime: string | null; closeTime: string | null; latitude: number | null; longitude: number | null; activities: ActivityInfo[];
};
export type ListenTrack = {
  audioId: number; destinationId: number; destinationName: string; languageCode: string; languageName: string;
  title: string; voice: string; transcript: string;
};
export type ListenCatalog = { hasAccess: boolean; validUntil: string | null; message: string; tracks: ListenTrack[] };
