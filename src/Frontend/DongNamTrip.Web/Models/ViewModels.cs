using DongNamTrip.Contracts;

namespace DongNamTrip.Web.Models;
public record HomeVm(List<Tour> Tours, List<Destination> Destinations);
public record TourListVm(List<Tour> Tours, string? Query, string? Region, string? Category, string? Sort, int Page, int TotalPages, int TotalCount);
public record TourDetailVm(Tour Tour, List<Destination> Destinations);
public record DestinationListVm(List<Destination> Destinations, string? Query, string? Region, string? Category);
public record PlannerVm(List<Destination> Destinations, int[] InitialIds);
public record TripsVm(List<Itinerary> Itineraries, List<Destination> Destinations);
