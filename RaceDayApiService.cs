using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services
{
    public class ApiResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
    }

    public class ApiResult<T> : ApiResult
    {
        public T? Data { get; set; }
    }

    /// <summary>
    /// Talks to RaceDay.API. After login the API's session cookie is kept in this user's
    /// own web session and sent with every later request, so the API knows who is calling.
    /// Network problems are returned as friendly errors instead of crashing the page.
    /// </summary>
    public class RaceDayApiService
    {
        private const string ApiCookieKey = "ApiCookie";
        private const string ApiCookieName = "RaceDay.API.Session";

        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _accessor;

        public RaceDayApiService(HttpClient http, IHttpContextAccessor accessor)
        {
            _http = http;
            _accessor = accessor;
        }

        private ISession? Session => _accessor.HttpContext?.Session;

        // ---------- low-level helpers ----------

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, url) { Content = content };
            var cookie = Session?.GetString(ApiCookieKey);
            if (!string.IsNullOrEmpty(cookie)) request.Headers.TryAddWithoutValidation("Cookie", cookie);
            return await _http.SendAsync(request);
        }

        private static JsonContent Json(object body) => JsonContent.Create(body);

        private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var text = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(text)) return null;
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString();
                    if (doc.RootElement.TryGetProperty("title", out var t)) return t.GetString();
                }
            }
            catch { /* body was not JSON */ }
            return null;
        }

        private async Task<ApiResult> DoAsync(HttpMethod method, string url, HttpContent? content, string fallbackError)
        {
            try
            {
                using var response = await SendAsync(method, url, content);
                if (response.IsSuccessStatusCode) return new ApiResult { Success = true, StatusCode = response.StatusCode };

                return new ApiResult
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    Error = await ReadMessageAsync(response) ?? fallbackError
                };
            }
            catch (HttpRequestException)
            {
                return new ApiResult { Success = false, Error = "Cannot reach the RaceDay API. Make sure RaceDay.API is running and ApiSettings:BaseUrl is correct." };
            }
            catch (TaskCanceledException)
            {
                return new ApiResult { Success = false, Error = "The RaceDay API took too long to respond." };
            }
        }

        private async Task<ApiResult<T>> GetAsync<T>(string url) where T : new()
        {
            try
            {
                using var response = await SendAsync(HttpMethod.Get, url);
                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResult<T>
                    {
                        Success = false,
                        StatusCode = response.StatusCode,
                        Error = await ReadMessageAsync(response) ?? "The request failed.",
                        Data = new T()
                    };
                }

                var data = await response.Content.ReadFromJsonAsync<T>();
                return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data ?? new T() };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<T> { Success = false, Data = new T(), Error = "Cannot reach the RaceDay API. Make sure RaceDay.API is running and ApiSettings:BaseUrl is correct." };
            }
            catch (TaskCanceledException)
            {
                return new ApiResult<T> { Success = false, Data = new T(), Error = "The RaceDay API took too long to respond." };
            }
            catch (JsonException)
            {
                return new ApiResult<T> { Success = false, Data = new T(), Error = "The API sent data in an unexpected format." };
            }
        }

        // ---------- auth ----------

        public async Task<ApiResult<LoginResponse>> LoginAsync(LoginViewModel model)
        {
            try
            {
                using var response = await _http.PostAsync("api/auth/login", Json(new { model.Email, model.Password }));
                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResult<LoginResponse>
                    {
                        Success = false,
                        StatusCode = response.StatusCode,
                        Error = await ReadMessageAsync(response) ?? "Invalid login attempt."
                    };
                }

                // Keep the API's session cookie in this user's web session
                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var apiCookie = cookies.FirstOrDefault(c => c.StartsWith(ApiCookieName + "=", StringComparison.Ordinal));
                    if (apiCookie != null) Session?.SetString(ApiCookieKey, apiCookie.Split(';')[0]);
                }

                var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return new ApiResult<LoginResponse> { Success = data != null, Data = data, Error = data == null ? "Unexpected response from API." : null };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<LoginResponse> { Success = false, Error = "Cannot reach the RaceDay API. Make sure RaceDay.API is running and ApiSettings:BaseUrl is correct." };
            }
            catch (TaskCanceledException)
            {
                return new ApiResult<LoginResponse> { Success = false, Error = "The RaceDay API took too long to respond." };
            }
        }

        public Task<ApiResult> RegisterAsync(RegisterViewModel model) =>
            DoAsync(HttpMethod.Post, "api/auth/register", Json(new
            {
                model.FullName, model.Email, model.Password, model.Role, model.OrganizationName, model.ContactNumber
            }), "Registration failed.");

        public async Task<ApiResult> LogoutAsync()
        {
            var result = await DoAsync(HttpMethod.Post, "api/auth/logout", null, "Logout failed.");
            Session?.Remove(ApiCookieKey);
            return result;
        }

        // ---------- events ----------

        public Task<ApiResult<List<Event>>> GetEventsAsync() => GetAsync<List<Event>>("api/events");
        public Task<ApiResult<List<Event>>> GetMyEventsAsync() => GetAsync<List<Event>>("api/events/mine");

        public async Task<ApiResult<Event>> GetEventAsync(int id)
        {
            try
            {
                using var response = await SendAsync(HttpMethod.Get, $"api/events/{id}");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<Event> { Success = false, StatusCode = response.StatusCode, Error = await ReadMessageAsync(response) ?? "Event not found." };

                return new ApiResult<Event> { Success = true, Data = await response.Content.ReadFromJsonAsync<Event>() };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<Event> { Success = false, Error = "Cannot reach the RaceDay API. Make sure RaceDay.API is running and ApiSettings:BaseUrl is correct." };
            }
        }

        private static object EventBody(Event m) => new
        {
            m.Title,
            Description = m.Description ?? string.Empty,
            m.EventType,
            m.Location,
            m.EventDate
        };

        public async Task<ApiResult<Event>> CreateEventAsync(Event model)
        {
            try
            {
                using var response = await SendAsync(HttpMethod.Post, "api/events", Json(EventBody(model)));
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<Event> { Success = false, StatusCode = response.StatusCode, Error = await ReadMessageAsync(response) ?? "Failed to create the event." };

                return new ApiResult<Event> { Success = true, Data = await response.Content.ReadFromJsonAsync<Event>() };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<Event> { Success = false, Error = "Cannot reach the RaceDay API. Make sure RaceDay.API is running and ApiSettings:BaseUrl is correct." };
            }
        }

        public Task<ApiResult> UpdateEventAsync(Event model) =>
            DoAsync(HttpMethod.Put, $"api/events/{model.EventId}", Json(EventBody(model)), "Failed to update the event.");

        public Task<ApiResult> DeleteEventAsync(int id) =>
            DoAsync(HttpMethod.Delete, $"api/events/{id}", null, "Failed to delete the event.");

        public Task<ApiResult> UploadBannerAsync(int eventId, IFormFile file) =>
            UploadAsync($"api/events/{eventId}/upload-banner", file);

        public Task<ApiResult> UploadProfilePictureAsync(IFormFile file) =>
            UploadAsync("api/users/upload-profile-picture", file);

        private async Task<ApiResult> UploadAsync(string url, IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            if (!string.IsNullOrEmpty(file.ContentType))
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);
            return await DoAsync(HttpMethod.Post, url, content, "Image upload failed. Check the Azure Blob Storage connection string / Azurite.");
        }

        // ---------- categories ----------

        public Task<ApiResult> CreateCategoryAsync(Category model) =>
            DoAsync(HttpMethod.Post, "api/categories", Json(new
            {
                model.EventId, model.Name, model.DistanceKm, model.EntryFee, model.MaxCapacity
            }), "Failed to add the category.");

        public Task<ApiResult> DeleteCategoryAsync(int id) =>
            DoAsync(HttpMethod.Delete, $"api/categories/{id}", null, "Failed to delete the category.");

        // ---------- enrolments ----------

        public Task<ApiResult> EnrolAsync(int categoryId) =>
            DoAsync(HttpMethod.Post, "api/enrolments", Json(new { CategoryId = categoryId }), "Could not enrol in this category.");

        public Task<ApiResult<List<Enrolment>>> GetMyEnrolmentsAsync() => GetAsync<List<Enrolment>>("api/enrolments/my-enrolments");
        public Task<ApiResult<List<Enrolment>>> GetEventEnrolmentsAsync(int eventId) => GetAsync<List<Enrolment>>($"api/enrolments/event/{eventId}");

        // ---------- results ----------

        public Task<ApiResult<List<RaceResult>>> GetMyResultsAsync() => GetAsync<List<RaceResult>>("api/results/my-results");

        public Task<ApiResult> CaptureResultAsync(CaptureResultViewModel model) =>
            DoAsync(HttpMethod.Post, "api/results", Json(new
            {
                model.EnrolmentId, model.FinishTime, model.OverallPosition, model.CategoryPosition
            }), "Could not save the result.");

        // ---------- profile ----------

        public Task<ApiResult<UserProfile>> GetProfileAsync() => GetProfileInternalAsync();

        private async Task<ApiResult<UserProfile>> GetProfileInternalAsync()
        {
            try
            {
                using var response = await SendAsync(HttpMethod.Get, "api/users/profile");
                if (!response.IsSuccessStatusCode)
                    return new ApiResult<UserProfile> { Success = false, StatusCode = response.StatusCode, Error = await ReadMessageAsync(response) ?? "Could not load your profile." };

                return new ApiResult<UserProfile> { Success = true, Data = await response.Content.ReadFromJsonAsync<UserProfile>() };
            }
            catch (HttpRequestException)
            {
                return new ApiResult<UserProfile> { Success = false, Error = "Cannot reach the RaceDay API. Make sure RaceDay.API is running and ApiSettings:BaseUrl is correct." };
            }
        }

        public Task<ApiResult> UpdateProfileAsync(UserProfile model) =>
            DoAsync(HttpMethod.Put, "api/users/profile", Json(new
            {
                model.FullName, model.Email, model.OrganizationName, model.ContactNumber
            }), "Could not update your profile.");
    }
}
