using Microsoft.Graph;

namespace OneDriver.Net.Services.GraphApi;

public interface IGraphServiceClientFactory
{
    Task<GraphServiceClient> CreateGraphServiceClientAsync();
}
