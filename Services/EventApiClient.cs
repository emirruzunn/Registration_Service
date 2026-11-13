using System.Net.Http.Json;

namespace RegistrationService.Services
{
    using RegistrationService.Dtos;
    public interface IEventApiClient
    {
        Task<EventDto?> GetEventAsync(int eventId);
    }

    public class EventApiClient : IEventApiClient
    {
        private readonly HttpClient _httpClient;

        public EventApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EventDto?> GetEventAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/events/{eventId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EventDto>();
                }

                return null; 
            }
            catch (HttpRequestException)
            {
                throw new Exception("CLUB_EVENT_SERVICE_UNAVAILABLE");
            }
        }
    }
}